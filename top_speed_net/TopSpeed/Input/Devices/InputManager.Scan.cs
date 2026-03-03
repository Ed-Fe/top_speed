using System;
using System.Collections.Generic;
using System.Threading;
using SharpDX;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager
    {
        private bool TryRescanJoystick(bool force = false)
        {
            if (_disposed)
                return false;
            var now = Environment.TickCount;
            if (!force && unchecked((uint)(now - _lastJoystickScan)) < (uint)JoystickRescanIntervalMs)
                return false;
            _lastJoystickScan = now;

            List<JoystickChoice> discovered;
            try
            {
                discovered = JoystickDevice.Discover(_directInput);
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            if (discovered.Count == 0)
                return false;

            if (discovered.Count == 1)
            {
                lock (_hidLock)
                {
                    _pendingJoystickChoices = null;
                }
                return TryAttachJoystick(discovered[0]);
            }

            lock (_hidLock)
            {
                _pendingJoystickChoices = discovered;
                _activeJoystickIsRacingWheel = false;
            }
            ClearJoystickDevice();
            return true;
        }

        private void StartHidScan()
        {
            if (_disposed || !_joystickEnabled || _gamepad.IsAvailable)
                return;
            lock (_hidScanLock)
            {
                if (_hidScanThread != null && _hidScanThread.IsAlive)
                    return;
                _hidScanCts?.Cancel();
                _hidScanCts?.Dispose();
                _hidScanCts = new CancellationTokenSource();
                var token = _hidScanCts.Token;
                _hidScanThread = new Thread(() => HidScanWorker(token))
                {
                    IsBackground = true,
                    Name = "JoystickScan"
                };
                _hidScanThread.Start();
            }
        }

        private void StopHidScan()
        {
            CancellationTokenSource? cts;
            Thread? thread;
            lock (_hidScanLock)
            {
                cts = _hidScanCts;
                thread = _hidScanThread;
                _hidScanCts = null;
                _hidScanThread = null;
            }

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            if (thread != null && thread.IsAlive)
                thread.Join(JoystickRescanIntervalMs + 500);
        }

        private void HidScanWorker(CancellationToken token)
        {
            var start = Environment.TickCount;
            while (true)
            {
                if (token.IsCancellationRequested || _disposed || !_joystickEnabled)
                    return;

                if (_gamepad.IsAvailable)
                    return;

                if (TryRescanJoystick(force: true))
                    return;

                var elapsed = unchecked((uint)(Environment.TickCount - start));
                if (elapsed >= (uint)JoystickScanTimeoutMs)
                {
                    JoystickScanTimedOut?.Invoke();
                    return;
                }

                if (token.WaitHandle.WaitOne(JoystickRescanIntervalMs))
                    return;
            }
        }

        private void ClearJoystickDevice()
        {
            JoystickDevice? oldJoystick;
            lock (_hidLock)
            {
                oldJoystick = _joystick;
                _joystick = null;
                _activeJoystickIsRacingWheel = false;
            }
            oldJoystick?.Dispose();
        }

        private bool TryAttachJoystick(JoystickChoice choice)
        {
            JoystickDevice? newJoystick;
            try
            {
                newJoystick = new JoystickDevice(_directInput, _windowHandle, choice);
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            if (!newJoystick.IsAvailable)
            {
                newJoystick.Dispose();
                return false;
            }

            JoystickDevice? oldJoystick;
            lock (_hidLock)
            {
                oldJoystick = _joystick;
                _joystick = newJoystick;
                _activeJoystickIsRacingWheel = choice.IsRacingWheel;
            }

            oldJoystick?.Dispose();
            return true;
        }
    }
}
