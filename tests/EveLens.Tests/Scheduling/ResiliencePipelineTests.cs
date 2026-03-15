// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Scheduling.Resilience;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Scheduling
{
    public class ResiliencePipelineTests
    {
        private const long CharId = 12345L;

        #region ErrorClassifier

        [Fact]
        public void ErrorClassifier_ClassifiesCorrectly()
        {
            ErrorClassifier.Classify(200).Should().Be(ErrorClassifier.ErrorClass.Success);
            ErrorClassifier.Classify(304).Should().Be(ErrorClassifier.ErrorClass.Success);
            ErrorClassifier.Classify(401).Should().Be(ErrorClassifier.ErrorClass.Auth);
            ErrorClassifier.Classify(403).Should().Be(ErrorClassifier.ErrorClass.Auth);
            ErrorClassifier.Classify(429).Should().Be(ErrorClassifier.ErrorClass.RateLimit);
            ErrorClassifier.Classify(500).Should().Be(ErrorClassifier.ErrorClass.Transient);
            ErrorClassifier.Classify(502).Should().Be(ErrorClassifier.ErrorClass.Transient);
            ErrorClassifier.Classify(503).Should().Be(ErrorClassifier.ErrorClass.Transient);
            ErrorClassifier.Classify(-1).Should().Be(ErrorClassifier.ErrorClass.TokenRefresh);
            ErrorClassifier.Classify(0).Should().Be(ErrorClassifier.ErrorClass.Skipped);
            ErrorClassifier.Classify(400).Should().Be(ErrorClassifier.ErrorClass.Permanent);
            ErrorClassifier.Classify(404).Should().Be(ErrorClassifier.ErrorClass.Permanent);
        }

        [Fact]
        public void ErrorClassifier_IsTransient()
        {
            ErrorClassifier.IsTransient(500).Should().BeTrue();
            ErrorClassifier.IsTransient(502).Should().BeTrue();
            ErrorClassifier.IsTransient(-1).Should().BeTrue();
            ErrorClassifier.IsTransient(200).Should().BeFalse();
            ErrorClassifier.IsTransient(401).Should().BeFalse();
            ErrorClassifier.IsTransient(429).Should().BeFalse();
        }

        #endregion

        #region RetryPolicy

        [Fact]
        public async Task RetryPolicy_SuccessOnFirstAttempt_NoRetry()
        {
            var policy = new RetryPolicy(maxRetries: 2);
            int callCount = 0;

            var result = await policy.ExecuteAsync(CharId, () =>
            {
                callCount++;
                return Task.FromResult(new FetchOutcome { StatusCode = 200 });
            });

            result.StatusCode.Should().Be(200);
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task RetryPolicy_TransientThenSuccess_Retries()
        {
            var policy = new RetryPolicy(maxRetries: 2);
            int callCount = 0;

            var result = await policy.ExecuteAsync(CharId, () =>
            {
                callCount++;
                int status = callCount < 2 ? 502 : 200;
                return Task.FromResult(new FetchOutcome { StatusCode = status });
            });

            result.StatusCode.Should().Be(200);
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task RetryPolicy_AllTransient_ReturnsLastFailure()
        {
            var policy = new RetryPolicy(maxRetries: 1);
            int callCount = 0;

            var result = await policy.ExecuteAsync(CharId, () =>
            {
                callCount++;
                return Task.FromResult(new FetchOutcome { StatusCode = 503 });
            });

            result.StatusCode.Should().Be(503);
            callCount.Should().Be(2); // initial + 1 retry
        }

        [Fact]
        public async Task RetryPolicy_AuthError_NoRetry()
        {
            var policy = new RetryPolicy(maxRetries: 2);
            int callCount = 0;

            var result = await policy.ExecuteAsync(CharId, () =>
            {
                callCount++;
                return Task.FromResult(new FetchOutcome { StatusCode = 401 });
            });

            result.StatusCode.Should().Be(401);
            callCount.Should().Be(1); // no retry for auth errors
        }

        [Fact]
        public async Task RetryPolicy_RateLimitError_NoRetry()
        {
            var policy = new RetryPolicy(maxRetries: 2);
            int callCount = 0;

            var result = await policy.ExecuteAsync(CharId, () =>
            {
                callCount++;
                return Task.FromResult(new FetchOutcome { StatusCode = 429 });
            });

            result.StatusCode.Should().Be(429);
            callCount.Should().Be(1);
        }

        #endregion

        #region CircuitBreakerPolicy

        [Fact]
        public async Task CircuitBreaker_ClosedOnSuccess()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 3);

            var result = await cb.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CircuitBreaker_OpensAfterThreshold()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 3, openDuration: TimeSpan.FromMinutes(5));

            // Hit threshold with transient failures
            for (int i = 0; i < 3; i++)
            {
                await cb.ExecuteAsync(CharId, () =>
                    Task.FromResult(new FetchOutcome { StatusCode = 500 }));
            }

            // Next call should fail fast (circuit open)
            // First call after open is a probe, second is the actual fail-fast
            await cb.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 500 })); // probe

            var result = await cb.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 })); // should fail fast

            result.StatusCode.Should().Be(0); // circuit open, fail fast
        }

        [Fact]
        public async Task CircuitBreaker_ResetsOnSuccess()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 3, openDuration: TimeSpan.FromMinutes(5));

            // 2 failures (below threshold)
            for (int i = 0; i < 2; i++)
            {
                await cb.ExecuteAsync(CharId, () =>
                    Task.FromResult(new FetchOutcome { StatusCode = 500 }));
            }

            // Success resets counter
            await cb.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            // 2 more failures — still below threshold because counter was reset
            for (int i = 0; i < 2; i++)
            {
                await cb.ExecuteAsync(CharId, () =>
                    Task.FromResult(new FetchOutcome { StatusCode = 500 }));
            }

            // Should still be closed (only 2 consecutive failures)
            var result = await cb.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CircuitBreaker_PerCharacterIsolation()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMinutes(5));
            long charA = 111L, charB = 222L;

            // Char A hits threshold
            for (int i = 0; i < 2; i++)
            {
                await cb.ExecuteAsync(charA, () =>
                    Task.FromResult(new FetchOutcome { StatusCode = 500 }));
            }

            // Char B should be unaffected
            var resultB = await cb.ExecuteAsync(charB, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            resultB.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CircuitBreaker_AuthErrors_DontAffectCircuit()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 2, openDuration: TimeSpan.FromMinutes(5));

            // Auth errors don't count toward circuit threshold
            for (int i = 0; i < 5; i++)
            {
                await cb.ExecuteAsync(CharId, () =>
                    Task.FromResult(new FetchOutcome { StatusCode = 401 }));
            }

            // Circuit should still be closed
            var result = await cb.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public void CircuitBreaker_RemoveCharacter_CleansUp()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 2);
            cb.RemoveCharacter(CharId); // should not throw
        }

        [Fact]
        public void CircuitBreaker_ResetCharacter_ClearsState()
        {
            var cb = new CircuitBreakerPolicy(failureThreshold: 2);
            cb.ResetCharacter(CharId); // should not throw even for unknown char
        }

        #endregion

        #region CharacterAlivePolicy

        [Fact]
        public async Task CharacterAlive_RegisteredChar_PassesThrough()
        {
            var policy = new CharacterAlivePolicy();
            policy.Register(CharId);

            var result = await policy.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task CharacterAlive_UnregisteredChar_ShortCircuits()
        {
            var policy = new CharacterAlivePolicy();
            policy.Register(CharId);
            policy.Unregister(CharId);

            var result = await policy.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(0);
        }

        [Fact]
        public async Task CharacterAlive_UnknownChar_ShortCircuits()
        {
            var policy = new CharacterAlivePolicy();

            var result = await policy.ExecuteAsync(99999L, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(0);
        }

        #endregion

        #region ResiliencePipeline (full chain)

        [Fact]
        public async Task Pipeline_FullChain_SuccessPassesThrough()
        {
            var alive = new CharacterAlivePolicy();
            alive.Register(CharId);
            var pipeline = new ResiliencePipeline(
                alive,
                new CircuitBreakerPolicy(),
                new RetryPolicy(maxRetries: 1));

            var result = await pipeline.ExecuteAsync(CharId, () =>
                Task.FromResult(new FetchOutcome { StatusCode = 200 }));

            result.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Pipeline_FullChain_DeadCharacterShortCircuitsBeforeRetry()
        {
            var alive = new CharacterAlivePolicy();
            // NOT registered
            int fetchCalled = 0;

            var pipeline = new ResiliencePipeline(
                alive,
                new CircuitBreakerPolicy(),
                new RetryPolicy(maxRetries: 2));

            var result = await pipeline.ExecuteAsync(CharId, () =>
            {
                fetchCalled++;
                return Task.FromResult(new FetchOutcome { StatusCode = 200 });
            });

            result.StatusCode.Should().Be(0);
            fetchCalled.Should().Be(0); // never reached the fetch
        }

        [Fact]
        public async Task Pipeline_FullChain_TransientRetryThenSuccess()
        {
            var alive = new CharacterAlivePolicy();
            alive.Register(CharId);
            int callCount = 0;

            var pipeline = new ResiliencePipeline(
                alive,
                new CircuitBreakerPolicy(),
                new RetryPolicy(maxRetries: 2));

            var result = await pipeline.ExecuteAsync(CharId, () =>
            {
                callCount++;
                int status = callCount < 2 ? 502 : 200;
                return Task.FromResult(new FetchOutcome { StatusCode = status });
            });

            result.StatusCode.Should().Be(200);
            callCount.Should().Be(2);
        }

        #endregion
    }
}
