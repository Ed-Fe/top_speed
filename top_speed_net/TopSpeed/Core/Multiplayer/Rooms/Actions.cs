using System;
using TopSpeed.Data;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private const int QuitLoadoutQuestionYesId = 2001;
        private const int QuitLoadoutQuestionNoId = 2002;

        private void OpenLeaveRoomConfirmation()
        {
            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak("You are not currently inside a game room.");
                return;
            }

            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(
                "Leave this game room?",
                "Are you sure you want to leave the current room?",
                QuestionId.No,
                HandleLeaveRoomQuestionResult,
                new QuestionButton(QuestionId.Yes, "Yes, leave this game room"),
                new QuestionButton(QuestionId.No, "No, stay in this game room", flags: QuestionButtonFlags.Default)));
        }

        private void HandleLeaveRoomQuestionResult(int resultId)
        {
            if (resultId == QuestionId.Yes)
                ConfirmLeaveRoom();
        }

        private void ConfirmLeaveRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!TrySend(session.SendRoomLeave(), "room leave request"))
                return;
            _speech.Speak("Leaving game room.");
            _menu.ShowRoot(MultiplayerMenuKeys.Lobby);
        }

        private void StartGame()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost)
            {
                _speech.Speak("Only the host can start the game.");
                return;
            }

            TrySend(session.SendRoomStartRace(), "race start request");
        }

        private void AddBotToRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak("Bots can only be managed by the host in race-with-bots rooms.");
                return;
            }

            TrySend(session.SendRoomAddBot(), "add bot request");
        }

        private void RemoveLastBotFromRoom()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom || !_state.Rooms.CurrentRoom.IsHost || _state.Rooms.CurrentRoom.RoomType != GameRoomType.BotsRace)
            {
                _speech.Speak("Bots can only be managed by the host in race-with-bots rooms.");
                return;
            }

            TrySend(session.SendRoomRemoveBot(), "remove bot request");
        }

        private void SubmitLoadoutReady(bool automaticTransmission)
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            var vehicleIndex = Math.Max(0, Math.Min(VehicleCatalog.VehicleCount - 1, _state.Rooms.PendingLoadoutVehicleIndex));
            var selectedCar = (CarType)vehicleIndex;
            _setLocalMultiplayerLoadout(vehicleIndex, automaticTransmission);
            if (!TrySend(session.SendRoomPlayerReady(selectedCar, automaticTransmission), "ready state"))
                return;
            _speech.Speak("Ready. Waiting for other players.");
            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
        }

        private void OpenLoadoutExitConfirmation()
        {
            if (_questions.IsQuestionMenu(_menu.CurrentId))
                return;

            _questions.Show(new Question(
                "Quit race preparation?",
                "Do you want to quit race preparation and stay in this game room?",
                QuitLoadoutQuestionNoId,
                HandleQuitLoadoutQuestionResult,
                new QuestionButton(QuitLoadoutQuestionYesId, "Yes, quit race preparation"),
                new QuestionButton(QuitLoadoutQuestionNoId, "No, continue preparing", flags: QuestionButtonFlags.Default)));
        }

        private void HandleQuitLoadoutQuestionResult(int resultId)
        {
            if (resultId == QuitLoadoutQuestionYesId)
                ConfirmQuitLoadout();
            else
                _menu.ShowRoot(MultiplayerMenuKeys.LoadoutVehicle);
        }

        private void ConfirmQuitLoadout()
        {
            var session = SessionOrNull();
            if (session == null)
            {
                _speech.Speak("Not connected to a server.");
                return;
            }

            if (!_state.Rooms.CurrentRoom.InRoom)
            {
                _speech.Speak("You are not in a game room.");
                return;
            }

            if (_state.Rooms.CurrentRoom.PreparingRace)
            {
                if (!TrySend(session.SendRoomPlayerWithdraw(), "race preparation withdrawal"))
                    return;
                _speech.Speak("You left race preparation and returned to room controls.");
            }
            else
            {
                _speech.Speak("Returned to room controls.");
            }

            _menu.ShowRoot(MultiplayerMenuKeys.RoomControls);
        }
    }
}


