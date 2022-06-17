﻿using System.ComponentModel;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;

namespace ActionRepeater.UI.Controls;

public sealed partial class CmdBarButton : UserControl
{
    [Description("The FontIcon Glyph"), Category("Data")]
    public string Glyph
    {
        get => _fontIcon.Glyph;
        set => _fontIcon.Glyph = value;
    }

    [Description("Button Text"), Category("Data")]
    public string Text
    {
        get => _textBlcok.Text;
        set => _textBlcok.Text = value;
    }

    [Description("Button Width"), Category("Data")]
    public double ButtonWidth
    {
        get => _stackpanel.Width;
        set => _stackpanel.Width = value;
    }

    [Description("Button Height"), Category("Data")]
    public double ButtonHeight
    {
        get => _stackpanel.Height;
        set => _stackpanel.Height = value;
    }

    [Description("Button Command"), Category("Data")]
    public ICommand Command
    {
        get => _button.Command;
        set => _button.Command = value;
    }

    public CmdBarButton()
    {
        InitializeComponent();
    }
}
