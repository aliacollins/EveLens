// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Helpers;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    /// <summary>
    /// The character card's right-click grouping menu -- the discoverable path to
    /// creating and changing groups without the drag gesture.
    /// </summary>
    public class CharacterGroupMenuModelTests
    {
        private static readonly Guid A = Guid.NewGuid();
        private static readonly Guid B = Guid.NewGuid();
        private static readonly Guid C = Guid.NewGuid();

        private static CharacterGroupSettings Group(string name, params Guid[] members)
        {
            var g = new CharacterGroupSettings { Name = name };
            foreach (var m in members) g.CharacterGuids.Add(m);
            return g;
        }

        [Fact]
        public void NoGroups_OffersNoGroupEntries_AndNoRemove()
        {
            var model = CharacterGroupMenuModel.Build(new List<CharacterGroupSettings>(), A);

            model.Groups.Should().BeEmpty();
            model.CurrentGroupName.Should().BeNull();
            model.CanRemoveFromGroup.Should().BeFalse();
        }

        [Fact]
        public void ListsEveryGroup_InDisplayOrder()
        {
            var groups = new List<CharacterGroupSettings>
            {
                Group("Zeta", B), Group("Alpha", C), Group("Mains", B, C)
            };

            var model = CharacterGroupMenuModel.Build(groups, A);

            model.Groups.Select(g => g.Name).Should().Equal("Zeta", "Alpha", "Mains");
        }

        [Fact]
        public void UngroupedCharacter_HasNoCurrentGroup_AndNoRemove()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", B), Group("Alts", C) };

            var model = CharacterGroupMenuModel.Build(groups, A);

            model.Groups.Should().OnlyContain(g => !g.IsCurrent);
            model.CurrentGroupName.Should().BeNull();
            model.CanRemoveFromGroup.Should().BeFalse();
        }

        [Fact]
        public void GroupedCharacter_MarksOnlyItsOwnGroupCurrent_AndOffersRemove()
        {
            var groups = new List<CharacterGroupSettings>
            {
                Group("Mains", B), Group("Alts", A, C), Group("Cyno", C)
            };

            var model = CharacterGroupMenuModel.Build(groups, A);

            model.Groups.Should().Equal(
                new CharacterGroupMenuEntry("Mains", false),
                new CharacterGroupMenuEntry("Alts", true),
                new CharacterGroupMenuEntry("Cyno", false));
            model.CurrentGroupName.Should().Be("Alts");
            model.CanRemoveFromGroup.Should().BeTrue();
        }

        [Fact]
        public void LegacyMultiGroupMembership_MarksEachHoldingGroup_FirstIsCurrent()
        {
            // Settings can list a character in two groups; neither entry may
            // be a live target (AddToGroup would no-op), and Remove clears both.
            var groups = new List<CharacterGroupSettings> { Group("One", A), Group("Two", B), Group("Three", A) };

            var model = CharacterGroupMenuModel.Build(groups, A);

            model.Groups.Where(g => g.IsCurrent).Select(g => g.Name).Should().Equal("One", "Three");
            model.CurrentGroupName.Should().Be("One");
            model.CanRemoveFromGroup.Should().BeTrue();
        }

        [Fact]
        public void BlankNamedGroups_AreSkipped()
        {
            var groups = new List<CharacterGroupSettings> { Group("", A), Group("Mains", B) };

            var model = CharacterGroupMenuModel.Build(groups, A);

            model.Groups.Select(g => g.Name).Should().Equal("Mains");
            model.CanRemoveFromGroup.Should().BeFalse();
        }

        [Fact]
        public void PickingAnotherGroup_ThenRebuilding_MovesCurrentMark()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A, B), Group("Alts", C) };

            var before = CharacterGroupMenuModel.Build(groups, A);
            var target = before.Groups.Single(g => !g.IsCurrent).Name;
            CharacterGroupMutator.AddToGroup(groups, target, A).Should().BeTrue();
            var after = CharacterGroupMenuModel.Build(groups, A);

            after.CurrentGroupName.Should().Be("Alts");
            groups.Single(g => g.Name == "Mains").CharacterGuids.Should().Equal(B);
        }

        [Fact]
        public void RemoveFromGroup_ThenRebuilding_HidesRemove_AndDropsEmptiedGroup()
        {
            var groups = new List<CharacterGroupSettings> { Group("Solo", A), Group("Alts", B) };

            CharacterGroupMenuModel.Build(groups, A).CanRemoveFromGroup.Should().BeTrue();
            CharacterGroupMutator.RemoveFromAllGroups(groups, A).Should().BeTrue();
            var after = CharacterGroupMenuModel.Build(groups, A);

            after.CanRemoveFromGroup.Should().BeFalse();
            after.Groups.Select(g => g.Name).Should().Equal("Alts");
        }
    }
}
