using System;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal static partial class VehicleLoader
    {
        private const string BuiltinPrefix = "builtin";
        private const string DefaultVehicleFolder = "default";

        public static VehicleDefinition LoadOfficial(int vehicleIndex, TrackWeather weather)
        {
            if (vehicleIndex < 0 || vehicleIndex >= VehicleCatalog.VehicleCount)
                vehicleIndex = 0;

            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var vehiclesRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var currentVehicleFolder = $"Vehicle{vehicleIndex + 1}";
            var spec = BuildSpec(parameters, weather);

            var def = new VehicleDefinition
            {
                CarType = (CarType)vehicleIndex,
                Name = parameters.Name,
                UserDefined = false
            };
            ApplyCommon(def, spec);

            foreach (VehicleAction action in Enum.GetValues(typeof(VehicleAction)))
            {
                var overridePath = parameters.GetSoundPath(action);
                if (!string.IsNullOrWhiteSpace(overridePath))
                {
                    def.SetSoundPath(action, Path.Combine(vehiclesRoot, overridePath!));
                }
                else
                {
                    def.SetSoundPath(action, ResolveOfficialFallback(vehiclesRoot, currentVehicleFolder, action));
                }
            }

            return def;
        }

        public static VehicleDefinition LoadCustom(string vehicleFile, TrackWeather weather)
        {
            var filePath = Path.IsPathRooted(vehicleFile)
                ? vehicleFile
                : Path.Combine(AssetPaths.Root, vehicleFile);
            var builtinRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            if (!VehicleTsvParser.TryLoadFromFile(filePath, out var parsed, out var issues))
            {
                var message = issues == null || issues.Count == 0
                    ? "Unknown parse error."
                    : string.Join(" ", issues);
                throw new InvalidDataException($"Failed to load custom vehicle '{filePath}'. {message}");
            }

            var spec = BuildSpec(parsed, weather);
            var def = new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                Name = parsed.Meta.Name,
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                CustomVersion = parsed.Meta.Version,
                CustomDescription = parsed.Meta.Description
            };
            ApplyCommon(def, spec);

            def.SetSoundPath(VehicleAction.Engine, ResolveCustomVehicleSound(parsed.Sounds.Engine, builtinRoot, parsed.SourceDirectory, VehicleAction.Engine));
            def.SetSoundPath(VehicleAction.Start, ResolveCustomVehicleSound(parsed.Sounds.Start, builtinRoot, parsed.SourceDirectory, VehicleAction.Start));
            def.SetSoundPath(VehicleAction.Horn, ResolveCustomVehicleSound(parsed.Sounds.Horn, builtinRoot, parsed.SourceDirectory, VehicleAction.Horn));
            if (!string.IsNullOrWhiteSpace(parsed.Sounds.Throttle))
                def.SetSoundPath(VehicleAction.Throttle, ResolveCustomVehicleSound(parsed.Sounds.Throttle!, builtinRoot, parsed.SourceDirectory, VehicleAction.Throttle));
            def.SetSoundPath(VehicleAction.Brake, ResolveCustomVehicleSound(parsed.Sounds.Brake, builtinRoot, parsed.SourceDirectory, VehicleAction.Brake));
            def.SetSoundPaths(VehicleAction.Crash, ResolveCustomVehicleSoundList(parsed.Sounds.CrashVariants, builtinRoot, parsed.SourceDirectory, VehicleAction.Crash));
            if (parsed.Sounds.BackfireVariants != null && parsed.Sounds.BackfireVariants.Count > 0)
                def.SetSoundPaths(VehicleAction.Backfire, ResolveCustomVehicleSoundList(parsed.Sounds.BackfireVariants, builtinRoot, parsed.SourceDirectory, VehicleAction.Backfire));

            return def;
        }
    }
}
