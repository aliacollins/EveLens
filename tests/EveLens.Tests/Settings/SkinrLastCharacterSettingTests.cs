// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Text.Json;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// The SKINR studio's last-character memory (Law 13: settings changes round-trip).
    /// </summary>
    public class SkinrLastCharacterSettingTests
    {
        [Fact]
        public void Default_IsZero()
        {
            new UISettings().SkinrLastCharacterId.Should().Be(0,
                "0 means never opened — the studio shows the landing instead of guessing");
        }

        [Fact]
        public void SkinrLastCharacterId_RoundTrips_Json()
        {
            var ui = new UISettings { SkinrLastCharacterId = 90000001L };
            string json = JsonSerializer.Serialize(ui, JsonDirectSerializationTests.JsonOptions);
            var result = JsonSerializer.Deserialize<UISettings>(json, JsonDirectSerializationTests.JsonOptions);
            result!.SkinrLastCharacterId.Should().Be(90000001L);
        }
    }
}
