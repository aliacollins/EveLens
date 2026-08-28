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
    /// Group mutations behind the overview's drag gestures (Issue #72 rework).
    /// </summary>
    public class CharacterGroupMutatorTests
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
        public void CreateGroup_AddsMembers_AndReturnsGroup()
        {
            var groups = new List<CharacterGroupSettings>();

            var created = CharacterGroupMutator.CreateGroup(groups, new[] { A, B });

            groups.Should().ContainSingle().Which.Should().BeSameAs(created);
            created.Name.Should().Be("New Group");
            created.CharacterGuids.Should().Equal(A, B);
        }

        [Fact]
        public void CreateGroup_NumbersName_WhenTaken()
        {
            var groups = new List<CharacterGroupSettings> { Group("New Group", C) };

            var created = CharacterGroupMutator.CreateGroup(groups, new[] { A, B });

            created.Name.Should().Be("New Group 2");
        }

        [Fact]
        public void CreateGroup_PullsMembersOutOfTheirOldGroups()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A, C) };

            CharacterGroupMutator.CreateGroup(groups, new[] { A, B });

            groups.Single(g => g.Name == "Mains").CharacterGuids.Should().Equal(C);
        }

        [Fact]
        public void AddToGroup_MovesCharacterBetweenGroups()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A), Group("Alts", B) };

            CharacterGroupMutator.AddToGroup(groups, "Alts", A).Should().BeTrue();

            groups.Single(g => g.Name == "Alts").CharacterGuids.Should().Equal(B, A);
            // "Mains" emptied out and must be gone — empty folders don't exist
            groups.Should().NotContain(g => g.Name == "Mains");
        }

        [Fact]
        public void AddToGroup_ReturnsFalse_ForUnknownGroupOrExistingMember()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A) };

            CharacterGroupMutator.AddToGroup(groups, "Nope", B).Should().BeFalse();
            CharacterGroupMutator.AddToGroup(groups, "Mains", A).Should().BeFalse();
        }

        [Fact]
        public void RenameGroup_Renames_AndRejectsDuplicatesOrBlank()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A), Group("Alts", B) };

            CharacterGroupMutator.RenameGroup(groups, "Mains", "Chars").Should().BeTrue();
            groups[0].Name.Should().Be("Chars");

            CharacterGroupMutator.RenameGroup(groups, "Chars", "ALTS").Should().BeFalse("name collides case-insensitively");
            CharacterGroupMutator.RenameGroup(groups, "Chars", "   ").Should().BeFalse("blank name");
            CharacterGroupMutator.RenameGroup(groups, "Nope", "X").Should().BeFalse("unknown group");
        }

        [Fact]
        public void MoveGroup_ReordersAndClampsAtEnds()
        {
            var groups = new List<CharacterGroupSettings> { Group("A", A), Group("B", B), Group("C", C) };

            CharacterGroupMutator.MoveGroup(groups, "C", -1).Should().BeTrue();
            groups.Select(g => g.Name).Should().Equal("A", "C", "B");

            CharacterGroupMutator.MoveGroup(groups, "A", -1).Should().BeFalse("already first");
            CharacterGroupMutator.MoveGroup(groups, "B", +1).Should().BeFalse("already last");
        }

        [Fact]
        public void DeleteGroup_RemovesGroup_MembersBecomeUngrouped()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A, B) };

            CharacterGroupMutator.DeleteGroup(groups, "Mains").Should().BeTrue();
            groups.Should().BeEmpty();
            CharacterGroupMutator.DeleteGroup(groups, "Mains").Should().BeFalse();
        }

        [Fact]
        public void RemoveFromAllGroups_DeletesEmptiedGroups()
        {
            var groups = new List<CharacterGroupSettings> { Group("Solo", A), Group("Pair", A, B) };

            CharacterGroupMutator.RemoveFromAllGroups(groups, A);

            groups.Should().ContainSingle().Which.Name.Should().Be("Pair");
            groups[0].CharacterGuids.Should().Equal(B);
        }
    }
}
