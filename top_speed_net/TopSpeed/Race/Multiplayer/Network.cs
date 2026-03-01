using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race.Multiplayer;
using TopSpeed.Vehicles;

namespace TopSpeed.Race
{
    internal sealed partial class LevelMultiplayer
    {
        public void ApplyRemoteData(PacketPlayerData data)
        {
            ApplyRemoteDataCore(
                data.PlayerNumber,
                data.Car,
                data.State,
                data.RaceData.PositionX,
                data.RaceData.PositionY,
                data.RaceData.Speed,
                data.RaceData.Frequency,
                data.EngineRunning,
                data.Braking,
                data.Horning,
                data.Backfiring,
                data.MediaLoaded,
                data.MediaPlaying,
                data.MediaId);
        }

        public void ApplyBump(PacketPlayerBumped bump)
        {
            if (bump.PlayerNumber != _playerNumber)
                return;
            _car.Bump(bump.BumpX, bump.BumpY, bump.BumpSpeed);
        }

        public void ApplyRemoteCrash(PacketPlayer crashed)
        {
            if (crashed.PlayerNumber == _playerNumber)
                return;
            if (crashed.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[crashed.PlayerNumber])
                return;
            if (_remotePlayers.TryGetValue(crashed.PlayerNumber, out var remote))
                remote.Player.Crash(remote.Player.PositionX, scheduleRestart: false);
        }

        public void ApplyRemoteFinish(PacketPlayer finished)
        {
            if (finished.PlayerNumber == _playerNumber)
                return;
            if (finished.PlayerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[finished.PlayerNumber])
                return;
            if (!_remotePlayers.TryGetValue(finished.PlayerNumber, out var remote))
                return;
            if (remote.Finished)
                return;

            remote.Finished = true;
            remote.State = PlayerState.Finished;
            remote.Player.Stop();
            AnnounceFinishOrder(_soundPlayerNr, _soundFinished, finished.PlayerNumber, ref _positionFinish);
        }

        public void RemoveRemotePlayer(byte playerNumber)
        {
            if (playerNumber < _disconnectedPlayerSlots.Length)
                _disconnectedPlayerSlots[playerNumber] = true;

            _remoteMediaTransfers.Remove(playerNumber);
            if (_remotePlayers.TryGetValue(playerNumber, out var remote))
            {
                remote.Player.FinalizePlayer();
                remote.Player.Dispose();
                _remotePlayers.Remove(playerNumber);
            }

            RemovePlayerFromSnapshotFrames(playerNumber);
        }

        public void HandleServerRaceStopped(PacketRaceResults _)
        {
            if (_serverStopReceived)
                return;

            _serverStopReceived = true;
            _snapshotFrames.Clear();
            _hasSnapshotTickNow = false;
            if (!_sentFinish)
            {
                _sentFinish = true;
                _currentState = PlayerState.Finished;
                TrySendRace(_session.SendPlayerState(_currentState), "finished state");
            }

            RequestExitWhenQueueIdle();
        }

        private RemotePlayer GetOrCreateRemotePlayer(byte playerNumber, CarType car, float positionX, float positionY)
        {
            if (_remotePlayers.TryGetValue(playerNumber, out var existing))
                return existing;

            var vehicleIndex = car == CarType.CustomVehicle ? 0 : (int)car;
            var bot = new ComputerPlayer(_audio, _track, _settings, vehicleIndex, playerNumber, () => _elapsedTotal, () => _started);
            bot.Initialize(positionX, positionY, GetSpatialTrackLength());
            var remote = new RemotePlayer(bot);
            _remotePlayers[playerNumber] = remote;
            return remote;
        }

        private void TryApplyPendingRemoteMedia(byte playerNumber, RemotePlayer remote)
        {
            if (!_remoteMediaTransfers.TryGetValue(playerNumber, out var transfer))
                return;
            if (!transfer.IsComplete)
                return;

            remote.Player.ApplyRadioMedia(transfer.MediaId, transfer.Extension, transfer.Data);
            _remoteMediaTransfers.Remove(playerNumber);
        }

        private void ApplyRemoteDataCore(
            byte playerNumber,
            CarType car,
            PlayerState state,
            float positionX,
            float positionY,
            ushort speed,
            int frequency,
            bool engineRunning,
            bool braking,
            bool horning,
            bool backfiring,
            bool mediaLoaded,
            bool mediaPlaying,
            uint mediaId)
        {
            if (playerNumber == _playerNumber)
                return;
            if (playerNumber < _disconnectedPlayerSlots.Length && _disconnectedPlayerSlots[playerNumber])
                return;

            var remote = GetOrCreateRemotePlayer(playerNumber, car, positionX, positionY);
            remote.State = state;
            if (state == PlayerState.Finished && !remote.Finished)
            {
                remote.Finished = true;
                remote.Player.Stop();
                AnnounceFinishOrder(_soundPlayerNr, _soundFinished, playerNumber, ref _positionFinish);
            }

            remote.Player.ApplyNetworkState(
                positionX,
                positionY,
                speed,
                frequency,
                engineRunning,
                braking,
                horning,
                backfiring,
                mediaLoaded,
                mediaPlaying,
                mediaId,
                _car.PositionX,
                _car.PositionY,
                GetSpatialTrackLength());
            TryApplyPendingRemoteMedia(playerNumber, remote);
        }
    }
}
