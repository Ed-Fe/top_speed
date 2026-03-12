using System;
using System.Collections.Generic;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed class StateMachine
        {
            private readonly Game _game;
            private readonly Dictionary<AppState, Action<float>> _handlers;

            public StateMachine(Game game)
            {
                _game = game ?? throw new ArgumentNullException(nameof(game));
                _handlers = new Dictionary<AppState, Action<float>>
                {
                    { AppState.Logo, UpdateLogo },
                    { AppState.Calibration, UpdateCalibration },
                    { AppState.Menu, UpdateMenu },
                    { AppState.TimeTrial, UpdateTimeTrial },
                    { AppState.SingleRace, UpdateSingleRace },
                    { AppState.MultiplayerRace, UpdateMultiplayerRace },
                    { AppState.Paused, UpdatePaused }
                };
            }

            public void Update(float deltaSeconds)
            {
                if (_handlers.TryGetValue(_game._state, out var handler))
                    handler(deltaSeconds);
            }

            private void UpdateLogo(float deltaSeconds)
            {
                if (_game._logo != null && !_game._logo.Update(_game._input, deltaSeconds))
                    return;

                _game._logo?.Dispose();
                _game._logo = null;
                _game._menu.ShowRoot("main");
                if (_game._needsCalibration)
                {
                    if (!_game.ShowSettingsIssuesDialog(() => _game.StartCalibrationSequence()))
                        _game.StartCalibrationSequence();
                    else
                        _game._state = AppState.Menu;
                }
                else
                {
                    _game.ShowSettingsIssuesDialog();
                    _game._menu.FadeInMenuMusic(force: true);
                    _game._state = AppState.Menu;
                }

                _game.StartAutoUpdateCheck();
            }

            private void UpdateCalibration(float _)
            {
                _game._menu.Update(_game._input);
                if (_game._calibrationOverlay && !Game.IsCalibrationMenu(_game._menu.CurrentId))
                {
                    _game._calibrationOverlay = false;
                    _game._state = AppState.Menu;
                }
            }

            private void UpdateMenu(float _)
            {
                _game.UpdateUpdateFlow();

                if (_game._session != null)
                {
                    _game.ProcessMultiplayerPackets();
                    if (_game._state != AppState.Menu)
                        return;
                }

                if (_game._textInputPromptActive)
                    return;

                if (_game.UpdateModalOperations())
                    return;

                if (_game._inputMapping.IsActive)
                {
                    _game._inputMapping.Update();
                    return;
                }

                var action = _game._menu.Update(_game._input);
                _game.HandleMenuAction(action);
            }

            private void UpdateTimeTrial(float deltaSeconds)
            {
                _game.RunTimeTrial(deltaSeconds);
            }

            private void UpdateSingleRace(float deltaSeconds)
            {
                _game.RunSingleRace(deltaSeconds);
            }

            private void UpdateMultiplayerRace(float deltaSeconds)
            {
                _game.RunMultiplayerRace(deltaSeconds);
            }

            private void UpdatePaused(float _)
            {
                _game.UpdatePaused();
            }
        }
    }
}
