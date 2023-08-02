﻿using System;
using System.Diagnostics;
using System.IO;
using ActionRepeater.Core.Input;
using ActionRepeater.UI.Pages;
using ActionRepeater.UI.Services;
using ActionRepeater.UI.ViewModels;
using ActionRepeater.Win32.WindowsAndMessages;
using ActionRepeater.Win32.WindowsAndMessages.Utilities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace ActionRepeater.UI;

public sealed partial class MainWindow : Window
{
    private const string WindowTitle = "ActionRepeater";
    private const int StartupWidth = 520;
    private const int StartupHeight = 700;
    private const int MinWidth = 394;
    private const int MinHeight = 206;

    public const string HomeRibbonTag = "h_homeRibbon";
    public const string AddRibbonTag = "h_addRibbon";
    public const string OptionsTag = "o";

    public nint Handle { get; }

    private readonly MainViewModel _vm;

    private readonly Recorder _recorder;
    private readonly HomePageParameter _homePageParameter;
    private readonly OptionsPageParameter _optionsPageParameter;
    private readonly WindowProperties _windowProperties;

    private readonly WindowMessageMonitor _msgMonitor;

    private readonly Thickness _titlebarOffset = new(33, 8, 0, 0);
    private readonly Windows.Graphics.RectInt32[] _dragRects = new Windows.Graphics.RectInt32[2];
    private const int DragRegionMinHeight = 18;

    public MainWindow(MainViewModel vm, Recorder recorder, HomePageParameter homePageParameter, OptionsPageParameter optionsPageParameter, WindowProperties windowProperties)
    {
        Handle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        windowProperties.Handle = Handle;
        windowProperties.DispatcherQueue = DispatcherQueue;

        Title = WindowTitle;
        App.SetWindowSize(Handle, StartupWidth, StartupHeight);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, @"Assets\Icon.ico"));
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            SetTitleBarColors();
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }

        if (App.Current.RequestedTheme == ApplicationTheme.Dark)
        {
            Win32.PInvoke.Helpers.SetWindowImmersiveDarkMode(Handle, true);
        }

        _msgMonitor = new(Handle);
        _msgMonitor.WindowMessageReceived += OnWindowMessageReceived;

        _vm = vm;

        _recorder = recorder;
        _homePageParameter = homePageParameter;
        _optionsPageParameter = optionsPageParameter;
        _windowProperties = windowProperties;

        InitializeComponent();

        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            _rectangle.Margin = _rectangle.Margin with { Top = _rectangle.Margin.Top + _titlebarOffset.Top };

            var fileButtonMargin = _fileButtonMenuBar.Margin;
            _fileButtonMenuBar.Margin = fileButtonMargin with
            {
                Left = fileButtonMargin.Left + _titlebarOffset.Left,
                Top = fileButtonMargin.Top + _titlebarOffset.Top
            };

            _navigationView.Margin = _navigationView.Margin with { Top = _navigationView.Margin.Top + _titlebarOffset.Top };

            _navViewOffsetItem.Width += _titlebarOffset.Left;

            _titlebarGrid.Visibility = Visibility.Visible;

            _titlebarGrid.SizeChanged += TitleBarGrid_SizeChanged;
        }

        // workaround because x:Bind-ing isnt working for some reason (it seems to bind after the window loses focus for the first time?)
        _openMenuItem.Command = _vm.ImportActionsCommand;
        _saveMenuItem.Command = _vm.ExportActionsCommand;
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        _windowProperties.XamlRoot = _grid.XamlRoot;
    }

    /// <summary>
    /// Set titlebar colors according to theme. WinUi doesnt do this automatically for some reason.
    /// </summary>
    private void SetTitleBarColors()
    {
        Debug.Assert(AppWindowTitleBar.IsCustomizationSupported());

        var titlebar = AppWindow.TitleBar;
        var bg         = App.Current.RequestedTheme == ApplicationTheme.Dark ? Windows.UI.Color.FromArgb(255, 0, 0, 0)       : Windows.UI.Color.FromArgb(255, 255, 255, 255);
        var hoverBG    = App.Current.RequestedTheme == ApplicationTheme.Dark ? Windows.UI.Color.FromArgb(255, 25, 25, 25)    : Windows.UI.Color.FromArgb(255, 230, 230, 230);
        var pressBG    = App.Current.RequestedTheme == ApplicationTheme.Dark ? Windows.UI.Color.FromArgb(255, 51, 51, 51)    : Windows.UI.Color.FromArgb(255, 204, 204, 204);
        var fg         = App.Current.RequestedTheme == ApplicationTheme.Dark ? Windows.UI.Color.FromArgb(255, 255, 255, 255) : Windows.UI.Color.FromArgb(255, 0, 0, 0);
        var inavtiveFG = App.Current.RequestedTheme == ApplicationTheme.Dark ? Windows.UI.Color.FromArgb(255, 102, 102, 102) : Windows.UI.Color.FromArgb(255, 153, 153, 153);

        titlebar.BackgroundColor = bg;
        titlebar.ButtonBackgroundColor = bg;
        titlebar.InactiveBackgroundColor = bg;
        titlebar.ButtonInactiveBackgroundColor = bg;
        titlebar.ButtonHoverBackgroundColor = hoverBG;
        titlebar.ButtonPressedBackgroundColor = pressBG;
        titlebar.ButtonForegroundColor = fg;
        titlebar.ButtonHoverForegroundColor = fg;
        titlebar.ButtonInactiveForegroundColor = inavtiveFG;
        titlebar.ButtonPressedForegroundColor = fg;
    }

    private void UpdateDragRegion()
    {
        Debug.Assert(AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar);

        var item = (FrameworkElement)_navigationView.MenuItems[^1];

        var gridMargin = Math.Abs(_titlebarGrid.TransformToVisual(_grid).TransformPoint(default).X);

        var dragRegionOffset = item.TransformToVisual(_grid).TransformPoint(new(0, 0)).X + item.ActualWidth + gridMargin;
        var windowWidth = _grid.ActualWidth + gridMargin * 2;

        var scalingFactor = App.GetWindowScalingFactor(Handle);

        const int resizeAreaOffset = 0;

        _dragRects[0] = new()
        {
            X = (int)(dragRegionOffset * scalingFactor),
            Y = resizeAreaOffset,
            Height = (int)((_titlebarGrid.ActualHeight - resizeAreaOffset) * scalingFactor),
            Width = (int)((windowWidth - dragRegionOffset) * scalingFactor)
        };

        _dragRects[1] = new()
        {
            X = 0,
            Y = resizeAreaOffset,
            Height = (int)((DragRegionMinHeight - resizeAreaOffset) * scalingFactor),
            Width = (int)(dragRegionOffset * scalingFactor)
        };

        AppWindow.TitleBar.SetDragRectangles(_dragRects);
    }

    private unsafe void OnWindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        switch (e.MessageType)
        {
            case WindowMessage.INPUT:
                _recorder.OnInputMessage(e);
                break;

            case WindowMessage.GETMINMAXINFO:
                float scalingFactor = App.GetWindowScalingFactor(Handle);
                MINMAXINFO* info = (MINMAXINFO*)e.Message.lParam;
                info->ptMinTrackSize.x = (int)(MinWidth * scalingFactor);
                info->ptMinTrackSize.y = (int)(MinHeight * scalingFactor);
                break;
        }
    }

    private void TitleBarGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (AppWindow.TitleBar.ExtendsContentIntoTitleBar) UpdateDragRegion();
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        _navigationView.SelectedItem = _navigationView.MenuItems[1];
        Navigate(HomeRibbonTag, new SuppressNavigationTransitionInfo());
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        Navigate((string)args.SelectedItemContainer.Tag, args.RecommendedNavigationTransitionInfo);
    }

    private void Navigate(string tag, NavigationTransitionInfo navInfo)
    {
        switch (tag)
        {
            case OptionsTag:
                _contentFrame.Navigate(typeof(OptionsPage), _optionsPageParameter, navInfo);
                break;

            case var t when t.StartsWith("h_"):
                if (_contentFrame.Content is not HomePage)
                {
                    _contentFrame.Navigate(typeof(HomePage), _homePageParameter, navInfo);
                    ((HomePage)_contentFrame.Content).NavigateRibbon(tag, new SuppressNavigationTransitionInfo());
                    break;
                }

                ((HomePage)_contentFrame.Content).NavigateRibbon(tag, navInfo);
                break;

            default:
                throw new NotImplementedException();
        }
    }
}
