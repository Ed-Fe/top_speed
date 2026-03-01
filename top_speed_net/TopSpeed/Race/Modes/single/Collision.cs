using System;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal sealed partial class LevelSingleRace
    {
        private void CheckForBumps()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                var bot = _computerPlayers[i];
                if (bot == null)
                    continue;
                if (_car.State == CarState.Running && !bot.Finished)
                {
                    var dx = _car.PositionX - bot.PositionX;
                    var dy = _car.PositionY - bot.PositionY;
                    var xThreshold = (_car.WidthM + bot.WidthM) * 0.5f;
                    var yThreshold = (_car.LengthM + bot.LengthM) * 0.5f;
                    if (Math.Abs(dx) < xThreshold && Math.Abs(dy) < yThreshold)
                    {
                        var bumpX = dx;
                        var bumpY = dy;
                        var bumpSpeed = _car.Speed - bot.Speed;
                        _car.Bump(bumpX, bumpY, bumpSpeed);
                        bot.Bump(-bumpX, -bumpY, -bumpSpeed);
                    }
                }
            }
        }

        private bool CheckFinish()
        {
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.Finished == false)
                    return false;
            }
            if (_lap <= _nrOfLaps)
                return false;
            return true;
        }
    }
}
