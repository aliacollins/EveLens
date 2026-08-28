// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Linq;
using EveLens.Common.Data;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Data
{
    /// <summary>
    /// The bundled-resources lookup contract that lets one build serve two layouts:
    /// exe-adjacent "Resources" (Windows/Linux) and the bundle-level "../Resources"
    /// of a macOS .app — where Contents/MacOS may hold only Mach-O code so the
    /// bundle can be code-signed and notarized (apple-platform-rs #192 forced this).
    /// </summary>
    public sealed class InstallResourceDirectoriesTests
    {
        [Fact]
        public void Candidates_ExeAdjacentFirst_ThenBundleResources()
        {
            var dirs = Datafile.InstallResourceDirectories().ToList();

            dirs.Should().HaveCount(2);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            dirs[0].Should().Be(Path.Combine(baseDir, "Resources"));
            dirs[1].Should().Be(Path.GetFullPath(Path.Combine(baseDir, "..", "Resources")));
        }

        [Fact]
        public void FindInstallResource_ReturnsNull_WhenAbsentEverywhere()
        {
            Datafile.FindInstallResource(
                "no-such-file-" + Guid.NewGuid().ToString("N") + ".bin")
                .Should().BeNull();
        }
    }
}
