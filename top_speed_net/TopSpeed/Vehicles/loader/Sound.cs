using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    internal static partial class VehicleLoader
    {
        private static string? ResolveOfficialFallback(string root, string vehicleFolder, VehicleAction action)
        {
            var fileName = GetDefaultFileName(action);
            var primaryPath = Path.GetFullPath(Path.Combine(root, vehicleFolder, fileName));
            if (File.Exists(primaryPath))
                return primaryPath;

            // Only fallback to 'default' folder for non-optional sounds.
            if (action == VehicleAction.Backfire || action == VehicleAction.Throttle)
                return null;

            var fallbackPath = Path.GetFullPath(Path.Combine(root, DefaultVehicleFolder, fileName));
            if (File.Exists(fallbackPath))
                return fallbackPath;

            return null;
        }

        private static string GetDefaultFileName(VehicleAction action)
        {
            switch (action)
            {
                case VehicleAction.Engine: return "engine.wav";
                case VehicleAction.Start: return "start.wav";
                case VehicleAction.Horn: return "horn.wav";
                case VehicleAction.Throttle: return "throttle.wav";
                case VehicleAction.Crash: return "crash.wav";
                case VehicleAction.Brake: return "brake.wav";
                case VehicleAction.Backfire: return "backfire.wav";
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private static string[] ResolveCustomVehicleSoundList(
            IReadOnlyList<string> values,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction)
        {
            var result = new List<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var resolved = ResolveCustomVehicleSound(values[i], builtinRoot, vehicleRoot, builtinAction);
                if (!string.IsNullOrWhiteSpace(resolved))
                    result.Add(resolved!);
            }

            if (result.Count == 0)
                throw new InvalidDataException($"No valid sound paths resolved for {builtinAction}.");

            return result.ToArray();
        }

        private static string ResolveCustomVehicleSound(
            string value,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Missing required sound value for {builtinAction}.");

            var trimmed = value.Trim();
            if (trimmed.StartsWith(BuiltinPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fromBuiltin = ResolveCustomBuiltinSound(trimmed, builtinRoot, builtinAction);
                if (!string.IsNullOrWhiteSpace(fromBuiltin))
                    return fromBuiltin!;
                throw new InvalidDataException($"Builtin sound reference '{trimmed}' for {builtinAction} could not be resolved.");
            }

            if (Path.IsPathRooted(trimmed))
                throw new InvalidDataException($"Absolute sound paths are not allowed for custom vehicles: {trimmed}");

            var normalized = trimmed
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            if (normalized.IndexOf(':') >= 0 || ContainsTraversal(normalized))
                throw new InvalidDataException($"Invalid custom sound path '{trimmed}'. Paths must stay inside the vehicle folder.");

            var rootFull = Path.GetFullPath(vehicleRoot);
            var candidate = Path.GetFullPath(Path.Combine(rootFull, normalized));
            if (!IsInsideRoot(rootFull, candidate))
                throw new InvalidDataException($"Custom sound path '{trimmed}' escapes the vehicle folder.");
            if (!File.Exists(candidate))
                throw new FileNotFoundException($"Custom vehicle sound file not found: {candidate}", candidate);
            return candidate;
        }

        private static bool ContainsTraversal(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar);
            for (var i = 0; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                if (segment == "." || segment == "..")
                    return true;
            }
            return false;
        }

        private static bool IsInsideRoot(string rootFull, string candidate)
        {
            if (string.Equals(rootFull, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
            var rootWithSeparator = rootFull.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        private static string? ResolveCustomBuiltinSound(string token, string builtinRoot, VehicleAction action)
        {
            if (!int.TryParse(token.Substring(BuiltinPrefix.Length), out var index))
                return null;
            index -= 1;
            if (index < 0 || index >= VehicleCatalog.VehicleCount)
                return null;

            var parameters = VehicleCatalog.Vehicles[index];
            var file = parameters.GetSoundPath(action);
            if (!string.IsNullOrWhiteSpace(file))
                return Path.Combine(builtinRoot, file!);

            return ResolveOfficialFallback(builtinRoot, $"Vehicle{index + 1}", action);
        }
    }
}
