using System;
using System.Collections.Generic;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpsertCurrentRoomParticipant(RoomEventInfo roomEvent)
        {
            if (roomEvent.SubjectPlayerId == 0)
                return;

            var players = new List<RoomParticipant>(_state.Rooms.CurrentRoom.Players ?? Array.Empty<RoomParticipant>());
            var index = players.FindIndex(p => p.PlayerId == roomEvent.SubjectPlayerId);
            var name = string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName)
                ? $"Player {roomEvent.SubjectPlayerNumber + 1}"
                : roomEvent.SubjectPlayerName;
            var item = new RoomParticipant
            {
                PlayerId = roomEvent.SubjectPlayerId,
                PlayerNumber = roomEvent.SubjectPlayerNumber,
                State = roomEvent.SubjectPlayerState,
                Name = name
            };

            if (index >= 0)
                players[index] = item;
            else
                players.Add(item);

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            _state.Rooms.CurrentRoom.Players = players.ToArray();
        }

        private void RemoveCurrentRoomParticipant(uint playerId)
        {
            if (playerId == 0)
                return;

            var players = new List<RoomParticipant>(_state.Rooms.CurrentRoom.Players ?? Array.Empty<RoomParticipant>());
            var removed = players.RemoveAll(p => p.PlayerId == playerId);
            if (removed == 0)
                return;

            players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
            _state.Rooms.CurrentRoom.Players = players.ToArray();
        }
    }
}

