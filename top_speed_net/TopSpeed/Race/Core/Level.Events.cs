using System;
using System.Collections.Generic;
using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Race.Events;
using TopSpeed.Tracks;

namespace TopSpeed.Race
{
    internal abstract partial class Level
    {
        protected void SayTime(int raceTime, bool detailed = true)
        {
            var minutes = raceTime / 60000;
            var seconds = (raceTime % 60000) / 1000;

            if (minutes != 0)
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[minutes]);
                _sayTimeLength += _soundNumbers[minutes].GetLengthSeconds();
                if (minutes == 1)
                {
                    PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundMinute);
                    _sayTimeLength += _soundMinute.GetLengthSeconds();
                }
                else
                {
                    PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundMinutes);
                    _sayTimeLength += _soundMinutes.GetLengthSeconds();
                }
            }

            PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[seconds]);
            _sayTimeLength += _soundNumbers[seconds].GetLengthSeconds();

            if (detailed)
            {
                var tens = ((raceTime % 60000) / 100) % 10;
                var hundreds = ((raceTime % 60000) / 10) % 10;
                var thousands = (raceTime % 60000) % 10;

                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundPoint);
                _sayTimeLength += _soundPoint.GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[tens]);
                _sayTimeLength += _soundNumbers[tens].GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[hundreds]);
                _sayTimeLength += _soundNumbers[hundreds].GetLengthSeconds();
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundNumbers[thousands]);
                _sayTimeLength += _soundNumbers[thousands].GetLengthSeconds();
            }

            if (!detailed && seconds == 1)
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundSecond);
                _sayTimeLength += _soundSecond.GetLengthSeconds();
            }
            else
            {
                PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundSeconds);
                _sayTimeLength += _soundSeconds.GetLengthSeconds();
            }
        }

        protected void AppendDefaultRaceFinishAnnouncement()
        {
            PushEvent(RaceEventType.PlaySound, _sayTimeLength, _soundYourTime);
            _sayTimeLength += _soundYourTime.GetLengthSeconds() + 0.5f;
            SayTime(_raceTime);
        }

        protected virtual void OnCarStartEvent()
        {
            // Vehicle start is manual.
        }

        protected virtual void OnRaceStartEvent()
        {
            _raceTime = 0;
            _stopwatch.Restart();
            _lap = 0;
            _started = true;
        }

        protected virtual void OnRaceFinishEvent()
        {
            AppendDefaultRaceFinishAnnouncement();
            PushEvent(RaceEventType.RaceTimeFinalize, _sayTimeLength);
        }

        protected virtual void OnRaceTimeFinalizeEvent()
        {
            _sayTimeLength = 0.0f;
        }

        protected bool HandleSharedLifecycleEvent(RaceEvent e)
        {
            if (e == null)
                return false;

            switch (e.Type)
            {
                case RaceEventType.CarStart:
                    OnCarStartEvent();
                    return true;
                case RaceEventType.RaceStart:
                    OnRaceStartEvent();
                    return true;
                case RaceEventType.RaceFinish:
                    OnRaceFinishEvent();
                    return true;
                case RaceEventType.RaceTimeFinalize:
                    OnRaceTimeFinalizeEvent();
                    return true;
                default:
                    return false;
            }
        }

        protected void CallNextRoad(Track.Road nextRoad)
        {
            if (_currentRoad.Type != TrackType.Straight &&
                nextRoad.Type == TrackType.Straight &&
                _soundTurnEndDing != null)
            {
                QueueSound(_soundTurnEndDing);
            }

            if ((int)_settings.Copilot > 0 && nextRoad.Type != TrackType.Straight)
            {
                var index = (int)nextRoad.Type - 1;
                if (index >= 0 && index < RandomSoundGroups && _totalRandomSounds[index] > 0)
                {
                    var sound = _randomSounds[index][Algorithm.RandomInt(_totalRandomSounds[index])];
                    QueueSound(sound);
                }
            }

            if ((int)_settings.Copilot > 1 && nextRoad.Surface != _currentRoad.Surface)
            {
                var index = (int)nextRoad.Surface + 8;
                if (index >= 0 && index < RandomSoundGroups && _totalRandomSounds[index] > 0)
                {
                    var sound = _randomSounds[index][Algorithm.RandomInt(_totalRandomSounds[index])];
                    PushEvent(RaceEventType.PlaySound, 1.0f, sound);
                }
            }

            _currentRoad = nextRoad;
        }

        protected void PushEvent(RaceEventType type, float time, TS.Audio.AudioSourceHandle? sound = null)
        {
            _events.Add(new RaceEvent
            {
                Type = type,
                Time = _elapsedTotal + time,
                Sound = sound,
                Sequence = _eventSequence++
            });
        }

        protected List<RaceEvent> CollectDueEvents()
        {
            _dueEvents.Clear();
            for (var i = _events.Count - 1; i >= 0; i--)
            {
                var e = _events[i];
                if (e.Time <= _elapsedTotal)
                {
                    _events.RemoveAt(i);
                    _dueEvents.Add(e);
                }
            }

            if (_dueEvents.Count > 1)
            {
                _dueEvents.Sort((a, b) =>
                {
                    var timeCompare = a.Time.CompareTo(b.Time);
                    return timeCompare != 0 ? timeCompare : a.Sequence.CompareTo(b.Sequence);
                });
            }

            return _dueEvents;
        }
    }
}
