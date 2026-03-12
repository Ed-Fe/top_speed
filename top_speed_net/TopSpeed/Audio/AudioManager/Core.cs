using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using TS.Audio;

namespace TopSpeed.Audio
{
    internal sealed partial class AudioManager : IGameAudio
    {
        private readonly AudioSystem _system;
        private readonly AudioOutput _output;
        private readonly object _cacheLock = new object();
        private readonly Dictionary<AudioCacheKey, CachedSource> _sourceCache =
            new Dictionary<AudioCacheKey, CachedSource>(new AudioCacheKeyComparer());
        private readonly Dictionary<AudioSourceHandle, AudioCacheKey> _handleCache =
            new Dictionary<AudioSourceHandle, AudioCacheKey>();
        private readonly object _pathCacheLock = new object();
        private readonly Dictionary<string, bool> _pathExistsCache =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private Thread? _updateThread;
        private volatile bool _updateRunning;

        public bool IsHrtfActive => _system.IsHrtfActive;
        public int OutputChannels => _output.Channels;
        public int OutputSampleRate => _output.SampleRate;

        public AudioManager(bool useHrtf = false, bool autoDetectDeviceFormat = true)
        {
            var config = new AudioSystemConfig
            {
                UseHrtf = useHrtf
            };
            if (autoDetectDeviceFormat)
            {
                config.Channels = 0;
                config.SampleRate = 0;
            }
            _system = new AudioSystem(config);
            _output = _system.CreateOutput(new AudioOutputConfig { Name = "main", Channels = 0, SampleRate = 0 });
        }

        public AudioSourceHandle CreateSource(string path, bool streamFromDisk = true, bool useHrtf = false)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Audio file not found.", path);
            return _output.CreateSource(path, streamFromDisk, useHrtf);
        }

        public AudioSourceHandle CreateLoopingSource(string path, bool useHrtf = false)
        {
            return CreateSource(path, streamFromDisk: false, useHrtf: useHrtf);
        }

        public AudioSourceHandle CreateSpatialSource(string path, bool streamFromDisk = true, bool allowHrtf = true)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Audio file not found.", path);

            return _output.CreateSpatialSource(path, streamFromDisk, allowHrtf);
        }

        public AudioSourceHandle CreateLoopingSpatialSource(string path, bool allowHrtf = true)
        {
            return CreateSpatialSource(path, streamFromDisk: false, allowHrtf: allowHrtf);
        }

        public AudioSourceHandle CreateProceduralSource(ProceduralAudioCallback callback, uint channels = 1, uint sampleRate = 44100, bool useHrtf = true)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            return _output.CreateProceduralSource(callback, channels, sampleRate, useHrtf);
        }

        public void Update()
        {
            _system.Update();
        }

        public void SetMasterVolume(float volume)
        {
            _output.SetMasterVolume(volume);
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _system.UpdateListenerAll(position, forward, up, velocity);
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            _output.SetRoomAcoustics(acoustics);
        }
    }
}
