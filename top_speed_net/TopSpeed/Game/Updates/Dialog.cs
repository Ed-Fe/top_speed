using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void ShowUpdateProgressDialog()
        {
            var total = System.Threading.Volatile.Read(ref _updateTotalBytes);
            var downloaded = System.Threading.Volatile.Read(ref _updateDownloadedBytes);
            var percent = System.Threading.Volatile.Read(ref _updatePercent);
            var items = new List<DialogItem>
            {
                new DialogItem($"File size: {FormatBytes(total)}"),
                new DialogItem($"Downloaded size: {FormatBytes(downloaded)}"),
                new DialogItem($"Percentage: {percent}%")
            };

            var dialog = new Dialog(
                "Downloading update...",
                null,
                QuestionId.Close,
                items,
                onResult: _ => _updateProgressOpen = false);
            _dialogs.Show(dialog);
        }

        private void ShowUpdateCompleteDialog()
        {
            if (_updateCompleteOpen)
                return;

            _updateCompleteOpen = true;
            var items = new List<DialogItem>
            {
                new DialogItem("The update download is complete."),
                new DialogItem("Press OK to close the game and install the update.")
            };

            var dialog = new Dialog(
                "Update download complete.",
                null,
                QuestionId.Cancel,
                items,
                onResult: resultId =>
                {
                    _updateCompleteOpen = false;
                    if (resultId == QuestionId.Ok)
                        LaunchUpdaterAndExit();
                },
                new DialogButton(QuestionId.Ok, "OK"));
            _dialogs.Show(dialog);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "Unknown";
            var units = new[] { "B", "KB", "MB", "GB" };
            var index = 0;
            var value = (double)bytes;
            while (value >= 1024d && index < units.Length - 1)
            {
                value /= 1024d;
                index++;
            }

            return $"{value.ToString("0.##", CultureInfo.InvariantCulture)} {units[index]}";
        }
    }
}
