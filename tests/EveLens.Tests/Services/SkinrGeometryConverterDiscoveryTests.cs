// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using EveLens.Core.Interfaces;
using EveLens.Common.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Regression tests for the beta.2 converter-discovery hole: the search list
    /// contained only dev-machine locations (repo walk-up, PATH Node), so every
    /// installed build reported "converter script missing" for every hull. The
    /// installed render runtime (1.0.3+) carries the converter and its own Node;
    /// discovery must find both there.
    /// </summary>
    [Collection("AppServices")]
    public sealed class SkinrGeometryConverterDiscoveryTests : IDisposable
    {
        private readonly string _dataDir = Path.Combine(Path.GetTempPath(),
            "evelens-conv-" + Guid.NewGuid().ToString("N"));

        public SkinrGeometryConverterDiscoveryTests()
        {
            var paths = Substitute.For<IApplicationPaths>();
            paths.DataDirectory.Returns(_dataDir);
            AppServices.SetApplicationPaths(paths);
        }

        public void Dispose()
        {
            AppServices.Reset();
            try { Directory.Delete(_dataDir, recursive: true); }
            catch (Exception) { /* temp dir; the OS reaps it eventually */ }
        }

        private string InstallFakeRuntime(params string[] relativeFiles)
        {
            string root = Path.Combine(_dataDir, "skinr-runtime", "9.9.9");
            foreach (string rel in relativeFiles)
            {
                string full = Path.Combine(root, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllText(full, "x");
            }
            // InstalledRoot demands a manifest and a current pointer, same as production.
            File.WriteAllText(Path.Combine(root, "manifest.json"), "{}");
            File.WriteAllText(
                Path.Combine(_dataDir, "skinr-runtime", "current.txt"), "9.9.9");
            return root;
        }

        [Fact]
        public void FindScript_UsesTheInstalledRuntime()
        {
            string root = InstallFakeRuntime(
                Path.Combine("gr2-convert", "convert.mjs"));

            SkinrGeometryConverter.FindScript().Should().Be(
                Path.Combine(root, "gr2-convert", "convert.mjs"),
                "an installed build has no repository to walk up to — " +
                "the runtime package is where the converter actually lives");
        }

        [Fact]
        public void DiscoverNode_PrefersTheRuntimesPinnedNode()
        {
            string exe = OperatingSystem.IsWindows() ? "node.exe" : "node";
            string root = InstallFakeRuntime(Path.Combine("node", exe));

            SkinrGeometryConverter.DiscoverNode().Should().Be(
                Path.Combine(root, "node", exe),
                "users do not have Node installed, and a stray PATH Node must " +
                "never shadow the one the converter was verified against");
        }

        [Fact]
        public void DiscoverNode_FindsTheUnixBinLayout()
        {
            if (OperatingSystem.IsWindows())
            {
                // The bin/ fallback is the macOS shape; on Windows prove only that a
                // bin-only tree is still found rather than skipped.
                string root = InstallFakeRuntime(
                    Path.Combine("node", "bin", "node.exe"));
                SkinrGeometryConverter.DiscoverNode().Should().Be(
                    Path.Combine(root, "node", "bin", "node.exe"));
            }
        }

        [Fact]
        public void FindScript_WithNoRuntime_StillFindsTheRepositoryCheckout()
        {
            // No fake runtime installed: from the test tree the walk-up must keep
            // working, or every developer machine regresses instead.
            string? found = SkinrGeometryConverter.FindScript();
            found.Should().NotBeNull();
            found.Should().EndWith(Path.Combine("gr2-convert", "convert.mjs"));
        }
    }
}
