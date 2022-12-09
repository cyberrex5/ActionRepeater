﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ActionRepeater.Core;
using ActionRepeater.Core.Extentions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActionRepeater.UI.ViewModels;

public sealed class OptionsPageViewModel : ObservableObject
{
    public int CursorMovementMode
    {
        get => (int)CoreOptions.Instance.CursorMovementMode;
        set => CoreOptions.Instance.CursorMovementMode = (CursorMovementMode)value;
    }

    public bool IsCursorMovementNone => CoreOptions.Instance.CursorMovementMode == Core.CursorMovementMode.None;

    public bool UseCursorPosOnClicks
    {
        get => CoreOptions.Instance.UseCursorPosOnClicks;
        set => CoreOptions.Instance.UseCursorPosOnClicks = value;
    }

    public int MaxClickInterval
    {
        get => CoreOptions.Instance.MaxClickInterval;
        set => CoreOptions.Instance.MaxClickInterval = value;
    }

    public bool SendKeyAutoRepeat
    {
        get => CoreOptions.Instance.SendKeyAutoRepeat;
        set => CoreOptions.Instance.SendKeyAutoRepeat = value;
    }

    public int Theme
    {
        get => (int)UIOptions.Instance.Theme;
        set => UIOptions.Instance.Theme = (Theme)value;
    }

    public int OptionsFileLocation
    {
        get => (int)UIOptions.Instance.OptionsFileLocation;
        set => UIOptions.Instance.OptionsFileLocation = (OptionsFileLocation)value;
    }

    public IEnumerable<string> CursorMovementCBItems => Enum.GetNames<CursorMovementMode>().Select(x => x.AddSpacesBetweenWords());

    public IEnumerable<string> ThemeCBItems => Enum.GetNames<Theme>().Select(x => x.AddSpacesBetweenWords());

    private readonly PropertyChangedEventArgs _isCursorMovementModeNoneChangedArgs = new(nameof(IsCursorMovementNone));

    public OptionsPageViewModel()
    {
        PropertyChangedEventHandler callPropChange = (s, e) =>
        {
            OnPropertyChanged(e);
            if (e.PropertyName?.Equals(nameof(CursorMovementMode)) == true) OnPropertyChanged(_isCursorMovementModeNoneChangedArgs);
        };
        CoreOptions.Instance.PropertyChanged += callPropChange;
        UIOptions.Instance.PropertyChanged += callPropChange;
    }
}
