using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        private bool TryHandleShortcut(IGameInput input)
        {
            if (TryHandleShortcutList(ActiveView.Shortcuts, input))
                return true;
            if (TryHandleShortcutList(_shortcuts, input))
                return true;
            return TryHandleShortcutList(_sharedShortcuts, input);
        }

        private bool TryHandleShortcutList(IReadOnlyList<MenuShortcut> shortcuts, IGameInput input)
        {
            if (shortcuts == null || shortcuts.Count == 0)
                return false;

            for (var i = 0; i < shortcuts.Count; i++)
            {
                var shortcut = shortcuts[i];
                if (shortcut == null)
                    continue;
                if (!input.WasPressed(shortcut.Key))
                    continue;

                CancelHint();
                shortcut.OnTrigger();
                return true;
            }

            return false;
        }

        private bool TryHandleLetterNavigation(IGameInput input)
        {
            if (_items.Count == 0)
                return false;

            if (!MenuInputUtil.TryGetPressedLetter(input, out var letter))
                return false;

            var start = _index == NoSelection ? 0 : (_index + 1) % _items.Count;
            for (var i = 0; i < _items.Count; i++)
            {
                var idx = (start + i) % _items.Count;
                if (!MenuInputUtil.ItemStartsWithLetter(_items[idx], letter))
                    continue;

                _activeActionIndex = NoSelection;
                MoveToIndex(idx);
                return true;
            }

            return false;
        }
    }
}

