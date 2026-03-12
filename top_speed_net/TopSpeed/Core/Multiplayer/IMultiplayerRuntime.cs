using TopSpeed.Menu;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal interface IMultiplayerRuntime
    {
        QuestionDialog Questions { get; }
        bool IsInRoom { get; }

        void ConfigureMenuCloseHandlers();
        void ShowMultiplayerMenuAfterRace();
        void BeginRaceLoadoutSelection();

        void BeginManualServerEntry();
        void BeginServerPortEntry();
        void StartServerDiscovery();
        void OpenSavedServersManager();
        bool UpdatePendingOperations();
        void OnSessionCleared();

        void NextChatCategory();
        void PreviousChatCategory();
        void OpenGlobalChatHotkey();
        void OpenRoomChatHotkey();

        void HandlePingReply(long receivedUtcTicks = 0);
        void HandleRoomList(PacketRoomList roomList);
        void HandleRoomState(PacketRoomState roomState);
        void HandleRoomEvent(PacketRoomEvent roomEvent);
        void HandleProtocolMessage(PacketProtocolMessage message);
    }
}
