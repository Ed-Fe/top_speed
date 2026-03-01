using TopSpeed.Data;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal static partial class VehicleLoader
    {
        private sealed class CommonSpec
        {
            public float SurfaceTractionFactor { get; set; }
            public float Deceleration { get; set; }
            public float TopSpeed { get; set; }
            public int IdleFreq { get; set; }
            public int TopFreq { get; set; }
            public int ShiftFreq { get; set; }
            public int Gears { get; set; }
            public float Steering { get; set; }
            public int HasWipers { get; set; }
            public float IdleRpm { get; set; }
            public float MaxRpm { get; set; }
            public float RevLimiter { get; set; }
            public float AutoShiftRpm { get; set; }
            public float EngineBraking { get; set; }
            public float MassKg { get; set; }
            public float DrivetrainEfficiency { get; set; }
            public float EngineBrakingTorqueNm { get; set; }
            public float TireGripCoefficient { get; set; }
            public float PeakTorqueNm { get; set; }
            public float PeakTorqueRpm { get; set; }
            public float IdleTorqueNm { get; set; }
            public float RedlineTorqueNm { get; set; }
            public float DragCoefficient { get; set; }
            public float FrontalAreaM2 { get; set; }
            public float RollingResistanceCoefficient { get; set; }
            public float LaunchRpm { get; set; }
            public float FinalDriveRatio { get; set; }
            public float ReverseMaxSpeedKph { get; set; }
            public float ReversePowerFactor { get; set; }
            public float ReverseGearRatio { get; set; }
            public float TireCircumferenceM { get; set; }
            public float LateralGripCoefficient { get; set; }
            public float HighSpeedStability { get; set; }
            public float WheelbaseM { get; set; }
            public float MaxSteerDeg { get; set; }
            public float WidthM { get; set; }
            public float LengthM { get; set; }
            public float PowerFactor { get; set; }
            public float[]? GearRatios { get; set; }
            public float BrakeStrength { get; set; }
            public TransmissionPolicy TransmissionPolicy { get; set; } = TransmissionPolicy.Default;
        }

        private static CommonSpec BuildSpec(VehicleParameters parameters, TrackWeather weather)
        {
            return new CommonSpec
            {
                SurfaceTractionFactor = parameters.SurfaceTractionFactor,
                Deceleration = parameters.Deceleration,
                TopSpeed = parameters.TopSpeed,
                IdleFreq = parameters.IdleFreq,
                TopFreq = parameters.TopFreq,
                ShiftFreq = parameters.ShiftFreq,
                Gears = parameters.Gears,
                Steering = parameters.Steering,
                HasWipers = parameters.HasWipers == 1 && weather == TrackWeather.Rain ? 1 : 0,
                IdleRpm = parameters.IdleRpm,
                MaxRpm = parameters.MaxRpm,
                RevLimiter = parameters.RevLimiter,
                AutoShiftRpm = parameters.AutoShiftRpm > 0f ? parameters.AutoShiftRpm : parameters.RevLimiter * 0.92f,
                EngineBraking = parameters.EngineBraking,
                MassKg = parameters.MassKg,
                DrivetrainEfficiency = parameters.DrivetrainEfficiency,
                EngineBrakingTorqueNm = parameters.EngineBrakingTorqueNm,
                TireGripCoefficient = parameters.TireGripCoefficient,
                PeakTorqueNm = parameters.PeakTorqueNm,
                PeakTorqueRpm = parameters.PeakTorqueRpm,
                IdleTorqueNm = parameters.IdleTorqueNm,
                RedlineTorqueNm = parameters.RedlineTorqueNm,
                DragCoefficient = parameters.DragCoefficient,
                FrontalAreaM2 = parameters.FrontalAreaM2,
                RollingResistanceCoefficient = parameters.RollingResistanceCoefficient,
                LaunchRpm = parameters.LaunchRpm,
                FinalDriveRatio = parameters.FinalDriveRatio,
                ReverseMaxSpeedKph = parameters.ReverseMaxSpeedKph,
                ReversePowerFactor = parameters.ReversePowerFactor,
                ReverseGearRatio = parameters.ReverseGearRatio,
                TireCircumferenceM = parameters.TireCircumferenceM,
                LateralGripCoefficient = parameters.LateralGripCoefficient,
                HighSpeedStability = parameters.HighSpeedStability,
                WheelbaseM = parameters.WheelbaseM,
                MaxSteerDeg = parameters.MaxSteerDeg,
                WidthM = parameters.WidthM,
                LengthM = parameters.LengthM,
                PowerFactor = parameters.PowerFactor,
                GearRatios = parameters.GearRatios,
                BrakeStrength = parameters.BrakeStrength,
                TransmissionPolicy = parameters.TransmissionPolicy
            };
        }

        private static CommonSpec BuildSpec(CustomVehicleTsvData parsed, TrackWeather weather)
        {
            return new CommonSpec
            {
                SurfaceTractionFactor = parsed.SurfaceTractionFactor,
                Deceleration = parsed.Deceleration,
                TopSpeed = parsed.TopSpeed,
                IdleFreq = parsed.IdleFreq,
                TopFreq = parsed.TopFreq,
                ShiftFreq = parsed.ShiftFreq,
                Gears = parsed.Gears,
                Steering = parsed.Steering,
                HasWipers = weather == TrackWeather.Rain ? parsed.HasWipers : 0,
                IdleRpm = parsed.IdleRpm,
                MaxRpm = parsed.MaxRpm,
                RevLimiter = parsed.RevLimiter,
                AutoShiftRpm = parsed.AutoShiftRpm > 0f ? parsed.AutoShiftRpm : parsed.RevLimiter * 0.92f,
                EngineBraking = parsed.EngineBraking,
                MassKg = parsed.MassKg,
                DrivetrainEfficiency = parsed.DrivetrainEfficiency,
                EngineBrakingTorqueNm = parsed.EngineBrakingTorqueNm,
                TireGripCoefficient = parsed.TireGripCoefficient,
                PeakTorqueNm = parsed.PeakTorqueNm,
                PeakTorqueRpm = parsed.PeakTorqueRpm,
                IdleTorqueNm = parsed.IdleTorqueNm,
                RedlineTorqueNm = parsed.RedlineTorqueNm,
                DragCoefficient = parsed.DragCoefficient,
                FrontalAreaM2 = parsed.FrontalAreaM2,
                RollingResistanceCoefficient = parsed.RollingResistanceCoefficient,
                LaunchRpm = parsed.LaunchRpm,
                FinalDriveRatio = parsed.FinalDriveRatio,
                ReverseMaxSpeedKph = parsed.ReverseMaxSpeedKph,
                ReversePowerFactor = parsed.ReversePowerFactor,
                ReverseGearRatio = parsed.ReverseGearRatio,
                TireCircumferenceM = parsed.TireCircumferenceM,
                LateralGripCoefficient = parsed.LateralGripCoefficient,
                HighSpeedStability = parsed.HighSpeedStability,
                WheelbaseM = parsed.WheelbaseM,
                MaxSteerDeg = parsed.MaxSteerDeg,
                WidthM = parsed.WidthM,
                LengthM = parsed.LengthM,
                PowerFactor = parsed.PowerFactor,
                GearRatios = parsed.GearRatios,
                BrakeStrength = parsed.BrakeStrength,
                TransmissionPolicy = parsed.TransmissionPolicy
            };
        }

        private static void ApplyCommon(VehicleDefinition def, CommonSpec spec)
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
    }
}
