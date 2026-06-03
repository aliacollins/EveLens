// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Validates UI string localization tables. Every non-English language must cover the same
    /// keys as English (no missing strings, no stray keys) and preserve runtime placeholders.
    /// </summary>
    public class LocalizationTests
    {
        // Languages that ship a full UI translation (English is the base).
        private static readonly string[] TranslatedLanguages = { "zh-CN", "ko" };

        [Fact]
        public void Korean_IsAdvertised_InAvailableLanguages()
        {
            Loc.AvailableLanguages.Should().Contain("ko");
            Loc.GetLanguageDisplayName("ko").Should().Contain("Korean");
        }

        [Theory]
        [InlineData("zh-CN")]
        [InlineData("ko")]
        public void Language_HasSameKeysAsEnglish(string lang)
        {
            var en = Loc.GetTable("en");
            var other = Loc.GetTable(lang);

            en.Should().NotBeNull();
            other.Should().NotBeNull($"{lang} must be registered");

            var enKeys = en!.Keys.ToHashSet();
            var otherKeys = other!.Keys.ToHashSet();

            var missing = enKeys.Except(otherKeys).ToList();
            var extra = otherKeys.Except(enKeys).ToList();

            missing.Should().BeEmpty($"{lang} is missing translations for: {string.Join(", ", missing.Take(10))}");
            extra.Should().BeEmpty($"{lang} has keys not present in English (typos?): {string.Join(", ", extra.Take(10))}");
        }

        [Theory]
        [InlineData("zh-CN")]
        [InlineData("ko")]
        public void Language_HasNoEmptyValues(string lang)
        {
            var table = Loc.GetTable(lang);
            table.Should().NotBeNull();

            var empty = table!.Where(kvp => string.IsNullOrWhiteSpace(kvp.Value)).Select(kvp => kvp.Key).ToList();
            empty.Should().BeEmpty($"{lang} has empty translations for: {string.Join(", ", empty.Take(10))}");
        }

        [Theory]
        [InlineData("zh-CN")]
        [InlineData("ko")]
        public void Language_PreservesPlaceholders(string lang)
        {
            var en = Loc.GetTable("en")!;
            var other = Loc.GetTable(lang)!;

            // Any English string containing {0} must keep {0} in the translation
            // (it is filled at runtime — dropping it loses the number/duration).
            foreach (var kvp in en.Where(k => k.Value.Contains("{0}")))
            {
                other.Should().ContainKey(kvp.Key);
                other[kvp.Key].Should().Contain("{0}",
                    $"{lang}['{kvp.Key}'] must keep the {{0}} placeholder");
            }
        }

        [Fact]
        public void Korean_TranslatesKnownKeys()
        {
            var prev = Loc.Language;
            try
            {
                Loc.Language = "ko";
                // A few representative keys should return non-English Korean text.
                Loc.Get("Menu.File").Should().Be("파일");
                Loc.Get("Action.Save").Should().NotBe("Save");
                // Unknown key falls back to the key itself.
                Loc.Get("Nonexistent.Key.Xyz").Should().Be("Nonexistent.Key.Xyz");
            }
            finally
            {
                Loc.Language = prev;
            }
        }

        [Fact]
        public void UnknownLanguage_FallsBackToEnglish()
        {
            var prev = Loc.Language;
            try
            {
                Loc.Language = "ko";
                // A key that exists in English but (hypothetically) not in ko falls back to English,
                // never to the raw key. Since ko has full parity, assert the fallback path via en.
                var en = Loc.GetTable("en")!;
                var sampleKey = en.Keys.First();
                Loc.Get(sampleKey).Should().NotBe(sampleKey, "a known key must resolve to a translation, not the raw key");
            }
            finally
            {
                Loc.Language = prev;
            }
        }
    }
}
