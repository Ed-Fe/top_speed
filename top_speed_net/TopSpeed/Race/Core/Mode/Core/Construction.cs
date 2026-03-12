using TopSpeed.Data;
using TopSpeed.Race.Panels;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TopSpeed.Vehicles.Core;
using TS.Audio;

namespace TopSpeed.Race
{
    internal abstract partial class RaceMode
    {
        private (Track Track, ICar Car, VehicleRadioController LocalRadio, RadioVehiclePanel RadioPanel, VehiclePanelManager PanelManager)
            CreateRuntimeObjects(
                string track,
                TrackData? trackData,
                bool userDefined,
                int vehicle,
                string? vehicleFile)
        {
            var loadedTrack = trackData == null
                ? Track.Load(track, _audio)
                : Track.LoadFromData(track, trackData, _audio, userDefined);
            var car = CarFactory.CreateDefault(
                _audio,
                loadedTrack,
                _input,
                _settings,
                vehicle,
                vehicleFile,
                () => _elapsedTotal,
                () => _started,
                _vibrationDevice);
            var localRadio = new VehicleRadioController(_audio);
            var radioPanel = new RadioVehiclePanel(
                _input,
                _audio,
                _settings,
                localRadio,
                NextLocalMediaId,
                SpeakText,
                HandleLocalRadioMediaLoaded,
                HandleLocalRadioPlaybackChanged);
            var panelManager = new VehiclePanelManager(new IVehicleRacePanel[]
            {
                new ControlVehiclePanel(),
                radioPanel
            });

            return (loadedTrack, car, localRadio, radioPanel, panelManager);
        }

        private void ApplyAdventureLapOverride(string track)
        {
            if (!string.IsNullOrWhiteSpace(track) &&
                track.IndexOf("adv", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _nrOfLaps = 1;
            }
        }

        private AudioSourceHandle[] CreateNumberSounds()
        {
            var sounds = new AudioSourceHandle[101];
            for (var i = 0; i <= 100; i++)
            {
                sounds[i] = LoadLanguageSound($"numbers\\{i}");
            }

            return sounds;
        }

        private (
            AudioSourceHandle Start,
            AudioSourceHandle BestTime,
            AudioSourceHandle NewTime,
            AudioSourceHandle YourTime,
            AudioSourceHandle Minute,
            AudioSourceHandle Minutes,
            AudioSourceHandle Second,
            AudioSourceHandle Seconds,
            AudioSourceHandle Point,
            AudioSourceHandle Percent) CreateRaceUiSounds()
        {
            return (
                LoadLanguageSound("race\\start321"),
                LoadLanguageSound("race\\time\\trackrecord"),
                LoadLanguageSound("race\\time\\newrecord"),
                LoadLanguageSound("race\\time\\yourtime"),
                LoadLanguageSound("race\\time\\minute"),
                LoadLanguageSound("race\\time\\minutes"),
                LoadLanguageSound("race\\time\\second"),
                LoadLanguageSound("race\\time\\seconds"),
                LoadLanguageSound("race\\time\\point"),
                LoadLanguageSound("race\\time\\percent"));
        }

        private AudioSourceHandle[] CreateUnkeySounds()
        {
            var sounds = new AudioSourceHandle[MaxUnkeys];
            for (var i = 0; i < MaxUnkeys; i++)
            {
                var file = $"unkey{i + 1}.wav";
                sounds[i] = LoadLegacySound(file);
            }

            return sounds;
        }

        private (AudioSourceHandle?[][] Sounds, int[] Totals) CreateRandomSoundContainers()
        {
            var sounds = new AudioSourceHandle?[RandomSoundGroups][];
            var totals = new int[RandomSoundGroups];
            for (var i = 0; i < RandomSoundGroups; i++)
                sounds[i] = new AudioSourceHandle?[RandomSoundMax];

            return (sounds, totals);
        }

        private void LoadDefaultRandomSounds()
        {
            LoadRandomSounds(RandomSound.EasyLeft, "race\\copilot\\easyleft");
            LoadRandomSounds(RandomSound.Left, "race\\copilot\\left");
            LoadRandomSounds(RandomSound.HardLeft, "race\\copilot\\hardleft");
            LoadRandomSounds(RandomSound.HairpinLeft, "race\\copilot\\hairpinleft");
            LoadRandomSounds(RandomSound.EasyRight, "race\\copilot\\easyright");
            LoadRandomSounds(RandomSound.Right, "race\\copilot\\right");
            LoadRandomSounds(RandomSound.HardRight, "race\\copilot\\hardright");
            LoadRandomSounds(RandomSound.HairpinRight, "race\\copilot\\hairpinright");
            LoadRandomSounds(RandomSound.Asphalt, "race\\copilot\\asphalt");
            LoadRandomSounds(RandomSound.Gravel, "race\\copilot\\gravel");
            LoadRandomSounds(RandomSound.Water, "race\\copilot\\water");
            LoadRandomSounds(RandomSound.Sand, "race\\copilot\\sand");
            LoadRandomSounds(RandomSound.Snow, "race\\copilot\\snow");
            LoadRandomSounds(RandomSound.Finish, "race\\info\\finish");
        }

        private AudioSourceHandle[] CreateLapSounds()
        {
            var sounds = new AudioSourceHandle[MaxLaps - 1];
            for (var i = 0; i < MaxLaps - 1; i++)
            {
                sounds[i] = LoadLanguageSound($"race\\info\\laps2go{i + 1}");
            }

            return sounds;
        }
    }
}
