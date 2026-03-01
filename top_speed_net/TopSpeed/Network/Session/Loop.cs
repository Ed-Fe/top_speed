using System;
using System.Threading;
using System.Threading.Tasks;

namespace TopSpeed.Network.Session
{
    internal sealed class Loop : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Task _pollTask;
        private readonly Task _keepAliveTask;
        private readonly Action _drain;

        public Loop(Action poll, Action drain, Action keepAliveSend)
        {
            _drain = drain ?? throw new ArgumentNullException(nameof(drain));
            _cts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoop(poll, _cts.Token));
            _keepAliveTask = Task.Run(() => KeepAliveLoop(keepAliveSend, _cts.Token));
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _pollTask.Wait(250); } catch { }
            try { _keepAliveTask.Wait(250); } catch { }
            _cts.Dispose();
        }

        private void PollLoop(Action poll, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                poll();
                _drain();
                Thread.Sleep(1);
            }

            _drain();
        }

        private static async Task KeepAliveLoop(Action keepAliveSend, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                keepAliveSend();
                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }
    }
}
