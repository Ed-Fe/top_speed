using System.Threading;
using TS.Audio;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed class CoordinatorAudioState
    {
        public CancellationTokenSource? ConnectingPulseCts;
        public AudioSourceHandle? ConnectingSound;
        public AudioSourceHandle? ConnectedSound;
        public AudioSourceHandle? OnlineSound;
        public AudioSourceHandle? OfflineSound;
        public AudioSourceHandle? PingStartSound;
        public AudioSourceHandle? PingSound;
        public AudioSourceHandle? RoomCreatedSound;
        public AudioSourceHandle? RoomJoinSound;
        public AudioSourceHandle? RoomLeaveSound;
        public AudioSourceHandle? ChatSound;
        public AudioSourceHandle? RoomChatSound;
        public AudioSourceHandle? BufferSwitchSound;
    }
}
