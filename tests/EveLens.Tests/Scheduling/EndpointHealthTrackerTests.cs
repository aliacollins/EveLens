// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Infrastructure.Scheduling.Health;
using EveLens.Common.Services;
using FluentAssertions;
using EveLens.Core.Events;
using EveLens.Core.Interfaces;
using Xunit;

namespace EveLens.Tests.Scheduling
{
    public class EndpointHealthTrackerTests
    {
        private const long CharId = 12345L;
        private const long CharId2 = 67890L;
        private const long Skills = 100L;
        private const long Assets = 200L;
        private const long Location = 300L;

        private readonly IEventAggregator _aggregator;
        private readonly EndpointHealthTracker _tracker;
        private readonly DateTime _baseTime = new(2026, 3, 19, 12, 0, 0, DateTimeKind.Utc);

        public EndpointHealthTrackerTests()
        {
            _aggregator = new EventAggregator();
            // No dispatcher — events fire synchronously in tests
            _tracker = new EndpointHealthTracker(_aggregator);
        }

        #region Helpers

        private FetchRecord Success(DateTime timestamp, TimeSpan? cache = null) => new()
        {
            Timestamp = timestamp,
            StatusCode = 200,
            CacheDuration = cache ?? TimeSpan.FromMinutes(2)
        };

        private FetchRecord NotModified(DateTime timestamp, TimeSpan? cache = null) => new()
        {
            Timestamp = timestamp,
            StatusCode = 304,
            CacheDuration = cache ?? TimeSpan.FromMinutes(2)
        };

        private FetchRecord Error(DateTime timestamp, int statusCode = 503, TimeSpan? cache = null) => new()
        {
            Timestamp = timestamp,
            StatusCode = statusCode,
            CacheDuration = cache ?? TimeSpan.FromMinutes(2)
        };

        private FetchRecord AuthError(DateTime timestamp) => new()
        {
            Timestamp = timestamp,
            StatusCode = 401,
            CacheDuration = TimeSpan.FromMinutes(2)
        };

        /// <summary>
        /// Records N consecutive failures starting at the given time, spaced 1 minute apart.
        /// Returns the timestamp after the last failure.
        /// </summary>
        private DateTime RecordFailures(int count, DateTime start, long charId = CharId,
            long endpoint = Skills, int statusCode = 503)
        {
            var t = start;
            for (int i = 0; i < count; i++)
            {
                _tracker.Record(charId, endpoint, Error(t, statusCode));
                t = t.AddMinutes(1);
            }
            return t;
        }

        /// <summary>
        /// Records N consecutive successes starting at the given time, spaced 1 minute apart.
        /// Returns the timestamp after the last success.
        /// </summary>
        private DateTime RecordSuccesses(int count, DateTime start, long charId = CharId,
            long endpoint = Skills)
        {
            var t = start;
            for (int i = 0; i < count; i++)
            {
                _tracker.Record(charId, endpoint, Success(t));
                t = t.AddMinutes(1);
            }
            return t;
        }

        #endregion

        #region Initial State

        [Fact]
        public void NewEndpoint_DefaultsToHealthy()
        {
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void NewCharacter_SummaryIsHealthy()
        {
            var summary = _tracker.GetCharacterHealth(CharId);
            summary.OverallHealth.Should().Be(CharacterHealth.Healthy);
            summary.HealthyCount.Should().Be(0);
        }

        #endregion

        #region Healthy -> Degraded

        [Fact]
        public void Healthy_OneFailure_StaysHealthy()
        {
            _tracker.Record(CharId, Skills, Error(_baseTime));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void Healthy_TwoFailures_StaysHealthy()
        {
            RecordFailures(2, _baseTime);

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void Healthy_ThreeFailures_TransitionsToDegraded()
        {
            RecordFailures(3, _baseTime);

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Healthy_ThreeFailuresAmongManySuccesses_StaysHealthy()
        {
            // 7 successes + 3 failures = 30% exactly, but only 3 failures
            // The rule is failures >= 3 AND failRate >= 30%
            // With 3/10 = 30%, this should trigger Degraded
            var t = _baseTime;
            for (int i = 0; i < 7; i++)
            {
                _tracker.Record(CharId, Skills, Success(t));
                t = t.AddMinutes(0.5);
            }
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, Error(t));
                t = t.AddMinutes(0.5);
            }

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Healthy_ThreeFailuresAmongManyMoreSuccesses_StaysHealthy()
        {
            // 8 successes + 3 failures = 27.3% < 30% threshold
            var t = _baseTime;
            for (int i = 0; i < 8; i++)
            {
                _tracker.Record(CharId, Skills, Success(t));
                t = t.AddMinutes(0.5);
            }
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, Error(t));
                t = t.AddMinutes(0.5);
            }

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        #endregion

        #region Degraded -> Failing

        [Fact]
        public void Degraded_HighFailureRate_TransitionsToFailing()
        {
            // Get to Degraded first
            RecordFailures(3, _baseTime);
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);

            // Now add more failures to reach 70% with >= 5 total
            // We have 3 failures. Add 1 success + 2 more failures = 5/6 = 83% > 70%
            var t = _baseTime.AddMinutes(3);
            _tracker.Record(CharId, Skills, Success(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Error(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Error(t));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Failing);
        }

        [Fact]
        public void Degraded_ModerateFailureRate_StaysDegraded()
        {
            // Get to Degraded
            RecordFailures(3, _baseTime);

            // Dilute with successes to bring rate below 70% but above 30%
            // Pattern: F,F,F,S,S,F,S,S = 4 fail / 8 total = 50%
            // No 3 consecutive successes (breaks at the F in middle)
            var t = _baseTime.AddMinutes(3);
            _tracker.Record(CharId, Skills, Success(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Success(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Error(t)); // breaks consecutive, 4/6=67% but total=6 >= 5, 67% < 70%
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Success(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Success(t)); // 4/8=50%, consec=2

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        #endregion

        #region Recovery (Degraded/Failing -> Healthy) — Hysteresis

        [Fact]
        public void Degraded_OneSuccess_StaysDegraded()
        {
            RecordFailures(3, _baseTime);
            _tracker.Record(CharId, Skills, Success(_baseTime.AddMinutes(3)));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Degraded_TwoConsecutiveSuccesses_StaysDegraded()
        {
            RecordFailures(3, _baseTime);
            RecordSuccesses(2, _baseTime.AddMinutes(3));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Degraded_ThreeConsecutiveSuccesses_RecoversToHealthy()
        {
            RecordFailures(3, _baseTime);
            RecordSuccesses(3, _baseTime.AddMinutes(3));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void Degraded_TwoSuccessesThenFailure_StaysDegraded()
        {
            RecordFailures(3, _baseTime);
            var t = _baseTime.AddMinutes(3);
            _tracker.Record(CharId, Skills, Success(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Success(t));
            t = t.AddMinutes(1);
            _tracker.Record(CharId, Skills, Error(t)); // Breaks consecutive successes

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Failing_ThreeConsecutiveSuccesses_RecoversToHealthy()
        {
            // Get to Failing: 5 consecutive failures
            RecordFailures(5, _baseTime);
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Failing);

            // Recover with 3 consecutive successes
            RecordSuccesses(3, _baseTime.AddMinutes(5));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void Failing_FailingSince_ClearedOnRecovery()
        {
            RecordFailures(5, _baseTime);
            var summary = _tracker.GetCharacterHealth(CharId);
            summary.FailingSince.Should().NotBeNull();

            RecordSuccesses(3, _baseTime.AddMinutes(5));
            summary = _tracker.GetCharacterHealth(CharId);
            summary.FailingSince.Should().BeNull();
        }

        #endregion

        #region Auth -> Suspended

        [Fact]
        public void AnyState_AuthError_ImmediatelySuspended()
        {
            _tracker.Record(CharId, Skills, AuthError(_baseTime));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Suspended);
        }

        [Fact]
        public void Healthy_ForbiddenError_ImmediatelySuspended()
        {
            _tracker.Record(CharId, Skills, Error(_baseTime, statusCode: 403));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Suspended);
        }

        [Fact]
        public void Degraded_AuthError_TransitionsToSuspended()
        {
            RecordFailures(3, _baseTime);
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);

            _tracker.Record(CharId, Skills, AuthError(_baseTime.AddMinutes(3)));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Suspended);
        }

        [Fact]
        public void Suspended_SuccessDoesNotRecover()
        {
            _tracker.Record(CharId, Skills, AuthError(_baseTime));
            RecordSuccesses(5, _baseTime.AddMinutes(1));

            // Suspended only exits via OnReAuthenticated
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Suspended);
        }

        #endregion

        #region Re-Authentication

        [Fact]
        public void OnReAuthenticated_AllEndpointsResetToHealthy()
        {
            _tracker.Record(CharId, Skills, AuthError(_baseTime));
            _tracker.Record(CharId, Assets, Error(_baseTime));
            RecordFailures(3, _baseTime, endpoint: Assets);

            _tracker.OnReAuthenticated(CharId);

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
            _tracker.GetHealth(CharId, Assets).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void OnReAuthenticated_DoesNotAffectOtherCharacters()
        {
            _tracker.Record(CharId, Skills, AuthError(_baseTime));
            _tracker.Record(CharId2, Skills, AuthError(_baseTime));

            _tracker.OnReAuthenticated(CharId);

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
            _tracker.GetHealth(CharId2, Skills).Should().Be(EndpointHealth.Suspended);
        }

        #endregion

        #region Dynamic Time Window

        [Fact]
        public void Window_FastEndpoint_UsesFloor()
        {
            // 5 second cache * 5 = 25 seconds, clamped to floor of 5 minutes
            // Failures from 6 minutes ago should be outside window
            var t = _baseTime;
            // Record 3 failures with 5-second cache
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Location, Error(t, cache: TimeSpan.FromSeconds(5)));
                t = t.AddMinutes(1);
            }
            _tracker.GetHealth(CharId, Location).Should().Be(EndpointHealth.Degraded);

            // Now record 3 successes 6 minutes later — old failures should age out of 5min window
            t = _baseTime.AddMinutes(8);
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Location, Success(t, cache: TimeSpan.FromSeconds(5)));
                t = t.AddMinutes(0.5);
            }

            _tracker.GetHealth(CharId, Location).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void Window_MediumEndpoint_ScalesCorrectly()
        {
            // 2 minute cache * 5 = 10 minutes
            // Failures from 9 minutes ago should still be in window
            var t = _baseTime;
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, Error(t, cache: TimeSpan.FromMinutes(2)));
                t = t.AddMinutes(1);
            }

            // 3 failures at t+0, t+1, t+2. Now at t+9 (within 10min window), record 3 successes
            t = _baseTime.AddMinutes(9);
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, Success(t, cache: TimeSpan.FromMinutes(2)));
                t = t.AddMinutes(0.1);
            }

            // 3 failures + 3 successes in window = 50% fail rate, still Degraded
            // But 3 consecutive successes triggers hysteresis recovery
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void Window_SlowEndpoint_UsesCeiling()
        {
            // 1 hour cache * 5 = 5 hours, clamped to ceiling of 30 minutes
            // Failures from 35 minutes ago should be outside window
            var t = _baseTime;
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Assets, Error(t, cache: TimeSpan.FromHours(1)));
                t = t.AddMinutes(5);
            }
            _tracker.GetHealth(CharId, Assets).Should().Be(EndpointHealth.Degraded);

            // 35 minutes later, record a single success — old failures aged out of 30min window
            t = _baseTime.AddMinutes(35);
            _tracker.Record(CharId, Assets, Success(t, cache: TimeSpan.FromHours(1)));

            // Only 1 outcome in window (the success) — insufficient data for transition check
            // But we're in Degraded, and we don't have 3 consecutive successes yet
            // The failures aged out, but we need 3 consecutive successes to recover
            _tracker.GetHealth(CharId, Assets).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Window_CacheDurationChanges_WindowAdjusts()
        {
            // Start with 2-minute cache
            _tracker.Record(CharId, Skills, Error(_baseTime, cache: TimeSpan.FromMinutes(2)));

            // Switch to 30-second cache — window should shrink to floor (5 min)
            _tracker.Record(CharId, Skills, Error(_baseTime.AddMinutes(1), cache: TimeSpan.FromSeconds(30)));
            _tracker.Record(CharId, Skills, Error(_baseTime.AddMinutes(2), cache: TimeSpan.FromSeconds(30)));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        [Fact]
        public void Window_OldOutcomesExcluded()
        {
            // Record 3 failures with 2min cache (window = 10min)
            RecordFailures(3, _baseTime);
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);

            // Fast forward 15 minutes — failures are now outside the 10min window
            // Record 3 successes — now only successes in window
            RecordSuccesses(3, _baseTime.AddMinutes(15));

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void RingBuffer_WrapsAt20()
        {
            // Fill buffer with 20 successes, then 3 failures
            var t = _baseTime;
            for (int i = 0; i < 20; i++)
            {
                _tracker.Record(CharId, Skills, Success(t));
                t = t.AddSeconds(10);
            }

            // Now add 3 failures — ring buffer wraps, oldest successes evicted
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, Error(t));
                t = t.AddSeconds(10);
            }

            // 17 successes + 3 failures in buffer (20 total)
            // 3/20 = 15% < 30% → stays Healthy
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void MultipleCharacters_Isolated()
        {
            RecordFailures(3, _baseTime, charId: CharId);
            RecordSuccesses(3, _baseTime, charId: CharId2);

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
            _tracker.GetHealth(CharId2, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void MultipleEndpoints_Isolated()
        {
            RecordFailures(3, _baseTime, endpoint: Skills);
            RecordSuccesses(3, _baseTime, endpoint: Assets);

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
            _tracker.GetHealth(CharId, Assets).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void RemoveCharacter_CleansUpState()
        {
            RecordFailures(3, _baseTime);
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);

            _tracker.RemoveCharacter(CharId);

            // After removal, defaults to Healthy (no state)
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void SkippedStatus_DoesNotCountAsFailure()
        {
            // StatusCode 0 = circuit open / dead character (Skipped)
            var t = _baseTime;
            for (int i = 0; i < 5; i++)
            {
                _tracker.Record(CharId, Skills, Error(t, statusCode: 0));
                t = t.AddMinutes(1);
            }

            // Skipped outcomes don't count as successes or failures
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void NotModified304_CountsAsSuccess()
        {
            RecordFailures(3, _baseTime);
            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);

            var t = _baseTime.AddMinutes(3);
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, NotModified(t));
                t = t.AddMinutes(1);
            }

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Healthy);
        }

        [Fact]
        public void RateLimited429_CountsAsFailure()
        {
            var t = _baseTime;
            for (int i = 0; i < 3; i++)
            {
                _tracker.Record(CharId, Skills, Error(t, statusCode: 429));
                t = t.AddMinutes(1);
            }

            _tracker.GetHealth(CharId, Skills).Should().Be(EndpointHealth.Degraded);
        }

        #endregion

        #region CharacterHealthSummary

        [Fact]
        public void Summary_AllHealthy_ReturnsHealthy()
        {
            RecordSuccesses(3, _baseTime, endpoint: Skills);
            RecordSuccesses(3, _baseTime, endpoint: Assets);

            var summary = _tracker.GetCharacterHealth(CharId);
            summary.OverallHealth.Should().Be(CharacterHealth.Healthy);
            summary.HealthyCount.Should().Be(2);
        }

        [Fact]
        public void Summary_AnyDegraded_ReturnsDegraded()
        {
            RecordSuccesses(3, _baseTime, endpoint: Skills);
            RecordFailures(3, _baseTime, endpoint: Assets);

            var summary = _tracker.GetCharacterHealth(CharId);
            summary.OverallHealth.Should().Be(CharacterHealth.Degraded);
            summary.DegradedCount.Should().Be(1);
            summary.HealthyCount.Should().Be(1);
        }

        [Fact]
        public void Summary_AnyFailing_ReturnsFailing()
        {
            RecordSuccesses(3, _baseTime, endpoint: Skills);
            RecordFailures(5, _baseTime, endpoint: Assets);

            var summary = _tracker.GetCharacterHealth(CharId);
            summary.OverallHealth.Should().Be(CharacterHealth.Failing);
            summary.FailingCount.Should().Be(1);
        }

        [Fact]
        public void Summary_AnySuspended_TakesPriority()
        {
            RecordFailures(5, _baseTime, endpoint: Skills);
            _tracker.Record(CharId, Assets, AuthError(_baseTime));

            var summary = _tracker.GetCharacterHealth(CharId);
            summary.OverallHealth.Should().Be(CharacterHealth.Suspended);
        }

        [Fact]
        public void Summary_FailingSince_TrackesEarliestFailing()
        {
            RecordFailures(5, _baseTime, endpoint: Skills);
            RecordFailures(5, _baseTime.AddMinutes(2), endpoint: Assets);

            var summary = _tracker.GetCharacterHealth(CharId);
            summary.FailingSince.Should().NotBeNull();
        }

        #endregion

        #region Event Publishing

        [Fact]
        public void TransitionPublishesEvent()
        {
            HealthStateChangedEvent? received = null;
            _aggregator.Subscribe<HealthStateChangedEvent>(e => received = e);

            RecordFailures(3, _baseTime);

            received.Should().NotBeNull();
            received!.CharacterId.Should().Be(CharId);
            received.OldState.Should().Be(HealthStateChangedEvent.StateHealthy);
            received.NewState.Should().Be(HealthStateChangedEvent.StateDegraded);
        }

        [Fact]
        public void SameState_DoesNotPublishEvent()
        {
            int eventCount = 0;
            _aggregator.Subscribe<HealthStateChangedEvent>(_ => eventCount++);

            // First 3 failures: Healthy -> Degraded (1 event)
            RecordFailures(3, _baseTime);
            eventCount.Should().Be(1);

            // One more failure while already Degraded — no new event
            // (Only 1 extra to stay below the 5-sample Failing threshold)
            _tracker.Record(CharId, Skills, Error(_baseTime.AddMinutes(3)));
            eventCount.Should().Be(1);
        }

        [Fact]
        public void Event_IncludesCharacterSummary()
        {
            // Set up two endpoints — one healthy, one about to degrade
            RecordSuccesses(3, _baseTime, endpoint: Assets);

            HealthStateChangedEvent? received = null;
            _aggregator.Subscribe<HealthStateChangedEvent>(e => received = e);

            RecordFailures(3, _baseTime, endpoint: Skills);

            received.Should().NotBeNull();
            received!.DegradedCount.Should().Be(1);
            received.HealthyCount.Should().Be(1);
        }

        [Fact]
        public void ReAuth_PublishesTransitionEvent()
        {
            _tracker.Record(CharId, Skills, AuthError(_baseTime));

            HealthStateChangedEvent? received = null;
            _aggregator.Subscribe<HealthStateChangedEvent>(e => received = e);

            _tracker.OnReAuthenticated(CharId);

            received.Should().NotBeNull();
            received!.OldState.Should().Be(HealthStateChangedEvent.StateSuspended);
            received!.NewState.Should().Be(HealthStateChangedEvent.StateHealthy);
        }

        #endregion
    }
}
