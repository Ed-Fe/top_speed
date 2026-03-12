using System;
using System.Collections.Generic;
using System.Diagnostics;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Race.Runtime;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TS.Audio;
using TopSpeed.Input.Devices.Vibration;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        protected RaceMode(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice)
            : this(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, null, userDefined: false)
        {
        }

        protected RaceMode(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            string track,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            TrackData? trackData,
            bool userDefined)
        {
            _audio = audio;
            _speech = speech;
            _settings = settings;
            _input = input;
            _vibrationDevice = vibrationDevice;
            _events = new List<RaceEvent>();
            _stopwatch = new Stopwatch();
            _soundQueue = new SoundQueue();
            _dueEvents = new List<RaceEvent>();

            _manualTransmission = !automaticTransmission;
            _nrOfLaps = nrOfLaps;
            _lap = 0;
            _speakTime = 0.0f;
            _unkeyQueue = 0;
            _highscore = 0;
            _sayTimeLength = 0.0f;

            var runtimeObjects = CreateRuntimeObjects(track, trackData, userDefined, vehicle, vehicleFile);
            _track = runtimeObjects.Track;
            _car = runtimeObjects.Car;
            _localRadio = runtimeObjects.LocalRadio;
            _radioPanel = runtimeObjects.RadioPanel;
            _panelManager = runtimeObjects.PanelManager;
            ApplyActivePanelInputAccess();
            RefreshCategoryVolumes();

            ApplyAdventureLapOverride(track);

            _soundNumbers = CreateNumberSounds();

            var raceUiSounds = CreateRaceUiSounds();
            _soundStart = raceUiSounds.Start;
            _soundBestTime = raceUiSounds.BestTime;
            _soundNewTime = raceUiSounds.NewTime;
            _soundYourTime = raceUiSounds.YourTime;
            _soundMinute = raceUiSounds.Minute;
            _soundMinutes = raceUiSounds.Minutes;
            _soundSecond = raceUiSounds.Second;
            _soundSeconds = raceUiSounds.Seconds;
            _soundPoint = raceUiSounds.Point;
            _soundPercent = raceUiSounds.Percent;

            _soundUnkey = CreateUnkeySounds();

            (_randomSounds, _totalRandomSounds) = CreateRandomSoundContainers();
            LoadDefaultRandomSounds();

            _soundLaps = CreateLapSounds();

            _soundTrackName = LoadTrackNameSound(_track.TrackName);
            _soundTurnEndDing = LoadLegacySound("ding.ogg");
        }
    }
}

