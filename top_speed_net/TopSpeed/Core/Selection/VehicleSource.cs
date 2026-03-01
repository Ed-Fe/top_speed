using System;
using System.Collections.Generic;
using TopSpeed.Vehicles;

namespace TopSpeed.Core
{
    internal sealed class VehicleSource : SourceBase<CustomVehicleInfo>
    {
        public VehicleSource()
            : base("Vehicles", "*.tsv")
        {
        }

        protected override string GetKey(CustomVehicleInfo info)
        {
            return info.Key;
        }

        protected override string GetDisplay(CustomVehicleInfo info)
        {
            return info.Display;
        }

        protected override (bool Success, CustomVehicleInfo Value) ParseCore(string file)
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

        private void AppendIssues(string file, IReadOnlyList<VehicleTsvIssue> issues)
        {
            AddFileIssue(file);

            if (issues == null || issues.Count == 0)
            {
                AddIssue("Failed to load this vehicle file.");
                return;
            }

            for (var i = 0; i < issues.Count; i++)
                AddIssue(issues[i].ToString());
        }
    }
}
