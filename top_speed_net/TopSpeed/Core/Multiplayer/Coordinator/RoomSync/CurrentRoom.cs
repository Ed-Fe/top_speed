using System;
using System.Collections.Generic;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private bool ApplyCurrentRoomEvent(
            RoomEventInfo roomEvent,
            List<PacketEffect> effects,
            out bool beginLoadout,
            out bool localHostChanged)
        {
            beginLoadout = false;
            localHostChanged = false;

            if (!_state.Rooms.CurrentRoom.InRoom || _state.Rooms.CurrentRoom.RoomId != roomEvent.RoomId)
                return false;

            var previousIsHost = _state.Rooms.CurrentRoom.IsHost;
            var session = SessionOrNull();

            _state.Rooms.CurrentRoom.RoomVersion = roomEvent.RoomVersion;
            if (!string.IsNullOrWhiteSpace(roomEvent.RoomName))
                _state.Rooms.CurrentRoom.RoomName = roomEvent.RoomName;
            _state.Rooms.CurrentRoom.HostPlayerId = roomEvent.HostPlayerId;
            _state.Rooms.CurrentRoom.RoomType = roomEvent.RoomType;
            _state.Rooms.CurrentRoom.PlayersToStart = roomEvent.PlayersToStart;
            _state.Rooms.CurrentRoom.RaceStarted = roomEvent.RaceStarted;
            _state.Rooms.CurrentRoom.PreparingRace = roomEvent.PreparingRace;
            _state.Rooms.CurrentRoom.TrackName = roomEvent.TrackName ?? string.Empty;
            _state.Rooms.CurrentRoom.Laps = roomEvent.Laps;
            _state.Rooms.CurrentRoom.IsHost = session != null && roomEvent.HostPlayerId == session.PlayerId;
            var localPlayerId = session?.PlayerId ?? 0u;

            switch (roomEvent.Kind)
            {
                case RoomEventKind.ParticipantJoined:
                    if (roomEvent.SubjectPlayerId != 0 && roomEvent.SubjectPlayerId != localPlayerId)
                    {
                        effects.Add(PacketEffect.PlaySound("room_join.ogg"));
                        effects.Add(PacketEffect.AddRoomEventHistory(HistoryText.ParticipantJoined(roomEvent)));
                    }
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.BotAdded:
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.ParticipantLeft:
                    if (roomEvent.SubjectPlayerId != 0 && roomEvent.SubjectPlayerId != localPlayerId)
                    {
                        effects.Add(PacketEffect.PlaySound("room_leave.ogg"));
                        effects.Add(PacketEffect.AddRoomEventHistory(HistoryText.ParticipantLeft(roomEvent)));
                    }
                    RemoveCurrentRoomParticipant(roomEvent.SubjectPlayerId);
                    break;

                case RoomEventKind.BotRemoved:
                    RemoveCurrentRoomParticipant(roomEvent.SubjectPlayerId);
                    break;

                case RoomEventKind.ParticipantStateChanged:
                    UpsertCurrentRoomParticipant(roomEvent);
                    break;

                case RoomEventKind.PrepareStarted:
                    beginLoadout = true;
                    break;
            }

            localHostChanged = previousIsHost != _state.Rooms.CurrentRoom.IsHost;
            if (localHostChanged &&
                _state.Rooms.CurrentRoom.IsHost &&
                (roomEvent.Kind == RoomEventKind.ParticipantLeft || roomEvent.Kind == RoomEventKind.HostChanged) &&
                (roomEvent.PlayerCount <= 1 || (_state.Rooms.CurrentRoom.Players?.Length ?? int.MaxValue) <= 1))
            {
                var hostText = HistoryText.BecameHost();
                effects.Add(PacketEffect.Speak(hostText));
                effects.Add(PacketEffect.AddRoomEventHistory(hostText));
            }

            _state.Rooms.WasHost = _state.Rooms.CurrentRoom.IsHost;
            return true;
        }
    }
}

