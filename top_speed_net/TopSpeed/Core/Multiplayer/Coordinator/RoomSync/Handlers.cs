using System;
using System.Collections.Generic;
using TopSpeed.Core.Multiplayer.Chat;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleRoomList(PacketRoomList roomList)
        {
            _roomsFlow.HandleRoomList(roomList);
        }

        internal void HandleRoomListCore(PacketRoomList roomList)
        {
            var effects = new List<PacketEffect>();
            _state.Rooms.RoomList = RoomMap.ToList(roomList);
            if (!_state.Rooms.IsRoomBrowserOpenPending)
                return;

            _state.Rooms.IsRoomBrowserOpenPending = false;
            if (!string.Equals(_menu.CurrentId, MultiplayerMenuKeys.Lobby, StringComparison.Ordinal))
                return;

            var rooms = _state.Rooms.RoomList.Rooms ?? Array.Empty<RoomSummaryInfo>();
            if (rooms.Length == 0)
            {
                effects.Add(PacketEffect.Speak("No game rooms are currently available."));
                DispatchPacketEffects(effects);
                return;
            }

            effects.Add(PacketEffect.UpdateRoomBrowser());
            effects.Add(PacketEffect.Push(MultiplayerMenuKeys.RoomBrowser));
            DispatchPacketEffects(effects);
        }

        public void HandleRoomState(PacketRoomState roomState)
        {
            _roomsFlow.HandleRoomState(roomState);
        }

        internal void HandleRoomStateCore(PacketRoomState roomState)
        {
            var effects = new List<PacketEffect>();
            var wasInRoom = _state.Rooms.WasInRoom;
            var previousRoomId = _state.Rooms.LastRoomId;
            var previousIsHost = _state.Rooms.WasHost;
            var previousRoomType = _state.Rooms.CurrentRoom.RoomType;
            _state.Rooms.CurrentRoom = RoomMap.ToSnapshot(roomState);

            if (_state.Rooms.CurrentRoom.InRoom)
            {
                if (!wasInRoom || previousRoomId != _state.Rooms.CurrentRoom.RoomId)
                {
                    effects.Add(PacketEffect.PlaySound("room_join.ogg"));
                    effects.Add(PacketEffect.AddRoomEventHistory(HistoryText.JoinedRoom(_state.Rooms.CurrentRoom.RoomName)));
                }
            }
            else if (wasInRoom)
            {
                effects.Add(PacketEffect.PlaySound("room_leave.ogg"));
                var leaveText = HistoryText.LeftRoom();
                effects.Add(PacketEffect.Speak(leaveText));
                effects.Add(PacketEffect.AddRoomEventHistory(leaveText));
            }

            _state.Rooms.WasInRoom = _state.Rooms.CurrentRoom.InRoom;
            _state.Rooms.LastRoomId = _state.Rooms.CurrentRoom.RoomId;
            _state.Rooms.WasHost = _state.Rooms.CurrentRoom.IsHost;
            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
                effects.Add(PacketEffect.CancelRoomOptions());

            if (_state.Rooms.CurrentRoom.InRoom && (!wasInRoom || previousRoomId != _state.Rooms.CurrentRoom.RoomId))
            {
                effects.Add(PacketEffect.ShowRoot(MultiplayerMenuKeys.RoomControls));
            }
            else if (!_state.Rooms.CurrentRoom.InRoom && wasInRoom)
            {
                effects.Add(PacketEffect.ShowRoot(MultiplayerMenuKeys.Lobby));
            }

            var roomControlsChanged =
                wasInRoom != _state.Rooms.CurrentRoom.InRoom ||
                previousIsHost != _state.Rooms.CurrentRoom.IsHost ||
                previousRoomType != _state.Rooms.CurrentRoom.RoomType;
            if (roomControlsChanged)
            {
                effects.Add(PacketEffect.RebuildRoomControls());
                effects.Add(PacketEffect.RebuildRoomOptions());
            }

            effects.Add(PacketEffect.RebuildRoomPlayers());
            DispatchPacketEffects(effects);
        }

        public void HandleRoomEvent(PacketRoomEvent roomEvent)
        {
            _roomsFlow.HandleRoomEvent(roomEvent);
        }

        internal void HandleRoomEventCore(PacketRoomEvent roomEvent)
        {
            var eventInfo = RoomMap.ToEvent(roomEvent);
            if (eventInfo == null)
                return;

            var effects = new List<PacketEffect>();
            if (eventInfo.Kind == RoomEventKind.RoomCreated)
            {
                var session = SessionOrNull();
                var isCreator = session != null && eventInfo.HostPlayerId == session.PlayerId;
                if (!isCreator)
                    effects.Add(PacketEffect.PlaySound("room_created.ogg"));
            }

            var roomEventText = HistoryText.FromRoomEvent(eventInfo);
            if (!string.IsNullOrWhiteSpace(roomEventText))
                effects.Add(PacketEffect.AddRoomEventHistory(roomEventText));

            ApplyRoomListEvent(eventInfo);

            ApplyCurrentRoomEvent(eventInfo, effects, out var beginLoadout, out var localHostChanged);
            if (localHostChanged)
            {
                effects.Add(PacketEffect.RebuildRoomControls());
                effects.Add(PacketEffect.RebuildRoomOptions());
            }
            if (_state.Rooms.CurrentRoom.InRoom)
                effects.Add(PacketEffect.RebuildRoomPlayers());

            if (beginLoadout)
                effects.Add(PacketEffect.BeginRaceLoadout());

            DispatchPacketEffects(effects);
        }

        public void HandleProtocolMessage(PacketProtocolMessage message)
        {
            _chatFlow.HandleProtocolMessage(message);
        }

        internal void HandleProtocolMessageCore(PacketProtocolMessage message)
        {
            if (message == null)
                return;

            var effects = new List<PacketEffect>();
            if (message.Code == ProtocolMessageCode.ServerPlayerConnected)
            {
                effects.Add(PacketEffect.PlaySound("online.ogg"));
                effects.Add(PacketEffect.AddConnectionHistory(message.Message));
            }
            else if (message.Code == ProtocolMessageCode.ServerPlayerDisconnected)
            {
                effects.Add(PacketEffect.PlaySound("offline.ogg"));
                effects.Add(PacketEffect.AddConnectionHistory(message.Message));
            }
            else if (message.Code == ProtocolMessageCode.Chat)
            {
                effects.Add(PacketEffect.PlaySound("chat.ogg"));
                effects.Add(PacketEffect.AddGlobalChatHistory(message.Message));
            }
            else if (message.Code == ProtocolMessageCode.RoomChat)
            {
                effects.Add(PacketEffect.PlaySound("room_chat.ogg"));
                effects.Add(PacketEffect.AddRoomChatHistory(message.Message));
            }
            else
            {
                effects.Add(PacketEffect.AddRoomEventHistory(message.Message));
            }

            if (!string.IsNullOrWhiteSpace(message.Message))
                effects.Add(PacketEffect.Speak(message.Message));

            DispatchPacketEffects(effects);
        }
    }
}



