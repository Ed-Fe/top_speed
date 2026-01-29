using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Walls
{
    public sealed class TrackWallManager
    {
        private readonly Dictionary<string, ShapeDefinition> _shapes;
        private readonly List<TrackWallDefinition> _walls;

        public TrackWallManager(IEnumerable<ShapeDefinition> shapes, IEnumerable<TrackWallDefinition> walls)
        {
            _shapes = new Dictionary<string, ShapeDefinition>(StringComparer.OrdinalIgnoreCase);
            _walls = new List<TrackWallDefinition>();

            if (shapes != null)
            {
                foreach (var shape in shapes)
                {
                    if (shape == null)
                        continue;
                    _shapes[shape.Id] = shape;
                }
            }

            if (walls != null)
            {
                foreach (var wall in walls)
                {
                    if (wall == null)
                        continue;
                    _walls.Add(wall);
                }
            }
        }

        public bool HasWalls => _walls.Count > 0;
        public IReadOnlyList<TrackWallDefinition> Walls => _walls;

        public bool TryGetWall(string id, out TrackWallDefinition wall)
        {
            wall = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            foreach (var candidate in _walls)
            {
                if (candidate != null && string.Equals(candidate.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    wall = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool ContainsAny(Vector2 position)
        {
            if (_walls.Count == 0)
                return false;
            foreach (var wall in _walls)
            {
                if (Contains(wall, position))
                    return true;
            }
            return false;
        }

        public bool TryFindCollision(Vector2 from, Vector2 to, out TrackWallDefinition wall)
        {
            wall = null!;
            if (_walls.Count == 0)
                return false;

            if (TryFindCollisionAtPoint(from, out wall) || TryFindCollisionAtPoint(to, out wall))
                return true;

            var delta = to - from;
            var distance = delta.Length();
            if (distance <= 0.001f)
                return false;

            var steps = Math.Max(1, (int)Math.Ceiling(distance / 1.0f));
            var step = delta / steps;
            var position = from;
            for (var i = 0; i <= steps; i++)
            {
                if (TryFindCollisionAtPoint(position, out wall))
                    return true;
                position += step;
            }

            return false;
        }

        private bool TryFindCollisionAtPoint(Vector2 position, out TrackWallDefinition wall)
        {
            wall = null!;
            foreach (var candidate in _walls)
            {
                if (Contains(candidate, position))
                {
                    wall = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool Contains(TrackWallDefinition wall, Vector2 position)
        {
            if (wall == null)
                return false;
            if (!_shapes.TryGetValue(wall.ShapeId, out var shape))
                return false;
            var width = wall.WidthMeters;
            return Contains(shape, position, width);
        }

        private static bool Contains(ShapeDefinition shape, Vector2 position, float widthMeters)
        {
            if (shape == null)
                return false;
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
                    return ContainsPolygonPath(shape.Points, position, widthMeters);
                case ShapeType.Polyline:
                    return ContainsPolylinePath(shape.Points, position, widthMeters);
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

        private static bool ContainsPolygonPath(IReadOnlyList<Vector2> points, Vector2 position, float widthMeters)
        {
            if (points == null || points.Count < 3)
                return false;

            var width = Math.Abs(widthMeters);
            if (width <= 0f)
                return ContainsPolygon(points, position);

            if (!ContainsPolygon(points, position))
                return false;
            return DistanceToPolylineSquared(points, position, true) <= (width * width);
        }

        private static bool ContainsPolylinePath(IReadOnlyList<Vector2> points, Vector2 position, float widthMeters)
        {
            if (points == null || points.Count < 2)
                return false;

            var width = Math.Abs(widthMeters);
            if (width <= 0f)
                return false;

            var radius = width * 0.5f;
            return DistanceToPolylineSquared(points, position, false) <= (radius * radius);
        }

        private static float DistanceToPolylineSquared(IReadOnlyList<Vector2> points, Vector2 position, bool closed)
        {
            if (points == null || points.Count < 2)
                return float.MaxValue;

            var segmentCount = points.Count - 1;
            var lastIndex = points.Count - 1;
            var lastEqualsFirst = Vector2.DistanceSquared(points[0], points[lastIndex]) <= 0.0001f;

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

        private static float DistanceToSegmentSquared(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var ap = p - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return Vector2.Dot(ap, ap);

            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t <= 0f)
                return Vector2.Dot(ap, ap);
            if (t >= 1f)
                return Vector2.DistanceSquared(p, b);
            var projection = a + ab * t;
            return Vector2.DistanceSquared(p, projection);
        }
    }
}
