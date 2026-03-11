using System;
using System.Net;
using TopSpeed.Protocol;
using TopSpeed.Server.Protocol;

namespace TopSpeed.Server.Network
{
    internal sealed partial class RaceServer
    {
        private bool HandlePendingHandshake(PlayerConnection player, Command command, byte[] payload, IPEndPoint endPoint)
        {
            if (player.Handshake == HandshakeState.Complete)
                return false;
            if (player.Handshake == HandshakeState.Rejected)
                return true;

            if (command == Command.KeepAlive)
                return true;

            if (command != Command.ProtocolHello)
            {
                RejectHandshake(player, $"Protocol negotiation is required before {command}. Please update your client.");
                return true;
            }

            if (!PacketSerializer.TryReadProtocolHello(payload, out var hello))
            {
                RejectHandshake(player, "Invalid protocol handshake packet.");
                PacketFail(endPoint, Command.ProtocolHello);
                return true;
            }

            EvaluateProtocolHello(player, hello);
            return true;
        }

        private void EvaluateProtocolHello(PlayerConnection player, PacketProtocolHello hello)
        {
            ProtocolRange clientRange;
            try
            {
                clientRange = new ProtocolRange(hello.MinSupported, hello.MaxSupported);
            }
            catch (ArgumentException)
            {
                RejectHandshake(player, "Invalid protocol range in handshake packet.");
                return;
            }

            player.ClientVersion = hello.ClientVersion;
            player.ClientSupportedRange = clientRange;

            var serverRange = ProtocolProfile.ServerSupported;
            var compat = ProtocolCompat.Resolve(clientRange, serverRange);

            var welcome = new PacketProtocolWelcome
            {
                Status = compat.Status,
                NegotiatedVersion = compat.NegotiatedVersion,
                ServerMinSupported = serverRange.MinSupported,
                ServerMaxSupported = serverRange.MaxSupported,
                Message = BuildHandshakeMessage(compat.Status, hello.ClientVersion, serverRange)
            };

            SendStream(player, PacketSerializer.WriteProtocolWelcome(welcome), PacketStream.Control);

            if (!compat.IsCompatible)
            {
                _logger.Warning(
                    $"Protocol rejected: player={player.Id}, endpoint={player.EndPoint}, clientVersion={hello.ClientVersion}, " +
                    $"clientSupported={clientRange}, serverSupported={serverRange}, status={compat.Status}.");
                player.Handshake = HandshakeState.Rejected;
                RemoveConnection(
                    player,
                    notifyRoom: false,
                    sendDisconnectPacket: true,
                    reason: "protocol_mismatch",
                    disconnectMessage: welcome.Message);
                return;
            }

            player.Handshake = HandshakeState.Complete;
            player.NegotiatedProtocol = compat.NegotiatedVersion;
            SendInitialConnectionState(player);
        }

        private void RejectHandshake(PlayerConnection player, string message)
        {
            var serverRange = ProtocolProfile.ServerSupported;
            var welcome = new PacketProtocolWelcome
            {
                Status = ProtocolCompatStatus.NoCommonVersion,
                NegotiatedVersion = default,
                ServerMinSupported = serverRange.MinSupported,
                ServerMaxSupported = serverRange.MaxSupported,
                Message = message ?? "Protocol negotiation failed."
            };
            _logger.Warning(
                $"Protocol rejected: player={player.Id}, endpoint={player.EndPoint}, " +
                $"clientVersion={player.ClientVersion}, clientSupported={player.ClientSupportedRange}, " +
                $"serverSupported={serverRange}, reason={welcome.Message}");

            SendStream(player, PacketSerializer.WriteProtocolWelcome(welcome), PacketStream.Control);
            SendProtocolMessage(player, ProtocolMessageCode.Failed, message ?? "Connection refused due to protocol mismatch.");
            player.Handshake = HandshakeState.Rejected;
            RemoveConnection(
                player,
                notifyRoom: false,
                sendDisconnectPacket: true,
                reason: "protocol_rejected",
                disconnectMessage: welcome.Message);
        }

        private void SendInitialConnectionState(PlayerConnection player)
        {
            SendStream(player, PacketSerializer.WritePlayerNumber(player.Id, 0), PacketStream.Control);
            if (!string.IsNullOrWhiteSpace(_config.Motd))
                SendStream(player, PacketSerializer.WriteServerInfo(new PacketServerInfo { Motd = _config.Motd }), PacketStream.Control);
            SendRoomState(player, null);
            _logger.Info($"Connection established: playerId={player.Id}, endpoint={player.EndPoint}, protocol={player.NegotiatedProtocol}.");
        }

        private static string BuildHandshakeMessage(ProtocolCompatStatus status, ProtocolVer clientVersion, ProtocolRange serverRange)
        {
            switch (status)
            {
                case ProtocolCompatStatus.Exact:
                    return "Protocol compatibility verified.";
                case ProtocolCompatStatus.CompatibleDowngrade:
                    if (clientVersion > serverRange.MaxSupported)
                        return $"Your client protocol version is newer than this server: {clientVersion}. This server supports protocol versions {serverRange}. You can continue, but some features may behave differently or may not work at all.";

                    return $"Your client protocol version is older than this server: {clientVersion}. This server supports protocol versions {serverRange}. You can continue, but some features may behave differently or may not work at all.";
                case ProtocolCompatStatus.ClientTooOld:
                    return $"Your client protocol version is out-of-date: {clientVersion}. This server supports protocol versions {serverRange}. Please update your client.";
                case ProtocolCompatStatus.ClientTooNew:
                    return $"Your client protocol version is too new: {clientVersion} and does not match this server protocol. This server supports protocol versions {serverRange}. The server needs an update.";
                default:
                    return $"Your client protocol version is {clientVersion}. This server supports protocol versions {serverRange}.";
            }
        }
    }
}
