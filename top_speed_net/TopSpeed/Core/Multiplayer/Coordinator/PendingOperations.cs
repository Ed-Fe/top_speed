using System;
using System.Collections.Generic;
using TopSpeed.Network;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public bool UpdatePendingOperations()
        {
            return _connectionFlow.UpdatePendingOperations();
        }

        internal bool UpdatePendingOperationsCore()
        {
            if (_state.Connection.ConnectTask != null)
            {
                if (!_state.Connection.ConnectTask.IsCompleted)
                    return true;

                var result = _state.Connection.ConnectTask.IsFaulted || _state.Connection.ConnectTask.IsCanceled
                    ? ConnectResult.CreateFail("Connection attempt failed.")
                    : _state.Connection.ConnectTask.GetAwaiter().GetResult();
                _lifetime.CompleteConnectOperation();
                HandleConnectResult(result);
                return false;
            }

            if (_state.Connection.DiscoveryTask != null)
            {
                if (!_state.Connection.DiscoveryTask.IsCompleted)
                    return true;

                IReadOnlyList<ServerInfo> servers;
                if (_state.Connection.DiscoveryTask.IsFaulted || _state.Connection.DiscoveryTask.IsCanceled)
                    servers = Array.Empty<ServerInfo>();
                else
                    servers = _state.Connection.DiscoveryTask.GetAwaiter().GetResult();

                _lifetime.CompleteDiscoveryOperation();
                HandleDiscoveryResult(servers);
                return false;
            }

            return false;
        }
    }
}


