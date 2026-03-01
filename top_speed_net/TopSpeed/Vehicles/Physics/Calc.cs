using System;
using TopSpeed.Common;
using TopSpeed.Data;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void GuardDynamicInputs()
        {
            if (!IsFinite(_speed))
                _speed = 0f;
            if (!IsFinite(_positionX))
                _positionX = 0f;
            if (!IsFinite(_positionY))
                _positionY = 0f;
            if (_positionY < 0f)
                _positionY = 0f;
        }

        private void ApplySurfaceModifiers()
        {
            _currentSurfaceTractionFactor = _surfaceTractionFactor;
            _currentDeceleration = _deceleration;
            _speedDiff = 0f;

            switch (_surface)
            {
                case TrackSurface.Gravel:
                    _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 2f) / 3f;
                    _currentDeceleration = (_currentDeceleration * 2f) / 3f;
                    break;
                case TrackSurface.Water:
                    _currentSurfaceTractionFactor = (_currentSurfaceTractionFactor * 3f) / 5f;
                    _currentDeceleration = (_currentDeceleration * 3f) / 5f;
                    break;
                case TrackSurface.Sand:
                    _currentSurfaceTractionFactor *= 0.5f;
                    _currentDeceleration = (_currentDeceleration * 3f) / 2f;
                    break;
                case TrackSurface.Snow:
                    _currentDeceleration *= 0.5f;
                    break;
            }
        }

        private int ResolveThrust()
        {
            if (_currentThrottle == 0)
                return _currentBrake;
            if (_currentBrake == 0)
                return _currentThrottle;
            return -_currentBrake > _currentThrottle ? _currentBrake : _currentThrottle;
        }

        private void ApplyThrottleDrive(
            float elapsed,
            float speedMpsCurrent,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            ref float longitudinalGripFactor)
        {
            if (reverseBlockedAtLapStart)
            {
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
                return;
            }

            var steeringCommandAccel = (_currentSteering / 100.0f) * _steering;
            if (steeringCommandAccel > 1.0f)
                steeringCommandAccel = 1.0f;
            else if (steeringCommandAccel < -1.0f)
                steeringCommandAccel = -1.0f;

            var steerRadAccel = (float)(Math.PI / 180.0) * (_maxSteerDeg * steeringCommandAccel);
            var curvatureAccel = (float)Math.Tan(steerRadAccel) / _wheelbaseM;
            var desiredLatAccel = curvatureAccel * speedMpsCurrent * speedMpsCurrent;
            var desiredLatAccelAbs = Math.Abs(desiredLatAccel);
            var grip = _tireGripCoefficient * surfaceTractionMod * _lateralGripCoefficient;
            var maxLatAccel = grip * 9.80665f;
            var lateralRatio = maxLatAccel > 0f ? Math.Min(1.0f, desiredLatAccelAbs / maxLatAccel) : 0f;
            longitudinalGripFactor = (float)Math.Sqrt(Math.Max(0.0, 1.0 - (lateralRatio * lateralRatio)));

            var driveRpm = CalculateDriveRpm(speedMpsCurrent, throttle);
            var engineTorque = CalculateEngineTorqueNm(driveRpm) * throttle * _powerFactor;
            var gearRatio = inReverse ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
            var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;
            wheelForce *= (_factor1 / 100f);
            if (inReverse)
                wheelForce *= _reversePowerFactor;

            var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMpsCurrent * speedMpsCurrent;
            var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            var accelMps2 = netForce / _massKg;
            var newSpeedMps = speedMpsCurrent + (accelMps2 * elapsed);
            if (newSpeedMps < 0f)
                newSpeedMps = 0f;

            _speedDiff = (newSpeedMps - speedMpsCurrent) * 3.6f;
            _lastDriveRpm = CalculateDriveRpm(newSpeedMps, throttle);
            if (_backfirePlayed)
                _backfirePlayed = false;
        }

        private void ApplyCoastDecel(float elapsed)
        {
            var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
            var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var brakeDecel = CalculateBrakeDecel(brakeInput, surfaceDecelMod);
            var engineBrakeDecel = CalculateEngineBrakingDecel(surfaceDecelMod);
            var totalDecel = _thrust < -10 ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
            _speedDiff = -totalDecel * elapsed;
            _lastDriveRpm = 0f;
        }

        private void ClampSpeedAndTransmission(
            float elapsed,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            _speed += _speedDiff;
            if (_speed > _topSpeed)
                _speed = _topSpeed;
            if (_speed < 0f)
                _speed = 0f;
            if (!IsFinite(_speed))
            {
                _speed = 0f;
                _speedDiff = 0f;
            }

            if (!IsFinite(_lastDriveRpm))
                _lastDriveRpm = _idleRpm;

            if (reverseBlockedAtLapStart && _thrust > 10f)
            {
                _speed = 0f;
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
            }

            if (inReverse)
            {
                var reverseMax = Math.Max(5.0f, _reverseMaxSpeedKph);
                if (_speed > reverseMax)
                    _speed = reverseMax;
                return;
            }

            if (_manualTransmission)
            {
                var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                if (_speed > gearMax)
                    _speed = gearMax;
            }
            else
            {
                UpdateAutomaticGear(elapsed, _speed / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
            }
        }

        private void SyncEngineFromSpeed(float elapsed)
        {
            _engine.SyncFromSpeed(_speed, GetDriveGear(), elapsed, _currentThrottle);
            if (_lastDriveRpm > 0f && _lastDriveRpm > _engine.Rpm)
                _engine.OverrideRpm(_lastDriveRpm);
        }

        private void UpdateBackfireStateAfterDrive()
        {
            if (_thrust > 0)
                return;

            if (!AnyBackfirePlaying() && !_backfirePlayed && Algorithm.RandomInt(5) == 1)
                PlayRandomBackfire();
            _backfirePlayed = true;
        }

        private void IntegrateVehiclePosition(float elapsed, float currentLapStart)
        {
            var speedMps = _speed / 3.6f;
            var longitudinalDelta = speedMps * elapsed;
            if (_gear == ReverseGear)
            {
                var nextPositionY = _positionY - longitudinalDelta;
                if (nextPositionY < currentLapStart)
                    nextPositionY = currentLapStart;
                if (nextPositionY < 0f)
                    nextPositionY = 0f;
                _positionY = nextPositionY;
            }
            else
            {
                _positionY += longitudinalDelta;
            }

            var surfaceMultiplier = _surface == TrackSurface.Snow ? 1.44f : 1.0f;
            var steeringCommandLat = (_currentSteering / 100.0f) * _steering;
            if (steeringCommandLat > 1.0f)
                steeringCommandLat = 1.0f;
            else if (steeringCommandLat < -1.0f)
                steeringCommandLat = -1.0f;
            var steerRadLat = (float)(Math.PI / 180.0) * (_maxSteerDeg * steeringCommandLat);
            var curvatureLat = (float)Math.Tan(steerRadLat) / _wheelbaseM;
            var surfaceTractionModLat = _surfaceTractionFactor > 0f ? _currentSurfaceTractionFactor / _surfaceTractionFactor : 1.0f;
            var gripLat = _tireGripCoefficient * surfaceTractionModLat * _lateralGripCoefficient;
            var maxLatAccelLat = gripLat * 9.80665f;
            var desiredLatAccelLat = curvatureLat * speedMps * speedMps;
            var massFactor = (float)Math.Sqrt(1500f / _massKg);
            if (massFactor > 3.0f)
                massFactor = 3.0f;
            var stabilityScale = 1.0f - (_highSpeedStability * (speedMps / StabilitySpeedRef) * massFactor);
            if (stabilityScale < 0.2f)
                stabilityScale = 0.2f;
            else if (stabilityScale > 1.0f)
                stabilityScale = 1.0f;
            var responseTime = BaseLateralSpeed / 20.0f;
            var maxLatSpeed = maxLatAccelLat * responseTime * stabilityScale;
            var desiredLatSpeed = desiredLatAccelLat * responseTime;
            if (desiredLatSpeed > maxLatSpeed)
                desiredLatSpeed = maxLatSpeed;
            else if (desiredLatSpeed < -maxLatSpeed)
                desiredLatSpeed = -maxLatSpeed;
            var lateralSpeed = desiredLatSpeed * surfaceMultiplier;
            _positionX += lateralSpeed * elapsed;
        }
    }
}
