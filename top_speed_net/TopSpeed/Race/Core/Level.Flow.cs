using System;
using TopSpeed.Common;
using TopSpeed.Input;
using TopSpeed.Race.Events;

namespace TopSpeed.Race
{
    internal abstract partial class Level
    {
        private const float RequestInfoThrottleSeconds = 1.0f;

        protected void BeginFrame(float raceStartDelaySeconds = DefaultRaceStartDelaySeconds)
        {
            EnsureStartSequenceScheduled(raceStartDelaySeconds);
            ProcessDueEvents();
        }

        protected void EnsureStartSequenceScheduled(float raceStartDelaySeconds = DefaultRaceStartDelaySeconds)
        {
            if (_elapsedTotal == 0.0f)
                ScheduleDefaultStartSequence(raceStartDelaySeconds);
        }

        protected void ProcessDueEvents()
        {
            var dueEvents = CollectDueEvents();
            for (var i = 0; i < dueEvents.Count; i++)
                DispatchRaceEvent(dueEvents[i]);
        }

        protected virtual void OnUnhandledRaceEvent(RaceEvent e)
        {
        }

        protected void HandleCoreRaceMetricsRequests(bool includeFinishedRaceTime)
        {
            HandleEngineStartRequest();
            HandleCurrentGearRequest();
            HandleCurrentLapNumberRequest();
            HandleCurrentRacePercentageRequest();
            HandleCurrentLapPercentageRequest();
            if (includeFinishedRaceTime)
                HandleCurrentRaceTimeRequestWithFinish();
            else
                HandleCurrentRaceTimeRequestActiveOnly();
        }

        protected void HandleGeneralInfoRequests(ref bool pauseKeyReleased)
        {
            HandleTrackNameRequest();
            HandleSpeedReportRequest();
            HandleDistanceReportRequest();
            HandlePauseRequest(ref pauseKeyReleased);
        }

        protected void HandlePlayerNumberRequest(int playerNumber)
        {
            if (_input.GetPlayerNumber())
            {
                QueueSound(_soundNumbers[playerNumber + 1]);
            }
        }

        protected float CalculateGridStartX(int gridIndex, float vehicleWidth, float startLineY)
        {
            var halfWidth = Math.Max(0.1f, vehicleWidth * 0.5f);
            var margin = 0.3f;
            var laneHalfWidth = _track.LaneHalfWidthAtPosition(startLineY);
            var laneOffset = laneHalfWidth - halfWidth - margin;
            if (laneOffset < 0f)
                laneOffset = 0f;
            return gridIndex % 2 == 1 ? laneOffset : -laneOffset;
        }

        protected static float CalculateGridStartY(int gridIndex, float rowSpacing, float startLineY)
        {
            var row = gridIndex / 2;
            return startLineY - (row * rowSpacing);
        }

        protected void HandleCommentRequests(
            float elapsed,
            Action<bool> comment,
            ref float lastComment,
            ref bool infoKeyReleased)
        {
            lastComment += elapsed;
            if (_settings.AutomaticInfo == AutomaticInfoMode.On && lastComment > 6.0f)
            {
                comment(true);
                lastComment = 0.0f;
            }

            if (_input.GetRequestInfo() && infoKeyReleased)
            {
                infoKeyReleased = false;
                if (_elapsedTotal >= _nextRequestInfoAt)
                {
                    comment(false);
                    lastComment = 0.0f;
                    _nextRequestInfoAt = _elapsedTotal + RequestInfoThrottleSeconds;
                }
            }
            else if (!_input.GetRequestInfo() && !infoKeyReleased)
            {
                infoKeyReleased = true;
            }
        }

        protected bool CompleteFrame(float elapsed)
        {
            if (UpdateExitWhenQueueIdle())
                return true;

            _elapsedTotal += elapsed;
            return false;
        }

        protected void RunPlayerVehicleStep(float elapsed, Action? afterTrackUpdate = null)
        {
            UpdateVehiclePanels(elapsed);
            _car.Run(elapsed);
            _track.Run(_car.PositionY);
            afterTrackUpdate?.Invoke();
            var road = _track.RoadAtPosition(_car.PositionY);
            _car.Evaluate(road);
            UpdateAudioListener(elapsed);
            if (_track.NextRoad(_car.PositionY, _car.Speed, (int)_settings.CurveAnnouncement, out var nextRoad))
                CallNextRoad(nextRoad);
        }

        protected bool HandlePlayerLapProgress(Action onPlayerFinished, bool announceLapsToGo = true)
        {
            var currentLap = _track.Lap(_car.PositionY);
            if (currentLap <= _lap)
                return false;

            _lap = currentLap;
            if (_lap > _nrOfLaps)
            {
                ApplyPlayerFinishState();
                onPlayerFinished?.Invoke();
                return true;
            }

            if (announceLapsToGo &&
                _settings.AutomaticInfo != AutomaticInfoMode.Off &&
                _lap > 1 &&
                _lap <= _nrOfLaps)
            {
                Speak(_soundLaps[_nrOfLaps - _lap], true);
            }

            return false;
        }

        private void DispatchRaceEvent(RaceEvent e)
        {
            if (HandleSharedLifecycleEvent(e))
                return;

            switch (e.Type)
            {
                case RaceEventType.PlaySound:
                    QueueSound(e.Sound);
                    break;
                case RaceEventType.PlayRadioSound:
                    _unkeyQueue--;
                    if (_unkeyQueue == 0)
                        Speak(_soundUnkey[Algorithm.RandomInt(MaxUnkeys)]);
                    break;
                default:
                    OnUnhandledRaceEvent(e);
                    break;
            }
        }
    }
}
