using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Speech;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager : IDisposable
    {
        private const int DefaultFadeMs = 1000;
        private readonly Dictionary<string, MenuScreen> _screens;
        private readonly Dictionary<string, MenuShortcut> _sharedShortcutActions;
        private readonly Dictionary<string, MenuShortcut> _globalShortcutActions;
        private readonly Stack<MenuScreen> _stack;
        private readonly AudioManager _audio;
        private readonly SpeechService _speech;
        private readonly Func<bool> _usageHintsEnabled;
        private bool _wrapNavigation = true;
        private bool _menuNavigatePanning;
        private string? _menuSoundPreset;
        private bool _menuMusicSuspended;
        private readonly List<MenuShortcut> _globalShortcuts;

        public MenuManager(AudioManager audio, SpeechService speech, Func<bool>? usageHintsEnabled = null)
        {
            _audio = audio;
            _speech = speech;
            _usageHintsEnabled = usageHintsEnabled ?? (() => false);
            _screens = new Dictionary<string, MenuScreen>(StringComparer.Ordinal);
            _sharedShortcutActions = new Dictionary<string, MenuShortcut>(StringComparer.Ordinal);
            _globalShortcutActions = new Dictionary<string, MenuShortcut>(StringComparer.Ordinal);
            _globalShortcuts = new List<MenuShortcut>();
            _stack = new Stack<MenuScreen>();
        }

        public void Register(MenuScreen screen)
        {
            if (!_screens.ContainsKey(screen.Id))
                _screens.Add(screen.Id, screen);
        }

        public void UpdateItems(string id, IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var screen = GetScreen(id);
            screen.ReplaceItems(items, preserveSelection);
        }

        public void SetShortcuts(string id, IEnumerable<MenuShortcut>? shortcuts)
        {
            var screen = GetScreen(id);
            screen.SetShortcuts(shortcuts);
        }

        public void RegisterSharedShortcutAction(string actionId, MenuShortcut shortcut)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("Shortcut action id is required.", nameof(actionId));
            _sharedShortcutActions[actionId] = shortcut ?? throw new ArgumentNullException(nameof(shortcut));
        }

        public void SetSharedShortcutActions(string id, IEnumerable<string>? actionIds)
        {
            var screen = GetScreen(id);
            if (actionIds == null)
            {
                screen.SetSharedShortcuts(null);
                return;
            }

            var shortcuts = new List<MenuShortcut>();
            foreach (var actionId in actionIds)
            {
                if (string.IsNullOrWhiteSpace(actionId))
                    continue;
                if (_sharedShortcutActions.TryGetValue(actionId, out var shortcut))
                    shortcuts.Add(shortcut);
            }

            screen.SetSharedShortcuts(shortcuts);
        }

        public void RegisterGlobalShortcutAction(string actionId, MenuShortcut shortcut)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("Shortcut action id is required.", nameof(actionId));
            _globalShortcutActions[actionId] = shortcut ?? throw new ArgumentNullException(nameof(shortcut));
        }

        public void SetGlobalShortcutActions(IEnumerable<string>? actionIds)
        {
            _globalShortcuts.Clear();
            if (actionIds == null)
                return;

            foreach (var actionId in actionIds)
            {
                if (string.IsNullOrWhiteSpace(actionId))
                    continue;
                if (_globalShortcutActions.TryGetValue(actionId, out var shortcut))
                    _globalShortcuts.Add(shortcut);
            }
        }

        public void SetGlobalShortcuts(IEnumerable<MenuShortcut>? shortcuts)
        {
            _globalShortcuts.Clear();
            if (shortcuts == null)
                return;
            _globalShortcuts.AddRange(shortcuts);
        }

        public void SetCloseHandler(string id, Func<MenuCloseSource, bool>? closeHandler)
        {
            var screen = GetScreen(id);
            screen.CloseHandler = closeHandler;
        }

    }
}
