using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Data;

namespace TopSpeed.Core
{
    internal sealed class TrackSource
    {
        private readonly Dictionary<string, (DateTime LastWriteUtc, TrackInfo Value)> _cache =
            new Dictionary<string, (DateTime LastWriteUtc, TrackInfo Value)>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _issues = new List<string>();

        public IEnumerable<string> GetFiles()
        {
            return GetInfo().Select(i => i.Key);
        }

        public IReadOnlyList<TrackInfo> GetInfo()
        {
            _issues.Clear();
            var files = Scan.Find("Tracks", "*.tsm");
            if (files.Count == 0)
            {
                _cache.Clear();
                return Array.Empty<TrackInfo>();
            }

            var items = new List<TrackInfo>(files.Count);
            var known = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (Scan.TryCached(file, _cache, Parse, out var info))
                    items.Add(info);
            }

            Scan.Prune(_cache, known);
            return items
                .OrderBy(item => item.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IReadOnlyList<string> ConsumeIssues()
        {
            if (_issues.Count == 0)
                return Array.Empty<string>();

            var copy = _issues.ToArray();
            _issues.Clear();
            return copy;
        }

        private (bool Success, TrackInfo Value) Parse(string file)
        {
            try
            {
                if (!TrackTsmParser.TryLoadFromFile(file, out var parsed, out var issues))
                {
                    AppendIssues(file, issues);
                    return (false, default);
                }

                var display = string.IsNullOrWhiteSpace(parsed.Name)
                    ? ResolveFolderName(file)
                    : parsed.Name!;
                if (string.IsNullOrWhiteSpace(display))
                    display = "Custom track";

                return (true, new TrackInfo(file, display));
            }
            catch (Exception ex)
            {
                _issues.Add($"File: {Path.GetFileName(file)}");
                _issues.Add(ex.Message);
                return (false, default);
            }
        }

        private void AppendIssues(string file, IReadOnlyList<TrackTsmIssue> issues)
        {
            _issues.Add($"File: {Path.GetFileName(file)}");

            if (issues == null || issues.Count == 0)
            {
                _issues.Add("Failed to load this track file.");
                return;
            }

            for (var i = 0; i < issues.Count; i++)
                _issues.Add(issues[i].ToString());
        }

        private static string ResolveFolderName(string file)
        {
            var directory = Path.GetDirectoryName(file);
            if (string.IsNullOrWhiteSpace(directory))
                return Path.GetFileNameWithoutExtension(file);
            var name = Path.GetFileName(directory);
            return string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file) : name;
        }
    }
}
