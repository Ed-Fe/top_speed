using System;
using SteamAudio;

namespace TopSpeed.Tracks.Acoustics
{
    internal sealed class TrackSteamAudioScene : IDisposable
    {
        private IPL.Scene _scene;
        private IPL.StaticMesh _staticMesh;
        private IPL.ProbeBatch _probeBatch;
        private bool _hasBakedReflections;
        private IPL.BakedDataIdentifier _bakedIdentifier;

        public TrackSteamAudioScene(IPL.Scene scene, IPL.StaticMesh staticMesh, IPL.ProbeBatch probeBatch, in IPL.BakedDataIdentifier bakedIdentifier, bool hasBakedReflections)
        {
            _scene = scene;
            _staticMesh = staticMesh;
            _probeBatch = probeBatch;
            _bakedIdentifier = bakedIdentifier;
            _hasBakedReflections = hasBakedReflections;
        }

        public IPL.Scene Scene => _scene;
        public IPL.ProbeBatch ProbeBatch => _probeBatch;
        public bool HasBakedReflections => _hasBakedReflections;
        public IPL.BakedDataIdentifier BakedIdentifier => _bakedIdentifier;

        public void Dispose()
        {
            if (_staticMesh.Handle != IntPtr.Zero && _scene.Handle != IntPtr.Zero)
                IPL.StaticMeshRemove(_staticMesh, _scene);

            if (_staticMesh.Handle != IntPtr.Zero)
                IPL.StaticMeshRelease(ref _staticMesh);

            if (_probeBatch.Handle != IntPtr.Zero)
                IPL.ProbeBatchRelease(ref _probeBatch);

            if (_scene.Handle != IntPtr.Zero)
                IPL.SceneRelease(ref _scene);
        }
    }
}
