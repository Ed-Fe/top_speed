using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RoomsFlow
    {
        private readonly MultiplayerCoordinator _owner;

        public RoomsFlow(MultiplayerCoordinator owner)
        {
            _owner = owner;
        }

        public bool IsInRoom => _owner.IsInRoomCore;

        public void ConfigureMenuCloseHandlers()
        {
            _owner.ConfigureMenuCloseHandlersCore();
        }

        public void ShowMultiplayerMenuAfterRace()
        {
            _owner.ShowMultiplayerMenuAfterRaceCore();
        }

        public void BeginRaceLoadoutSelection()
        {
            _owner.BeginRaceLoadoutSelectionCore();
        }

        public void OnSessionCleared()
        {
            _owner.OnSessionClearedCore();
        }

        public void HandleRoomList(PacketRoomList roomList)
        {
            _owner.HandleRoomListCore(roomList);
        }

        public void HandleRoomState(PacketRoomState roomState)
        {
            _owner.HandleRoomStateCore(roomState);
        }

        public void HandleRoomEvent(PacketRoomEvent roomEvent)
        {
            _owner.HandleRoomEventCore(roomEvent);
        }
    }
}

