using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomSummaryInfo
    {
        public uint RoomId;
        public string RoomName = string.Empty;
        public GameRoomType RoomType;
        public byte PlayerCount;
        public byte PlayersToStart;
        public bool RaceStarted;
        public string TrackName = string.Empty;
    }
}
