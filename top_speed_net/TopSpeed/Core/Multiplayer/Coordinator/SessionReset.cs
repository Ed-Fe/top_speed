using System;
using TopSpeed.Input;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void OnSessionCleared()
        {
            _roomsFlow.OnSessionCleared();
        }

        internal void OnSessionClearedCore()
        {
            _lifetime.CancelAllOperations();
            _lifetime.ResetPing();
            _lifetime.StopNetworkAudio();
            _state.Rooms.RoomList = new RoomListInfo();
            _state.Rooms.CurrentRoom = new RoomSnapshot { InRoom = false, Players = Array.Empty<RoomParticipant>() };
            _state.Rooms.WasInRoom = false;
            _state.Rooms.WasHost = false;
            _state.Rooms.LastRoomId = 0;
            _state.Rooms.IsRoomBrowserOpenPending = false;
            ResetCreateRoomDraft();
            _state.Rooms.PendingLoadoutVehicleIndex = 0;
            _state.Rooms.RoomOptionsDraftActive = false;
            _state.Rooms.RoomOptionsTrackName = string.Empty;
            _state.Rooms.RoomOptionsTrackRandom = false;
            _state.Rooms.RoomOptionsLaps = 1;
            _state.Rooms.RoomOptionsPlayersToStart = 2;
            _state.SavedServers.Draft = new SavedServerEntry();
            _state.SavedServers.Original = null;
            _state.SavedServers.EditIndex = -1;
            _state.SavedServers.PendingDeleteIndex = -1;
            _state.Connection.HasPendingCompatibilityResult = false;
            _state.Connection.PendingCompatibilityResult = default;
            _state.Chat.History.Clear();
            RebuildLobbyMenu();
            RebuildCreateRoomMenu();
            RebuildSavedServersMenu();
            RebuildSavedServerFormMenu();
            RebuildRoomControlsMenu();
            RebuildRoomOptionsMenu();
            RebuildRoomPlayersMenu();
            RebuildLoadoutVehicleMenu();
            RebuildLoadoutTransmissionMenu();
            UpdateRoomBrowserMenu();
            UpdateHistoryScreens();
        }
    }
}



