using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private void PrepareMode(RaceMode mode)
        {
            _setup.Mode = mode;
            _setup.ClearSelection();
        }

        private void CompleteTransmission(RaceMode mode, TransmissionMode transmission)
        {
            _setup.Transmission = transmission;
            _actions.QueueRaceStart(mode);
        }

        private MenuScreen BuildTrackTypeMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Race track", MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.RaceTrack), onActivate: () => _setup.TrackCategory = TrackCategory.RaceTrack),
                new MenuItem("Street adventure", MenuAction.None, nextMenuId: TrackMenuId(mode, TrackCategory.StreetAdventure), onActivate: () => _setup.TrackCategory = TrackCategory.StreetAdventure),
                new MenuItem("Custom track", MenuAction.None, onActivate: () => OpenCustomTrackMenuOrAnnounce(mode)),
                new MenuItem("Random", MenuAction.None, onActivate: () => PushRandomTrackType(mode)),
                BackItem()
            };
            return _menu.CreateMenu(id, items, "Choose track type");
        }

        private MenuScreen BuildTrackMenu(string id, RaceMode mode, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var trackList = TrackList.GetTracks(category);
            var nextMenuId = VehicleMenuId(mode);

            foreach (var track in trackList)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectTrack(category, key)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectRandomTrack(category)));
            items.Add(BackItem());
            return _menu.CreateMenu(id, items, "Select a track");
        }

        private MenuScreen BuildCustomTrackMenu(string id, RaceMode mode)
        {
            return _menu.CreateMenu(id, BuildCustomTrackItems(mode), "Select a custom track");
        }

        private void RefreshCustomTrackMenu(RaceMode mode)
        {
            var id = TrackMenuId(mode, TrackCategory.CustomTrack);
            _menu.UpdateItems(id, BuildCustomTrackItems(mode));
        }

        private List<MenuItem> BuildCustomTrackItems(RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = VehicleMenuId(mode);
            var customTracks = _selection.GetCustomTrackInfo();
            if (customTracks.Count == 0)
            {
                items.Add(BackItem());
                return items;
            }

            foreach (var track in customTracks)
            {
                var key = track.Key;
                items.Add(new MenuItem(track.Display, MenuAction.None, nextMenuId: nextMenuId,
                    onActivate: () => _selection.SelectTrack(TrackCategory.CustomTrack, key)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomCustomTrack));
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildVehicleMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = TransmissionMenuId(mode);

            for (var i = 0; i < VehicleCatalog.VehicleCount; i++)
            {
                var index = i;
                var name = VehicleCatalog.Vehicles[i].Name;
                items.Add(new MenuItem(name, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectVehicle(index)));
            }

            items.Add(new MenuItem("Custom", MenuAction.None, onActivate: () => OpenCustomVehicleMenuOrAnnounce(mode)));
            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomCustomVehicle));
            items.Add(BackItem());
            return _menu.CreateMenu(id, items, "Select a vehicle");
        }

        private MenuScreen BuildCustomVehicleMenu(string id, RaceMode mode)
        {
            return _menu.CreateMenu(id, BuildCustomVehicleItems(mode), "Select a custom vehicle");
        }

        private void RefreshCustomVehicleMenu(RaceMode mode)
        {
            _menu.UpdateItems(CustomVehicleMenuId(mode), BuildCustomVehicleItems(mode));
        }

        private List<MenuItem> BuildCustomVehicleItems(RaceMode mode)
        {
            var items = new List<MenuItem>();
            var nextMenuId = TransmissionMenuId(mode);
            var customVehicles = _selection.GetCustomVehicleInfo();
            if (customVehicles.Count == 0)
            {
                items.Add(BackItem());
                return items;
            }

            foreach (var vehicle in customVehicles)
            {
                var filePath = vehicle.Key;
                var displayName = string.IsNullOrWhiteSpace(vehicle.Display) ? "Custom vehicle" : vehicle.Display;
                items.Add(new MenuItem(displayName, MenuAction.None, nextMenuId: nextMenuId, onActivate: () => _selection.SelectCustomVehicle(filePath)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, nextMenuId: nextMenuId, onActivate: _selection.SelectRandomVehicle));
            items.Add(BackItem());
            return items;
        }

        private MenuScreen BuildTransmissionMenu(string id, RaceMode mode)
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Automatic", MenuAction.None, onActivate: () => CompleteTransmission(mode, TransmissionMode.Automatic)),
                new MenuItem("Manual", MenuAction.None, onActivate: () => CompleteTransmission(mode, TransmissionMode.Manual)),
                new MenuItem("Random", MenuAction.None, onActivate: () => CompleteTransmission(mode, Algorithm.RandomInt(2) == 0 ? TransmissionMode.Automatic : TransmissionMode.Manual)),
                BackItem()
            };
            return _menu.CreateMenu(id, items, "Select transmission mode");
        }

        private void PushRandomTrackType(RaceMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var roll = Algorithm.RandomInt(customTracks.Count > 0 ? 3 : 2);
            var category = roll switch
            {
                0 => TrackCategory.RaceTrack,
                1 => TrackCategory.StreetAdventure,
                _ => TrackCategory.CustomTrack
            };

            _setup.TrackCategory = category;
            if (category == TrackCategory.CustomTrack)
                RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, category));
        }

        private void OpenCustomTrackMenuOrAnnounce(RaceMode mode)
        {
            var customTracks = _selection.GetCustomTrackInfo();
            var issues = _selection.ConsumeCustomTrackIssues();
            if (customTracks.Count == 0)
            {
                if (issues.Count > 0)
                {
                    _actions.ShowMessageDialog(
                        "Custom track errors",
                        "Some custom track files are invalid and were skipped.",
                        issues);
                }
                else
                {
                    _actions.SpeakMessage("No custom tracks found.");
                }
                return;
            }

            _setup.TrackCategory = TrackCategory.CustomTrack;
            RefreshCustomTrackMenu(mode);
            _menu.Push(TrackMenuId(mode, TrackCategory.CustomTrack));

            if (issues.Count > 0)
            {
                _actions.ShowMessageDialog(
                    "Custom track errors",
                    "Some custom track files are invalid and were skipped.",
                    issues);
            }
        }

        private void OpenCustomVehicleMenuOrAnnounce(RaceMode mode)
        {
            var customVehicles = _selection.GetCustomVehicleInfo();
            var issues = _selection.ConsumeCustomVehicleIssues();
            if (customVehicles.Count == 0)
            {
                if (issues.Count > 0)
                {
                    _actions.ShowMessageDialog(
                        "Custom vehicle errors",
                        "Some custom vehicle files are invalid and were skipped.",
                        issues);
                }
                else
                {
                    _actions.SpeakMessage("No custom vehicles found.");
                }
                return;
            }

            RefreshCustomVehicleMenu(mode);
            _menu.Push(CustomVehicleMenuId(mode));

            if (issues.Count > 0)
            {
                _actions.ShowMessageDialog(
                    "Custom vehicle errors",
                    "Some custom vehicle files are invalid and were skipped.",
                    issues);
            }
        }

        private static string TrackMenuId(RaceMode mode, TrackCategory category)
        {
            var prefix = mode == RaceMode.TimeTrial ? "time_trial" : "single_race";
            return category switch
            {
                TrackCategory.RaceTrack => $"{prefix}_tracks_race",
                TrackCategory.StreetAdventure => $"{prefix}_tracks_adventure",
                _ => $"{prefix}_tracks_custom"
            };
        }

        private static string VehicleMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_vehicles" : "single_race_vehicles";
        }

        private static string CustomVehicleMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_vehicles_custom" : "single_race_vehicles_custom";
        }

        private static string TransmissionMenuId(RaceMode mode)
        {
            return mode == RaceMode.TimeTrial ? "time_trial_transmission" : "single_race_transmission";
        }

        private static MenuItem BackItem()
        {
            return new MenuItem("Go back", MenuAction.Back);
        }
    }
}
