using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Network;
using TS.Audio;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class RuntimeLifetime
    {
        private readonly CoordinatorState _state;

        public RuntimeLifetime(CoordinatorState state)
        {
            _state = state;
        }

        public CancellationTokenSource BeginConnectOperation()
        {
            CancelConnectOperation();
            var cts = new CancellationTokenSource();
            _state.Connection.ConnectCts = cts;
            return cts;
        }

        public void SetConnectTask(Task<ConnectResult> task)
        {
            _state.Connection.ConnectTask = task;
        }

        public void CompleteConnectOperation()
        {
            _state.Connection.ConnectTask = null;
            DisposeToken(ref _state.Connection.ConnectCts);
        }

        public void CancelConnectOperation()
        {
            _state.Connection.ConnectTask = null;
            CancelAndDisposeToken(ref _state.Connection.ConnectCts);
        }

        public CancellationTokenSource BeginDiscoveryOperation()
        {
            CancelDiscoveryOperation();
            var cts = new CancellationTokenSource();
            _state.Connection.DiscoveryCts = cts;
            return cts;
        }

        public void SetDiscoveryTask(Task<IReadOnlyList<ServerInfo>> task)
        {
            _state.Connection.DiscoveryTask = task;
        }

        public void CompleteDiscoveryOperation()
        {
            _state.Connection.DiscoveryTask = null;
            DisposeToken(ref _state.Connection.DiscoveryCts);
        }

        public void CancelDiscoveryOperation()
        {
            _state.Connection.DiscoveryTask = null;
            CancelAndDisposeToken(ref _state.Connection.DiscoveryCts);
        }

        public CancellationToken BeginConnectingPulse()
        {
            StopConnectingPulse();
            var cts = new CancellationTokenSource();
            _state.Audio.ConnectingPulseCts = cts;
            return cts.Token;
        }

        public void StopConnectingPulse()
        {
            CancelAndDisposeToken(ref _state.Audio.ConnectingPulseCts);
            StopAudio(_state.Audio.ConnectingSound);
        }

        public void ResetPing()
        {
            _state.Connection.IsPingPending = false;
            _state.Connection.PingStartedAtTicks = 0;
        }

        public void StopNetworkAudio()
        {
            StopAudio(_state.Audio.ConnectingSound);
            StopAudio(_state.Audio.ConnectedSound);
            StopAudio(_state.Audio.OnlineSound);
            StopAudio(_state.Audio.OfflineSound);
            StopAudio(_state.Audio.PingStartSound);
            StopAudio(_state.Audio.PingSound);
            StopAudio(_state.Audio.RoomCreatedSound);
            StopAudio(_state.Audio.RoomJoinSound);
            StopAudio(_state.Audio.RoomLeaveSound);
            StopAudio(_state.Audio.ChatSound);
            StopAudio(_state.Audio.RoomChatSound);
            StopAudio(_state.Audio.BufferSwitchSound);
        }

        public void CancelAllOperations()
        {
            CancelConnectOperation();
            CancelDiscoveryOperation();
            StopConnectingPulse();
        }

        private static void StopAudio(AudioSourceHandle? handle)
        {
            try
            {
                handle?.Stop();
            }
            catch
            {
            }
        }

        private static void CancelAndDisposeToken(ref CancellationTokenSource? cts)
        {
            try
            {
                cts?.Cancel();
            }
            catch
            {
            }

            DisposeToken(ref cts);
        }

        private static void DisposeToken(ref CancellationTokenSource? cts)
        {
            try
            {
                cts?.Dispose();
            }
            catch
            {
            }
            finally
            {
                cts = null;
            }
        }
    }
}
