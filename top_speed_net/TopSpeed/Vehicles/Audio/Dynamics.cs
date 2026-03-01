using System;
using TopSpeed.Audio;
using TopSpeed.Input;
using TopSpeed.Data;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void UpdateThrottleLoopAudio(float elapsed)
        {
            if (_soundThrottle == null)
                return;

            if (_soundEngine.IsPlaying)
            {
                if (_currentThrottle > 50)
                {
                    if (!_soundThrottle.IsPlaying)
                    {
                        if (_throttleVolume < 80.0f)
                            _throttleVolume = 80.0f;
                        SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                        _prevThrottleVolume = _throttleVolume;
                        _soundThrottle.Play(loop: true);
                    }
                    else
                    {
                        if (_throttleVolume >= 80.0f)
                            _throttleVolume += (100.0f - _throttleVolume) * elapsed;
                        else
                            _throttleVolume = 80.0f;
                        if (_throttleVolume > 100.0f)
                            _throttleVolume = 100.0f;
                        if ((int)_throttleVolume != (int)_prevThrottleVolume)
                        {
                            SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                            _prevThrottleVolume = _throttleVolume;
                        }
                    }
                }
                else
                {
                    _throttleVolume -= 10.0f * elapsed;
                    var min = _speed * 95 / _topSpeed;
                    if (_throttleVolume < min)
                        _throttleVolume = min;
                    if ((int)_throttleVolume != (int)_prevThrottleVolume)
                    {
                        SetPlayerEngineVolumePercent(_soundThrottle, (int)_throttleVolume);
                        _prevThrottleVolume = _throttleVolume;
                    }
                }
            }
            else if (_soundThrottle.IsPlaying)
            {
                _soundThrottle.Stop();
            }
        }

        private void UpdateBrakeAndSteeringOutput()
        {
            if (_thrust < -50 && _speed > 0)
            {
                BrakeSound();
                _vibration?.Gain(VibrationEffectType.Spring, (int)(50.0f * _speed / _topSpeed));
                _currentSteering = (_currentSteering * 2) / 3;
            }
            else if (_currentSteering != 0 && _speed > _topSpeed / 2)
            {
                if (_thrust > -50)
                    BrakeCurveSound();
            }
            else
            {
                if (_soundBrake.IsPlaying)
                    _soundBrake.Stop();
                SetSurfaceLoopVolumePercent(_soundAsphalt, 90);
                SetSurfaceLoopVolumePercent(_soundGravel, 90);
                SetSurfaceLoopVolumePercent(_soundWater, 90);
                SetSurfaceLoopVolumePercent(_soundSand, 90);
                SetSurfaceLoopVolumePercent(_soundSnow, 90);
            }
        }

        private void UpdateFrameAudioAndFeedback()
        {
            if (_frame % 4 != 0)
                return;

            _frame = 0;
            _brakeFrequency = (int)(11025 + 22050 * _speed / _topSpeed);
            if (_brakeFrequency != _prevBrakeFrequency)
            {
                _soundBrake.SetFrequency(_brakeFrequency);
                _prevBrakeFrequency = _brakeFrequency;
            }

            if (_speed <= 50.0f)
                SetPlayerEventVolumePercent(_soundBrake, (int)(100 - (50 - _speed)));
            else
                SetPlayerEventVolumePercent(_soundBrake, 100);

            if (_manualTransmission)
                UpdateEngineFreqManual();
            else
                UpdateEngineFreq();
            UpdateSoundRoad();

            if (_vibration == null)
                return;

            if (_surface == TrackSurface.Gravel)
                _vibration.Gain(VibrationEffectType.Gravel, (int)(_speed * 10000 / _topSpeed));
            else
                _vibration.Gain(VibrationEffectType.Gravel, 0);

            if (_speed == 0)
                _vibration.Gain(VibrationEffectType.Spring, 10000);
            else
                _vibration.Gain(VibrationEffectType.Spring, (int)(10000 * _speed / _topSpeed));

            if (_speed < _topSpeed / 10)
                _vibration.Gain(VibrationEffectType.Engine, (int)(10000 - _speed * 10 / _topSpeed));
            else
                _vibration.Gain(VibrationEffectType.Engine, 0);
        }

        private void EnsureSurfaceLoopPlaying()
        {
            switch (_surface)
            {
                case TrackSurface.Asphalt:
                    EnsureSurfaceLoop(_soundAsphalt, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Gravel:
                    EnsureSurfaceLoop(_soundGravel, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Water:
                    EnsureSurfaceLoop(_soundWater, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
                case TrackSurface.Sand:
                    EnsureSurfaceLoop(_soundSand, (int)(_surfaceFrequency / 2.5f));
                    break;
                case TrackSurface.Snow:
                    EnsureSurfaceLoop(_soundSnow, Math.Min(_surfaceFrequency, MaxSurfaceFreq));
                    break;
            }
        }

        private static void EnsureSurfaceLoop(TS.Audio.AudioSourceHandle sound, int frequency)
        {
            if (sound.IsPlaying)
                return;
            sound.SetFrequency(frequency);
            sound.Play(loop: true);
        }
    }
}
