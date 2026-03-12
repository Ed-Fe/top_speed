using TopSpeed.Speech;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void AddGlobalChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddGlobalChat(text);
            UpdateHistoryScreens();
        }

        private void AddRoomChatMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddRoomChat(text);
            UpdateHistoryScreens();
        }

        private void AddConnectionMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddConnection(text);
            UpdateHistoryScreens();
        }

        private void AddRoomEventMessage(string message)
        {
            var text = NormalizeChatMessage(message);
            if (text == null)
                return;

            _state.Chat.History.AddRoomEvent(text);
            UpdateHistoryScreens();
        }

        internal void NextChatCategory()
        {
            _chatFlow.NextCategory();
        }

        internal void NextChatCategoryCore()
        {
            _state.Chat.History.MoveToNext();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_state.Chat.History.CategoryLabel(), SpeechService.SpeakFlag.None);
        }

        internal void PreviousChatCategory()
        {
            _chatFlow.PreviousCategory();
        }

        internal void PreviousChatCategoryCore()
        {
            _state.Chat.History.MoveToPrevious();
            PlayNetworkSound("buffer_switch.ogg");
            UpdateHistoryScreens();
            _speech.Speak(_state.Chat.History.CategoryLabel(), SpeechService.SpeakFlag.None);
        }
    }
}


