// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using EveLens.Common.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EveLens.Tests.Logging
{
    public class TraceLoggerProviderTests : IDisposable
    {
        private readonly TraceLoggerProvider _provider = new TraceLoggerProvider();
        private readonly TestTraceListener _listener = new TestTraceListener();

        public TraceLoggerProviderTests()
        {
            Trace.Listeners.Add(_listener);
        }

        public void Dispose()
        {
            Trace.Listeners.Remove(_listener);
            _provider.Dispose();
        }

        [Fact]
        public void CreateLogger_ReturnsNonNull()
        {
            var logger = _provider.CreateLogger("Category");
            logger.Should().NotBeNull();
        }

        [Fact]
        public void Logger_WritesToSystemDiagnosticsTrace()
        {
            // Arrange
            var logger = _provider.CreateLogger("TestService");

            // Act
            logger.LogWarning("something happened");

            // Assert
            _listener.LastMessage.Should().NotBeNullOrEmpty();
            _listener.LastMessage.Should().Contain("TestService");
            _listener.LastMessage.Should().Contain("something happened");
        }

        [Fact]
        public void Logger_FormatIncludesTimestampAndLevel()
        {
            // Arrange
            var logger = _provider.CreateLogger("Svc");

            // Act
            logger.LogError("boom");

            // Assert
            _listener.LastMessage.Should().Contain("[Error]");
            _listener.LastMessage.Should().Contain("Z >");
            _listener.LastMessage.Should().Contain("boom");
        }

        /// <summary>
        /// Custom TraceListener to capture output in tests.
        /// </summary>
        private sealed class TestTraceListener : TraceListener
        {
            public string? LastMessage { get; private set; }

            public override void Write(string? message) => LastMessage = message;
            public override void WriteLine(string? message) => LastMessage = message;
        }
    }
}
