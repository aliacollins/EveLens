// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Avalonia.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for the process-wide exception backstop. Without it, an exception escaping a
    /// background thread, unobserved Task, or synchronous UI lifecycle override terminates the
    /// process silently — the root amplifier of the "crashes a lot, no logs" reports.
    /// </summary>
    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public void Install_IsIdempotent_DoesNotThrowOnRepeatedCalls()
        {
            Action act = () =>
            {
                GlobalExceptionHandler.Install();
                GlobalExceptionHandler.Install();
                GlobalExceptionHandler.Install();
            };

            act.Should().NotThrow("Install must be safe to call multiple times across startup paths");
        }

        [Fact]
        public void HandleDispatcherException_WithException_DoesNotThrow()
        {
            // Mirrors the dispatcher hook in App.OnFrameworkInitializationCompleted: logging the
            // exception must itself never throw, so the UI thread can survive and continue.
            Action act = () => GlobalExceptionHandler.HandleDispatcherException(
                new InvalidOperationException("simulated UI dispatcher failure"));

            act.Should().NotThrow();
        }

        [Fact]
        public void HandleDispatcherException_WithNull_DoesNotThrow()
        {
            Action act = () => GlobalExceptionHandler.HandleDispatcherException(null);

            act.Should().NotThrow("a null exception payload must be handled defensively");
        }
    }
}
