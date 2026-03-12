using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Settings;
using TopSpeed.Core.Updates;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Race;
using TopSpeed.Speech;
using TopSpeed.Windowing;
using CoreRaceMode = TopSpeed.Core.RaceMode;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private enum AppState
        {
            Logo,
            Menu,
            TimeTrial,
            SingleRace,
            MultiplayerRace,
            Paused,
            Calibration
        }

        private readonly GameWindow _window;
        private readonly IGameAudio _audio;
        private readonly IGameSpeech _speech;
        private readonly IGameInput _input;
        private readonly MenuManager _menu;
        private readonly DialogManager _dialogs;
        private readonly ChoiceDialogManager _choices;
        private readonly RaceSettings _settings;
        private readonly IReadOnlyList<SettingsIssue> _settingsIssues;
        private readonly RaceInput _raceInput;
        private readonly RaceSetup _setup;
        private readonly IRaceModeFactory _raceModeFactory;
        private readonly StateMachine _stateMachine;
        private readonly SettingsManager _settingsManager;
        private readonly RaceSelection _selection;
        private readonly MenuRegistry _menuRegistry;
        private readonly IMultiplayerRuntime _multiplayerCoordinator;
        private readonly UpdateConfig _updateConfig;
        private readonly UpdateService _updateService;
        private readonly ClientPktReg _mpPktReg;
        private readonly ConcurrentQueue<QueuedIncomingPacket> _queuedMultiplayerPackets;
        private MultiplayerSession? _session;
        private readonly InputMappingHandler _inputMapping;
        private LogoScreen? _logo;
        private AppState _state;
        private AppState _pausedState;
        private bool _needsCalibration;
        private bool _calibrationMenusRegistered;
        private string? _calibrationReturnMenuId;
        private bool _calibrationOverlay;
        private Stopwatch? _calibrationStopwatch;
        private bool _pendingRaceStart;
        private CoreRaceMode _pendingMode;
        private bool _pauseKeyReleased = true;
        private TimeTrialMode? _timeTrial;
        private SingleRaceMode? _singleRace;
        private MultiplayerMode? _multiplayerRace;
        private bool _multiplayerRaceQuitConfirmActive;
        private TrackData? _pendingMultiplayerTrack;
        private string _pendingMultiplayerTrackName = string.Empty;
        private int _pendingMultiplayerLaps;
        private bool _pendingMultiplayerStart;
        private int _multiplayerVehicleIndex;
        private bool _multiplayerAutomaticTransmission = true;
        private bool _updateCheckQueued;
        private bool _updatePromptShown;
        private Task<UpdateCheckResult>? _updateCheckTask;
        private UpdateInfo? _pendingUpdateInfo;
        private Task<DownloadResult>? _updateDownloadTask;
        private CancellationTokenSource? _updateDownloadCts;
        private long _updateTotalBytes;
        private long _updateDownloadedBytes;
        private int _updatePercent;
        private int _updateTonePercent;
        private int _lastSpokenUpdatePercent;
        private bool _updateProgressOpen;
        private bool _updateCompleteOpen;
        private string _updateZipPath = string.Empty;
        private bool _manualUpdateRequest;
        private bool _audioLoopActive;
        private bool _textInputPromptActive;
        private Action<TextInputResult>? _textInputPromptCallback;
        public bool IsModalInputActive { get; private set; }
        internal int LoopIntervalMs => IsMenuState(_state) ? 15 : 8;

        private const string CalibrationIntroMenuId = "calibration_intro";
        private const string CalibrationSampleMenuId = "calibration_sample";
        private const string CalibrationInstructions =
            "Screen-reader calibration. You'll be presented with a short piece of text on the next screen. Press ENTER when your screen-reader finishes speaking it.";
        private const string CalibrationSampleText =
            "I really have nothing interesting to put here not even the secret to life except this really long run on sentence that is probably the most boring thing you have ever read but that will help me get an idea of how fast your screen reader is speaking.";

        public event Action? ExitRequested;

        private readonly struct QueuedIncomingPacket
        {
            public QueuedIncomingPacket(MultiplayerSession session, IncomingPacket packet)
            {
                Session = session;
                Packet = packet;
            }

            public MultiplayerSession Session { get; }
            public IncomingPacket Packet { get; }
        }
    }
}

