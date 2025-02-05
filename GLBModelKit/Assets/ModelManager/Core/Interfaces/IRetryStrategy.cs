using System;

namespace DevToolKit.Models.Core
{
    public interface IRetryStrategy
    {
        int MaxRetryCount { get; }
        float RetryDelay { get; }
        bool ShouldRetry(Exception ex, int attemptCount);
        float GetDelayForAttempt(int attemptCount);
    }
}
