using System;
using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public readonly struct BotRoad
    {
        public BotRoad(float left, float right, TrackSurface surface, TrackType type, float length)
        {
            Left = left;
            Right = right;
            Surface = surface;
            Type = type;
            Length = length;
        }

        public float Left { get; }
        public float Right { get; }
        public TrackSurface Surface { get; }
        public TrackType Type { get; }
        public float Length { get; }
    }

    public static class BotRoadModel
    {
        private const float LegacyLaneWidthMeters = 50.0f;

        public static float GetLapDistance(TrackDefinition[] definitions)
        {
            if (definitions == null || definitions.Length == 0)
                return 0f;

            var distance = 0f;
            for (var i = 0; i < definitions.Length; i++)
                distance += Math.Max(1f, definitions[i].Length);
            return distance;
        }

        public static BotRoad RoadAtPosition(TrackDefinition[] definitions, float position, float laneHalfWidth)
        {
            if (definitions == null || definitions.Length == 0)
                return new BotRoad(-laneHalfWidth, laneHalfWidth, TrackSurface.Asphalt, TrackType.Straight, 50f);

            var lapDistance = GetLapDistance(definitions);
            if (lapDistance <= 0f)
                return new BotRoad(-laneHalfWidth, laneHalfWidth, TrackSurface.Asphalt, TrackType.Straight, 50f);

            var curveScale = LegacyLaneWidthMeters > 0f ? laneHalfWidth / LegacyLaneWidthMeters : 1.0f;
            if (curveScale <= 0f)
                curveScale = 0.01f;

            var lapCenter = ComputeLapCenter(definitions, curveScale);
            var lap = (int)Math.Floor(position / lapDistance);
            var pos = WrapPosition(position, lapDistance);
            var dist = 0.0f;
            var center = lap * lapCenter;

            for (var i = 0; i < definitions.Length; i++)
            {
                var segmentLength = Math.Max(1f, definitions[i].Length);
                if (dist <= pos && dist + segmentLength > pos)
                {
                    var relPos = pos - dist;
                    return ApplyRoadOffset(center, relPos, definitions[i], laneHalfWidth, curveScale);
                }

                center = UpdateCenter(center, definitions[i], curveScale);
                dist += segmentLength;
            }

            return new BotRoad(-laneHalfWidth, laneHalfWidth, TrackSurface.Asphalt, TrackType.Straight, 50f);
        }

        private static float ComputeLapCenter(TrackDefinition[] definitions, float curveScale)
        {
            var center = 0f;
            for (var i = 0; i < definitions.Length; i++)
                center = UpdateCenter(center, definitions[i], curveScale);
            return center;
        }

        private static float WrapPosition(float position, float lapDistance)
        {
            if (lapDistance <= 0f)
                return position;
            var wrapped = position % lapDistance;
            if (wrapped < 0f)
                wrapped += lapDistance;
            return wrapped;
        }

        private static float UpdateCenter(float center, TrackDefinition definition, float curveScale)
        {
            switch (definition.Type)
            {
                case TrackType.EasyLeft:
                    return center - (definition.Length * curveScale) / 2f;
                case TrackType.Left:
                    return center - (definition.Length * curveScale) * 2f / 3f;
                case TrackType.HardLeft:
                    return center - definition.Length * curveScale;
                case TrackType.HairpinLeft:
                    return center - (definition.Length * curveScale) * 3f / 2f;
                case TrackType.EasyRight:
                    return center + (definition.Length * curveScale) / 2f;
                case TrackType.Right:
                    return center + (definition.Length * curveScale) * 2f / 3f;
                case TrackType.HardRight:
                    return center + definition.Length * curveScale;
                case TrackType.HairpinRight:
                    return center + (definition.Length * curveScale) * 3f / 2f;
                default:
                    return center;
            }
        }

        private static BotRoad ApplyRoadOffset(
            float center,
            float relPos,
            TrackDefinition definition,
            float laneHalfWidth,
            float curveScale)
        {
            var offset = relPos * curveScale;
            float left;
            float right;

            switch (definition.Type)
            {
                case TrackType.Straight:
                    left = center - laneHalfWidth;
                    right = center + laneHalfWidth;
                    break;
                case TrackType.EasyLeft:
                    left = center - laneHalfWidth - offset / 2f;
                    right = center + laneHalfWidth - offset / 2f;
                    break;
                case TrackType.Left:
                    left = center - laneHalfWidth - offset * 2f / 3f;
                    right = center + laneHalfWidth - offset * 2f / 3f;
                    break;
                case TrackType.HardLeft:
                    left = center - laneHalfWidth - offset;
                    right = center + laneHalfWidth - offset;
                    break;
                case TrackType.HairpinLeft:
                    left = center - laneHalfWidth - offset * 3f / 2f;
                    right = center + laneHalfWidth - offset * 3f / 2f;
                    break;
                case TrackType.EasyRight:
                    left = center - laneHalfWidth + offset / 2f;
                    right = center + laneHalfWidth + offset / 2f;
                    break;
                case TrackType.Right:
                    left = center - laneHalfWidth + offset * 2f / 3f;
                    right = center + laneHalfWidth + offset * 2f / 3f;
                    break;
                case TrackType.HardRight:
                    left = center - laneHalfWidth + offset;
                    right = center + laneHalfWidth + offset;
                    break;
                case TrackType.HairpinRight:
                    left = center - laneHalfWidth + offset * 3f / 2f;
                    right = center + laneHalfWidth + offset * 3f / 2f;
                    break;
                default:
                    left = center - laneHalfWidth;
                    right = center + laneHalfWidth;
                    break;
            }

            return new BotRoad(left, right, definition.Surface, definition.Type, definition.Length);
        }
    }
}
