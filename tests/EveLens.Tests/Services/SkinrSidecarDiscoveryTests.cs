// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// How the SKINR sidecar finds a render runtime, and — the part that actually broke — what it
    /// says when it cannot.
    /// </summary>
    /// <remarks>
    /// <para><b>The defect these pin down.</b> Discovery tries three roots in order and each attempt
    /// overwrites the paths from the last, so a total failure left the options holding the
    /// <em>last</em> layout tried: a folder beside the executable that only exists in an installed
    /// build. <c>Validate()</c> then dutifully reported four missing paths under
    /// <c>bin/Debug/net10.0/skinr/</c> and never mentioned <c>EVELENS_TRINITY_ROOT</c> at all. Every
    /// word of it was true and the whole message pointed the wrong way — it reads as "create these
    /// four folders" when the fix is to name one root.</para>
    ///
    /// <para>This is the project's recurring failure signature: not a crash, but a reader that fails
    /// and reports something plausible about the wrong subject. The fix is structural rather than
    /// editorial — discovery now keeps the search, not just its last footprint — so these tests
    /// assert on the search being present and on the absence of the misleading per-path wall.</para>
    ///
    /// <para><b>On environment variables in tests.</b> They are process-global, so every test here
    /// sets both variables explicitly and restores them, and they live in one class so xUnit runs
    /// them serially. A test that only set the variable it cared about would pass or fail depending
    /// on the developer's own machine — which is precisely the condition that hid this bug.</para>
    /// </remarks>
    public sealed class SkinrSidecarDiscoveryTests
    {
        private const string RuntimeVar = SkinrSidecarOptions.RuntimeRootVariable;
        private const string TrinityVar = SkinrSidecarOptions.TrinityRootVariable;

        /// <summary>
        /// Runs discovery against the two given roots and nothing else.
        /// </summary>
        /// <remarks>
        /// The lookup is injected rather than staged in the process environment. Discovery also
        /// consults the persisted Windows scopes — see
        /// <see cref="SkinrRuntimeDiscoveryScopeTests"/> for why — and a process variable cannot
        /// mask those, so setting one to null here used to hand these tests the developer's own
        /// registry value and make every assertion depend on the machine running it.
        /// </remarks>
        private static SkinrSidecarOptions DiscoverWith(string? runtimeRoot, string? trinityRoot) =>
            SkinrSidecarOptions.Discover(
                Path.Combine(Path.GetTempPath(), "evelens-test-rescache"),
                Path.Combine(Path.GetTempPath(), "evelens-test-geometry"),
                indexFiles: null,
                resolve: name => name == RuntimeVar
                    ? (runtimeRoot, "process")
                    : name == TrinityVar ? (trinityRoot, "process") : (null, "process"));

        /// <summary>
        /// Builds the layout an installed EveLens lays down: an interpreter and a renderer script,
        /// which are the two files discovery treats as proof a root is real.
        /// </summary>
        private static string CreateShippedRuntime(bool withScript = true)
        {
            string root = Path.Combine(
                Path.GetTempPath(), "evelens-skinr-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "python"));
            Directory.CreateDirectory(Path.Combine(root, "renderer"));
            Directory.CreateDirectory(Path.Combine(root, "engine"));
            Directory.CreateDirectory(Path.Combine(root, "bin"));
            File.WriteAllText(Path.Combine(root, "python", "python.exe"), string.Empty);
            if (withScript)
                File.WriteAllText(Path.Combine(root, "renderer", "skinr_sidecar.py"), string.Empty);
            return root;
        }

        private static string NonexistentPath() =>
            Path.Combine(Path.GetTempPath(), "evelens-absent-" + Guid.NewGuid().ToString("N"));

        // --- the search is recorded ------------------------------------------

        /// <summary>
        /// With neither variable set, all three attempts are still reported. "Not set" is a finding:
        /// it is the difference between the variable being wrong and the variable being unknown to
        /// whoever is reading the message.
        /// </summary>
        [Fact]
        public void An_unset_variable_is_recorded_as_a_step_not_skipped()
        {
            SkinrSidecarOptions options = DiscoverWith(null, null);

            options.DiscoverySteps.Should().HaveCount(3);
            options.DiscoverySteps[0].Should().Be($"{RuntimeVar} not set");
            options.DiscoverySteps[1].Should().Be($"{TrinityVar} not set");
            options.DiscoverySteps[2].Should().StartWith("beside the executable (");
        }

        [Fact]
        public void The_roots_are_tried_in_order_runtime_then_trinity_then_beside_the_exe()
        {
            string badRuntime = NonexistentPath();
            string badTrinity = NonexistentPath();

            SkinrSidecarOptions options = DiscoverWith(badRuntime, badTrinity);

            options.DiscoverySteps.Should().HaveCount(3);
            // The scope is part of the line because it is the actionable half of the answer: a root
            // found in "user" scope that the process never inherited is a different fix from a root
            // that is simply wrong.
            options.DiscoverySteps[0].Should().StartWith($"{RuntimeVar}={badRuntime} (process) — ");
            options.DiscoverySteps[1].Should().StartWith($"{TrinityVar}={badTrinity} (process) — ");
            options.DiscoverySteps[2].Should().StartWith("beside the executable (");
        }

        /// <summary>
        /// A rejected root names the file that was absent. "Not found" without a path is a message
        /// that requires the reader to already know the layout.
        /// </summary>
        [Fact]
        public void A_rejected_root_says_which_file_was_missing()
        {
            string root = NonexistentPath();

            SkinrSidecarOptions options = DiscoverWith(root, null);

            options.DiscoverySteps[0].Should().Contain("no interpreter");
            options.DiscoverySteps[0].Should().Contain(Path.Combine(root, "python", "python.exe"));
        }

        /// <summary>
        /// Both are named when both are missing. Reporting only the first sends someone to fix one
        /// path, rebuild, and come back for the second.
        /// </summary>
        [Fact]
        public void Both_mandatory_files_are_named_when_both_are_absent()
        {
            string root = NonexistentPath();

            SkinrSidecarOptions options = DiscoverWith(root, null);

            options.DiscoverySteps[0].Should().Contain("no interpreter");
            options.DiscoverySteps[0].Should().Contain("no renderer script");
        }

        [Fact]
        public void A_runtime_missing_only_its_script_says_so_and_does_not_blame_the_interpreter()
        {
            string root = CreateShippedRuntime(withScript: false);
            try
            {
                SkinrSidecarOptions options = DiscoverWith(root, null);

                options.DiscoverySteps[0].Should().Contain("no renderer script");
                options.DiscoverySteps[0].Should().NotContain("no interpreter");
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        // --- a root that works stops the search ------------------------------

        [Fact]
        public void A_complete_runtime_root_wins_and_the_search_stops_there()
        {
            string root = CreateShippedRuntime();
            try
            {
                SkinrSidecarOptions options = DiscoverWith(root, NonexistentPath());

                options.DiscoverySteps.Should().HaveCount(1, "a win ends the search");
                options.DiscoverySteps[0].Should().Be($"{RuntimeVar}={root} (process) — found");
                options.PythonPath.Should().Be(Path.Combine(root, "python", "python.exe"));
                options.ScriptPath.Should()
                    .Be(Path.Combine(root, "renderer", "skinr_sidecar.py"));
                options.ArtDirectory.Should().Be(Path.Combine(root, "engine"));
                options.BinDirectory.Should().Be(Path.Combine(root, "bin"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        // --- what Validate() says --------------------------------------------

        /// <summary>
        /// The message the user actually saw. One problem, naming the search and both variables —
        /// not four missing paths under a folder that only exists in an installed build.
        /// </summary>
        [Fact]
        public void Finding_no_runtime_at_all_is_reported_as_one_problem_naming_the_search()
        {
            SkinrSidecarOptions options = DiscoverWith(null, null);

            string[] runtime = options.Validate()
                .Where(p => p.StartsWith("no render runtime found", StringComparison.Ordinal))
                .ToArray();

            runtime.Should().HaveCount(1, "one problem, not four");
            runtime[0].Should().Contain("Searched:");
            runtime[0].Should().Contain(RuntimeVar);
            runtime[0].Should().Contain(TrinityVar);
            runtime[0].Should().Contain("not set", "the search itself is the actionable part");
        }

        [Fact]
        public void The_four_per_path_messages_are_gone_when_nothing_was_found()
        {
            SkinrSidecarOptions options = DiscoverWith(null, null);

            IReadOnlyList<string> problems = options.Validate();

            problems.Should().NotContain(p => p.StartsWith("render interpreter not found"));
            problems.Should().NotContain(p => p.StartsWith("render script not found"));
            problems.Should().NotContain(p => p.StartsWith("engine art directory not found"));
            problems.Should().NotContain(p => p.StartsWith("engine runtime directory not found"));
        }

        /// <summary>
        /// A half-present runtime is a different problem and keeps the per-piece messages. A broken
        /// install needs to know <em>which</em> piece is broken; the collapsed message would send
        /// someone to reconfigure a root that is already correct.
        /// </summary>
        [Fact]
        public void A_partially_present_runtime_still_gets_the_per_piece_messages()
        {
            string root = CreateShippedRuntime();
            try
            {
                Directory.Delete(Path.Combine(root, "engine"));

                SkinrSidecarOptions options = DiscoverWith(root, null);
                IReadOnlyList<string> problems = options.Validate();

                problems.Should().Contain(p => p.StartsWith("engine art directory not found"));
                problems.Should().NotContain(p => p.StartsWith("no render runtime found"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        /// <summary>
        /// Options assembled by hand — every existing test, and any caller that knows its own paths —
        /// have no search to report, so they keep the granular messages.
        /// </summary>
        [Fact]
        public void Hand_built_options_have_no_search_and_keep_the_per_piece_messages()
        {
            var options = new SkinrSidecarOptions();

            options.DiscoverySteps.Should().BeEmpty();
            IReadOnlyList<string> problems = options.Validate();

            problems.Should().Contain(p => p.StartsWith("render interpreter not found"));
            problems.Should().Contain(p => p.StartsWith("render script not found"));
            problems.Should().NotContain(p => p.StartsWith("no render runtime found"));
        }

        /// <summary>
        /// Discovery never throws and never returns null, whatever the environment says. The UI needs
        /// a sentence to display; an exception escaping a window's constructor is not one.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not a path at all")]
        [InlineData(@"Z:\definitely\not\here")]
        public void Discovery_survives_any_value_of_the_variables(string value)
        {
            SkinrSidecarOptions options = DiscoverWith(value, value);

            options.Should().NotBeNull();
            options.DiscoverySteps.Should().NotBeEmpty();
            options.Validate().Should().NotBeEmpty("nothing is installed in a test run");
        }
    }
}
