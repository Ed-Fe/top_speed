using System;
using System.Collections.Generic;
using System.Threading;
using TopSpeed.Menu;
using TopSpeed.Network;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private bool HandleServerAddressInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Please enter a server address.");
                return false;
            }

            var host = trimmed;
            int? overridePort = null;
            var lastColon = trimmed.LastIndexOf(':');
            if (lastColon > 0 && lastColon < trimmed.Length - 1)
            {
                var portPart = trimmed.Substring(lastColon + 1);
                if (int.TryParse(portPart, out var parsedPort))
                {
                    host = trimmed.Substring(0, lastColon);
                    overridePort = parsedPort;
                }
            }

            _settings.LastServerAddress = host;
            _saveSettings();
            _state.Connection.PendingServerAddress = host;
            _state.Connection.PendingServerPort = overridePort ?? ResolveServerPort();
            BeginCallSignInput();
            return true;
        }

        private void BeginCallSignInput()
        {
            PromptCallSignInput(null);
        }

        private bool HandleCallSignInput(string text)
        {
            var trimmed = (text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                _speech.Speak("Call sign cannot be empty.");
                return false;
            }

            _state.Connection.PendingCallSign = trimmed;
            AttemptConnect(_state.Connection.PendingServerAddress, _state.Connection.PendingServerPort, _state.Connection.PendingCallSign);
            return true;
        }

        private void AttemptConnect(string host, int port, string callSign)
        {
            _speech.Speak("Attempting to connect, please wait...");
            ClearPendingCompatibilityResult(disposeSession: true);
            _clearSession();
            _lifetime.ResetPing();
            StartConnectingPulse();
            var connectCts = _lifetime.BeginConnectOperation();
            _lifetime.SetConnectTask(_connector.ConnectAsync(host, port, callSign, TimeSpan.FromSeconds(5), connectCts.Token));
        }

        private void HandleConnectResult(ConnectResult result)
        {
            StopConnectingPulse();
            if (result.Success && result.Session != null)
            {
                if (result.RequiresCompatibilityConfirmation && result.CompatibilityNotice.HasValue)
                {
                    _state.Connection.PendingCompatibilityResult = result;
                    _state.Connection.HasPendingCompatibilityResult = true;
                    ShowCompatibilityDialog(result.CompatibilityNotice.Value);
                    _enterMenuState();
                    return;
                }

                CompleteSuccessfulConnection(result);
                return;
            }

            ShowConnectionFailedDialog(result.Message);
            _enterMenuState();
        }

        private void CompleteSuccessfulConnection(ConnectResult result)
        {
            var session = result.Session;
            if (session == null)
                return;

            _setSession(session);
            _resetPendingState();
            ClearPendingCompatibilityResult(disposeSession: false);

            OnSessionCleared();
            PlayNetworkSound("connected.ogg");

            var welcome = "Connected to server.";
            if (!string.IsNullOrWhiteSpace(result.Motd))
                welcome += $" Message of the day: {result.Motd}.";
            _speech.Speak(welcome);
            _menu.FadeOutMenuMusic();
            _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
            _enterMenuState();
        }

        private void ShowCompatibilityDialog(CompatibilityNotice notice)
        {
            var items = new List<DialogItem>
            {
                new DialogItem(string.IsNullOrWhiteSpace(notice.Message)
                    ? "The server and client are compatible, but not an exact match."
                    : notice.Message),
                new DialogItem($"Your client protocol version: {notice.ClientVersion}"),
                new DialogItem($"Server supported protocol versions: {notice.ServerSupported.MinSupported} to {notice.ServerSupported.MaxSupported}")
            };

            var dialog = new Dialog(
                "Compatibility warning",
                "Review these details before connecting.",
                QuestionId.Close,
                items,
                HandleCompatibilityDialogResult,
                new DialogButton(QuestionId.Confirm, "Continue connection", flags: DialogButtonFlags.Default),
                new DialogButton(QuestionId.Close, "Disconnect"));
            _dialogs.Show(dialog);
        }

        private void HandleCompatibilityDialogResult(int resultId)
        {
            if (!_state.Connection.HasPendingCompatibilityResult)
                return;

            if (resultId == QuestionId.Confirm)
            {
                var result = _state.Connection.PendingCompatibilityResult;
                _state.Connection.HasPendingCompatibilityResult = false;
                _state.Connection.PendingCompatibilityResult = default;
                CompleteSuccessfulConnection(result);
                return;
            }

            if (_state.Connection.PendingCompatibilityResult.Session != null)
                _state.Connection.PendingCompatibilityResult.Session.Dispose();
            ClearPendingCompatibilityResult(disposeSession: false);
            _speech.Speak("Connection canceled.");
            _enterMenuState();
        }

        private void ClearPendingCompatibilityResult(bool disposeSession)
        {
            if (!_state.Connection.HasPendingCompatibilityResult)
                return;

            if (disposeSession && _state.Connection.PendingCompatibilityResult.Session != null)
                _state.Connection.PendingCompatibilityResult.Session.Dispose();

            _state.Connection.HasPendingCompatibilityResult = false;
            _state.Connection.PendingCompatibilityResult = default;
        }

        private void ShowConnectionFailedDialog(string message)
        {
            var text = string.IsNullOrWhiteSpace(message)
                ? "The connection attempt failed for an unknown reason."
                : message.Trim();

            var dialog = new Dialog(
                "Connection failed",
                null,
                QuestionId.Ok,
                new[] { new DialogItem(text) },
                null,
                new DialogButton(QuestionId.Ok, "OK"));
            _dialogs.Show(dialog);
        }
    }
}


