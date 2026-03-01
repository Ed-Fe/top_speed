using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal sealed partial class LevelMultiplayer
    {
        private void UpdatePositions()
        {
            _position = 1;
            foreach (var remote in _remotePlayers.Values)
            {
                if (remote.Player.PositionY > _car.PositionY)
                    _position++;
            }
        }

        private void Comment(bool automatic)
        {
            if (!_started || _lap > _nrOfLaps)
                return;

            var position = 1;
            var inFrontNumber = -1;
            var inFrontDist = 500.0f;
            var onTailNumber = -1;
            var onTailDist = 500.0f;

            foreach (var remote in _remotePlayers.Values)
            {
                var bot = remote.Player;
                if (bot.PositionY > _car.PositionY)
                {
                    position++;
                }

                var delta = GetRelativeRaceDelta(bot.PositionY);
                if (delta > 0f)
                {
                    var dist = delta;
                    if (dist < inFrontDist)
                    {
                        inFrontNumber = bot.PlayerNumber;
                        inFrontDist = dist;
                    }
                }
                else if (delta < 0f)
                {
                    var dist = -delta;
                    if (dist < onTailDist)
                    {
                        onTailNumber = bot.PlayerNumber;
                        onTailDist = dist;
                    }
                }
            }

            if (automatic && position != _positionComment)
            {
                if (position - 1 < _soundPosition.Length)
                    SpeakIfLoaded(_soundPosition[position - 1], true);
                _positionComment = position;
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFrontNumber != -1 && inFrontNumber < _soundPlayerNr.Length)
                {
                    SpeakIfLoaded(_soundPlayerNr[inFrontNumber], true);
                    var sound = _randomSounds[(int)RandomSound.Front][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Front])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }
            else
            {
                if (onTailNumber != -1 && onTailNumber < _soundPlayerNr.Length)
                {
                    SpeakIfLoaded(_soundPlayerNr[onTailNumber], true);
                    var sound = _randomSounds[(int)RandomSound.Tail][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Tail])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }

            if (inFrontNumber == -1 && onTailNumber == -1 && !automatic)
            {
                if (position - 1 < _soundPosition.Length)
                    SpeakIfLoaded(_soundPosition[position - 1], true);
                _positionComment = position;
            }
        }

        private int CalculatePlayerPerc(int player)
        {
            if (player == _playerNumber)
                return ClampPercent(_car.PositionY);

            var targetNumber = (byte)player;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return ClampPercent(remote.Player.PositionY);

            return 0;
        }

        private int ClampPercent(float positionY)
        {
            var perc = (int)((positionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            if (perc > 100)
                perc = 100;
            if (perc < 0)
                perc = 0;
            return perc;
        }

        private float GetRaceDistance()
        {
            if (_track.Length <= 0f)
                return 0f;

            var laps = _nrOfLaps > 0 ? _nrOfLaps : 1;
            return _track.Length * laps;
        }

        private float GetSpatialTrackLength()
        {
            var raceDistance = GetRaceDistance();
            if (raceDistance <= 0f)
                return _track.Length;
            return raceDistance * 2f;
        }

        private float GetRelativeRaceDelta(float otherPositionY)
        {
            return otherPositionY - _car.PositionY;
        }

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == _playerNumber)
            {
                if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                    return FormatVehicleName(_car.CustomFile);
                return _car.VehicleName;
            }

            var targetNumber = (byte)playerIndex;
            if (_remotePlayers.TryGetValue(targetNumber, out var remote))
                return VehicleCatalog.Vehicles[remote.Player.VehicleIndex].Name;

            return "Vehicle";
        }

        private bool HasPlayerInRace(int playerIndex)
        {
            if (playerIndex == _playerNumber)
                return true;

            if (playerIndex < 0 || playerIndex >= MaxPlayers)
                return false;

            return _remotePlayers.ContainsKey((byte)playerIndex);
        }
    }
}
