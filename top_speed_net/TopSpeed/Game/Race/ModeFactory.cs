using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Input.Devices.Vibration;
using TopSpeed.Network;
using TopSpeed.Race;
using TopSpeed.Speech;
using TopSpeed.Tracks;

namespace TopSpeed.Game
{
    internal interface IRaceModeFactory
    {
        TimeTrialMode CreateTimeTrial(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice);

        SingleRaceMode CreateSingleRace(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice);

        MultiplayerMode CreateMultiplayer(
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint playerId,
            byte playerNumber);
    }

    internal sealed class RaceModeFactory : IRaceModeFactory
    {
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly RaceSettings _settings;
        private readonly RaceInput _raceInput;

        public RaceModeFactory(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput raceInput)
        {
            _audio = audio;
            _speech = speech;
            _settings = settings;
            _raceInput = raceInput;
        }

        public TimeTrialMode CreateTimeTrial(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
        {
            return new TimeTrialMode(
                _audio,
                _speech,
                _settings,
                _raceInput,
                track,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice);
        }

        public SingleRaceMode CreateSingleRace(
            string track,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
        {
            return new SingleRaceMode(
                _audio,
                _speech,
                _settings,
                _raceInput,
                track,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice);
        }

        public MultiplayerMode CreateMultiplayer(
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int laps,
            int vehicleIndex,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint playerId,
            byte playerNumber)
        {
            return new MultiplayerMode(
                _audio,
                _speech,
                _settings,
                _raceInput,
                trackData,
                trackName,
                automaticTransmission,
                laps,
                vehicleIndex,
                vehicleFile,
                vibrationDevice,
                session,
                playerId,
                playerNumber);
        }
    }
}
