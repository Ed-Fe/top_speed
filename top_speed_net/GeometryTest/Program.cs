using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Map;
using TopSpeed.Tracks.Topology;
using TopSpeed.Tracks.Walls;

namespace TopSpeed.GeometryTest
{
    internal static class Program
    {
        private const string ExploreArg = "explore";
        private const string AutoArg = "auto";
        private const string ValidateArg = "validate";

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && IsHelpArg(args[0]))
                {
                    PrintUsage();
                    return 0;
                }

                if (args.Length > 0 && IsExploreArg(args[0]))
                    return AutoExplore(args.Skip(1).ToArray());

                if (args.Length > 0 && IsValidateArg(args[0]))
                    return ValidatePaths(args.Skip(1).ToArray());

                return ValidatePaths(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Map validation failed with exception:");
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static int ValidatePaths(string[] args)
        {
            var files = ResolveTrackFiles(args.Length > 0 ? args[0] : null);
            if (files.Count == 0)
                return 1;

            var allOk = true;
            foreach (var file in files)
            {
                if (!ValidateFile(file))
                    allOk = false;
            }

            return allOk ? 0 : 1;
        }

        private static int AutoExplore(string[] args)
        {
            var files = ResolveTrackFiles(args.Length > 0 ? args[0] : null);
            if (files.Count == 0)
                return 1;

            var allOk = true;
            foreach (var file in files)
            {
                if (!ValidateAndExploreFile(file))
                    allOk = false;
            }

            return allOk ? 0 : 1;
        }

        private static bool ValidateFile(string path)
        {
            Console.WriteLine($"Validating: {path}");
            var validation = TrackMapValidator.ValidateFile(path, new TrackMapValidationOptions
            {
                RequireSafeZones = false,
                RequireIntersections = false,
                TreatUnreachableCellsAsErrors = false
            });

            if (validation.Issues.Count == 0)
            {
                Console.WriteLine("  OK");
                Console.WriteLine();
                return true;
            }

            foreach (var issue in validation.Issues)
                Console.WriteLine($"  {issue}");

            Console.WriteLine();
            return validation.IsValid;
        }

        private static bool ValidateAndExploreFile(string path)
        {
            Console.WriteLine($"Auto-explore: {path}");
            var validation = TrackMapValidator.ValidateFile(path, new TrackMapValidationOptions
            {
                RequireSafeZones = false,
                RequireIntersections = false,
                TreatUnreachableCellsAsErrors = false
            });

            var exploreResult = ExploreFile(path, validation, out var logPath);
            Console.WriteLine($"  Log: {logPath}");
            Console.WriteLine();
            return exploreResult && validation.IsValid;
        }

        private static bool ExploreFile(string path, TrackMapValidationResult validation, out string logPath)
        {
            var lines = new List<string>
            {
                $"Track: {path}",
                $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                string.Empty,
                "Validation:"
            };

            if (validation.Issues.Count == 0)
            {
                lines.Add("  OK");
            }
            else
            {
                foreach (var issue in validation.Issues)
                    lines.Add($"  {issue}");
            }

            lines.Add(string.Empty);
            lines.Add("Auto-explore:");

            if (!TrackMapFormat.TryParse(path, out var map, out var parseIssues) || map == null)
            {
                lines.Add("  FAILED: unable to parse map.");
                foreach (var issue in parseIssues)
                    lines.Add($"  {issue}");
                return WriteLog(path, lines, out logPath) && false;
            }

            var areaManager = new TrackAreaManager(map.Shapes, map.Areas);
            var wallManager = new TrackWallManager(map.Shapes, map.Walls);

            if (!TryGetDrivableBounds(map, out var minX, out var minZ, out var maxX, out var maxZ))
            {
                lines.Add("  FAILED: no drivable areas found.");
                return WriteLog(path, lines, out logPath) && false;
            }

            var step = map.Metadata.CellSizeMeters;
            if (step <= 0.01f)
                step = 1f;

            var countX = Math.Max(1, (int)Math.Floor((maxX - minX) / step) + 1);
            var countZ = Math.Max(1, (int)Math.Floor((maxZ - minZ) / step) + 1);
            var total = countX * countZ;
            var drivable = new bool[total];
            var visited = new bool[total];

            var drivableCount = 0;
            for (var iz = 0; iz < countZ; iz++)
            {
                var z = minZ + (iz * step);
                for (var ix = 0; ix < countX; ix++)
                {
                    var x = minX + (ix * step);
                    var position = new Vector2(x, z);
                    if (!areaManager.ContainsTrackArea(position))
                        continue;
                    if (IsBlockedByWall(wallManager, position))
                        continue;
                    drivable[ix + (iz * countX)] = true;
                    drivableCount++;
                }
            }

            var start = new Vector2(map.Metadata.StartX, map.Metadata.StartZ);
            var startIndex = FindClosestDrivableIndex(start, minX, minZ, step, countX, countZ, drivable, out var startOnTrack, out var startDistance);
            if (startIndex < 0)
            {
                lines.Add("  FAILED: no reachable drivable samples.");
                return WriteLog(path, lines, out logPath) && false;
            }

            var reachableCount = 0;
            var queue = new Queue<int>();
            visited[startIndex] = true;
            queue.Enqueue(startIndex);
            reachableCount++;

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var ix = index % countX;
                var iz = index / countX;
                var position = new Vector2(minX + (ix * step), minZ + (iz * step));

                TryVisitNeighbor(ix + 1, iz);
                TryVisitNeighbor(ix - 1, iz);
                TryVisitNeighbor(ix, iz + 1);
                TryVisitNeighbor(ix, iz - 1);

                void TryVisitNeighbor(int nx, int nz)
                {
                    if (nx < 0 || nz < 0 || nx >= countX || nz >= countZ)
                        return;
                    var nIndex = nx + (nz * countX);
                    if (!drivable[nIndex] || visited[nIndex])
                        return;
                    var neighbor = new Vector2(minX + (nx * step), minZ + (nz * step));
                    if (IsBlockedByWall(wallManager, position, neighbor))
                        return;
                    visited[nIndex] = true;
                    reachableCount++;
                    queue.Enqueue(nIndex);
                }
            }

            var unreachable = drivableCount - reachableCount;
            lines.Add($"  Bounds: X {minX:0.##}..{maxX:0.##}, Z {minZ:0.##}..{maxZ:0.##}");
            lines.Add($"  Step: {step:0.##}m, Samples: {total}, Drivable: {drivableCount}, Reachable: {reachableCount}, Unreachable: {unreachable}");
            if (!startOnTrack)
                lines.Add($"  Start outside track: nearest drivable at {startDistance:0.##}m from start.");

            if (unreachable > 0)
            {
                lines.Add("  Unreachable samples (first 25):");
                var reported = 0;
                for (var iz = 0; iz < countZ && reported < 25; iz++)
                {
                    var z = minZ + (iz * step);
                    for (var ix = 0; ix < countX && reported < 25; ix++)
                    {
                        var idx = ix + (iz * countX);
                        if (drivable[idx] && !visited[idx])
                        {
                            var x = minX + (ix * step);
                            lines.Add($"    x={x:0.##}, z={z:0.##}");
                            reported++;
                        }
                    }
                }
            }

            return WriteLog(path, lines, out logPath) && unreachable == 0 && startIndex >= 0;
        }

        private static bool TryGetDrivableBounds(TrackMapDefinition map, out float minX, out float minZ, out float maxX, out float maxZ)
        {
            minX = minZ = maxX = maxZ = 0f;
            if (map.Areas.Count == 0 || map.Shapes.Count == 0)
                return false;

            var shapes = new Dictionary<string, ShapeDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var shape in map.Shapes)
            {
                if (shape == null || string.IsNullOrWhiteSpace(shape.Id))
                    continue;
                shapes[shape.Id] = shape;
            }

            var hasBounds = false;
            foreach (var area in map.Areas)
            {
                if (area == null || !IsDrivableArea(area))
                    continue;
                if (!shapes.TryGetValue(area.ShapeId, out var shape))
                    continue;
                var width = area.WidthMeters.GetValueOrDefault();
                if (!TryGetShapeBoundsExpanded(shape, width, out var sMinX, out var sMinZ, out var sMaxX, out var sMaxZ))
                    continue;
                if (!hasBounds)
                {
                    minX = sMinX;
                    minZ = sMinZ;
                    maxX = sMaxX;
                    maxZ = sMaxZ;
                    hasBounds = true;
                }
                else
                {
                    if (sMinX < minX) minX = sMinX;
                    if (sMinZ < minZ) minZ = sMinZ;
                    if (sMaxX > maxX) maxX = sMaxX;
                    if (sMaxZ > maxZ) maxZ = sMaxZ;
                }
            }

            return hasBounds;
        }

        private static bool TryGetShapeBoundsExpanded(
            ShapeDefinition shape,
            float widthMeters,
            out float minX,
            out float minZ,
            out float maxX,
            out float maxZ)
        {
            minX = minZ = maxX = maxZ = 0f;
            if (shape == null)
                return false;

            var expand = Math.Abs(widthMeters) * 0.5f;
            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    minX = Math.Min(shape.X, shape.X + shape.Width) - expand;
                    maxX = Math.Max(shape.X, shape.X + shape.Width) + expand;
                    minZ = Math.Min(shape.Z, shape.Z + shape.Height) - expand;
                    maxZ = Math.Max(shape.Z, shape.Z + shape.Height) + expand;
                    return true;
                case ShapeType.Circle:
                    {
                        var radius = Math.Abs(shape.Radius) + expand;
                        minX = shape.X - radius;
                        maxX = shape.X + radius;
                        minZ = shape.Z - radius;
                        maxZ = shape.Z + radius;
                        return true;
                    }
                case ShapeType.Ring:
                    {
                        var ringWidth = Math.Abs(widthMeters) > 0f ? Math.Abs(widthMeters) : Math.Abs(shape.RingWidth);
                        if (shape.Radius > 0f)
                        {
                            var outer = Math.Abs(shape.Radius) + ringWidth;
                            minX = shape.X - outer;
                            maxX = shape.X + outer;
                            minZ = shape.Z - outer;
                            maxZ = shape.Z + outer;
                            return true;
                        }

                        minX = Math.Min(shape.X, shape.X + shape.Width) - ringWidth;
                        maxX = Math.Max(shape.X, shape.X + shape.Width) + ringWidth;
                        minZ = Math.Min(shape.Z, shape.Z + shape.Height) - ringWidth;
                        maxZ = Math.Max(shape.Z, shape.Z + shape.Height) + ringWidth;
                        return true;
                    }
                case ShapeType.Polygon:
                case ShapeType.Polyline:
                    if (shape.Points == null || shape.Points.Count == 0)
                        return false;
                    minX = shape.Points[0].X;
                    maxX = shape.Points[0].X;
                    minZ = shape.Points[0].Y;
                    maxZ = shape.Points[0].Y;
                    for (var i = 1; i < shape.Points.Count; i++)
                    {
                        var point = shape.Points[i];
                        if (point.X < minX) minX = point.X;
                        if (point.X > maxX) maxX = point.X;
                        if (point.Y < minZ) minZ = point.Y;
                        if (point.Y > maxZ) maxZ = point.Y;
                    }
                    minX -= expand;
                    maxX += expand;
                    minZ -= expand;
                    maxZ += expand;
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBlockedByWall(TrackWallManager wallManager, Vector2 position)
        {
            if (wallManager == null || !wallManager.HasWalls)
                return false;

            foreach (var wall in wallManager.Walls)
            {
                if (wall == null)
                    continue;
                if (wall.CollisionMode == TrackWallCollisionMode.Pass)
                    continue;
                if (wallManager.Contains(wall, position))
                    return true;
            }

            return false;
        }

        private static bool IsBlockedByWall(TrackWallManager wallManager, Vector2 from, Vector2 to)
        {
            if (wallManager == null || !wallManager.HasWalls)
                return false;
            if (IsBlockedByWall(wallManager, from) || IsBlockedByWall(wallManager, to))
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
                if (IsBlockedByWall(wallManager, position))
                    return true;
                position += step;
            }

            return false;
        }

        private static int FindClosestDrivableIndex(
            Vector2 start,
            float minX,
            float minZ,
            float step,
            int countX,
            int countZ,
            bool[] drivable,
            out bool startOnTrack,
            out float startDistance)
        {
            startOnTrack = false;
            startDistance = 0f;
            if (drivable.Length == 0)
                return -1;

            var startIx = (int)Math.Round((start.X - minX) / step);
            var startIz = (int)Math.Round((start.Y - minZ) / step);
            startIx = Math.Clamp(startIx, 0, countX - 1);
            startIz = Math.Clamp(startIz, 0, countZ - 1);
            var startIndex = startIx + (startIz * countX);
            if (startIndex >= 0 && startIndex < drivable.Length && drivable[startIndex])
            {
                startOnTrack = true;
                startDistance = 0f;
                return startIndex;
            }

            var bestIndex = -1;
            var bestDistance = double.MaxValue;
            for (var iz = 0; iz < countZ; iz++)
            {
                var z = minZ + (iz * step);
                for (var ix = 0; ix < countX; ix++)
                {
                    var idx = ix + (iz * countX);
                    if (!drivable[idx])
                        continue;
                    var x = minX + (ix * step);
                    var dx = x - start.X;
                    var dz = z - start.Y;
                    var dist = (dx * dx) + (dz * dz);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestIndex = idx;
                    }
                }
            }

            if (bestIndex >= 0)
                startDistance = (float)Math.Sqrt(bestDistance);
            return bestIndex;
        }

        private static bool IsDrivableArea(TrackAreaDefinition area)
        {
            if (area == null)
                return false;
            if (area.Type == TrackAreaType.Boundary || area.Type == TrackAreaType.OffTrack)
                return false;
            if (area.Type == TrackAreaType.Start || area.Type == TrackAreaType.Finish ||
                area.Type == TrackAreaType.Checkpoint || area.Type == TrackAreaType.Intersection)
                return false;
            return true;
        }

        private static List<string> ResolveTrackFiles(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (File.Exists(path) && path.EndsWith(".tsm", StringComparison.OrdinalIgnoreCase))
                    return new List<string> { path };
                if (Directory.Exists(path))
                    return Directory.GetFiles(path, "*.tsm").OrderBy(p => p).ToList();
            }

            var tracksRoot = Path.Combine(AppContext.BaseDirectory, "Tracks");
            if (!Directory.Exists(tracksRoot))
            {
                Console.WriteLine($"Tracks folder not found: {tracksRoot}");
                return new List<string>();
            }

            var files = Directory.GetFiles(tracksRoot, "*.tsm").OrderBy(p => p).ToList();
            if (files.Count == 0)
                Console.WriteLine("No .tsm files found.");
            return files;
        }

        private static bool WriteLog(string trackPath, List<string> lines, out string logPath)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var name = Path.GetFileNameWithoutExtension(trackPath);
            var directory = Path.GetDirectoryName(trackPath);
            if (string.IsNullOrWhiteSpace(directory))
                directory = Directory.GetCurrentDirectory();
            logPath = Path.Combine(directory, $"{name}_autoexplore_{timestamp}.log");
            File.WriteAllLines(logPath, lines);
            return true;
        }

        private static bool IsExploreArg(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return false;
            var trimmed = arg.Trim().TrimStart('-', '/');
            return string.Equals(trimmed, ExploreArg, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trimmed, AutoArg, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidateArg(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return false;
            var trimmed = arg.Trim().TrimStart('-', '/');
            return string.Equals(trimmed, ValidateArg, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHelpArg(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                return false;
            var trimmed = arg.Trim();
            return string.Equals(trimmed, "-h", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trimmed, "--help", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(trimmed, "/?", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("GeometryTest usage:");
            Console.WriteLine("  GeometryTest [path]");
            Console.WriteLine("  GeometryTest validate [path]");
            Console.WriteLine("  GeometryTest explore [path]");
            Console.WriteLine();
            Console.WriteLine("If no path is provided, uses the Tracks folder next to the executable.");
        }
    }
}
