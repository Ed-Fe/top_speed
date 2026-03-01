using TopSpeed.Race.Events;

namespace TopSpeed.Race
{
    internal sealed partial class LevelSingleRace
    {
        public void Run(float elapsed)
        {
            BeginFrame(_raceStartDelay);

            UpdatePositions();

            for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
            {
                var bot = _computerPlayers[botIndex];
                if (bot == null)
                    continue;
                bot.Run(elapsed, _car.PositionX, _car.PositionY);
                if (_track.Lap(bot.PositionY) > _nrOfLaps && !bot.Finished)
                {
                    bot.Stop();
                    bot.SetFinished(true);
                    AnnounceFinishOrder(_soundPlayerNr, _soundFinished, bot.PlayerNumber, ref _positionFinish);
                    if (CheckFinish())
                        PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                }
            }

            RunPlayerVehicleStep(elapsed);
            HandlePlayerLapProgress(
                onPlayerFinished: () =>
                {
                    AnnounceFinishOrder(_soundPlayerNr, _soundFinished, _playerNumber, ref _positionFinish);
                    if (CheckFinish())
                        PushEvent(RaceEventType.RaceFinish, 1.0f + _speakTime - _elapsedTotal);
                });

            CheckForBumps();

            HandleCoreRaceMetricsRequests(includeFinishedRaceTime: true);
            HandleCommentRequests(elapsed, Comment, ref _lastComment, ref _infoKeyReleased);

            HandlePlayerInfoRequests(
                _nComputerPlayers,
                player => player >= 0 && player <= _nComputerPlayers,
                GetVehicleNameForPlayer,
                CalculatePlayerPerc);

            HandlePlayerNumberRequest(_playerNumber);
            HandleGeneralInfoRequests(ref _pauseKeyReleased);

            if (CompleteFrame(elapsed))
                return;
        }

        protected override void OnRaceStartEvent()
        {
            base.OnRaceStartEvent();
            if (_botsScheduled)
                return;

            for (var botIndex = 0; botIndex < _nComputerPlayers; botIndex++)
                _computerPlayers[botIndex]?.PendingStart(0.0f);
            _botsScheduled = true;
        }

        protected override void OnRaceTimeFinalizeEvent()
        {
            base.OnRaceTimeFinalizeEvent();
            RequestExitWhenQueueIdle();
        }

        public void Pause()
        {
            PauseCore(() =>
            {
                for (var i = 0; i < _nComputerPlayers; i++)
                    _computerPlayers[i]?.Pause();
            });
        }

        public void Unpause()
        {
            UnpauseCore(() =>
            {
                for (var i = 0; i < _nComputerPlayers; i++)
                    _computerPlayers[i]?.Unpause();
            });
        }
    }
}
