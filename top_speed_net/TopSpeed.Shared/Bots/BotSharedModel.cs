using TopSpeed.Data;
using TopSpeed.Protocol;

namespace TopSpeed.Bots
{
    public readonly struct BotVehicleProfile
    {
        public BotVehicleProfile(float topSpeedKph, float powerFactor, float deceleration)
        {
            TopSpeedKph = topSpeedKph;
            PowerFactor = powerFactor;
            Deceleration = deceleration;
        }

        public float TopSpeedKph { get; }
        public float PowerFactor { get; }
        public float Deceleration { get; }
    }

    public static class BotSharedModel
    {
        public static void GetControlInputs(
            int difficulty,
            int random,
            TrackType currentType,
            TrackType nextType,
            float relPos,
            out float throttle,
            out float steering)
        {
            throttle = 100f;
            steering = 0f;

            if (currentType == TrackType.HairpinLeft || nextType == TrackType.HairpinLeft)
            {
                switch (difficulty)
                {
                    case 0:
                        if (relPos > 0.65f) steering = -100f;
                        break;
                    case 1:
                        if (relPos > 0.55f) steering = -100f;
                        throttle = 66f;
                        break;
                    default:
                        if (relPos > 0.55f) steering = -100f;
                        throttle = 33f;
                        break;
                }
                return;
            }

            if (currentType == TrackType.HairpinRight || nextType == TrackType.HairpinRight)
            {
                switch (difficulty)
                {
                    case 0:
                        if (relPos < 0.35f) steering = 100f;
                        break;
                    case 1:
                        if (relPos < 0.45f) steering = 100f;
                        throttle = 66f;
                        break;
                    default:
                        if (relPos < 0.45f) steering = 100f;
                        throttle = 33f;
                        break;
                }
                return;
            }

            if (relPos < 0.40f)
            {
                if (relPos > 0.2f)
                {
                    steering = difficulty switch
                    {
                        0 => 100f - random / 5f,
                        1 => 100f - random / 10f,
                        _ => 100f - random / 25f
                    };
                }
                else
                {
                    switch (difficulty)
                    {
                        case 0:
                            steering = 100f - random / 10f;
                            break;
                        case 1:
                            steering = 100f - random / 20f;
                            throttle = 75f;
                            break;
                        default:
                            steering = 100f;
                            throttle = 50f;
                            break;
                    }
                }
                return;
            }

            if (relPos > 0.6f)
            {
                if (relPos < 0.8f)
                {
                    steering = difficulty switch
                    {
                        0 => -100f + random / 5f,
                        1 => -100f + random / 10f,
                        _ => -100f + random / 25f
                    };
                }
                else
                {
                    switch (difficulty)
                    {
                        case 0:
                            steering = -100f + random / 10f;
                            break;
                        case 1:
                            steering = -100f + random / 20f;
                            throttle = 75f;
                            break;
                        default:
                            steering = -100f;
                            throttle = 50f;
                            break;
                    }
                }
            }
        }

        public static float GetCurveSpeedFactor(TrackType type)
        {
            return type switch
            {
                TrackType.EasyLeft => 0.92f,
                TrackType.EasyRight => 0.92f,
                TrackType.Left => 0.82f,
                TrackType.Right => 0.82f,
                TrackType.HardLeft => 0.70f,
                TrackType.HardRight => 0.70f,
                TrackType.HairpinLeft => 0.55f,
                TrackType.HairpinRight => 0.55f,
                _ => 1.0f
            };
        }

        public static float GetSurfaceSpeedFactor(TrackSurface surface)
        {
            return surface switch
            {
                TrackSurface.Gravel => 0.90f,
                TrackSurface.Water => 0.82f,
                TrackSurface.Sand => 0.74f,
                TrackSurface.Snow => 0.78f,
                _ => 1.0f
            };
        }

        public static float GetDifficultyTargetSpeedFactor(int difficulty)
        {
            return difficulty switch
            {
                0 => 0.76f,
                2 => 0.95f,
                _ => 0.86f
            };
        }

        public static float GetDifficultyAccelFactor(int difficulty)
        {
            return difficulty switch
            {
                0 => 0.85f,
                2 => 1.05f,
                _ => 0.95f
            };
        }

        public static float GetDifficultyBrakeFactor(int difficulty)
        {
            return difficulty switch
            {
                0 => 1.10f,
                2 => 0.92f,
                _ => 1.00f
            };
        }

        public static BotVehicleProfile GetVehicleProfile(CarType car)
        {
            return car switch
            {
                CarType.Vehicle1 => new BotVehicleProfile(315f, 0.70f, 0.40f),
                CarType.Vehicle2 => new BotVehicleProfile(312f, 0.75f, 0.45f),
                CarType.Vehicle3 => new BotVehicleProfile(160f, 0.35f, 0.30f),
                CarType.Vehicle4 => new BotVehicleProfile(235f, 0.45f, 0.35f),
                CarType.Vehicle5 => new BotVehicleProfile(200f, 0.40f, 0.35f),
                CarType.Vehicle6 => new BotVehicleProfile(210f, 0.50f, 0.30f),
                CarType.Vehicle7 => new BotVehicleProfile(350f, 0.80f, 0.80f),
                CarType.Vehicle8 => new BotVehicleProfile(250f, 0.55f, 0.40f),
                CarType.Vehicle9 => new BotVehicleProfile(160f, 0.30f, 0.20f),
                CarType.Vehicle10 => new BotVehicleProfile(299f, 0.85f, 0.50f),
                CarType.Vehicle11 => new BotVehicleProfile(310f, 0.90f, 0.55f),
                CarType.Vehicle12 => new BotVehicleProfile(299f, 0.80f, 0.48f),
                _ => new BotVehicleProfile(220f, 0.50f, 0.35f)
            };
        }
    }
}
