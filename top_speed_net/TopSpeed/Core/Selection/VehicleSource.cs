using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TopSpeed.Vehicles;

namespace TopSpeed.Core
{
    internal sealed class VehicleSource
    {
        private readonly Dictionary<string, (DateTime LastWriteUtc, CustomVehicleInfo Value)> _cache =
            new Dictionary<string, (DateTime LastWriteUtc, CustomVehicleInfo Value)>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _issues = new List<string>();

        public IEnumerable<string> GetFiles()
        {
            return GetInfo().Select(i => i.Key);
        }

        public IReadOnlyList<CustomVehicleInfo> GetInfo()
        {
            _issues.Clear();
            var files = Scan.Find("Vehicles", "*.tsv");
            if (files.Count == 0)
            {
                _cache.Clear();
                return Array.Empty<CustomVehicleInfo>();
            }

            var items = new List<CustomVehicleInfo>(files.Count);
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

        private (bool Success, CustomVehicleInfo Value) Parse(string file)
        {
            try
            {
                if (!VehicleTsvParser.TryLoadFromFile(file, out var parsed, out var issues))
                {
                    AppendIssues(file, issues);
                    return (false, default);
                }

                var info = new CustomVehicleInfo(
                    file,
                    string.IsNullOrWhiteSpace(parsed.Meta.Name) ? "Custom vehicle" : parsed.Meta.Name,
                    parsed.Meta.Version ?? string.Empty,
                    parsed.Meta.Description ?? string.Empty);
                return (true, info);
            }
            catch (Exception ex)
            {
                _issues.Add($"File: {Path.GetFileName(file)}");
                _issues.Add(ex.Message);
                return (false, default);
            }
        }

        private void AppendIssues(string file, IReadOnlyList<VehicleTsvIssue> issues)
        {
            _issues.Add($"File: {Path.GetFileName(file)}");

            if (issues == null || issues.Count == 0)
            {
                _issues.Add("Failed to load this vehicle file.");
                return;
            }

            for (var i = 0; i < issues.Count; i++)
                _issues.Add(issues[i].ToString());
        }
    }
}
