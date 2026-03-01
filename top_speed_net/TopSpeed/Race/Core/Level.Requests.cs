using System;
using TopSpeed.Common;
using TopSpeed.Input;
using TopSpeed.Race.Events;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal abstract partial class Level
    {
        protected void HandleEngineStartRequest()
        {
            if (_input.GetStartEngine() && _started && !_finished)
            {
                var canStart = !_engineStarted || _car.State == CarState.Crashed;
                if (canStart)
                {
                    _engineStarted = true;
                    if (_car.State == CarState.Crashed)
                        _car.RestartAfterCrash();
                    else
                        _car.Start();
                }
            }
        }

        protected void HandleCurrentGearRequest()
        {
            if (_input.GetCurrentGear() && _started && _lap <= _nrOfLaps)
            {
                var gear = _car.Gear;
                SpeakText(_car.InReverseGear ? "Gear reverse" : $"Gear {gear}");
            }
        }

        protected void HandleCurrentLapNumberRequest()
        {
            if (_input.GetCurrentLapNr() && _started && _lap <= _nrOfLaps)
            {
                SpeakText($"Lap {_lap}");
            }
        }

        protected void HandleCurrentRacePercentageRequest()
        {
            if (_input.GetCurrentRacePerc() && _started && _lap <= _nrOfLaps)
            {
                var perc = (_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Race percentage", units));
            }
        }

        protected void HandleCurrentLapPercentageRequest()
        {
            if (_input.GetCurrentLapPerc() && _started && _lap <= _nrOfLaps)
            {
                var perc = ((_car.PositionY - (_track.Length * (_lap - 1))) / _track.Length) * 100.0f;
                var units = Math.Max(0, Math.Min(100, (int)perc));
                SpeakText(FormatPercentageText("Lap percentage", units));
            }
        }

        protected void HandleCurrentRaceTimeRequestActiveOnly()
        {
            if (_input.GetCurrentRaceTime() && _started && _lap <= _nrOfLaps)
            {
                var text = FormatTimeText((int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs), detailed: false);
                SpeakText($"Race time {text}");
            }
        }

        protected void HandleCurrentRaceTimeRequestWithFinish()
        {
            if (_input.GetCurrentRaceTime() && _started)
            {
                var timeMs = _lap <= _nrOfLaps
                    ? (int)(_stopwatch.ElapsedMilliseconds - _stopwatchDiffMs)
                    : _raceTime;
                var text = FormatTimeText(timeMs, detailed: false);
                SpeakText($"Race time {text}");
            }
        }

        protected void HandleTrackNameRequest()
        {
            if (_input.GetTrackName())
            {
                SpeakText(FormatTrackName(_track.TrackName));
            }
        }

        protected void HandleSpeedReportRequest()
        {
            if (_input.GetSpeedReport() && _started && _lap <= _nrOfLaps)
            {
                var speedKmh = _car.SpeedKmh;
                var rpm = _car.EngineRpm;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var speedMph = speedKmh * KmToMiles;
                    SpeakText($"{speedMph:F0} miles per hour, {rpm:F0} RPM");
                }
                else
                {
                    SpeakText($"{speedKmh:F0} kilometers per hour, {rpm:F0} RPM");
                }
            }
        }

        protected void HandleDistanceReportRequest()
        {
            if (_input.GetDistanceReport() && _started && _lap <= _nrOfLaps)
            {
                var distanceM = _car.DistanceMeters;
                if (_settings.Units == UnitSystem.Imperial)
                {
                    var distanceMiles = distanceM / MetersPerMile;
                    if (distanceMiles >= 1f)
                        SpeakText($"{distanceMiles:F1} miles traveled");
                    else
                        SpeakText($"{distanceM * MetersToFeet:F0} feet traveled");
                }
                else
                {
                    var distanceKm = distanceM / 1000f;
                    if (distanceKm >= 1f)
                        SpeakText($"{distanceKm:F1} kilometers traveled");
                    else
                        SpeakText($"{distanceM:F0} meters traveled");
                }
            }
        }

        protected void HandlePauseRequest(ref bool pauseKeyReleased)
        {
            if (!_input.GetPause() && !pauseKeyReleased)
            {
                pauseKeyReleased = true;
            }
            else if (_input.GetPause() && pauseKeyReleased && _started && _lap <= _nrOfLaps && _car.State == CarState.Running)
            {
                pauseKeyReleased = false;
                PauseRequested = true;
            }
        }
    }
}
