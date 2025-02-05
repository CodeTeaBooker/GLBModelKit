using System;
using System.Threading;

namespace DevToolKit.Utilities
{
    public class CancellationTokenManager : IDisposable
    {
        private CancellationTokenSource _cts;

        public CancellationTokenManager()
        {
            _cts = new CancellationTokenSource();
        }

        public CancellationToken Token => _cts.Token;

        public void Reset()
        {
            DisposeToken();
            _cts = new CancellationTokenSource();
        }

        private void DisposeToken()
        {
            if (_cts != null)
            {
                try
                {
                    if (!_cts.IsCancellationRequested)
                        _cts.Cancel();
                }
                catch (ObjectDisposedException) { }
                finally
                {
                    _cts.Dispose();
                }
            }
        }

        public void Dispose()
        {
            DisposeToken();
        }
    }
}
