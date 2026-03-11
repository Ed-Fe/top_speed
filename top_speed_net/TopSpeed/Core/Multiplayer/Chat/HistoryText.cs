using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer.Chat
{
    internal static class HistoryText
    {
        public static string JoinedRoom(string roomName)
        {
            return $"You joined {NormalizeRoomName(roomName)}.";
        }

        public static string LeftRoom()
        {
            return "You left the game room.";
        }

        public static string BecameHost()
        {
            return "You are now host of this game.";
        }

        public static string ParticipantJoined(PacketRoomEvent roomEvent)
        {
            return $"{ResolvePlayerName(roomEvent)} joined the current room.";
        }

        public static string ParticipantLeft(PacketRoomEvent roomEvent)
        {
            return $"{ResolvePlayerName(roomEvent)} left the current room.";
        }

        public static string FromRoomEvent(PacketRoomEvent roomEvent)
        {
            var roomName = NormalizeRoomName(roomEvent.RoomName);
            switch (roomEvent.Kind)
            {
                case RoomEventKind.RaceStarted:
                    return $"Race started in {roomName}.";
                case RoomEventKind.RaceStopped:
                    return $"Race stopped in {roomName}.";
                default:
                    return string.Empty;
            }
        }

        private static string ResolvePlayerName(PacketRoomEvent roomEvent)
        {
            if (!string.IsNullOrWhiteSpace(roomEvent.SubjectPlayerName))
                return roomEvent.SubjectPlayerName.Trim();
            return $"Player {roomEvent.SubjectPlayerNumber + 1}";
        }

        private static string NormalizeRoomName(string roomName)
        {
            if (!string.IsNullOrWhiteSpace(roomName))
                return roomName.Trim();
            return "game room";
        }
    }
}
