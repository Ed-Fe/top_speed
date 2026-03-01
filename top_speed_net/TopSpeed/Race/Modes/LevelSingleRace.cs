using System;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Speech;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed partial class LevelSingleRace : Level
    {
        private const int MaxComputerPlayers = 7;
        private const int MaxPlayers = 8;
        private const float StartLineY = 140.0f;

        private readonly ComputerPlayer?[] _computerPlayers;
        private readonly AudioSourceHandle?[] _soundPosition;
        private readonly AudioSourceHandle?[] _soundPlayerNr;
        private readonly AudioSourceHandle?[] _soundFinished;

        private AudioSourceHandle? _soundYouAre;
        private AudioSourceHandle? _soundPlayer;
        private float _lastComment;
        private bool _infoKeyReleased;
        private int _positionFinish;
        private int _position;
        private int _positionComment;
        private int _playerNumber;
        private int _nComputerPlayers;
        private bool _pauseKeyReleased = true;
        private float _raceStartDelay;
        private bool _botsScheduled;

        public LevelSingleRace(
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
            : base(audio, speech, settings, input, track, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice)
        {
            _nComputerPlayers = Math.Min(settings.NrOfComputers, MaxComputerPlayers);
            _playerNumber = 1;
            _lastComment = 0.0f;
            _infoKeyReleased = true;
            _positionFinish = 0;

            _computerPlayers = new ComputerPlayer?[MaxComputerPlayers];
            _soundPosition = new AudioSourceHandle?[MaxPlayers];
            _soundPlayerNr = new AudioSourceHandle?[MaxPlayers];
            _soundFinished = new AudioSourceHandle?[MaxPlayers];
        }

        public void Initialize(int playerNumber)
        {
            InitializeLevel();
            _playerNumber = playerNumber;
            _position = playerNumber + 1;
            _positionComment = playerNumber + 1;
            _raceStartDelay = DefaultRaceStartDelaySeconds;
            _botsScheduled = false;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var botNumber = i;
                if (botNumber >= _playerNumber)
                    botNumber++;
                _computerPlayers[i] = GenerateRandomPlayer(botNumber);
            }

            var maxLength = _car.LengthM;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot != null && bot.LengthM > maxLength)
                    maxLength = bot.LengthM;
            }

            var rowSpacing = Math.Max(10.0f, maxLength * 1.5f);
            var playerX = CalculateGridStartX(_playerNumber, _car.WidthM, StartLineY);
            var playerY = CalculateGridStartY(_playerNumber, rowSpacing, StartLineY);
            _car.SetPosition(playerX, playerY);

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                var botX = CalculateGridStartX(bot.PlayerNumber, bot.WidthM, StartLineY);
                var botY = CalculateGridStartY(bot.PlayerNumber, rowSpacing, StartLineY);
                bot.Initialize(botX, botY, _track.Length);
            }

            LoadPositionSounds(
                _soundPlayerNr,
                _soundPosition,
                _soundFinished,
                _nComputerPlayers + 1,
                MaxPlayers,
                useMaxForLast: true);
            LoadRaceUiSounds(out _soundYouAre, out _soundPlayer);
            SpeakRaceIntro(_soundYouAre, _soundPlayer, _playerNumber + 1);
        }

        public void FinalizeLevelSingleRace()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                _computerPlayers[i]?.FinalizePlayer();
                _computerPlayers[i]?.Dispose();
            }

            DisposePositionSounds(
                _soundPlayerNr,
                _soundPosition,
                _soundFinished,
                _nComputerPlayers + 1);

            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            FinalizeLevel();
        }

    }
}
