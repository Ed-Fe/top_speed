using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Data;

namespace TopSpeed.Tracks
{
    internal sealed partial class Track
    {
        public static Track Load(string nameOrPath, AudioManager audio)
        {
            if (TrackCatalog.BuiltIn.TryGetValue(nameOrPath, out var builtIn))
                return new Track(nameOrPath, builtIn, audio, userDefined: false);

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
            if (TrackTsmParser.TryLoad(filename, out var parsed, out _, MinPartLengthMeters))
                return parsed;

            return new TrackData(
                true,
                TrackWeather.Sunny,
                TrackAmbience.NoAmbience,
                new[] { new TrackDefinition(TrackType.Straight, TrackSurface.Asphalt, TrackNoise.NoNoise, MinPartLengthMeters) });
        }
    }
}
