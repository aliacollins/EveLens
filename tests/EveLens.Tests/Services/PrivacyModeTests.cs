// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations;
using EveLens.Common.Events;
using EveLens.Common.Helpers;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    [Collection("AppServices")]
    public class PrivacyModeTests
    {
        public PrivacyModeTests()
        {
            AppServices.Reset();
        }

        [Fact]
        public void PrivacyMode_DefaultsToFalse()
        {
            AppServices.PrivacyModeEnabled.Should().BeFalse();
            AppServices.PrivacyMask.Should().Be(PrivacyCategories.None);
        }

        [Fact]
        public void TogglePrivacyMode_SetsAllCategories()
        {
            AppServices.TogglePrivacyMode();
            AppServices.PrivacyModeEnabled.Should().BeTrue();
            AppServices.PrivacyMask.Should().Be(PrivacyCategories.All);
        }

        [Fact]
        public void TogglePrivacyMode_ClearsWhenAnySet()
        {
            AppServices.TogglePrivacyCategory(PrivacyCategories.Balance);
            AppServices.PrivacyModeEnabled.Should().BeTrue();

            AppServices.TogglePrivacyMode();
            AppServices.PrivacyModeEnabled.Should().BeFalse();
            AppServices.PrivacyMask.Should().Be(PrivacyCategories.None);
        }

        [Fact]
        public void TogglePrivacyMode_SetsAllWhenNoneSet()
        {
            AppServices.TogglePrivacyMode();
            AppServices.PrivacyMask.Should().Be(PrivacyCategories.All);

            AppServices.TogglePrivacyMode();
            AppServices.PrivacyMask.Should().Be(PrivacyCategories.None);
        }

        [Fact]
        public void TogglePrivacyMode_PublishesEvent()
        {
            bool received = false;
            bool eventValue = false;

            AppServices.EventAggregator.Subscribe<PrivacyModeChangedEvent>(e =>
            {
                received = true;
                eventValue = e.IsEnabled;
            });

            AppServices.TogglePrivacyMode();

            received.Should().BeTrue();
            eventValue.Should().BeTrue();
        }

        [Fact]
        public void Reset_ClearsPrivacyMode()
        {
            AppServices.TogglePrivacyMode();
            AppServices.PrivacyModeEnabled.Should().BeTrue();

            AppServices.Reset();
            AppServices.PrivacyModeEnabled.Should().BeFalse();
            AppServices.PrivacyMask.Should().Be(PrivacyCategories.None);
        }

        [Fact]
        public void IsPrivate_ReturnsTrueForSetCategory()
        {
            AppServices.TogglePrivacyCategory(PrivacyCategories.Balance);

            AppServices.IsPrivate(PrivacyCategories.Balance).Should().BeTrue();
            AppServices.IsPrivate(PrivacyCategories.Name).Should().BeFalse();
            AppServices.IsPrivate(PrivacyCategories.SkillPoints).Should().BeFalse();
        }

        [Fact]
        public void TogglePrivacyCategory_XorsFlag()
        {
            AppServices.TogglePrivacyCategory(PrivacyCategories.Name);
            AppServices.IsPrivate(PrivacyCategories.Name).Should().BeTrue();

            AppServices.TogglePrivacyCategory(PrivacyCategories.Name);
            AppServices.IsPrivate(PrivacyCategories.Name).Should().BeFalse();
        }

        [Fact]
        public void TogglePrivacyCategory_PublishesEvent()
        {
            bool received = false;
            AppServices.EventAggregator.Subscribe<PrivacyModeChangedEvent>(e => received = true);

            AppServices.TogglePrivacyCategory(PrivacyCategories.Training);

            received.Should().BeTrue();
        }

        [Fact]
        public void TogglePrivacyCategory_MultipleIndependent()
        {
            AppServices.TogglePrivacyCategory(PrivacyCategories.Balance);
            AppServices.TogglePrivacyCategory(PrivacyCategories.SkillPoints);

            AppServices.IsPrivate(PrivacyCategories.Balance).Should().BeTrue();
            AppServices.IsPrivate(PrivacyCategories.SkillPoints).Should().BeTrue();
            AppServices.IsPrivate(PrivacyCategories.Name).Should().BeFalse();
            AppServices.IsPrivate(PrivacyCategories.Training).Should().BeFalse();
            AppServices.PrivacyModeEnabled.Should().BeTrue();
        }

        [Fact]
        public void PrivacyModeEnabled_TrueWhenAnyCategorySet()
        {
            AppServices.TogglePrivacyCategory(PrivacyCategories.Remaps);
            AppServices.PrivacyModeEnabled.Should().BeTrue();

            AppServices.TogglePrivacyCategory(PrivacyCategories.Remaps);
            AppServices.PrivacyModeEnabled.Should().BeFalse();
        }

        [Fact]
        public void PrivacyCategories_AllIncludesEveryFlag()
        {
            PrivacyCategories.All.Should().HaveFlag(PrivacyCategories.Name);
            PrivacyCategories.All.Should().HaveFlag(PrivacyCategories.CorpAlliance);
            PrivacyCategories.All.Should().HaveFlag(PrivacyCategories.Balance);
            PrivacyCategories.All.Should().HaveFlag(PrivacyCategories.SkillPoints);
            PrivacyCategories.All.Should().HaveFlag(PrivacyCategories.Training);
            PrivacyCategories.All.Should().HaveFlag(PrivacyCategories.Remaps);
        }

        // PrivacyHelper per-category property tests

        [Fact]
        public void PrivacyHelper_IsNameHidden_ReflectsCategory()
        {
            PrivacyHelper.IsNameHidden.Should().BeFalse();
            AppServices.TogglePrivacyCategory(PrivacyCategories.Name);
            PrivacyHelper.IsNameHidden.Should().BeTrue();
            PrivacyHelper.IsBalanceHidden.Should().BeFalse();
        }

        [Fact]
        public void PrivacyHelper_IsBalanceHidden_ReflectsCategory()
        {
            AppServices.TogglePrivacyCategory(PrivacyCategories.Balance);
            PrivacyHelper.IsBalanceHidden.Should().BeTrue();
            PrivacyHelper.IsNameHidden.Should().BeFalse();
        }

        [Fact]
        public void PrivacyHelper_IsActive_TrueWhenAnyCategory()
        {
            PrivacyHelper.IsActive.Should().BeFalse();
            AppServices.TogglePrivacyCategory(PrivacyCategories.Training);
            PrivacyHelper.IsActive.Should().BeTrue();
        }

        // Existing PrivacyHelper method tests (backward compatibility)

        [Fact]
        public void PrivacyHelper_MaskIsk_WhenEnabled_ReturnsMasked()
        {
            AppServices.TogglePrivacyMode();

            var result = PrivacyHelper.MaskIsk(1_234_567.89m);
            result.Should().Be($"{PrivacyHelper.Mask} ISK");
        }

        [Fact]
        public void PrivacyHelper_MaskIsk_WhenDisabled_ReturnsFormatted()
        {
            var result = PrivacyHelper.MaskIsk(1_234_567.89m);
            result.Should().Be("1,234,567.89 ISK");
        }

        [Fact]
        public void PrivacyHelper_MaskText_WhenEnabled_ReturnsMask()
        {
            AppServices.TogglePrivacyMode();

            PrivacyHelper.MaskText("secret").Should().Be(PrivacyHelper.Mask);
        }

        [Fact]
        public void PrivacyHelper_MaskText_WhenDisabled_ReturnsOriginal()
        {
            PrivacyHelper.MaskText("visible").Should().Be("visible");
        }

        [Fact]
        public void PrivacyHelper_MaskNumber_WhenEnabled_ReturnsMaskWithSuffix()
        {
            AppServices.TogglePrivacyMode();

            PrivacyHelper.MaskNumber(12345, "SP").Should().Be($"{PrivacyHelper.Mask} SP");
            PrivacyHelper.MaskNumber(12345).Should().Be(PrivacyHelper.Mask);
        }

        [Fact]
        public void PrivacyHelper_MaskNumber_WhenDisabled_ReturnsFormatted()
        {
            PrivacyHelper.MaskNumber(12345, "SP").Should().Be("12,345 SP");
            PrivacyHelper.MaskNumber(12345).Should().Be("12,345");
        }

        [Fact]
        public void PrivacyHelper_MaskFormatted_WhenEnabled_ReturnsMask()
        {
            AppServices.TogglePrivacyMode();

            PrivacyHelper.MaskFormatted("27.1M SP").Should().Be(PrivacyHelper.Mask);
        }

        [Fact]
        public void PrivacyHelper_MaskFormatted_WhenDisabled_ReturnsOriginal()
        {
            PrivacyHelper.MaskFormatted("27.1M SP").Should().Be("27.1M SP");
        }

        [Fact]
        public void PrivacyHelper_IsActive_ReflectsAppServicesState()
        {
            PrivacyHelper.IsActive.Should().BeFalse();

            AppServices.TogglePrivacyMode();
            PrivacyHelper.IsActive.Should().BeTrue();

            AppServices.TogglePrivacyMode();
            PrivacyHelper.IsActive.Should().BeFalse();
        }
    }
}
