// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    public sealed class SkinrLeftoverCleanupTests : IDisposable
    {
        private readonly string _root;

        public SkinrLeftoverCleanupTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "evelens-skinr-cleanup-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private string Touch(params string[] parts)
        {
            string path = Path.Combine(_root, Path.Combine(parts));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "x");
            return path;
        }

        [Fact]
        public void Run_RemovesSkinrLeftovers_AndKeepsEverythingElse()
        {
            Touch("skinr-runtime", "1.0.4", "manifest.json");
            Touch("cache", "skinr", "thumbs", "a.png");
            Touch("cache", "resources", "abc.gr2");
            Touch("cache", "resources-index", "resfileindex.txt");
            string licenses = Touch("cache", "characters", "12345", "skinr_licenses.json");
            string components = Touch("cache", "characters", "12345", "skinr_components.json");

            string skills = Touch("cache", "characters", "12345", "skills.json");
            string image = Touch("cache", "images", "portrait.png");
            string settings = Touch("settings.json");

            int removed = new SkinrLeftoverCleanup(_root).Run();

            removed.Should().Be(6);
            Directory.Exists(Path.Combine(_root, "skinr-runtime")).Should().BeFalse();
            Directory.Exists(Path.Combine(_root, "cache", "skinr")).Should().BeFalse();
            Directory.Exists(Path.Combine(_root, "cache", "resources")).Should().BeFalse();
            Directory.Exists(Path.Combine(_root, "cache", "resources-index")).Should().BeFalse();
            File.Exists(licenses).Should().BeFalse();
            File.Exists(components).Should().BeFalse();

            File.Exists(skills).Should().BeTrue();
            File.Exists(image).Should().BeTrue();
            File.Exists(settings).Should().BeTrue();
        }

        [Fact]
        public void Run_IsANoOp_WhenNothingIsLeft()
        {
            Touch("settings.json");

            new SkinrLeftoverCleanup(_root).Run().Should().Be(0);
            new SkinrLeftoverCleanup(_root).Run().Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Run_ReturnsZero_ForMissingDataDirectory(string? dataDirectory)
        {
            new SkinrLeftoverCleanup(dataDirectory!).Run().Should().Be(0);
            new SkinrLeftoverCleanup(Path.Combine(_root, "does-not-exist")).Run().Should().Be(0);
        }
    }
}
