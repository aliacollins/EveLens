// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Velopack.Logging;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="VelopackTraceLogger"/>, the sink that carries Velopack's
    /// account of an update into the EveLens trace log.
    /// </summary>
    /// <remarks>
    /// These exist because a macOS in-place update could fail leaving no evidence
    /// anywhere a user or maintainer could reach: Velopack had no logger installed, so
    /// its reason for refusing an update was discarded, and the only symptom was a
    /// version number that did not change. Every assertion here protects a piece of
    /// that missing evidence trail.
    /// </remarks>
    [Collection("AppServices")]
    public class VelopackTraceLoggerTests
    {
        public VelopackTraceLoggerTests() => AppServices.Reset();

        /// <summary>Captures trace output so forwarding can be asserted.</summary>
        private static (VelopackTraceLogger logger, List<string> traced) NewLogger()
        {
            var traced = new List<string>();
            var trace = Substitute.For<ITraceService>();
            trace.When(t => t.Trace(Arg.Any<string>(), Arg.Any<bool>()))
                 .Do(ci => traced.Add(ci.ArgAt<string>(0)));
            AppServices.SetTraceService(trace);
            return (new VelopackTraceLogger(), traced);
        }

        [Fact]
        public void ImplementsVelopackLoggerInterface()
        {
            // Velopack only accepts IVelopackLogger; if this breaks, the logger
            // silently stops being installed and we lose the evidence trail again.
            new VelopackTraceLogger().Should().BeAssignableTo<IVelopackLogger>();
        }

        [Fact]
        public void ForwardsInformationToTraceService()
        {
            var (logger, traced) = NewLogger();

            logger.Log(VelopackLogLevel.Information, "Applying package", null);

            traced.Should().ContainSingle()
                .Which.Should().Contain("Applying package").And.Contain("Velopack");
        }

        [Fact]
        public void DropsChattyLevelsBelowInformation()
        {
            var (logger, traced) = NewLogger();

            logger.Log(VelopackLogLevel.Trace, "chunk 1 of 900", null);
            logger.Log(VelopackLogLevel.Debug, "chunk 2 of 900", null);

            // Per-chunk download progress would drown the trace file for no gain.
            traced.Should().BeEmpty();
        }

        [Fact]
        public void IgnoresEmptyMessageWithNoException()
        {
            var (logger, traced) = NewLogger();

            logger.Log(VelopackLogLevel.Information, "   ", null);

            traced.Should().BeEmpty();
        }

        [Fact]
        public void IncludesExceptionTypeAndMessage()
        {
            var (logger, traced) = NewLogger();

            logger.Log(VelopackLogLevel.Error, "Failed to apply",
                new UnauthorizedAccessException("bundle is read-only"));

            traced.Should().ContainSingle().Which.Should()
                .Contain("Failed to apply")
                .And.Contain("UnauthorizedAccessException")
                .And.Contain("bundle is read-only");
        }

        [Fact]
        public void LastProblemStartsNull()
        {
            var (logger, _) = NewLogger();

            logger.LastProblem.Should().BeNull();
        }

        [Fact]
        public void InformationDoesNotCountAsAProblem()
        {
            var (logger, _) = NewLogger();

            logger.Log(VelopackLogLevel.Information, "Downloading update", null);

            // Only warnings and worse are shown to the user as a failure reason.
            logger.LastProblem.Should().BeNull();
        }

        [Theory]
        [InlineData(VelopackLogLevel.Warning)]
        [InlineData(VelopackLogLevel.Error)]
        [InlineData(VelopackLogLevel.Critical)]
        public void CapturesWarningsAndWorseAsLastProblem(VelopackLogLevel level)
        {
            var (logger, _) = NewLogger();

            logger.Log(level, "User cancelled elevation prompt", null);

            // This is the string the update dialog shows instead of silently
            // resetting the button and leaving the user on the old version.
            logger.LastProblem.Should().Be("User cancelled elevation prompt");
        }

        [Fact]
        public void LastProblemKeepsTheMostRecentFailure()
        {
            var (logger, _) = NewLogger();

            logger.Log(VelopackLogLevel.Warning, "first failure", null);
            logger.Log(VelopackLogLevel.Error, "second failure", null);

            logger.LastProblem.Should().Be("second failure");
        }

        [Fact]
        public void RecentLogKeepsOrderNewestLast()
        {
            var (logger, _) = NewLogger();

            logger.Log(VelopackLogLevel.Information, "step one", null);
            logger.Log(VelopackLogLevel.Information, "step two", null);

            string tail = logger.RecentLog();
            tail.IndexOf("step one", StringComparison.Ordinal).Should()
                .BeLessThan(tail.IndexOf("step two", StringComparison.Ordinal));
        }

        [Fact]
        public void RecentLogIsBoundedSoItCannotGrowForever()
        {
            var (logger, _) = NewLogger();

            for (int i = 0; i < 100; i++)
                logger.Log(VelopackLogLevel.Information, $"line {i}", null);

            string[] lines = logger.RecentLog()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            lines.Should().HaveCount(20);
            lines[^1].Should().Contain("line 99");
            logger.RecentLog().Should().NotContain("line 0 ");
        }

        [Fact]
        public void RedactsOsUserNameFromMacHomePathsBeforeTracing()
        {
            var (logger, traced) = NewLogger();

            // The exact shape UpdateMac logs during an apply. The trace log is what
            // users paste into GitHub issues, and it also leaves the machine on the
            // TCP diagnostic stream — the account name must never reach either.
            logger.Log(VelopackLogLevel.Information,
                "Replacing bundle at /Users/alia/Applications/EveLens.app", null);

            traced.Should().ContainSingle().Which.Should()
                .NotContain("alia").And.Contain("/Users/[REDACTED]/Applications");
        }

        [Fact]
        public void RedactsOsUserNameFromWindowsPathsBeforeTracing()
        {
            var (logger, traced) = NewLogger();

            logger.Log(VelopackLogLevel.Information,
                @"Reading packages from C:\Users\alia\AppData\Local\EveLens\packages", null);

            traced.Should().ContainSingle().Which.Should()
                .NotContain("alia").And.Contain(@"C:\Users\[REDACTED]\AppData");
        }

        [Fact]
        public void LastProblemIsRedactedBecauseTheDialogShowsIt()
        {
            var (logger, _) = NewLogger();

            // LastProblem becomes LastError becomes dialog text — and dialog text
            // gets screenshotted and pasted into issues just like the trace log.
            logger.Log(VelopackLogLevel.Error, "Failed to apply",
                new UnauthorizedAccessException(
                    "Access to the path '/Users/alia/Applications/EveLens.app' is denied."));

            logger.LastProblem.Should()
                .NotContain("alia").And.Contain("/Users/[REDACTED]/");
        }

        [Fact]
        public void RecentLogTailIsRedacted()
        {
            var (logger, _) = NewLogger();

            logger.Log(VelopackLogLevel.Information,
                "Extracting to /home/alia/.local/share/velopack", null);

            logger.RecentLog().Should()
                .NotContain("alia").And.Contain("/home/[REDACTED]/");
        }

        [Fact]
        public void LoggingNeverThrowsWhenTraceServiceIsAbsent()
        {
            // The startup hook installs this logger before any services exist —
            // it must never be the reason the app fails to start.
            AppServices.Reset();
            var logger = new VelopackTraceLogger();

            Action act = () => logger.Log(VelopackLogLevel.Error, "boom", new Exception("x"));

            act.Should().NotThrow();
        }

        [Fact]
        public void AppServicesExposesASingleSharedInstance()
        {
            // The startup hook and VelopackUpdateService must see the same log,
            // otherwise the service cannot report what the hook observed.
            AppServices.VelopackLogger.Should().BeSameAs(AppServices.VelopackLogger);
        }
    }
}
