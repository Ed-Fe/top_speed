using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomOptionsMenu()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak("You are not currently inside a game room.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak("Only the host can change game options.");
                return;
            }

            BeginRoomOptionsDraft();
            RebuildRoomOptionsMenu();
            _menu.Push(MultiplayerMenuKeys.RoomOptions);
        }

        private void BeginRoomOptionsDraft()
        {
            _state.Rooms.RoomOptionsDraftActive = true;
            _state.Rooms.RoomOptionsTrackRandom = false;
            _state.Rooms.RoomOptionsTrackName = string.IsNullOrWhiteSpace(_state.Rooms.CurrentRoom.TrackName)
                ? TrackList.RaceTracks[0].Key
                : _state.Rooms.CurrentRoom.TrackName;
            _state.Rooms.RoomOptionsLaps = _state.Rooms.CurrentRoom.Laps > 0 ? _state.Rooms.CurrentRoom.Laps : (byte)1;
            _state.Rooms.RoomOptionsPlayersToStart = _state.Rooms.CurrentRoom.PlayersToStart >= 2 ? _state.Rooms.CurrentRoom.PlayersToStart : (byte)2;
            if (_state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne)
                _state.Rooms.RoomOptionsPlayersToStart = 2;
        }

        private void CancelRoomOptionsChanges()
        {
            _state.Rooms.RoomOptionsDraftActive = false;
            _state.Rooms.RoomOptionsTrackRandom = false;
        }

        private void ConfirmRoomOptionsChanges()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || !_state.Rooms.RoomOptionsDraftActive)
            {
                _speech.Speak("Only the host can change game options.");
                return;
            }

            var appliedAny = false;
            var currentTrack = string.IsNullOrWhiteSpace(_state.Rooms.CurrentRoom.TrackName) ? TrackList.RaceTracks[0].Key : _state.Rooms.CurrentRoom.TrackName;
            if (!string.Equals(currentTrack, _state.Rooms.RoomOptionsTrackName, StringComparison.OrdinalIgnoreCase))
            {
                if (!TrySend(session.SendRoomSetTrack(_state.Rooms.RoomOptionsTrackName), "track change request"))
                    return;
                appliedAny = true;
            }

            if (_state.Rooms.CurrentRoom.Laps != _state.Rooms.RoomOptionsLaps)
            {
                if (!TrySend(session.SendRoomSetLaps(_state.Rooms.RoomOptionsLaps), "lap count change request"))
                    return;
                appliedAny = true;
            }

            if (_state.Rooms.CurrentRoom.RoomType != GameRoomType.OneOnOne)
            {
                var playersToStart = _state.Rooms.RoomOptionsPlayersToStart < 2 ? (byte)2 : _state.Rooms.RoomOptionsPlayersToStart;
                if (_state.Rooms.CurrentRoom.PlayersToStart != playersToStart)
                {
                    if (!TrySend(session.SendRoomSetPlayersToStart(playersToStart), "player count change request"))
                        return;
                    appliedAny = true;
                }
            }

            CancelRoomOptionsChanges();
            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
            _speech.Speak(appliedAny ? "Room options updated." : "No option changes to apply.");
        }

        private string GetRoomOptionsTrackText()
        {
            if (!_state.Rooms.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (_state.Rooms.RoomOptionsTrackRandom)
                return "Track, currently random chosen.";

            var trackName = TryGetTrackDisplay(_state.Rooms.RoomOptionsTrackName, out var display)
                ? display
                : _state.Rooms.RoomOptionsTrackName;
            return $"Track, currently {trackName}.";
        }

        private int GetRoomOptionsLapsIndex()
        {
            if (!_state.Rooms.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var laps = _state.Rooms.RoomOptionsLaps < 1 ? (byte)1 : _state.Rooms.RoomOptionsLaps;
            return Math.Max(0, Math.Min(LapCountOptions.Length - 1, laps - 1));
        }

        private void SetRoomOptionsLaps(byte laps)
        {
            if (!_state.Rooms.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (laps < 1 || laps > 16)
                return;
            _state.Rooms.RoomOptionsLaps = laps;
        }

        private int GetRoomOptionsPlayersToStartIndex()
        {
            if (!_state.Rooms.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            var playersToStart = _state.Rooms.RoomOptionsPlayersToStart < 2 ? (byte)2 : _state.Rooms.RoomOptionsPlayersToStart;
            return Math.Max(0, Math.Min(RoomCapacityOptions.Length - 1, playersToStart - 2));
        }

        private void SetRoomOptionsPlayersToStart(byte playersToStart)
        {
            if (!_state.Rooms.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            if (_state.Rooms.CurrentRoom.RoomType == GameRoomType.OneOnOne)
            {
                _state.Rooms.RoomOptionsPlayersToStart = 2;
                return;
            }

            if (playersToStart < 2 || playersToStart > ProtocolConstants.MaxRoomPlayersToStart)
                return;

            _state.Rooms.RoomOptionsPlayersToStart = playersToStart;
        }

        private void OpenRoomTrackTypeMenu()
        {
            if (!_state.Rooms.RoomOptionsDraftActive)
                BeginRoomOptionsDraft();

            RebuildRoomTrackTypeMenu();
            RebuildRoomTrackMenu(MultiplayerMenuKeys.RoomTrackRace, TrackCategory.RaceTrack);
            RebuildRoomTrackMenu(MultiplayerMenuKeys.RoomTrackAdventure, TrackCategory.StreetAdventure);
            _menu.Push(MultiplayerMenuKeys.RoomTrackType);
        }

        private void RebuildRoomTrackTypeMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Race track", MenuAction.None, nextMenuId: MultiplayerMenuKeys.RoomTrackRace),
                new MenuItem("Street adventure", MenuAction.None, nextMenuId: MultiplayerMenuKeys.RoomTrackAdventure),
                new MenuItem("Random", MenuAction.None, onActivate: SelectRandomRoomTrackAny),
                new MenuItem("Go back", MenuAction.Back)
            };

            _menu.UpdateItems(MultiplayerMenuKeys.RoomTrackType, items);
        }

        private void RebuildRoomTrackMenu(string menuId, TrackCategory category)
        {
            var items = new List<MenuItem>();
            var tracks = TrackList.GetTracks(category);
            for (var i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                items.Add(new MenuItem(track.Display, MenuAction.None, onActivate: () => SelectRoomTrack(track.Key, false)));
            }

            items.Add(new MenuItem("Random", MenuAction.None, onActivate: () => SelectRandomRoomTrackCategory(category)));
            items.Add(new MenuItem("Go back", MenuAction.Back));
            _menu.UpdateItems(menuId, items);
        }

        private void SelectRandomRoomTrackAny()
        {
            if (RoomTrackOptions.Length == 0)
            {
                SelectRoomTrack(TrackList.RaceTracks[0].Key, true);
                return;
            }

            var index = Algorithm.RandomInt(RoomTrackOptions.Length);
            SelectRoomTrack(RoomTrackOptions[index].Key, true);
        }

        private void SelectRandomRoomTrackCategory(TrackCategory category)
        {
            var tracks = TrackList.GetTracks(category);
            if (tracks.Count == 0)
            {
                SelectRandomRoomTrackAny();
                return;
            }

            var index = Algorithm.RandomInt(tracks.Count);
            SelectRoomTrack(tracks[index].Key, true);
        }

        private void SelectRoomTrack(string trackKey, bool randomChosen)
        {
            _state.Rooms.RoomOptionsTrackName = string.IsNullOrWhiteSpace(trackKey) ? TrackList.RaceTracks[0].Key : trackKey;
            _state.Rooms.RoomOptionsTrackRandom = randomChosen;
            ReturnToRoomOptionsMenu();
            _speech.Speak(GetRoomOptionsTrackText());
        }

        private void ReturnToRoomOptionsMenu()
        {
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                return;

            while (_menu.CanPop && !string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                _menu.PopToPrevious(announceTitle: false);

            if (!string.Equals(_menu.CurrentId, MultiplayerMenuKeys.RoomOptions, StringComparison.Ordinal))
                _menu.Push(MultiplayerMenuKeys.RoomOptions);
        }

        private static bool TryGetTrackDisplay(string trackKey, out string display)
        {
            display = string.Empty;
            if (string.IsNullOrWhiteSpace(trackKey))
                return false;

            for (var i = 0; i < RoomTrackOptions.Length; i++)
            {
                if (!string.Equals(RoomTrackOptions[i].Key, trackKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                display = RoomTrackOptions[i].Display;
                return true;
            }

            return false;
        }
    }
}



