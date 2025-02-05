using System;

namespace DevToolKit.Models.Core
{
    public abstract class DisposableBase : IDisposable
    {
        private readonly object _disposeLock = new object();
        private bool _disposed;

        public bool IsDisposed => _disposed;

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            lock (_disposeLock)
            {
                if (!_disposed)
                {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                    _disposed = true;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        ~DisposableBase()
        {
            Dispose(false);
        }
    }
}
