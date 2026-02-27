using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Bookkeeping.Infrastructure.Policies;

/// <summary>
/// Factory for the shared Polly v8 ResiliencePipeline used on all DB operations.
/// Retry: 3 attempts, 200 ms exponential back-off.
/// Circuit-Breaker: opens after 50 % failure ratio over a 30 s window.
/// </summary>
public static class ResiliencePolicies
{
    public static ResiliencePipeline BuildDbPipeline(ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex is not OperationCanceledException),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception,
                        "DB operation retry {Attempt} after {Delay}",
                        args.AttemptNumber + 1, args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(10),
                OnOpened = args =>
                {
                    logger.LogError("DB circuit-breaker OPENED for {Duration}", args.BreakDuration);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("DB circuit-breaker CLOSED — resuming normal operation");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}
