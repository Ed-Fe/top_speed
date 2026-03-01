using System;
using System.Collections.Concurrent;
using System.Net;
using LiteNetLib;
using TopSpeed.Network.Session;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal sealed class MultiplayerSession : IDisposable
    {
        private readonly NetManager _manager;
        private readonly IPEndPoint _serverEndPoint;
        private readonly ConcurrentQueue<IncomingPacket> _incoming;
        private readonly Sender _sender;
        private readonly Media _media;
        private readonly Loop _loop;
        private Action<IncomingPacket>? _packetSink;
        private byte _playerNumber;

        public MultiplayerSession(
            NetManager manager,
            NetPeer peer,
            IPEndPoint serverEndPoint,
            uint playerId,
            byte playerNumber,
            string? motd,
            string? playerName,
            ConcurrentQueue<IncomingPacket> incoming)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _serverEndPoint = serverEndPoint ?? throw new ArgumentNullException(nameof(serverEndPoint));
            _incoming = incoming ?? throw new ArgumentNullException(nameof(incoming));
            _sender = new Sender(peer ?? throw new ArgumentNullException(nameof(peer)));
            _media = new Media(_sender);
            PlayerId = playerId;
            _playerNumber = playerNumber;
            Motd = motd ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            _loop = new Loop(PollEventsSafe, DrainIncomingToSink, SendKeepAlive);
        }

        public IPAddress Address => _serverEndPoint.Address;
        public int Port => _serverEndPoint.Port;
        public uint PlayerId { get; }
        public byte PlayerNumber => _playerNumber;
        public string Motd { get; }
        public string PlayerName { get; }

        public void UpdatePlayerNumber(byte playerNumber)
        {
            _playerNumber = playerNumber;
        }

        public bool TryDequeuePacket(out IncomingPacket packet)
        {
            return _incoming.TryDequeue(out packet);
        }

        public void SetPacketSink(Action<IncomingPacket>? packetSink)
        {
            _packetSink = packetSink;
            if (packetSink != null)
                DrainIncomingToSink();
        }

        public void SendPlayerState(PlayerState state)
        {
            var payload = ClientPacketSerializer.WritePlayerState(Command.PlayerState, PlayerId, PlayerNumber, state);
            _sender.TrySend(payload, PacketStream.Control);
        }

        public void SendPlayerData(
            PlayerRaceData raceData,
            CarType car,
            PlayerState state,
            bool engine,
            bool braking,
            bool horning,
            bool backfiring,
            bool radioLoaded,
            bool radioPlaying,
            uint radioMediaId)
        {
            var payload = ClientPacketSerializer.WritePlayerDataToServer(
                PlayerId,
                PlayerNumber,
                car,
                raceData,
                state,
                engine,
                braking,
                horning,
                backfiring,
                radioLoaded,
                radioPlaying,
                radioMediaId);
            _sender.TrySend(payload, PacketStream.RaceState, PacketDeliveryKind.Sequenced);
        }

        public bool SendRadioMedia(uint mediaId, string filePath)
        {
            return _media.TrySendBuffered(PlayerId, PlayerNumber, mediaId, filePath);
        }

        public bool SendRadioMediaStreamed(uint mediaId, string filePath)
        {
            return _media.TrySendStreamed(PlayerId, PlayerNumber, mediaId, filePath);
        }

        public void SendPlayerStarted()
        {
            _sender.TrySend(
                ClientPacketSerializer.WritePlayer(Command.PlayerStarted, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }

        public void SendPlayerFinished()
        {
            _sender.TrySend(
                ClientPacketSerializer.WritePlayer(Command.PlayerFinished, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }

        public void SendPlayerFinalize(PlayerState state)
        {
            _sender.TrySend(
                ClientPacketSerializer.WritePlayerState(Command.PlayerFinalize, PlayerId, PlayerNumber, state),
                PacketStream.Control);
        }

        public void SendPlayerCrashed()
        {
            _sender.TrySend(
                ClientPacketSerializer.WritePlayer(Command.PlayerCrashed, PlayerId, PlayerNumber),
                PacketStream.RaceEvent);
        }

        public void SendPing()
        {
            _sender.TrySend(ClientPacketSerializer.WriteGeneral(Command.Ping), PacketStream.Control);
        }

        public void SendRoomListRequest()
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomListRequest(), PacketStream.Query);
        }

        public void SendRoomStateRequest()
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomStateRequest(), PacketStream.Query);
        }

        public void SendRoomGetRequest(uint roomId)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomGetRequest(roomId), PacketStream.Query);
        }

        public void SendRoomCreate(string roomName, GameRoomType roomType, byte playersToStart)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomCreate(roomName, roomType, playersToStart), PacketStream.Room);
        }

        public void SendRoomJoin(uint roomId)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomJoin(roomId), PacketStream.Room);
        }

        public void SendRoomLeave()
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomLeave(), PacketStream.Room);
        }

        public void SendRoomSetTrack(string trackName)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomSetTrack(trackName), PacketStream.Room);
        }

        public void SendRoomSetLaps(byte laps)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomSetLaps(laps), PacketStream.Room);
        }

        public void SendRoomStartRace()
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomStartRace(), PacketStream.Room);
        }

        public void SendRoomSetPlayersToStart(byte playersToStart)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomSetPlayersToStart(playersToStart), PacketStream.Room);
        }

        public void SendRoomAddBot()
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomAddBot(), PacketStream.Room);
        }

        public void SendRoomRemoveBot()
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomRemoveBot(), PacketStream.Room);
        }

        public void SendRoomPlayerReady(CarType car, bool automaticTransmission)
        {
            _sender.TrySend(ClientPacketSerializer.WriteRoomPlayerReady(car, automaticTransmission), PacketStream.Room);
        }

        public void Dispose()
        {
            _loop.Dispose();
            _manager.Stop();
        }

        private void PollEventsSafe()
        {
            try
            {
                _manager.PollEvents();
            }
            catch
            {
                // Keep session alive even if the transport reports a transient poll error.
            }
        }

        private void SendKeepAlive()
        {
            _sender.TrySend(
                new[] { ProtocolConstants.Version, (byte)Command.KeepAlive },
                PacketStream.Control,
                PacketDeliveryKind.Unreliable);
        }

        private void DrainIncomingToSink()
        {
            var sink = _packetSink;
            if (sink == null)
                return;

            while (_incoming.TryDequeue(out var packet))
            {
                try
                {
                    sink(packet);
                }
                catch
                {
                    // Keep main-thread packet handling resilient against callback failures.
                }
            }
        }
    }

    internal readonly struct IncomingPacket
    {
        public IncomingPacket(Command command, byte[] payload)
            : this(command, payload, 0)
        {
        }

        public IncomingPacket(Command command, byte[] payload, long receivedUtcTicks)
        {
            Command = command;
            Payload = payload;
            ReceivedUtcTicks = receivedUtcTicks;
        }

        public Command Command { get; }
        public byte[] Payload { get; }
        public long ReceivedUtcTicks { get; }
    }
}
