using System;
using System.Collections.Generic;
using System.Numerics;
using TopSpeed.Tracks.Geometry;

namespace TopSpeed.Tracks.Walls
{
    public sealed class TrackWallManager
    {
        private readonly Dictionary<string, GeometryDefinition> _geometries;
        private readonly Dictionary<string, IReadOnlyList<Vector2>> _geometryPoints2D;
        private readonly List<TrackWallDefinition> _walls;

        public TrackWallManager(IEnumerable<GeometryDefinition> geometries, IEnumerable<TrackWallDefinition> walls)
        {
            _geometries = new Dictionary<string, GeometryDefinition>(StringComparer.OrdinalIgnoreCase);
            _geometryPoints2D = new Dictionary<string, IReadOnlyList<Vector2>>(StringComparer.OrdinalIgnoreCase);
            _walls = new List<TrackWallDefinition>();

            if (geometries != null)
            {
                foreach (var geometry in geometries)
                {
                    if (geometry == null)
                        continue;
                    _geometries[geometry.Id] = geometry;
                    _geometryPoints2D[geometry.Id] = ProjectToXZ(geometry.Points);
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
        public bool TryGetGeometry(string id, out GeometryDefinition geometry)
        {
            geometry = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _geometries.TryGetValue(id.Trim(), out geometry!);
        }

        public bool TryGetGeometryPoints2D(string id, out IReadOnlyList<Vector2> points)
        {
            points = Array.Empty<Vector2>();
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (_geometryPoints2D.TryGetValue(id.Trim(), out var cached))
            {
                points = cached;
                return true;
            }

            if (!_geometries.TryGetValue(id.Trim(), out var geometry))
                return false;

            var projected = ProjectToXZ(geometry.Points);
            _geometryPoints2D[id.Trim()] = projected;
            points = projected;
            return true;
        }

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
            if (!_geometries.TryGetValue(wall.GeometryId, out var geometry))
                return false;
            var width = wall.WidthMeters;
            return Contains(geometry, position, width);
        }

        private bool Contains(GeometryDefinition geometry, Vector2 position, float widthMeters)
        {
            if (geometry == null)
                return false;
            if (!_geometryPoints2D.TryGetValue(geometry.Id, out var points2D))
                points2D = ProjectToXZ(geometry.Points);

            switch (geometry.Type)
            {
                case GeometryType.Polygon:
                    return ContainsPolygonPath(points2D, position, widthMeters);
                case GeometryType.Polyline:
                case GeometryType.Spline:
                    return ContainsPolylinePath(points2D, position, widthMeters);
                case GeometryType.Mesh:
                case GeometryType.Undefined:
                default:
                    return false;
            }
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

        private static IReadOnlyList<Vector2> ProjectToXZ(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return Array.Empty<Vector2>();

            var projected = new List<Vector2>(points.Count);
            foreach (var point in points)
                projected.Add(new Vector2(point.X, point.Z));
            return projected;
        }
    }
}
