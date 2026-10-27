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

        /// <summary>
        /// A new group is created only after the pilot names it,
        /// so the name rules are checked before anything is built.
        /// </summary>
        [Theory]
        [InlineData("", "Overview.GroupNameRequired")]
        [InlineData("   ", "Overview.GroupNameRequired")]
        [InlineData("mains", "Overview.GroupNameTaken")]
        [InlineData(" Mains ", "Overview.GroupNameTaken")]
        [InlineData("ungrouped", "Overview.GroupNameTaken")]
        [InlineData("Miners", null)]
        public void GroupNameProblem_ExplainsWhyANameCantBeUsed(string name, string? expected)
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A) };
            CharacterGroupMutator.GroupNameProblem(groups, name).Should().Be(expected);
        }

        [Fact]
        public void GroupNameProblem_LetsARenamedGroupKeepItsOwnName()
        {
            var mains = Group("Mains", A);
            var groups = new List<CharacterGroupSettings> { mains, Group("Alts", B) };
            CharacterGroupMutator.GroupNameProblem(groups, "MAINS", except: mains).Should().BeNull();
            CharacterGroupMutator.GroupNameProblem(groups, "alts", except: mains).Should().Be("Overview.GroupNameTaken");
        }

        [Fact]
        public void RenameGroup_CannotTakeTheUngroupedSectionName()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A) };
            CharacterGroupMutator.RenameGroup(groups, "Mains", "Ungrouped").Should().BeFalse(
                "a group called Ungrouped would be mistaken for the ungrouped section");
            groups[0].Name.Should().Be("Mains");
        }

        [Fact]
        public void CreateGroup_WithAValidatedName_UsesItExactly()
        {
            var groups = new List<CharacterGroupSettings> { Group("Mains", A) };
            CharacterGroupMutator.GroupNameProblem(groups, "Haulers").Should().BeNull();
            CharacterGroupMutator.CreateGroup(groups, new[] { B }, "Haulers").Name.Should().Be("Haulers");
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

        [Fact]
        public void RemoveFromAllGroups_ReportsWhetherCharacterWasGrouped()
        {
            var groups = new List<CharacterGroupSettings> { Group("Pair", A, B) };

            CharacterGroupMutator.RemoveFromAllGroups(groups, C).Should().BeFalse();
            groups.Single().CharacterGuids.Should().Equal(A, B);

            CharacterGroupMutator.RemoveFromAllGroups(groups, A).Should().BeTrue();
            groups.Single().CharacterGuids.Should().Equal(B);
        }

        [Fact]
        public void CreateGroup_UsesCallerSuppliedBaseName()
        {
            var groups = new List<CharacterGroupSettings>();

            var created = CharacterGroupMutator.CreateGroup(groups, new[] { A }, "Nuevo grupo");

            created.Name.Should().Be("Nuevo grupo");
        }

        [Fact]
        public void CreateGroup_NumbersCallerSuppliedBaseName_WhenTaken()
        {
            var groups = new List<CharacterGroupSettings> { Group("Nuevo grupo", C) };

            var second = CharacterGroupMutator.CreateGroup(groups, new[] { A }, "Nuevo grupo");
            var third = CharacterGroupMutator.CreateGroup(groups, new[] { B }, "Nuevo grupo");

            second.Name.Should().Be("Nuevo grupo 2");
            third.Name.Should().Be("Nuevo grupo 3");
        }

        [Fact]
        public void CreateGroup_NumbersBaseName_CaseInsensitively()
        {
            var groups = new List<CharacterGroupSettings> { Group("nuevo GRUPO", C) };

            CharacterGroupMutator.CreateGroup(groups, new[] { A }, "Nuevo grupo")
                .Name.Should().Be("Nuevo grupo 2");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CreateGroup_BlankBaseName_FallsBackToDefault(string? baseName)
        {
            var groups = new List<CharacterGroupSettings>();

            CharacterGroupMutator.CreateGroup(groups, new[] { A }, baseName)
                .Name.Should().Be(CharacterGroupMutator.DefaultGroupName);
        }

        [Fact]
        public void CreateGroup_SingleMember_FromExistingGroup_MovesIt()
        {
            // The card menu's "New group..." path: one character, possibly grouped.
            var groups = new List<CharacterGroupSettings> { Group("Mains", A, B) };

            var created = CharacterGroupMutator.CreateGroup(groups, new[] { A }, "New Group");

            created.CharacterGuids.Should().Equal(A);
            groups.Single(g => g.Name == "Mains").CharacterGuids.Should().Equal(B);
            groups.Should().HaveCount(2);
        }
    }
}
