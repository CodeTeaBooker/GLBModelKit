using DevToolKit.Models.Events;
using DevToolKit.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    public abstract class BaseModelLoader : DisposableBase, IModelLoader
    {
        public event EventHandler<ModelLoadEventArgs> ModelLoadStateChanged;

        private volatile bool _isProcessing;
        protected CancellationTokenManager _tokenManager = new CancellationTokenManager();

        public virtual int MaxRetryCount { get; set; } = 3;
        public virtual float RetryDelay { get; set; } = 1.0f;

        public bool IsProcessing
        {
            get => _isProcessing;
            protected set => _isProcessing = value;
        }

        public async Task<GameObject> LoadModelAsync(string path, Transform parent = null, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();

            if (IsProcessing)
            {
                NotifyStateChanged(ModelLoadEventArgs.CreateLoadFailedEvent(path, "Loader busy"));
                return null;
            }

            IsProcessing = true;
            _tokenManager.Reset();

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _tokenManager.Token))
            {
                try
                {
                    if (!ValidatePath(path))
                    {
                        NotifyStateChanged(ModelLoadEventArgs.CreateLoadFailedEvent(path, "Invalid path"));
                        return null;
                    }

                    NotifyStateChanged(ModelLoadEventArgs.CreateLoadStartEvent(path));
                    var result = await ExecuteLoad(path, parent, linkedCts.Token);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    NotifyStateChanged(ModelLoadEventArgs.CreateLoadCancelledEvent(path));
                    throw;
                }
                catch (Exception ex)
                {
                    NotifyStateChanged(ModelLoadEventArgs.CreateLoadFailedEvent(path, ex.Message));
                    throw;
                }
                finally
                {
                    IsProcessing = false;
                }
            }
        }

        public virtual void Cancel()
        {
            ThrowIfDisposed();
            try
            {
                _tokenManager?.Dispose();
                IsProcessing = false;
            }
            catch (ObjectDisposedException) { }
        }

        protected abstract Task<GameObject> ExecuteLoad(string path, Transform parent, CancellationToken token);

        protected virtual bool ValidatePath(string path)
        {
            return !string.IsNullOrEmpty(path);
        }

        protected void NotifyStateChanged(ModelLoadEventArgs args)
        {
            if (!IsDisposed)
            {
                try
                {
                    ModelLoadStateChanged?.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BaseModelLoader] Error in event handler: {ex.Message}");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                try
                {
                    Cancel();
                }
                catch (ObjectDisposedException) { }
                _tokenManager?.Dispose();
                ModelLoadStateChanged = null;
            }
            base.Dispose(disposing);
        }
    }
}
