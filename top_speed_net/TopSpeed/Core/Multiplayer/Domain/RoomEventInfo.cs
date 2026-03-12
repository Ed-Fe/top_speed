using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomEventInfo
    {
        public uint RoomId;
        public uint RoomVersion;
        public RoomEventKind Kind;
        public uint HostPlayerId;
        public GameRoomType RoomType;
        public byte PlayerCount;
        public byte PlayersToStart;
        public bool RaceStarted;
        public bool PreparingRace;
        public string TrackName = string.Empty;
        public byte Laps;
        public string RoomName = string.Empty;
        public uint SubjectPlayerId;
        public byte SubjectPlayerNumber;
        public PlayerState SubjectPlayerState;
        public string SubjectPlayerName = string.Empty;
    }
}
