using System;
using System.Collections.Generic;
using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void OpenRoomBrowser()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (_state.Rooms.IsRoomBrowserOpenPending)
                return;

            _state.Rooms.IsRoomBrowserOpenPending = true;
            if (!TrySend(session.SendRoomListRequest(), "room list request"))
                _state.Rooms.IsRoomBrowserOpenPending = false;
        }

        private void JoinRoom(uint roomId)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            TrySend(session.SendRoomJoin(roomId), "room join request");
        }

        private void UpdateRoomBrowserMenu()
        {
            var items = new List<MenuItem>();
            var rooms = _state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
            {
                items.Add(new MenuItem("No game rooms found", MenuAction.None));
            }
            else
            {
                foreach (var room in rooms)
                {
                    var roomCopy = room;
                    var typeText = roomCopy.RoomType switch
                    {
                        GameRoomType.OneOnOne => "one-on-one",
                        GameRoomType.PlayersRace => "race without bots",
                        _ => "race with bots"
                    };
                    var label = typeText;
                    if (!string.IsNullOrWhiteSpace(roomCopy.RoomName))
                        label += $", {roomCopy.RoomName}";
                    label += $" game with {roomCopy.PlayerCount} people";
                    label += $", maximum {roomCopy.PlayersToStart} players";
                    if (roomCopy.RaceStarted)
                        label += ", in progress";
                    else if (roomCopy.PlayerCount >= roomCopy.PlayersToStart)
                        label += ", room is full";
                    items.Add(new MenuItem(label, MenuAction.None, onActivate: () => JoinRoom(roomCopy.RoomId)));
                }
            }

            items.Add(new MenuItem("Return to multiplayer lobby", MenuAction.Back));
            _menu.UpdateItems(MultiplayerMenuKeys.RoomBrowser, items);
        }
    }
}


