using System;

namespace TopSpeed.Bots
{
    public static class BotRaceRules
    {
        public const float DefaultLaneHalfWidth = 5.0f;
        public const float AdventureLaneHalfWidth = 80.0f;
        public const float StartLineY = 140.0f;
        public const float StartGridMargin = 0.3f;
        public const float MinStartRowSpacing = 10.0f;
        public const float FullCrashMinSpeedKph = 50.0f;
        public const float DefaultBotEngineStartSeconds = 1.35f;
        public const float DefaultBotCrashRecoverySeconds = 2.5f;

        public static float GetLaneHalfWidthForTrack(string? trackName)
        {
            if (!string.IsNullOrWhiteSpace(trackName)
                && trackName!.IndexOf("adv", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return AdventureLaneHalfWidth;
            }

            return DefaultLaneHalfWidth;
        }

        public static float CalculateStartRowSpacing(float maxVehicleLength)
        {
            return Math.Max(MinStartRowSpacing, maxVehicleLength * 1.5f);
        }

        public static float CalculateStartX(int gridIndex, float vehicleWidth, float laneHalfWidth)
        {
            var halfWidth = Math.Max(0.1f, vehicleWidth * 0.5f);
            var laneOffset = laneHalfWidth - halfWidth - StartGridMargin;
            if (laneOffset < 0f)
                laneOffset = 0f;
            return gridIndex % 2 == 1 ? laneOffset : -laneOffset;
        }

        public static float CalculateStartY(int gridIndex, float rowSpacing)
        {
            var row = gridIndex / 2;
            return StartLineY - (row * rowSpacing);
        }

        public static float CalculateRelativeLanePosition(float positionX, float roadLeft, float laneHalfWidth)
        {
            if (laneHalfWidth <= 0f)
                return 0.5f;

            var laneWidth = laneHalfWidth * 2.0f;
            if (laneWidth <= 0f)
                return 0.5f;

            return (positionX - roadLeft) / laneWidth;
        }

        public static bool IsOutsideRoad(float relativeLanePosition)
        {
            return relativeLanePosition < 0f || relativeLanePosition > 1f;
        }

        public static bool IsFullCrash(int gear, float speedKph)
        {
            return gear > 1 || speedKph >= FullCrashMinSpeedKph;
        }

        public static float RoadCenter(float left, float right)
        {
            return (left + right) * 0.5f;
        }
    }
}
