using System;

namespace TopSpeed.Input
{
    internal sealed class JoystickChoice
    {
        public JoystickChoice(Guid instanceGuid, string displayName, bool isRacingWheel)
        {
            InstanceGuid = instanceGuid;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? instanceGuid.ToString() : displayName;
            IsRacingWheel = isRacingWheel;
        }

        public Guid InstanceGuid { get; }
        public string DisplayName { get; }
        public bool IsRacingWheel { get; }
    }
}
