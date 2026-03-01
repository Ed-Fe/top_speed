using System;
using System.Collections.Generic;
using TopSpeed.Core;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal interface IMenuActions
    {
        void SaveMusicVolume(float volume);
        void QueueRaceStart(RaceMode mode);
        void StartServerDiscovery();
        void OpenSavedServersManager();
        void BeginManualServerEntry();
        void SpeakMessage(string text);
        void ShowMessageDialog(string title, string caption, IReadOnlyList<string> items);
        void SpeakNotImplemented();
        void BeginServerPortEntry();
        void RestoreDefaults();
        void RecalibrateScreenReaderRate();
        void SetDevice(InputDeviceMode mode);
        void ToggleCurveAnnouncements();
        void ToggleSetting(Action update);
        void UpdateSetting(Action update);
        void ApplyAudioSettings();
        void BeginMapping(InputMappingMode mode, InputAction action);
        string FormatMappingValue(InputAction action, InputMappingMode mode);
    }

    internal sealed partial class MenuRegistry
    {
        private readonly MenuManager _menu;
        private readonly RaceSettings _settings;
        private readonly RaceSetup _setup;
        private readonly RaceInput _raceInput;
        private readonly RaceSelection _selection;
        private readonly IMenuActions _actions;
        private readonly IReadOnlyList<string> _menuSoundPresets;

        public MenuRegistry(
            MenuManager menu,
            RaceSettings settings,
            RaceSetup setup,
            RaceInput raceInput,
            RaceSelection selection,
            IMenuActions actions)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _raceInput = raceInput ?? throw new ArgumentNullException(nameof(raceInput));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
            _menuSoundPresets = LoadMenuSoundPresets();
        }

        public void RegisterAll()
        {
            RegisterMainMenu();

            _menu.Register(BuildMultiplayerMenu());
            _menu.Register(BuildMultiplayerServersMenu());
            _menu.Register(BuildMultiplayerSavedServersMenu());
            _menu.Register(BuildMultiplayerSavedServerFormMenu());
            _menu.Register(BuildMultiplayerLobbyMenu());
            _menu.Register(BuildMultiplayerRoomsMenu());
            _menu.Register(BuildMultiplayerCreateRoomMenu());
            _menu.Register(BuildMultiplayerRoomControlsMenu());
            _menu.Register(BuildMultiplayerRoomPlayersMenu());
            _menu.Register(BuildMultiplayerRoomOptionsMenu());
            _menu.Register(BuildMultiplayerLoadoutVehicleMenu());
            _menu.Register(BuildMultiplayerLoadoutTransmissionMenu());

            _menu.Register(BuildTrackTypeMenu("time_trial_type", RaceMode.TimeTrial));
            _menu.Register(BuildTrackTypeMenu("single_race_type", RaceMode.SingleRace));

            _menu.Register(BuildTrackMenu("time_trial_tracks_race", RaceMode.TimeTrial, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("time_trial_tracks_adventure", RaceMode.TimeTrial, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("time_trial_tracks_custom", RaceMode.TimeTrial));
            _menu.Register(BuildTrackMenu("single_race_tracks_race", RaceMode.SingleRace, TrackCategory.RaceTrack));
            _menu.Register(BuildTrackMenu("single_race_tracks_adventure", RaceMode.SingleRace, TrackCategory.StreetAdventure));
            _menu.Register(BuildCustomTrackMenu("single_race_tracks_custom", RaceMode.SingleRace));

            _menu.Register(BuildVehicleMenu("time_trial_vehicles", RaceMode.TimeTrial));
            _menu.Register(BuildCustomVehicleMenu("time_trial_vehicles_custom", RaceMode.TimeTrial));
            _menu.Register(BuildVehicleMenu("single_race_vehicles", RaceMode.SingleRace));
            _menu.Register(BuildCustomVehicleMenu("single_race_vehicles_custom", RaceMode.SingleRace));

            _menu.Register(BuildTransmissionMenu("time_trial_transmission", RaceMode.TimeTrial));
            _menu.Register(BuildTransmissionMenu("single_race_transmission", RaceMode.SingleRace));

            _menu.Register(BuildOptionsMenu());
            _menu.Register(BuildOptionsGameSettingsMenu());
            _menu.Register(BuildOptionsVolumeSettingsMenu());
            _menu.Register(BuildOptionsControlsMenu());
            _menu.Register(BuildOptionsControlsDeviceMenu());
            _menu.Register(BuildOptionsControlsKeyboardMenu());
            _menu.Register(BuildOptionsControlsJoystickMenu());
            _menu.Register(BuildOptionsRaceSettingsMenu());
            _menu.Register(BuildOptionsRestoreMenu());
            _menu.Register(BuildOptionsServerSettingsMenu());
        }

        private void RegisterMainMenu()
        {
            var mainMenu = _menu.CreateMenu("main", new[]
            {
                new MenuItem("Quick start", MenuAction.QuickStart),
                new MenuItem("Time trial", MenuAction.None, nextMenuId: "time_trial_type", onActivate: () => PrepareMode(RaceMode.TimeTrial)),
                new MenuItem("Single race", MenuAction.None, nextMenuId: "single_race_type", onActivate: () => PrepareMode(RaceMode.SingleRace)),
                new MenuItem("MultiPlayer game", MenuAction.None, nextMenuId: "multiplayer"),
                new MenuItem("Options", MenuAction.None, nextMenuId: "options_main"),
                new MenuItem("Exit Game", MenuAction.Exit)
            }, "Main menu", titleProvider: MainMenuTitle);

            mainMenu.MusicFile = "theme1.ogg";
            mainMenu.MusicVolume = _settings.MusicVolume;
            mainMenu.MusicVolumeChanged = _actions.SaveMusicVolume;
            _menu.Register(mainMenu);
        }
    }
}
