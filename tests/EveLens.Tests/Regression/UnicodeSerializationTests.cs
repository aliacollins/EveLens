// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Text.Json;
using EveLens.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for unicode character preservation in JSON serialization.
    /// Reported by macOS user: ship name "♪ ♥ ♪" was displayed as "\u266a \u2665 \u266a"
    /// because System.Text.Json's default encoder escapes non-ASCII characters.
    /// </summary>
    public class UnicodeSerializationTests
    {
        [Theory]
        [InlineData("♪ ♥ ♪")]
        [InlineData("Ragnarok ★")]
        [InlineData("日本語テスト")]
        [InlineData("Ñoño's Rifter")]
        public void DirectJsonOptions_PreservesUnicodeCharacters(string unicodeName)
        {
            // All test strings are BMP characters (U+0000..U+FFFF).
            // Astral plane characters (emoji) still get surrogate-pair escaped
            // by UnsafeRelaxedJsonEscaping — that's expected and harmless since
            // the deserializer reconstructs them correctly.
            var testObj = new { shipName = unicodeName };

            string json = JsonSerializer.Serialize(testObj, SettingsFileManager.DirectJsonOptions);

            json.Should().Contain(unicodeName,
                "BMP unicode characters must be written as-is, not as \\uNNNN escape sequences");
        }

        [Theory]
        [InlineData("♪ ♥ ♪")]
        [InlineData("Ragnarok ★")]
        public void DirectJsonOptions_RoundTripsUnicodeCorrectly(string unicodeName)
        {
            var original = new ShipNameDto { ShipName = unicodeName };

            string json = JsonSerializer.Serialize(original, SettingsFileManager.DirectJsonOptions);
            var deserialized = JsonSerializer.Deserialize<ShipNameDto>(json, SettingsFileManager.DirectJsonOptions);

            deserialized.Should().NotBeNull();
            deserialized!.ShipName.Should().Be(unicodeName);
        }

        [Fact]
        public void DirectJsonOptions_BackwardCompatible_WithEscapedUnicode()
        {
            // Old files may contain escaped unicode — deserializer must still handle them
            string legacyJson = """{"ShipName":"\u266a \u2665 \u266a"}""";

            var result = JsonSerializer.Deserialize<ShipNameDto>(legacyJson, SettingsFileManager.DirectJsonOptions);

            result.Should().NotBeNull();
            result!.ShipName.Should().Be("♪ ♥ ♪",
                "deserializer must handle both escaped and literal unicode");
        }

        private class ShipNameDto
        {
            public string? ShipName { get; set; }
        }
    }
}
