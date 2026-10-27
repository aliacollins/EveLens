// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    /// <summary>
    /// Reorder is always available: entering it from an automatic sort switches the
    /// overview to Custom order instead of leaving the button disabled.
    /// </summary>
    public class OverviewReorderPolicyTests
    {
        [Theory]
        [InlineData(OverviewSortMode.Name)]
        [InlineData(OverviewSortMode.SkillPoints)]
        [InlineData(OverviewSortMode.TrainingUrgency)]
        public void EnteringReorder_FromAutomaticSort_SwitchesToCustom(OverviewSortMode mode)
        {
            OverviewReorderPolicy.ShouldSwitchToCustom(mode, enteringReorder: true).Should().BeTrue();
        }

        [Fact]
        public void EnteringReorder_FromCustom_DoesNotSwitch()
        {
            OverviewReorderPolicy.ShouldSwitchToCustom(OverviewSortMode.Custom, enteringReorder: true)
                .Should().BeFalse();
        }

        [Theory]
        [InlineData(OverviewSortMode.Custom)]
        [InlineData(OverviewSortMode.Name)]
        [InlineData(OverviewSortMode.SkillPoints)]
        [InlineData(OverviewSortMode.TrainingUrgency)]
        public void LeavingReorder_ViaDone_NeverTouchesSort(OverviewSortMode mode)
        {
            OverviewReorderPolicy.ShouldSwitchToCustom(mode, enteringReorder: false).Should().BeFalse();
        }

        [Theory]
        [InlineData(OverviewSortMode.Name)]
        [InlineData(OverviewSortMode.SkillPoints)]
        [InlineData(OverviewSortMode.TrainingUrgency)]
        public void PickingAutomaticSort_WhileReordering_EndsReorderMode(OverviewSortMode mode)
        {
            OverviewReorderPolicy.StaysInReorderMode(inReorderMode: true, mode).Should().BeFalse();
        }

        [Fact]
        public void PickingCustom_WhileReordering_StaysInReorderMode()
        {
            OverviewReorderPolicy.StaysInReorderMode(inReorderMode: true, OverviewSortMode.Custom)
                .Should().BeTrue();
        }

        [Theory]
        [InlineData(OverviewSortMode.Custom)]
        [InlineData(OverviewSortMode.Name)]
        public void SortChange_OutsideReorderMode_NeverEntersIt(OverviewSortMode mode)
        {
            OverviewReorderPolicy.StaysInReorderMode(inReorderMode: false, mode).Should().BeFalse();
        }
    }
}
