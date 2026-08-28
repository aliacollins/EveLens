// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for <see cref="MacInstallService"/>, the Gatekeeper App Translocation
    /// detector and repairer.
    /// </summary>
    /// <remarks>
    /// These exist because a translocated macOS app runs from a read-only mirror where
    /// every in-place update fails inside the spawned updater AFTER the app has exited
    /// — no error handling can ever observe it, so this detection is the only line of
    /// defense. The paths in these tests are real shapes captured from the live
    /// incident on 2026-08-28 (usernames replaced).
    /// </remarks>
    public class MacInstallServiceTests
    {
        private const string TranslocatedProcess =
            "/private/var/folders/85/xx25gfd3cmvf635lzr0000gn/T/AppTranslocation/" +
            "CA859D2C-2240-4DB6-96C8-5E3BE7B953E5/d/EveLens.app/Contents/MacOS/EveLens";

        private const string HealthyProcess =
            "/Applications/EveLens.app/Contents/MacOS/EveLens";

        [Fact]
        public void DetectsTranslocatedProcessPath()
        {
            new MacInstallService(TranslocatedProcess).IsTranslocated.Should().BeTrue();
        }

        [Theory]
        [InlineData(HealthyProcess)]
        [InlineData("/Users/someone/Downloads/EveLens.app/Contents/MacOS/EveLens")]
        [InlineData(@"C:\Program Files\EveLens\EveLens.Avalonia.exe")]
        [InlineData("/usr/lib/evelens/EveLens")]
        public void HealthyPathsAreNotTranslocated(string path)
        {
            new MacInstallService(path).IsTranslocated.Should().BeFalse();
        }

        [Fact]
        public void NullProcessPathIsNotTranslocated()
        {
            // Environment.ProcessPath can be null in exotic hosts; the service must
            // report healthy, not throw, or startup dies before the first window.
            new MacInstallService(null).IsTranslocated.Should().BeFalse();
        }

        [Fact]
        public void ResolvesTheRunningBundleRootFromTheProcessPath()
        {
            new MacInstallService(TranslocatedProcess).RunningBundlePath.Should().Be(
                "/private/var/folders/85/xx25gfd3cmvf635lzr0000gn/T/AppTranslocation/" +
                "CA859D2C-2240-4DB6-96C8-5E3BE7B953E5/d/EveLens.app");
        }

        [Fact]
        public void RealBundleOfAHealthyInstallIsTheRunningBundle()
        {
            new MacInstallService(HealthyProcess).RealBundlePath
                .Should().Be("/Applications/EveLens.app");
        }

        [Fact]
        public void RealBundleOfATranslocatedInstallPointsAtApplications()
        {
            // The translocated mount hides the original location; the repair target is
            // the canonical install dir, keyed by the bundle's own name.
            new MacInstallService(TranslocatedProcess).RealBundlePath
                .Should().EndWith("EveLens.app")
                .And.NotContain("AppTranslocation");
        }

        [Fact]
        public void NoBundleInPathMeansNoRealBundle()
        {
            // A bare binary outside any .app (dev runs, Linux) has nothing to repair.
            new MacInstallService("/usr/lib/evelens/EveLens").RealBundlePath.Should().BeNull();
        }

        [Fact]
        public void HealRefusesWhenNotTranslocated()
        {
            // Healing a healthy install would pointlessly relaunch the app.
            new MacInstallService(HealthyProcess).HealAndRelaunch().Should().BeFalse();
        }

        [Fact]
        public void ImplementsTheCoreInterface()
        {
            new MacInstallService().Should().BeAssignableTo<IMacInstallService>();
        }
    }
}
