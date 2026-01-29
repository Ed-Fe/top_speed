using System;
using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Tracks.Sectors;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Guidance
{
    public enum TrackApproachSide
    {
        Entry = 0,
        Exit = 1
    }

    public readonly struct TrackApproachAlignment
    {
        public TrackApproachAlignment(
            string sectorId,
            TrackApproachSide side,
            float targetHeadingDegrees,
            float deltaDegrees,
            float? widthMeters,
            float? lengthMeters,
            float? toleranceDegrees,
            string? portalId)
        {
            SectorId = sectorId;
            Side = side;
            TargetHeadingDegrees = targetHeadingDegrees;
            DeltaDegrees = deltaDegrees;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            ToleranceDegrees = toleranceDegrees;
            PortalId = portalId;
        }

        public string SectorId { get; }
        public TrackApproachSide Side { get; }
        public float TargetHeadingDegrees { get; }
        public float DeltaDegrees { get; }
        public float? WidthMeters { get; }
        public float? LengthMeters { get; }
        public float? ToleranceDegrees { get; }
        public string? PortalId { get; }
    }

    public sealed class TrackApproachManager
    {
        private readonly Dictionary<string, List<TrackApproachDefinition>> _approachesBySector;
        private readonly TrackPortalManager _portalManager;

        public TrackApproachManager(
            IEnumerable<TrackSectorDefinition> sectors,
            IEnumerable<TrackApproachDefinition> approaches,
            TrackPortalManager portalManager)
        {
            if (portalManager == null)
                throw new ArgumentNullException(nameof(portalManager));

            _portalManager = portalManager;
            _approachesBySector = new Dictionary<string, List<TrackApproachDefinition>>(StringComparer.OrdinalIgnoreCase);

            if (approaches != null)
            {
                foreach (var approach in approaches)
                {
                    if (approach == null)
                        continue;
                    AddApproach(approach);
                }
            }

            if (sectors == null)
                return;

            foreach (var sector in sectors)
            {
                if (sector == null)
                    continue;
                if (_approachesBySector.ContainsKey(sector.Id))
                    continue;
                if (!HasGuidanceMetadata(sector))
                    continue;
                var approach = BuildApproach(sector);
                if (approach != null)
                    AddApproach(approach);
            }
        }

        public TrackApproachManager(IEnumerable<TrackSectorDefinition> sectors, TrackPortalManager portalManager)
            : this(sectors, Array.Empty<TrackApproachDefinition>(), portalManager)
        {
        }

        public IReadOnlyCollection<TrackApproachDefinition> Approaches
        {
            get
            {
                var list = new List<TrackApproachDefinition>();
                foreach (var entry in _approachesBySector.Values)
                {
                    if (entry == null || entry.Count == 0)
                        continue;
                    list.AddRange(entry);
                }
                return list;
            }
        }

        public bool TryGetApproach(string sectorId, out TrackApproachDefinition approach)
        {
            approach = null!;
            if (string.IsNullOrWhiteSpace(sectorId))
                return false;
            if (!_approachesBySector.TryGetValue(sectorId.Trim(), out var list) || list.Count == 0)
                return false;
            approach = list[0];
            return true;
        }

        public bool TryGetBestAlignment(string sectorId, float headingDegrees, out TrackApproachAlignment alignment)
        {
            alignment = default;
            if (string.IsNullOrWhiteSpace(sectorId))
                return false;
            if (!_approachesBySector.TryGetValue(sectorId.Trim(), out var list) || list.Count == 0)
                return false;

            var heading = NormalizeDegrees(headingDegrees);
            var hasAlignment = false;
            var best = default(TrackApproachAlignment);

            foreach (var approach in list)
            {
                var hasEntry = TryBuildAlignment(approach, TrackApproachSide.Entry, heading, out var entryAlignment);
                var hasExit = TryBuildAlignment(approach, TrackApproachSide.Exit, heading, out var exitAlignment);

                if (!hasEntry && !hasExit)
                    continue;

                var candidate = hasEntry && hasExit
                    ? (entryAlignment.DeltaDegrees <= exitAlignment.DeltaDegrees ? entryAlignment : exitAlignment)
                    : (hasEntry ? entryAlignment : exitAlignment);

                if (!hasAlignment || candidate.DeltaDegrees < best.DeltaDegrees)
                {
                    best = candidate;
                    hasAlignment = true;
                }
            }

            if (!hasAlignment)
                return false;

            alignment = best;
            return true;
        }

        private void AddApproach(TrackApproachDefinition approach)
        {
            if (!_approachesBySector.TryGetValue(approach.SectorId, out var list))
            {
                list = new List<TrackApproachDefinition>();
                _approachesBySector[approach.SectorId] = list;
            }
            list.Add(approach);
        }

        private TrackApproachDefinition? BuildApproach(TrackSectorDefinition sector)
        {
            if (sector == null)
                return null;

            var metadata = sector.Metadata;
            var name = GetString(metadata, "name", "approach_name");
            var entryPortalId = GetString(metadata, "entry_portal", "entry");
            var exitPortalId = GetString(metadata, "exit_portal", "exit");
            var entryHeading = GetHeading(metadata, "entry_heading", "entry_dir", "entry_direction");
            var exitHeading = GetHeading(metadata, "exit_heading", "exit_dir", "exit_direction");
            var width = GetFloat(metadata, "width", "lane_width", "approach_width");
            var length = GetFloat(metadata, "length", "approach_length");
            var tolerance = GetFloat(metadata, "tolerance", "alignment_tolerance", "align_tol");

            var portals = _portalManager.GetPortalsForSector(sector.Id);
            if (string.IsNullOrWhiteSpace(entryPortalId))
                entryPortalId = ResolvePortalId(portals, PortalRole.Entry);
            if (string.IsNullOrWhiteSpace(exitPortalId))
                exitPortalId = ResolvePortalId(portals, PortalRole.Exit);

            if (!entryHeading.HasValue && !string.IsNullOrWhiteSpace(entryPortalId))
                entryHeading = ResolvePortalHeading(entryPortalId!, PortalRole.Entry);
            if (!exitHeading.HasValue && !string.IsNullOrWhiteSpace(exitPortalId))
                exitHeading = ResolvePortalHeading(exitPortalId!, PortalRole.Exit);

            if (!entryHeading.HasValue && !exitHeading.HasValue &&
                string.IsNullOrWhiteSpace(entryPortalId) && string.IsNullOrWhiteSpace(exitPortalId))
                return null;

            return new TrackApproachDefinition(
                sector.Id,
                name,
                entryPortalId,
                exitPortalId,
                entryHeading,
                exitHeading,
                width,
                length,
                tolerance,
                metadata);
        }

        private static bool HasGuidanceMetadata(TrackSectorDefinition sector)
        {
            if (sector == null || sector.Metadata == null || sector.Metadata.Count == 0)
                return false;

            if (TryGetBool(sector.Metadata, out var enabled, "guide_enabled", "guidance_enabled", "approach_enabled") && !enabled)
                return false;

            if (TryGetBool(sector.Metadata, out enabled, "guide_enabled", "guidance_enabled", "approach_enabled") && enabled)
                return true;

            foreach (var key in sector.Metadata.Keys)
            {
                if (IsGuidanceKey(key))
                    return true;
            }

            return false;
        }

        private static bool IsGuidanceKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            switch (key.Trim().ToLowerInvariant())
            {
                case "entry_portal":
                case "exit_portal":
                case "entry_heading":
                case "exit_heading":
                case "approach_name":
                case "approach_range":
                case "approach_side":
                case "approach_sides":
                case "approach_width":
                case "approach_length":
                case "width":
                case "lane_width":
                case "length":
                case "tolerance":
                case "alignment_tolerance":
                case "align_tol":
                case "beacon_shape":
                case "beacon_range":
                case "beacon_mode":
                case "turn_range":
                case "turn_shape":
                case "centerline_shape":
                    return true;
            }

            return false;
        }

        private static bool TryGetBool(
            IReadOnlyDictionary<string, string> metadata,
            out bool value,
            params string[] keys)
        {
            value = false;
            if (metadata == null || metadata.Count == 0)
                return false;

            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
                    continue;
                if (TryParseBool(raw, out value))
                    return true;
            }

            return false;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            var trimmed = raw.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
            }
            return bool.TryParse(raw, out value);
        }

        private bool TryBuildAlignment(
            TrackApproachDefinition approach,
            TrackApproachSide side,
            float headingDegrees,
            out TrackApproachAlignment alignment)
        {
            alignment = default;
            float? targetHeading;
            string? portalId;

            if (side == TrackApproachSide.Entry)
            {
                targetHeading = approach.EntryHeadingDegrees;
                portalId = approach.EntryPortalId;
            }
            else
            {
                targetHeading = approach.ExitHeadingDegrees;
                portalId = approach.ExitPortalId;
            }

            if (!targetHeading.HasValue)
                return false;

            var delta = DeltaDegrees(headingDegrees, targetHeading.Value);
            alignment = new TrackApproachAlignment(
                approach.SectorId,
                side,
                targetHeading.Value,
                delta,
                approach.WidthMeters,
                approach.LengthMeters,
                approach.AlignmentToleranceDegrees,
                portalId);

            return true;
        }

        private static string? ResolvePortalId(IReadOnlyList<PortalDefinition> portals, PortalRole role)
        {
            if (portals == null || portals.Count == 0)
                return null;

            foreach (var portal in portals)
            {
                if (portal.Role == role || portal.Role == PortalRole.EntryExit)
                    return portal.Id;
            }
            return portals[0].Id;
        }

        private float? ResolvePortalHeading(string portalId, PortalRole role)
        {
            if (!_portalManager.TryGetPortal(portalId, out var portal))
                return null;

            switch (role)
            {
                case PortalRole.Entry:
                    return portal.EntryHeadingDegrees ?? portal.ExitHeadingDegrees;
                case PortalRole.Exit:
                    return portal.ExitHeadingDegrees ?? portal.EntryHeadingDegrees;
                default:
                    return portal.EntryHeadingDegrees ?? portal.ExitHeadingDegrees;
            }
        }

        private static string? GetString(
            IReadOnlyDictionary<string, string> metadata,
            params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return null;
        }

        private static float? GetFloat(
            IReadOnlyDictionary<string, string> metadata,
            params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var value))
                    continue;
                if (TryParseFloat(value, out var parsed))
                    return parsed;
            }
            return null;
        }

        private static float? GetHeading(
            IReadOnlyDictionary<string, string> metadata,
            params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var value))
                    continue;
                if (TryParseHeading(value, out var heading))
                    return heading;
            }
            return null;
        }

        private static bool TryParseHeading(string value, out float heading)
        {
            heading = 0f;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "n":
                case "north":
                    heading = 0f;
                    return true;
                case "e":
                case "east":
                    heading = 90f;
                    return true;
                case "s":
                case "south":
                    heading = 180f;
                    return true;
                case "w":
                case "west":
                    heading = 270f;
                    return true;
            }
            return TryParseFloat(value, out heading);
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }

        private static float DeltaDegrees(float current, float target)
        {
            var diff = Math.Abs(NormalizeDegrees(current - target));
            return diff > 180f ? 360f - diff : diff;
        }
    }
}
