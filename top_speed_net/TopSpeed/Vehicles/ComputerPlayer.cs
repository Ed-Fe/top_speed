using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TopSpeed.Bots;
using TopSpeed.Tracks;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal sealed class ComputerPlayer : IDisposable
    {
        private const float CallLength = 30.0f;
        private const float BaseLateralSpeed = 7.0f;
        private const float StabilitySpeedRef = 45.0f;
        private const float AutoShiftHysteresis = 0.05f;
        private const float AutoShiftCooldownSeconds = 0.15f;
        private const float AudioLateralBoost = 1.0f;

        private readonly AudioManager _audio;
        private readonly Track _track;
        private readonly RaceSettings _settings;
        private readonly Func<float> _currentTime;
        private readonly Func<bool> _started;
        private readonly Action<string>? _debugSpeak;
        private readonly int _playerNumber;
        private readonly int _vehicleIndex;

        private readonly List<BotEvent> _events;
        private readonly string _legacyRoot;

        private ComputerState _state;
        private TrackSurface _surface;
        private int _gear;
        private float _speed;
        private float _positionX;
        private float _positionY;
        private int _switchingGear;
        private float _autoShiftCooldown;
        private float _trackLength;

        private float _surfaceTractionFactor;
        private float _deceleration;
        private float _topSpeed;
        private float _massKg;
        private float _drivetrainEfficiency;
        private float _engineBrakingTorqueNm;
        private float _tireGripCoefficient;
        private float _brakeStrength;
        private float _wheelRadiusM;
        private float _engineBraking;
        private float _idleRpm;
        private float _revLimiter;
        private float _finalDriveRatio;
        private float _powerFactor;
        private float _peakTorqueNm;
        private float _peakTorqueRpm;
        private float _idleTorqueNm;
        private float _redlineTorqueNm;
        private float _dragCoefficient;
        private float _frontalAreaM2;
        private float _rollingResistanceCoefficient;
        private float _launchRpm;
        private float _lateralGripCoefficient;
        private float _highSpeedStability;
        private float _wheelbaseM;
        private float _maxSteerDeg;
        private float _widthM;
        private float _lengthM;
        private int _idleFreq;
        private int _topFreq;
        private int _shiftFreq;
        private int _gears;
        private float _steering;
        private int _steeringFactor;

        private int _random;
        private int _prevFrequency;
        private int _frequency;
        private int _prevBrakeFrequency;
        private int _brakeFrequency;
        private float _laneWidth;
        private float _relPos;
        private float _nextRelPos;
        private float _diffX;
        private float _diffY;
        private int _currentSteering;
        private int _currentThrottle;
        private int _currentBrake;
        private float _speedDiff;
        private int _difficulty;
        private bool _finished;
        private bool _horning;
        private bool _backfirePlayedAuto;
        private bool _networkBackfireActive;
        private bool _remoteEngineStartPending;
        private float _remoteEngineStartRemaining;
        private int _remoteEnginePendingFrequency;
        private bool _crashLateralAnchored;
        private float _crashLateralFromCenter;
        private int _frame;
        private Vector3 _lastAudioPosition;
        private bool _audioInitialized;
        private float _lastAudioUpdateTime;
        private readonly VehicleRadioController _radio;
        private bool _radioLoaded;
        private bool _radioPlaying;
        private uint _radioMediaId;

        private AudioSourceHandle _soundEngine;
        private AudioSourceHandle _soundHorn;
        private AudioSourceHandle _soundStart;
        private AudioSourceHandle _soundCrash;
        private AudioSourceHandle _soundBrake;
        private AudioSourceHandle _soundMiniCrash;
        private AudioSourceHandle _soundBump;
        private AudioSourceHandle? _soundBackfire;

        private EngineModel _engine;
        private readonly BotPhysicsConfig _physicsConfig;

        public ComputerPlayer(
            AudioManager audio,
            Track track,
            RaceSettings settings,
            int vehicleIndex,
            int playerNumber,
            Func<float> currentTime,
            Func<bool> started,
            Action<string>? debugSpeak = null)
        {
            _audio = audio;
            _track = track;
            _settings = settings;
            _playerNumber = playerNumber;
            _vehicleIndex = vehicleIndex;
            _currentTime = currentTime;
            _started = started;
            _debugSpeak = debugSpeak;
            _events = new List<BotEvent>();
            _legacyRoot = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _radio = new VehicleRadioController(audio);

            _surface = TrackSurface.Asphalt;
            _gear = 1;
            _state = ComputerState.Stopped;
            _switchingGear = 0;
            _horning = false;
            _difficulty = (int)settings.Difficulty;
            _prevFrequency = 0;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _laneWidth = 0;
            _relPos = 0f;
            _nextRelPos = 0f;
            _diffX = 0;
            _diffY = 0;
            _currentSteering = 0;
            _currentThrottle = 0;
            _currentBrake = 0;
            _speedDiff = 0;
            _speed = 0;
            _frame = 1;
            _finished = false;
            _random = Algorithm.RandomInt(100);
            _networkBackfireActive = false;
            _remoteEngineStartPending = false;
            _remoteEngineStartRemaining = 0f;
            _remoteEnginePendingFrequency = _idleFreq;
            _crashLateralAnchored = false;
            _crashLateralFromCenter = 0f;
            _radioLoaded = false;
            _radioPlaying = false;
            _radioMediaId = 0;

            var definition = VehicleLoader.LoadOfficial(vehicleIndex, track.Weather);
            _surfaceTractionFactor = definition.SurfaceTractionFactor;
            _deceleration = definition.Deceleration;
            _topSpeed = definition.TopSpeed;
            _massKg = Math.Max(1f, definition.MassKg);
            _drivetrainEfficiency = Math.Max(0.1f, Math.Min(1.0f, definition.DrivetrainEfficiency));
            _engineBrakingTorqueNm = Math.Max(0f, definition.EngineBrakingTorqueNm);
            _tireGripCoefficient = Math.Max(0.1f, definition.TireGripCoefficient);
            _brakeStrength = Math.Max(0.1f, definition.BrakeStrength);
            _wheelRadiusM = Math.Max(0.01f, definition.TireCircumferenceM / (2.0f * (float)Math.PI));
            _engineBraking = Math.Max(0.05f, Math.Min(1.0f, definition.EngineBraking));
            _idleRpm = definition.IdleRpm;
            _revLimiter = definition.RevLimiter;
            _finalDriveRatio = definition.FinalDriveRatio;
            _powerFactor = Math.Max(0.1f, definition.PowerFactor);
            _peakTorqueNm = Math.Max(0f, definition.PeakTorqueNm);
            _peakTorqueRpm = Math.Max(_idleRpm + 100f, definition.PeakTorqueRpm);
            _idleTorqueNm = Math.Max(0f, definition.IdleTorqueNm);
            _redlineTorqueNm = Math.Max(0f, definition.RedlineTorqueNm);
            _dragCoefficient = Math.Max(0.01f, definition.DragCoefficient);
            _frontalAreaM2 = Math.Max(0.1f, definition.FrontalAreaM2);
            _rollingResistanceCoefficient = Math.Max(0.001f, definition.RollingResistanceCoefficient);
            _launchRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, definition.LaunchRpm));
            _lateralGripCoefficient = Math.Max(0.1f, definition.LateralGripCoefficient);
            _highSpeedStability = Math.Max(0f, Math.Min(1.0f, definition.HighSpeedStability));
            _wheelbaseM = Math.Max(0.5f, definition.WheelbaseM);
            _maxSteerDeg = Math.Max(5f, Math.Min(60f, definition.MaxSteerDeg));
            _widthM = Math.Max(0.5f, definition.WidthM);
            _lengthM = Math.Max(0.5f, definition.LengthM);
            _idleFreq = definition.IdleFreq;
            _topFreq = definition.TopFreq;
            _shiftFreq = definition.ShiftFreq;
            _gears = definition.Gears;
            _steering = definition.Steering;
            _steeringFactor = definition.SteeringFactor;
            _frequency = _idleFreq;

            _engine = new EngineModel(
                definition.IdleRpm,
                definition.MaxRpm,
                definition.RevLimiter,
                definition.AutoShiftRpm,
                definition.EngineBraking,
                definition.TopSpeed,
                definition.FinalDriveRatio,
                definition.TireCircumferenceM,
                definition.Gears,
                definition.GearRatios);

            _physicsConfig = new BotPhysicsConfig(
                _surfaceTractionFactor,
                _deceleration,
                _topSpeed,
                _massKg,
                _drivetrainEfficiency,
                _engineBrakingTorqueNm,
                _tireGripCoefficient,
                _brakeStrength,
                _wheelRadiusM,
                _engineBraking,
                _idleRpm,
                _revLimiter,
                _finalDriveRatio,
                _powerFactor,
                _peakTorqueNm,
                _peakTorqueRpm,
                _idleTorqueNm,
                _redlineTorqueNm,
                _dragCoefficient,
                _frontalAreaM2,
                _rollingResistanceCoefficient,
                _launchRpm,
                _lateralGripCoefficient,
                _highSpeedStability,
                _wheelbaseM,
                _maxSteerDeg,
                _steering,
                _gears,
                definition.GearRatios);

            _soundEngine = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Engine), "engine", looped: true);
            _soundStart = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Start), "start");
            _soundHorn = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Horn), "horn", looped: true);
            _soundCrash = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Crash), "crash");
            _soundBrake = CreateRequiredSound(definition.GetSoundPath(VehicleAction.Brake), "brake", looped: true);
            _soundEngine.SetDopplerFactor(1f);
            _soundHorn.SetDopplerFactor(0f);
            _soundBrake.SetDopplerFactor(0f);
            _soundMiniCrash = CreateRequiredSound(Path.Combine(_legacyRoot, "crashshort.wav"), "mini crash");
            _soundBump = CreateRequiredSound(Path.Combine(_legacyRoot, "bump.wav"), "bump");
            _soundCrash.SetDopplerFactor(0f);
            _soundMiniCrash.SetDopplerFactor(0f);
            _soundBump.SetDopplerFactor(0f);
            _soundBackfire = TryCreateSound(definition.GetSoundPath(VehicleAction.Backfire));
        }

        public ComputerState State => _state;
        public float PositionX => _positionX;
        public float PositionY => _positionY;
        public float Speed => _speed;
        public int PlayerNumber => _playerNumber;
        public int VehicleIndex => _vehicleIndex;
        public bool Finished => _finished;
        public void SetFinished(bool value) => _finished = value;
        public float WidthM => _widthM;
        public float LengthM => _lengthM;

        public void Initialize(float positionX, float positionY, float trackLength)
        {
            _positionX = positionX;
            _positionY = Math.Max(0f, positionY);
            _trackLength = trackLength;
            _laneWidth = _track.LaneWidth;
            _audioInitialized = false;
            _lastAudioPosition = new Vector3(positionX, 0f, _positionY);
            _lastAudioUpdateTime = 0f;
        }

        public void FinalizePlayer()
        {
            _soundEngine.Stop();
            _radio.PauseForGame();
        }

        public void PendingStart(float baseDelay)
        {
            float difficultyDelay;
            var randomValue = Algorithm.RandomInt(100) / 100f;

            switch (_difficulty)
            {
                case 2: // Hard
                    difficultyDelay = 0.1f + (randomValue * 0.4f);
                    break;
                case 1: // Normal
                    difficultyDelay = 1.0f + (randomValue * 1.5f);
                    break;
                case 0: // Easy
                default:
                    difficultyDelay = 2.5f + (randomValue * 2.5f);
                    break;
            }

            var startTime = baseDelay + difficultyDelay;
            PushEvent(BotEventType.CarComputerStart, startTime);
        }

        public void Start()
        {
            var delay = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
            PushEvent(BotEventType.CarStart, delay);
            _soundStart.Play(loop: false);
            _speed = 0;
            _prevFrequency = _idleFreq;
            _frequency = _idleFreq;
            _prevBrakeFrequency = 0;
            _brakeFrequency = 0;
            _switchingGear = 0;
            _state = ComputerState.Starting;
        }

        public void Crash(float newPosition, bool scheduleRestart = true)
        {
            var crashRoad = _track.RoadComputer(_positionY);
            var crashRoadCenterX = (crashRoad.Left + crashRoad.Right) * 0.5f;
            _crashLateralFromCenter = _positionX - crashRoadCenterX;
            _crashLateralAnchored = true;

            _speed = 0;
            _soundCrash.Play(loop: false);
            _soundEngine.Stop();
            _soundEngine.SeekToStart();
            _soundEngine.SetPanPercent(0);
            _soundBrake.Stop();
            _soundBrake.SeekToStart();
            _soundHorn.Stop();
            _gear = 1;
            _positionX = newPosition;
            _state = ComputerState.Crashing;
            if (scheduleRestart)
                PushEvent(BotEventType.CarRestart, _soundCrash.GetLengthSeconds() + 1.25f);
        }

        public void MiniCrash(float newPosition)
        {
            _speed /= 4;
            _positionX = newPosition;
            _soundMiniCrash.Play(loop: false);
        }

        public void Bump(float bumpX, float bumpY, float bumpSpeed)
        {
            if (bumpY != 0)
            {
                _speed -= bumpSpeed;
                _positionY += bumpY;
                if (_positionY < 0f)
                    _positionY = 0f;
            }

            if (bumpX > 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
            }
            else if (bumpX < 0)
            {
                _positionX += 2 * bumpX;
                _speed -= _speed / 5;
            }

            if (_speed < 0)
                _speed = 0;
            _soundBump.Play(loop: false);
            Horn();
        }

        public void Stop()
        {
            _state = ComputerState.Stopping;
        }

        public void Quiet()
        {
            _soundBrake.Stop();
            _soundHorn.Stop();
            _soundEngine.SetVolumePercent(80);
            if (_soundBackfire != null)
                _soundBackfire.SetVolumePercent(80);
        }

        public void Run(float elapsed, float playerX, float playerY)
        {
            if (_positionY < 0f)
                _positionY = 0f;

            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;
            _diffY = ((_diffY % _trackLength) + _trackLength) % _trackLength;
            if (_diffY > _trackLength / 2)
                _diffY = (_diffY - _trackLength) % _trackLength;

            if (!_horning && _diffY < -100.0f)
            {
                if (Algorithm.RandomInt(2500) == 1)
                {
                    var duration = Algorithm.RandomInt(80);
                    _horning = true;
                    PushEvent(BotEventType.StopHorn, 0.2f + (duration / 80.0f));
                }
            }

            if (_state == ComputerState.Running && _started())
            {
                AI();
                if (_currentBrake != 0 && _surface == TrackSurface.Asphalt)
                {
                    if (!_soundBrake.IsPlaying)
                        _soundBrake.Play(loop: true);
                }
                else if (_soundBrake.IsPlaying)
                {
                    _soundBrake.Stop();
                }

                var beforeSpeed = _speed;
                var physicsState = new BotPhysicsState
                {
                    PositionX = _positionX,
                    PositionY = _positionY,
                    SpeedKph = _speed,
                    Gear = _gear,
                    AutoShiftCooldownSeconds = _autoShiftCooldown
                };
                var physicsInput = new BotPhysicsInput(elapsed, _surface, _currentThrottle, _currentBrake, _currentSteering);
                BotPhysics.Step(_physicsConfig, ref physicsState, physicsInput);

                _positionX = physicsState.PositionX;
                _positionY = physicsState.PositionY;
                _speed = physicsState.SpeedKph;
                _gear = physicsState.Gear;
                _autoShiftCooldown = physicsState.AutoShiftCooldownSeconds;
                _speedDiff = _speed - beforeSpeed;

                _engine.SyncFromSpeed(_speed, _gear, elapsed, _currentThrottle);

                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    _brakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                    if (_brakeFrequency != _prevBrakeFrequency)
                    {
                        _soundBrake.SetFrequency(_brakeFrequency);
                        _prevBrakeFrequency = _brakeFrequency;
                    }
                    UpdateEngineFreq();
                }

                var road = _track.RoadComputer(_positionY);
                if (!_finished)
                    Evaluate(road);
            }
            else if (_state == ComputerState.Stopping)
            {
                _speed -= (elapsed * 100 * _deceleration);
                if (_speed < 0)
                    _speed = 0;
                if (_frame % 4 == 0)
                {
                    _frame = 0;
                    UpdateEngineFreq();
                }
                _frame++;
            }

            if (_horning && _state == ComputerState.Running)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else
            {
                if (_soundHorn.IsPlaying)
                    _soundHorn.Stop();
            }

            if (_crashLateralAnchored && !_soundCrash.IsPlaying)
                _crashLateralAnchored = false;

            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time < _currentTime())
                {
                    _events.RemoveAt(i);
                    switch (e.Type)
                    {
                        case BotEventType.CarStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} engine start.");
                            _soundEngine.SetFrequency(_idleFreq);
                            _soundEngine.Play(loop: true);
                            _state = ComputerState.Running;
                            break;
                        case BotEventType.CarComputerStart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarComputerStart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} start trigger.");
                            Start();
                            break;
                        case BotEventType.CarRestart:
                            if (!_started())
                            {
                                PushEvent(BotEventType.CarRestart, 0.25f);
                                break;
                            }
                            _debugSpeak?.Invoke($"Debug: bot {_playerNumber + 1} restart trigger.");
                            Start();
                            break;
                        case BotEventType.InGear:
                            _switchingGear = 0;
                            break;
                        case BotEventType.StopHorn:
                            _horning = false;
                            break;
                        case BotEventType.StartHorn:
                            _horning = true;
                            break;
                    }
                }
            }

            // Spatialize using the final state/position of this frame so crash/start transitions
            // are immediately aligned with the bot's current location.
            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);
        }

        public void ApplyNetworkState(
            float positionX,
            float positionY,
            float speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            bool radioLoaded,
            bool radioPlaying,
            uint radioMediaId,
            float playerX,
            float playerY,
            float trackLength)
        {
            _positionX = positionX;
            _positionY = Math.Max(0f, positionY);
            _speed = speed;
            _trackLength = trackLength;
            var preserveCrashState = _state == ComputerState.Crashing && !engineRunning;

            _diffX = _positionX - playerX;
            _diffY = _positionY - playerY;
            _diffY = ((_diffY % _trackLength) + _trackLength) % _trackLength;
            if (_diffY > _trackLength / 2)
                _diffY = (_diffY - _trackLength) % _trackLength;

            var elapsed = 0f;
            var now = _currentTime();
            if (_audioInitialized)
            {
                elapsed = now - _lastAudioUpdateTime;
                if (elapsed < 0f)
                    elapsed = 0f;
            }
            _lastAudioUpdateTime = now;
            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);

            if (engineRunning)
            {
                var targetFrequency = frequency > 0 ? frequency : _idleFreq;
                _remoteEnginePendingFrequency = targetFrequency;
                if (!_soundEngine.IsPlaying)
                {
                    if (!_remoteEngineStartPending)
                    {
                        _soundStart.Stop();
                        _soundStart.SeekToStart();
                        _soundStart.Play(loop: false);
                        _remoteEngineStartPending = true;
                        _remoteEngineStartRemaining = Math.Max(0f, _soundStart.GetLengthSeconds() - 0.1f);
                    }
                }
                if (_soundEngine.IsPlaying && _prevFrequency != targetFrequency)
                {
                    _soundEngine.SetFrequency(targetFrequency);
                    _prevFrequency = targetFrequency;
                }
            }
            else
            {
                _remoteEngineStartPending = false;
                _remoteEngineStartRemaining = 0f;
                if (_soundEngine.IsPlaying)
                    _soundEngine.Stop();
            }

            if (braking)
            {
                if (!_soundBrake.IsPlaying)
                    _soundBrake.Play(loop: true);
                var targetBrakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
                if (_prevBrakeFrequency != targetBrakeFrequency)
                {
                    _soundBrake.SetFrequency(targetBrakeFrequency);
                    _prevBrakeFrequency = targetBrakeFrequency;
                }
            }
            else if (_soundBrake.IsPlaying)
            {
                _soundBrake.Stop();
            }

            if (horning)
            {
                if (!_soundHorn.IsPlaying)
                    _soundHorn.Play(loop: true);
            }
            else if (_soundHorn.IsPlaying)
            {
                _soundHorn.Stop();
            }

            if (backfiring && !_networkBackfireActive && _soundBackfire != null)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
                _soundBackfire.Play(loop: false);
            }
            _networkBackfireActive = backfiring;
            ApplyRadioState(radioLoaded, radioPlaying, radioMediaId);

            if (preserveCrashState)
                _state = ComputerState.Crashing;
            else if (_remoteEngineStartPending)
                _state = ComputerState.Starting;
            else if (engineRunning)
                _state = ComputerState.Running;
            else
                _state = ComputerState.Stopped;
        }

        public void UpdateRemoteAudio(float playerX, float playerY, float trackLength, float elapsed)
        {
            _trackLength = trackLength;
            UpdateSpatialAudio(playerX, playerY, _trackLength, elapsed);
            if (_remoteEngineStartPending)
            {
                _remoteEngineStartRemaining -= Math.Max(0f, elapsed);
                if (_remoteEngineStartRemaining <= 0f)
                {
                    _remoteEngineStartPending = false;
                    if (!_soundEngine.IsPlaying)
                        _soundEngine.Play(loop: true);
                    if (_prevFrequency != _remoteEnginePendingFrequency)
                    {
                        _soundEngine.SetFrequency(_remoteEnginePendingFrequency);
                        _prevFrequency = _remoteEnginePendingFrequency;
                    }
                }
            }
        }

        public void ApplyRadioState(bool loaded, bool playing, uint mediaId)
        {
            _radioLoaded = loaded;
            _radioPlaying = loaded && playing;
            _radioMediaId = loaded ? mediaId : 0u;
            if (!loaded)
            {
                _radio.ClearMedia();
                return;
            }

            if (_radio.HasMedia && mediaId != 0 && _radio.MediaId != mediaId)
                _radio.ClearMedia();

            _radio.SetPlayback(_radioPlaying);
        }

        public void ApplyRadioMedia(uint mediaId, string extension, byte[] data)
        {
            if (mediaId == 0 || data == null || data.Length == 0)
                return;
            if (!_radio.TryLoadFromBytes(data, extension, mediaId, preservePlaybackState: true, out _))
                return;
            _radioLoaded = true;
            _radioMediaId = mediaId;
            _radio.SetPlayback(_radioPlaying);
        }

        public void Evaluate(Track.Road road)
        {
            if (_state == ComputerState.Running && _started())
            {
                if (_frame % 4 == 0)
                {
                    _relPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, road.Left, _laneWidth);
                    if (BotRaceRules.IsOutsideRoad(_relPos))
                    {
                        var fullCrash = BotRaceRules.IsFullCrash(_gear, _speed);
                        if (fullCrash)
                            Crash(BotRaceRules.RoadCenter(road.Left, road.Right));
                        else
                            MiniCrash(BotRaceRules.RoadCenter(road.Left, road.Right));
                    }
                }
            }

            _surface = road.Surface;
            _frame++;
        }

        public void Pause()
        {
            _radio.PauseForGame();
            if (_state == ComputerState.Starting)
                _soundStart.Stop();
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Stop();
            if (_soundBrake.IsPlaying)
                _soundBrake.Stop();
            if (_soundHorn.IsPlaying)
                _soundHorn.Stop();
            if (_soundBackfire != null && _soundBackfire.IsPlaying)
            {
                _soundBackfire.Stop();
                _soundBackfire.SeekToStart();
            }
            if (_soundCrash.IsPlaying)
            {
                _soundCrash.Stop();
                _soundCrash.SeekToStart();
            }
        }

        public void Unpause()
        {
            _radio.ResumeFromGame();
            if (_state == ComputerState.Starting)
                _soundStart.Play(loop: false);
            else if (_state == ComputerState.Running || _state == ComputerState.Stopping)
                _soundEngine.Play(loop: true);
        }

        public void Dispose()
        {
            _soundEngine.Dispose();
            _soundHorn.Dispose();
            _soundStart.Dispose();
            _soundCrash.Dispose();
            _soundBrake.Dispose();
            _soundMiniCrash.Dispose();
            _soundBump.Dispose();
            _soundBackfire?.Dispose();
            _radio.Dispose();
        }

        private void AI()
        {
            var road = _track.RoadComputer(_positionY);
            _relPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, road.Left, _laneWidth);
            var nextRoad = _track.RoadComputer(_positionY + CallLength);
            _nextRelPos = BotRaceRules.CalculateRelativeLanePosition(_positionX, nextRoad.Left, _laneWidth);
            BotSharedModel.GetControlInputs(_difficulty, _random, road.Type, nextRoad.Type, _relPos, out var throttle, out var steering);
            _currentThrottle = (int)Math.Round(throttle);
            _currentSteering = (int)Math.Round(steering);
        }

        private void Horn()
        {
            var duration = Algorithm.RandomInt(80);
            PushEvent(BotEventType.StartHorn, 0.3f);
            PushEvent(BotEventType.StopHorn, 0.5f + duration / 80.0f);
        }

        private void PushEvent(BotEventType type, float time)
        {
            _events.Add(new BotEvent { Type = type, Time = _currentTime() + time });
        }

        private void UpdateEngineFreq()
        {
            var gearForSound = _engine.GetGearForSpeedKmh(_speed);
            var gearRange = _engine.GetGearRangeKmh(gearForSound);
            var gearMin = _engine.GetGearMinSpeedKmh(gearForSound);

            if (gearForSound == 1)
            {
                var gearSpeed = gearRange <= 0f ? 0f : Math.Min(1.0f, _speed / gearRange);
                _frequency = (int)(gearSpeed * (_topFreq - _idleFreq)) + _idleFreq;
            }
            else
            {
                var gearSpeed = (_speed - gearMin) / (float)gearRange;
                if (gearSpeed < 0.07f)
                {
                    _frequency = (int)(((0.07f - gearSpeed) / 0.07f) * (_topFreq - _shiftFreq) + _shiftFreq);
                    if (_soundBackfire != null)
                    {
                        if (!_backfirePlayedAuto)
                        {
                            if (Algorithm.RandomInt(5) == 1 && !_soundBackfire.IsPlaying)
                                _soundBackfire.Play(loop: false);
                        }
                        _backfirePlayedAuto = true;
                    }
                }
                else
                {
                    _frequency = (int)(gearSpeed * (_topFreq - _shiftFreq) + _shiftFreq);
                    if (_soundBackfire != null && _backfirePlayedAuto)
                        _backfirePlayedAuto = false;
                }
            }

            if (_switchingGear != 0)
                _frequency = (_frequency + _prevFrequency * 2) / 3;

            if (_frequency != _prevFrequency)
            {
                _soundEngine.SetFrequency(_frequency);
                _prevFrequency = _frequency;
            }
        }

        private int CalculateAcceleration()
        {
            var gearRange = _engine.GetGearRangeKmh(_gear);
            var gearMin = _engine.GetGearMinSpeedKmh(_gear);
            var gearCenter = gearMin + (gearRange * 0.18f);
            _speedDiff = _speed - gearCenter;
            var relSpeedDiff = _speedDiff / (gearRange * 1.0f);
            if (Math.Abs(relSpeedDiff) < 1.9f)
            {
                var acceleration = (int)(100 * (0.5f + Math.Cos(relSpeedDiff * Math.PI * 0.5f)));
                return acceleration < 5 ? 5 : acceleration;
            }

            var minAcceleration = (int)(100 * (0.5f + Math.Cos(0.95f * Math.PI)));
            return minAcceleration < 5 ? 5 : minAcceleration;
        }

        private float CalculateDriveRpm(float speedMps, float throttle)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            var gearRatio = _engine.GetGearRatio(_gear);
            var speedBasedRpm = wheelCircumference > 0f
                ? (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio
                : 0f;
            var launchTarget = _idleRpm + (throttle * (_launchRpm - _idleRpm));
            var rpm = Math.Max(speedBasedRpm, launchTarget);
            if (rpm < _idleRpm)
                rpm = _idleRpm;
            if (rpm > _revLimiter)
                rpm = _revLimiter;
            return rpm;
        }

        private void UpdateAutomaticGear(float elapsed, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            if (_gears <= 1)
                return;

            if (_autoShiftCooldown > 0f)
            {
                _autoShiftCooldown -= elapsed;
                return;
            }

            var currentAccel = ComputeNetAccelForGear(_gear, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
            var bestGear = _gear;
            var bestAccel = currentAccel;

            if (_gear < _gears)
            {
                var upAccel = ComputeNetAccelForGear(_gear + 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
                if (upAccel > bestAccel)
                {
                    bestAccel = upAccel;
                    bestGear = _gear + 1;
                }
            }

            if (_gear > 1)
            {
                var downAccel = ComputeNetAccelForGear(_gear - 1, speedMps, throttle, surfaceTractionMod, longitudinalGripFactor);
                if (downAccel > bestAccel)
                {
                    bestAccel = downAccel;
                    bestGear = _gear - 1;
                }
            }

            var currentRpm = SpeedToRpm(speedMps, _gear);
            if (_gear < _gears && currentRpm >= _revLimiter * 0.995f)
            {
                ShiftAutomaticGear(_gear + 1);
                return;
            }

            var shiftRpm = _idleRpm + (_revLimiter - _idleRpm) * 0.35f;
            if (_gear > 1 && currentRpm < shiftRpm)
            {
                ShiftAutomaticGear(_gear - 1);
                return;
            }

            if (bestGear != _gear && bestAccel > currentAccel * (1f + AutoShiftHysteresis))
                ShiftAutomaticGear(bestGear);
        }

        private void ShiftAutomaticGear(int newGear)
        {
            if (newGear == _gear)
                return;
            _switchingGear = newGear > _gear ? 1 : -1;
            _gear = newGear;
            PushEvent(BotEventType.InGear, 0.2f);
            _autoShiftCooldown = AutoShiftCooldownSeconds;
        }

        private float ComputeNetAccelForGear(int gear, float speedMps, float throttle, float surfaceTractionMod, float longitudinalGripFactor)
        {
            var rpm = SpeedToRpm(speedMps, gear);
            if (rpm <= 0f)
                return float.NegativeInfinity;
            if (rpm > _revLimiter && gear < _gears)
                return float.NegativeInfinity;

            var engineTorque = CalculateEngineTorqueNm(rpm) * throttle * _powerFactor;
            var gearRatio = _engine.GetGearRatio(gear);
            var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;

            var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMps * speedMps;
            var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            return netForce / _massKg;
        }

        private float SpeedToRpm(float speedMps, int gear)
        {
            var wheelCircumference = _wheelRadiusM * 2.0f * (float)Math.PI;
            if (wheelCircumference <= 0f)
                return 0f;
            var gearRatio = _engine.GetGearRatio(gear);
            return (speedMps / wheelCircumference) * 60f * gearRatio * _finalDriveRatio;
        }

        private float CalculateEngineTorqueNm(float rpm)
        {
            if (_peakTorqueNm <= 0f)
                return 0f;
            var clampedRpm = Math.Max(_idleRpm, Math.Min(_revLimiter, rpm));
            if (clampedRpm <= _peakTorqueRpm)
            {
                var denom = _peakTorqueRpm - _idleRpm;
                var t = denom > 0f ? (clampedRpm - _idleRpm) / denom : 0f;
                return SmoothStep(_idleTorqueNm, _peakTorqueNm, t);
            }
            else
            {
                var denom = _revLimiter - _peakTorqueRpm;
                var t = denom > 0f ? (clampedRpm - _peakTorqueRpm) / denom : 0f;
                return SmoothStep(_peakTorqueNm, _redlineTorqueNm, t);
            }
        }

        private static float SmoothStep(float a, float b, float t)
        {
            var clamped = Math.Max(0f, Math.Min(1f, t));
            clamped = clamped * clamped * (3f - 2f * clamped);
            return a + (b - a) * clamped;
        }

        private float CalculateBrakeDecel(float brakeInput, float surfaceDecelMod)
        {
            if (brakeInput <= 0f)
                return 0f;
            var grip = Math.Max(0.1f, _tireGripCoefficient * surfaceDecelMod);
            var decelMps2 = brakeInput * _brakeStrength * grip * 9.80665f;
            return decelMps2 * 3.6f;
        }

        private float CalculateEngineBrakingDecel(float surfaceDecelMod)
        {
            if (_engineBrakingTorqueNm <= 0f || _massKg <= 0f || _wheelRadiusM <= 0f)
                return 0f;
            var rpmRange = _revLimiter - _idleRpm;
            if (rpmRange <= 0f)
                return 0f;
            var rpmFactor = (_engine.Rpm - _idleRpm) / rpmRange;
            if (rpmFactor <= 0f)
                return 0f;
            rpmFactor = Math.Max(0f, Math.Min(1f, rpmFactor));
            var gearRatio = _engine.GetGearRatio(_gear);
            var drivelineTorque = _engineBrakingTorqueNm * _engineBraking * rpmFactor;
            var wheelTorque = drivelineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var decelMps2 = (wheelForce / _massKg) * surfaceDecelMod;
            return Math.Max(0f, decelMps2 * 3.6f);
        }

        private void UpdateSpatialAudio(float listenerX, float listenerY, float trackLength, float elapsed)
        {
            // Non-local vehicle spatialization uses track-relative lateral position so left/right
            // remains stable regardless of the listener vehicle's lane position.
            var road = _track.RoadComputer(_positionY);
            var laneHalfWidth = Math.Max(0.1f, Math.Abs(road.Right - road.Left) * 0.5f);
            var roadCenterX = (road.Left + road.Right) * 0.5f;

            var lateralFromCenter = _positionX - roadCenterX;
            var dz = AudioWorld.WrapDelta(_positionY - listenerY, trackLength);
            var normalizedLateral = (lateralFromCenter / laneHalfWidth) * AudioLateralBoost;
            if (normalizedLateral < -1f)
                normalizedLateral = -1f;
            else if (normalizedLateral > 1f)
                normalizedLateral = 1f;

            // Project by lane angle, not raw x/z ratio: keeps left/right cues clear while preserving
            // true front/back distance for attenuation.
            var absDz = Math.Abs(dz);
            var radialDistance = absDz < 1f ? 1f : absDz;
            var lateralAngle = normalizedLateral * (float)(Math.PI / 2.0);
            var worldX = listenerX + (float)Math.Sin(lateralAngle) * radialDistance;
            var worldZ = listenerY + ((dz < 0f ? -1f : 1f) * (float)Math.Cos(lateralAngle) * radialDistance);

            var position = AudioWorld.Position(worldX, worldZ);
            var crashNormalizedLateral = normalizedLateral;
            if (_crashLateralAnchored)
            {
                crashNormalizedLateral = (_crashLateralFromCenter / laneHalfWidth) * AudioLateralBoost;
                if (crashNormalizedLateral < -1f)
                    crashNormalizedLateral = -1f;
                else if (crashNormalizedLateral > 1f)
                    crashNormalizedLateral = 1f;
            }
            var crashAngle = crashNormalizedLateral * (float)(Math.PI / 2.0);
            var crashWorldX = listenerX + (float)Math.Sin(crashAngle) * radialDistance;
            var crashWorldZ = listenerY + ((dz < 0f ? -1f : 1f) * (float)Math.Cos(crashAngle) * radialDistance);
            var crashPosition = AudioWorld.Position(crashWorldX, crashWorldZ);

            var velocity = Vector3.Zero;
            var velUnits = Vector3.Zero;
            if (_audioInitialized && elapsed > 0f)
            {
                velUnits = new Vector3((worldX - _lastAudioPosition.X) / elapsed, 0f, (worldZ - _lastAudioPosition.Z) / elapsed);
                velocity = AudioWorld.ToMeters(velUnits);
            }
            _lastAudioPosition = new Vector3(worldX, 0f, worldZ);
            _audioInitialized = true;

            SetSpatial(_soundEngine, position, velocity);
            SetSpatial(_soundStart, position, velocity);
            SetSpatial(_soundHorn, position, velocity);
            SetSpatial(_soundCrash, crashPosition, velocity);
            SetSpatial(_soundBrake, position, velocity);
            SetSpatial(_soundBackfire, position, velocity);
            SetSpatial(_soundBump, position, velocity);
            SetSpatial(_soundMiniCrash, position, velocity);
            _radio.UpdateSpatial(worldX, worldZ, velUnits);
        }

        private static void SetSpatial(AudioSourceHandle? sound, Vector3 position, Vector3 velocity)
        {
            if (sound == null)
                return;
            sound.SetPosition(position);
            sound.SetVelocity(velocity);
        }

        private AudioSourceHandle CreateRequiredSound(string? path, string label, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"Sound path not provided for {label}.");
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                throw new FileNotFoundException("Sound file not found.", resolved);
            return looped
                ? _audio.CreateLoopingSource(resolved, useHrtf: true)
                : _audio.CreateSource(resolved, streamFromDisk: true, useHrtf: true);
        }

        private AudioSourceHandle? TryCreateSound(string? path, bool looped = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;
            var resolved = path!.Trim();
            if (!File.Exists(resolved))
                return null;
            return looped
                ? _audio.CreateLoopingSource(resolved, useHrtf: true)
                : _audio.CreateSource(resolved, streamFromDisk: true, useHrtf: true);
        }

        private sealed class BotEvent
        {
            public float Time { get; set; }
            public BotEventType Type { get; set; }
        }

        private enum BotEventType
        {
            CarStart,
            CarComputerStart,
            CarRestart,
            InGear,
            StopHorn,
            StartHorn
        }

        internal enum ComputerState
        {
            Stopped,
            Starting,
            Running,
            Crashing,
            Stopping
        }
    }
}

