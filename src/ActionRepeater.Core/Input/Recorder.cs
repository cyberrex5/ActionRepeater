﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using ActionRepeater.Core.Action;
using ActionRepeater.Core.Utilities;
using ActionRepeater.Win32;
using ActionRepeater.Win32.Input;
using ActionRepeater.Win32.WindowsAndMessages.Utilities;

namespace ActionRepeater.Core.Input;

public sealed class Recorder
{
    private const int MinWaitDuration = 10; // in milliseconds

    public event EventHandler<bool>? IsRecordingChanged;
    public event EventHandler<InputAction>? ActionAdded;

    public bool IsRecording { get; private set; }

    public bool IsSubscribed { get; private set; }

    /// <summary>
    /// Function that returns true if the mouse click shouldnt be recorded.
    /// </summary>
    public Func<bool>? ShouldRecordMouseClick;

    private bool _shouldRecordMouseMovement;

    private readonly StopwatchSlim _mouseStopwatch = new();
    private readonly StopwatchSlim _actionStopwatch = new();
    private readonly TimeConsistencyChecker _wheelMsgTCC = new();

    private readonly CoreOptions _options;
    private readonly ActionCollection _actionCollection;
    private readonly Func<nint> _getWindowHandle;

    public Recorder(CoreOptions options, ActionCollection actionCollection, Func<nint> getWindowHandle)
    {
        _options = options;
        _actionCollection = actionCollection;
        _getWindowHandle = getWindowHandle;

        // this *could* cause a memory leak *if* the action collection should live longer than this recorder instance,
        // *but* this will not happen with the current usage of these, as both are registered as a singleton.
        // if for some reason in the future that changed this class would implement IDisposable and unsubscribe from the event in Dispose.
        _actionCollection.ActionsCountChanged += (_, _) =>
        {
            if (_actionCollection.Actions.Count == 0)
            {
                Restart();
            }
        };
    }

    private InputAction? GetLastAction() => _actionCollection.Actions.Count > 0 ? _actionCollection.Actions[^1] : null;

    private void ReplaceLastAction(InputAction action)
    {
        // the caller of this func always checks if the action list is not empty
        if (_actionCollection.Actions[^1] == _actionCollection.FilteredActions[^1])
        {
            ((ObservableCollectionEx<InputAction>)_actionCollection.FilteredActions)[^1] = action;
        }
        ((ObservableCollectionEx<InputAction>)_actionCollection.Actions)[^1] = action;
    }

    public void Restart()
    {
        _wheelMsgTCC.Reset();
        _actionStopwatch.Restart();
    }

    public void StartRecording()
    {
        if (!IsSubscribed)
        {
            throw new InvalidOperationException($"Not currently registered for raw input. Call {nameof(Recorder)}.{nameof(Recorder.RegisterRawInput)}() to register.");
        }

        Restart();

        _shouldRecordMouseMovement = _options.CursorMovementMode != CursorMovementMode.None;

        if (_shouldRecordMouseMovement)
        {
            if (_actionCollection.CursorPathStart is null)
            {
                Debug.Assert(_actionCollection.CursorPath.Count == 0, $"{nameof(_actionCollection.CursorPath)} is not empty.");

                _actionCollection.CursorPathStart = new(PInvoke.Helpers.GetCursorPos(), 0);
                Debug.WriteLine($"[{nameof(Recorder)}] set cursor start path pos to: {_actionCollection.CursorPathStart.Value.Delta}");
            }

            _mouseStopwatch.Restart();
        }

        IsRecording = true;
        IsRecordingChanged?.Invoke(this, true);
    }

    public void StopRecording()
    {
        if (!IsSubscribed) return;

        UnregisterRawInput();

        if (_actionCollection.CursorPath.Count == 0)
        {
            _actionCollection.CursorPathStart = null;
            Debug.WriteLine($"[{nameof(Recorder)}] set cursor start path pos to: null");
        }

        IsRecording = false;
        IsRecordingChanged?.Invoke(this, false);
    }

    public void RegisterRawInput()
    {
        nint targetWindowHandle = _getWindowHandle();
        var rid = new RAWINPUTDEVICE[]
        {
            new()
            {
                usUsagePage = UsagePage.GenericDesktopControl,
                usUsage = UsageId.Mouse,
                dwFlags = RawInputFlags.INPUTSINK,
                hwndTarget = targetWindowHandle
            },
            new()
            {
                usUsagePage = UsagePage.GenericDesktopControl,
                usUsage = UsageId.Keyboard,
                dwFlags = RawInputFlags.INPUTSINK,
                hwndTarget = targetWindowHandle
            }
        };

        if (!PInvoke.RegisterRawInputDevices(rid))
        {
            throw new Win32Exception();
        }

        IsSubscribed = true;
    }

    public void UnregisterRawInput()
    {
        var inputDevices = new RAWINPUTDEVICE[]
        {
            new()
            {
                usUsagePage = UsagePage.GenericDesktopControl,
                usUsage = UsageId.Mouse,
                dwFlags = RawInputFlags.REMOVE,
                hwndTarget = nint.Zero // hwndTarget must be nint.Zero when RawInputFlags.REMOVE is set
            },
            new()
            {
                usUsagePage = UsagePage.GenericDesktopControl,
                usUsage = UsageId.Keyboard,
                dwFlags = RawInputFlags.REMOVE,
                hwndTarget = nint.Zero
            }
        };

        if (!PInvoke.RegisterRawInputDevices(inputDevices))
        {
            throw new Win32Exception();
        }

        IsSubscribed = false;
    }

    public void OnInputMessage(WindowMessageEventArgs e)
    {
        var inputCode = unchecked(e.Message.wParam & 0xff);
        if (!PInvoke.GetRawInputData(e.Message.lParam, out RAWINPUT inputData))
        {
            Debug.WriteLine($"[{nameof(Recorder)}] ERROR RETRIEVING RAW INPUT");
        }

        switch (inputData.header.dwType)
        {
            case RawInputType.Mouse:
                RAWMOUSE data = inputData.data.mouse;

                if (data.rawButtonData.buttonInfo.usButtonFlags == 0) // move event
                {
                    if (!_shouldRecordMouseMovement) break;

                    if (data.usFlags.HasFlag(RawMouseState.MOVE_ABSOLUTE))
                    {
                        //bool isVirtualDesktop = data.usFlags.HasFlag(RawMouseState.VIRTUAL_DESKTOP);
                        throw new NotSupportedException($"Mouse movement mode not supported. ({data.usFlags})");
                    }
                    else
                    {
                        OnMouseMove(data.lLastX, data.lLastY);
                    }
                }
                else // button/wheel event
                {
                    var buttonInfo = data.rawButtonData.buttonInfo;
                    switch (buttonInfo.usButtonFlags)
                    {
                        case RawMouseButtonState.HWHEEL:
                        case RawMouseButtonState.WHEEL:
                            OnMouseWheelMessage(buttonInfo);
                            break;

                        default:
                            OnMouseButtonMessage(buttonInfo.usButtonFlags);
                            break;
                    }
                }

                break;

            case RawInputType.Keyboard:
                VirtualKey key = inputData.data.keyboard.VKey;
                if (key == VirtualKey.NO_KEY || (ushort)key == 255) break;

                OnKeyboardMessage(key, inputData.data.keyboard.Flags);
                break;
        }

        if (inputCode == 0)
        {
            e.Handled = true;
            e.Result = PInvoke.DefWindowProc(e.Message.hwnd, e.Message.message, e.Message.wParam, e.Message.lParam);
        }
    }

    private void OnMouseMove(int deltaX, int deltaY)
    {
        long nsSinceLastMov = _mouseStopwatch.RestartAndGetElapsedNS();
        _actionCollection.CursorPath.Add(new(new POINT(deltaX, deltaY), nsSinceLastMov));
    }

    private void OnMouseWheelMessage(RAWMOUSE.RawButtonData.RawButtonInfo buttonInfo)
    {
        int wheelStepCount = unchecked((short)buttonInfo.usButtonData) / 120;

        if (_wheelMsgTCC.UpdateAndCheckIfConsistent() && GetLastAction() is MouseWheelAction lastMWAction)
        {
            int prevWheelStepCount = lastMWAction.StepCount;

            if ((wheelStepCount < 0) == (prevWheelStepCount < 0))
            {
                lastMWAction.StepCount = prevWheelStepCount + wheelStepCount;
                lastMWAction.DurationMS = _wheelMsgTCC.TickDeltasTotal;
                return;
            }
        }

        AddAction(new MouseWheelAction(buttonInfo.usButtonFlags == RawMouseButtonState.HWHEEL, wheelStepCount));
    }

    private void OnMouseButtonMessage(RawMouseButtonState buttonState)
    {
        var (button, type) = buttonState switch
        {
            RawMouseButtonState.LEFT_BUTTON_DOWN   => (MouseButton.Left, MouseButtonActionType.MouseButtonDown),
            RawMouseButtonState.LEFT_BUTTON_UP     => (MouseButton.Left, MouseButtonActionType.MouseButtonUp),

            RawMouseButtonState.RIGHT_BUTTON_DOWN  => (MouseButton.Right, MouseButtonActionType.MouseButtonDown),
            RawMouseButtonState.RIGHT_BUTTON_UP    => (MouseButton.Right, MouseButtonActionType.MouseButtonUp),

            RawMouseButtonState.MIDDLE_BUTTON_DOWN => (MouseButton.Middle, MouseButtonActionType.MouseButtonDown),
            RawMouseButtonState.MIDDLE_BUTTON_UP   => (MouseButton.Middle, MouseButtonActionType.MouseButtonUp),

            RawMouseButtonState.XBUTTON1_DOWN      => (MouseButton.X1, MouseButtonActionType.MouseButtonDown),
            RawMouseButtonState.XBUTTON1_UP        => (MouseButton.X1, MouseButtonActionType.MouseButtonUp),

            RawMouseButtonState.XBUTTON2_DOWN      => (MouseButton.X2, MouseButtonActionType.MouseButtonDown),
            RawMouseButtonState.XBUTTON2_UP        => (MouseButton.X2, MouseButtonActionType.MouseButtonUp),

            _ => throw new NotSupportedException($"{buttonState} not supported.")
        };

        if (button == MouseButton.Left && ShouldRecordMouseClick?.Invoke() == true) return;

        AddAction(new MouseButtonAction(type, button, PInvoke.Helpers.GetCursorPos(), _options.UseCursorPosOnClicks));
    }

    private void OnKeyboardMessage(VirtualKey key, RawInputKeyFlags keyFlags)
    {
        if (keyFlags.HasFlag(RawInputKeyFlags.BREAK)) // key up
        {
            AddAction(new KeyAction(KeyActionType.KeyUp, key));
        }
        else //if (keyFlags.HasFlag(RawInputKeyFlags.MAKE))
        {
            AddAction(new KeyAction(KeyActionType.KeyDown, key, IsAutoRepeat()));
        }

        bool IsAutoRepeat()
        {
            var actions = _actionCollection.Actions;

            for (int i = actions.Count - 1; i > -1; --i)
            {
                if (actions[i] is KeyAction keyActionI && keyActionI.Key == key)
                {
                    return keyActionI.ActionType == KeyActionType.KeyDown;
                }
            }

            return false;
        }
    }

    private void AddAction(InputAction action)
    {
        if (action is not MouseWheelAction)
        {
            _wheelMsgTCC.Reset();
        }

        int elapsedMS = (int)_actionStopwatch.RestartAndGetElapsedMS();

        if (elapsedMS <= _options.MaxClickInterval)
        {
            if (CheckAndReplaceWithClickAction(action))
            {
                return;
            }
        }

        if (elapsedMS >= MinWaitDuration)
        {
            _actionCollection.Add(new WaitAction(elapsedMS));
        }

        _actionCollection.Add(action);

        ActionAdded?.Invoke(this, action);
    }

    /// <returns>true if a click action was added, otherwise false.</returns>
    private bool CheckAndReplaceWithClickAction(InputAction currentActionToAdd)
    {
        if (currentActionToAdd is KeyAction curKeyAction
            && curKeyAction.ActionType == KeyActionType.KeyUp
            && GetLastAction() is KeyAction lastKeyAction
            && lastKeyAction.ActionType == KeyActionType.KeyDown
            && !lastKeyAction.IsAutoRepeat
            && lastKeyAction.Key == curKeyAction.Key)
        {
            ReplaceLastAction(new KeyAction(KeyActionType.KeyPress, curKeyAction.Key));
            return true;
        }

        if (currentActionToAdd is MouseButtonAction curMBAction
            && curMBAction.ActionType == MouseButtonActionType.MouseButtonUp
            && GetLastAction() is MouseButtonAction lastMBAction
            && lastMBAction.ActionType == MouseButtonActionType.MouseButtonDown
            && lastMBAction.Button == curMBAction.Button
            && lastMBAction.Position == curMBAction.Position)
        {
            ReplaceLastAction(new MouseButtonAction(MouseButtonActionType.MouseButtonClick, curMBAction.Button, curMBAction.Position, curMBAction.UsePosition));
            return true;
        }

        return false;
    }
}
