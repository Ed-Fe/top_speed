using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Guidance;
using TopSpeed.Tracks.Geometry;
using TopSpeed.Tracks.Sectors;
using TopSpeed.Tracks.Topology;
using TopSpeed.Tracks.Walls;
using TS.Audio;

namespace TopSpeed.Tracks.Map
{
    internal readonly struct TrackTurnGuidance
    {
        public TrackTurnGuidance(
            string sectorId,
            string? entryPortalId,
            string? exitPortalId,
            float turnHeadingDegrees,
            float distanceMeters,
            float guidanceRangeMeters,
            bool passed)
        {
            SectorId = sectorId;
            EntryPortalId = entryPortalId;
            ExitPortalId = exitPortalId;
            TurnHeadingDegrees = turnHeadingDegrees;
            DistanceMeters = distanceMeters;
            GuidanceRangeMeters = guidanceRangeMeters;
            Passed = passed;
        }

        public string SectorId { get; }
        public string? EntryPortalId { get; }
        public string? ExitPortalId { get; }
        public float TurnHeadingDegrees { get; }
        public float DistanceMeters { get; }
        public float GuidanceRangeMeters { get; }
        public bool Passed { get; }
    }

    internal sealed class MapTrack : IDisposable
    {
        private const float CallLengthMeters = 30.0f;

        private readonly AudioManager _audio;
        private readonly TrackMap _map;
        private readonly TrackAreaManager _areaManager;
        private readonly TrackSectorManager _sectorManager;
        private readonly TrackSectorRuleManager _sectorRuleManager;
        private readonly TrackPortalManager _portalManager;
        private readonly TrackApproachManager _approachManager;
        private readonly TrackApproachBeacon _approachBeacon;
        private readonly TrackBranchManager _branchManager;
        private readonly TrackWallManager _wallManager;
        private readonly string _trackName;
        private readonly bool _userDefined;
        private TrackNoise _currentNoise;
        private readonly float _trackLength;

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
        private AudioSourceHandle? _soundBeacon;
        private float _beaconCooldown;

        private MapTrack(string trackName, TrackMap map, AudioManager audio, bool userDefined)
        {
            _trackName = string.IsNullOrWhiteSpace(trackName) ? "Track" : trackName.Trim();
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _userDefined = userDefined;
            _currentNoise = TrackNoise.NoNoise;
            _areaManager = map.BuildAreaManager();
            _portalManager = map.BuildPortalManager();
            _sectorManager = new TrackSectorManager(map.Sectors, _areaManager, _portalManager);
            _sectorRuleManager = map.BuildSectorRuleManager();
            _approachManager = new TrackApproachManager(map.Sectors, map.Approaches, _portalManager);
            _approachBeacon = new TrackApproachBeacon(map);
            _branchManager = map.BuildBranchManager();
            _wallManager = new TrackWallManager(map.Shapes, map.Walls);
            _trackLength = ResolveTrackLength();
            InitializeSounds();
        }

        public static MapTrack Load(string nameOrPath, AudioManager audio)
        {
            if (!TrackMapLoader.TryResolvePath(nameOrPath, out var path))
                throw new FileNotFoundException("Track map not found.", nameOrPath);

            var map = TrackMapLoader.Load(path);
            var name = map.Name;
            var userDefined = path.IndexOfAny(new[] { '\\', '/' }) >= 0;
            return new MapTrack(name, map, audio, userDefined);
        }

        public string TrackName => _trackName;
        public TrackWeather Weather => _map.Weather;
        public TrackAmbience Ambience => _map.Ambience;
        public TrackSurface InitialSurface => _map.DefaultSurface;
        public bool UserDefined => _userDefined;
        public float LaneWidth => Math.Max(0.5f, _map.DefaultWidthMeters * 0.5f);
        public TrackMap Map => _map;
        public float Length => _trackLength;
        public TrackSectorRuleManager SectorRules => _sectorRuleManager;
        public TrackBranchManager Branches => _branchManager;
        public bool HasFinishArea => !string.IsNullOrWhiteSpace(_map.FinishAreaId);

        public int Lap(float distanceMeters)
        {
            var length = Length;
            if (length <= 0f)
                return 1;
            return (int)(distanceMeters / length) + 1;
        }

        public void SetLaneWidth(float laneWidth)
        {
            _map.DefaultWidthMeters = Math.Max(0.5f, laneWidth * 2f);
        }

        public MapMovementState CreateStartState()
        {
            return MapMovement.CreateStart(_map);
        }

        public MapMovementState CreateStateFromWorld(Vector3 worldPosition, float headingDegrees)
        {
            return new MapMovementState
            {
                WorldPosition = worldPosition,
                HeadingDegrees = MapMovement.NormalizeDegrees(headingDegrees),
                DistanceMeters = 0f
            };
        }

        public TrackPose GetPose(MapMovementState state)
        {
            var forward = MapMovement.HeadingVector(state.HeadingDegrees);
            var right = new Vector3(forward.Z, 0f, -forward.X);
            var up = Vector3.UnitY;
            var heading = state.HeadingDegrees * (float)Math.PI / 180f;
            return new TrackPose(state.WorldPosition, forward, right, up, heading, 0f);
        }

        public TrackRoad RoadAt(MapMovementState state)
        {
            var road = BuildDefaultRoad();
            var safeZone = false;
            var length = _map.CellSizeMeters;
            var width = Math.Max(0.5f, _map.DefaultWidthMeters);
            var surface = _map.DefaultSurface;
            var noise = _map.DefaultNoise;

            ApplyAreaOverrides(state.WorldPosition, state.HeadingDegrees, ref width, ref length, ref surface, ref noise, ref safeZone);

            road.Left = -width * 0.5f;
            road.Right = width * 0.5f;
            road.Length = length;
            road.Surface = surface;
            road.Type = TrackType.Straight;
            road.IsSafeZone = safeZone;
            road.IsOutOfBounds = !IsWithinTrackInternal(state.WorldPosition, safeZone);
            ApplySectorRuleFields(state.WorldPosition, state.HeadingDegrees, ref road);
            return road;
        }

        private float ResolveTrackLength()
        {
            var best = 0f;
            if (_map.Approaches != null)
            {
                foreach (var approach in _map.Approaches)
                {
                    if (approach?.LengthMeters is { } len && len > best)
                        best = len;
                }
            }

            if (best > 0f)
                return best;

            return Math.Max(0f, best);
        }

        public bool IsInsideFinishArea(Vector3 worldPosition)
        {
            if (_areaManager == null || string.IsNullOrWhiteSpace(_map.FinishAreaId))
                return false;
            if (!_areaManager.TryGetArea(_map.FinishAreaId!, out var area))
                return false;
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            return _areaManager.Contains(area, position);
        }

        public bool TryGetSectorRules(
            Vector3 worldPosition,
            float headingDegrees,
            out TrackSectorDefinition sector,
            out TrackSectorRules rules,
            out PortalDefinition? portal,
            out float? portalDeltaDegrees)
        {
            sector = null!;
            rules = null!;
            portal = null;
            portalDeltaDegrees = null;

            if (_sectorManager == null || _sectorRuleManager == null)
                return false;

            var position2D = new Vector2(worldPosition.X, worldPosition.Z);
            if (!_sectorManager.TryLocate(position2D, headingDegrees, out var foundSector, out var foundPortal, out var delta))
                return false;

            if (!_sectorRuleManager.TryGetRules(foundSector.Id, out var foundRules))
                return false;

            sector = foundSector;
            rules = foundRules;
            portal = foundPortal;
            portalDeltaDegrees = delta;
            return true;
        }

        public bool TryMove(ref MapMovementState state, float distanceMeters, float headingDegrees, out TrackRoad road, out bool boundaryHit)
        {
            boundaryHit = false;
            road = BuildDefaultRoad();
            if (Math.Abs(distanceMeters) < 0.001f)
            {
                road = RoadAt(state);
                return false;
            }

            var previousPosition = state.WorldPosition;
            var heading = MapMovement.NormalizeDegrees(headingDegrees);
            var direction = MapMovement.HeadingVector(heading);
            var nextPosition = previousPosition + (direction * distanceMeters);

            if (TryGetWallCollision(previousPosition, nextPosition, out _))
            {
                boundaryHit = true;
                road = RoadAt(state);
                return false;
            }

            if (!IsWithinTrack(nextPosition))
            {
                boundaryHit = true;
                road = RoadAt(state);
                return false;
            }

            if (!IsSectorTransitionAllowed(previousPosition, nextPosition, heading))
            {
                boundaryHit = true;
                road = RoadAt(state);
                return false;
            }

            state.WorldPosition = nextPosition;
            state.HeadingDegrees = heading;
            state.DistanceMeters += distanceMeters;
            road = RoadAt(state);
            return true;
        }

        public bool TryGetTurnGuidance(Vector3 worldPosition, float headingDegrees, out TrackTurnGuidance guidance)
        {
            guidance = default;
            if (_approachManager == null)
                return false;

            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var best = default(TurnCandidate);
            var hasBest = false;

            foreach (var approach in _approachManager.Approaches)
            {
                if (approach == null)
                    continue;

                var guidanceRange = 0f;
                if (approach.Metadata != null && approach.Metadata.Count > 0 &&
                    TryGetMetadataFloat(approach.Metadata, out var rangeValue, "turn_range", "guidance_range", "turn_guidance_range"))
                {
                    guidanceRange = Math.Max(1f, rangeValue);
                }

                if (IsApproachSideEnabled(approach, TrackApproachSide.Entry))
                    TryBuildTurnCandidate(approach, TrackApproachSide.Entry, position, guidanceRange, ref best, ref hasBest);
                if (IsApproachSideEnabled(approach, TrackApproachSide.Exit))
                    TryBuildTurnCandidate(approach, TrackApproachSide.Exit, position, guidanceRange, ref best, ref hasBest);
            }

            if (!hasBest)
                return false;

            var passed = false;
            if (best.PortalHeadingDegrees.HasValue)
            {
                var forward = HeadingToVector(best.PortalHeadingDegrees.Value);
                var toPlayer = position - best.PortalPosition;
                passed = Vector2.Dot(forward, toPlayer) > 0f;
            }

            var sectorId = best.SectorId ?? string.Empty;
            guidance = new TrackTurnGuidance(
                sectorId,
                best.EntryPortalId,
                best.ExitPortalId,
                best.TurnHeadingDegrees,
                best.DistanceMeters,
                best.GuidanceRangeMeters,
                passed);
            return true;
        }

        public bool TryGetAvailableHeadings(Vector3 worldPosition, float headingDegrees, out IReadOnlyList<float> headings)
        {
            var list = new List<float>();
            var position = new Vector2(worldPosition.X, worldPosition.Z);

            if (_sectorManager != null &&
                _sectorManager.TryLocate(position, headingDegrees, out var sector, out _, out _))
            {
                var branches = _branchManager.GetBranchesForSector(sector.Id);
                foreach (var branch in branches)
                {
                    if (branch == null)
                        continue;
                    foreach (var exit in branch.Exits)
                    {
                        if (exit == null)
                            continue;
                        var exitHeading = exit.HeadingDegrees;
                        if (!exitHeading.HasValue && _portalManager.TryGetPortal(exit.PortalId, out var portal))
                            exitHeading = portal.ExitHeadingDegrees ?? portal.EntryHeadingDegrees;
                        if (exitHeading.HasValue)
                            AddHeading(list, exitHeading.Value);
                    }
                }
            }

            headings = list;
            return list.Count > 0;
        }

        public bool NextRoad(MapMovementState state, float speed, int curveAnnouncementMode, out TrackRoad road)
        {
            road = RoadAt(state);
            if (!TryGetTurnGuidance(state.WorldPosition, state.HeadingDegrees, out var guidance))
                return false;
            if (guidance.Passed)
                return false;

            if (guidance.GuidanceRangeMeters > 0f && guidance.DistanceMeters > guidance.GuidanceRangeMeters)
                return false;

            road.Type = ResolveTurnType(state.HeadingDegrees, guidance.TurnHeadingDegrees);
            return true;
        }

        public void Initialize()
        {
            _currentNoise = TrackNoise.NoNoise;
            _beaconCooldown = 0f;
        }

        public void Run(MapMovementState state, float elapsed)
        {
            var noise = _map.DefaultNoise;
            var surface = _map.DefaultSurface;
            var safeZone = false;
            var length = _map.CellSizeMeters;
            var width = Math.Max(0.5f, _map.DefaultWidthMeters);
            ApplyAreaOverrides(state.WorldPosition, state.HeadingDegrees, ref width, ref length, ref surface, ref noise, ref safeZone);
            if (noise != _currentNoise)
            {
                StopNoise(_currentNoise);
                _currentNoise = noise;
            }

            UpdateNoiseLoop(noise);
            UpdateApproachBeacon(state, elapsed);

            if (_map.Weather == TrackWeather.Rain)
                PlayIfNotPlaying(_soundRain);
            else
                StopSound(_soundRain);

            if (_map.Weather == TrackWeather.Wind)
                PlayIfNotPlaying(_soundWind);
            else
                StopSound(_soundWind);

            if (_map.Weather == TrackWeather.Storm)
                PlayIfNotPlaying(_soundStorm);
            else
                StopSound(_soundStorm);

            if (_map.Ambience == TrackAmbience.Desert)
                PlayIfNotPlaying(_soundDesert);
            else if (_map.Ambience == TrackAmbience.Airport)
                PlayIfNotPlaying(_soundAirport);
            else
            {
                StopSound(_soundDesert);
                StopSound(_soundAirport);
            }
        }

        public void FinalizeTrack()
        {
            StopAllSounds();
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
            DisposeSound(_soundBeacon);
        }

        private TrackRoad BuildDefaultRoad()
        {
            var width = Math.Max(0.5f, _map.DefaultWidthMeters);
            return new TrackRoad
            {
                Left = -width * 0.5f,
                Right = width * 0.5f,
                Surface = _map.DefaultSurface,
                Type = TrackType.Straight,
                Length = _map.CellSizeMeters
            };
        }

        private void ApplyAreaOverrides(
            Vector3 worldPosition,
            float headingDegrees,
            ref float width,
            ref float length,
            ref TrackSurface surface,
            ref TrackNoise noise,
            ref bool safeZone)
        {
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var heading = MapMovement.ToCardinal(headingDegrees);
            ApplySectorOverrides(position, heading, ref width, ref length, ref surface, ref noise, ref safeZone);

            if (_areaManager == null)
                return;

            var areas = _areaManager.FindAreasContaining(position);
            if (areas.Count == 0)
                return;

            TrackAreaDefinition? surfaceArea = null;
            TrackAreaDefinition? widthArea = null;
            foreach (var candidate in areas)
            {
                if (candidate == null)
                    continue;

                if (candidate.Type == TrackAreaType.SafeZone || (candidate.Flags & TrackAreaFlags.SafeZone) != 0)
                    safeZone = true;

                surfaceArea = candidate;
                if (IsWidthAffectingArea(candidate))
                    widthArea = candidate;
            }

            if (surfaceArea != null)
            {
                if (surfaceArea.Surface.HasValue)
                    surface = surfaceArea.Surface.Value;
                if (surfaceArea.Noise.HasValue)
                    noise = surfaceArea.Noise.Value;
            }

            if (widthArea != null)
            {
                if (widthArea.WidthMeters.HasValue)
                    width = Math.Max(0.5f, widthArea.WidthMeters.Value);

                if (!TryApplyMetadataDimensions(widthArea.Metadata, ref width, ref length))
                    TryApplyShapeDimensions(widthArea, heading, ref width, ref length);
            }
        }

        private void ApplySectorOverrides(
            Vector2 position,
            MapDirection heading,
            ref float width,
            ref float length,
            ref TrackSurface surface,
            ref TrackNoise noise,
            ref bool safeZone)
        {
            if (_sectorManager == null)
                return;

            var sectors = _sectorManager.FindSectorsContaining(position);
            if (sectors.Count == 0)
                return;

            var sector = sectors[sectors.Count - 1];
            if (sector.Surface.HasValue)
                surface = sector.Surface.Value;
            if (sector.Noise.HasValue)
                noise = sector.Noise.Value;
            if ((sector.Flags & TrackSectorFlags.SafeZone) != 0)
                safeZone = true;

            TryApplyMetadataDimensions(sector.Metadata, ref width, ref length);
        }

        private static bool TryApplyMetadataDimensions(
            IReadOnlyDictionary<string, string> metadata,
            ref float width,
            ref float length)
        {
            if (metadata == null || metadata.Count == 0)
                return false;

            var hadAny = false;
            if (TryGetMetadataFloat(metadata, out var widthValue, "intersection_width", "width", "lane_width"))
            {
                width = Math.Max(0.5f, widthValue);
                hadAny = true;
            }
            if (TryGetMetadataFloat(metadata, out var lengthValue, "intersection_length", "length"))
            {
                length = Math.Max(0.1f, lengthValue);
                hadAny = true;
            }
            return hadAny;
        }

        private void TryApplyShapeDimensions(
            TrackAreaDefinition area,
            MapDirection heading,
            ref float width,
            ref float length)
        {
            if (!_areaManager.TryGetShape(area.ShapeId, out var shape))
                return;

            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    ApplyRectangleDimensions(shape, heading, ref width, ref length);
                    break;
                case ShapeType.Circle:
                    width = Math.Max(width, shape.Radius * 2f);
                    length = Math.Max(length, shape.Radius * 2f);
                    break;
            }
        }

        private static void ApplyRectangleDimensions(
            ShapeDefinition shape,
            MapDirection heading,
            ref float width,
            ref float length)
        {
            var rectWidth = Math.Abs(shape.Width);
            var rectHeight = Math.Abs(shape.Height);
            if (rectWidth <= 0f || rectHeight <= 0f)
                return;

            switch (heading)
            {
                case MapDirection.North:
                case MapDirection.South:
                    width = Math.Max(width, rectWidth);
                    length = Math.Max(length, rectHeight);
                    break;
                case MapDirection.East:
                case MapDirection.West:
                    width = Math.Max(width, rectHeight);
                    length = Math.Max(length, rectWidth);
                    break;
            }
        }

        private static bool TryGetMetadataFloat(
            IReadOnlyDictionary<string, string> metadata,
            out float value,
            params string[] keys)
        {
            value = 0f;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                    return true;
            }
            return false;
        }

        private static TrackType ResolveTurnType(float currentHeading, float targetHeading)
        {
            var delta = NormalizeDegrees(targetHeading - currentHeading);
            if (delta > 180f)
                delta -= 360f;
            var abs = Math.Abs(delta);
            if (abs < 5f)
                return TrackType.Straight;

            TrackType left;
            TrackType right;
            if (abs < 30f)
            {
                left = TrackType.EasyLeft;
                right = TrackType.EasyRight;
            }
            else if (abs < 70f)
            {
                left = TrackType.Left;
                right = TrackType.Right;
            }
            else if (abs < 120f)
            {
                left = TrackType.HardLeft;
                right = TrackType.HardRight;
            }
            else
            {
                left = TrackType.HairpinLeft;
                right = TrackType.HairpinRight;
            }

            return delta < 0f ? left : right;
        }


        private void TryBuildTurnCandidate(
            TrackApproachDefinition approach,
            TrackApproachSide side,
            Vector2 position,
            float guidanceRangeMeters,
            ref TurnCandidate best,
            ref bool hasBest)
        {
            var portalId = side == TrackApproachSide.Entry ? approach.EntryPortalId : approach.ExitPortalId;
            var portalHeading = side == TrackApproachSide.Entry ? approach.EntryHeadingDegrees : approach.ExitHeadingDegrees;
            if (string.IsNullOrWhiteSpace(portalId) || !portalHeading.HasValue)
                return;
            if (!_portalManager.TryGetPortal(portalId!, out var portal))
                return;

            var portalPos = new Vector2(portal.X, portal.Z);
            var distance = Vector2.Distance(position, portalPos);
            if (TryGetTurnShape(approach, out var turnShape))
                distance = DistanceToShape(turnShape, position, approach.WidthMeters);

            if (!hasBest || distance < best.DistanceMeters)
            {
                best = new TurnCandidate
                {
                    SectorId = approach.SectorId,
                    EntryPortalId = approach.EntryPortalId,
                    ExitPortalId = approach.ExitPortalId,
                    TurnHeadingDegrees = portalHeading.Value,
                    DistanceMeters = distance,
                    PortalPosition = portalPos,
                    PortalHeadingDegrees = portalHeading,
                    GuidanceRangeMeters = guidanceRangeMeters
                };
                hasBest = true;
            }
        }

        private static bool IsApproachSideEnabled(TrackApproachDefinition approach, TrackApproachSide side)
        {
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return true;

            if (TryGetMetadataBool(approach.Metadata, out var enabled, "enabled") && !enabled)
                return false;
            if (TryGetMetadataBool(approach.Metadata, out var turnEnabled, "turn_enabled") && !turnEnabled)
                return false;

            if (TryGetMetadataString(approach.Metadata, out var raw, "approach_side", "approach_sides", "side"))
            {
                var trimmed = raw.Trim().ToLowerInvariant();
                if (trimmed.Contains("none") || trimmed.Contains("off") || trimmed.Contains("disabled"))
                    return false;
                var hasEntry = trimmed.Contains("entry");
                var hasExit = trimmed.Contains("exit");
                if (hasEntry || hasExit)
                    return side == TrackApproachSide.Entry ? hasEntry : hasExit;
            }

            if (TryGetMetadataBool(approach.Metadata, out var entryEnabled, "approach_entry", "entry_enabled", "entry_beacon"))
            {
                if (side == TrackApproachSide.Entry)
                    return entryEnabled;
            }
            if (TryGetMetadataBool(approach.Metadata, out var exitEnabled, "approach_exit", "exit_enabled", "exit_beacon"))
            {
                if (side == TrackApproachSide.Exit)
                    return exitEnabled;
            }

            return true;
        }

        private static bool TryGetMetadataBool(
            IReadOnlyDictionary<string, string> metadata,
            out bool value,
            params string[] keys)
        {
            value = false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (bool.TryParse(raw, out value))
                    return true;
                var trimmed = raw.Trim().ToLowerInvariant();
                if (trimmed is "1" or "yes" or "true" or "on" or "enabled")
                {
                    value = true;
                    return true;
                }
                if (trimmed is "0" or "no" or "false" or "off" or "disabled")
                {
                    value = false;
                    return true;
                }
            }
            return false;
        }

        private struct TurnCandidate
        {
            public string SectorId;
            public string? EntryPortalId;
            public string? ExitPortalId;
            public float TurnHeadingDegrees;
            public float DistanceMeters;
            public Vector2 PortalPosition;
            public float? PortalHeadingDegrees;
            public float GuidanceRangeMeters;
        }

        private bool TryGetTurnShape(TrackApproachDefinition approach, out ShapeDefinition shape)
        {
            shape = null!;
            if (approach?.Metadata == null || approach.Metadata.Count == 0)
                return false;
            if (!TryGetMetadataString(approach.Metadata, out var shapeId, "turn_shape", "centerline_shape", "beacon_shape", "approach_shape"))
                return false;
            return _areaManager.TryGetShape(shapeId, out shape);
        }

        private static bool TryGetMetadataString(
            IReadOnlyDictionary<string, string> metadata,
            out string value,
            params string[] keys)
        {
            value = string.Empty;
            foreach (var key in keys)
            {
                if (metadata.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
                {
                    value = raw.Trim();
                    return true;
                }
            }
            return false;
        }

        private static float DistanceToShape(ShapeDefinition shape, Vector2 position, float? widthOverride)
        {
            switch (shape.Type)
            {
                case ShapeType.Rectangle:
                    return DistanceToRectangle(shape, position);
                case ShapeType.Circle:
                    return DistanceToCircle(shape, position);
                case ShapeType.Polyline:
                    var width = widthOverride ?? 0f;
                    return Math.Max(0f, DistanceToPolyline(shape.Points, position) - (width * 0.5f));
                case ShapeType.Polygon:
                    return DistanceToPolygon(shape.Points, position);
                default:
                    var center = new Vector2(shape.X, shape.Z);
                    return Vector2.Distance(position, center);
            }
        }

        private static float DistanceToRectangle(ShapeDefinition shape, Vector2 position)
        {
            var minX = shape.X;
            var maxX = shape.X + shape.Width;
            var minZ = shape.Z;
            var maxZ = shape.Z + shape.Height;

            var dx = 0f;
            if (position.X < minX)
                dx = minX - position.X;
            else if (position.X > maxX)
                dx = position.X - maxX;

            var dz = 0f;
            if (position.Y < minZ)
                dz = minZ - position.Y;
            else if (position.Y > maxZ)
                dz = position.Y - maxZ;

            return (float)Math.Sqrt((dx * dx) + (dz * dz));
        }

        private static float DistanceToCircle(ShapeDefinition shape, Vector2 position)
        {
            var center = new Vector2(shape.X, shape.Z);
            var distance = Vector2.Distance(center, position);
            return Math.Max(0f, distance - shape.Radius);
        }

        private static float DistanceToPolyline(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count == 0)
                return float.MaxValue;
            if (points.Count == 1)
                return Vector2.Distance(points[0], position);

            var best = float.MaxValue;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var distance = DistanceToSegment(points[i], points[i + 1], position);
                if (distance < best)
                    best = distance;
            }
            return best;
        }

        private static float DistanceToPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            if (points == null || points.Count == 0)
                return float.MaxValue;

            if (IsPointInPolygon(points, position))
                return 0f;

            var best = float.MaxValue;
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Count];
                var distance = DistanceToSegment(a, b, position);
                if (distance < best)
                    best = distance;
            }
            return best;
        }

        private static bool IsPointInPolygon(IReadOnlyList<Vector2> points, Vector2 position)
        {
            var inside = false;
            var j = points.Count - 1;
            for (var i = 0; i < points.Count; i++)
            {
                var pi = points[i];
                var pj = points[j];
                var intersect = ((pi.Y > position.Y) != (pj.Y > position.Y)) &&
                                (position.X < (pj.X - pi.X) * (position.Y - pi.Y) / (pj.Y - pi.Y + float.Epsilon) + pi.X);
                if (intersect)
                    inside = !inside;
                j = i;
            }
            return inside;
        }

        private static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 position)
        {
            var ab = b - a;
            var ap = position - a;
            var abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= float.Epsilon)
                return Vector2.Distance(a, position);
            var t = Vector2.Dot(ap, ab) / abLenSq;
            if (t <= 0f)
                return Vector2.Distance(a, position);
            if (t >= 1f)
                return Vector2.Distance(b, position);
            var closest = a + (ab * t);
            return Vector2.Distance(closest, position);
        }


        private void InitializeSounds()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "Legacy");
            _soundCrowd = CreateLoop(root, "crowd.wav");
            _soundOcean = CreateLoop(root, "ocean.wav");
            _soundRain = CreateLoop(root, "rain.wav");
            _soundWind = CreateLoop(root, "wind.wav");
            _soundStorm = CreateLoop(root, "storm.wav");
            _soundDesert = CreateLoop(root, "desert.wav");
            _soundAirport = CreateLoop(root, "airport.wav");
            _soundAirplane = CreateLoop(root, "airplane.wav");
            _soundClock = CreateLoop(root, "clock.wav");
            _soundJet = CreateLoop(root, "jet.wav");
            _soundThunder = CreateLoop(root, "thunder.wav");
            _soundPile = CreateLoop(root, "pile.wav");
            _soundConstruction = CreateLoop(root, "const.wav");
            _soundRiver = CreateLoop(root, "river.wav");
            _soundHelicopter = CreateLoop(root, "helicopter.wav");
            _soundOwl = CreateLoop(root, "owl.wav");
            _soundBeacon = CreateSpatial(root, "beacon.wav");
        }

        private AudioSourceHandle? CreateLoop(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateLoopingSource(path);
        }

        private AudioSourceHandle? CreateSpatial(string root, string file)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
                return null;
            return _audio.CreateSpatialSource(path, streamFromDisk: true, allowHrtf: true);
        }

        private void UpdateNoiseLoop(TrackNoise noise)
        {
            switch (noise)
            {
                case TrackNoise.Crowd:
                    PlayIfNotPlaying(_soundCrowd);
                    break;
                case TrackNoise.Ocean:
                    PlayIfNotPlaying(_soundOcean);
                    break;
                case TrackNoise.Trackside:
                    PlayIfNotPlaying(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    PlayIfNotPlaying(_soundClock);
                    break;
                case TrackNoise.Jet:
                    PlayIfNotPlaying(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    PlayIfNotPlaying(_soundThunder);
                    break;
                case TrackNoise.Pile:
                case TrackNoise.Construction:
                    PlayIfNotPlaying(_soundPile);
                    PlayIfNotPlaying(_soundConstruction);
                    break;
                case TrackNoise.River:
                    PlayIfNotPlaying(_soundRiver);
                    break;
                case TrackNoise.Helicopter:
                    PlayIfNotPlaying(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    PlayIfNotPlaying(_soundOwl);
                    break;
            }
        }

        private void StopNoise(TrackNoise noise)
        {
            switch (noise)
            {
                case TrackNoise.Crowd:
                    StopSound(_soundCrowd);
                    break;
                case TrackNoise.Ocean:
                    StopSound(_soundOcean);
                    break;
                case TrackNoise.Trackside:
                    StopSound(_soundAirplane);
                    break;
                case TrackNoise.Clock:
                    StopSound(_soundClock);
                    break;
                case TrackNoise.Jet:
                    StopSound(_soundJet);
                    break;
                case TrackNoise.Thunder:
                    StopSound(_soundThunder);
                    break;
                case TrackNoise.Pile:
                case TrackNoise.Construction:
                    StopSound(_soundPile);
                    StopSound(_soundConstruction);
                    break;
                case TrackNoise.River:
                    StopSound(_soundRiver);
                    break;
                case TrackNoise.Helicopter:
                    StopSound(_soundHelicopter);
                    break;
                case TrackNoise.Owl:
                    StopSound(_soundOwl);
                    break;
            }
        }

        private static void PlayIfNotPlaying(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            if (!sound.IsPlaying)
                sound.Play(loop: true);
        }

        private void UpdateApproachBeacon(MapMovementState state, float elapsed)
        {
            if (_soundBeacon == null)
                return;

            var headingDegrees = state.HeadingDegrees;
            if (_approachBeacon.TryGetCue(state.WorldPosition, headingDegrees, out var cue) && !cue.Passed)
            {
                var position = AudioWorld.ToMeters(new Vector3(cue.BeaconPosition.X, 0f, cue.BeaconPosition.Y));
                _soundBeacon.SetPosition(position);
                _soundBeacon.SetVelocity(Vector3.Zero);
                _beaconCooldown -= elapsed;
                if (_beaconCooldown <= 0f)
                {
                    _soundBeacon.Stop();
                    _soundBeacon.SeekToStart();
                    _soundBeacon.Play(loop: false);
                    _beaconCooldown = 1.5f;
                }
                return;
            }

            _beaconCooldown = 0f;
            StopSound(_soundBeacon);
        }

        public bool IsWithinTrack(Vector3 worldPosition)
        {
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            var safeZone = IsSafeZone(position);
            return IsWithinTrackInternal(worldPosition, safeZone);
        }

        private bool IsWithinTrackInternal(Vector3 worldPosition, bool isSafeZone)
        {
            var position = new Vector2(worldPosition.X, worldPosition.Z);
            if (IsBlockedBySectorRules(position))
                return false;
            if (IsBlockedByWall(position))
                return false;
            if (_areaManager != null && _areaManager.ContainsTrackArea(position))
                return true;
            return isSafeZone;
        }

        public bool TryGetWallCollision(Vector3 fromWorld, Vector3 toWorld, out TrackWallDefinition wall)
        {
            wall = null!;
            if (_wallManager == null || !_wallManager.HasWalls)
                return false;

            var from = new Vector2(fromWorld.X, fromWorld.Z);
            var to = new Vector2(toWorld.X, toWorld.Z);
            var delta = to - from;
            var distance = delta.Length();
            if (distance <= 0.001f)
                return false;

            var steps = Math.Max(1, (int)Math.Ceiling(distance / 1.0f));
            var step = delta / steps;
            var pos = from;
            for (var i = 0; i <= steps; i++)
            {
                if (TryFindWallAt(pos, out wall))
                    return true;
                pos += step;
            }

            return false;
        }

        private bool IsBlockedByWall(Vector2 position)
        {
            if (_wallManager == null || !_wallManager.HasWalls)
                return false;
            return TryFindWallAt(position, out _);
        }

        private bool TryFindWallAt(Vector2 position, out TrackWallDefinition wall)
        {
            wall = null!;
            foreach (var candidate in _wallManager.Walls)
            {
                if (candidate == null)
                    continue;
                if (candidate.CollisionMode == TrackWallCollisionMode.Pass)
                    continue;
                if (_wallManager.Contains(candidate, position))
                {
                    wall = candidate;
                    return true;
                }
            }
            return false;
        }

        private static bool IsWidthAffectingArea(TrackAreaDefinition area)
        {
            if (area == null)
                return false;
            if (area.Type == TrackAreaType.Boundary || area.Type == TrackAreaType.OffTrack)
                return false;
            if (area.Type == TrackAreaType.Start || area.Type == TrackAreaType.Finish ||
                area.Type == TrackAreaType.Checkpoint || area.Type == TrackAreaType.Intersection)
                return false;
            return true;
        }

        private bool IsSafeZone(Vector2 position)
        {
            if (_areaManager == null)
                return false;

            var areas = _areaManager.FindAreasContaining(position);
            if (areas.Count == 0)
                return false;

            foreach (var area in areas)
            {
                if (area.Type == TrackAreaType.SafeZone || (area.Flags & TrackAreaFlags.SafeZone) != 0)
                    return true;
            }
            return false;
        }

        private static float HeadingDegrees(MapDirection heading)
        {
            return heading switch
            {
                MapDirection.North => 0f,
                MapDirection.East => 90f,
                MapDirection.South => 180f,
                MapDirection.West => 270f,
                _ => 0f
            };
        }

        private static Vector2 HeadingToVector(float headingDegrees)
        {
            var radians = headingDegrees * (float)Math.PI / 180f;
            return new Vector2((float)Math.Sin(radians), (float)Math.Cos(radians));
        }

        private static void AddHeading(List<float> list, float headingDegrees)
        {
            var normalized = NormalizeDegrees(headingDegrees);
            for (var i = 0; i < list.Count; i++)
            {
                var delta = Math.Abs(NormalizeDegrees(list[i]) - normalized);
                if (delta < 1f || Math.Abs(delta - 360f) < 1f)
                    return;
            }
            list.Add(normalized);
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }

        internal bool IsSectorTransitionAllowed(Vector3 fromPosition, Vector3 toPosition, float headingDegrees)
        {
            if (_sectorRuleManager == null || _sectorManager == null)
                return true;

            var fromPos = new Vector2(fromPosition.X, fromPosition.Z);
            var toPos = new Vector2(toPosition.X, toPosition.Z);

            var hasFrom = _sectorManager.TryLocate(fromPos, headingDegrees, out var fromSector, out var fromPortal, out _);
            var hasTo = _sectorManager.TryLocate(toPos, headingDegrees, out var toSector, out var toPortal, out _);

            if (!hasTo)
                return true;

            if (!_sectorRuleManager.TryGetRules(toSector.Id, out var toRules))
                return true;

            if (toRules.IsClosed || toRules.IsRestricted)
                return false;

            if (hasFrom && !string.Equals(fromSector.Id, toSector.Id, StringComparison.OrdinalIgnoreCase))
            {
                var entryPortalId = toPortal?.Id;
                var exitPortalId = fromPortal?.Id;
                var direction = MapMovement.ToCardinal(headingDegrees);
                if (!_sectorRuleManager.AllowsExit(fromSector.Id, exitPortalId, direction))
                    return false;
                if (!_sectorRuleManager.AllowsEntry(toSector.Id, entryPortalId, direction))
                    return false;
            }

            return true;
        }

        private void ApplySectorRuleFields(Vector3 worldPosition, float headingDegrees, ref TrackRoad road)
        {
            if (_sectorRuleManager == null || _sectorManager == null)
                return;

            var position = new Vector2(worldPosition.X, worldPosition.Z);
            if (!_sectorManager.TryLocate(position, headingDegrees, out var sector, out _, out _))
                return;
            if (!_sectorRuleManager.TryGetRules(sector.Id, out var rules))
                return;

            road.IsClosed = rules.IsClosed;
            road.IsRestricted = rules.IsRestricted;
            road.RequiresStop = rules.RequiresStop;
            road.RequiresYield = rules.RequiresYield;
            road.MinSpeedKph = rules.MinSpeedKph;
            road.MaxSpeedKph = rules.MaxSpeedKph;
        }

        private bool IsBlockedBySectorRules(Vector2 position)
        {
            if (_sectorRuleManager == null || _sectorManager == null)
                return false;

            var sectors = _sectorManager.FindSectorsContaining(position);
            if (sectors.Count == 0)
                return false;

            foreach (var sector in sectors)
            {
                if (_sectorRuleManager.TryGetRules(sector.Id, out var rules) &&
                    (rules.IsClosed || rules.IsRestricted))
                    return true;
            }

            return false;
        }

        private static void StopSound(AudioSourceHandle? sound)
        {
            if (sound == null)
                return;
            sound.Stop();
        }

        private void StopAllSounds()
        {
            StopSound(_soundCrowd);
            StopSound(_soundOcean);
            StopSound(_soundRain);
            StopSound(_soundWind);
            StopSound(_soundStorm);
            StopSound(_soundDesert);
            StopSound(_soundAirport);
            StopSound(_soundAirplane);
            StopSound(_soundClock);
            StopSound(_soundJet);
            StopSound(_soundThunder);
            StopSound(_soundPile);
            StopSound(_soundConstruction);
            StopSound(_soundRiver);
            StopSound(_soundHelicopter);
            StopSound(_soundOwl);
            StopSound(_soundBeacon);
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
