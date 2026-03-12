using System.Collections.Generic;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        private void DispatchPacketEffects(IReadOnlyList<PacketEffect> effects)
        {
            if (effects == null || effects.Count == 0)
                return;

            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                switch (effect.Kind)
                {
                    case PacketEffectKind.PlaySound:
                        if (!string.IsNullOrWhiteSpace(effect.Text))
                            PlayNetworkSound(effect.Text);
                        break;
                    case PacketEffectKind.Speak:
                        if (!string.IsNullOrWhiteSpace(effect.Text))
                            _speech.Speak(effect.Text);
                        break;
                    case PacketEffectKind.AddConnectionHistory:
                        AddConnectionMessage(effect.Text);
                        break;
                    case PacketEffectKind.AddGlobalChatHistory:
                        AddGlobalChatMessage(effect.Text);
                        break;
                    case PacketEffectKind.AddRoomChatHistory:
                        AddRoomChatMessage(effect.Text);
                        break;
                    case PacketEffectKind.AddRoomEventHistory:
                        AddRoomEventMessage(effect.Text);
                        break;
                    case PacketEffectKind.ShowRootMenu:
                        _menu.ShowRoot(effect.MenuId);
                        break;
                    case PacketEffectKind.PushMenu:
                        _menu.Push(effect.MenuId);
                        break;
                    case PacketEffectKind.RebuildRoomControls:
                        RebuildRoomControlsMenu();
                        break;
                    case PacketEffectKind.RebuildRoomOptions:
                        RebuildRoomOptionsMenu();
                        break;
                    case PacketEffectKind.RebuildRoomPlayers:
                        RebuildRoomPlayersMenu();
                        break;
                    case PacketEffectKind.UpdateRoomBrowser:
                        UpdateRoomBrowserMenu();
                        break;
                    case PacketEffectKind.BeginRaceLoadout:
                        BeginRaceLoadoutSelection();
                        break;
                    case PacketEffectKind.CancelRoomOptions:
                        CancelRoomOptionsChanges();
                        break;
                }
            }
        }
    }
}
