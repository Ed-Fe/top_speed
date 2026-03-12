using System;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuManager
    {
        public MenuAction Update(IGameInput input)
        {
            if (_stack.Count == 0)
                return MenuAction.None;

            if (TryHandleGlobalShortcuts(input))
                return MenuAction.None;

            var current = _stack.Peek();
            var result = current.Update(input);

            if (result.BackRequested)
                return HandleClose(current, MenuCloseSource.Shortcut);

            if (result.ActivatedItem == null)
                return MenuAction.None;

            var item = result.ActivatedItem;
            var stackCount = _stack.Count;
            var announcement = item.ActivateAndGetAnnouncement();
            if (announcement != null)
                current.CancelPendingHint();
            var stackChanged = _stack.Count != stackCount || _stack.Peek() != current;
            if (item.Action == MenuAction.Back || item.IsCloseItem)
                return HandleClose(current, MenuCloseSource.Item);
            if (!string.IsNullOrWhiteSpace(item.NextMenuId))
            {
                Push(item.NextMenuId!);
                return MenuAction.None;
            }

            if (!stackChanged && !item.SuppressPostActivateAnnouncement && !string.IsNullOrWhiteSpace(announcement))
                _speech.Speak(announcement!);
            return item.Action;
        }

        private bool TryHandleGlobalShortcuts(IGameInput input)
        {
            if (_globalShortcuts.Count == 0)
                return false;

            for (var i = 0; i < _globalShortcuts.Count; i++)
            {
                var shortcut = _globalShortcuts[i];
                if (shortcut == null)
                    continue;
                if (!input.WasPressed(shortcut.Key))
                    continue;

                if (_stack.Count > 0)
                    _stack.Peek().CancelPendingHint();
                shortcut.OnTrigger();
                return true;
            }

            return false;
        }

        private MenuAction HandleClose(MenuScreen current, MenuCloseSource source)
        {
            if (current.TryHandleClose(source))
                return MenuAction.None;

            if (_stack.Count > 1)
            {
                _stack.Peek().CancelPendingHint();
                _stack.Pop();
                _stack.Peek().QueueTitleAnnouncement();
                return MenuAction.None;
            }

            return MenuAction.Exit;
        }

        private MenuScreen GetScreen(string id)
        {
            if (!_screens.TryGetValue(id, out var screen))
                throw new InvalidOperationException($"Menu not registered: {id}");
            return screen;
        }
    }
}

