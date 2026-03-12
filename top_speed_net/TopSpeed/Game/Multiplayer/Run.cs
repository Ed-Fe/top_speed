using System;
using SharpDX.DirectInput;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Race;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void RunMultiplayerRace(float elapsed)
        {
            if (_multiplayerRace == null)
            {
                EndMultiplayerRace();
                return;
            }

            ProcessMultiplayerPackets();
            if (_multiplayerRace == null)
                return;

            _multiplayerRace.Run(elapsed);
            if (_multiplayerRace.WantsExit)
            {
                EndMultiplayerRace();
                return;
            }

            if (_multiplayerRaceQuitConfirmActive)
            {
                var action = _menu.Update(_input);
                HandleMenuAction(action);
                return;
            }

            if (!_textInputPromptActive && !_dialogs.HasActiveOverlayDialog && !_choices.HasActiveChoiceDialog)
            {
                if (_input.WasPressed(Key.Slash))
                {
                    _multiplayerCoordinator.OpenGlobalChatHotkey();
                    return;
                }

                if (_input.WasPressed(Key.Backslash))
                {
                    _multiplayerCoordinator.OpenRoomChatHotkey();
                    return;
                }
            }

            if (_input.WasPressed(Key.Escape))
                OpenMultiplayerRaceQuitConfirmation();
        }

        private void StartMultiplayerRace()
        {
            if (_session == null)
                return;
            if (_multiplayerRace != null)
                return;
            if (_pendingMultiplayerTrack == null)
            {
                _pendingMultiplayerStart = true;
                return;
            }

            _pendingMultiplayerStart = false;
            FadeOutMenuMusic();
            var trackName = string.IsNullOrWhiteSpace(_pendingMultiplayerTrackName) ? "custom" : _pendingMultiplayerTrackName;
            var laps = _pendingMultiplayerLaps > 0 ? _pendingMultiplayerLaps : _settings.NrOfLaps;
            var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _multiplayerVehicleIndex));
            var automatic = _multiplayerAutomaticTransmission;

            _multiplayerRace?.FinalizeMultiplayerMode();
            _multiplayerRace?.Dispose();
            _multiplayerRace = _raceModeFactory.CreateMultiplayer(
                _pendingMultiplayerTrack!,
                trackName,
                automatic,
                laps,
                vehicleIndex,
                null,
                _input.VibrationDevice,
                _session,
                _session.PlayerId,
                _session.PlayerNumber);
            _multiplayerRace.Initialize();
            _state = AppState.MultiplayerRace;
        }

        private void EndMultiplayerRace()
        {
            _multiplayerRace?.FinalizeMultiplayerMode();
            _multiplayerRace?.Dispose();
            _multiplayerRace = null;
            _multiplayerRaceQuitConfirmActive = false;

            if (_session != null)
            {
                TrySendSession(_session.SendPlayerState(PlayerState.NotReady), "not-ready state");
                _state = AppState.Menu;
                _multiplayerCoordinator.ShowMultiplayerMenuAfterRace();
            }
            else
            {
                _state = AppState.Menu;
                _menu.ShowRoot("main");
                _menu.FadeInMenuMusic();
            }
        }
    }
}


