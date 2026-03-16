// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations.CCPAPI;
using CharMethods = EveLens.Common.Enumerations.CCPAPI.ESIAPICharacterMethods;
using EveLens.Common.Models;
using EveLens.Common.Notifications;
using EveLens.Common.Serialization.Eve;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Notifications
{
    /// <summary>
    /// Tests for the transient error suppression and error categorization systems.
    ///
    /// Transient ESI errors (5xx, timeouts, rate limits) are suppressed until they persist
    /// for 3 consecutive poll cycles. Auth failures (401/403) and not-found (404) are shown
    /// immediately since they require user action.
    ///
    /// Error categorization maps HTTP status codes to user-friendly labels so users can
    /// distinguish ESI hiccups from auth problems from rate limiting at a glance.
    /// </summary>
    public class ErrorNotificationTests
    {
        #region Helpers

        private static CCPCharacter CreateCharacter()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(1L, "Test Pilot");
            return new CCPCharacter(identity, services);
        }

        private static IAPIResult MockError(int statusCode)
        {
            var result = Substitute.For<IAPIResult>();
            result.HasError.Returns(true);
            result.ErrorCode.Returns(statusCode);
            result.ErrorMessage.Returns($"HTTP {statusCode}");
            return result;
        }

        private static IAPIResult MockSuccess()
        {
            var result = Substitute.For<IAPIResult>();
            result.HasError.Returns(false);
            result.ErrorCode.Returns(200);
            return result;
        }

        #endregion

        #region Transient Error Suppression

        [Fact]
        public void TransientError_FirstFailure_Suppressed()
        {
            var character = CreateCharacter();
            var error = MockError(500);

            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeFalse("first transient failure should be suppressed");
        }

        [Fact]
        public void TransientError_SecondFailure_StillSuppressed()
        {
            var character = CreateCharacter();
            var error = MockError(500);

            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeFalse("second transient failure should still be suppressed");
        }

        [Fact]
        public void TransientError_ThirdFailure_Notifies()
        {
            var character = CreateCharacter();
            var error = MockError(500);

            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeTrue("third consecutive failure should notify the user");
        }

        [Fact]
        public void TransientError_SuccessResetsCounter()
        {
            var character = CreateCharacter();
            var error = MockError(502);
            var success = MockSuccess();

            // Two failures, then success
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            character.ShouldNotifyError(success, CharMethods.CharacterSheet);

            // Next failure starts from 1 again
            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeFalse("success should reset the consecutive failure counter");
        }

        [Fact]
        public void TransientError_DifferentEndpoints_IndependentCounters()
        {
            var character = CreateCharacter();
            var error = MockError(503);

            // Two failures on location
            character.ShouldNotifyError(error, CharMethods.Location);
            character.ShouldNotifyError(error, CharMethods.Location);

            // Two failures on skills (independent counter)
            character.ShouldNotifyError(error, CharMethods.SkillQueue);
            character.ShouldNotifyError(error, CharMethods.SkillQueue);

            // Third failure on location — should notify
            bool notifyLocation = character.ShouldNotifyError(error, CharMethods.Location);

            notifyLocation.Should().BeTrue("each endpoint tracks failures independently");
        }

        [Fact]
        public void TransientError_RateLimit429_Suppressed()
        {
            var character = CreateCharacter();
            var error = MockError(429);

            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeFalse("rate limit errors should be treated as transient");
        }

        [Fact]
        public void TransientError_Timeout_Suppressed()
        {
            var character = CreateCharacter();
            var error = MockError(0); // timeout / connection error

            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeFalse("timeouts should be treated as transient");
        }

        [Fact]
        public void TransientError_TokenRefresh_Suppressed()
        {
            var character = CreateCharacter();
            var error = MockError(-1); // token refresh in-flight

            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeFalse("token refresh should be treated as transient");
        }

        [Fact]
        public void TransientError_OnlyOneNotificationPerCharacter()
        {
            var character = CreateCharacter();
            var error = MockError(500);

            // Drive location past threshold → notified
            character.ShouldNotifyError(error, CharMethods.Location);
            character.ShouldNotifyError(error, CharMethods.Location);
            character.ShouldNotifyError(error, CharMethods.Location);

            // Drive skills past threshold — should still be suppressed (one error per character)
            character.ShouldNotifyError(error, CharMethods.SkillQueue);
            character.ShouldNotifyError(error, CharMethods.SkillQueue);
            bool notify = character.ShouldNotifyError(error, CharMethods.SkillQueue);

            notify.Should().BeFalse("only one error notification per character at a time");
        }

        [Fact]
        public void TransientError_AfterFirstClears_SecondCanNotify()
        {
            var character = CreateCharacter();
            var error = MockError(500);
            var success = MockSuccess();

            // Location hits threshold → notified
            character.ShouldNotifyError(error, CharMethods.Location);
            character.ShouldNotifyError(error, CharMethods.Location);
            character.ShouldNotifyError(error, CharMethods.Location);

            // Location recovers → notification cleared
            character.ShouldNotifyError(success, CharMethods.Location);

            // Skills already past threshold (accumulated while location was shown)
            character.ShouldNotifyError(error, CharMethods.SkillQueue);
            character.ShouldNotifyError(error, CharMethods.SkillQueue);
            bool notify = character.ShouldNotifyError(error, CharMethods.SkillQueue);

            notify.Should().BeTrue("after first error clears, second endpoint can notify");
        }

        #endregion

        #region Immediate Error Notification (Auth/NotFound)

        [Theory]
        [InlineData(401)]
        [InlineData(403)]
        public void AuthError_NotifiesImmediately(int statusCode)
        {
            var character = CreateCharacter();
            var error = MockError(statusCode);

            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeTrue($"HTTP {statusCode} auth errors should notify immediately");
        }

        [Fact]
        public void NotFoundError_NotifiesImmediately()
        {
            var character = CreateCharacter();
            var error = MockError(404);

            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeTrue("HTTP 404 should notify immediately — character may be biomassed");
        }

        [Fact]
        public void AuthError_StillOnlyOnePerCharacter()
        {
            var character = CreateCharacter();
            var authError = MockError(401);

            // First auth error notifies
            character.ShouldNotifyError(authError, CharMethods.CharacterSheet);

            // Second auth error on different endpoint — suppressed (one per character)
            bool notify = character.ShouldNotifyError(authError, CharMethods.Location);

            notify.Should().BeFalse("auth errors still respect one-per-character limit");
        }

        #endregion

        #region Error Categorization

        [Theory]
        [InlineData(-1, "Token refresh")]
        [InlineData(0, "Connection error")]
        [InlineData(401, "Auth expired")]
        [InlineData(403, "Auth expired")]
        [InlineData(404, "Not found")]
        [InlineData(429, "Rate limited")]
        [InlineData(500, "ESI server error")]
        [InlineData(502, "ESI server error")]
        [InlineData(503, "ESI server error")]
        [InlineData(504, "ESI server error")]
        [InlineData(418, "ESI error")]
        public void CategoryLabel_MapsStatusCodeCorrectly(int statusCode, string expectedLabel)
        {
            var result = MockError(statusCode);
            var notification = new APIErrorNotificationEventArgs(new object(), result);

            notification.CategoryLabel.Should().Be(expectedLabel);
        }

        [Fact]
        public void CategoryLabel_NullResult_ReturnsDefault()
        {
            // Edge case: result is null (shouldn't happen, but defensive)
            var notification = new APIErrorNotificationEventArgs(new object(), null!);

            notification.CategoryLabel.Should().Be("ESI error");
        }

        #endregion

        #region Success Recovery

        [Fact]
        public void Success_ClearsNotifiedError()
        {
            var character = CreateCharacter();
            var error = MockError(500);
            var success = MockSuccess();

            // Drive past threshold
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            // Recover
            character.ShouldNotifyError(success, CharMethods.CharacterSheet);

            // Should be able to notify again after 3 more failures
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            character.ShouldNotifyError(error, CharMethods.CharacterSheet);
            bool notify = character.ShouldNotifyError(error, CharMethods.CharacterSheet);

            notify.Should().BeTrue("success should fully reset, allowing re-notification");
        }

        [Fact]
        public void Success_OnUnnotifiedEndpoint_DoesNothing()
        {
            var character = CreateCharacter();
            var success = MockSuccess();

            // Success on an endpoint that never errored — should not crash or have side effects
            bool notify = character.ShouldNotifyError(success, CharMethods.CharacterSheet);

            notify.Should().BeFalse("success on clean endpoint is a no-op");
        }

        #endregion

        #region Sleep/Wake Burst Scenario

        [Fact]
        public void SleepWakeBurst_AllTransientErrors_AllSuppressed()
        {
            var character = CreateCharacter();
            var timeout = MockError(0);
            var tokenRefresh = MockError(-1);
            var serverError = MockError(502);

            // Simulate wake-from-sleep: burst of different transient errors on same endpoint
            // Each is a DIFFERENT error type but only first failure of each
            character.ShouldNotifyError(timeout, CharMethods.Location).Should().BeFalse();
            character.ShouldNotifyError(tokenRefresh, CharMethods.CharacterSheet).Should().BeFalse();
            character.ShouldNotifyError(serverError, CharMethods.SkillQueue).Should().BeFalse();

            // None should have notified — all first-time transient
        }

        #endregion
    }
}
