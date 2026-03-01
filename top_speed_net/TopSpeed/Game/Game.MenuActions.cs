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
        void IMenuActions.SaveMusicVolume(float volume) => SaveMusicVolume(volume);
        void IMenuActions.QueueRaceStart(RaceMode mode) => QueueRaceStart(mode);
        void IMenuActions.StartServerDiscovery() => _multiplayerCoordinator.StartServerDiscovery();
        void IMenuActions.OpenSavedServersManager() => _multiplayerCoordinator.OpenSavedServersManager();
        void IMenuActions.BeginManualServerEntry() => _multiplayerCoordinator.BeginManualServerEntry();
        void IMenuActions.SpeakMessage(string text) => _speech.Speak(text);
        void IMenuActions.ShowMessageDialog(string title, string caption, IReadOnlyList<string> items) => ShowMessageDialog(title, caption, items);
        void IMenuActions.SpeakNotImplemented() => _speech.Speak("Not implemented yet.");
        void IMenuActions.BeginServerPortEntry() => _multiplayerCoordinator.BeginServerPortEntry();
        void IMenuActions.RestoreDefaults() => RestoreDefaults();
        void IMenuActions.RecalibrateScreenReaderRate() => StartCalibrationSequence("options_game");
        void IMenuActions.SetDevice(InputDeviceMode mode) => SetDevice(mode);
        void IMenuActions.ToggleCurveAnnouncements() => ToggleCurveAnnouncements();
        void IMenuActions.ToggleSetting(Action update) => ToggleSetting(update);
        void IMenuActions.UpdateSetting(Action update) => UpdateSetting(update);
        void IMenuActions.ApplyAudioSettings() => ApplyAudioSettings();
        void IMenuActions.BeginMapping(InputMappingMode mode, InputAction action) => _inputMapping.BeginMapping(mode, action);
        string IMenuActions.FormatMappingValue(InputAction action, InputMappingMode mode) => _inputMapping.FormatMappingValue(action, mode);

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
    }
}
