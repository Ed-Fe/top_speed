using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race.Events;
using TopSpeed.Race.Multiplayer;
using TopSpeed.Speech;
using TopSpeed.Tracks;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race
{
    internal sealed partial class LevelMultiplayer : Level
    {
        private const int MaxPlayers = ProtocolConstants.MaxPlayers;
        private const float SendIntervalSeconds = 1f / 60f;
        private const float StartLineY = 140.0f;
        private const float ServerTickRate = 125.0f;
        private const float SnapshotDelayTicks = 4.0f;
        private const int SnapshotBufferMax = 8;

        private readonly MultiplayerSession _session;
        private readonly uint _playerId;
        private readonly byte _playerNumber;
        private readonly Dictionary<byte, RemotePlayer> _remotePlayers;
        private readonly Dictionary<byte, MediaTransfer> _remoteMediaTransfers;
        private readonly List<SnapshotFrame> _snapshotFrames;
        private readonly AudioSourceHandle?[] _soundPosition;
        private readonly AudioSourceHandle?[] _soundPlayerNr;
        private readonly AudioSourceHandle?[] _soundFinished;
        private readonly bool[] _disconnectedPlayerSlots;

        private AudioSourceHandle? _soundYouAre;
        private AudioSourceHandle? _soundPlayer;
        private float _lastComment;
        private bool _infoKeyReleased;
        private int _positionFinish;
        private int _position;
        private int _positionComment;
        private bool _pauseKeyReleased = true;
        private float _sendAccumulator;
        private bool _sentStart;
        private bool _sentFinish;
        private bool _serverStopReceived;
        private PlayerState _currentState;
        private CarState _lastCarState;
        private uint _lastRaceSnapshotSequence;
        private uint _lastRaceSnapshotTick;
        private bool _hasRaceSnapshotSequence;
        private float _snapshotTickNow;
        private bool _hasSnapshotTickNow;
        private bool _sendFailureAnnounced;

        public LevelMultiplayer(
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            RaceInput input,
            TrackData trackData,
            string trackName,
            bool automaticTransmission,
            int nrOfLaps,
            int vehicle,
            string? vehicleFile,
            IVibrationDevice? vibrationDevice,
            MultiplayerSession session,
            uint playerId,
            byte playerNumber)
            : base(audio, speech, settings, input, trackName, automaticTransmission, nrOfLaps, vehicle, vehicleFile, vibrationDevice, trackData, trackData.UserDefined)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _playerId = playerId;
            _playerNumber = playerNumber;
            _remotePlayers = new Dictionary<byte, RemotePlayer>();
            _remoteMediaTransfers = new Dictionary<byte, MediaTransfer>();
            _snapshotFrames = new List<SnapshotFrame>(SnapshotBufferMax);
            _soundPosition = new AudioSourceHandle?[MaxPlayers];
            _soundPlayerNr = new AudioSourceHandle?[MaxPlayers];
            _soundFinished = new AudioSourceHandle?[MaxPlayers];
            _disconnectedPlayerSlots = new bool[MaxPlayers];
            _currentState = PlayerState.NotReady;
        }

        public void Initialize()
        {
            InitializeLevel();
            _positionFinish = 0;
            _position = _playerNumber + 1;
            _positionComment = _position;
            _lastComment = 0.0f;
            _infoKeyReleased = true;
            _sendAccumulator = 0.0f;
            _sentStart = false;
            _sentFinish = false;
            _serverStopReceived = false;
            _lastCarState = _car.State;
            _lastRaceSnapshotSequence = 0;
            _lastRaceSnapshotTick = 0;
            _hasRaceSnapshotSequence = false;
            _snapshotFrames.Clear();
            _snapshotTickNow = 0f;
            _hasSnapshotTickNow = false;
            _sendFailureAnnounced = false;
            Array.Clear(_disconnectedPlayerSlots, 0, _disconnectedPlayerSlots.Length);

            var rowSpacing = Math.Max(10.0f, _car.LengthM * 1.5f);
            var positionX = CalculateGridStartX(_playerNumber, _car.WidthM, StartLineY);
            var positionY = CalculateGridStartY(_playerNumber, rowSpacing, StartLineY);
            _car.SetPosition(positionX, positionY);

            LoadPositionSounds(
                _soundPlayerNr,
                _soundPosition,
                _soundFinished,
                MaxPlayers,
                MaxPlayers);
            LoadRaceUiSounds(out _soundYouAre, out _soundPlayer);
            SpeakRaceIntro(_soundYouAre, _soundPlayer, _playerNumber + 1);

            _currentState = PlayerState.AwaitingStart;
            TrySendRace(_session.SendPlayerState(_currentState), "awaiting-start state");
        }

        public void FinalizeLevelMultiplayer()
        {
            foreach (var remote in _remotePlayers.Values)
            {
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
            }
            _remotePlayers.Clear();
            _remoteMediaTransfers.Clear();
            _snapshotFrames.Clear();

            DisposePositionSounds(
                _soundPlayerNr,
                _soundPosition,
                _soundFinished,
                _soundPosition.Length);

            DisposeSound(_soundYouAre);
            DisposeSound(_soundPlayer);
            FinalizeLevel();
        }

        public void Run(float elapsed)
        {
            BeginFrame();

            ApplyBufferedRaceSnapshots(elapsed);
            UpdatePositions();
            RunPlayerVehicleStep(elapsed, afterTrackUpdate: () =>
            {
                var spatialTrackLength = GetSpatialTrackLength();
                foreach (var remote in _remotePlayers.Values)
                    remote.Player.UpdateRemoteAudio(_car.PositionX, _car.PositionY, spatialTrackLength, elapsed);
            });

            if (_started
                && !_sentFinish
                && _lastCarState != CarState.Crashing
                && _lastCarState != CarState.Crashed
                && (_car.State == CarState.Crashing || _car.State == CarState.Crashed))
            {
                TrySendRace(_session.SendPlayerCrashed(), "crash event");
            }
            _lastCarState = _car.State;

            HandlePlayerLapProgress(
                onPlayerFinished: () =>
                {
                    AnnounceFinishOrder(_soundPlayerNr, _soundFinished, _playerNumber, ref _positionFinish);
                    if (!_sentFinish)
                    {
                        _sentFinish = true;
                        _currentState = PlayerState.Finished;
                        TrySendRace(_session.SendPlayerFinished(), "finish event");
                        TrySendRace(_session.SendPlayerState(_currentState), "finished state");
                    }
                    PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                });

            HandleCoreRaceMetricsRequests(includeFinishedRaceTime: true);
            HandleCommentRequests(elapsed, Comment, ref _lastComment, ref _infoKeyReleased);

            HandlePlayerInfoRequests(
                MaxPlayers - 1,
                HasPlayerInRace,
                GetVehicleNameForPlayer,
                CalculatePlayerPerc);

            HandlePlayerNumberRequest(_playerNumber);
            HandleGeneralInfoRequests(ref _pauseKeyReleased);

            _sendAccumulator += elapsed;
            if (_sendAccumulator >= SendIntervalSeconds)
            {
                _sendAccumulator = 0.0f;
                var state = _currentState;
                if (_sentFinish)
                    state = PlayerState.Finished;
                else if (_started)
                    state = PlayerState.Racing;

                var raceData = new PlayerRaceData
                {
                    PositionX = _car.PositionX,
                    PositionY = _car.PositionY,
                    Speed = (ushort)_car.Speed,
                    Frequency = _car.Frequency
                };
                TrySendRace(_session.SendPlayerData(
                    raceData,
                    _car.CarType,
                    state,
                    _car.EngineRunning,
                    _car.Braking,
                    _car.Horning,
                    _car.Backfiring(),
                    LocalMediaLoaded,
                    LocalMediaPlaying,
                    LocalMediaId),
                    "player state update");
            }

            if (CompleteFrame(elapsed))
                return;
        }

        protected override void OnRaceStartEvent()
        {
            base.OnRaceStartEvent();
            if (_sentStart)
                return;

            _sentStart = true;
            _currentState = PlayerState.Racing;
            TrySendRace(_session.SendPlayerStarted(), "race start event");
            TrySendRace(_session.SendPlayerState(_currentState), "racing state");
        }

        public void Pause()
        {
            PauseCore(() =>
            {
                foreach (var remote in _remotePlayers.Values)
                    remote.Player.Pause();
            });
        }

        public void Unpause()
        {
            UnpauseCore(() =>
            {
                foreach (var remote in _remotePlayers.Values)
                    remote.Player.Unpause();
            });
        }

        protected override void OnLocalRadioMediaLoaded(uint mediaId, string mediaPath)
        {
            if (!TrySendRace(_session.SendRadioMediaStreamed(mediaId, mediaPath), "radio media"))
                SpeakText("Failed to transmit radio media.");
        }

        private bool TrySendRace(bool sent, string action)
        {
            if (sent)
                return true;

            if (_sendFailureAnnounced)
                return false;

            _sendFailureAnnounced = true;
            SpeakText($"Network send failed while sending {action}.");
            return false;
        }
    }
}
