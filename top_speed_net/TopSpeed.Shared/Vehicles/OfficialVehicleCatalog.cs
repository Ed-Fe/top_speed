using System;
using TopSpeed.Protocol;

namespace TopSpeed.Vehicles
{
    public sealed class OfficialVehicleSpec
    {
        public OfficialVehicleSpec(
            CarType carType,
            string name,
            int hasWipers,
            float surfaceTractionFactor,
            float deceleration,
            float topSpeed,
            int idleFreq,
            int topFreq,
            int shiftFreq,
            int gears,
            float steering,
            int steeringFactor,
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
            float[] gearRatios,
            float brakeStrength,
            TransmissionPolicy? transmissionPolicy = null)
        {
            CarType = carType;
            Name = name;
            HasWipers = hasWipers;
            SurfaceTractionFactor = surfaceTractionFactor;
            Deceleration = deceleration;
            TopSpeed = topSpeed;
            IdleFreq = idleFreq;
            TopFreq = topFreq;
            ShiftFreq = shiftFreq;
            Gears = gears;
            Steering = steering;
            SteeringFactor = steeringFactor;
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
            TransmissionPolicy = transmissionPolicy ?? TransmissionPolicy.Default;
        }

        public CarType CarType { get; }
        public string Name { get; }
        public int HasWipers { get; }
        public float SurfaceTractionFactor { get; }
        public float Deceleration { get; }
        public float TopSpeed { get; }
        public int IdleFreq { get; }
        public int TopFreq { get; }
        public int ShiftFreq { get; }
        public int Gears { get; }
        public float Steering { get; }
        public int SteeringFactor { get; }
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
        public float[] GearRatios { get; }
        public float BrakeStrength { get; }
        public TransmissionPolicy TransmissionPolicy { get; }
    }

    public static class OfficialVehicleCatalog
    {
        public const int VehicleCount = 12;

        private static readonly float[] GtrRatios = { 4.06f, 2.30f, 1.59f, 1.25f, 1.00f, 0.80f };
        private static readonly float[] Gt3RsRatios = { 3.75f, 2.38f, 1.72f, 1.34f, 1.11f, 1.02f, 0.95f };
        private static readonly float[] Fiat500Ratios = { 3.909f, 2.238f, 1.520f, 1.156f, 0.872f };
        private static readonly float[] MiniCooperSRatios = { 3.92f, 2.14f, 1.39f, 1.09f, 0.89f, 0.76f };
        private static readonly float[] Mustang69Ratios = { 2.32f, 1.69f, 1.29f, 1.00f };
        private static readonly float[] CamryRatios = { 5.25f, 3.03f, 1.95f, 1.46f, 1.22f, 1.00f, 0.95f, 0.90f };
        private static readonly float[] AventadorRatios = { 3.91f, 2.10f, 1.55f, 1.30f, 1.19f, 1.06f, 0.98f };
        private static readonly float[] Bmw3SeriesRatios = { 4.71f, 3.14f, 2.11f, 1.67f, 1.29f, 1.00f, 0.96f, 0.90f };
        private static readonly float[] SprinterRatios = { 4.3772f, 2.8586f, 1.9206f, 1.3684f, 1.0000f, 0.9200f, 0.8500f };
        private static readonly float[] Zx10rRatios = { 2.600f, 2.222f, 1.944f, 1.722f, 1.550f, 1.391f };
        private static readonly float[] PanigaleV4Ratios = { 2.40f, 2.00f, 1.7368f, 1.5238f, 1.3636f, 1.2273f };
        private static readonly float[] R1Ratios = { 2.533f, 2.063f, 1.762f, 1.522f, 1.364f, 1.269f };

        private static readonly float[] Auto8Upshifts = { 0f, 0f, 0f, 0f, 0.18f, 0.24f, 0.30f, 0.34f };
        private static readonly float[] Auto7Upshifts = { 0f, 0f, 0f, 0f, 0.18f, 0.24f, 0.28f };
        private static readonly float[] Auto6Upshifts = { 0f, 0f, 0f, 0f, 0.20f, 0.26f };

        public static readonly OfficialVehicleSpec[] Vehicles =
        {
            new OfficialVehicleSpec(
                CarType.Vehicle1, "Nissan GT-R Nismo",
                hasWipers: 1, surfaceTractionFactor: 0.06f, deceleration: 0.40f, topSpeed: 268.0f,
                idleFreq: 22050, topFreq: 55000, shiftFreq: 26000, gears: 6, steering: 1.60f, steeringFactor: 60,
                idleRpm: 900f, maxRpm: 8000f, revLimiter: 7600f, autoShiftRpm: 7600f * 0.92f, engineBraking: 0.25f,
                massKg: 1774f, drivetrainEfficiency: 0.80f, engineBrakingTorqueNm: 652f, tireGripCoefficient: 1.0f,
                peakTorqueNm: 652f, peakTorqueRpm: 3600f, idleTorqueNm: 652f * 0.3f, redlineTorqueNm: 652f * 0.6f,
                dragCoefficient: 0.26f, frontalAreaM2: 2.2f, rollingResistanceCoefficient: 0.015f, launchRpm: 2500f,
                finalDriveRatio: 3.70f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(285, 35, 20), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.779f, maxSteerDeg: 35f, widthM: 1.895f, lengthM: 4.689f,
                powerFactor: 0.67f, gearRatios: GtrRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto6Upshifts, upshiftRpmFraction: 0.90f)),

            new OfficialVehicleSpec(
                CarType.Vehicle2, "Porsche 911 GT3 RS",
                hasWipers: 1, surfaceTractionFactor: 0.07f, deceleration: 0.45f, topSpeed: 248.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 7, steering: 1.50f, steeringFactor: 55,
                idleRpm: 950f, maxRpm: 9000f, revLimiter: 8500f, autoShiftRpm: 8500f * 0.92f, engineBraking: 0.22f,
                massKg: 1450f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 465f, tireGripCoefficient: 1.05f,
                peakTorqueNm: 465f, peakTorqueRpm: 6250f, idleTorqueNm: 465f * 0.3f, redlineTorqueNm: 465f * 0.6f,
                dragCoefficient: 0.30f, frontalAreaM2: 2.0f, rollingResistanceCoefficient: 0.015f, launchRpm: 3000f,
                finalDriveRatio: 3.97f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(325, 30, 21), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.456f, maxSteerDeg: 35f, widthM: 1.852f, lengthM: 4.572f,
                powerFactor: 0.64f, gearRatios: Gt3RsRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto7Upshifts, upshiftRpmFraction: 0.90f)),

            new OfficialVehicleSpec(
                CarType.Vehicle3, "Fiat 500",
                hasWipers: 1, surfaceTractionFactor: 0.035f, deceleration: 0.30f, topSpeed: 122.0f,
                idleFreq: 6000, topFreq: 25000, shiftFreq: 19000, gears: 5, steering: 1.50f, steeringFactor: 72,
                idleRpm: 750f, maxRpm: 6000f, revLimiter: 5500f, autoShiftRpm: 5500f * 0.92f, engineBraking: 0.40f,
                massKg: 865f, drivetrainEfficiency: 0.88f, engineBrakingTorqueNm: 102f, tireGripCoefficient: 0.88f,
                peakTorqueNm: 102f, peakTorqueRpm: 3000f, idleTorqueNm: 102f * 0.3f, redlineTorqueNm: 102f * 0.6f,
                dragCoefficient: 0.30f, frontalAreaM2: 2.1f, rollingResistanceCoefficient: 0.014f, launchRpm: 1800f,
                finalDriveRatio: 3.353f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(195, 45, 16), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.300f, maxSteerDeg: 35f, widthM: 1.627f, lengthM: 3.546f,
                powerFactor: 0.78f, gearRatios: Fiat500Ratios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(4, true, upshiftRpmFraction: 0.86f)),

            new OfficialVehicleSpec(
                CarType.Vehicle4, "Mini Cooper S",
                hasWipers: 1, surfaceTractionFactor: 0.045f, deceleration: 0.35f, topSpeed: 192.0f,
                idleFreq: 6000, topFreq: 27000, shiftFreq: 20000, gears: 6, steering: 1.40f, steeringFactor: 56,
                idleRpm: 800f, maxRpm: 6500f, revLimiter: 6000f, autoShiftRpm: 6000f * 0.92f, engineBraking: 0.32f,
                massKg: 1265f, drivetrainEfficiency: 0.88f, engineBrakingTorqueNm: 280f, tireGripCoefficient: 0.95f,
                peakTorqueNm: 280f, peakTorqueRpm: 1250f, idleTorqueNm: 280f * 0.3f, redlineTorqueNm: 280f * 0.6f,
                dragCoefficient: 0.31f, frontalAreaM2: 2.1f, rollingResistanceCoefficient: 0.014f, launchRpm: 2200f,
                finalDriveRatio: 3.59f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(195, 55, 16), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.494f, maxSteerDeg: 35f, widthM: 1.744f, lengthM: 3.876f,
                powerFactor: 0.80f, gearRatios: MiniCooperSRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto6Upshifts, upshiftRpmFraction: 0.88f)),

            new OfficialVehicleSpec(
                CarType.Vehicle5, "Ford Mustang 1969",
                hasWipers: 1, surfaceTractionFactor: 0.04f, deceleration: 0.35f, topSpeed: 186.0f,
                idleFreq: 6000, topFreq: 33000, shiftFreq: 27500, gears: 4, steering: 2.30f, steeringFactor: 80,
                idleRpm: 650f, maxRpm: 5500f, revLimiter: 5000f, autoShiftRpm: 5000f * 0.92f, engineBraking: 0.35f,
                massKg: 1440f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 481f, tireGripCoefficient: 0.90f,
                peakTorqueNm: 481f, peakTorqueRpm: 3000f, idleTorqueNm: 481f * 0.3f, redlineTorqueNm: 481f * 0.6f,
                dragCoefficient: 0.40f, frontalAreaM2: 2.5f, rollingResistanceCoefficient: 0.017f, launchRpm: 2000f,
                finalDriveRatio: 3.25f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(215, 70, 14), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.743f, maxSteerDeg: 35f, widthM: 1.811f, lengthM: 4.760f,
                powerFactor: 0.78f, gearRatios: Mustang69Ratios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(4, false, upshiftRpmFraction: 0.88f)),

            new OfficialVehicleSpec(
                CarType.Vehicle6, "Toyota Camry",
                hasWipers: 1, surfaceTractionFactor: 0.035f, deceleration: 0.30f, topSpeed: 170.0f,
                idleFreq: 7025, topFreq: 40000, shiftFreq: 32500, gears: 8, steering: 2.20f, steeringFactor: 95,
                idleRpm: 700f, maxRpm: 5600f, revLimiter: 5000f, autoShiftRpm: 4600f, engineBraking: 0.38f,
                massKg: 1470f, drivetrainEfficiency: 0.88f, engineBrakingTorqueNm: 250f, tireGripCoefficient: 0.90f,
                peakTorqueNm: 250f, peakTorqueRpm: 3800f, idleTorqueNm: 250f * 0.4f, redlineTorqueNm: 250f * 0.90f,
                dragCoefficient: 0.27f, frontalAreaM2: 2.2f, rollingResistanceCoefficient: 0.014f, launchRpm: 2000f,
                finalDriveRatio: 3.20f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(215, 55, 17), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.825f, maxSteerDeg: 35f, widthM: 1.839f, lengthM: 4.879f,
                powerFactor: 0.64f, gearRatios: CamryRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(6, true, Auto8Upshifts, upshiftRpmFraction: 0.88f, minUpshiftNetAccelerationMps2: -0.15f)),

            new OfficialVehicleSpec(
                CarType.Vehicle7, "Lamborghini Aventador",
                hasWipers: 1, surfaceTractionFactor: 0.08f, deceleration: 0.80f, topSpeed: 282.0f,
                idleFreq: 6000, topFreq: 26000, shiftFreq: 21000, gears: 7, steering: 1.90f, steeringFactor: 65,
                idleRpm: 1000f, maxRpm: 8500f, revLimiter: 8000f, autoShiftRpm: 8000f * 0.92f, engineBraking: 0.20f,
                massKg: 1640f, drivetrainEfficiency: 0.80f, engineBrakingTorqueNm: 720f, tireGripCoefficient: 1.05f,
                peakTorqueNm: 720f, peakTorqueRpm: 6200f, idleTorqueNm: 720f * 0.22f, redlineTorqueNm: 720f * 0.58f,
                dragCoefficient: 0.30f, frontalAreaM2: 2.0f, rollingResistanceCoefficient: 0.015f, launchRpm: 2400f,
                finalDriveRatio: 2.86f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(355, 25, 21), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.700f, maxSteerDeg: 25f, widthM: 2.030f, lengthM: 4.780f,
                powerFactor: 0.56f, gearRatios: AventadorRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto7Upshifts, upshiftRpmFraction: 0.90f)),

            new OfficialVehicleSpec(
                CarType.Vehicle8, "BMW 3 Series",
                hasWipers: 1, surfaceTractionFactor: 0.045f, deceleration: 0.40f, topSpeed: 222.0f,
                idleFreq: 10000, topFreq: 45000, shiftFreq: 34000, gears: 8, steering: 2.00f, steeringFactor: 70,
                idleRpm: 750f, maxRpm: 6500f, revLimiter: 6000f, autoShiftRpm: 6000f * 0.92f, engineBraking: 0.30f,
                massKg: 1524f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 346f, tireGripCoefficient: 0.93f,
                peakTorqueNm: 350f, peakTorqueRpm: 1250f, idleTorqueNm: 350f * 0.3f, redlineTorqueNm: 350f * 0.6f,
                dragCoefficient: 0.27f, frontalAreaM2: 2.2f, rollingResistanceCoefficient: 0.014f, launchRpm: 2000f,
                finalDriveRatio: 3.15f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(225, 50, 17), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 2.810f, maxSteerDeg: 35f, widthM: 1.811f, lengthM: 4.624f,
                powerFactor: 0.91f, gearRatios: Bmw3SeriesRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(6, true, Auto8Upshifts, upshiftRpmFraction: 0.80f, minUpshiftNetAccelerationMps2: -0.20f)),

            new OfficialVehicleSpec(
                CarType.Vehicle9, "Mercedes Sprinter",
                hasWipers: 1, surfaceTractionFactor: 0.02f, deceleration: 0.20f, topSpeed: 138.0f,
                idleFreq: 22050, topFreq: 30550, shiftFreq: 22550, gears: 7, steering: 1.50f, steeringFactor: 85,
                idleRpm: 600f, maxRpm: 4500f, revLimiter: 4000f, autoShiftRpm: 4000f * 0.92f, engineBraking: 0.45f,
                massKg: 1970f, drivetrainEfficiency: 0.85f, engineBrakingTorqueNm: 380f, tireGripCoefficient: 0.82f,
                peakTorqueNm: 440f, peakTorqueRpm: 1400f, idleTorqueNm: 440f * 0.3f, redlineTorqueNm: 440f * 0.6f,
                dragCoefficient: 0.34f, frontalAreaM2: 2.9f, rollingResistanceCoefficient: 0.018f, launchRpm: 1800f,
                finalDriveRatio: 3.923f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(245, 75, 16), lateralGripCoefficient: 1.0f, highSpeedStability: 0f,
                wheelbaseM: 3.658f, maxSteerDeg: 35f, widthM: 2.019f, lengthM: 5.931f,
                powerFactor: 0.58f, gearRatios: SprinterRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto7Upshifts, upshiftRpmFraction: 0.72f, minUpshiftNetAccelerationMps2: -0.30f)),

            new OfficialVehicleSpec(
                CarType.Vehicle10, "Kawasaki Ninja ZX-10R",
                hasWipers: 0, surfaceTractionFactor: 0.09f, deceleration: 0.50f, topSpeed: 266.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.40f, steeringFactor: 50,
                idleRpm: 1100f, maxRpm: 14000f, revLimiter: 13500f, autoShiftRpm: 13500f * 0.92f, engineBraking: 0.28f,
                massKg: 207f, drivetrainEfficiency: 0.92f, engineBrakingTorqueNm: 114.9f, tireGripCoefficient: 1.10f,
                peakTorqueNm: 114.9f, peakTorqueRpm: 11500f, idleTorqueNm: 114.9f * 0.3f, redlineTorqueNm: 114.9f * 0.6f,
                dragCoefficient: 0.58f, frontalAreaM2: 0.6f, rollingResistanceCoefficient: 0.016f, launchRpm: 4000f,
                finalDriveRatio: 3.8562f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(190, 55, 17), lateralGripCoefficient: 0.80f, highSpeedStability: 0.25f,
                wheelbaseM: 1.450f, maxSteerDeg: 35f, widthM: 0.749f, lengthM: 2.085f,
                powerFactor: 0.70f, gearRatios: Zx10rRatios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(6, false, upshiftRpmFraction: 0.90f)),

            new OfficialVehicleSpec(
                CarType.Vehicle11, "Ducati Panigale V4",
                hasWipers: 0, surfaceTractionFactor: 0.10f, deceleration: 0.55f, topSpeed: 252.0f,
                idleFreq: 22050, topFreq: 60000, shiftFreq: 35000, gears: 6, steering: 1.30f, steeringFactor: 50,
                idleRpm: 1200f, maxRpm: 15000f, revLimiter: 14500f, autoShiftRpm: 14500f * 0.92f, engineBraking: 0.25f,
                massKg: 191f, drivetrainEfficiency: 0.92f, engineBrakingTorqueNm: 121f, tireGripCoefficient: 1.12f,
                peakTorqueNm: 121f, peakTorqueRpm: 10000f, idleTorqueNm: 121f * 0.3f, redlineTorqueNm: 121f * 0.6f,
                dragCoefficient: 0.55f, frontalAreaM2: 0.6f, rollingResistanceCoefficient: 0.016f, launchRpm: 4000f,
                finalDriveRatio: 4.6125f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(200, 60, 17), lateralGripCoefficient: 0.80f, highSpeedStability: 0.25f,
                wheelbaseM: 1.469f, maxSteerDeg: 35f, widthM: 0.806f, lengthM: 2.110f,
                powerFactor: 0.66f, gearRatios: PanigaleV4Ratios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto6Upshifts, upshiftRpmFraction: 0.90f)),

            new OfficialVehicleSpec(
                CarType.Vehicle12, "Yamaha YZF-R1",
                hasWipers: 0, surfaceTractionFactor: 0.085f, deceleration: 0.48f, topSpeed: 262.0f,
                idleFreq: 22050, topFreq: 27550, shiftFreq: 23550, gears: 6, steering: 1.50f, steeringFactor: 66,
                idleRpm: 1100f, maxRpm: 14500f, revLimiter: 14000f, autoShiftRpm: 14000f * 0.92f, engineBraking: 0.30f,
                massKg: 201f, drivetrainEfficiency: 0.92f, engineBrakingTorqueNm: 113.3f, tireGripCoefficient: 1.10f,
                peakTorqueNm: 112.4f, peakTorqueRpm: 11500f, idleTorqueNm: 112.4f * 0.3f, redlineTorqueNm: 112.4f * 0.6f,
                dragCoefficient: 0.55f, frontalAreaM2: 0.6f, rollingResistanceCoefficient: 0.016f, launchRpm: 4000f,
                finalDriveRatio: 4.1807f, reverseMaxSpeedKph: 35f, reversePowerFactor: 0.55f, reverseGearRatio: 3.2f,
                tireCircumferenceM: TireCircumferenceM(190, 55, 17), lateralGripCoefficient: 0.80f, highSpeedStability: 0.25f,
                wheelbaseM: 1.405f, maxSteerDeg: 35f, widthM: 0.690f, lengthM: 2.055f,
                powerFactor: 0.68f, gearRatios: R1Ratios, brakeStrength: 1.0f,
                transmissionPolicy: Policy(5, true, Auto6Upshifts, upshiftRpmFraction: 0.88f))
        };

        public static OfficialVehicleSpec Get(int vehicleIndex)
        {
            if (vehicleIndex < 0 || vehicleIndex >= Vehicles.Length)
                vehicleIndex = 0;
            return Vehicles[vehicleIndex];
        }

        private static TransmissionPolicy Policy(
            int intendedTopSpeedGear,
            bool allowOverdriveAboveGameTopSpeed,
            float[]? upshiftCooldownBySourceGear = null,
            float upshiftRpmFraction = 0.92f,
            float downshiftRpmFraction = 0.35f,
            float upshiftHysteresis = 0.05f,
            float baseAutoShiftCooldownSeconds = 0.15f,
            float minUpshiftNetAccelerationMps2 = -0.05f,
            float topSpeedPursuitSpeedFraction = 0.97f)
        {
            return new TransmissionPolicy(
                intendedTopSpeedGear: intendedTopSpeedGear,
                allowOverdriveAboveGameTopSpeed: allowOverdriveAboveGameTopSpeed,
                upshiftRpmFraction: upshiftRpmFraction,
                downshiftRpmFraction: downshiftRpmFraction,
                upshiftHysteresis: upshiftHysteresis,
                baseAutoShiftCooldownSeconds: baseAutoShiftCooldownSeconds,
                minUpshiftNetAccelerationMps2: minUpshiftNetAccelerationMps2,
                topSpeedPursuitSpeedFraction: topSpeedPursuitSpeedFraction,
                preferIntendedTopSpeedGearNearLimit: true,
                upshiftCooldownBySourceGear: upshiftCooldownBySourceGear);
        }

        private static float TireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var sidewallMm = widthMm * (aspectPercent / 100f);
            var diameterMm = (rimInches * 25.4f) + (2f * sidewallMm);
            return (float)(Math.PI * (diameterMm / 1000f));
        }
    }
}
