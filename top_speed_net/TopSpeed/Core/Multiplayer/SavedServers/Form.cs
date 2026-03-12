using System;
using System.Collections.Generic;
using TopSpeed.Input;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void RebuildSavedServerFormMenu()
        {
            var controls = new[]
            {
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_state.SavedServers.Draft.Name)
                        ? "Server name, currently empty."
                        : $"Server name, currently set to {_state.SavedServers.Draft.Name}",
                    UpdateSavedServerDraftName),
                new MenuFormControl(
                    () => string.IsNullOrWhiteSpace(_state.SavedServers.Draft.Host)
                        ? "Server IP or host, currently empty."
                        : $"Server IP or host, currently set to {_state.SavedServers.Draft.Host}",
                    UpdateSavedServerDraftHost),
                new MenuFormControl(
                    () => _state.SavedServers.Draft.Port > 0
                        ? $"Server port, currently set to {_state.SavedServers.Draft.Port}"
                        : "Server port, currently empty.",
                    UpdateSavedServerDraftPort)
            };

            var saveLabel = _state.SavedServers.EditIndex >= 0 ? "Save server changes" : "Save server";
            var items = MenuFormBuilder.BuildItems(
                controls,
                saveLabel,
                SaveSavedServerDraft,
                "Go back");
            _menu.UpdateItems(MultiplayerMenuKeys.SavedServerForm, items, preserveSelection: true);
        }

        private void CloseSavedServerForm()
        {
            if (!IsSavedServerDraftDirty())
            {
                _menu.PopToPrevious();
                return;
            }

            _questions.Show(new Question(
                "Save changes before closing?",
                "Are you sure you would like to discard all changes?.",
                HandleSavedServerDiscardQuestionResult,
                new QuestionButton(QuestionId.Confirm, "Save changes", flags: QuestionButtonFlags.Default),
                new QuestionButton(QuestionId.Close, "Discard changes")));
        }

        private bool IsSavedServerDraftDirty()
        {
            var current = NormalizeSavedServerDraft(_state.SavedServers.Draft);
            var original = NormalizeSavedServerDraft(_state.SavedServers.Original ?? new SavedServerEntry());

            if (_state.SavedServers.EditIndex < 0)
                return !string.IsNullOrWhiteSpace(current.Host) || !string.IsNullOrWhiteSpace(current.Name) || current.Port != 0;

            return !string.Equals(current.Name, original.Name, StringComparison.Ordinal)
                || !string.Equals(current.Host, original.Host, StringComparison.OrdinalIgnoreCase)
                || current.Port != original.Port;
        }

        private void DiscardSavedServerDraftChanges()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.SavedServerForm, StringComparison.Ordinal))
                _menu.PopToPrevious();
        }

        private void SaveSavedServerDraft()
        {
            var normalized = NormalizeSavedServerDraft(_state.SavedServers.Draft);
            if (string.IsNullOrWhiteSpace(normalized.Host))
            {
                _speech.Speak("Server IP or host cannot be empty.");
                return;
            }

            var servers = _settings.SavedServers ?? (_settings.SavedServers = new List<SavedServerEntry>());
            if (_state.SavedServers.EditIndex >= 0 && _state.SavedServers.EditIndex < servers.Count)
                servers[_state.SavedServers.EditIndex] = normalized;
            else
                servers.Add(normalized);

            _saveSettings();
            RebuildSavedServersMenu();

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                _menu.PopToPrevious();
            if (string.Equals(_menu.CurrentId, MultiplayerMenuKeys.SavedServerForm, StringComparison.Ordinal))
                _menu.PopToPrevious();

            _speech.Speak("Server saved.");
        }
    }
}


