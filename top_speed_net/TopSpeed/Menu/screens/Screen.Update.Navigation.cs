using System;
using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public void ResetSelection(int? preferredSelectionIndex = null)
        {
            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = preferredSelectionIndex;
            _justEntered = true;
            _autoFocusPending = true;
            CancelHint();
        }

        public void ReplaceItems(IEnumerable<MenuItem> items, bool preserveSelection = false)
        {
            var previousIndex = _index;
            var hadSelection = previousIndex != NoSelection;

            _items.Clear();
            AddVisibleItems(_items, items);
            CancelHint();

            if (preserveSelection && hadSelection && _items.Count > 0)
            {
                _index = Math.Max(0, Math.Min(previousIndex, _items.Count - 1));
                _activeActionIndex = NoSelection;
                _pendingFocusIndex = null;
                _justEntered = false;
                _autoFocusPending = false;
                return;
            }

            _index = NoSelection;
            _activeActionIndex = NoSelection;
            _pendingFocusIndex = null;
            _justEntered = true;
            _autoFocusPending = true;
        }

        private static void AddVisibleItems(List<MenuItem> target, IEnumerable<MenuItem> items)
        {
            if (target == null || items == null)
                return;

            foreach (var item in items)
            {
                if (item == null || item.IsHidden)
                    continue;
                target.Add(item);
            }
        }

        private void HandleNavigation(UpdateInputState state)
        {
            if (_index == NoSelection)
            {
                if (state.MoveDown)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                    _autoFocusPending = false;
                }
                else if (state.MoveUp)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                    _autoFocusPending = false;
                }
                else if (state.MoveHome)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(0);
                    _autoFocusPending = false;
                }
                else if (state.MoveEnd)
                {
                    _activeActionIndex = NoSelection;
                    MoveToIndex(_items.Count - 1);
                    _autoFocusPending = false;
                }

                return;
            }

            if (state.MoveUp)
            {
                _activeActionIndex = NoSelection;
                MoveSelectionAndAnnounce(-1);
            }
            else if (state.MoveDown)
            {
                _activeActionIndex = NoSelection;
                MoveSelectionAndAnnounce(1);
            }
            else if (state.MoveHome)
            {
                _activeActionIndex = NoSelection;
                MoveToIndex(0);
            }
            else if (state.MoveEnd)
            {
                _activeActionIndex = NoSelection;
                MoveToIndex(_items.Count - 1);
            }
        }

        private void MoveSelectionAndAnnounce(int delta)
        {
            var moved = MoveSelection(delta, out var wrapped, out var edgeReached);
            if (moved)
            {
                if (wrapped)
                {
                    PlayNavigateSound();
                    PlaySfx(_wrapSound);
                }
                else
                {
                    PlayNavigateSound();
                }
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
            }
            else if (wrapped)
            {
                PlaySfx(_wrapSound);
            }
            else if (edgeReached)
            {
                PlaySfx(_edgeSound);
            }
        }

        private void MoveToIndex(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _items.Count)
                return;
            if (_index == NoSelection)
            {
                _index = targetIndex;
                PlayNavigateSound();
                AnnounceCurrent(!_justEntered);
                _justEntered = false;
                return;
            }
            if (targetIndex == _index)
            {
                PlaySfx(WrapNavigation ? _wrapSound : _edgeSound);
                return;
            }
            _index = targetIndex;
            PlayNavigateSound();
            AnnounceCurrent(!_justEntered);
            _justEntered = false;
        }

        private bool MoveSelection(int delta, out bool wrapped, out bool edgeReached)
        {
            wrapped = false;
            edgeReached = false;
            if (_items.Count == 0)
                return false;
            if (_index == NoSelection)
            {
                _index = delta >= 0 ? 0 : _items.Count - 1;
                return true;
            }
            var previous = _index;
            if (WrapNavigation)
            {
                var next = _index + delta;
                if (next < 0 || next >= _items.Count)
                    wrapped = true;
                _index = (next + _items.Count) % _items.Count;
                return _index != previous;
            }

            var nextIndex = _index + delta;
            if (nextIndex < 0 || nextIndex >= _items.Count)
            {
                edgeReached = true;
                return false;
            }
            _index = nextIndex;
            return _index != previous;
        }

        private void FocusFirstItem()
        {
            if (_items.Count == 0)
                return;
            var targetIndex = 0;
            if (_pendingFocusIndex.HasValue)
                targetIndex = Math.Max(0, Math.Min(_items.Count - 1, _pendingFocusIndex.Value));
            _pendingFocusIndex = null;
            _index = targetIndex;
            _activeActionIndex = NoSelection;
            PlayNavigateSound();
            AnnounceCurrent(purge: false);
            _justEntered = false;
        }
    }
}
