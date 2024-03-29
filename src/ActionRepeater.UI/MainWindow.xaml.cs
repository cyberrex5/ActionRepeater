﻿using System;
using System.Diagnostics;
using System.IO;
using ActionRepeater.Core.Input;
using ActionRepeater.UI.Services;
using ActionRepeater.UI.ViewModels;
using ActionRepeater.UI.Views;
using ActionRepeater.Win32.WindowsAndMessages;
using ActionRepeater.Win32.WindowsAndMessages.Utilities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private readonly HomeView _homeView;
    private readonly OptionsView _optionsView;

    private readonly Recorder _recorder;
    private readonly WindowProperties _windowProperties;

    private readonly WindowMessageMonitor _msgMonitor;

    private readonly Thickness _titlebarOffset = new(33, 8, 0, 0);
    private readonly Windows.Graphics.RectInt32[] _dragRects = new Windows.Graphics.RectInt32[2];
    private const int DragRegionMinHeight = 18;

    private object? _prevSelectedMenuItem;

    public MainWindow(MainViewModel vm, HomeView homeView, OptionsView optionsView, Recorder recorder, WindowProperties windowProperties)
    {
        Handle = WinRT.Interop.WindowNative.GetWindowHandle(this);

        _vm = vm;

        _homeView = homeView;
        _optionsView = optionsView;

        _recorder = recorder;
        _windowProperties = windowProperties;

        _windowProperties.Handle = Handle;
        _windowProperties.DispatcherQueue = DispatcherQueue;

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
        _prevSelectedMenuItem = _navigationView.SelectedItem;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        bool isInitialNavigation = _prevSelectedMenuItem == null;
        Navigate((string)args.SelectedItemContainer.Tag, isInitialNavigation);
        _prevSelectedMenuItem = _navigationView.SelectedItem;
    }

    private void Navigate(string tag, bool suppressTransition = false)
    {
        bool isNavigatingRight = IsNavigatingRight(tag);

        switch (tag)
        {
            case OptionsTag:
                _navViewPresenter.Navigate(_optionsView, isNavigatingRight, suppressTransition);
                break;

            case var t when t.StartsWith("h_", StringComparison.Ordinal):
                if (_navViewPresenter.Content is not HomeView)
                {
                    _navViewPresenter.Navigate(_homeView, isNavigatingRight, suppressTransition);

                    _homeView.NavigateRibbon(tag, isNavigatingRight, true);
                    break;
                }

                _homeView.NavigateRibbon(tag, isNavigatingRight, suppressTransition);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    private bool IsNavigatingRight(string tag)
    {
        var items = _navigationView.MenuItems;
        int selectedItemIdx = items.IndexOf(_prevSelectedMenuItem);

        int nextItemIdx = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if ((string)((NavigationViewItem)items[i]).Tag == tag)
            {
                nextItemIdx = i;
                break;
            }
        }

        return nextItemIdx > selectedItemIdx;
    }
}
