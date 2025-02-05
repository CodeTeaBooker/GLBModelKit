using System;
using System.Net.Http;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    public class DefaultRetryStrategy : IRetryStrategy
    {
        private const string LOG_DOMAIN = nameof(DefaultRetryStrategy);

        public int MaxRetryCount { get; }
        public float RetryDelay { get; }

        private readonly bool _retryOnTimeout;
        private readonly bool _retryOnNetworkError;

        public DefaultRetryStrategy(
            int maxRetryCount = 3,
            float retryDelay = 1.0f,
            bool retryOnTimeout = true,
            bool retryOnNetworkError = true)
        {
            if (maxRetryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetryCount), "Max retry count must be greater than 0");

            if (retryDelay <= 0)
                throw new ArgumentOutOfRangeException(nameof(retryDelay), "Retry delay must be greater than 0");

            MaxRetryCount = maxRetryCount;
            RetryDelay = retryDelay;
            _retryOnTimeout = retryOnTimeout;
            _retryOnNetworkError = retryOnNetworkError;
        }

        public bool ShouldRetry(Exception ex, int attemptCount)
        {
            if (ex == null)
            {
                Debug.LogWarning($"[{LOG_DOMAIN}] Null exception passed to ShouldRetry");
                return false;
            }

            // Check retry count
            if (attemptCount >= MaxRetryCount)
            {
                Debug.Log($"[{LOG_DOMAIN}] Maximum retry attempts ({MaxRetryCount}) reached");
                return false;
            }

            // Never retry these exceptions
            if (ex is OperationCanceledException ||
                ex is ObjectDisposedException ||
                ex is ArgumentException ||
                ex is NotSupportedException)
            {
                Debug.Log($"[{LOG_DOMAIN}] Non-retriable exception type: {ex.GetType().Name}");
                return false;
            }

            // Optionally retry these exceptions
            if (ex is TimeoutException)
            {
                return _retryOnTimeout;
            }

            if (ex is HttpRequestException)
            {
                return _retryOnNetworkError;
            }

            // GLB model specific errors
            if (ex.Message.Contains("Failed to load GLB") ||
                ex.Message.Contains("Error parsing GLB"))
            {
                Debug.Log($"[{LOG_DOMAIN}] GLB parsing error, no retry");
                return false;
            }

            // IO or network errors are usually retriable
            if (ex is System.IO.IOException ||
                ex is System.Net.Sockets.SocketException)
            {
                return true;
            }

            // Default to allowing retry
            Debug.Log($"[{LOG_DOMAIN}] Using default retry behavior for exception type: {ex.GetType().Name}");
            return true;
        }

        public virtual float GetDelayForAttempt(int attemptCount)
        {
            // Add small random jitter to prevent thundering herd
            return RetryDelay * UnityEngine.Random.Range(0.8f, 1.2f);
        }

        public override string ToString()
        {
            return $"DefaultRetryStrategy[MaxRetry:{MaxRetryCount}, Delay:{RetryDelay}s]";
        }
    }
}