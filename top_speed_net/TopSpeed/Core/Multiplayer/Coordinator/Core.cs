using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TopSpeed.Audio;
using TopSpeed.Data;
using TopSpeed.Input;
using TopSpeed.Menu;
using TopSpeed.Network;
using TopSpeed.Protocol;
using TopSpeed.Speech;
using TopSpeed.Windowing;
using TS.Audio;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator : IMultiplayerRuntime
    {
        private const int MaxChatMessages = 100;
        private static readonly string[] RoomTypeOptions = { "Race with bots", "Race without bots", "One-on-one without bots" };
        private static readonly string[] RoomCapacityOptions = BuildNumericOptions(2, ProtocolConstants.MaxRoomPlayersToStart, "players");
        private static readonly string[] LapCountOptions = BuildNumericOptions(1, 16, "laps");
        private static readonly TrackInfo[] RoomTrackOptions = BuildRoomTrackOptions();
        private const int ConnectingPulseIntervalMs = 500;
        private readonly CoordinatorState _state = new CoordinatorState();

        private readonly MenuManager _menu;
        private readonly QuestionDialog _questions;
        private readonly DialogManager _dialogs;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly RaceSettings _settings;
        private readonly MultiplayerConnector _connector;
        private readonly Action<string, string?, SpeechService.SpeakFlag, bool, Action<TextInputResult>> _promptTextInput;
        private readonly Action _saveSettings;
        private readonly Action _enterMenuState;
        private readonly Action<MultiplayerSession> _setSession;
        private readonly Func<MultiplayerSession?> _getSession;
        private readonly Action _clearSession;
        private readonly Action _resetPendingState;
        private readonly Action<int, bool> _setLocalMultiplayerLoadout;
        private readonly RuntimeLifetime _lifetime;
        private readonly ConnectionFlow _connectionFlow;
        private readonly RoomsFlow _roomsFlow;
        private readonly SavedServersFlow _savedServersFlow;
        private readonly ChatFlow _chatFlow;

        public QuestionDialog Questions => _questions;

        public MultiplayerCoordinator(
            MenuManager menu,
            DialogManager dialogs,
            AudioManager audio,
            SpeechService speech,
            RaceSettings settings,
            MultiplayerConnector connector,
            Action<string, string?, SpeechService.SpeakFlag, bool, Action<TextInputResult>> promptTextInput,
            Action saveSettings,
            Action enterMenuState,
            Action<MultiplayerSession> setSession,
            Func<MultiplayerSession?> getSession,
            Action clearSession,
            Action resetPendingState,
            Action<int, bool> setLocalMultiplayerLoadout)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _questions = new QuestionDialog(_menu);
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _speech = speech ?? throw new ArgumentNullException(nameof(speech));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _promptTextInput = promptTextInput ?? throw new ArgumentNullException(nameof(promptTextInput));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
            _enterMenuState = enterMenuState ?? throw new ArgumentNullException(nameof(enterMenuState));
            _setSession = setSession ?? throw new ArgumentNullException(nameof(setSession));
            _getSession = getSession ?? throw new ArgumentNullException(nameof(getSession));
            _clearSession = clearSession ?? throw new ArgumentNullException(nameof(clearSession));
            _resetPendingState = resetPendingState ?? throw new ArgumentNullException(nameof(resetPendingState));
            _setLocalMultiplayerLoadout = setLocalMultiplayerLoadout ?? throw new ArgumentNullException(nameof(setLocalMultiplayerLoadout));
            _lifetime = new RuntimeLifetime(_state);
            _connectionFlow = new ConnectionFlow(this);
            _roomsFlow = new RoomsFlow(this);
            _savedServersFlow = new SavedServersFlow(this);
            _chatFlow = new ChatFlow(this);
        }
    }
}


