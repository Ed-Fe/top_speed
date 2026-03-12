using System;
using System.Collections.Generic;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Input
{
    internal interface IGameInput : IDisposable
    {
        event Action? JoystickScanTimedOut;

        InputState Current { get; }
        bool ActiveJoystickIsRacingWheel { get; }
        bool IgnoreJoystickAxesForMenuNavigation { get; }
        IVibrationDevice? VibrationDevice { get; }

        void Update();
        bool IsDown(Key key);
        bool WasPressed(Key key);
        bool TryGetJoystickState(out JoystickStateSnapshot state);
        void SetDeviceMode(InputDeviceMode mode);
        bool TryGetPendingJoystickChoices(out IReadOnlyList<JoystickChoice> choices);
        bool TrySelectJoystick(Guid instanceGuid);
        bool IsAnyInputHeld();
        bool IsAnyMenuInputHeld();
        bool IsMenuBackHeld();
        void LatchMenuBack();
        bool ShouldIgnoreMenuBack();
        void ResetState();
        void Suspend();
        void Resume();
    }
}
