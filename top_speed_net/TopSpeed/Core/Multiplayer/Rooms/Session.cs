using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void Disconnect()
        {
            _state.Connection.IsPingPending = false;
            _clearSession();
            _speech.Speak("Disconnected from server.");
            _menu.ShowRoot("main");
            _menu.FadeInMenuMusic();
            _enterMenuState();
        }

        private void OpenDisconnectConfirmation()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(
                "Leave server?",
                "Are you sure you want to disconnect?",
                QuestionId.No,
                HandleDisconnectQuestionResult,
                new QuestionButton(QuestionId.Yes, "Yes, disconnect from the server"),
                new QuestionButton(QuestionId.No, "No, stay connected", flags: QuestionButtonFlags.Default)));
        }

        private void HandleDisconnectQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                Disconnect();
        }
    }
}

