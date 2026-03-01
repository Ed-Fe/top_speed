using System;
using TopSpeed.Common;
using TopSpeed.Data;

namespace TopSpeed.Race
{
    internal sealed partial class LevelSingleRace
    {
        private void Comment(bool automatic)
        {
            if (!_started || _lap > _nrOfLaps)
                return;

            var position = 1;
            var inFront = -1;
            var inFrontDist = 500.0f;
            var onTail = -1;
            var onTailDist = 500.0f;

            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;

                if (bot.PositionY > _car.PositionY)
                {
                    position++;
                }

                var delta = GetRelativeTrackDelta(bot.PositionY);
                if (delta > 0f)
                {
                    var dist = delta;
                    if (dist < inFrontDist)
                    {
                        inFront = i;
                        inFrontDist = dist;
                    }
                }
                else if (delta < 0f)
                {
                    var dist = -delta;
                    if (dist < onTailDist)
                    {
                        onTail = i;
                        onTailDist = dist;
                    }
                }
            }

            if (automatic && position != _positionComment)
            {
                if (position == _nComputerPlayers + 1)
                    SpeakIfLoaded(_soundPosition[_nComputerPlayers], true);
                else
                    SpeakIfLoaded(_soundPosition[position - 1], true);
                _positionComment = position;
                return;
            }

            if (inFrontDist < onTailDist)
            {
                if (inFront != -1)
                {
                    var bot = _computerPlayers[inFront]!;
                    SpeakIfLoaded(_soundPlayerNr[bot.PlayerNumber], true);
                    var sound = _randomSounds[(int)RandomSound.Front][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Front])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }
            else
            {
                if (onTail != -1)
                {
                    var bot = _computerPlayers[onTail]!;
                    SpeakIfLoaded(_soundPlayerNr[bot.PlayerNumber], true);
                    var sound = _randomSounds[(int)RandomSound.Tail][Algorithm.RandomInt(_totalRandomSounds[(int)RandomSound.Tail])];
                    if (sound != null)
                        Speak(sound, true);
                    return;
                }
            }

            if (inFront == -1 && onTail == -1 && !automatic)
            {
                if (position == _nComputerPlayers + 1)
                    SpeakIfLoaded(_soundPosition[_nComputerPlayers], true);
                else
                    SpeakIfLoaded(_soundPosition[position - 1], true);
                _positionComment = position;
            }
        }

        private int CalculatePlayerPerc(int player)
        {
            int perc;
            if (player == _playerNumber)
                perc = (int)((_car.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            else if (player > _playerNumber)
                perc = (int)((_computerPlayers[player - 1]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            else
                perc = (int)((_computerPlayers[player]!.PositionY / (float)(_track.Length * _nrOfLaps)) * 100.0f);
            if (perc > 100)
                perc = 100;
            return perc;
        }

        private string GetVehicleNameForPlayer(int playerIndex)
        {
            if (playerIndex == _playerNumber)
            {
                if (_car.UserDefined && !string.IsNullOrWhiteSpace(_car.CustomFile))
                    return FormatVehicleName(_car.CustomFile);
                return _car.VehicleName;
            }

            if (playerIndex < _playerNumber)
            {
                var bot = _computerPlayers[playerIndex];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }
            else if (playerIndex > _playerNumber)
            {
                var bot = _computerPlayers[playerIndex - 1];
                if (bot != null)
                    return VehicleCatalog.Vehicles[bot.VehicleIndex].Name;
            }

            return "Vehicle";
        }
    }
}
