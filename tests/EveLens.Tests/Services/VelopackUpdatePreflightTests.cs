// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for the App Translocation pre-flight in
    /// <see cref="VelopackUpdateService.ApplyAndRestart"/>.
    /// </summary>
    /// <remarks>
    /// The pre-flight exists because a translocated install fails its apply inside the
    /// spawned updater AFTER this process has exited — the C# side logs "executed
    /// successfully" and dies, so no catch block, dialog, or trace line can ever report
    /// the failure at apply time. Refusing up front is the only honest behavior.
    /// </remarks>
    [Collection("AppServices")]
    public class VelopackUpdatePreflightTests
    {
        public VelopackUpdatePreflightTests() => AppServices.Reset();

        private static IMacInstallService Translocated(bool value)
        {
            var svc = Substitute.For<IMacInstallService>();
            svc.IsTranslocated.Returns(value);
            return svc;
        }

        [Fact]
        public void ApplyRefusesWhenTranslocated()
        {
            AppServices.SetMacInstall(Translocated(true));
            var service = new VelopackUpdateService();

            service.ApplyAndRestart().Should().BeFalse();
        }

        [Fact]
        public void TranslocatedRefusalExplainsItselfInLastError()
        {
            AppServices.SetMacInstall(Translocated(true));
            var service = new VelopackUpdateService();

            service.ApplyAndRestart();

            // This text is what the failure dialog shows — it must name the condition
            // and the way out, because the user cannot discover either on their own.
            service.LastError.Should().Contain("Translocation").And.Contain("Applications");
        }

        [Fact]
        public void HealthyInstallFallsThroughToTheNormalPendingCheck()
        {
            AppServices.SetMacInstall(Translocated(false));
            var service = new VelopackUpdateService();

            service.ApplyAndRestart().Should().BeFalse();

            // Refused for the ordinary reason (nothing downloaded), not translocation.
            service.LastError.Should().NotContain("Translocation");
        }
    }
}
