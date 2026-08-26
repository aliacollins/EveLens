// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Which environment scope the render-runtime search is allowed to believe.
    /// </summary>
    /// <remarks>
    /// <para><b>The bug these close.</b> The viewer reported
    /// "3D preview not available on this platform — EVELENS_TRINITY_ROOT not set" on a machine where
    /// that variable was permanently set. Both statements were true at once: a process inherits its
    /// environment frozen at launch, while <c>setx</c> and the System Properties dialog write the
    /// registry, which only <em>later</em> shells pick up. So the app was launched from a shell that
    /// predated the setting, found nothing in its own block, and reported the honest but useless
    /// conclusion that the variable was unset.</para>
    ///
    /// <para>"I set this permanently" means it applies now, not after a reboot — so discovery reads
    /// the persisted scopes too. Process scope still wins, because exporting a variable in one shell
    /// to override the machine-wide value for a single launch is the reason that ordering exists.</para>
    ///
    /// <para>Both stores are injected here. A test that wrote to <c>HKCU\Environment</c> to prove
    /// this would leave residue on the machine running it and would pass or fail based on that
    /// machine's configuration, which is the opposite of what a unit test is for.</para>
    /// </remarks>
    public sealed class SkinrRuntimeDiscoveryScopeTests
    {
        private const string Name = "EVELENS_TRINITY_ROOT";

        /// <summary>A persisted store holding exactly what it is given, and nothing implicit.</summary>
        private static Func<string, EnvironmentVariableTarget, string?> Store(
            params (EnvironmentVariableTarget Target, string Value)[] entries)
        {
            var map = new Dictionary<EnvironmentVariableTarget, string>();
            foreach ((EnvironmentVariableTarget target, string value) in entries)
                map[target] = value;
            return (_, target) => map.TryGetValue(target, out string? v) ? v : null;
        }

        private static Func<string, string?> Empty => _ => null;

        [Fact]
        public void TheUserScopeIsFoundWhenTheProcessNeverInheritedIt()
        {
            // The exact reported failure: set with setx, absent from the launching shell.
            (string? value, string scope) = SkinrSidecarOptions.ResolveVariable(
                Name, Empty, Store((EnvironmentVariableTarget.User, @"D:\trinity-inspect")));

            value.Should().Be(@"D:\trinity-inspect");
            scope.Should().Be("user",
                because: "the reported scope is the whole diagnostic value of the discovery log — "
                       + "saying 'process' for a registry read would send someone to fix the wrong "
                       + "thing");
        }

        [Fact]
        public void ProcessScopeWinsOverAPersistedValue()
        {
            (string? value, string scope) = SkinrSidecarOptions.ResolveVariable(
                Name, _ => @"D:\one-off-build",
                Store((EnvironmentVariableTarget.User, @"D:\trinity-inspect"),
                      (EnvironmentVariableTarget.Machine, @"C:\corporate")));

            value.Should().Be(@"D:\one-off-build");
            scope.Should().Be("process");
        }

        [Fact]
        public void UserScopeWinsOverMachineScope()
        {
            // A developer's own setting outranks one an administrator pushed machine-wide, matching
            // how Windows itself resolves the two.
            (string? value, string scope) = SkinrSidecarOptions.ResolveVariable(
                Name, Empty,
                Store((EnvironmentVariableTarget.User, @"D:\trinity-inspect"),
                      (EnvironmentVariableTarget.Machine, @"C:\corporate")));

            value.Should().Be(@"D:\trinity-inspect");
            scope.Should().Be("user");
        }

        [Fact]
        public void MachineScopeIsStillConsultedWhenTheUserHasNoSetting()
        {
            (string? value, string scope) = SkinrSidecarOptions.ResolveVariable(
                Name, Empty, Store((EnvironmentVariableTarget.Machine, @"C:\corporate")));

            value.Should().Be(@"C:\corporate");
            scope.Should().Be("machine");
        }

        [Fact]
        public void APlatformWithNoPersistedStoreReportsUnsetRatherThanProbing()
        {
            // Unix has no registry to consult — the shell profile IS the persistence mechanism, and
            // it is already in the process block. Passing null must read as "nothing more to try",
            // never as an error.
            (string? value, string scope) = SkinrSidecarOptions.ResolveVariable(Name, Empty, null);

            value.Should().BeNull();
            scope.Should().Be("process");
        }

        [Fact]
        public void AWhitespaceValueIsNotASetting()
        {
            // An empty-but-present variable is a common shape — `set VAR=` leaves one behind — and
            // treating it as a root sends discovery looking for an interpreter under "".
            (string? value, string scope) = SkinrSidecarOptions.ResolveVariable(
                Name, _ => "   ", Store((EnvironmentVariableTarget.User, @"D:\trinity-inspect")));

            value.Should().Be(@"D:\trinity-inspect");
            scope.Should().Be("user");
        }

        [Fact]
        public void NoScopeHavingAValueIsReportedAsUnsetAndNotAsAFailure()
        {
            (string? value, _) = SkinrSidecarOptions.ResolveVariable(Name, Empty, Store());

            value.Should().BeNull(
                because: "a missing runtime is a first-class answer the viewer turns into a "
                       + "sentence, not an exception");
        }
    }
}
