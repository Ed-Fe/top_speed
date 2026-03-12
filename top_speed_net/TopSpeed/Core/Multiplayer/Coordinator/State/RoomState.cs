using System;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorRoomState
    {
        public RoomListInfo RoomList = new RoomListInfo();
        public RoomSnapshot CurrentRoom = new RoomSnapshot { InRoom = false, Players = Array.Empty<RoomParticipant>() };
        public bool WasInRoom;
        public uint LastRoomId;
        public bool WasHost;
        public bool IsRoomBrowserOpenPending;
        public GameRoomType CreateRoomType = GameRoomType.BotsRace;
        public byte CreateRoomPlayersToStart = 2;
        public string CreateRoomName = string.Empty;
        public int PendingLoadoutVehicleIndex;
        public bool RoomOptionsDraftActive;
        public string RoomOptionsTrackName = string.Empty;
        public bool RoomOptionsTrackRandom;
        public byte RoomOptionsLaps = 1;
        public byte RoomOptionsPlayersToStart = 2;
    }
}
