using System;
using System.Diagnostics;
using System.Speech.Synthesis;
using System.Threading;

namespace TopSpeed.Speech
{
    internal sealed partial class SpeechService : IGameSpeech
    {
        public enum SpeakFlag
        {
            None,
            NoInterrupt,
            NoInterruptButStop,
            Interruptable,
            InterruptableButStop
        }

        private readonly Stopwatch _watch = new Stopwatch();
        private readonly JawsClient _jaws;
        private readonly NvdaClient _nvda;
        private SpeechSynthesizer? _sapi;
        private long _timeRequiredMs;
        private string _lastSpoken = string.Empty;
        private Func<bool>? _isInputHeld;

        public SpeechService(Func<bool>? isInputHeld = null)
        {
            _isInputHeld = isInputHeld;
            _jaws = new JawsClient();
            _nvda = new NvdaClient();
        }

        public bool IsAvailable => _jaws.IsAvailable || _nvda.IsAvailable || _sapi != null;

        public float ScreenReaderRateMs { get; set; }

        public void BindInputProbe(Func<bool> isInputHeld)
        {
            _isInputHeld = isInputHeld;
        }

        public void Speak(string text)
        {
            Speak(text, SpeakFlag.None);
        }

        public void Speak(string text, SpeakFlag flag)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (flag == SpeakFlag.NoInterruptButStop || flag == SpeakFlag.InterruptableButStop)
                Purge();

            text = text.Trim();
            _lastSpoken = text;

            var spoke = false;
            if (_jaws.IsAvailable)
            {
                spoke = _jaws.Speak(text, flag == SpeakFlag.NoInterruptButStop || flag == SpeakFlag.InterruptableButStop);
                if (spoke)
                    StartSpeakTimer(text);
            }

            if (!spoke && _nvda.IsAvailable)
            {
                spoke = _nvda.Speak(text);
                if (spoke)
                    StartSpeakTimer(text);
            }

            if (!spoke)
            {
                EnsureSapi();
                _sapi!.SpeakAsync(text);
                while (!IsSpeaking())
                {
                    Thread.Sleep(0);
                }
            }

            if (flag == SpeakFlag.None)
                return;

            if (flag == SpeakFlag.Interruptable || flag == SpeakFlag.InterruptableButStop)
            {
                while (IsInputHeld())
                {
                    if (!IsSpeaking())
                        break;
                    Thread.Sleep(0);
                }
            }

            while (IsSpeaking())
            {
                if ((flag == SpeakFlag.Interruptable || flag == SpeakFlag.InterruptableButStop) && IsInputHeld())
                    break;
                Thread.Sleep(10);
            }
        }

        public bool IsSpeaking()
        {
            if (_watch.IsRunning)
                return _watch.ElapsedMilliseconds < _timeRequiredMs;
            return _sapi != null && _sapi.State == SynthesizerState.Speaking;
        }

        public void Purge()
        {
            _watch.Reset();
            _timeRequiredMs = 0;
            if (_sapi != null)
            {
                try
                {
                    _sapi.SpeakAsyncCancelAll();
                }
                catch (OperationCanceledException)
                {
                }
                while (IsSpeaking())
                {
                    Thread.Sleep(0);
                }
            }
            _jaws.Stop();
            _nvda.Cancel();
        }

        public void Dispose()
        {
            Purge();
            _sapi?.Dispose();
            _nvda.Dispose();
        }

        private void EnsureSapi()
        {
            if (_sapi == null)
                _sapi = new SpeechSynthesizer();
        }

        private void StartSpeakTimer(string text)
        {
            if (ScreenReaderRateMs <= 0f)
            {
                _watch.Reset();
                _timeRequiredMs = 0;
                return;
            }

            var words = CountWords(text);
            _timeRequiredMs = (long)(words * ScreenReaderRateMs);
            _watch.Reset();
            _watch.Start();
        }

        private static int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private bool IsInputHeld()
        {
            try
            {
                return _isInputHeld != null && _isInputHeld();
            }
            catch
            {
                return false;
            }
        }
    }
}
