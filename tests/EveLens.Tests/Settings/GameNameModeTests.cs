// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Text.Json;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// Tests for the game-name language policy (Discussion #79): Korean players want English
    /// ship/item/skill names with a Korean UI; Chinese players want translated names. The policy
    /// lives in <see cref="LanguageRegistry"/> (per-language default) + the
    /// <see cref="UISettings.GameNameMode"/> user override.
    /// </summary>
    public class GameNameModeTests
    {
        [Fact]
        public void Default_IsAuto()
        {
            new UISettings().GameNameMode.Should().Be(GameNameMode.Auto,
                "existing settings files without the element must resolve to the community default");
        }

        [Theory]
        [InlineData(GameNameMode.Auto)]
        [InlineData(GameNameMode.Localized)]
        [InlineData(GameNameMode.English)]
        public void GameNameMode_RoundTrips_Json(GameNameMode mode)
        {
            var ui = new UISettings { GameNameMode = mode };
            string json = JsonSerializer.Serialize(ui, JsonDirectSerializationTests.JsonOptions);
            var result = JsonSerializer.Deserialize<UISettings>(json, JsonDirectSerializationTests.JsonOptions);
            result!.GameNameMode.Should().Be(mode);
        }

        [Fact]
        public void LanguageRegistry_Korean_DefaultsToEnglishGameNames()
        {
            LanguageRegistry.LocalizedGameNamesDefault("ko").Should().BeFalse(
                "Korean players navigate by English game names (Discussion #79)");
        }

        [Fact]
        public void LanguageRegistry_Chinese_DefaultsToLocalizedGameNames()
        {
            LanguageRegistry.LocalizedGameNamesDefault("zh-CN").Should().BeTrue(
                "the Chinese community expects translated game names");
        }

        [Fact]
        public void LanguageRegistry_English_NeverLocalizesGameNames()
        {
            LanguageRegistry.LocalizedGameNamesDefault("en").Should().BeFalse();
        }

        [Fact]
        public void LanguageRegistry_UnknownCode_DefaultsToEnglishGameNames()
        {
            LanguageRegistry.LocalizedGameNamesDefault("xx-YY").Should().BeFalse(
                "an unknown language code must fail safe to English names");
        }
    }
}
