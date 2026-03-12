using System;

namespace TopSpeed.Speech
{
    internal interface IGameSpeech : IDisposable
    {
        float ScreenReaderRateMs { get; set; }
        void Speak(string text);
        void Speak(string text, SpeechService.SpeakFlag flag);
    }
}
