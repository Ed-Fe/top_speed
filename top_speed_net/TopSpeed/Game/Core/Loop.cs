namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public void Update(float deltaSeconds)
        {
            _input.Update();
            if (_input.TryGetJoystickState(out var joystick))
                _raceInput.Run(_input.Current, joystick, deltaSeconds, _input.ActiveJoystickIsRacingWheel);
            else
                _raceInput.Run(_input.Current, deltaSeconds);

            TryShowDeviceChoiceDialog();

            _raceInput.SetOverlayInputBlocked(
                _state == AppState.MultiplayerRace &&
                (_multiplayerCoordinator.Questions.HasActiveOverlayQuestion || _dialogs.HasActiveOverlayDialog));

            UpdateTextInputPrompt();
            _stateMachine.Update(deltaSeconds);

            if (_pendingRaceStart)
            {
                _pendingRaceStart = false;
                StartRace(_pendingMode);
            }

            SyncAudioLoopState();
        }
    }
}
