namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        void IMultiplayerRuntime.NextChatCategory()
        {
            NextChatCategory();
        }

        void IMultiplayerRuntime.PreviousChatCategory()
        {
            PreviousChatCategory();
        }

        void IMultiplayerRuntime.OpenGlobalChatHotkey()
        {
            OpenGlobalChatHotkey();
        }

        void IMultiplayerRuntime.OpenRoomChatHotkey()
        {
            OpenRoomChatHotkey();
        }
    }
}
