using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Areas
{
    public sealed class TrackAreaManager
    {
        private readonly Dictionary<string, ShapeDefinition> _shapes;
        private readonly Dictionary<string, TrackAreaDefinition> _areasById;
        private readonly List<TrackAreaDefinition> _areas;

        public TrackAreaManager(IEnumerable<ShapeDefinition> shapes, IEnumerable<TrackAreaDefinition> areas)
        {
            _shapes = new Dictionary<string, ShapeDefinition>(StringComparer.OrdinalIgnoreCase);
            _areasById = new Dictionary<string, TrackAreaDefinition>(StringComparer.OrdinalIgnoreCase);
            _areas = new List<TrackAreaDefinition>();

            if (shapes != null)
            {
                foreach (var shape in shapes)
                    AddShape(shape);
            }

            if (areas != null)
            {
                foreach (var area in areas)
                    AddArea(area);
            }
        }

        public IReadOnlyList<TrackAreaDefinition> Areas => _areas;

        public void AddShape(ShapeDefinition shape)
        {
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            _shapes[shape.Id] = shape;
        }

        public void AddArea(TrackAreaDefinition area)
        {
            if (area == null)
                throw new ArgumentNullException(nameof(area));
            _areasById[area.Id] = area;
            _areas.Add(area);
        }

        public bool TryGetShape(string id, out ShapeDefinition shape)
        {
            shape = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _shapes.TryGetValue(id.Trim(), out shape!);
        }

        public bool TryGetArea(string id, out TrackAreaDefinition area)
        {
            area = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _areasById.TryGetValue(id.Trim(), out area!);
        }

        public IReadOnlyList<TrackAreaDefinition> FindAreasContaining(Vector2 position)
        {
            if (_areas.Count == 0)
                return Array.Empty<TrackAreaDefinition>();

            var hits = new List<TrackAreaDefinition>();
            foreach (var area in _areas)
            {
                if (Contains(area, position))
                    hits.Add(area);
            }
            return hits;
        }

        public bool Contains(TrackAreaDefinition area, Vector2 position)
        {
            if (area == null)
                return false;
            if (!TryGetShape(area.ShapeId, out var shape))
                return false;
            var width = area.WidthMeters.GetValueOrDefault();
            var centered = IsCenteredClosedWidth(area.Metadata);
            return Contains(shape, position, width, centered);
        }

        public bool ContainsShape(string shapeId, Vector2 position, float? widthMeters = null)
        {
            if (!TryGetShape(shapeId, out var shape))
                return false;
            var width = widthMeters.GetValueOrDefault();
            return Contains(shape, position, width, false);
        }

        public bool ContainsTrackArea(Vector2 position)
        {
            if (_areas.Count == 0)
                return false;

            foreach (var area in _areas)
            {
                if (area == null)
                    continue;
                if (area.Type == TrackAreaType.Boundary || area.Type == TrackAreaType.OffTrack)
                    continue;
                if (area.Type == TrackAreaType.Start || area.Type == TrackAreaType.Finish ||
                    area.Type == TrackAreaType.Checkpoint || area.Type == TrackAreaType.Intersection)
                    continue;
                if (Contains(area, position))
                    return true;
            }

            return false;
        }

        private static bool Contains(ShapeDefinition shape, Vector2 position, float widthMeters, bool closedCentered)
        {
            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    return widthMeters > 0f
                        ? ContainsRectanglePath(shape, position, widthMeters)
                        : ContainsRectangle(shape, position);
                case ShapeType.Circle:
                    return widthMeters > 0f
                        ? ContainsCirclePath(shape, position, widthMeters)
                        : ContainsCircle(shape, position);
                case ShapeType.Ring:
                    return widthMeters > 0f
                        ? ContainsRingPath(shape, position, widthMeters)
                        : ContainsRing(shape, position);
                case ShapeType.Polygon:
                    return ContainsPolygonPath(shape.Points, position, widthMeters, closedCentered);
                case ShapeType.Polyline:
                    return ContainsPolylinePath(shape.Points, position, widthMeters, closedCentered);
                default:
                    return false;
            }
        }

        private static bool ContainsRectangle(ShapeDefinition shape, Vector2 position)
        {
            var minX = shape.X;
            var minZ = shape.Z;
            var maxX = shape.X + shape.Width;
            var maxZ = shape.Z + shape.Height;
            return position.X >= minX && position.X <= maxX &&
                   position.Y >= minZ && position.Y <= maxZ;
        }

        private static bool ContainsCircle(ShapeDefinition shape, Vector2 position)
        {
            var dx = position.X - shape.X;
            var dz = position.Y - shape.Z;
            return (dx * dx + dz * dz) <= (shape.Radius * shape.Radius);
        }

        private static bool ContainsRectanglePath(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            if (widthMeters <= 0f)
                return false;

            var minX = Math.Min(shape.X, shape.X + shape.Width);
            var maxX = Math.Max(shape.X, shape.X + shape.Width);
            var minZ = Math.Min(shape.Z, shape.Z + shape.Height);
            var maxZ = Math.Max(shape.Z, shape.Z + shape.Height);
            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;
            var lengthX = Math.Abs(shape.Width);
            var lengthZ = Math.Abs(shape.Height);
            var halfWidth = widthMeters * 0.5f;
            if (lengthX >= lengthZ)
            {
                if (position.X < minX || position.X > maxX)
                    return false;
                return Math.Abs(position.Y - centerZ) <= halfWidth;
            }

            if (position.Y < minZ || position.Y > maxZ)
                return false;
            return Math.Abs(position.X - centerX) <= halfWidth;
        }

        private static bool ContainsCirclePath(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            var radius = Math.Abs(shape.Radius);
            if (radius <= 0f || widthMeters <= 0f)
                return false;

            var dist = Vector2.Distance(new Vector2(shape.X, shape.Z), position);
            var inner = Math.Max(0f, radius - widthMeters);
            return dist >= inner && dist <= radius;
        }

        private static bool ContainsRing(ShapeDefinition shape, Vector2 position)
        {
            var ringWidth = Math.Abs(shape.RingWidth);
            if (ringWidth <= 0f)
                return false;

            if (shape.Radius > 0f)
                return ContainsRingCircle(shape, position, ringWidth);

            return ContainsRingRectangle(shape, position, ringWidth);
        }

        private static bool ContainsRingPath(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            var ringWidth = Math.Abs(widthMeters);
            if (ringWidth <= 0f)
                return false;

            if (shape.Radius > 0f)
                return ContainsRingCircle(shape, position, ringWidth);

            return ContainsRingRectangle(shape, position, ringWidth);
        }

        private static bool ContainsRingCircle(ShapeDefinition shape, Vector2 position, float ringWidth)
        {
            var dx = position.X - shape.X;
            var dz = position.Y - shape.Z;
            var distSq = dx * dx + dz * dz;
            var inner = Math.Abs(shape.Radius);
            var outer = inner + ringWidth;
            return distSq >= (inner * inner) && distSq <= (outer * outer);
        }

        private static bool ContainsRingRectangle(ShapeDefinition shape, Vector2 position, float ringWidth)
        {
            var innerMinX = shape.X;
            var innerMinZ = shape.Z;
            var innerMaxX = shape.X + shape.Width;
            var innerMaxZ = shape.Z + shape.Height;
            if (innerMaxX <= innerMinX || innerMaxZ <= innerMinZ)
                return false;

            var outerMinX = innerMinX - ringWidth;
            var outerMinZ = innerMinZ - ringWidth;
            var outerMaxX = innerMaxX + ringWidth;
            var outerMaxZ = innerMaxZ + ringWidth;

            var insideOuter = position.X >= outerMinX && position.X <= outerMaxX &&
                              position.Y >= outerMinZ && position.Y <= outerMaxZ;
            if (!insideOuter)
                return false;

            var insideInner = position.X >= innerMinX && position.X <= innerMaxX &&
                              position.Y >= innerMinZ && position.Y <= innerMaxZ;
            return !insideInner;
        }

        private static bool ContainsPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count < 3)
                return false;

            var inside = false;
            var j = points.Count - 1;
            for (var i = 0; i < points.Count; i++)
            {
                var xi = points[i].X;
                var zi = points[i].Y;
                var xj = points[j].X;
                var zj = points[j].Y;

                var intersect = ((zi > position.Y) != (zj > position.Y)) &&
                                (position.X < (xj - xi) * (position.Y - zi) / (zj - zi + float.Epsilon) + xi);
                if (intersect)
                    inside = !inside;
                j = i;
            }

            return inside;
        }

        private static bool ContainsPolygonPath(
            IReadOnlyList<Vector2> points,
            Vector2 position,
            float widthMeters,
            bool closedCentered)
        {
            if (points == null || points.Count < 3)
                return false;

            var width = Math.Abs(widthMeters);
            if (width <= 0f)
                return ContainsPolygon(points, position);

            if (closedCentered)
            {
                var radius = width * 0.5f;
                return DistanceToPolylineSquared(points, position, true) <= (radius * radius);
            }

            if (!ContainsPolygon(points, position))
                return false;
            return DistanceToPolylineSquared(points, position, true) <= (width * width);
        }

        private static bool ContainsPolylinePath(
            IReadOnlyList<Vector2> points,
            Vector2 position,
            float widthMeters,
            bool closedCentered)
        {
            if (points == null || points.Count < 2)
                return false;

            var width = Math.Abs(widthMeters);
            if (width <= 0f)
                return false;

            var closed = IsClosedPolyline(points);
            if (!closed)
            {
                var radius = width * 0.5f;
                return DistanceToPolylineSquared(points, position, false) <= (radius * radius);
            }

            if (closedCentered)
            {
                var radius = width * 0.5f;
                return DistanceToPolylineSquared(points, position, true) <= (radius * radius);
            }

            if (!ContainsPolygon(points, position))
                return false;
            return DistanceToPolylineSquared(points, position, true) <= (width * width);
        }

        private static float DistanceToSegmentSquared(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var ap = p - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return Vector2.Dot(ap, ap);

            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            var closest = a + ab * t;
            var delta = p - closest;
            return Vector2.Dot(delta, delta);
        }

        private static float DistanceToPolylineSquared(IReadOnlyList<Vector2> points, Vector2 position, bool closed)
        {
            if (points == null || points.Count < 2)
                return float.MaxValue;

            var count = points.Count;
            var lastIndex = count - 1;
            var lastEqualsFirst = count > 1 && Vector2.DistanceSquared(points[0], points[lastIndex]) <= 0.0001f;
            var segmentCount = lastEqualsFirst ? lastIndex : count - 1;

            var best = float.MaxValue;
            for (var i = 0; i < segmentCount; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var dist = DistanceToSegmentSquared(a, b, position);
                if (dist < best)
                    best = dist;
            }

            if (closed && !lastEqualsFirst)
            {
                var dist = DistanceToSegmentSquared(points[lastIndex], points[0], position);
                if (dist < best)
                    best = dist;
            }

            return best;
        }

        private static bool IsClosedPolyline(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return false;
            return Vector2.DistanceSquared(points[0], points[points.Count - 1]) <= 0.0001f;
        }

        private static bool IsCenteredClosedWidth(IReadOnlyDictionary<string, string> metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return false;

            if (!TryGetMetadataValue(metadata, out var mode, "width_mode", "path_width_mode", "width_align", "width_alignment"))
                return false;

            var trimmed = mode.Trim().ToLowerInvariant();
            return trimmed.Contains("center") ||
                   trimmed.Contains("centre") ||
                   trimmed.Contains("both") ||
                   trimmed.Contains("sym");
        }

        private static bool TryGetMetadataValue(
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
    }
}
