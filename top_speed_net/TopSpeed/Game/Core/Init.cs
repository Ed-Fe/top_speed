using System;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Core.Settings;
using TopSpeed.Core.Updates;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Speech;
using TopSpeed.Windowing;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        public Game(GameWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _settingsManager = new SettingsManager();
            var settingsLoad = _settingsManager.Load();
            _settings = settingsLoad.Settings;
            _settingsIssues = settingsLoad.Issues;
            var audio = new AudioManager(_settings.HrtfAudio, _settings.AutoDetectAudioDeviceFormat);
            var input = new InputManager(_window.Handle);
            var speech = new SpeechService(input.IsAnyInputHeld);
            _audio = audio;
            _input = input;
            _speech = speech;
            speech.ScreenReaderRateMs = _settings.ScreenReaderRateMs;
            input.JoystickScanTimedOut += () => speech.Speak("No joystick detected.");
            input.SetDeviceMode(_settings.DeviceMode);
            _raceInput = new RaceInput(_settings);
            _setup = new RaceSetup();
            _raceModeFactory = new RaceModeFactory(audio, speech, _settings, _raceInput);
            _stateMachine = new StateMachine(this);
            _menu = new MenuManager(audio, speech, () => _settings.UsageHints);
            _dialogs = new DialogManager(_menu);
            _choices = new ChoiceDialogManager(_menu, message => speech.Speak(message));
            _menu.SetWrapNavigation(_settings.MenuWrapNavigation);
            _menu.SetMenuSoundPreset(_settings.MenuSoundPreset);
            _menu.SetMenuNavigatePanning(_settings.MenuNavigatePanning);
            _selection = new RaceSelection(_setup, _settings);
            _menuRegistry = new MenuRegistry(_menu, _settings, _setup, _raceInput, _selection, this, this, this, this, this, this);
            _inputMapping = new InputMappingHandler(input, _raceInput, _settings, speech, SaveSettings);
            _updateConfig = UpdateConfig.Default;
            _updateService = new UpdateService(_updateConfig);
            _multiplayerCoordinator = new MultiplayerCoordinator(
                _menu,
                _dialogs,
                audio,
                speech,
                _settings,
                new MultiplayerConnector(),
                BeginPromptTextInput,
                SaveSettings,
                EnterMenuState,
                SetSession,
                GetSession,
                ClearSession,
                ResetPendingMultiplayerState,
                SetMultiplayerLoadout);
            _mpPktReg = new ClientPktReg();
            _queuedMultiplayerPackets = new System.Collections.Concurrent.ConcurrentQueue<QueuedIncomingPacket>();
            RegisterMultiplayerPacketHandlers();
            _menuRegistry.RegisterAll();
            _multiplayerCoordinator.ConfigureMenuCloseHandlers();
            _settings.AudioVolumes ??= new AudioVolumeSettings();
            _settings.SyncAudioCategoriesFromMusicVolume();
            ApplyAudioSettings();
            _needsCalibration = _settings.ScreenReaderRateMs <= 0f;
        }

        public void Initialize()
        {
            _logo = new LogoScreen((AudioManager)_audio);
            _logo.Start();
            _state = AppState.Logo;
        }
    }
}
