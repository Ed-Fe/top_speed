using System;
using System.IO;
using TopSpeed.Protocol;

namespace TopSpeed.Network.Session
{
    internal sealed class Media
    {
        private const int ChunkSize = ProtocolConstants.MaxMediaChunkBytes;
        private readonly Sender _sender;

        public Media(Sender sender)
        {
            _sender = sender;
        }

        public bool TrySendBuffered(uint playerId, byte playerNumber, uint mediaId, string filePath)
        {
            if (!TryValidateInput(mediaId, filePath))
                return false;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(filePath);
            }
            catch
            {
                return false;
            }

            if (!TryValidateLength(bytes.Length))
                return false;

            var extension = NormalizeExtension(filePath);
            _sender.TrySend(
                ClientPacketSerializer.WritePlayerMediaBegin(playerId, playerNumber, mediaId, (uint)bytes.Length, extension),
                PacketStream.Media);

            var chunkIndex = 0;
            var offset = 0;
            while (offset < bytes.Length)
            {
                var length = Math.Min(ChunkSize, bytes.Length - offset);
                var chunk = new byte[length];
                Buffer.BlockCopy(bytes, offset, chunk, 0, length);
                _sender.TrySend(
                    ClientPacketSerializer.WritePlayerMediaChunk(playerId, playerNumber, mediaId, (ushort)chunkIndex, chunk),
                    PacketStream.Media);
                chunkIndex++;
                offset += length;
            }

            _sender.TrySend(ClientPacketSerializer.WritePlayerMediaEnd(playerId, playerNumber, mediaId), PacketStream.Media);
            return true;
        }

        public bool TrySendStreamed(uint playerId, byte playerNumber, uint mediaId, string filePath)
        {
            if (!TryValidateInput(mediaId, filePath))
                return false;

            FileStream? stream = null;
            try
            {
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (!TryValidateLength(stream.Length))
                    return false;

                var extension = NormalizeExtension(filePath);
                _sender.TrySend(
                    ClientPacketSerializer.WritePlayerMediaBegin(playerId, playerNumber, mediaId, (uint)stream.Length, extension),
                    PacketStream.Media);

                var chunkIndex = 0;
                var buffer = new byte[ChunkSize];
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    var chunk = new byte[read];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, read);
                    _sender.TrySend(
                        ClientPacketSerializer.WritePlayerMediaChunk(playerId, playerNumber, mediaId, (ushort)chunkIndex, chunk),
                        PacketStream.Media);
                    chunkIndex++;
                }

                _sender.TrySend(ClientPacketSerializer.WritePlayerMediaEnd(playerId, playerNumber, mediaId), PacketStream.Media);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private static bool TryValidateInput(uint mediaId, string filePath)
        {
            if (mediaId == 0 || string.IsNullOrWhiteSpace(filePath))
                return false;
            return File.Exists(filePath);
        }

        private static bool TryValidateLength(long length)
        {
            return length > 0 && length <= ProtocolConstants.MaxMediaBytes;
        }

        private static string NormalizeExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath).Trim().TrimStart('.');
            if (extension.Length > ProtocolConstants.MaxMediaFileExtensionLength)
                extension = extension.Substring(0, ProtocolConstants.MaxMediaFileExtensionLength);
            return extension;
        }
    }
}
