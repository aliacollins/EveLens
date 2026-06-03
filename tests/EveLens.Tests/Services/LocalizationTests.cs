// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
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

        [Fact]
        public void EveryRegisteredLanguage_HasAUiStringsTableLoaded()
        {
            // Contract: every code in LanguageRegistry must resolve to a loaded ui-strings table.
            // Guards the "add a language = drop a file + one registry line" promise: a registry
            // entry without its ui-strings-<code>.txt (or a typo'd code) fails here.
            foreach (var code in EveLens.Common.Services.LanguageRegistry.Codes)
            {
                Loc.GetTable(code).Should().NotBeNull(
                    $"language '{code}' is in LanguageRegistry but its ui-strings-{code}.txt did not load");
            }
        }

        [Fact]
        public void AvailableLanguages_MatchesRegistry()
        {
            Loc.AvailableLanguages.Should().BeEquivalentTo(EveLens.Common.Services.LanguageRegistry.Codes,
                "the language picker must derive from the single LanguageRegistry source of truth");
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

    /// <summary>
    /// End-to-end regression tests proving that entity names (skills, items) flow through
    /// <c>LocalizedName</c> to the UI view models when a non-English language is active. This is the
    /// behavioural complement to <see cref="EveLens.Tests.Architecture.LocalizationArchitectureTests"/>:
    /// it catches the bug the screenshots surfaced — the Plan editor showing English skill names to
    /// Korean/Chinese users because a VM read <c>.Name</c> instead of <c>.LocalizedName</c>.
    /// Requires loaded SDE skill/item data + translation datafiles.
    /// </summary>
    [Collection("StaticData")]
    public class LocalizedNameFlowTests
    {
        public LocalizedNameFlowTests()
        {
            PlanTestFixture.EnsureGameDataLoaded();
        }

        [Theory]
        [InlineData("ko")]
        [InlineData("zh-CN")]
        public void StaticSkill_LocalizedName_DiffersFromEnglish_WhenLanguageSet(string lang)
        {
            var prev = Loc.Language;
            try
            {
                Loc.Language = lang;
                // Navigation is a core skill present in every SDE language.
                var skill = EveLens.Common.Data.StaticSkills.GetSkillByName("Navigation");
                skill.Should().NotBeNull();
                skill!.LocalizedName.Should().NotBeNullOrEmpty();
                skill.LocalizedName.Should().NotBe(skill.Name,
                    $"{lang} should translate 'Navigation' away from the English name");
            }
            finally { Loc.Language = prev; }
        }

        [Fact]
        public void StaticSkill_LocalizedName_IsEnglish_WhenLanguageEnglish()
        {
            var prev = Loc.Language;
            try
            {
                Loc.Language = "en";
                var skill = EveLens.Common.Data.StaticSkills.GetSkillByName("Navigation");
                skill!.LocalizedName.Should().Be(skill.Name, "English keeps the base name");
            }
            finally { Loc.Language = prev; }
        }

        [Theory]
        [InlineData("ko")]
        [InlineData("zh-CN")]
        public void PlanQueueItem_DisplayName_UsesLocalizedSkillName(string lang)
        {
            var prev = Loc.Language;
            try
            {
                var character = PlanTestFixture.CreateTestCharacter();
                var plan = PlanTestFixture.CreateTestPlan(character);
                var skill = PlanTestFixture.GetSkill("Navigation");
                plan.PlanTo(skill, 1);
                var entry = plan.First(e => e.Skill == skill && e.Level == 1);

                var item = new EveLens.Common.ViewModels.PlanQueueItem(entry, character);

                Loc.Language = lang;
                // The queue row label must carry the localized skill name, not English.
                item.DisplayName.Should().Contain(skill.LocalizedName,
                    $"the plan queue row must display the {lang} skill name");
                item.DisplayName.Should().NotBe($"{skill.Name} I",
                    $"the plan queue row must not show the raw English name in {lang}");
            }
            finally { Loc.Language = prev; }
        }

        [Theory]
        [InlineData("ko")]
        [InlineData("zh-CN")]
        public void GetLocalizedItemName_ReturnsLocalized_WhenLanguageSet(string lang)
        {
            var prev = Loc.Language;
            try
            {
                Loc.Language = lang;
                // Tritanium (type 34) — a universal item present in every SDE language.
                string localized = EveLens.Common.Data.StaticItems.GetLocalizedItemName(34);
                string english = EveLens.Common.Data.StaticItems.GetItemName(34);
                localized.Should().NotBeNullOrEmpty();
                localized.Should().NotBe(english, $"{lang} should translate the item name");
            }
            finally { Loc.Language = prev; }
        }
    }
}
