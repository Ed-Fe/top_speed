using System;
using System.Collections.Generic;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void ApplyRoomListEvent(RoomEventInfo roomEvent)
        {
            if (roomEvent.Kind == RoomEventKind.None)
                return;

            var rooms = new List<RoomSummaryInfo>(_state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>());
            var index = rooms.FindIndex(r => r.RoomId == roomEvent.RoomId);

            switch (roomEvent.Kind)
            {
                case RoomEventKind.RoomRemoved:
                    if (index >= 0)
                        rooms.RemoveAt(index);
                    break;

                case RoomEventKind.RoomCreated:
                case RoomEventKind.RoomSummaryUpdated:
                case RoomEventKind.RaceStarted:
                case RoomEventKind.RaceStopped:
                case RoomEventKind.ParticipantJoined:
                case RoomEventKind.ParticipantLeft:
                case RoomEventKind.BotAdded:
                case RoomEventKind.BotRemoved:
                case RoomEventKind.PlayersToStartChanged:
                    var summary = new RoomSummaryInfo
                    {
                        RoomId = roomEvent.RoomId,
                        RoomName = roomEvent.RoomName ?? string.Empty,
                        RoomType = roomEvent.RoomType,
                        PlayerCount = roomEvent.PlayerCount,
                        PlayersToStart = roomEvent.PlayersToStart,
                        RaceStarted = roomEvent.RaceStarted,
                        TrackName = roomEvent.TrackName ?? string.Empty
                    };
                    if (index >= 0)
                        rooms[index] = summary;
                    else if (roomEvent.Kind != RoomEventKind.RoomSummaryUpdated || roomEvent.RoomId != 0)
                        rooms.Add(summary);
                    break;
            }

            rooms.Sort((a, b) => a.RoomId.CompareTo(b.RoomId));
            _state.Rooms.RoomList = new RoomListInfo { Rooms = rooms.ToArray() };
        }
    }
}

