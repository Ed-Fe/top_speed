using System;
using System.Collections.Generic;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Core;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        void IMenuAudioActions.SaveMusicVolume(float volume) => SaveMusicVolume(volume);
        void IMenuAudioActions.ApplyAudioSettings() => ApplyAudioSettings();

        void IMenuRaceActions.QueueRaceStart(RaceMode mode) => QueueRaceStart(mode);

        void IMenuServerActions.StartServerDiscovery() => _multiplayerCoordinator.StartServerDiscovery();
        void IMenuServerActions.OpenSavedServersManager() => _multiplayerCoordinator.OpenSavedServersManager();
        void IMenuServerActions.BeginManualServerEntry() => _multiplayerCoordinator.BeginManualServerEntry();
        void IMenuServerActions.BeginServerPortEntry() => _multiplayerCoordinator.BeginServerPortEntry();

        void IMenuUiActions.SpeakMessage(string text) => _speech.Speak(text);
        void IMenuUiActions.ShowMessageDialog(string title, string caption, IReadOnlyList<string> items) => ShowMessageDialog(title, caption, items);
        void IMenuUiActions.ShowChoiceDialog(string title, string? caption, IReadOnlyDictionary<int, string> items, bool cancelable, string? cancelLabel, Action<ChoiceDialogResult>? onResult)
            => ShowChoiceDialog(title, caption, items, cancelable, cancelLabel, onResult);
        void IMenuUiActions.SpeakNotImplemented() => _speech.Speak("Not implemented yet.");

        void IMenuSettingsActions.RestoreDefaults() => RestoreDefaults();
        void IMenuSettingsActions.RecalibrateScreenReaderRate() => StartCalibrationSequence("options_game");
        void IMenuSettingsActions.SetDevice(InputDeviceMode mode) => SetDevice(mode);
        void IMenuSettingsActions.UpdateSetting(Action update) => UpdateSetting(update);

        void IMenuMappingActions.BeginMapping(InputMappingMode mode, InputAction action) => _inputMapping.BeginMapping(mode, action);
        string IMenuMappingActions.FormatMappingValue(InputAction action, InputMappingMode mode) => _inputMapping.FormatMappingValue(action, mode);

        private void ShowMessageDialog(string title, string caption, IReadOnlyList<string> items)
        {
            var dialogItems = new List<DialogItem>();
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var line = items[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    dialogItems.Add(new DialogItem(line));
                }
            }

            var dialog = new Dialog(
                title ?? string.Empty,
                caption,
                QuestionId.Ok,
                dialogItems,
                onResult: null,
                new DialogButton(QuestionId.Ok, "OK"));
            _dialogs.Show(dialog);
        }

        private void ShowChoiceDialog(
            string title,
            string? caption,
            IReadOnlyDictionary<int, string> items,
            bool cancelable,
            string? cancelLabel,
            Action<ChoiceDialogResult>? onResult)
        {
            var flags = cancelable ? ChoiceDialogFlags.Cancelable : ChoiceDialogFlags.None;
            var dialog = new ChoiceDialog(title, caption, items, onResult, flags, cancelLabel);
            _choices.Show(dialog);
        }

        private void TryShowDeviceChoiceDialog()
        {
            if (!IsMenuState(_state))
                return;
            if (_textInputPromptActive || _inputMapping.IsActive)
                return;
            if (_choices.HasActiveChoiceDialog)
                return;
            if (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion || _dialogs.HasActiveOverlayDialog)
                return;
            if (!_input.TryGetPendingJoystickChoices(out var discovered) || discovered.Count == 0)
                return;

            var items = new Dictionary<int, string>();
            var guidByChoiceId = new Dictionary<int, Guid>();

            for (var i = 0; i < discovered.Count; i++)
            {
                var choiceId = i + 1;
                var choice = discovered[i];
                var label = choice.IsRacingWheel
                    ? $"{choice.DisplayName} (Racing wheel)"
                    : choice.DisplayName;
                items[choiceId] = label;
                guidByChoiceId[choiceId] = choice.InstanceGuid;
            }

            ShowChoiceDialog(
                "Choose controller",
                "Multiple game controllers were detected. Select one controller to use.",
                items,
                cancelable: false,
                cancelLabel: null,
                onResult: result =>
                {
                    if (result.IsCanceled)
                        return;

                    if (!guidByChoiceId.TryGetValue(result.ChoiceId, out var instanceGuid))
                        return;

                    if (_input.TrySelectJoystick(instanceGuid))
                    {
                        if (items.TryGetValue(result.ChoiceId, out var label))
                            _speech.Speak($"Controller selected. {label}.");
                        else
                            _speech.Speak("Controller selected.");
                        return;
                    }

                    _speech.Speak("Unable to activate the selected controller.");
                });
        }
    }
}
