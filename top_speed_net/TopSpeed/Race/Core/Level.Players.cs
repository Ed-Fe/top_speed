using System;
using TopSpeed.Audio;
using TS.Audio;

namespace TopSpeed.Race
{
    internal abstract partial class Level
    {
        protected virtual void LoadPositionSounds(
            AudioSourceHandle?[] playerSounds,
            AudioSourceHandle?[] positionSounds,
            AudioSourceHandle?[] finishSounds,
            int slots,
            int maxPositionLabel,
            bool useMaxForLast = false)
        {
            var safeSlots = Math.Max(0, Math.Min(slots, Math.Min(playerSounds.Length, Math.Min(positionSounds.Length, finishSounds.Length))));
            for (var i = 0; i < safeSlots; i++)
            {
                var playerNumber = i + 1;
                var positionIndex = useMaxForLast && i == safeSlots - 1
                    ? maxPositionLabel
                    : Math.Min(maxPositionLabel, playerNumber);
                playerSounds[i] = LoadLanguageSound($"race\\info\\player{playerNumber}");
                positionSounds[i] = LoadLanguageSound($"race\\info\\youarepos{positionIndex}");
                finishSounds[i] = LoadLanguageSound($"race\\info\\finished{positionIndex}");
            }

            LoadRandomSounds(RandomSound.Front, "race\\info\\front");
            LoadRandomSounds(RandomSound.Tail, "race\\info\\tail");
        }

        protected virtual void DisposePositionSounds(
            AudioSourceHandle?[] playerSounds,
            AudioSourceHandle?[] positionSounds,
            AudioSourceHandle?[] finishSounds,
            int slots)
        {
            var safeSlots = Math.Max(0, Math.Min(slots, Math.Min(playerSounds.Length, Math.Min(positionSounds.Length, finishSounds.Length))));
            for (var i = 0; i < safeSlots; i++)
            {
                DisposeSound(playerSounds[i]);
                DisposeSound(positionSounds[i]);
                DisposeSound(finishSounds[i]);
            }
        }

        protected virtual void LoadRaceUiSounds(out AudioSourceHandle? soundYouAre, out AudioSourceHandle? soundPlayer)
        {
            soundYouAre = LoadLanguageSound("race\\youare");
            soundPlayer = LoadLanguageSound("race\\player");
            _soundTheme4 = LoadLanguageSound("music\\theme4", streamFromDisk: false);
            _soundPause = LoadLanguageSound("race\\pause");
            _soundUnpause = LoadLanguageSound("race\\unpause");
            _soundTheme4.SetVolumePercent((int)Math.Round(_settings.MusicVolume * 100f));
        }

        protected virtual void SpeakRaceIntro(AudioSourceHandle? soundYouAre, AudioSourceHandle? soundPlayer, int playerNumber)
        {
            SpeakIfLoaded(soundYouAre);
            SpeakIfLoaded(soundPlayer);
            if (playerNumber >= 0 && playerNumber < _soundNumbers.Length)
                Speak(_soundNumbers[playerNumber]);
        }

        protected virtual void AnnounceFinishOrder(
            AudioSourceHandle?[] playerSounds,
            AudioSourceHandle?[] finishSounds,
            int playerNumber,
            ref int finishOrder)
        {
            if (playerNumber < 0 || playerNumber >= playerSounds.Length)
                return;
            if (finishSounds.Length == 0)
                return;

            SpeakIfLoaded(playerSounds[playerNumber], true);
            var finishIndex = Math.Min(finishOrder, finishSounds.Length - 1);
            SpeakIfLoaded(finishSounds[finishIndex], true);
            finishOrder++;
        }

        protected virtual void HandlePlayerInfoRequests(
            int maxPlayerIndex,
            Func<int, bool> hasPlayer,
            Func<int, string> vehicleNameForPlayer,
            Func<int, int> playerPercent)
        {
            if (_input.TryGetPlayerInfo(out var infoPlayer)
                && _acceptPlayerInfo
                && infoPlayer >= 0
                && infoPlayer <= maxPlayerIndex
                && hasPlayer(infoPlayer))
            {
                _acceptPlayerInfo = false;
                SpeakText(vehicleNameForPlayer(infoPlayer));
                PushEvent(Events.RaceEventType.AcceptPlayerInfo, 0.5f);
            }

            if (_input.TryGetPlayerPosition(out var positionPlayer)
                && _acceptPlayerInfo
                && _started
                && positionPlayer >= 0
                && positionPlayer <= maxPlayerIndex
                && hasPlayer(positionPlayer))
            {
                _acceptPlayerInfo = false;
                var perc = playerPercent(positionPlayer);
                SpeakText(FormatPercentageText(string.Empty, perc));
                PushEvent(Events.RaceEventType.AcceptPlayerInfo, 0.5f);
            }
        }

        protected virtual void SpeakIfLoaded(AudioSourceHandle? sound, bool unKey = false)
        {
            if (sound == null)
                return;
            Speak(sound, unKey);
        }
    }
}
