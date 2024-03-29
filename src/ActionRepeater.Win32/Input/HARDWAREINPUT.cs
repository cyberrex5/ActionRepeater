﻿namespace ActionRepeater.Win32.Input;

/// <summary>Contains information about a simulated message generated by an input device other than a keyboard or mouse.</summary>
/// <remarks>
/// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-hardwareinput">Learn more about this API from docs.microsoft.com</see>.</para>
/// </remarks>
public struct HARDWAREINPUT
{
    /// <summary>
    /// <para>Type: <b>DWORD</b> The message generated by the input hardware.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-hardwareinput#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    public uint uMsg;
    /// <summary>
    /// <para>Type: <b>WORD</b> The low-order word of the <i>lParam </i> parameter for <b>uMsg</b>.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-hardwareinput#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    public ushort wParamL;
    /// <summary>
    /// <para>Type: <b>WORD</b> The high-order word of the <i>lParam </i> parameter for <b>uMsg</b>.</para>
    /// <para><see href="https://docs.microsoft.com/windows/win32/api//winuser/ns-winuser-hardwareinput#members">Read more on docs.microsoft.com</see>.</para>
    /// </summary>
    public ushort wParamH;
}
