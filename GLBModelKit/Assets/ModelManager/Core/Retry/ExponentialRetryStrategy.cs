using System;
using UnityEngine;

namespace DevToolKit.Models.Core
{
    /// <summary>
    /// A retry strategy that implements exponential backoff
    /// </summary>
    public class ExponentialRetryStrategy : DefaultRetryStrategy
    {
        private readonly float _maxDelay;
        private readonly float _baseDelay;

        public ExponentialRetryStrategy(
            int maxRetryCount = 3,
            float baseDelay = 1.0f,
            float maxDelay = 10.0f,
            bool retryOnTimeout = true,
            bool retryOnNetworkError = true)
            : base(maxRetryCount, baseDelay, retryOnTimeout, retryOnNetworkError)
        {
            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
        }

        public override float GetDelayForAttempt(int attemptCount)
        {
            // Calculate exponential backoff delay: baseDelay * 2^(attemptCount-1)
            float delay = _baseDelay * (float)Math.Pow(2, attemptCount - 1);

            // Add random jitter to prevent retry storms
            delay *= UnityEngine.Random.Range(0.8f, 1.2f);

            // Ensure delay doesn't exceed maximum
            return Mathf.Min(delay, _maxDelay);
        }

        public override string ToString()
        {
            return $"ExponentialRetryStrategy[MaxRetry:{MaxRetryCount}, Base:{_baseDelay}s, Max:{_maxDelay}s]";
        }
    }
}