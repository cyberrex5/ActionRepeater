﻿using System.Runtime.InteropServices;

namespace ActionRepeater.Win32.WindowsAndMessages;

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
public unsafe delegate nint SUBCLASSPROC(nint hWnd, uint uMsg, nuint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData);
