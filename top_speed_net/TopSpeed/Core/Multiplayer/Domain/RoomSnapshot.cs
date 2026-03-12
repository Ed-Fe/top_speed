using System;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomSnapshot
    {
        public uint RoomVersion;
        public uint RoomId;
        public uint HostPlayerId;
        public string RoomName = string.Empty;
        public GameRoomType RoomType;
        public byte PlayersToStart;
        public bool InRoom;
        public bool IsHost;
        public bool RaceStarted;
        public bool PreparingRace;
        public string TrackName = string.Empty;
        public byte Laps;
        public RoomParticipant[] Players = Array.Empty<RoomParticipant>();
    }
}
