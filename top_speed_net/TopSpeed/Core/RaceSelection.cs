using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Input;

namespace TopSpeed.Core
{
    internal sealed class RaceSelection
    {
        private readonly RaceSetup _setup;
        private readonly RaceSettings _settings;
        private readonly TrackSource _tracks;
        private readonly VehicleSource _vehicles;

        public RaceSelection(RaceSetup setup, RaceSettings settings)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _tracks = new TrackSource();
            _vehicles = new VehicleSource();
        }

        public void SelectTrack(TrackCategory category, string trackKey)
        {
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = trackKey;
        }

        public void SelectRandomTrack(TrackCategory category)
        {
            SelectRandomTrack(category, _settings.RandomCustomTracks);
        }

        public void SelectRandomTrack(TrackCategory category, bool includeCustom)
        {
            if (category == TrackCategory.CustomTrack)
            {
                SelectRandomCustomTrack();
                return;
            }

            var customTracks = includeCustom ? GetCustomTrackFiles() : Array.Empty<string>();
            _setup.TrackCategory = category;
            _setup.TrackNameOrFile = TrackList.GetRandomTrackKey(category, customTracks);
        }

        public void SelectRandomTrackAny(bool includeCustom)
        {
            var customTracks = includeCustom ? GetCustomTrackFiles() : Array.Empty<string>();
            var pick = TrackList.GetRandomTrackAny(customTracks);
            _setup.TrackCategory = pick.Category;
            _setup.TrackNameOrFile = pick.Key;
        }

        public void SelectRandomCustomTrack()
        {
            var customTracks = GetCustomTrackFiles().ToList();
            if (customTracks.Count == 0)
            {
                SelectTrack(TrackCategory.RaceTrack, TrackList.RaceTracks[0].Key);
                return;
            }

            var index = Algorithm.RandomInt(customTracks.Count);
            SelectTrack(TrackCategory.CustomTrack, customTracks[index]);
        }

        public void SelectVehicle(int index)
        {
            _setup.VehicleIndex = index;
            _setup.VehicleFile = null;
        }

        public void SelectCustomVehicle(string file)
        {
            _setup.VehicleIndex = null;
            _setup.VehicleFile = file;
        }

        public void SelectRandomVehicle()
        {
            var customFiles = _settings.RandomCustomVehicles
                ? GetCustomVehicleInfo().Select(v => v.Key).ToList()
                : new List<string>();
            var total = VehicleCatalog.VehicleCount + customFiles.Count;
            if (total <= 0)
            {
                SelectVehicle(0);
                return;
            }

            var roll = Algorithm.RandomInt(total);
            if (roll < VehicleCatalog.VehicleCount)
            {
                SelectVehicle(roll);
                return;
            }

            var customIndex = roll - VehicleCatalog.VehicleCount;
            if (customIndex >= 0 && customIndex < customFiles.Count)
                SelectCustomVehicle(customFiles[customIndex]);
            else
                SelectVehicle(0);
        }

        public void SelectRandomCustomVehicle()
        {
            var customFiles = GetCustomVehicleInfo().Select(v => v.Key).ToList();
            if (customFiles.Count == 0)
            {
                SelectVehicle(0);
                return;
            }

            var index = Algorithm.RandomInt(customFiles.Count);
            SelectCustomVehicle(customFiles[index]);
        }

        public IEnumerable<string> GetCustomTrackFiles()
        {
            return _tracks.GetFiles();
        }

        public IReadOnlyList<TrackInfo> GetCustomTrackInfo()
        {
            return _tracks.GetInfo();
        }

        public IReadOnlyList<string> ConsumeCustomTrackIssues()
        {
            return _tracks.ConsumeIssues();
        }

        public IEnumerable<string> GetCustomVehicleFiles()
        {
            return _vehicles.GetFiles();
        }

        public IReadOnlyList<CustomVehicleInfo> GetCustomVehicleInfo()
        {
            return _vehicles.GetInfo();
        }

        public IReadOnlyList<string> ConsumeCustomVehicleIssues()
        {
            return _vehicles.ConsumeIssues();
        }
    }
}
