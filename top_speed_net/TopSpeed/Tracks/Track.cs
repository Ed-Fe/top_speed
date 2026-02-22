using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TS.Audio;

namespace TopSpeed.Tracks
{
    internal sealed class Track : IDisposable
    {
        private const float LaneWidthMeters = 5.0f;
        private const float LegacyLaneWidthMeters = 50.0f;
        private const float CallLengthMeters = 30.0f;
        private const float MinPartLengthMeters = 50.0f;

        public struct Road
        {
            public float Left;
            public float Right;
            public TrackSurface Surface;
            public TrackType Type;
            public float Length;
        }

        private sealed class RuntimeTrackSound
        {
            private const float DefaultRandomCrossfadeSeconds = 0.75f;
            private readonly AudioManager _audio;
            private readonly string _sourceRootFullPath;
            private readonly Random _random;
            private readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> _soundDefinitions;
            private readonly Action<AudioSourceHandle, float> _enqueueFadeOut;
            private AudioSourceHandle? _handle;
            private string? _selectedPath;

            public RuntimeTrackSound(
                AudioManager audio,
                string sourceDirectory,
                Random random,
                IReadOnlyDictionary<string, TrackSoundSourceDefinition> soundDefinitions,
                Action<AudioSourceHandle, float> enqueueFadeOut,
                string id,
                TrackSoundSourceDefinition definition)
            {
                _audio = audio;
                _sourceRootFullPath = Path.GetFullPath(sourceDirectory);
                _random = random;
                _soundDefinitions = soundDefinitions;
                _enqueueFadeOut = enqueueFadeOut;
                Id = id;
                Definition = definition;
                ActiveDefinition = definition;
                LastAreaIndex = -1;
            }

            public string Id { get; }
            public TrackSoundSourceDefinition Definition { get; private set; }
            public TrackSoundSourceDefinition ActiveDefinition { get; private set; }
            public AudioSourceHandle? Handle => _handle;
            public int LastAreaIndex { get; set; }
            public bool TriggerActive { get; set; }
            public bool TriggerInitialized { get; set; }

            public void UpdateDefinition(TrackSoundSourceDefinition definition)
            {
                Definition = definition;
                ActiveDefinition = definition;
            }

            public bool EnsureCreated(bool refreshRandomVariant)
            {
                var selection = SelectVariant(refreshRandomVariant);
                if (!selection.HasValue)
                    return false;

                var activeDefinition = selection.Value.Definition;
                var path = selection.Value.Path;
                if (_handle != null && !refreshRandomVariant && string.Equals(path, _selectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    ActiveDefinition = activeDefinition;
                    return true;
                }

                var previousHandle = _handle;
                _selectedPath = path;
                ActiveDefinition = activeDefinition;

                _handle = activeDefinition.Spatial
                    ? _audio.CreateSpatialSource(path, streamFromDisk: true, allowHrtf: activeDefinition.AllowHrtf)
                    : _audio.CreateSource(path, streamFromDisk: true, useHrtf: false);
                ApplySourceSettings(_handle, activeDefinition);

                if (previousHandle != null)
                    DisposePreviousHandle(previousHandle, refreshRandomVariant);

                if (_handle != null)
                    _handle.SeekToStart();

                return true;
            }

            public void Play()
            {
                if (_handle == null)
                    return;

                if (_handle.IsPlaying)
                    return;

                if (ActiveDefinition.FadeInSeconds > 0f)
                    _handle.Play(ActiveDefinition.Loop, ActiveDefinition.FadeInSeconds);
                else
                    _handle.Play(ActiveDefinition.Loop);
            }

            public void Stop()
            {
                if (_handle == null)
                    return;

                if (ActiveDefinition.FadeOutSeconds > 0f)
                    _handle.Stop(ActiveDefinition.FadeOutSeconds);
                else
                    _handle.Stop();
            }

            public void Dispose()
            {
                DisposeHandle();
            }

            private (TrackSoundSourceDefinition Definition, string Path)? SelectVariant(bool refreshRandomVariant)
            {
                if (!refreshRandomVariant && !string.IsNullOrWhiteSpace(_selectedPath))
                    return (ActiveDefinition, _selectedPath!);

                var variants = BuildVariantCandidates();
                if (variants.Count == 0)
                    return null;

                if (Definition.Type == TrackSoundSourceType.Random && variants.Count > 1)
                {
                    var index = _random.Next(variants.Count);
                    return variants[index];
                }

                return variants[0];
            }

            private List<(TrackSoundSourceDefinition Definition, string Path)> BuildVariantCandidates()
            {
                var resolved = new List<(TrackSoundSourceDefinition Definition, string Path)>();
                if (!string.IsNullOrWhiteSpace(Definition.Path))
                {
                    var path = ResolveSoundPath(Definition.Path!);
                    if (path != null)
                        resolved.Add((Definition, path));
                }

                for (var i = 0; i < Definition.VariantPaths.Count; i++)
                {
                    var path = ResolveSoundPath(Definition.VariantPaths[i]);
                    if (path != null)
                        resolved.Add((Definition, path));
                }

                for (var i = 0; i < Definition.VariantSourceIds.Count; i++)
                {
                    var sourceId = Definition.VariantSourceIds[i];
                    if (string.IsNullOrWhiteSpace(sourceId))
                        continue;

                    if (!_soundDefinitions.TryGetValue(sourceId, out var sourceDefinition))
                        continue;

                    if (sourceDefinition.Type == TrackSoundSourceType.Random || string.IsNullOrWhiteSpace(sourceDefinition.Path))
                        continue;

                    var path = ResolveSoundPath(sourceDefinition.Path!);
                    if (path != null)
                        resolved.Add((sourceDefinition, path));
                }

                return resolved;
            }

            private static void ApplySourceSettings(AudioSourceHandle handle, TrackSoundSourceDefinition definition)
            {
                handle.SetVolume(definition.Volume);
                handle.SetPitch(definition.Pitch);
                handle.SetPan(definition.Pan);

                if (definition.MinDistance.HasValue ||
                    definition.MaxDistance.HasValue ||
                    definition.Rolloff.HasValue ||
                    definition.StartRadiusMeters.HasValue ||
                    definition.EndRadiusMeters.HasValue)
                {
                    var minDistance = definition.MinDistance ?? definition.StartRadiusMeters ?? 1.0f;
                    var maxDistance = definition.MaxDistance ?? definition.EndRadiusMeters ?? 10000f;
                    var rolloff = definition.Rolloff ?? 1.0f;
                    handle.SetDistanceModel(DistanceModel.Inverse, minDistance, maxDistance, rolloff);
                }
            }

            private string? ResolveSoundPath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                var trimmed = path.Trim();
                if (Path.IsPathRooted(trimmed))
                    return null;

                var normalized = trimmed
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);
                if (normalized.IndexOf(':') >= 0 || ContainsTraversal(normalized))
                    return null;

                var candidate = Path.GetFullPath(Path.Combine(_sourceRootFullPath, normalized));
                if (!IsInsideTrackRoot(candidate))
                    return null;

                return File.Exists(candidate) ? candidate : null;
            }

            private bool IsInsideTrackRoot(string candidate)
            {
                if (string.Equals(candidate, _sourceRootFullPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                var rootWithSeparator = _sourceRootFullPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
            }

            private static bool ContainsTraversal(string path)
            {
                var parts = path.Split(Path.DirectorySeparatorChar);
                for (var i = 0; i < parts.Length; i++)
                {
                    var segment = parts[i].Trim();
                    if (segment == "." || segment == "..")
                        return true;
                }

                return false;
            }

            private void DisposeHandle()
            {
                if (_handle == null)
                    return;
                _handle.Stop();
                _handle.Dispose();
                _handle = null;
            }

            private void DisposePreviousHandle(AudioSourceHandle previousHandle, bool refreshRandomVariant)
            {
                var fadeOutSeconds = 0f;
                if (refreshRandomVariant && Definition.Type == TrackSoundSourceType.Random)
                {
                    fadeOutSeconds = Definition.CrossfadeSeconds.HasValue
                        ? Math.Max(0f, Definition.CrossfadeSeconds.Value)
                        : DefaultRandomCrossfadeSeconds;
                }

                if (fadeOutSeconds > 0f && previousHandle.IsPlaying)
                {
                    previousHandle.Stop(fadeOutSeconds);
                    _enqueueFadeOut(previousHandle, fadeOutSeconds);
                    return;
                }

                previousHandle.Stop();
                previousHandle.Dispose();
            }
        }

        private sealed class PendingHandleStop
        {
            public PendingHandleStop(AudioSourceHandle handle, DateTime disposeAtUtc)
            {
                Handle = handle;
                DisposeAtUtc = disposeAtUtc;
            }

            public AudioSourceHandle Handle { get; }
            public DateTime DisposeAtUtc { get; }
        }

        private readonly AudioManager _audio;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private readonly TrackDefinition[] _definition;
        private readonly int _segmentCount;
        private readonly TrackWeather _weather;
        private readonly TrackAmbience _ambience;
        private readonly IReadOnlyDictionary<string, TrackRoomDefinition> _roomProfiles;
        private readonly IReadOnlyDictionary<string, TrackSoundSourceDefinition> _soundDefinitions;
        private readonly Dictionary<string, int> _segmentIndexById;
        private readonly Dictionary<string, RuntimeTrackSound> _segmentTrackSounds;
        private readonly List<RuntimeTrackSound> _allTrackSounds;
        private readonly List<PendingHandleStop> _pendingHandleStops;
        private readonly Random _random;
        private readonly string _sourceDirectory;
        private readonly float[] _segmentStartDistances;

        private float _laneWidth;
        private float _curveScale;
        private float _callLength;
        private float _lapDistance;
        private float _lapCenter;
        private int _currentRoad;
        private float _relPos;
        private float _prevRelPos;
        private int _lastCalled;
        private float _factor;
        private float _noiseLength;
        private float _noiseStartPos;
        private float _noiseEndPos;
        private bool _noisePlaying;
        private int _activeAudioSegmentIndex;
        private RoomAcoustics _activeRoomAcoustics;

        private AudioSourceHandle? _soundCrowd;
        private AudioSourceHandle? _soundOcean;
        private AudioSourceHandle? _soundRain;
        private AudioSourceHandle? _soundWind;
        private AudioSourceHandle? _soundStorm;
        private AudioSourceHandle? _soundDesert;
        private AudioSourceHandle? _soundAirport;
        private AudioSourceHandle? _soundAirplane;
        private AudioSourceHandle? _soundClock;
        private AudioSourceHandle? _soundJet;
        private AudioSourceHandle? _soundThunder;
        private AudioSourceHandle? _soundPile;
        private AudioSourceHandle? _soundConstruction;
        private AudioSourceHandle? _soundRiver;
        private AudioSourceHandle? _soundHelicopter;
        private AudioSourceHandle? _soundOwl;

        private Track(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            _trackName = trackName.Length < 64 ? trackName : string.Empty;
            _userDefined = userDefined;
            _audio = audio;
            _laneWidth = LaneWidthMeters;
            UpdateCurveScale();
            _callLength = CallLengthMeters;
            _weather = data.Weather;
            _ambience = data.Ambience;
            _definition = data.Definitions;
            _segmentCount = _definition.Length;
            _roomProfiles = data.RoomProfiles;
            _soundDefinitions = data.SoundSources;
            _segmentIndexById = BuildSegmentIndex(_definition);
            _segmentTrackSounds = new Dictionary<string, RuntimeTrackSound>(StringComparer.OrdinalIgnoreCase);
            _allTrackSounds = new List<RuntimeTrackSound>();
            _pendingHandleStops = new List<PendingHandleStop>();
            _random = new Random();
            _sourceDirectory = ResolveSourceDirectory(data.SourcePath);
            _segmentStartDistances = new float[Math.Max(0, _segmentCount)];
            _activeAudioSegmentIndex = -1;
            _activeRoomAcoustics = RoomAcoustics.Default;

            InitializeSounds();
            InitializeTrackSoundSources();
        }

        public static Track Load(string nameOrPath, AudioManager audio)
        {
            if (TrackCatalog.BuiltIn.TryGetValue(nameOrPath, out var builtIn))
            {
                return new Track(nameOrPath, builtIn, audio, userDefined: false);
            }

            var data = ReadCustomTrackData(nameOrPath);
            var displayName = ResolveCustomTrackName(nameOrPath, data.Name);
            return new Track(displayName, data, audio, userDefined: true);
        }

        public static Track LoadFromData(string trackName, TrackData data, AudioManager audio, bool userDefined)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            return new Track(trackName, data, audio, userDefined);
        }

        public string TrackName => _trackName;
        public float Length => _lapDistance;
        public int SegmentCount => _segmentCount;
        public float LapDistance => _lapDistance;
        public TrackWeather Weather => _weather;
        public TrackAmbience Ambience => _ambience;
        public bool UserDefined => _userDefined;
        public float LaneWidth => _laneWidth;
        public TrackSurface InitialSurface => _definition.Length > 0 ? _definition[0].Surface : TrackSurface.Asphalt;

        public void SetLaneWidth(float laneWidth)
        {
            _laneWidth = laneWidth;
            UpdateCurveScale();
        }

        public float LaneHalfWidthAtPosition(float position)
        {
            var laneHalfWidth = _laneWidth;
            var segmentIndex = RoadIndexAt(position);
            if (segmentIndex >= 0 && segmentIndex < _segmentCount)
            {
                var segmentWidth = _definition[segmentIndex].Width;
                if (segmentWidth > 0f)
                {
                    var segmentHalfWidth = segmentWidth * 0.5f;
                    if (segmentHalfWidth > 0f)
                        laneHalfWidth = segmentHalfWidth;
                }
            }

            return Math.Max(0.1f, laneHalfWidth);
        }

        public int Lap(float position)
        {
            if (_lapDistance <= 0)
                return 1;
            var lap = (int)Math.Floor(position / _lapDistance) + 1;
            return lap < 1 ? 1 : lap;
        }


        public void Initialize()
        {
            _lapDistance = 0;
            _lapCenter = 0;
            for (var i = 0; i < _segmentCount; i++)
            {
                _segmentStartDistances[i] = _lapDistance;
                _lapDistance += _definition[i].Length;
                switch (_definition[i].Type)
                {
                    case TrackType.EasyLeft:
                        _lapCenter -= (_definition[i].Length * _curveScale) / 2;
                        break;
                    case TrackType.Left:
                        _lapCenter -= (_definition[i].Length * _curveScale) * 2 / 3;
                        break;
                    case TrackType.HardLeft:
                        _lapCenter -= _definition[i].Length * _curveScale;
                        break;
                    case TrackType.HairpinLeft:
                        _lapCenter -= (_definition[i].Length * _curveScale) * 3 / 2;
                        break;
                    case TrackType.EasyRight:
                        _lapCenter += (_definition[i].Length * _curveScale) / 2;
                        break;
                    case TrackType.Right:
                        _lapCenter += (_definition[i].Length * _curveScale) * 2 / 3;
                        break;
                    case TrackType.HardRight:
                        _lapCenter += _definition[i].Length * _curveScale;
                        break;
                    case TrackType.HairpinRight:
                        _lapCenter += (_definition[i].Length * _curveScale) * 3 / 2;
                        break;
                }
            }

            if (_weather == TrackWeather.Rain)
                _soundRain?.Play(loop: true);
            else if (_weather == TrackWeather.Wind)
                _soundWind?.Play(loop: true);
            else if (_weather == TrackWeather.Storm)
                _soundStorm?.Play(loop: true);

            if (_ambience == TrackAmbience.Desert)
                _soundDesert?.Play(loop: true);
            else if (_ambience == TrackAmbience.Airport)
                _soundAirport?.Play(loop: true);

            if (_segmentCount > 0)
                ApplySegmentAcoustics(0);
            ActivateTrackSoundsForPosition(0f, 0);
        }

        public void FinalizeTrack()
        {
            if (_weather == TrackWeather.Rain)
                _soundRain?.Stop();
            else if (_weather == TrackWeather.Wind)
                _soundWind?.Stop();
            else if (_weather == TrackWeather.Storm)
                _soundStorm?.Stop();

            if (_ambience == TrackAmbience.Desert)
                _soundDesert?.Stop();
            else if (_ambience == TrackAmbience.Airport)
                _soundAirport?.Stop();

            for (var i = 0; i < _allTrackSounds.Count; i++)
                _allTrackSounds[i].Stop();

            DisposePendingHandleStops();

            _activeAudioSegmentIndex = -1;
            _activeRoomAcoustics = RoomAcoustics.Default;
            _audio.SetRoomAcoustics(RoomAcoustics.Default);
        }

        public void Run(float position)
        {
            UpdatePendingHandleStops();

            if (_noisePlaying && position > _noiseEndPos)
                _noisePlaying = false;

            if (_segmentCount == 0)
                return;

            var segmentIndex = RoadIndexAt(position);
            if (segmentIndex >= 0)
            {
                if (segmentIndex != _activeAudioSegmentIndex)
                    ApplySegmentAcoustics(segmentIndex);
                ActivateTrackSoundsForPosition(position, segmentIndex);
            }

            switch (_definition[_currentRoad].Noise)
            {
                case TrackNoise.Crowd:
                    UpdateLoopingNoise(_soundCrowd, position);
                    break;
                case TrackNoise.Ocean:
                    UpdateLoopingNoise(_soundOcean, position, pan: -10);
                    break;
                case TrackNoise.Runway:
                    PlayIfNotPlaying(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    UpdateLoopingNoise(_soundClock, position, pan: 25);
                    break;
                case TrackNoise.Jet:
                    PlayIfNotPlaying(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    PlayIfNotPlaying(_soundThunder);
                    break;
                case TrackNoise.Pile:
                    UpdateLoopingNoise(_soundPile, position);
                    break;
                case TrackNoise.Construction:
                    UpdateLoopingNoise(_soundConstruction, position);
                    break;
                case TrackNoise.River:
                    UpdateLoopingNoise(_soundRiver, position);
                    break;
                case TrackNoise.Helicopter:
                    PlayIfNotPlaying(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    PlayIfNotPlaying(_soundOwl);
                    break;
                default:
                    _soundCrowd?.Stop();
                    _soundOcean?.Stop();
                    _soundClock?.Stop();
                    _soundPile?.Stop();
                    _soundConstruction?.Stop();
                    _soundRiver?.Stop();
                    break;
            }
        }

        public Road RoadAtPosition(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            var lap = (int)Math.Floor(position / _lapDistance);
            var pos = WrapPosition(position);
            var dist = 0.0f;
            var center = lap * _lapCenter;

            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                {
                    _prevRelPos = _relPos;
                    _relPos = pos - dist;
                    _currentRoad = i;
                    var road = new Road
                    {
                        Type = _definition[i].Type,
                        Surface = _definition[i].Surface,
                        Length = _definition[i].Length
                    };

                    ApplyRoadOffset(ref road, center, _relPos, _definition[i].Type);
                    return road;
                }

                center = UpdateCenter(center, _definition[i]);
                dist += _definition[i].Length;
            }

            return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };
        }

        public Road RoadComputer(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            var lap = (int)Math.Floor(position / _lapDistance);
            var pos = WrapPosition(position);
            var dist = 0.0f;
            var center = lap * _lapCenter;
            var relPos = 0.0f;

            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                {
                    relPos = pos - dist;
                    var road = new Road
                    {
                        Type = _definition[i].Type,
                        Surface = _definition[i].Surface,
                        Length = _definition[i].Length
                    };

                    ApplyRoadOffset(ref road, center, relPos, _definition[i].Type);
                    return road;
                }

                center = UpdateCenter(center, _definition[i]);
                dist += _definition[i].Length;
            }

            return new Road { Left = 0, Right = 0, Surface = TrackSurface.Asphalt, Type = TrackType.Straight, Length = MinPartLengthMeters };
        }

        public bool NextRoad(float position, float speed, int curveAnnouncementMode, out Road road)
        {
            road = new Road();
            if (_segmentCount == 0)
                return false;

            if (curveAnnouncementMode == 0)
            {
                var currentLength = _definition[_currentRoad].Length;
                if ((_relPos + _callLength > currentLength) && (_prevRelPos + _callLength <= currentLength))
                {
                    var next = _definition[(_currentRoad + 1) % _segmentCount];
                    road.Type = next.Type;
                    road.Surface = next.Surface;
                    road.Length = next.Length;
                    return true;
                }
                return false;
            }

            var lookAhead = _callLength + speed / 2;
            var roadAhead = RoadIndexAt(position + lookAhead);
            if (roadAhead < 0)
                return false;

            var delta = (roadAhead - _lastCalled + _segmentCount) % _segmentCount;
            if (delta > 0 && delta <= _segmentCount / 2)
            {
                var next = _definition[roadAhead];
                road.Type = next.Type;
                road.Surface = next.Surface;
                road.Length = next.Length;
                _lastCalled = roadAhead;
                return true;
            }

            return false;
        }

        private int RoadIndexAt(float position)
        {
            if (_lapDistance == 0)
                Initialize();

            var pos = WrapPosition(position);
            var dist = 0.0f;
            for (var i = 0; i < _segmentCount; i++)
            {
                if (dist <= pos && dist + _definition[i].Length > pos)
                    return i;
                dist += _definition[i].Length;
            }
            return -1;
        }

        private void CalculateNoiseLength()
        {
            _noiseLength = 0;
            var i = _currentRoad;
            while (i < _segmentCount && _definition[i].Noise == _definition[_currentRoad].Noise)
            {
                _noiseLength += _definition[i].Length;
                i++;
            }
            _noisePlaying = true;
        }

        private void UpdateLoopingNoise(AudioSourceHandle? sound, float position, int? pan = null)
        {
            if (sound == null)
                return;

            if (!_noisePlaying)
            {
                CalculateNoiseLength();
                _noiseStartPos = position;
                _noiseEndPos = position + _noiseLength;
            }

            _factor = (position - _noiseStartPos) * 1.0f / _noiseLength;
            if (_factor < 0.5f)
                _factor *= 2.0f;
            else
                _factor = 2.0f * (1.0f - _factor);

            SetVolumePercent(sound, (int)(80.0f + _factor * 20.0f));
            if (!sound.IsPlaying)
            {
                if (pan.HasValue)
                    sound.SetPan(pan.Value / 100f);
                sound.Play(loop: true);
            }
        }

        private static void PlayIfNotPlaying(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            if (!sound.IsPlaying)
                sound.Play(loop: false);
        }

        private static void SetVolumePercent(AudioSourceHandle sound, int volume)
        {
            var clamped = Math.Max(0, Math.Min(100, volume));
            sound.SetVolume(clamped / 100f);
        }

        private void InitializeSounds()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _soundCrowd = CreateLegacySound(root, "crowd.wav");
            _soundOcean = CreateLegacySound(root, "ocean.wav");
            _soundRain = CreateLegacySound(root, "rain.wav");
            _soundWind = CreateLegacySound(root, "wind.wav");
            _soundStorm = CreateLegacySound(root, "storm.wav");
            _soundDesert = CreateLegacySound(root, "desert.wav");
            _soundAirport = CreateLegacySound(root, "airport.wav");
            _soundAirplane = CreateLegacySound(root, "airplane.wav");
            _soundClock = CreateLegacySound(root, "clock.wav");
            _soundJet = CreateLegacySound(root, "jet.wav");
            _soundThunder = CreateLegacySound(root, "thunder.wav");
            _soundPile = CreateLegacySound(root, "pile.wav");
            _soundConstruction = CreateLegacySound(root, "const.wav");
            _soundRiver = CreateLegacySound(root, "river.wav");
            _soundHelicopter = CreateLegacySound(root, "helicopter.wav");
            _soundOwl = CreateLegacySound(root, "owl.wav");
        }

        private AudioSourceHandle? CreateLegacySound(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateLoopingSource(path);
        }

        private void InitializeTrackSoundSources()
        {
            if (_soundDefinitions.Count == 0)
                return;

            foreach (var pair in _soundDefinitions)
            {
                var runtime = new RuntimeTrackSound(
                    _audio,
                    _sourceDirectory,
                    _random,
                    _soundDefinitions,
                    EnqueuePendingHandleStop,
                    pair.Key,
                    pair.Value);
                _segmentTrackSounds[pair.Key] = runtime;
                _allTrackSounds.Add(runtime);

                if (pair.Value.Global && runtime.EnsureCreated(refreshRandomVariant: false))
                    runtime.Play();
            }
        }

        private void EnqueuePendingHandleStop(AudioSourceHandle handle, float fadeOutSeconds)
        {
            if (fadeOutSeconds <= 0f)
            {
                handle.Dispose();
                return;
            }

            var disposeAt = DateTime.UtcNow.AddSeconds(fadeOutSeconds);
            _pendingHandleStops.Add(new PendingHandleStop(handle, disposeAt));
        }

        private void UpdatePendingHandleStops()
        {
            if (_pendingHandleStops.Count == 0)
                return;

            var now = DateTime.UtcNow;
            for (var i = _pendingHandleStops.Count - 1; i >= 0; i--)
            {
                if (now < _pendingHandleStops[i].DisposeAtUtc)
                    continue;

                _pendingHandleStops[i].Handle.Dispose();
                _pendingHandleStops.RemoveAt(i);
            }
        }

        private void DisposePendingHandleStops()
        {
            for (var i = 0; i < _pendingHandleStops.Count; i++)
                _pendingHandleStops[i].Handle.Dispose();
            _pendingHandleStops.Clear();
        }

        private void ActivateTrackSoundsForPosition(float position, int segmentIndex)
        {
            if (_segmentTrackSounds.Count == 0)
                return;

            foreach (var runtime in _allTrackSounds)
            {
                var shouldPlay = ShouldPlayRuntimeSound(runtime, position, segmentIndex);
                if (!shouldPlay)
                {
                    runtime.Stop();
                    continue;
                }

                var refreshRandom =
                    runtime.Definition.Type == TrackSoundSourceType.Random &&
                    runtime.Definition.RandomMode == TrackSoundRandomMode.PerArea &&
                    runtime.LastAreaIndex != segmentIndex;

                if (!runtime.EnsureCreated(refreshRandom))
                    continue;

                if (runtime.Handle != null)
                {
                    UpdateTrackSoundPlacement(runtime, position, segmentIndex);
                    runtime.Play();
                }

                runtime.LastAreaIndex = segmentIndex;
            }
        }

        private bool ShouldPlayRuntimeSound(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            var definition = runtime.Definition;
            if (definition.Global)
                return true;

            var hasStartOrEndConditions = HasStartOrEndConditions(definition);
            if (hasStartOrEndConditions)
                return UpdateTriggerState(runtime, position, segmentIndex);

            if (IsSoundAssignedToSegment(segmentIndex, runtime.Id))
                return true;

            return IsSegmentInSoundArea(segmentIndex, definition);
        }

        private bool IsSoundAssignedToSegment(int segmentIndex, string soundId)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return false;

            var segment = _definition[segmentIndex];
            for (var i = 0; i < segment.SoundSourceIds.Count; i++)
            {
                if (string.Equals(segment.SoundSourceIds[i], soundId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool HasStartOrEndConditions(TrackSoundSourceDefinition definition)
        {
            return !string.IsNullOrWhiteSpace(definition.StartAreaId) ||
                   !string.IsNullOrWhiteSpace(definition.EndAreaId) ||
                   definition.StartPosition.HasValue ||
                   definition.EndPosition.HasValue;
        }

        private bool UpdateTriggerState(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            if (!runtime.TriggerInitialized)
            {
                runtime.TriggerInitialized = true;
                runtime.TriggerActive = false;
            }

            if (!runtime.TriggerActive)
            {
                runtime.TriggerActive = IsStartConditionMet(runtime.Definition, position, segmentIndex);
            }
            else if (IsEndConditionMet(runtime.Definition, position, segmentIndex))
            {
                runtime.TriggerActive = false;
            }

            return runtime.TriggerActive;
        }

        private bool IsStartConditionMet(TrackSoundSourceDefinition definition, float position, int segmentIndex)
        {
            var hasStartCondition = !string.IsNullOrWhiteSpace(definition.StartAreaId) || definition.StartPosition.HasValue;
            if (!hasStartCondition)
                return true;

            if (IsAreaConditionMet(segmentIndex, definition.StartAreaId))
                return true;

            if (definition.StartPosition.HasValue &&
                IsPositionConditionMet(position, definition.StartPosition.Value, definition.StartRadiusMeters))
            {
                return true;
            }

            return false;
        }

        private bool IsEndConditionMet(TrackSoundSourceDefinition definition, float position, int segmentIndex)
        {
            if (IsAreaConditionMet(segmentIndex, definition.EndAreaId))
                return true;

            if (definition.EndPosition.HasValue &&
                IsPositionConditionMet(position, definition.EndPosition.Value, definition.EndRadiusMeters))
            {
                return true;
            }

            return false;
        }

        private bool IsAreaConditionMet(int segmentIndex, string? areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
                return false;
            if (!_segmentIndexById.TryGetValue(areaId!, out var areaSegment))
                return false;
            return areaSegment == segmentIndex;
        }

        private bool IsPositionConditionMet(float playerPosition, Vector3 targetPosition, float? radiusMeters)
        {
            var radius = radiusMeters ?? 1f;
            if (radius <= 0f)
                radius = 1f;

            var listenerZ = _lapDistance > 0f ? WrapPosition(playerPosition) : playerPosition;
            var dx = targetPosition.X;
            var dy = targetPosition.Y;
            var dz = _lapDistance > 0f
                ? AudioWorld.WrapDelta(targetPosition.Z - listenerZ, _lapDistance)
                : targetPosition.Z - listenerZ;

            var distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
            return distanceSquared <= (radius * radius);
        }

        private void UpdateTrackSoundPlacement(RuntimeTrackSound runtime, float position, int segmentIndex)
        {
            var handle = runtime.Handle;
            if (handle == null)
                return;

            var definition = runtime.ActiveDefinition;
            if (!definition.Spatial)
            {
                handle.SetPan(definition.Pan);
                return;
            }

            var sourcePos = ComputeTrackSoundPosition(runtime, position, segmentIndex);
            handle.SetPosition(sourcePos);
            handle.SetVelocity(Vector3.Zero);
        }

        private Vector3 ComputeTrackSoundPosition(RuntimeTrackSound runtime, float playerPosition, int segmentIndex)
        {
            var definition = runtime.ActiveDefinition;
            var lapPos = _lapDistance > 0f ? WrapPosition(playerPosition) : playerPosition;
            var segmentStart = GetSegmentStartDistance(segmentIndex);
            var segmentLength = segmentIndex >= 0 && segmentIndex < _definition.Length
                ? _definition[segmentIndex].Length
                : MinPartLengthMeters;
            var segmentCenter = segmentStart + (segmentLength * 0.5f);

            if (definition.Type == TrackSoundSourceType.Moving &&
                TryComputeMovingSoundPosition(definition, playerPosition, segmentIndex, out var movingPosition))
            {
                var wrappedZ = WrapWorldZ(movingPosition.Z, lapPos, playerPosition);
                return new Vector3(
                    AudioWorld.ToMeters(movingPosition.X),
                    AudioWorld.ToMeters(movingPosition.Y),
                    AudioWorld.ToMeters(wrappedZ));
            }

            if (definition.StartPosition.HasValue && definition.EndPosition.HasValue)
            {
                var t = ComputeAreaProgress(segmentIndex, definition);
                if (t <= 0f &&
                    definition.SpeedMetersPerSecond.HasValue &&
                    Math.Abs(definition.SpeedMetersPerSecond.Value) > 0.0001f &&
                    _lapDistance > 0f &&
                    definition.StartAreaId == null &&
                    definition.EndAreaId == null)
                {
                    var phase = (WrapPosition(playerPosition) * definition.SpeedMetersPerSecond.Value) / _lapDistance;
                    t = phase - (float)Math.Floor(phase);
                }
                var start = definition.StartPosition.Value;
                var end = definition.EndPosition.Value;
                var x = Lerp(start.X, end.X, t);
                var y = Lerp(start.Y, end.Y, t);
                var z = Lerp(start.Z, end.Z, t);
                var wrappedZ = WrapWorldZ(z, lapPos, playerPosition);
                return new Vector3(AudioWorld.ToMeters(x), AudioWorld.ToMeters(y), AudioWorld.ToMeters(wrappedZ));
            }

            if (definition.Position.HasValue)
            {
                var pos = definition.Position.Value;
                var wrappedZ = WrapWorldZ(pos.Z, lapPos, playerPosition);
                return new Vector3(AudioWorld.ToMeters(pos.X), AudioWorld.ToMeters(pos.Y), AudioWorld.ToMeters(wrappedZ));
            }

            var xDefault = 0f;
            var zDefault = WrapWorldZ(segmentCenter, lapPos, playerPosition);
            return new Vector3(AudioWorld.ToMeters(xDefault), 0f, AudioWorld.ToMeters(zDefault));
        }

        private bool TryComputeMovingSoundPosition(
            TrackSoundSourceDefinition definition,
            float playerPosition,
            int segmentIndex,
            out Vector3 position)
        {
            position = default;
            var speed = definition.SpeedMetersPerSecond ?? 0f;
            if (Math.Abs(speed) <= 0.0001f)
                return false;

            var pathLength = _lapDistance > 0f ? _lapDistance : 0f;
            var hasAreaSpan = TryResolveAreaSpan(definition, out var areaStartZ, out _, out var areaLength);
            if (hasAreaSpan)
                pathLength = areaLength;
            if (pathLength <= 0f)
                return false;

            var phase = (WrapPosition(playerPosition) * speed) / pathLength;
            phase -= (float)Math.Floor(phase);
            if (phase < 0f)
                phase += 1f;

            if (definition.StartPosition.HasValue && definition.EndPosition.HasValue)
            {
                var start = definition.StartPosition.Value;
                var end = definition.EndPosition.Value;
                position = new Vector3(
                    Lerp(start.X, end.X, phase),
                    Lerp(start.Y, end.Y, phase),
                    Lerp(start.Z, end.Z, phase));
                return true;
            }

            if (definition.Position.HasValue)
            {
                var anchor = definition.Position.Value;
                var travel = pathLength * phase;
                var z = hasAreaSpan ? (areaStartZ + travel) : (anchor.Z + travel);
                if (_lapDistance > 0f)
                    z = WrapPosition(z);

                position = new Vector3(anchor.X, anchor.Y, z);
                return true;
            }

            var fallbackZ = GetSegmentCenterDistance(segmentIndex) + (pathLength * phase);
            if (_lapDistance > 0f)
                fallbackZ = WrapPosition(fallbackZ);
            position = new Vector3(0f, 0f, fallbackZ);
            return true;
        }

        private bool TryResolveAreaSpan(TrackSoundSourceDefinition definition, out float startZ, out float endZ, out float pathLength)
        {
            startZ = 0f;
            endZ = 0f;
            pathLength = 0f;
            if (string.IsNullOrWhiteSpace(definition.StartAreaId) || string.IsNullOrWhiteSpace(definition.EndAreaId))
                return false;

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId!, out var startIndex))
                return false;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId!, out var endIndex))
                return false;

            startZ = GetSegmentCenterDistance(startIndex);
            endZ = GetSegmentCenterDistance(endIndex);

            if (_lapDistance > 0f)
            {
                pathLength = endZ - startZ;
                if (pathLength < 0f)
                    pathLength += _lapDistance;
                if (pathLength <= 0f)
                    pathLength = _definition[endIndex].Length;
            }
            else
            {
                pathLength = Math.Max(0.001f, Math.Abs(endZ - startZ));
            }

            return pathLength > 0f;
        }

        private float GetSegmentStartDistance(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return 0f;

            if (_lapDistance > 0f && segmentIndex < _segmentStartDistances.Length)
                return _segmentStartDistances[segmentIndex];

            var start = 0f;
            for (var i = 0; i < segmentIndex; i++)
                start += _definition[i].Length;
            return start;
        }

        private float GetSegmentCenterDistance(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _definition.Length)
                return 0f;
            return GetSegmentStartDistance(segmentIndex) + (_definition[segmentIndex].Length * 0.5f);
        }

        private float ComputeAreaProgress(int segmentIndex, TrackSoundSourceDefinition definition)
        {
            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return 0f;

            if (definition.StartAreaId == null || definition.EndAreaId == null)
                return 0f;

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId, out var startIndex))
                return 0f;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId, out var endIndex))
                return 0f;

            if (startIndex == endIndex)
                return 0f;

            var span = (endIndex - startIndex + _segmentCount) % _segmentCount;
            if (span == 0)
                return 0f;

            var delta = (segmentIndex - startIndex + _segmentCount) % _segmentCount;
            var t = delta / (float)span;
            if (t < 0f)
                t = 0f;
            if (t > 1f)
                t = 1f;
            return t;
        }

        private bool IsSegmentInSoundArea(int segmentIndex, TrackSoundSourceDefinition definition)
        {
            if (definition.StartAreaId == null && definition.EndAreaId == null)
                return false;

            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return false;

            if (definition.StartAreaId == null || definition.EndAreaId == null)
            {
                if (definition.StartAreaId != null && _segmentIndexById.TryGetValue(definition.StartAreaId, out var startOnly))
                    return startOnly == segmentIndex;
                if (definition.EndAreaId != null && _segmentIndexById.TryGetValue(definition.EndAreaId, out var endOnly))
                    return endOnly == segmentIndex;
                return false;
            }

            if (!_segmentIndexById.TryGetValue(definition.StartAreaId, out var start))
                return false;
            if (!_segmentIndexById.TryGetValue(definition.EndAreaId, out var end))
                return false;

            if (start <= end)
                return segmentIndex >= start && segmentIndex <= end;

            return segmentIndex >= start || segmentIndex <= end;
        }

        private void ApplySegmentAcoustics(int segmentIndex)
        {
            _activeAudioSegmentIndex = segmentIndex;
            var room = ResolveRoomAcoustics(segmentIndex);
            if (RoomEquals(_activeRoomAcoustics, room))
                return;

            _activeRoomAcoustics = room;
            _audio.SetRoomAcoustics(room);
        }

        private RoomAcoustics ResolveRoomAcoustics(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _segmentCount)
                return RoomAcoustics.Default;

            var definition = _definition[segmentIndex];
            TrackRoomDefinition? roomDefinition = null;
            if (!string.IsNullOrWhiteSpace(definition.RoomId))
            {
                if (_roomProfiles.TryGetValue(definition.RoomId!, out var room))
                    roomDefinition = room;
                else if (TrackRoomLibrary.TryGetPreset(definition.RoomId!, out var preset))
                    roomDefinition = preset;
            }

            var acoustics = roomDefinition == null
                ? RoomAcoustics.Default
                : ToRoomAcoustics(roomDefinition);

            if (definition.RoomOverrides != null)
                ApplyRoomOverrides(ref acoustics, definition.RoomOverrides);

            return acoustics;
        }

        private static RoomAcoustics ToRoomAcoustics(TrackRoomDefinition room)
        {
            return new RoomAcoustics
            {
                HasRoom = true,
                ReverbTimeSeconds = room.ReverbTimeSeconds,
                ReverbGain = room.ReverbGain,
                HfDecayRatio = room.HfDecayRatio,
                LateReverbGain = room.LateReverbGain,
                Diffusion = room.Diffusion,
                AirAbsorptionScale = room.AirAbsorption,
                OcclusionScale = room.OcclusionScale,
                TransmissionScale = room.TransmissionScale,
                OcclusionOverride = room.OcclusionOverride,
                TransmissionOverrideLow = room.TransmissionOverrideLow,
                TransmissionOverrideMid = room.TransmissionOverrideMid,
                TransmissionOverrideHigh = room.TransmissionOverrideHigh,
                AirAbsorptionOverrideLow = room.AirAbsorptionOverrideLow,
                AirAbsorptionOverrideMid = room.AirAbsorptionOverrideMid,
                AirAbsorptionOverrideHigh = room.AirAbsorptionOverrideHigh
            };
        }

        private static void ApplyRoomOverrides(ref RoomAcoustics acoustics, TrackRoomOverrides overrides)
        {
            acoustics.HasRoom = true;
            if (overrides.ReverbTimeSeconds.HasValue) acoustics.ReverbTimeSeconds = overrides.ReverbTimeSeconds.Value;
            if (overrides.ReverbGain.HasValue) acoustics.ReverbGain = overrides.ReverbGain.Value;
            if (overrides.HfDecayRatio.HasValue) acoustics.HfDecayRatio = overrides.HfDecayRatio.Value;
            if (overrides.LateReverbGain.HasValue) acoustics.LateReverbGain = overrides.LateReverbGain.Value;
            if (overrides.Diffusion.HasValue) acoustics.Diffusion = overrides.Diffusion.Value;
            if (overrides.AirAbsorption.HasValue) acoustics.AirAbsorptionScale = overrides.AirAbsorption.Value;
            if (overrides.OcclusionScale.HasValue) acoustics.OcclusionScale = overrides.OcclusionScale.Value;
            if (overrides.TransmissionScale.HasValue) acoustics.TransmissionScale = overrides.TransmissionScale.Value;
            if (overrides.OcclusionOverride.HasValue) acoustics.OcclusionOverride = overrides.OcclusionOverride.Value;
            if (overrides.TransmissionOverrideLow.HasValue) acoustics.TransmissionOverrideLow = overrides.TransmissionOverrideLow.Value;
            if (overrides.TransmissionOverrideMid.HasValue) acoustics.TransmissionOverrideMid = overrides.TransmissionOverrideMid.Value;
            if (overrides.TransmissionOverrideHigh.HasValue) acoustics.TransmissionOverrideHigh = overrides.TransmissionOverrideHigh.Value;
            if (overrides.AirAbsorptionOverrideLow.HasValue) acoustics.AirAbsorptionOverrideLow = overrides.AirAbsorptionOverrideLow.Value;
            if (overrides.AirAbsorptionOverrideMid.HasValue) acoustics.AirAbsorptionOverrideMid = overrides.AirAbsorptionOverrideMid.Value;
            if (overrides.AirAbsorptionOverrideHigh.HasValue) acoustics.AirAbsorptionOverrideHigh = overrides.AirAbsorptionOverrideHigh.Value;
        }

        private static bool RoomEquals(RoomAcoustics a, RoomAcoustics b)
        {
            return a.HasRoom == b.HasRoom &&
                   AreClose(a.ReverbTimeSeconds, b.ReverbTimeSeconds) &&
                   AreClose(a.ReverbGain, b.ReverbGain) &&
                   AreClose(a.HfDecayRatio, b.HfDecayRatio) &&
                   AreClose(a.LateReverbGain, b.LateReverbGain) &&
                   AreClose(a.Diffusion, b.Diffusion) &&
                   AreClose(a.AirAbsorptionScale, b.AirAbsorptionScale) &&
                   AreClose(a.OcclusionScale, b.OcclusionScale) &&
                   AreClose(a.TransmissionScale, b.TransmissionScale) &&
                   NullableClose(a.OcclusionOverride, b.OcclusionOverride) &&
                   NullableClose(a.TransmissionOverrideLow, b.TransmissionOverrideLow) &&
                   NullableClose(a.TransmissionOverrideMid, b.TransmissionOverrideMid) &&
                   NullableClose(a.TransmissionOverrideHigh, b.TransmissionOverrideHigh) &&
                   NullableClose(a.AirAbsorptionOverrideLow, b.AirAbsorptionOverrideLow) &&
                   NullableClose(a.AirAbsorptionOverrideMid, b.AirAbsorptionOverrideMid) &&
                   NullableClose(a.AirAbsorptionOverrideHigh, b.AirAbsorptionOverrideHigh);
        }

        private static bool NullableClose(float? a, float? b)
        {
            if (!a.HasValue && !b.HasValue)
                return true;
            if (!a.HasValue || !b.HasValue)
                return false;
            return AreClose(a.Value, b.Value);
        }

        private static bool AreClose(float a, float b)
        {
            return Math.Abs(a - b) < 0.0001f;
        }

        private static Dictionary<string, int> BuildSegmentIndex(IReadOnlyList<TrackDefinition> definitions)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < definitions.Count; i++)
            {
                var id = definitions[i].SegmentId;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                var normalizedId = id!;
                if (!map.ContainsKey(normalizedId))
                    map[normalizedId] = i;
            }
            return map;
        }

        private static string ResolveSourceDirectory(string? sourcePath)
        {
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                var path = Path.GetFullPath(sourcePath);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    return directory!;
            }
            return Path.Combine(AssetPaths.Root, "Tracks");
        }

        private float WrapPosition(float position)
        {
            if (_lapDistance <= 0f)
                return position;
            var wrapped = position % _lapDistance;
            if (wrapped < 0f)
                wrapped += _lapDistance;
            return wrapped;
        }

        private float WrapWorldZ(float zInLap, float listenerLapPos, float listenerWorldPos)
        {
            if (_lapDistance <= 0f)
                return zInLap;
            var delta = AudioWorld.WrapDelta(zInLap - listenerLapPos, _lapDistance);
            return listenerWorldPos + delta;
        }

        private static float Lerp(float a, float b, float t)
        {
            if (t < 0f)
                t = 0f;
            if (t > 1f)
                t = 1f;
            return a + ((b - a) * t);
        }

        private float UpdateCenter(float center, TrackDefinition definition)    
        {
            switch (definition.Type)
            {
                case TrackType.EasyLeft:
                    return center - (definition.Length * _curveScale) / 2;
                case TrackType.Left:
                    return center - (definition.Length * _curveScale) * 2 / 3;
                case TrackType.HardLeft:
                    return center - definition.Length * _curveScale;
                case TrackType.HairpinLeft:
                    return center - (definition.Length * _curveScale) * 3 / 2;
                case TrackType.EasyRight:
                    return center + (definition.Length * _curveScale) / 2;
                case TrackType.Right:
                    return center + (definition.Length * _curveScale) * 2 / 3;
                case TrackType.HardRight:
                    return center + definition.Length * _curveScale;
                case TrackType.HairpinRight:
                    return center + (definition.Length * _curveScale) * 3 / 2;
                default:
                    return center;
            }
        }

        private void ApplyRoadOffset(ref Road road, float center, float relPos, TrackType type)
        {
            var offset = relPos * _curveScale;
            switch (type)
            {
                case TrackType.Straight:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
                case TrackType.EasyLeft:
                    road.Left = center - _laneWidth - offset / 2;
                    road.Right = center + _laneWidth - offset / 2;
                    break;
                case TrackType.Left:
                    road.Left = center - _laneWidth - offset * 2 / 3;
                    road.Right = center + _laneWidth - offset * 2 / 3;
                    break;
                case TrackType.HardLeft:
                    road.Left = center - _laneWidth - offset;
                    road.Right = center + _laneWidth - offset;
                    break;
                case TrackType.HairpinLeft:
                    road.Left = center - _laneWidth - offset * 3 / 2;
                    road.Right = center + _laneWidth - offset * 3 / 2;
                    break;
                case TrackType.EasyRight:
                    road.Left = center - _laneWidth + offset / 2;
                    road.Right = center + _laneWidth + offset / 2;
                    break;
                case TrackType.Right:
                    road.Left = center - _laneWidth + offset * 2 / 3;
                    road.Right = center + _laneWidth + offset * 2 / 3;
                    break;
                case TrackType.HardRight:
                    road.Left = center - _laneWidth + offset;
                    road.Right = center + _laneWidth + offset;
                    break;
                case TrackType.HairpinRight:
                    road.Left = center - _laneWidth + offset * 3 / 2;
                    road.Right = center + _laneWidth + offset * 3 / 2;
                    break;
                default:
                    road.Left = center - _laneWidth;
                    road.Right = center + _laneWidth;
                    break;
            }
        }

        private void UpdateCurveScale()
        {
            _curveScale = LegacyLaneWidthMeters > 0f ? _laneWidth / LegacyLaneWidthMeters : 1.0f;
            if (_curveScale <= 0f)
                _curveScale = 0.01f;
        }

        private static string ResolveCustomTrackName(string path, string? name)
        {
            var trimmedName = name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedName))
                return trimmedName!;
            var directory = Path.GetDirectoryName(path);
            var folderName = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFileName(directory);
            if (!string.IsNullOrWhiteSpace(folderName))
                return folderName!;
            var fileName = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
        }

        private static TrackData ReadCustomTrackData(string filename)
        {
            if (TrackTsmParser.TryLoad(filename, out var parsed, out var issues, MinPartLengthMeters))
                return parsed;

            LogTrackIssues(filename, issues);
            return new TrackData(true, TrackWeather.Sunny, TrackAmbience.NoAmbience,
                new[] { new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLengthMeters) });
        }

        private static void LogTrackIssues(string filename, IReadOnlyList<TrackTsmIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                Console.WriteLine($"[Track] Failed to load '{filename}'.");
                return;
            }

            Console.WriteLine($"[Track] Failed to load '{filename}':");
            for (var i = 0; i < issues.Count; i++)
                Console.WriteLine($"  - {issues[i]}");
        }

        public void Dispose()
        {
            FinalizeTrack();
            DisposeSound(_soundCrowd);
            DisposeSound(_soundOcean);
            DisposeSound(_soundRain);
            DisposeSound(_soundWind);
            DisposeSound(_soundStorm);
            DisposeSound(_soundDesert);
            DisposeSound(_soundAirport);
            DisposeSound(_soundAirplane);
            DisposeSound(_soundClock);
            DisposeSound(_soundJet);
            DisposeSound(_soundThunder);
            DisposeSound(_soundPile);
            DisposeSound(_soundConstruction);
            DisposeSound(_soundRiver);
            DisposeSound(_soundHelicopter);
            DisposeSound(_soundOwl);

            for (var i = 0; i < _allTrackSounds.Count; i++)
                _allTrackSounds[i].Dispose();

            DisposePendingHandleStops();
        }

        private static void DisposeSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
            sound.Dispose();
        }
    }
}
