using System;
using System.Collections.Generic;
using TopSpeed.Menu;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void UpdateHistoryScreens()
        {
            var items = _state.Chat.History.GetCurrentItems();
            TryUpdateChatScreen(MultiplayerMenuKeys.Lobby, items);
            TryUpdateChatScreen(MultiplayerMenuKeys.RoomControls, items);
        }

        private void TryUpdateChatScreen(string menuId, IEnumerable<MenuItem> items)
        {
            try
            {
                _menu.UpdateItems(menuId, MultiplayerScreenKeys.SharedLobbyChat, items, preserveSelection: true);
            }
            catch (InvalidOperationException)
            {
                // Menus may not be registered yet during startup.
            }
        }

        private static string? NormalizeChatMessage(string message)
        {
            var text = (message ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
    }
}


