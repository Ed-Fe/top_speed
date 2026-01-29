using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Tracks.Map;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Guidance
{
    internal readonly struct TrackApproachCue
    {
        public TrackApproachCue(
            string sectorId,
            TrackApproachSide side,
            string portalId,
            Vector2 portalPosition,
            Vector2 beaconPosition,
            float targetHeadingDegrees,
            float deltaDegrees,
            float distanceMeters,
            float? widthMeters,
            float? lengthMeters,
            float? toleranceDegrees,
            bool passed)
        {
            SectorId = sectorId;
            Side = side;
            PortalId = portalId;
            PortalPosition = portalPosition;
            BeaconPosition = beaconPosition;
            TargetHeadingDegrees = targetHeadingDegrees;
            DeltaDegrees = deltaDegrees;
            DistanceMeters = distanceMeters;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            ToleranceDegrees = toleranceDegrees;
            Passed = passed;
        }

        public string SectorId { get; }
        public TrackApproachSide Side { get; }
        public string PortalId { get; }
        public Vector2 PortalPosition { get; }
        public Vector2 BeaconPosition { get; }
        public float TargetHeadingDegrees { get; }
        public float DeltaDegrees { get; }
        public float DistanceMeters { get; }
        public float? WidthMeters { get; }
        public float? LengthMeters { get; }
        public float? ToleranceDegrees { get; }
        public bool Passed { get; }
    }

    internal sealed class TrackApproachBeacon
    {
        private readonly TrackPortalManager _portalManager;
        private readonly TrackApproachManager _approachManager;
        private readonly Dictionary<string, ShapeDefinition> _shapesById;
        private readonly float _rangeMeters;

        public TrackApproachBeacon(TrackMap map, float rangeMeters = 50f)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _portalManager = map.BuildPortalManager();
            _approachManager = new TrackApproachManager(map.Sectors, map.Approaches, _portalManager);
            _shapesById = new Dictionary<string, ShapeDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var shape in map.Shapes)
            {
                if (shape == null || string.IsNullOrWhiteSpace(shape.Id))
                    continue;
                _shapesById[shape.Id.Trim()] = shape;
            }
            _rangeMeters = Math.Max(1f, rangeMeters);
        }

        public float RangeMeters => _rangeMeters;

        public bool TryGetCue(Vector3 worldPosition, float headingDegrees, out TrackApproachCue cue)
        {
            cue = default;
            if (_approachManager.Approaches.Count == 0)
                return false;

            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var best = default(Candidate);
            var hasBest = false;

            foreach (var approach in _approachManager.Approaches)
            {
                if (approach == null)
                    continue;

                var range = GetApproachRange(approach, _rangeMeters);
                if (IsSideEnabled(approach, TrackApproachSide.Entry))
                {
                    if (TryBuildCandidate(approach, TrackApproachSide.Entry, position, range, ref best, ref hasBest))
                        continue;
                }
                if (IsSideEnabled(approach, TrackApproachSide.Exit))
                    TryBuildCandidate(approach, TrackApproachSide.Exit, position, range, ref best, ref hasBest);
            }

            if (!hasBest)
                return false;

            var delta = DeltaDegrees(headingDegrees, best.TargetHeadingDegrees);
            var forward = HeadingToVector(best.TargetHeadingDegrees);
            var toPlayer = position - best.PortalPosition;
            var passed = Vector2.Dot(forward, toPlayer) > 0f;
            var beaconPosition = best.PortalPosition;
            if (best.UseHeadingBeacon)
                beaconPosition = ResolveBeaconPosition(position, best.TargetHeadingDegrees, best.BeaconShape);

            var sectorId = best.SectorId ?? string.Empty;
            var portalId = best.PortalId ?? string.Empty;
            cue = new TrackApproachCue(
                sectorId,
                best.Side,
                portalId,
                best.PortalPosition,
                beaconPosition,
                best.TargetHeadingDegrees,
                delta,
                best.DistanceMeters,
                best.WidthMeters,
                best.LengthMeters,
                best.ToleranceDegrees,
                passed);
            return true;
        }

        private bool TryBuildCandidate(
            TrackApproachDefinition approach,
            TrackApproachSide side,
            Vector2 position,
            float rangeMeters,
            ref Candidate best,
            ref bool hasBest)
        {
            var portalId = side == TrackApproachSide.Entry ? approach.EntryPortalId : approach.ExitPortalId;
            var heading = side == TrackApproachSide.Entry ? approach.EntryHeadingDegrees : approach.ExitHeadingDegrees;
            if (!heading.HasValue || string.IsNullOrWhiteSpace(portalId))
                return false;
            if (!_portalManager.TryGetPortal(portalId!, out var portal))
                return false;

            var portalPos = new Vector2(portal.X, portal.Z);
            var portalDistance = Vector2.Distance(position, portalPos);
            var rangeDistance = portalDistance;
            var hasShape = TryGetBeaconShape(approach, out var beaconShape);
            if (hasShape)
                rangeDistance = DistanceToShape(beaconShape, position, approach.WidthMeters);
            if (rangeDistance > rangeMeters)
                return false;

            if (!hasBest || portalDistance < best.DistanceMeters)
            {
                var useHeadingBeacon = ResolveBeaconPlacement(approach);
                var candidateShape = hasShape ? beaconShape : null;
                best = new Candidate
                {
                    SectorId = approach.SectorId,
                    Side = side,
                    PortalId = portal.Id,
                    PortalPosition = portalPos,
                    TargetHeadingDegrees = heading.Value,
                    DistanceMeters = portalDistance,
                    WidthMeters = approach.WidthMeters,
                    LengthMeters = approach.LengthMeters,
                    ToleranceDegrees = approach.AlignmentToleranceDegrees,
                    UseHeadingBeacon = useHeadingBeacon,
                    BeaconShape = candidateShape
                };
                hasBest = true;
            }

            return true;
        }

        private static float GetApproachRange(TrackApproachDefinition approach, float defaultRange)
        {
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return Math.Max(0f, defaultRange);

            if (TryGetFloat(approach.Metadata, out var range, "approach_range", "beacon_range", "range"))
                return Math.Max(0f, range);

            return Math.Max(0f, defaultRange);
        }

        private static bool IsSideEnabled(TrackApproachDefinition approach, TrackApproachSide side)
        {
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return true;

            if (TryGetBool(approach.Metadata, out var enabled, "enabled") && !enabled)
                return false;
            if (TryGetBool(approach.Metadata, out var beaconEnabled, "beacon_enabled") && !beaconEnabled)
                return false;

            if (TryGetString(approach.Metadata, out var raw, "approach_side", "approach_sides", "side"))
            {
                var trimmed = raw.Trim().ToLowerInvariant();
                if (trimmed.Contains("none") || trimmed.Contains("off") || trimmed.Contains("disabled"))
                    return false;
                var hasEntry = trimmed.Contains("entry");
                var hasExit = trimmed.Contains("exit");
                if (hasEntry || hasExit)
                    return side == TrackApproachSide.Entry ? hasEntry : hasExit;
            }

            if (TryGetBool(approach.Metadata, out var entryEnabled, "approach_entry", "entry_enabled", "entry_beacon"))
            {
                if (side == TrackApproachSide.Entry)
                    return entryEnabled;
            }
            if (TryGetBool(approach.Metadata, out var exitEnabled, "approach_exit", "exit_enabled", "exit_beacon"))
            {
                if (side == TrackApproachSide.Exit)
                    return exitEnabled;
            }

            return true;
        }

        private static bool ResolveBeaconPlacement(TrackApproachDefinition approach)
        {
            var useHeadingBeacon = true;

            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return useHeadingBeacon;

            if (TryGetString(approach.Metadata, out var mode, "beacon_mode", "beacon_position", "beacon_style"))
            {
                var trimmed = mode.Trim().ToLowerInvariant();
                if (trimmed.Contains("portal") || trimmed.Contains("point"))
                    useHeadingBeacon = false;
                else if (trimmed.Contains("heading") || trimmed.Contains("direction"))
                    useHeadingBeacon = true;
            }

            return useHeadingBeacon;
        }

        private static Vector2 ResolveBeaconPosition(Vector2 position, float headingDegrees, ShapeDefinition? shape)
        {
            var forward = HeadingToVector(headingDegrees);
            if (shape == null)
                return position + forward;

            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    return ClosestPointOnRectangle(position, shape);
                case ShapeType.Circle:
                    return ClosestPointOnCircle(position, shape);
                case ShapeType.Polyline:
                    return ClosestPointOnPolyline(position, shape.Points);
                case ShapeType.Polygon:
                    return ClosestPointOnPolygon(position, shape.Points);
                default:
                    return position + forward;
            }
        }

        private static Vector2 ClosestPointOnRectangle(Vector2 position, ShapeDefinition shape)
        {
            var minX = shape.X;
            var maxX = shape.X + shape.Width;
            var minZ = shape.Z;
            var maxZ = shape.Z + shape.Height;
            var x = Math.Max(minX, Math.Min(maxX, position.X));
            var z = Math.Max(minZ, Math.Min(maxZ, position.Y));
            return new Vector2(x, z);
        }

        private static Vector2 ClosestPointOnCircle(Vector2 position, ShapeDefinition shape)
        {
            var center = new Vector2(shape.X, shape.Z);
            var toPoint = position - center;
            var length = toPoint.Length();
            if (length <= float.Epsilon)
                return center + new Vector2(shape.Radius, 0f);
            return center + (toPoint / length) * Math.Max(0f, shape.Radius);
        }

        private static Vector2 ClosestPointOnPolyline(Vector2 position, IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return position;
            if (points.Count == 1)
                return points[0];

            var best = points[0];
            var bestDist = float.MaxValue;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var candidate = ClosestPointOnSegment(points[i], points[i + 1], position);
                var dist = Vector2.DistanceSquared(candidate, position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }
            return best;
        }

        private static Vector2 ClosestPointOnPolygon(Vector2 position, IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count == 0)
                return position;
            if (IsPointInPolygon(points, position))
                return position;

            var best = points[0];
            var bestDist = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                var candidate = ClosestPointOnSegment(a, b, position);
                var dist = Vector2.DistanceSquared(candidate, position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }
            return best;
        }

        private static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var ap = p - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return a;

            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            return a + (ab * t);
        }

        private bool TryGetBeaconShape(TrackApproachDefinition approach, out ShapeDefinition shape)
        {
            shape = null!;
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return false;

            if (!TryGetString(approach.Metadata, out var shapeId, "beacon_shape", "centerline_shape", "beacon_zone", "approach_shape"))
                return false;

            return _shapesById.TryGetValue(shapeId.Trim(), out shape!);
        }

        private static float DistanceToShape(ShapeDefinition shape, Vector2 position, float? widthOverride)
        {
            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    return DistanceToRectangle(shape, position);
                case ShapeType.Circle:
                    return DistanceToCircle(shape, position);
                case ShapeType.Polyline:
                    var width = widthOverride ?? 0f;
                    return Math.Max(0f, DistanceToPolyline(shape.Points, position) - (width * 0.5f));
                case ShapeType.Polygon:
                    return DistanceToPolygon(shape.Points, position);
                default:
                    var center = new Vector2(shape.X, shape.Z);
                    return Vector2.Distance(position, center);
            }
        }

        private static float DistanceToRectangle(ShapeDefinition shape, Vector2 position)
        {
            var minX = shape.X;
            var maxX = shape.X + shape.Width;
            var minZ = shape.Z;
            var maxZ = shape.Z + shape.Height;

            var dx = 0f;
            if (position.X < minX)
                dx = minX - position.X;
            else if (position.X > maxX)
                dx = position.X - maxX;

            var dz = 0f;
            if (position.Y < minZ)
                dz = minZ - position.Y;
            else if (position.Y > maxZ)
                dz = position.Y - maxZ;

            return (float)Math.Sqrt((dx * dx) + (dz * dz));
        }

        private static float DistanceToCircle(ShapeDefinition shape, Vector2 position)
        {
            var center = new Vector2(shape.X, shape.Z);
            var distance = Vector2.Distance(center, position);
            return Math.Max(0f, distance - shape.Radius);
        }

        private static float DistanceToPolyline(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count == 0)
                return float.MaxValue;
            if (points.Count == 1)
                return Vector2.Distance(points[0], position);

            var best = float.MaxValue;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var distance = DistanceToSegment(points[i], points[i + 1], position);
                if (distance < best)
                    best = distance;
            }
            return best;
        }

        private static float DistanceToPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count == 0)
                return float.MaxValue;

            if (IsPointInPolygon(points, position))
                return 0f;

            var best = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                var distance = DistanceToSegment(a, b, position);
                if (distance < best)
                    best = distance;
            }
            return best;
        }

        private static bool IsPointInPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            var inside = false;
            var j = points.Count - 1;
            for (var i = 0; i < points.Count; i++)
            {
                var pi = points[i];
                var pj = points[j];
                var intersect = ((pi.Y > position.Y) != (pj.Y > position.Y)) &&
                                (position.X < (pj.X - pi.X) * (position.Y - pi.Y) / (pj.Y - pi.Y + float.Epsilon) + pi.X);
                if (intersect)
                    inside = !inside;
                j = i;
            }
            return inside;
        }

        private static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 position)
        {
            var ab = b - a;
            var ap = position - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return Vector2.Distance(a, position);
            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t <= 0f)
                return Vector2.Distance(a, position);
            if (t >= 1f)
                return Vector2.Distance(b, position);
            var closest = a + (ab * t);
            return Vector2.Distance(closest, position);
        }

        private static bool TryGetFloat(
            IReadOnlyDictionary<string, string> metadata,
            out float value,
            params string[] keys)
        {
            value = 0f;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }

        private static bool TryGetBool(
            IReadOnlyDictionary<string, string> metadata,
            out bool value,
            params string[] keys)
        {
            value = false;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (TryParseBool(raw, out value))
                    return true;
            }
            return false;
        }

        private static bool TryGetString(
            IReadOnlyDictionary<string, string> metadata,
            out string value,
            params string[] keys)
        {
            value = string.Empty;
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (metadata.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    value = raw;
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var trimmed = raw.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
            }
            return bool.TryParse(raw, out value);
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }

        private static float DeltaDegrees(float current, float target)
        {
            var diff = Math.Abs(NormalizeDegrees(current - target));
            return diff > 180f ? 360f - diff : diff;
        }

        private static Vector2 HeadingToVector(float headingDegrees)
        {
            var radians = headingDegrees * (float)Math.PI / 180f;
            return new Vector2((float)Math.Sin(radians), (float)Math.Cos(radians));
        }

        private struct Candidate
        {
            public string? SectorId;
            public TrackApproachSide Side;
            public string? PortalId;
            public Vector2 PortalPosition;
            public float TargetHeadingDegrees;
            public float DistanceMeters;
            public float? WidthMeters;
            public float? LengthMeters;
            public float? ToleranceDegrees;
            public bool UseHeadingBeacon;
            public ShapeDefinition? BeaconShape;
        }
    }
}
