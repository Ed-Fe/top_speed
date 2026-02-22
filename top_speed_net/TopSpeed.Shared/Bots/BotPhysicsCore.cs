using System;
using TopSpeed.Data;

namespace TopSpeed.Bots
{
    public struct BotPhysicsState
    {
        public float PositionX;
        public float PositionY;
        public float SpeedKph;
        public int Gear;
        public float AutoShiftCooldownSeconds;
    }

    public readonly struct BotPhysicsInput
    {
        public BotPhysicsInput(float elapsedSeconds, TrackSurface surface, int throttle, int brake, int steering)
        {
            ElapsedSeconds = elapsedSeconds;
            Surface = surface;
            Throttle = throttle;
            Brake = brake;
            Steering = steering;
        }

        public float ElapsedSeconds { get; }
        public TrackSurface Surface { get; }
        public int Throttle { get; }
        public int Brake { get; }
        public int Steering { get; }
    }

    public sealed class BotPhysicsConfig
    {
        public BotPhysicsConfig(
            float surfaceTractionFactor,
            float deceleration,
            float topSpeedKph,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
            float brakeStrength,
            float wheelRadiusM,
            float engineBraking,
            float idleRpm,
            float revLimiter,
            float finalDriveRatio,
            float powerFactor,
            float peakTorqueNm,
            float peakTorqueRpm,
            float idleTorqueNm,
            float redlineTorqueNm,
            float dragCoefficient,
            float frontalAreaM2,
            float rollingResistanceCoefficient,
            float launchRpm,
            float lateralGripCoefficient,
            float highSpeedStability,
            float wheelbaseM,
            float maxSteerDeg,
            float steering,
            int gears,
            float[]? gearRatios = null)
        {
            SurfaceTractionFactor = Math.Max(0.01f, surfaceTractionFactor);
            Deceleration = Math.Max(0.01f, deceleration);
            TopSpeedKph = Math.Max(1f, topSpeedKph);
            MassKg = Math.Max(1f, massKg);
            DrivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, drivetrainEfficiency));
            EngineBrakingTorqueNm = Math.Max(0f, engineBrakingTorqueNm);
            TireGripCoefficient = Math.Max(0.1f, tireGripCoefficient);
            BrakeStrength = Math.Max(0.1f, brakeStrength);
            WheelRadiusM = Math.Max(0.01f, wheelRadiusM);
            EngineBraking = Math.Max(0.05f, Math.Min(1.0f, engineBraking));
            IdleRpm = Math.Max(500f, idleRpm);
            RevLimiter = Math.Max(IdleRpm, revLimiter);
            FinalDriveRatio = Math.Max(0.1f, finalDriveRatio);
            PowerFactor = Math.Max(0.1f, powerFactor);
            PeakTorqueNm = Math.Max(0f, peakTorqueNm);
            PeakTorqueRpm = Math.Max(IdleRpm + 100f, peakTorqueRpm);
            IdleTorqueNm = Math.Max(0f, idleTorqueNm);
            RedlineTorqueNm = Math.Max(0f, redlineTorqueNm);
            DragCoefficient = Math.Max(0.01f, dragCoefficient);
            FrontalAreaM2 = Math.Max(0.1f, frontalAreaM2);
            RollingResistanceCoefficient = Math.Max(0.001f, rollingResistanceCoefficient);
            LaunchRpm = Math.Max(IdleRpm, Math.Min(RevLimiter, launchRpm));
            LateralGripCoefficient = Math.Max(0.1f, lateralGripCoefficient);
            HighSpeedStability = Math.Max(0f, Math.Min(1.0f, highSpeedStability));
            WheelbaseM = Math.Max(0.5f, wheelbaseM);
            MaxSteerDeg = Math.Max(5f, Math.Min(60f, maxSteerDeg));
            Steering = steering;
            Gears = Math.Max(1, gears);
            GearRatios = BuildRatios(Gears, gearRatios);
        }

        public float SurfaceTractionFactor { get; }
        public float Deceleration { get; }
        public float TopSpeedKph { get; }
        public float MassKg { get; }
        public float DrivetrainEfficiency { get; }
        public float EngineBrakingTorqueNm { get; }
        public float TireGripCoefficient { get; }
        public float BrakeStrength { get; }
        public float WheelRadiusM { get; }
        public float EngineBraking { get; }
        public float IdleRpm { get; }
        public float RevLimiter { get; }
        public float FinalDriveRatio { get; }
        public float PowerFactor { get; }
        public float PeakTorqueNm { get; }
        public float PeakTorqueRpm { get; }
        public float IdleTorqueNm { get; }
        public float RedlineTorqueNm { get; }
        public float DragCoefficient { get; }
        public float FrontalAreaM2 { get; }
        public float RollingResistanceCoefficient { get; }
        public float LaunchRpm { get; }
        public float LateralGripCoefficient { get; }
        public float HighSpeedStability { get; }
        public float WheelbaseM { get; }
        public float MaxSteerDeg { get; }
        public float Steering { get; }
        public int Gears { get; }
        public float[] GearRatios { get; }

        public float GetGearRatio(int gear)
        {
            var clamped = Math.Max(1, Math.Min(Gears, gear));
            return GearRatios[clamped - 1];
        }

        private static float[] BuildRatios(int gears, float[]? provided)
        {
            if (provided != null && provided.Length == gears)
                return provided;

            var ratios = new float[gears];
            const float first = 3.5f;
            const float last = 0.85f;
            var logFirst = Math.Log(first);
            var logLast = Math.Log(last);
            for (var i = 0; i < gears; i++)
            {
                var t = gears > 1 ? i / (float)(gears - 1) : 0f;
                ratios[i] = (float)Math.Exp(logFirst + ((logLast - logFirst) * t));
            }
            return ratios;
        }
    }

    public static class BotPhysics
    {
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float AutoShiftHysteresis = 0.05f;
        private const float AutoShiftCooldownSeconds = 0.15f;

        public static void Step(BotPhysicsConfig config, ref BotPhysicsState state, in BotPhysicsInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (input.ElapsedSeconds <= 0f)
                return;

            if (state.Gear < 1 || state.Gear > config.Gears)
                state.Gear = 1;

            var surfaceTraction = config.SurfaceTractionFactor;
            var surfaceDecel = config.Deceleration;
            switch (input.Surface)
            {
                case TrackSurface.Gravel:
                    surfaceTraction = (surfaceTraction * 2f) / 3f;
                    surfaceDecel = (surfaceDecel * 2f) / 3f;
                    break;
                case TrackSurface.Water:
                    surfaceTraction = (surfaceTraction * 3f) / 5f;
                    surfaceDecel = (surfaceDecel * 3f) / 5f;
                    break;
                case TrackSurface.Sand:
                    surfaceTraction = surfaceTraction / 2f;
                    surfaceDecel = (surfaceDecel * 3f) / 2f;
                    break;
                case TrackSurface.Snow:
                    surfaceDecel = surfaceDecel / 2f;
                    break;
            }

            var thrust = 0f;
            if (input.Throttle == 0)
                thrust = input.Brake;
            else if (input.Brake == 0 || -input.Brake <= input.Throttle)
                thrust = input.Throttle;
            else
                thrust = input.Brake;

            var speedKph = Math.Max(0f, state.SpeedKph);
            var speedMpsCurrent = speedKph / 3.6f;
            var throttle = Math.Max(0f, Math.Min(100f, input.Throttle)) / 100f;
            var steeringInput = input.Steering;
            var surfaceTractionMod = surfaceTraction / config.SurfaceTractionFactor;
            var longitudinalGripFactor = 1.0f;
            var speedDiffKph = 0f;

            if (thrust > 10f)
            {
                var steeringCommandAccel = (steeringInput / 100.0f) * config.Steering;
                steeringCommandAccel = Clamp(steeringCommandAccel, -1f, 1f);
                var steerRadAccel = DegToRad(config.MaxSteerDeg * steeringCommandAccel);
                var curvatureAccel = (float)Math.Tan(steerRadAccel) / config.WheelbaseM;
                var desiredLatAccel = curvatureAccel * speedMpsCurrent * speedMpsCurrent;
                var desiredLatAccelAbs = Math.Abs(desiredLatAccel);
                var grip = config.TireGripCoefficient * surfaceTractionMod * config.LateralGripCoefficient;
                var maxLatAccel = grip * 9.80665f;
                var lateralRatio = maxLatAccel > 0f ? Math.Min(1.0f, desiredLatAccelAbs / maxLatAccel) : 0f;
                longitudinalGripFactor = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (lateralRatio * lateralRatio)));

                var driveRpm = CalculateDriveRpm(config, state.Gear, speedMpsCurrent, throttle);
                var engineTorque = CalculateEngineTorqueNm(config, driveRpm) * throttle * config.PowerFactor;
                var gearRatio = config.GetGearRatio(state.Gear);
                var wheelTorque = engineTorque * gearRatio * config.FinalDriveRatio * config.DrivetrainEfficiency;
                var wheelForce = wheelTorque / config.WheelRadiusM;
                var tractionLimit = config.TireGripCoefficient * surfaceTractionMod * config.MassKg * 9.80665f;
                if (wheelForce > tractionLimit)
                    wheelForce = tractionLimit;
                wheelForce *= longitudinalGripFactor;

                var dragForce = 0.5f * 1.225f * config.DragCoefficient * config.FrontalAreaM2 * speedMpsCurrent * speedMpsCurrent;
                var rollingForce = config.RollingResistanceCoefficient * config.MassKg * 9.80665f;
                var netForce = wheelForce - dragForce - rollingForce;
                var accelMps2 = netForce / config.MassKg;
                var newSpeedMps = speedMpsCurrent + (accelMps2 * input.ElapsedSeconds);
                if (newSpeedMps < 0f)
                    newSpeedMps = 0f;
                speedDiffKph = (newSpeedMps - speedMpsCurrent) * 3.6f;
            }
            else
            {
                var surfaceDecelMod = surfaceDecel / config.Deceleration;
                var brakeInput = Math.Max(0f, Math.Min(100f, -input.Brake)) / 100f;
                var brakeDecel = CalculateBrakeDecel(config, brakeInput, surfaceDecelMod);
                var engineBrakeDecel = CalculateEngineBrakingDecel(config, state.Gear, speedMpsCurrent, surfaceDecelMod);
                var totalDecel = thrust < -10f ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
                speedDiffKph = -totalDecel * input.ElapsedSeconds;
            }

            speedKph += speedDiffKph;
            if (speedKph > config.TopSpeedKph)
                speedKph = config.TopSpeedKph;
            if (speedKph < 0f)
                speedKph = 0f;

            UpdateAutomaticGear(config, ref state, input.ElapsedSeconds, speedKph / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
            if (thrust < -50f && speedKph > 0f)
                steeringInput = steeringInput * 2 / 3;

            var speedMps = speedKph / 3.6f;
            state.PositionY += speedMps * input.ElapsedSeconds;
            state.SpeedKph = speedKph;

            var surfaceMultiplier = input.Surface == TrackSurface.Snow ? 1.44f : 1.0f;
            var steeringCommandLat = (steeringInput / 100.0f) * config.Steering;
            steeringCommandLat = Clamp(steeringCommandLat, -1f, 1f);
            var steerRadLat = DegToRad(config.MaxSteerDeg * steeringCommandLat);
            var curvatureLat = (float)Math.Tan(steerRadLat) / config.WheelbaseM;
            var surfaceTractionModLat = surfaceTraction / config.SurfaceTractionFactor;
            var gripLat = config.TireGripCoefficient * surfaceTractionModLat * config.LateralGripCoefficient;
            var maxLatAccelLat = gripLat * 9.80665f;
            var desiredLatAccelLat = curvatureLat * speedMps * speedMps;
            var massFactor = (float)Math.Sqrt(1500f / config.MassKg);
            if (massFactor > 3.0f)
                massFactor = 3.0f;
            var stabilityScale = 1.0f - (config.HighSpeedStability * (speedMps / StabilitySpeedRef) * massFactor);
            if (stabilityScale < 0.2f)
                stabilityScale = 0.2f;
            else if (stabilityScale > 1.0f)
                stabilityScale = 1.0f;
            var responseTime = BaseLateralSpeed / 20.0f;
            var maxLatSpeed = maxLatAccelLat * responseTime * stabilityScale;
            var desiredLatSpeed = desiredLatAccelLat * responseTime;
            if (desiredLatSpeed > maxLatSpeed)
                desiredLatSpeed = maxLatSpeed;
            else if (desiredLatSpeed < -maxLatSpeed)
                desiredLatSpeed = -maxLatSpeed;
            var lateralSpeed = desiredLatSpeed * surfaceMultiplier;
            state.PositionX += lateralSpeed * input.ElapsedSeconds;
        }

        private static void UpdateAutomaticGear(
            BotPhysicsConfig config,
            ref BotPhysicsState state,
            float elapsed,
            float speedMps,
            float throttle,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            if (config.Gears <= 1)
                return;

            if (state.AutoShiftCooldownSeconds > 0f)
            {
                state.AutoShiftCooldownSeconds -= elapsed;
                return;
            }

            var currentAccel = ComputeNetAccelForGear(config, state.Gear, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
            var bestGear = state.Gear;
            var bestAccel = currentAccel;

            if (state.Gear < config.Gears)
            {
                var upAccel = ComputeNetAccelForGear(config, state.Gear + 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
                if (upAccel > bestAccel)
                {
                    bestAccel = upAccel;
                    bestGear = state.Gear + 1;
                }
            }

            if (state.Gear > 1)
            {
                var downAccel = ComputeNetAccelForGear(config, state.Gear - 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
                if (downAccel > bestAccel)
                {
                    bestAccel = downAccel;
                    bestGear = state.Gear - 1;
                }
            }

            var currentRpm = SpeedToRpm(config, speedMps, state.Gear);
            if (state.Gear < config.Gears && currentRpm >= config.RevLimiter * 0.995f)
            {
                state.Gear += 1;
                state.AutoShiftCooldownSeconds = AutoShiftCooldownSeconds;
                return;
            }

            var shiftRpm = config.IdleRpm + (config.RevLimiter - config.IdleRpm) * 0.35f;
            if (state.Gear > 1 && currentRpm < shiftRpm)
            {
                state.Gear -= 1;
                state.AutoShiftCooldownSeconds = AutoShiftCooldownSeconds;
                return;
            }

            if (bestGear != state.Gear && bestAccel > currentAccel * (1f + AutoShiftHysteresis))
            {
                state.Gear = bestGear;
                state.AutoShiftCooldownSeconds = AutoShiftCooldownSeconds;
            }
        }

        private static float ComputeNetAccelForGear(
            BotPhysicsConfig config,
            int gear,
            float speedMps,
            float throttle,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            var rpm = SpeedToRpm(config, speedMps, gear);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > config.RevLimiter && gear < config.Gears)
                return float.NegativeInfinity;

            var engineTorque = CalculateEngineTorqueNm(config, rpm) * throttle * config.PowerFactor;
            var gearRatio = config.GetGearRatio(gear);
            var wheelTorque = engineTorque * gearRatio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var tractionLimit = config.TireGripCoefficient * surfaceTractionMod * config.MassKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;

            var dragForce = 0.5f * 1.225f * config.DragCoefficient * config.FrontalAreaM2 * speedMps * speedMps;
            var rollingForce = config.RollingResistanceCoefficient * config.MassKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            return netForce / config.MassKg;
        }

        private static float SpeedToRpm(BotPhysicsConfig config, float speedMps, int gear)
        {
            var wheelCircumference = config.WheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = config.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * config.FinalDriveRatio;
        }

        private static float CalculateDriveRpm(BotPhysicsConfig config, int gear, float speedMps, float throttle)
        {
            var wheelCircumference = config.WheelRadiusM * 2.0f * (float)Math.PI;
            var gearRatio = config.GetGearRatio(gear);
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * config.FinalDriveRatio
                : 0f;
            var launchTarget = config.IdleRpm + (throttle * (config.LaunchRpm - config.IdleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            if (rpm < config.IdleRpm)
                rpm = config.IdleRpm;
            if (rpm > config.RevLimiter)
                rpm = config.RevLimiter;
            return rpm;
        }

        private static float CalculateEngineTorqueNm(BotPhysicsConfig config, float rpm)
        {
            if (config.PeakTorqueNm <= 0f)
                return 0f;

            var clampedRpm = Math.Max(config.IdleRpm, Math.Min(config.RevLimiter, rpm));
            if (clampedRpm <= config.PeakTorqueRpm)
            {
                var denom = config.PeakTorqueRpm - config.IdleRpm;
                var t = denom > 0f ? (clampedRpm - config.IdleRpm) / denom : 0f;
                return SmoothStep(config.IdleTorqueNm, config.PeakTorqueNm, t);
            }

            {
                var denom = config.RevLimiter - config.PeakTorqueRpm;
                var t = denom > 0f ? (clampedRpm - config.PeakTorqueRpm) / denom : 0f;
                return SmoothStep(config.PeakTorqueNm, config.RedlineTorqueNm, t);
            }
        }

        private static float CalculateBrakeDecel(BotPhysicsConfig config, float brakeInput, float surfaceDecelMod)
        {
            if (brakeInput <= 0f)
                return 0f;
            var grip = Math.Max(0.1f, config.TireGripCoefficient * surfaceDecelMod);
            var decelMps2 = brakeInput * config.BrakeStrength * grip * 9.80665f;
            return decelMps2 * 3.6f;
        }

        private static float CalculateEngineBrakingDecel(BotPhysicsConfig config, int gear, float speedMps, float surfaceDecelMod)
        {
            if (config.EngineBrakingTorqueNm <= 0f || config.MassKg <= 0f || config.WheelRadiusM <= 0f)
                return 0f;

            var rpmRange = config.RevLimiter - config.IdleRpm;
            if (rpmRange <= 0f)
                return 0f;
            var rpm = SpeedToRpm(config, speedMps, gear);
            var rpmFactor = (rpm - config.IdleRpm) / rpmRange;
            if (rpmFactor <= 0f)
                return 0f;
            rpmFactor = Clamp(rpmFactor, 0f, 1f);
            var gearRatio = config.GetGearRatio(gear);
            var drivelineTorque = config.EngineBrakingTorqueNm * config.EngineBraking * rpmFactor;
            var wheelTorque = drivelineTorque * gearRatio * config.FinalDriveRatio * config.DrivetrainEfficiency;
            var wheelForce = wheelTorque / config.WheelRadiusM;
            var decelMps2 = (wheelForce / config.MassKg) * surfaceDecelMod;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        private static float SmoothStep(float a, float b, float t)
        {
            var clamped = Clamp(t, 0f, 1f);
            clamped = clamped * clamped * (3f - 2f * clamped);
            return a + (b - a) * clamped;
        }

        private static float Clamp(float v, float min, float max)
        {
            if (v < min)
                return min;
            if (v > max)
                return max;
            return v;
        }

        private static float DegToRad(float deg)
        {
            return (float)(Math.PI / 180.0) * deg;
        }
    }
}
