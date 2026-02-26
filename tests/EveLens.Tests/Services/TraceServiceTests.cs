// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using EveLens.Common.Services;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Comprehensive tests for <see cref="TraceService"/>, verifying interface conformance,
    /// level filtering, no-throw guarantees, and file logging lifecycle.
    /// </summary>
    public class TraceServiceTests
    {
        #region Interface Conformance

        [Fact]
        public void TraceService_ImplementsITraceService()
        {
            // Arrange & Act
            var svc = new TraceService();

            // Assert
            svc.Should().BeAssignableTo<ITraceService>();
        }

        #endregion

        #region Default MinimumLevel

        [Fact]
        public void MinimumLevel_DefaultsToDebug()
        {
            // Arrange & Act
            var svc = new TraceService();

            // Assert
            svc.MinimumLevel.Should().Be(TraceLevel.Debug);
        }

        [Fact]
        public void MinimumLevel_CanBeSetToAnyLevel()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            foreach (TraceLevel level in Enum.GetValues(typeof(TraceLevel)))
            {
                svc.MinimumLevel = level;
                svc.MinimumLevel.Should().Be(level);
            }
        }

        #endregion

        #region No-Throw Guarantees

        [Fact]
        public void Trace_StringBool_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            var act = () => svc.Trace("test message", false);
            act.Should().NotThrow();
        }

        [Fact]
        public void Trace_StringBool_WithPrintMethod_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            var act = () => svc.Trace("test message", true);
            act.Should().NotThrow();
        }

        [Fact]
        public void Trace_FormatArgs_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            var act = () => svc.Trace("test {0} {1}", "arg1", "arg2");
            act.Should().NotThrow();
        }

        [Fact]
        public void Trace_LevelStringBool_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            foreach (TraceLevel level in Enum.GetValues(typeof(TraceLevel)))
            {
                var act = () => svc.Trace(level, "test message", false);
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void Trace_LevelFormatArgs_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            foreach (TraceLevel level in Enum.GetValues(typeof(TraceLevel)))
            {
                var act = () => svc.Trace(level, "test {0}", "arg");
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void Trace_EmptyMessage_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            var act = () => svc.Trace(string.Empty, false);
            act.Should().NotThrow();
        }

        #endregion

        #region Level Filtering

        [Theory]
        [InlineData(TraceLevel.Warning, TraceLevel.Debug, false)]
        [InlineData(TraceLevel.Warning, TraceLevel.Info, false)]
        [InlineData(TraceLevel.Warning, TraceLevel.Warning, true)]
        [InlineData(TraceLevel.Warning, TraceLevel.Error, true)]
        [InlineData(TraceLevel.Error, TraceLevel.Debug, false)]
        [InlineData(TraceLevel.Error, TraceLevel.Info, false)]
        [InlineData(TraceLevel.Error, TraceLevel.Warning, false)]
        [InlineData(TraceLevel.Error, TraceLevel.Error, true)]
        [InlineData(TraceLevel.Debug, TraceLevel.Debug, true)]
        [InlineData(TraceLevel.Debug, TraceLevel.Info, true)]
        [InlineData(TraceLevel.Debug, TraceLevel.Warning, true)]
        [InlineData(TraceLevel.Debug, TraceLevel.Error, true)]
        [InlineData(TraceLevel.Info, TraceLevel.Debug, false)]
        [InlineData(TraceLevel.Info, TraceLevel.Info, true)]
        public void LevelFiltering_WritesToFile_OnlyWhenLevelMeetsMinimum(
            TraceLevel minimumLevel, TraceLevel messageLevel, bool shouldAppear)
        {
            // Arrange
            var svc = new TraceService();
            svc.MinimumLevel = minimumLevel;
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"MARKER_{Guid.NewGuid():N}";

                // Act
                svc.Trace(messageLevel, marker, false);
                svc.StopLogging();

                // Assert
                string content = File.ReadAllText(tempFile);
                if (shouldAppear)
                    content.Should().Contain(marker);
                else
                    content.Should().NotContain(marker);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void LevelFiltering_FormatOverload_SuppressesBelowMinimum()
        {
            // Arrange
            var svc = new TraceService();
            svc.MinimumLevel = TraceLevel.Error;
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"FMT_{Guid.NewGuid():N}";

                // Act - Info level should be suppressed when minimum is Error
                svc.Trace(TraceLevel.Info, "fmt {0}", marker);
                svc.StopLogging();

                // Assert
                string content = File.ReadAllText(tempFile);
                content.Should().NotContain(marker);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void LevelFiltering_FormatOverload_AllowsAtOrAboveMinimum()
        {
            // Arrange
            var svc = new TraceService();
            svc.MinimumLevel = TraceLevel.Warning;
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"FMT_{Guid.NewGuid():N}";

                // Act - Error level should pass when minimum is Warning
                svc.Trace(TraceLevel.Error, "fmt {0}", marker);
                svc.StopLogging();

                // Assert
                string content = File.ReadAllText(tempFile);
                content.Should().Contain(marker);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region File Logging Lifecycle

        [Fact]
        public void StartLogging_CreatesTraceFile()
        {
            // Arrange
            var svc = new TraceService();
            string tempFile = Path.GetTempFileName();
            File.Delete(tempFile); // Remove so we can verify creation

            try
            {
                // Act
                svc.StartLogging(tempFile);

                // Assert
                File.Exists(tempFile).Should().BeTrue("StartLogging should create the trace file");

                svc.StopLogging();
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void StopLogging_ClosesFile_AllowsSubsequentAccess()
        {
            // Arrange
            var svc = new TraceService();
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                svc.Trace("message before stop", false);

                // Act
                svc.StopLogging();

                // Assert - file should be readable (not locked) after StopLogging
                var act = () => File.ReadAllText(tempFile);
                act.Should().NotThrow("file should not be locked after StopLogging");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void StopLogging_WhenNotStarted_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();

            // Act & Assert
            var act = () => svc.StopLogging();
            act.Should().NotThrow("StopLogging should be safe when logging was never started");
        }

        [Fact]
        public void StopLogging_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var svc = new TraceService();
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                svc.StopLogging();

                // Act & Assert
                var act = () => svc.StopLogging();
                act.Should().NotThrow("calling StopLogging twice should be safe");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void MessagesAreWrittenToTraceFile_AfterStartLogging()
        {
            // Arrange
            var svc = new TraceService();
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"MSG_{Guid.NewGuid():N}";

                // Act
                svc.Trace(marker, false);
                svc.StopLogging();

                // Assert
                string content = File.ReadAllText(tempFile);
                content.Should().Contain(marker, "message should be written to the trace file");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void MultipleMessages_AllWrittenToTraceFile()
        {
            // Arrange
            var svc = new TraceService();
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker1 = $"M1_{Guid.NewGuid():N}";
                string marker2 = $"M2_{Guid.NewGuid():N}";
                string marker3 = $"M3_{Guid.NewGuid():N}";

                // Act
                svc.Trace(marker1, false);
                svc.Trace(TraceLevel.Warning, marker2, false);
                svc.Trace(TraceLevel.Error, "fmt {0}", marker3);
                svc.StopLogging();

                // Assert
                string content = File.ReadAllText(tempFile);
                content.Should().Contain(marker1);
                content.Should().Contain(marker2);
                content.Should().Contain(marker3);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void LevelTag_AppearsInOutput_ForNonInfoLevels()
        {
            // Arrange
            var svc = new TraceService();
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);

                // Act
                svc.Trace(TraceLevel.Warning, "warn-msg", false);
                svc.Trace(TraceLevel.Error, "err-msg", false);
                svc.Trace(TraceLevel.Debug, "dbg-msg", false);
                svc.StopLogging();

                // Assert
                string content = File.ReadAllText(tempFile);
                content.Should().Contain("[Warning]");
                content.Should().Contain("[Error]");
                content.Should().Contain("[Debug]");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void InfoLevel_DoesNotIncludeLevelTag()
        {
            // Arrange
            var svc = new TraceService();
            svc.MinimumLevel = TraceLevel.Debug;
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"INFO_{Guid.NewGuid():N}";

                // Act - use the level overload explicitly with Info
                svc.Trace(TraceLevel.Info, marker, false);
                svc.StopLogging();

                // Assert - Info level doesn't get a [Info] tag
                string content = File.ReadAllText(tempFile);
                content.Should().Contain(marker);
                content.Should().NotContain("[Info]");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region Legacy Overload Routing

        [Fact]
        public void Trace_StringBool_RoutesToInfoLevel()
        {
            // Arrange - set minimum to Warning, so Info should be suppressed
            var svc = new TraceService();
            svc.MinimumLevel = TraceLevel.Warning;
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"LEGACY_{Guid.NewGuid():N}";

                // Act - legacy overload routes to Info
                svc.Trace(marker, false);
                svc.StopLogging();

                // Assert - should be suppressed because Info < Warning
                string content = File.ReadAllText(tempFile);
                content.Should().NotContain(marker,
                    "Trace(string,bool) routes to Info level, which should be filtered by Warning minimum");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Trace_FormatArgs_RoutesToInfoLevel()
        {
            // Arrange - set minimum to Warning, so Info should be suppressed
            var svc = new TraceService();
            svc.MinimumLevel = TraceLevel.Warning;
            string tempFile = Path.GetTempFileName();

            try
            {
                svc.StartLogging(tempFile);
                string marker = $"LEGFMT_{Guid.NewGuid():N}";

                // Act - legacy format overload routes to Info
                svc.Trace("msg {0}", marker);
                svc.StopLogging();

                // Assert - should be suppressed because Info < Warning
                string content = File.ReadAllText(tempFile);
                content.Should().NotContain(marker,
                    "Trace(string,params) routes to Info level, which should be filtered by Warning minimum");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion
    }

    /// <summary>
    /// Regression tests verifying AppServices defaults to standalone TraceService
    /// and that the circular delegation footgun (TraceServiceAdapter -> EveLensClient ->
    /// AppServices.TraceService -> TraceServiceAdapter -> StackOverflow) cannot occur
    /// under the default configuration.
    /// </summary>
    [Collection("AppServices")]
    public class TraceServiceCircularDelegationTests
    {
        public TraceServiceCircularDelegationTests()
        {
            AppServices.Reset();
        }

        [Fact]
        public void AppServices_DefaultTraceService_IsNotTraceServiceAdapter()
        {
            // The default must be the standalone TraceService, NOT TraceServiceAdapter.
            // If TraceServiceAdapter is registered as AppServices.TraceService, calling
            // Trace() causes infinite recursion: adapter -> EveLensClient.Trace() ->
            // AppServices.TraceService -> adapter -> StackOverflowException.
            var svc = AppServices.TraceService;

            svc.Should().NotBeNull();
            svc.Should().BeOfType<TraceService>(
                "TraceServiceAdapter causes infinite recursion when registered as AppServices.TraceService");
            svc.Should().NotBeOfType<TraceServiceAdapter>(
                "TraceServiceAdapter delegates to EveLensClient.Trace which delegates back to AppServices.TraceService");
        }

        [Fact]
        public void AppServices_Reset_RestoresStandaloneTraceService()
        {
            // After Reset, the trace service should still be the standalone TraceService,
            // not the legacy adapter that would cause circular delegation.
            var mock = NSubstitute.Substitute.For<ITraceService>();
            AppServices.SetTraceService(mock);
            AppServices.TraceService.Should().BeSameAs(mock);

            AppServices.Reset();

            AppServices.TraceService.Should().BeOfType<TraceService>(
                "Reset must restore the standalone TraceService, not the legacy adapter");
        }
    }
}
