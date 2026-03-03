using SharpDX.DirectInput;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        private bool TryHandlePendingTitle(InputManager input)
        {
            if (!_titlePending)
                return true;

            if (input.IsAnyMenuInputHeld())
                return false;

            _titlePending = false;
            AnnounceTitle();
            return true;
        }

        private UpdateInputState CaptureInputState(InputManager input)
        {
            var state = new UpdateInputState(
                input.WasPressed(Key.Up),
                input.WasPressed(Key.Down),
                input.WasPressed(Key.Home),
                input.WasPressed(Key.End),
                input.WasPressed(Key.Left),
                input.WasPressed(Key.Right),
                input.WasPressed(Key.PageUp),
                input.WasPressed(Key.PageDown),
                input.WasPressed(Key.Return) || input.WasPressed(Key.NumberPadEnter),
                input.WasPressed(Key.Escape));

            if (input.TryGetJoystickState(out var joystick))
            {
                var useAxes = !input.IgnoreJoystickAxesForMenuNavigation;
                if (!_hasJoystickCenter && MenuInputUtil.IsNearCenter(joystick, useAxes))
                {
                    _joystickCenter = joystick;
                    _hasJoystickCenter = true;
                }

                var previous = _hasPrevJoystick ? _prevJoystick : _joystickCenter;
                state.MoveUp |= MenuInputUtil.WasJoystickUpPressed(joystick, previous, useAxes);
                state.MoveDown |= MenuInputUtil.WasJoystickDownPressed(joystick, previous, useAxes);
                state.Activate |= MenuInputUtil.WasJoystickActivatePressed(joystick, previous, useAxes);
                state.Back |= MenuInputUtil.WasJoystickBackPressed(joystick, previous, useAxes);
                _prevJoystick = joystick;
                _hasPrevJoystick = true;
            }
            else
            {
                _hasPrevJoystick = false;
            }

            return state;
        }

        private bool TryHandleHeldInputGate(InputManager input, UpdateInputState state, out MenuUpdateResult result)
        {
            result = MenuUpdateResult.None;
            if (!_ignoreHeldInput)
                return false;

            if (input.IsMenuBackHeld())
            {
                input.LatchMenuBack();
                _ignoreHeldInput = false;
                _autoFocusPending = false;
                result = MenuUpdateResult.Back;
                return true;
            }

            if (state.MoveUp)
            {
                _ignoreHeldInput = false;
                _autoFocusPending = false;
                MoveToIndex(_items.Count - 1);
                return true;
            }

            if (state.MoveDown)
            {
                _ignoreHeldInput = false;
                _autoFocusPending = false;
                MoveToIndex(0);
                return true;
            }

            if (state.MoveHome)
            {
                _ignoreHeldInput = false;
                _autoFocusPending = false;
                MoveToIndex(0);
                return true;
            }

            if (state.MoveEnd)
            {
                _ignoreHeldInput = false;
                _autoFocusPending = false;
                MoveToIndex(_items.Count - 1);
                return true;
            }

            if (state.Activate || state.Back)
            {
                _ignoreHeldInput = false;
                return false;
            }

            if (input.IsAnyMenuInputHeld())
                return true;

            _ignoreHeldInput = false;
            input.ResetState();
            return false;
        }

        private struct UpdateInputState
        {
            public UpdateInputState(
                bool moveUp,
                bool moveDown,
                bool moveHome,
                bool moveEnd,
                bool moveLeft,
                bool moveRight,
                bool pageUp,
                bool pageDown,
                bool activate,
                bool back)
            {
                MoveUp = moveUp;
                MoveDown = moveDown;
                MoveHome = moveHome;
                MoveEnd = moveEnd;
                MoveLeft = moveLeft;
                MoveRight = moveRight;
                PageUp = pageUp;
                PageDown = pageDown;
                Activate = activate;
                Back = back;
            }

            public bool MoveUp;
            public bool MoveDown;
            public bool MoveHome;
            public bool MoveEnd;
            public bool MoveLeft;
            public bool MoveRight;
            public bool PageUp;
            public bool PageDown;
            public bool Activate;
            public bool Back;
        }
    }
}
