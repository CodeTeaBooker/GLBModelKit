using DevToolKit.Models.Core;
using DevToolKit.Models.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DevToolKit.Models.Loaders
{
    public class RetryableModelLoader : BaseModelLoader
    {
        private readonly IModelLoader _innerLoader;
        private readonly IRetryStrategy _retryStrategy;
        private readonly CancellationTokenSource _linkedCancellation = new CancellationTokenSource();

        private const string LOG_DOMAIN = nameof(RetryableModelLoader);

        public override int MaxRetryCount => _retryStrategy.MaxRetryCount;
        public override float RetryDelay => _retryStrategy.RetryDelay;

        public RetryableModelLoader(IModelLoader innerLoader, IRetryStrategy retryStrategy)
        {
            _innerLoader = innerLoader ?? throw new ArgumentNullException(nameof(innerLoader));
            _retryStrategy = retryStrategy ?? throw new ArgumentNullException(nameof(retryStrategy));
            _innerLoader.ModelLoadStateChanged += OnInnerLoaderStateChanged;
        }

        private void OnInnerLoaderStateChanged(object sender, ModelLoadEventArgs e)
        {
            if (!IsDisposed)
                NotifyStateChanged(e);
        }

        protected override async Task<GameObject> ExecuteLoad(string path, Transform parent, CancellationToken token)
        {
            int attemptCount = 0;
            Exception lastException = null;

            while (true)
            {
                try
                {
                    return await _innerLoader.LoadModelAsync(path, parent, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attemptCount++;

                    if (!_retryStrategy.ShouldRetry(ex, attemptCount))
                    {
                        Debug.LogError($"[{LOG_DOMAIN}] All retry attempts failed for path: {path}. Attempts: {attemptCount}, Last error: {ex.Message}");
                        throw;
                    }

                    float delay = _retryStrategy.GetDelayForAttempt(attemptCount);
                    Debug.LogWarning($"[{LOG_DOMAIN}] Load attempt {attemptCount} failed: {ex.Message}. Retrying in {delay:F1} seconds...");

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delay), token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw lastException;
                    }
                }
            }
        }

        public override void Cancel()
        {
            base.Cancel();
            _linkedCancellation.Cancel();
            _innerLoader?.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                if (_innerLoader != null)
                {
                    _innerLoader.ModelLoadStateChanged -= OnInnerLoaderStateChanged;
                    if (_innerLoader is IDisposable disposable)
                    {
                        try { disposable.Dispose(); } catch (ObjectDisposedException) { }
                    }
                }
                _linkedCancellation.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
