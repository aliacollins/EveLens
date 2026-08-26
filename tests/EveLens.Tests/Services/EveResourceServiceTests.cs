// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Pure parsing pieces of the CCP resource CDN chain. Line formats captured
    /// live from build 3471488 on 2026-08-19.
    /// </summary>
    public class EveResourceServiceTests
    {
        [Fact]
        public void ParseIndexLine_ParsesRealEntry()
        {
            var entry = EveResourceService.ParseIndexLine(
                "res:/dx9/model/ship/minmatar/frigate/mf1/mf1_t1.gr2," +
                "87/8786355361193828_242b9685f72a03ac947745d780d5c285," +
                "242b9685f72a03ac947745d780d5c285,335572,305024");

            entry.Should().NotBeNull();
            entry!.ResPath.Should().Be("res:/dx9/model/ship/minmatar/frigate/mf1/mf1_t1.gr2");
            entry.CdnPath.Should().Be("87/8786355361193828_242b9685f72a03ac947745d780d5c285");
            entry.Md5.Should().Be("242b9685f72a03ac947745d780d5c285");
        }

        [Fact]
        public void ParseIndexLine_NormalizesCaseForLookup()
        {
            var entry = EveResourceService.ParseIndexLine(
                "res:/UI/Texture/classes/Cosmetics/Ship/materials/swatch.png,ab/abcdef_123,123,10,5");
            entry!.ResPath.Should().Be(
                "res:/ui/texture/classes/cosmetics/ship/materials/swatch.png",
                "res: paths are case-insensitive in the game; the index key must be canonical");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("app:/resfileindex.txt,1d/xyz,4045,20639396,5266499")]
        [InlineData("res:/only-two-fields,xx")]
        public void ParseIndexLine_RejectsNonResourceLines(string line)
        {
            EveResourceService.ParseIndexLine(line).Should().BeNull();
        }

        [Fact]
        public void ExtractBuildNumber_ReadsClientJson()
        {
            EveResourceService.ExtractBuildNumber(
                    """{"build": "3471488", "buildNumber": "3471488", "protected": false}""")
                .Should().Be("3471488");
            EveResourceService.ExtractBuildNumber("{}").Should().BeNull();
            EveResourceService.ExtractBuildNumber(null).Should().BeNull();
        }
    }
}
