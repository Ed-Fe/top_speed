using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed partial class MultiplayerConnector
    {
        private static ResolveResult TryResolveHost(string host)
        {
            try
            {
                var addresses = Dns.GetHostAddresses(host);
                for (var i = 0; i < addresses.Length; i++)
                {
                    var candidate = addresses[i];
                    if (candidate.AddressFamily == AddressFamily.InterNetwork)
                        return ResolveResult.Ok(candidate);
                }

                return addresses.Length > 0
                    ? ResolveResult.Ok(addresses[0])
                    : ResolveResult.Fail("Unable to resolve server address.");
            }
            catch (SocketException ex)
            {
                return ResolveResult.Fail($"Unable to resolve server address: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return ResolveResult.Fail($"Unable to resolve server address: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return ResolveResult.Fail($"Unable to resolve server address: {ex.Message}");
            }
        }

        private static SendResult TrySendHandshakePacket(NetPeer peer, byte[] payload)
        {
            try
            {
                peer.Send(payload, DeliveryMethod.ReliableOrdered);
                return SendResult.Ok();
            }
            catch (ObjectDisposedException ex)
            {
                return SendResult.Fail($"Failed to send handshake: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return SendResult.Fail($"Failed to send handshake: {ex.Message}");
            }
        }

        private static void TrySendKeepAlive(NetPeer peer, byte[] payload)
        {
            try
            {
                peer.Send(payload, DeliveryMethod.Unreliable);
            }
            catch (ObjectDisposedException)
            {
                // Ignore keepalive failures during connect.
            }
            catch (InvalidOperationException)
            {
                // Ignore keepalive failures during connect.
            }
        }

        private static string SanitizeCallSign(string callSign)
        {
            var trimmed = (callSign ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                trimmed = "Player";
            if (trimmed.Length > ProtocolConstants.MaxPlayerNameLength)
                trimmed = trimmed.Substring(0, ProtocolConstants.MaxPlayerNameLength);
            return trimmed;
        }

        private static byte[] BuildPlayerHelloPacket(string callSign)
        {
            var buffer = new byte[2 + ProtocolConstants.MaxPlayerNameLength];
            var writer = new PacketWriter(buffer);
            writer.WriteByte(ProtocolConstants.Version);
            writer.WriteByte((byte)Command.PlayerHello);
            writer.WriteFixedString(callSign ?? string.Empty, ProtocolConstants.MaxPlayerNameLength);
            return buffer;
        }

        private static byte[] BuildProtocolHelloPacket()
        {
            var packet = new PacketProtocolHello
            {
                ClientVersion = ProtocolProfile.Current,
                MinSupported = ProtocolProfile.ClientSupported.MinSupported,
                MaxSupported = ProtocolProfile.ClientSupported.MaxSupported
            };
            return ClientPacketSerializer.WriteProtocolHello(packet);
        }

        private static byte[] BuildPlayerStatePacket()
        {
            var buffer = new byte[2 + 4 + 1 + 1];
            buffer[0] = ProtocolConstants.Version;
            buffer[1] = (byte)Command.PlayerState;
            var idBytes = BitConverter.GetBytes(0u);
            buffer[2] = idBytes[0];
            buffer[3] = idBytes[1];
            buffer[4] = idBytes[2];
            buffer[5] = idBytes[3];
            buffer[6] = 0;
            buffer[7] = (byte)PlayerState.NotReady;
            return buffer;
        }

        private static bool IsCompatibilityAccepted(ProtocolCompatStatus status)
        {
            return status == ProtocolCompatStatus.Exact || status == ProtocolCompatStatus.CompatibleDowngrade;
        }

        private static string BuildProtocolRefusalFallback(PacketProtocolWelcome welcome)
        {
            var range = new ProtocolRange(welcome.ServerMinSupported, welcome.ServerMaxSupported);
            return $"Your client protocol version is {ProtocolProfile.Current}. This server supports protocol versions {range}.";
        }

        private readonly struct ResolveResult
        {
            private ResolveResult(bool success, IPAddress? address, string error)
            {
                Success = success;
                Address = address;
                Error = error;
            }

            public bool Success { get; }
            public IPAddress? Address { get; }
            public string Error { get; }

            public static ResolveResult Ok(IPAddress address) => new ResolveResult(true, address, string.Empty);
            public static ResolveResult Fail(string error) => new ResolveResult(false, null, error ?? "Unable to resolve server address.");
        }

        private readonly struct SendResult
        {
            private SendResult(bool success, string error)
            {
                Success = success;
                Error = error;
            }

            public bool Success { get; }
            public string Error { get; }

            public static SendResult Ok() => new SendResult(true, string.Empty);
            public static SendResult Fail(string error) => new SendResult(false, error ?? "Failed to send handshake.");
        }
    }
}
