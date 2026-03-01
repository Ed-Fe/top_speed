using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal static class VehicleLoader
    {
        private readonly struct CommonSpec
        {
            public CommonSpec(
                float surfaceTractionFactor,
                float deceleration,
                float topSpeed,
                int idleFreq,
                int topFreq,
                int shiftFreq,
                int gears,
                float steering,
                int hasWipers,
                float idleRpm,
                float maxRpm,
                float revLimiter,
                float autoShiftRpm,
                float engineBraking,
                float massKg,
                float drivetrainEfficiency,
                float engineBrakingTorqueNm,
                float tireGripCoefficient,
                float peakTorqueNm,
                float peakTorqueRpm,
                float idleTorqueNm,
                float redlineTorqueNm,
                float dragCoefficient,
                float frontalAreaM2,
                float rollingResistanceCoefficient,
                float launchRpm,
                float finalDriveRatio,
                float reverseMaxSpeedKph,
                float reversePowerFactor,
                float reverseGearRatio,
                float tireCircumferenceM,
                float lateralGripCoefficient,
                float highSpeedStability,
                float wheelbaseM,
                float maxSteerDeg,
                float widthM,
                float lengthM,
                float powerFactor,
                float[]? gearRatios,
                float brakeStrength,
                TransmissionPolicy transmissionPolicy)
            {
                SurfaceTractionFactor = surfaceTractionFactor;
                Deceleration = deceleration;
                TopSpeed = topSpeed;
                IdleFreq = idleFreq;
                TopFreq = topFreq;
                ShiftFreq = shiftFreq;
                Gears = gears;
                Steering = steering;
                HasWipers = hasWipers;
                IdleRpm = idleRpm;
                MaxRpm = maxRpm;
                RevLimiter = revLimiter;
                AutoShiftRpm = autoShiftRpm;
                EngineBraking = engineBraking;
                MassKg = massKg;
                DrivetrainEfficiency = drivetrainEfficiency;
                EngineBrakingTorqueNm = engineBrakingTorqueNm;
                TireGripCoefficient = tireGripCoefficient;
                PeakTorqueNm = peakTorqueNm;
                PeakTorqueRpm = peakTorqueRpm;
                IdleTorqueNm = idleTorqueNm;
                RedlineTorqueNm = redlineTorqueNm;
                DragCoefficient = dragCoefficient;
                FrontalAreaM2 = frontalAreaM2;
                RollingResistanceCoefficient = rollingResistanceCoefficient;
                LaunchRpm = launchRpm;
                FinalDriveRatio = finalDriveRatio;
                ReverseMaxSpeedKph = reverseMaxSpeedKph;
                ReversePowerFactor = reversePowerFactor;
                ReverseGearRatio = reverseGearRatio;
                TireCircumferenceM = tireCircumferenceM;
                LateralGripCoefficient = lateralGripCoefficient;
                HighSpeedStability = highSpeedStability;
                WheelbaseM = wheelbaseM;
                MaxSteerDeg = maxSteerDeg;
                WidthM = widthM;
                LengthM = lengthM;
                PowerFactor = powerFactor;
                GearRatios = gearRatios;
                BrakeStrength = brakeStrength;
                TransmissionPolicy = transmissionPolicy;
            }

            public float SurfaceTractionFactor { get; }
            public float Deceleration { get; }
            public float TopSpeed { get; }
            public int IdleFreq { get; }
            public int TopFreq { get; }
            public int ShiftFreq { get; }
            public int Gears { get; }
            public float Steering { get; }
            public int HasWipers { get; }
            public float IdleRpm { get; }
            public float MaxRpm { get; }
            public float RevLimiter { get; }
            public float AutoShiftRpm { get; }
            public float EngineBraking { get; }
            public float MassKg { get; }
            public float DrivetrainEfficiency { get; }
            public float EngineBrakingTorqueNm { get; }
            public float TireGripCoefficient { get; }
            public float PeakTorqueNm { get; }
            public float PeakTorqueRpm { get; }
            public float IdleTorqueNm { get; }
            public float RedlineTorqueNm { get; }
            public float DragCoefficient { get; }
            public float FrontalAreaM2 { get; }
            public float RollingResistanceCoefficient { get; }
            public float LaunchRpm { get; }
            public float FinalDriveRatio { get; }
            public float ReverseMaxSpeedKph { get; }
            public float ReversePowerFactor { get; }
            public float ReverseGearRatio { get; }
            public float TireCircumferenceM { get; }
            public float LateralGripCoefficient { get; }
            public float HighSpeedStability { get; }
            public float WheelbaseM { get; }
            public float MaxSteerDeg { get; }
            public float WidthM { get; }
            public float LengthM { get; }
            public float PowerFactor { get; }
            public float[]? GearRatios { get; }
            public float BrakeStrength { get; }
            public TransmissionPolicy TransmissionPolicy { get; }
        }

        private const string BuiltinPrefix = "builtin";
        private const string DefaultVehicleFolder = "default";

        public static VehicleDefinition LoadOfficial(int vehicleIndex, TrackWeather weather)
        {
            if (vehicleIndex < 0 || vehicleIndex >= VehicleCatalog.VehicleCount)
                vehicleIndex = 0;

            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var vehiclesRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var currentVehicleFolder = $"Vehicle{vehicleIndex + 1}";

            var spec = new CommonSpec(
                parameters.SurfaceTractionFactor,
                parameters.Deceleration,
                parameters.TopSpeed,
                parameters.IdleFreq,
                parameters.TopFreq,
                parameters.ShiftFreq,
                parameters.Gears,
                parameters.Steering,
                parameters.HasWipers == 1 && weather == TrackWeather.Rain ? 1 : 0,
                parameters.IdleRpm,
                parameters.MaxRpm,
                parameters.RevLimiter,
                parameters.AutoShiftRpm > 0f ? parameters.AutoShiftRpm : parameters.RevLimiter * 0.92f,
                parameters.EngineBraking,
                parameters.MassKg,
                parameters.DrivetrainEfficiency,
                parameters.EngineBrakingTorqueNm,
                parameters.TireGripCoefficient,
                parameters.PeakTorqueNm,
                parameters.PeakTorqueRpm,
                parameters.IdleTorqueNm,
                parameters.RedlineTorqueNm,
                parameters.DragCoefficient,
                parameters.FrontalAreaM2,
                parameters.RollingResistanceCoefficient,
                parameters.LaunchRpm,
                parameters.FinalDriveRatio,
                parameters.ReverseMaxSpeedKph,
                parameters.ReversePowerFactor,
                parameters.ReverseGearRatio,
                parameters.TireCircumferenceM,
                parameters.LateralGripCoefficient,
                parameters.HighSpeedStability,
                parameters.WheelbaseM,
                parameters.MaxSteerDeg,
                parameters.WidthM,
                parameters.LengthM,
                parameters.PowerFactor,
                parameters.GearRatios,
                parameters.BrakeStrength,
                parameters.TransmissionPolicy);

            var def = new VehicleDefinition
            {
                CarType = (CarType)vehicleIndex,
                Name = parameters.Name,
                UserDefined = false
            };
            ApplyCommon(ref def, in spec);

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

            var hasWipers = weather == TrackWeather.Rain ? parsed.HasWipers : 0;
            var spec = new CommonSpec(
                parsed.SurfaceTractionFactor,
                parsed.Deceleration,
                parsed.TopSpeed,
                parsed.IdleFreq,
                parsed.TopFreq,
                parsed.ShiftFreq,
                parsed.Gears,
                parsed.Steering,
                hasWipers,
                parsed.IdleRpm,
                parsed.MaxRpm,
                parsed.RevLimiter,
                parsed.AutoShiftRpm > 0f ? parsed.AutoShiftRpm : parsed.RevLimiter * 0.92f,
                parsed.EngineBraking,
                parsed.MassKg,
                parsed.DrivetrainEfficiency,
                parsed.EngineBrakingTorqueNm,
                parsed.TireGripCoefficient,
                parsed.PeakTorqueNm,
                parsed.PeakTorqueRpm,
                parsed.IdleTorqueNm,
                parsed.RedlineTorqueNm,
                parsed.DragCoefficient,
                parsed.FrontalAreaM2,
                parsed.RollingResistanceCoefficient,
                parsed.LaunchRpm,
                parsed.FinalDriveRatio,
                parsed.ReverseMaxSpeedKph,
                parsed.ReversePowerFactor,
                parsed.ReverseGearRatio,
                parsed.TireCircumferenceM,
                parsed.LateralGripCoefficient,
                parsed.HighSpeedStability,
                parsed.WheelbaseM,
                parsed.MaxSteerDeg,
                parsed.WidthM,
                parsed.LengthM,
                parsed.PowerFactor,
                parsed.GearRatios,
                parsed.BrakeStrength,
                parsed.TransmissionPolicy);

            var def = new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                Name = parsed.Meta.Name,
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                CustomVersion = parsed.Meta.Version,
                CustomDescription = parsed.Meta.Description
            };
            ApplyCommon(ref def, in spec);

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

        private static void ApplyCommon(ref VehicleDefinition def, in CommonSpec spec)
        {
            def.SurfaceTractionFactor = spec.SurfaceTractionFactor;
            def.Deceleration = spec.Deceleration;
            def.TopSpeed = spec.TopSpeed;
            def.IdleFreq = spec.IdleFreq;
            def.TopFreq = spec.TopFreq;
            def.ShiftFreq = spec.ShiftFreq;
            def.Gears = spec.Gears;
            def.Steering = spec.Steering;
            def.HasWipers = spec.HasWipers;
            def.IdleRpm = spec.IdleRpm;
            def.MaxRpm = spec.MaxRpm;
            def.RevLimiter = spec.RevLimiter;
            def.AutoShiftRpm = spec.AutoShiftRpm;
            def.EngineBraking = spec.EngineBraking;
            def.MassKg = spec.MassKg;
            def.DrivetrainEfficiency = spec.DrivetrainEfficiency;
            def.EngineBrakingTorqueNm = spec.EngineBrakingTorqueNm;
            def.TireGripCoefficient = spec.TireGripCoefficient;
            def.PeakTorqueNm = spec.PeakTorqueNm;
            def.PeakTorqueRpm = spec.PeakTorqueRpm;
            def.IdleTorqueNm = spec.IdleTorqueNm;
            def.RedlineTorqueNm = spec.RedlineTorqueNm;
            def.DragCoefficient = spec.DragCoefficient;
            def.FrontalAreaM2 = spec.FrontalAreaM2;
            def.RollingResistanceCoefficient = spec.RollingResistanceCoefficient;
            def.LaunchRpm = spec.LaunchRpm;
            def.FinalDriveRatio = spec.FinalDriveRatio;
            def.ReverseMaxSpeedKph = spec.ReverseMaxSpeedKph;
            def.ReversePowerFactor = spec.ReversePowerFactor;
            def.ReverseGearRatio = spec.ReverseGearRatio;
            def.TireCircumferenceM = spec.TireCircumferenceM;
            def.LateralGripCoefficient = spec.LateralGripCoefficient;
            def.HighSpeedStability = spec.HighSpeedStability;
            def.WheelbaseM = spec.WheelbaseM;
            def.MaxSteerDeg = spec.MaxSteerDeg;
            def.WidthM = spec.WidthM;
            def.LengthM = spec.LengthM;
            def.PowerFactor = spec.PowerFactor;
            def.GearRatios = spec.GearRatios;
            def.BrakeStrength = spec.BrakeStrength;
            def.TransmissionPolicy = spec.TransmissionPolicy;
        }

        private static string? ResolveOfficialFallback(string root, string vehicleFolder, VehicleAction action)
        {
            var fileName = GetDefaultFileName(action);
            var primaryPath = Path.GetFullPath(Path.Combine(root, vehicleFolder, fileName));
            if (File.Exists(primaryPath))
                return primaryPath;

            // Only fallback to 'default' folder for non-optional sounds
            // Throttle and Backfire are vehicle-specific features
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

            var vehiclesRoot = builtinRoot;
            var parameters = VehicleCatalog.Vehicles[index];
            var file = parameters.GetSoundPath(action);
            if (!string.IsNullOrWhiteSpace(file))
                return Path.Combine(vehiclesRoot, file!);

            return ResolveOfficialFallback(vehiclesRoot, $"Vehicle{index + 1}", action);
        }
    }
}


