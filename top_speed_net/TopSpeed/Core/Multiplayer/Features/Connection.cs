namespace TopSpeed.Core.Multiplayer
{
    internal sealed class ConnectionFlow
    {
        private readonly MultiplayerCoordinator _owner;

        public ConnectionFlow(MultiplayerCoordinator owner)
        {
            _owner = owner;
        }

        public void BeginManualServerEntry()
        {
            _owner.BeginManualServerEntryCore();
        }

        public void BeginServerPortEntry()
        {
            _owner.BeginServerPortEntryCore();
        }

        public void StartServerDiscovery()
        {
            _owner.StartServerDiscoveryCore();
        }

        public bool UpdatePendingOperations()
        {
            return _owner.UpdatePendingOperationsCore();
        }

        public void HandlePingReply(long receivedUtcTicks)
        {
            _owner.HandlePingReplyCore(receivedUtcTicks);
        }
    }
}

