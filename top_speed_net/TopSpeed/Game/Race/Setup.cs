using System;
using System.Collections.Generic;
using TopSpeed.Audio;
using TopSpeed.Common;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Race;
using TopSpeed.Tracks;
using CoreRaceMode = TopSpeed.Core.RaceMode;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void PrepareQuickStart()
        {
            _setup.Mode = CoreRaceMode.QuickStart;
            _setup.ClearSelection();
            _selection.SelectRandomTrackAny(_settings.RandomCustomTracks);
            _selection.SelectRandomVehicle();
            _setup.Transmission = TransmissionMode.Automatic;
        }

        private void QueueRaceStart(CoreRaceMode mode)
        {
            _pendingRaceStart = true;
            _pendingMode = mode;
        }

        private void StartRace(CoreRaceMode mode)
        {
            FadeOutMenuMusic();
            var track = string.IsNullOrWhiteSpace(_setup.TrackNameOrFile)
                ? TrackList.RaceTracks[0].Key
                : _setup.TrackNameOrFile!;
            var vehicleIndex = _setup.VehicleIndex ?? 0;
            var vehicleFile = _setup.VehicleFile;
            var automatic = _setup.Transmission == TransmissionMode.Automatic;

            try
            {
                switch (mode)
                {
                    case CoreRaceMode.TimeTrial:
                        _timeTrial?.FinalizeTimeTrialMode();
                        _timeTrial?.Dispose();
                        _timeTrial = null;

                        var timeTrial = _raceModeFactory.CreateTimeTrial(
                            track,
                            automatic,
                            _settings.NrOfLaps,
                            vehicleIndex,
                            vehicleFile,
                            _input.VibrationDevice);
                        timeTrial.Initialize();
                        _timeTrial = timeTrial;
                        _state = AppState.TimeTrial;
                        _speech.Speak("Time trial.");
                        break;
                    case CoreRaceMode.QuickStart:
                    case CoreRaceMode.SingleRace:
                        _singleRace?.FinalizeSingleRaceMode();
                        _singleRace?.Dispose();
                        _singleRace = null;

                        var singleRace = _raceModeFactory.CreateSingleRace(
                            track,
                            automatic,
                            _settings.NrOfLaps,
                            vehicleIndex,
                            vehicleFile,
                            _input.VibrationDevice);
                        singleRace.Initialize(Algorithm.RandomInt(_settings.NrOfComputers + 1));
                        _singleRace = singleRace;
                        _state = AppState.SingleRace;
                        _speech.Speak(mode == CoreRaceMode.QuickStart ? "Quick start." : "Single race.");
                        break;
                }
            }
            catch (TrackLoadException ex)
            {
                HandleTrackLoadFailure(ex);
            }
        }

        private void HandleTrackLoadFailure(TrackLoadException ex)
        {
            _state = AppState.Menu;
            _menu.FadeInMenuMusic(force: true);

            var items = new List<string>();
            if (ex.Details != null && ex.Details.Count > 0)
            {
                for (var i = 0; i < ex.Details.Count; i++)
                    items.Add(ex.Details[i]);
            }
            else
            {
                items.Add(ex.Message);
            }

            ShowMessageDialog(
                "Track load error",
                "The selected track could not be loaded. The race was not started.",
                items);
        }
    }
}


