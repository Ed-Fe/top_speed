using System;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal static class RoomMap
    {
        public static RoomListInfo ToList(PacketRoomList? packet)
        {
            if (packet == null || packet.Rooms == null || packet.Rooms.Length == 0)
                return new RoomListInfo();

            var rooms = new RoomSummaryInfo[packet.Rooms.Length];
            for (var i = 0; i < packet.Rooms.Length; i++)
            {
                var src = packet.Rooms[i] ?? new PacketRoomSummary();
                rooms[i] = new RoomSummaryInfo
                {
                    RoomId = src.RoomId,
                    RoomName = src.RoomName ?? string.Empty,
                    RoomType = src.RoomType,
                    PlayerCount = src.PlayerCount,
                    PlayersToStart = src.PlayersToStart,
                    RaceStarted = src.RaceStarted,
                    TrackName = src.TrackName ?? string.Empty
                };
            }

            return new RoomListInfo { Rooms = rooms };
        }

        public static RoomSnapshot ToSnapshot(PacketRoomState? packet)
        {
            if (packet == null)
                return new RoomSnapshot();

            return new RoomSnapshot
            {
                RoomVersion = packet.RoomVersion,
                RoomId = packet.RoomId,
                HostPlayerId = packet.HostPlayerId,
                RoomName = packet.RoomName ?? string.Empty,
                RoomType = packet.RoomType,
                PlayersToStart = packet.PlayersToStart,
                InRoom = packet.InRoom,
                IsHost = packet.IsHost,
                RaceStarted = packet.RaceStarted,
                PreparingRace = packet.PreparingRace,
                TrackName = packet.TrackName ?? string.Empty,
                Laps = packet.Laps,
                Players = ToParticipants(packet.Players)
            };
        }

        public static RoomEventInfo? ToEvent(PacketRoomEvent? packet)
        {
            if (packet == null)
                return null;

            return new RoomEventInfo
            {
                RoomId = packet.RoomId,
                RoomVersion = packet.RoomVersion,
                Kind = packet.Kind,
                HostPlayerId = packet.HostPlayerId,
                RoomType = packet.RoomType,
                PlayerCount = packet.PlayerCount,
                PlayersToStart = packet.PlayersToStart,
                RaceStarted = packet.RaceStarted,
                PreparingRace = packet.PreparingRace,
                TrackName = packet.TrackName ?? string.Empty,
                Laps = packet.Laps,
                RoomName = packet.RoomName ?? string.Empty,
                SubjectPlayerId = packet.SubjectPlayerId,
                SubjectPlayerNumber = packet.SubjectPlayerNumber,
                SubjectPlayerState = packet.SubjectPlayerState,
                SubjectPlayerName = packet.SubjectPlayerName ?? string.Empty
            };
        }

        private static RoomParticipant[] ToParticipants(PacketRoomPlayer[]? packetPlayers)
        {
            if (packetPlayers == null || packetPlayers.Length == 0)
                return Array.Empty<RoomParticipant>();

            var players = new RoomParticipant[packetPlayers.Length];
            for (var i = 0; i < packetPlayers.Length; i++)
            {
                var src = packetPlayers[i] ?? new PacketRoomPlayer();
                players[i] = new RoomParticipant
                {
                    PlayerId = src.PlayerId,
                    PlayerNumber = src.PlayerNumber,
                    State = src.State,
                    Name = src.Name ?? string.Empty
                };
            }

            return players;
        }
    }
}
