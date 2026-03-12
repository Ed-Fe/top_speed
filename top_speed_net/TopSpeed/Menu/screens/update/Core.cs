using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuScreen
    {
        public MenuUpdateResult Update(IGameInput input)
        {
            if (_items.Count == 0)
                return MenuUpdateResult.None;

            if (!TryHandlePendingTitle(input))
                return MenuUpdateResult.None;

            if (TryHandleShortcut(input))
                return MenuUpdateResult.None;

            var state = CaptureInputState(input);

            if (input.ShouldIgnoreMenuBack())
                return MenuUpdateResult.None;

            if (TryHandleLetterNavigation(input))
                return MenuUpdateResult.None;

            if (state.NextScreen && SwitchToNextScreen())
                return MenuUpdateResult.None;

            if (state.PreviousScreen && SwitchToPreviousScreen())
                return MenuUpdateResult.None;

            if (TryHandleHeldInputGate(input, state, out var heldResult))
                return heldResult;

            if (TryHandleItemAdjustment(state))
                return MenuUpdateResult.None;

            if (TryHandleActionBrowse(state))
                return MenuUpdateResult.None;

            HandleNavigation(state);
            HandleMusicAdjustment(state);

            if (state.Activate)
                return HandleActivation();

            if (state.Back)
            {
                input.LatchMenuBack();
                return MenuUpdateResult.Back;
            }

            if (_index == NoSelection && _autoFocusPending)
            {
                FocusFirstItem();
                _autoFocusPending = false;
            }

            return MenuUpdateResult.None;
        }
    }
}

