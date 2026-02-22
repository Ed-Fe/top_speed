using System;
using TopSpeed.Protocol;

namespace TopSpeed.Bots
{
    public static class BotPhysicsCatalog
    {
        private static readonly float[] GtrRatios = { 4.06f, 2.30f, 1.59f, 1.25f, 1.00f, 0.80f };
        private static readonly float[] Gt3RsRatios = { 3.75f, 2.38f, 1.72f, 1.34f, 1.11f, 0.96f, 0.84f };
        private static readonly float[] Fiat500Ratios = { 3.909f, 2.238f, 1.520f, 1.156f, 0.872f };
        private static readonly float[] MiniCooperSRatios = { 3.92f, 2.14f, 1.39f, 1.09f, 0.89f, 0.76f };
        private static readonly float[] Mustang69Ratios = { 2.32f, 1.69f, 1.29f, 1.00f };
        private static readonly float[] CamryRatios = { 5.25f, 3.03f, 1.95f, 1.46f, 1.22f, 1.00f, 0.81f, 0.67f };
        private static readonly float[] AventadorRatios = { 3.91f, 2.44f, 1.81f, 1.46f, 1.19f, 0.97f, 0.89f };
        private static readonly float[] Bmw3SeriesRatios = { 4.71f, 3.14f, 2.11f, 1.67f, 1.29f, 1.00f, 0.84f, 0.67f };
        private static readonly float[] SprinterRatios = { 4.3772f, 2.8586f, 1.9206f, 1.3684f, 1.0000f, 0.8204f, 0.7276f };
        private static readonly float[] Zx10rRatios = { 2.600f, 2.222f, 1.944f, 1.722f, 1.550f, 1.391f };
        private static readonly float[] PanigaleV4Ratios = { 2.40f, 2.00f, 1.7368f, 1.5238f, 1.3636f, 1.2273f };
        private static readonly float[] R1Ratios = { 2.533f, 2.063f, 1.762f, 1.522f, 1.364f, 1.269f };

        public static BotPhysicsConfig Get(CarType car)
        {
            switch (car)
            {
                case CarType.Vehicle1:
                    return Create(0.06f, 0.40f, 315f, 1774f, 0.80f, 652f, 1.0f, 0.25f, 900f, 7600f, 3.70f, 0.70f, 652f, 3600f, 652f * 0.3f, 652f * 0.6f, 0.26f, 2.2f, 0.015f, 2500f, 1.0f, 0.0f, 2.779f, 35f, 1.60f, GtrRatios, TireCircumferenceM(285, 35, 20));
                case CarType.Vehicle2:
                    return Create(0.07f, 0.45f, 312f, 1450f, 0.85f, 465f, 1.05f, 0.22f, 950f, 8500f, 3.97f, 0.75f, 465f, 6250f, 465f * 0.3f, 465f * 0.6f, 0.33f, 2.0f, 0.015f, 3000f, 1.0f, 0.0f, 2.456f, 35f, 1.50f, Gt3RsRatios, TireCircumferenceM(325, 30, 21));
                case CarType.Vehicle3:
                    return Create(0.035f, 0.30f, 160f, 865f, 0.88f, 102f, 0.88f, 0.40f, 750f, 5500f, 3.353f, 0.35f, 102f, 3000f, 102f * 0.3f, 102f * 0.6f, 0.33f, 2.1f, 0.015f, 1800f, 1.0f, 0.0f, 2.300f, 35f, 1.50f, Fiat500Ratios, TireCircumferenceM(195, 45, 16));
                case CarType.Vehicle4:
                    return Create(0.045f, 0.35f, 235f, 1265f, 0.88f, 280f, 0.95f, 0.32f, 800f, 6000f, 3.59f, 0.45f, 280f, 1250f, 280f * 0.3f, 280f * 0.6f, 0.33f, 2.1f, 0.015f, 2200f, 1.0f, 0.0f, 2.494f, 35f, 1.40f, MiniCooperSRatios, TireCircumferenceM(195, 55, 16));
                case CarType.Vehicle5:
                    return Create(0.04f, 0.35f, 200f, 1440f, 0.85f, 481f, 0.90f, 0.35f, 650f, 5000f, 3.25f, 0.40f, 481f, 3000f, 481f * 0.3f, 481f * 0.6f, 0.45f, 2.5f, 0.018f, 2000f, 1.0f, 0.0f, 2.743f, 35f, 2.30f, Mustang69Ratios, TireCircumferenceM(215, 70, 14));
                case CarType.Vehicle6:
                    return Create(0.035f, 0.30f, 210f, 1470f, 0.88f, 250f, 0.90f, 0.38f, 700f, 5500f, 2.80f, 0.50f, 250f, 5000f, 250f * 0.3f, 250f * 0.6f, 0.29f, 2.2f, 0.015f, 2000f, 1.0f, 0.0f, 2.825f, 35f, 2.20f, CamryRatios, TireCircumferenceM(215, 55, 17));
                case CarType.Vehicle7:
                    return Create(0.08f, 0.80f, 350f, 1640f, 0.80f, 720f, 1.05f, 0.20f, 1000f, 8000f, 2.86f, 0.80f, 720f, 5500f, 720f * 0.3f, 720f * 0.6f, 0.33f, 2.0f, 0.015f, 3000f, 1.0f, 0.0f, 2.700f, 35f, 2.10f, AventadorRatios, TireCircumferenceM(355, 25, 21));
                case CarType.Vehicle8:
                    return Create(0.045f, 0.40f, 250f, 1524f, 0.85f, 346f, 0.93f, 0.30f, 750f, 6000f, 3.15f, 0.55f, 350f, 1250f, 350f * 0.3f, 350f * 0.6f, 0.29f, 2.2f, 0.015f, 2000f, 1.0f, 0.0f, 2.810f, 35f, 2.00f, Bmw3SeriesRatios, TireCircumferenceM(225, 50, 17));
                case CarType.Vehicle9:
                    return Create(0.02f, 0.20f, 160f, 1970f, 0.85f, 380f, 0.82f, 0.45f, 600f, 4000f, 3.923f, 0.30f, 440f, 1400f, 440f * 0.3f, 440f * 0.6f, 0.35f, 2.9f, 0.020f, 1800f, 1.0f, 0.0f, 3.658f, 35f, 1.50f, SprinterRatios, TireCircumferenceM(245, 75, 16));
                case CarType.Vehicle10:
                    return Create(0.09f, 0.50f, 299f, 207f, 0.92f, 114.9f, 1.10f, 0.28f, 1100f, 13500f, 3.8562f, 0.85f, 114.9f, 11500f, 114.9f * 0.3f, 114.9f * 0.6f, 0.58f, 0.6f, 0.016f, 4000f, 0.80f, 0.25f, 1.450f, 35f, 1.40f, Zx10rRatios, TireCircumferenceM(190, 55, 17));
                case CarType.Vehicle11:
                    return Create(0.10f, 0.55f, 310f, 191f, 0.92f, 121f, 1.12f, 0.25f, 1200f, 14500f, 4.6125f, 0.90f, 121f, 10000f, 121f * 0.3f, 121f * 0.6f, 0.55f, 0.6f, 0.016f, 4000f, 0.80f, 0.25f, 1.469f, 35f, 1.30f, PanigaleV4Ratios, TireCircumferenceM(200, 60, 17));
                case CarType.Vehicle12:
                    return Create(0.085f, 0.48f, 299f, 201f, 0.92f, 113.3f, 1.10f, 0.30f, 1100f, 14000f, 4.1807f, 0.80f, 112.4f, 11500f, 112.4f * 0.3f, 112.4f * 0.6f, 0.55f, 0.6f, 0.016f, 4000f, 0.80f, 0.25f, 1.405f, 35f, 1.50f, R1Ratios, TireCircumferenceM(190, 55, 17));
                default:
                    return Create(0.04f, 0.35f, 220f, 1500f, 0.85f, 220f, 0.9f, 0.30f, 800f, 6500f, 3.5f, 0.50f, 250f, 4000f, 75f, 150f, 0.30f, 2.2f, 0.015f, 2000f, 1.0f, 0.0f, 2.7f, 35f, 1.8f, null, 2.0f);
            }
        }

        private static BotPhysicsConfig Create(
            float surfaceTractionFactor,
            float deceleration,
            float topSpeedKph,
            float massKg,
            float drivetrainEfficiency,
            float engineBrakingTorqueNm,
            float tireGripCoefficient,
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
            float[]? gearRatios,
            float tireCircumferenceM)
        {
            var gears = gearRatios?.Length ?? 6;
            var wheelRadiusM = Math.Max(0.01f, tireCircumferenceM / (2.0f * (float)Math.PI));

            return new BotPhysicsConfig(
                surfaceTractionFactor,
                deceleration,
                topSpeedKph,
                massKg,
                drivetrainEfficiency,
                engineBrakingTorqueNm,
                tireGripCoefficient,
                brakeStrength: 1.0f,
                wheelRadiusM,
                engineBraking,
                idleRpm,
                revLimiter,
                finalDriveRatio,
                powerFactor,
                peakTorqueNm,
                peakTorqueRpm,
                idleTorqueNm,
                redlineTorqueNm,
                dragCoefficient,
                frontalAreaM2,
                rollingResistanceCoefficient,
                launchRpm,
                lateralGripCoefficient,
                highSpeedStability,
                wheelbaseM,
                maxSteerDeg,
                steering,
                gears,
                gearRatios);
        }

        private static float TireCircumferenceM(int widthMm, int aspectPercent, int rimInches)
        {
            var sidewallMm = widthMm * (aspectPercent / 100f);
            var diameterMm = (rimInches * 25.4f) + (2f * sidewallMm);
            return (float)(Math.PI * (diameterMm / 1000f));
        }
    }
}
