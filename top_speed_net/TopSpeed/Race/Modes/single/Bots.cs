using TopSpeed.Common;
using TopSpeed.Data;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal sealed partial class LevelSingleRace
    {
        private ComputerPlayer GenerateRandomPlayer(int playerNumber)
        {
            var vehicleIndex = Algorithm.RandomInt(VehicleCatalog.VehicleCount);
            return new ComputerPlayer(
                _audio,
                _track,
                _settings,
                vehicleIndex,
                playerNumber,
                () => _elapsedTotal,
                () => _started,
                null);
        }

        private void UpdatePositions()
        {
            _position = 1;
            for (var i = 0; i < _nComputerPlayers; i++)
            {
                if (_computerPlayers[i]?.PositionY > _car.PositionY)
                    _position++;
            }
        }
    }
}
