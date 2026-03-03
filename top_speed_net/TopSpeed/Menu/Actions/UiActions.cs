using System;
using System.Collections.Generic;

namespace TopSpeed.Menu
{
    internal interface IMenuUiActions
    {
        void SpeakMessage(string text);
        void ShowMessageDialog(string title, string caption, IReadOnlyList<string> items);
        void ShowChoiceDialog(string title, string? caption, IReadOnlyDictionary<int, string> items, bool cancelable, string? cancelLabel, Action<ChoiceDialogResult>? onResult);
        void SpeakNotImplemented();
    }
}
