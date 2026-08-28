// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EveLens.Common.Interfaces;
using EveLens.Common.Services;
using EveLens.Common.Serialization.Esi;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Regression for the Mac busy-loop: with the platform flagged Supported but
    /// no runtime installed, the prerenderer resolved every market design and
    /// attempted a sidecar per one — hundreds of doomed attempts that made the
    /// window unusable. Without a local engine it must park designs quietly and
    /// never touch the resolver or the sidecar.
    /// </summary>
    public sealed class SkinrThumbnailPrerendererTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(),
            "evelens-thumbs-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); }
            catch (Exception) { /* temp dir */ }
        }

        [Fact]
        public async Task WithoutALocalEngine_ParksDesignsWithoutResolvingOrRendering()
        {
            var resolver = Substitute.For<ISkinrRecipeResolver>();
            var recipes = new List<EsiSkinrRecipe>
            {
                new() { Id = "design-a" },
                new() { Id = "design-b" },
            };
            using var sut = new SkinrThumbnailPrerenderer(
                new SkinrThumbnailCache(_dir),
                () => recipes,
                communityPreviews: () => false,
                resolver: resolver,
                canRenderLocally: () => false);

            sut.Start();
            // The loop marks each design once (100ms cadence); give it room.
            for (int i = 0; i < 100 && sut.FailedCount < 2; i++)
                await Task.Delay(50);

            sut.FailedCount.Should().Be(2,
                "both designs must be parked exactly once, not retried forever");
            sut.Rendered.Should().Be(0);
            resolver.DidNotReceiveWithAnyArgs().Resolve(default!);
        }
    }
}
