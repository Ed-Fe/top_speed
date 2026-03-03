using System;
using System.Collections.Generic;
using System.Globalization;
using SharpDX;
using SharpDX.DirectInput;

namespace TopSpeed.Input
{
    internal sealed class JoystickDevice : IVibrationDevice
    {
        private readonly Joystick? _joystick;
        private readonly Guid _instanceGuid;
        private readonly string _displayName;
        private readonly bool _isRacingWheel;
        private JoystickStateSnapshot _state;
        private readonly Dictionary<VibrationEffectType, ForceFeedbackEffect> _effects = new Dictionary<VibrationEffectType, ForceFeedbackEffect>();
        private bool _connected;

        public JoystickDevice(DirectInput directInput, IntPtr windowHandle, JoystickChoice choice)
        {
            if (choice == null)
                throw new ArgumentNullException(nameof(choice));

            _instanceGuid = choice.InstanceGuid;
            _displayName = choice.DisplayName;
            _isRacingWheel = choice.IsRacingWheel;

            if (_instanceGuid == Guid.Empty)
                return;

            _joystick = new Joystick(directInput, _instanceGuid);
            _joystick.SetCooperativeLevel(windowHandle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            _joystick.Properties.BufferSize = 128;

            foreach (var deviceObject in _joystick.GetObjects())
            {
                if ((deviceObject.ObjectId.Flags & DeviceObjectTypeFlags.Axis) != 0)
                {
                    _joystick.GetObjectPropertiesById(deviceObject.ObjectId).Range = new InputRange(-100, 100);
                }
            }

            try
            {
                _joystick.Properties.AutoCenter = false;
            }
            catch (SharpDXException)
            {
                // Some devices do not support auto-centering configuration.
            }

            _connected = true;
        }

        public bool IsAvailable => _joystick != null && _connected;

        public Guid InstanceGuid => _instanceGuid;
        public string DisplayName => _displayName;
        public bool IsRacingWheel => _isRacingWheel;

        internal Joystick? Device => _joystick;

        public bool ForceFeedbackCapable
        {
            get
            {
                if (_joystick == null)
                    return false;
                return (_joystick.Capabilities.Flags & DeviceFlags.ForceFeedback) != 0;
            }
        }

        public JoystickStateSnapshot State => _state;

        public bool Update()
        {
            if (_joystick == null)
                return false;
            try
            {
                _joystick.Acquire();
                _joystick.Poll();
                var state = _joystick.GetCurrentState();
                _state = JoystickStateSnapshot.From(state);
                _connected = true;
                return true;
            }
            catch (SharpDXException)
            {
                _connected = false;
                return false;
            }
        }

        public void LoadEffect(VibrationEffectType type, string effectPath)
        {
            if (!ForceFeedbackCapable || _joystick == null)
                return;

            if (_effects.TryGetValue(type, out var existing))
            {
                existing.Dispose();
                _effects.Remove(type);
            }

            _effects[type] = new ForceFeedbackEffect(this, effectPath);
        }

        public void PlayEffect(VibrationEffectType type, int intensity = 10000)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Play();
        }

        public void StopEffect(VibrationEffectType type)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Stop();
        }

        public void Gain(VibrationEffectType type, int value)
        {
            if (_effects.TryGetValue(type, out var effect))
                effect.Gain(value);
        }

        public void Dispose()
        {
            if (_joystick == null)
                return;
            foreach (var effect in _effects.Values)
                effect.Dispose();
            _effects.Clear();
            try
            {
                _joystick.Unacquire();
            }
            catch (SharpDXException)
            {
            }
            _joystick.Dispose();
        }

        public static List<JoystickChoice> Discover(DirectInput directInput)
        {
            var discovered = new Dictionary<Guid, JoystickChoice>();
            AddDeviceType(discovered, directInput, DeviceType.Driving, treatAsWheel: true);
            AddDeviceType(discovered, directInput, DeviceType.Joystick, treatAsWheel: false);
            AddDeviceType(discovered, directInput, DeviceType.Gamepad, treatAsWheel: false);

            var result = new List<JoystickChoice>(discovered.Values);
            result.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        private static void AddDeviceType(
            Dictionary<Guid, JoystickChoice> discovered,
            DirectInput directInput,
            DeviceType deviceType,
            bool treatAsWheel)
        {
            var devices = directInput.GetDevices(deviceType, DeviceEnumerationFlags.AttachedOnly);
            for (var i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                var instanceGuid = device.InstanceGuid;
                if (instanceGuid == Guid.Empty)
                    continue;

                var displayName = BuildDisplayName(device);
                var wheelByName = LooksLikeWheel(displayName) || LooksLikeWheel(device.ProductName);
                var isWheel = treatAsWheel || wheelByName;

                if (discovered.TryGetValue(instanceGuid, out var existing))
                {
                    if (!existing.IsRacingWheel && isWheel)
                        discovered[instanceGuid] = new JoystickChoice(existing.InstanceGuid, existing.DisplayName, true);
                    continue;
                }

                discovered.Add(instanceGuid, new JoystickChoice(instanceGuid, displayName, isWheel));
            }
        }

        private static string BuildDisplayName(DeviceInstance device)
        {
            if (!string.IsNullOrWhiteSpace(device.InstanceName))
                return device.InstanceName.Trim();
            if (!string.IsNullOrWhiteSpace(device.ProductName))
                return device.ProductName.Trim();
            return "Controller " + device.InstanceGuid.ToString("D", CultureInfo.InvariantCulture);
        }

        private static bool LooksLikeWheel(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var value = (name ?? string.Empty).ToLowerInvariant();
            return value.Contains("wheel")
                || value.Contains("steering")
                || value.Contains("pedal")
                || value.Contains("racing");
        }
    }
}
