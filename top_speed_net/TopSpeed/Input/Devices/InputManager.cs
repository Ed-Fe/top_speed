using System;
using System.Collections.Generic;
using System.Threading;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager : IDisposable
    {
        private const int JoystickRescanIntervalMs = 1000;
        private const int JoystickScanTimeoutMs = 5000;
        private const int MenuBackThreshold = 50;

        private readonly DirectInput _directInput;
        private readonly Keyboard _keyboard;
        private readonly GamepadDevice _gamepad;
        private JoystickDevice? _joystick;
        private readonly InputState _current;
        private readonly InputState _previous;
        private readonly bool[] _keyLatch;
        private readonly IntPtr _windowHandle;
        private int _lastJoystickScan;
        private bool _suspended;
        private bool _menuBackLatched;
        private readonly object _hidLock = new object();
        private readonly object _hidScanLock = new object();
        private Thread? _hidScanThread;
        private CancellationTokenSource? _hidScanCts;
        private bool _joystickEnabled;
        private List<JoystickChoice>? _pendingJoystickChoices;
        private bool _activeJoystickIsRacingWheel;
        private bool _disposed;

        public InputState Current => _current;
        public bool IgnoreJoystickAxesForMenuNavigation => _joystickEnabled && !_gamepad.IsAvailable && _activeJoystickIsRacingWheel;

        public event Action? JoystickScanTimedOut;

        public InputManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _directInput = new DirectInput();
            _keyboard = new Keyboard(_directInput);
            _keyboard.Properties.BufferSize = 128;
            _keyboard.SetCooperativeLevel(windowHandle, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
            _gamepad = new GamepadDevice();
            _current = new InputState();
            _previous = new InputState();
            _keyLatch = new bool[256];
            TryAcquire();
        }

        public bool IsDown(Key key) => _current.IsDown(key);

        public IVibrationDevice? VibrationDevice => _gamepad.IsAvailable
            ? (_joystickEnabled ? _gamepad : null)
            : (_joystickEnabled ? GetJoystickDevice() : null);

        public void SetDeviceMode(InputDeviceMode mode)
        {
            var enableJoystick = mode != InputDeviceMode.Keyboard;
            if (enableJoystick == _joystickEnabled)
                return;

            _joystickEnabled = enableJoystick;
            if (!_joystickEnabled)
            {
                StopHidScan();
                lock (_hidLock)
                {
                    _pendingJoystickChoices = null;
                }
                ClearJoystickDevice();
                return;
            }

            if (_gamepad.IsAvailable)
            {
                lock (_hidLock)
                {
                    _pendingJoystickChoices = null;
                    _activeJoystickIsRacingWheel = false;
                }
                return;
            }

            if (GetJoystickDevice() == null)
                StartHidScan();
        }

        private JoystickDevice? GetJoystickDevice()
        {
            lock (_hidLock)
            {
                return _joystick != null && _joystick.IsAvailable ? _joystick : null;
            }
        }

        public bool TryGetPendingJoystickChoices(out IReadOnlyList<JoystickChoice> choices)
        {
            lock (_hidLock)
            {
                if (_pendingJoystickChoices == null || _pendingJoystickChoices.Count == 0)
                {
                    choices = Array.Empty<JoystickChoice>();
                    return false;
                }

                choices = _pendingJoystickChoices.ToArray();
                return true;
            }
        }

        public bool TrySelectJoystick(Guid instanceGuid)
        {
            if (instanceGuid == Guid.Empty)
                return false;

            List<JoystickChoice>? pendingChoices;
            lock (_hidLock)
            {
                pendingChoices = _pendingJoystickChoices == null
                    ? null
                    : new List<JoystickChoice>(_pendingJoystickChoices);
            }

            JoystickChoice? selected = null;
            if (pendingChoices != null)
            {
                for (var i = 0; i < pendingChoices.Count; i++)
                {
                    if (pendingChoices[i].InstanceGuid == instanceGuid)
                    {
                        selected = pendingChoices[i];
                        break;
                    }
                }
            }

            if (selected == null)
            {
                var discovered = JoystickDevice.Discover(_directInput);
                for (var i = 0; i < discovered.Count; i++)
                {
                    if (discovered[i].InstanceGuid == instanceGuid)
                    {
                        selected = discovered[i];
                        break;
                    }
                }
            }

            if (selected == null)
                return false;

            if (!TryAttachJoystick(selected))
                return false;

            lock (_hidLock)
            {
                _pendingJoystickChoices = null;
            }
            StopHidScan();
            return true;
        }
    }
}
