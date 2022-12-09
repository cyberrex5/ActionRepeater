﻿using System.Collections.Generic;
using ActionRepeater.Core.Input;
using POINT = ActionRepeater.Win32.POINT;
using VirtualKey = ActionRepeater.Win32.Input.VirtualKey;

namespace ActionRepeater.Core.Action;

public static class ActionDescriptionTemplates
{
    public static readonly IReadOnlyDictionary<VirtualKey, string> VirtualKeyFriendlyNames = new Dictionary<VirtualKey, string>()
    {
        { VirtualKey.NO_KEY, "No Key" },
        { (VirtualKey)3, "Cancel" },
        { (VirtualKey)8, "Backspace" },
        { (VirtualKey)9, "Tab" },
        { (VirtualKey)12, "Clear" },
        { (VirtualKey)13, "Enter/Return" },
        { VirtualKey.CONTROL, "Control" },
        { (VirtualKey)19, "Pause" },
        { (VirtualKey)20, "Caps Lock" },
        { (VirtualKey)27, "Escape" },
        { (VirtualKey)28, "IME Convert" },
        { (VirtualKey)29, "IME NonConvert" },
        { (VirtualKey)30, "IME Accept" },
        { (VirtualKey)31, "IME Mode change request" },
        { (VirtualKey)32, "Spacebar" },
        { (VirtualKey)33, "Page Up" },
        { (VirtualKey)34, "Page Down" },
        { (VirtualKey)35, "End" },
        { (VirtualKey)36, "Home" },
        { (VirtualKey)37, "Left Arrow" },
        { (VirtualKey)38, "Up Arrow" },
        { (VirtualKey)39, "Right Arrow" },
        { (VirtualKey)40, "Down Arrow" },
        { (VirtualKey)41, "Select" },
        { (VirtualKey)42, "Print" },
        { (VirtualKey)43, "Execute" },
        { (VirtualKey)44, "Print Screen" },
        { (VirtualKey)45, "Insert" },
        { (VirtualKey)46, "Delete" },
        { (VirtualKey)47, "Help" },
        { (VirtualKey)48, "0" },
        { (VirtualKey)49, "1" },
        { (VirtualKey)50, "2" },
        { (VirtualKey)51, "3" },
        { (VirtualKey)52, "4" },
        { (VirtualKey)53, "5" },
        { (VirtualKey)54, "6" },
        { (VirtualKey)55, "7" },
        { (VirtualKey)56, "8" },
        { (VirtualKey)57, "9" },
        { (VirtualKey)65, "A" },
        { (VirtualKey)66, "B" },
        { (VirtualKey)67, "C" },
        { (VirtualKey)68, "D" },
        { (VirtualKey)69, "E" },
        { (VirtualKey)70, "F" },
        { (VirtualKey)71, "G" },
        { (VirtualKey)72, "H" },
        { (VirtualKey)73, "I" },
        { (VirtualKey)74, "J" },
        { (VirtualKey)75, "K" },
        { (VirtualKey)76, "L" },
        { (VirtualKey)77, "M" },
        { (VirtualKey)78, "N" },
        { (VirtualKey)79, "O" },
        { (VirtualKey)80, "P" },
        { (VirtualKey)81, "Q" },
        { (VirtualKey)82, "R" },
        { (VirtualKey)83, "S" },
        { (VirtualKey)84, "T" },
        { (VirtualKey)85, "U" },
        { (VirtualKey)86, "V" },
        { (VirtualKey)87, "W" },
        { (VirtualKey)88, "X" },
        { (VirtualKey)89, "Y" },
        { (VirtualKey)90, "Z" },
        { (VirtualKey)91, "Left Windows Key" },
        { (VirtualKey)92, "Right Windows Key" },
        { (VirtualKey)93, "Application/Menu (displays an application-specific context menu)" },
        { (VirtualKey)95, "Computer Sleep" },
        { (VirtualKey)96, "0 (numpad)" },
        { (VirtualKey)97, "1 (numpad)" },
        { (VirtualKey)98, "2 (numpad)" },
        { (VirtualKey)99, "3 (numpad)" },
        { (VirtualKey)100, "4 (numpad)" },
        { (VirtualKey)101, "5 (numpad)" },
        { (VirtualKey)102, "6 (numpad)" },
        { (VirtualKey)103, "7 (numpad)" },
        { (VirtualKey)104, "8 (numpad)" },
        { (VirtualKey)105, "9 (numpad)" },
        { (VirtualKey)106, "Multiply" },
        { (VirtualKey)107, "Add" },
        { (VirtualKey)108, "Separator" },
        { (VirtualKey)109, "Subtract" },
        { (VirtualKey)110, "Decimal" },
        { (VirtualKey)111, "Divide" },
        { (VirtualKey)112, "F1" },
        { (VirtualKey)113, "F2" },
        { (VirtualKey)114, "F3" },
        { (VirtualKey)115, "F4" },
        { (VirtualKey)116, "F5" },
        { (VirtualKey)117, "F6" },
        { (VirtualKey)118, "F7" },
        { (VirtualKey)119, "F8" },
        { (VirtualKey)120, "F9" },
        { (VirtualKey)121, "F10" },
        { (VirtualKey)122, "F11" },
        { (VirtualKey)123, "F12" },
        { (VirtualKey)124, "F13" },
        { (VirtualKey)125, "F14" },
        { (VirtualKey)126, "F15" },
        { (VirtualKey)127, "F16" },
        { (VirtualKey)128, "F17" },
        { (VirtualKey)129, "F18" },
        { (VirtualKey)130, "F19" },
        { (VirtualKey)131, "F20" },
        { (VirtualKey)132, "F21" },
        { (VirtualKey)133, "F22" },
        { (VirtualKey)134, "F23" },
        { (VirtualKey)135, "F24" },
        { (VirtualKey)144, "Num Lock" },
        { (VirtualKey)145, "Scroll Lock" },
        { (VirtualKey)160, "Left Shift" },
        { (VirtualKey)161, "Right Shift" },
        { (VirtualKey)162, "Left Ctrl" },
        { (VirtualKey)163, "Right Ctrl" },
        { (VirtualKey)164, "Left Alt" },
        { (VirtualKey)165, "Right Alt" },
        { (VirtualKey)166, "Browser Back" },
        { (VirtualKey)167, "Browser Forward" },
        { (VirtualKey)168, "Browser Refresh" },
        { (VirtualKey)169, "Browser Stop" },
        { (VirtualKey)170, "Browser Search" },
        { (VirtualKey)171, "Browser Favorites" },
        { (VirtualKey)172, "Browser Home" },
        { (VirtualKey)173, "Volume Mute" },
        { (VirtualKey)174, "Volume Down" },
        { (VirtualKey)175, "Volume Up" },
        { (VirtualKey)176, "Media Next Track" },
        { (VirtualKey)177, "Media Previous Track" },
        { (VirtualKey)178, "Media Stop" },
        { (VirtualKey)179, "Media Play Pause" },
        { (VirtualKey)180, "Launch Mail" },
        { (VirtualKey)181, "Select Media" },
        { (VirtualKey)182, "Launch Application1" },
        { (VirtualKey)183, "Launch Application2" },
        { (VirtualKey)186, "OEM 1 (varies by keyboard. for US Standard, Semicolon)" },
        { (VirtualKey)187, "OEM Addition" },
        { (VirtualKey)188, "OEM Comma" },
        { (VirtualKey)189, "OEM Minus" },
        { (VirtualKey)190, "OEM Period" },
        { (VirtualKey)191, "OEM 2 (varies by keyboard. for US Standard, Question Mark)" },
        { (VirtualKey)192, "OEM 3 (varies by keyboard. for US Standard, tilde)" },
        { (VirtualKey)193, "ABNT_C1 (Brazilian)" },
        { (VirtualKey)194, "ABNT_C2 (Brazilian)" },
        { (VirtualKey)219, "OEM 4 (varies by keyboard. for US Standard, Open Brackets)" },
        { (VirtualKey)220, "OEM 5 (varies by keyboard. for US Standard, Pipe)" },
        { (VirtualKey)221, "OEM 6 (varies by keyboard. for US Standard, Close Brackets)" },
        { (VirtualKey)222, "OEM 7 (varies by keyboard. for US Standard, Quotes)" },
        { (VirtualKey)223, "OEM 8" },
        { (VirtualKey)226, "OEM 102 (varies by keyboard. for US Standard, Backslash)" },
        { (VirtualKey)229, "A special key masking the real key being processed by an IME" },
        { (VirtualKey)240, "OEM ATTN" },
        { (VirtualKey)241, "OEM FINISH" },
        { (VirtualKey)242, "OEM COPY" },
        { (VirtualKey)243, "OEM AUTO" },
        { (VirtualKey)244, "OEM ENLW" },
        { (VirtualKey)245, "OEM BACKTAB" },
        { (VirtualKey)246, "ATTN" },
        { (VirtualKey)247, "CrSel" },
        { (VirtualKey)248, "ExSel" },
        { (VirtualKey)249, "Erase EOF" },
        { (VirtualKey)250, "Play" },
        { (VirtualKey)251, "Zoom" },
        { (VirtualKey)253, "PA1" },
        { (VirtualKey)254, "OEM Clear" },
        { (VirtualKey)0xC3, "Gamepad A" },
        { (VirtualKey)0xC4, "Gamepad B" },
        { (VirtualKey)0xC5, "Gamepad X" },
        { (VirtualKey)0xC6, "Gamepad Y" },
        { (VirtualKey)0xC7, "Gamepad Right Shoulder" },
        { (VirtualKey)0xC8, "Gamepad Left Shoulder" },
        { (VirtualKey)0xC9, "Gamepad Left Trigger" },
        { (VirtualKey)0xCA, "Gamepad Right Trigger" },
        { (VirtualKey)0xCB, "Gamepad Dpad Up" },
        { (VirtualKey)0xCC, "Gamepad Dpad Down" },
        { (VirtualKey)0xCD, "Gamepad Dpad Left" },
        { (VirtualKey)0xCE, "Gamepad Dpad Right" },
        { (VirtualKey)0xCF, "Gamepad Menu" },
        { (VirtualKey)0xD0, "Gamepad View" },
        { (VirtualKey)0xD1, "Gamepad Left Thumbstick Button" },
        { (VirtualKey)0xD2, "Gamepad Right Thumbstick Button" },
        { (VirtualKey)0xD3, "Gamepad Left Thumbstick Up" },
        { (VirtualKey)0xD4, "Gamepad Left Thumbstick Down" },
        { (VirtualKey)0xD5, "Gamepad Left Thumbstick Right" },
        { (VirtualKey)0xD6, "Gamepad Left Thumbstick Left" },
        { (VirtualKey)0xD7, "Gamepad Right Thumbstick Up" },
        { (VirtualKey)0xD8, "Gamepad Right Thumbstick Down" },
        { (VirtualKey)0xD9, "Gamepad Right Thumbstick Right" },
        { (VirtualKey)0xDA, "Gamepad Right Thumbstick Left" },
    };

    public static string Button(MouseButton b) => $"{b} Button";

    public static string ButtonPoint(MouseButton b, POINT p) => $"{b} Button at ({p.x}, {p.y})";

    public static string DurationMS(int ms) => $"{ms/1000.0:0.00} seconds";

    public static string WheelSteps(int count) => count < 0 ? $"{-count} steps backward" : $"{count} steps forward";

    public static string WheelSteps(int count, int ms)
        => count < 0 ? $"{-count} steps backward, over {DurationMS(ms)}" : $"{count} steps forward, over {DurationMS(ms)}";

    public static string HorizontalWheelSteps(int count) => count < 0 ? $"{-count} steps to the left" : $"{count} steps to the right";

    public static string HorizontalWheelSteps(int count, int ms)
    {
        if (ms == 0) return HorizontalWheelSteps(count);
        return count < 0 ? $"{-count} steps to the left, over {DurationMS(ms)}" : $"{count} steps to the right, over {DurationMS(ms)}";
    }

    public static string KeyFriendlyName(VirtualKey key) => VirtualKeyFriendlyNames.TryGetValue(key, out string? name) ? name : key.ToString();

    public static string KeyAutoRepeat(VirtualKey key) => $"{KeyFriendlyName(key)} (auto-repeat)";
}
