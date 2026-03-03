using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager
    {
        public bool IsAnyInputHeld()
        {
            if (_suspended)
                return false;

            UpdateMenuBackLatchImmediate();

            if (IsAnyKeyboardKeyHeld())
                return true;

            return IsAnyJoystickButtonHeld();
        }

        public bool IsAnyMenuInputHeld()
        {
            if (_suspended)
                return false;

            if (IsAnyKeyboardKeyHeld(ignoreModifiers: true))
                return true;

            return IsAnyJoystickButtonHeld();
        }

        public bool IsMenuBackHeld()
        {
            if (_suspended)
                return false;

            if (IsDown(Key.Escape))
                return true;

            if (!_joystickEnabled)
                return false;

            if (TryGetJoystickState(out var state))
                return IsMenuBackHeld(state);

            return false;
        }

        public void LatchMenuBack()
        {
            _menuBackLatched = true;
        }

        public bool ShouldIgnoreMenuBack()
        {
            if (!_menuBackLatched)
                return false;
            if (IsMenuBackHeld())
                return true;
            _menuBackLatched = false;
            return false;
        }

        private bool IsAnyKeyboardKeyHeld(bool ignoreModifiers = false)
        {
            try
            {
                _keyboard.Acquire();
                var state = _keyboard.GetCurrentState();
                if (!ignoreModifiers)
                    return state.PressedKeys.Count > 0;

                foreach (var key in state.PressedKeys)
                {
                    if (key == Key.LeftControl || key == Key.RightControl ||
                        key == Key.LeftShift || key == Key.RightShift ||
                        key == Key.LeftAlt || key == Key.RightAlt)
                        continue;
                    return true;
                }

                return false;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        private bool TryGetKeyboardState(out KeyboardState state)
        {
            state = null!;
            try
            {
                _keyboard.Acquire();
                state = _keyboard.GetCurrentState();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
        }

        private bool IsAnyJoystickButtonHeld()
        {
            if (!_joystickEnabled)
                return false;

            if (_gamepad.IsAvailable)
            {
                _gamepad.Update();
                return _gamepad.State.HasAnyButtonDown();
            }

            var joystick = GetJoystickDevice();
            if (joystick == null || !joystick.IsAvailable)
                return false;

            if (!joystick.Update())
                return false;

            return joystick.State.HasAnyButtonDown();
        }

        private void UpdateMenuBackLatchImmediate()
        {
            if (!_menuBackLatched)
                return;
            if (!IsMenuBackHeldImmediate())
                _menuBackLatched = false;
        }

        private bool IsMenuBackHeldImmediate()
        {
            try
            {
                _keyboard.Acquire();
                var state = _keyboard.GetCurrentState();
                foreach (var key in state.PressedKeys)
                {
                    if (key == Key.Escape)
                        return true;
                }
            }
            catch (SharpDXException)
            {
            }

            if (_gamepad.IsAvailable)
            {
                _gamepad.Update();
                var state = _gamepad.State;
                return IsMenuBackHeld(state);
            }

            if (!_joystickEnabled)
                return false;

            var joystick = GetJoystickDevice();
            if (joystick == null || !joystick.IsAvailable)
                return false;

            return joystick.Update() && IsMenuBackHeld(joystick.State);
        }

        private bool IsMenuBackHeld(JoystickStateSnapshot state)
        {
            if (state.Pov4)
                return true;
            if (IgnoreJoystickAxesForMenuNavigation)
                return false;
            return state.X < -MenuBackThreshold;
        }
    }
}
