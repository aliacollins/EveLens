// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Models;
using EveLens.Common.ViewModels;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class CharacterComparisonViewModelTests
    {
        [Fact]
        public void CanInstantiate()
        {
            var vm = new CharacterComparisonViewModel();
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_IsEmpty()
        {
            var vm = new CharacterComparisonViewModel();
            vm.SelectedCharacters.Should().BeEmpty();
            vm.Groups.Should().BeEmpty();
            vm.VisibleSkillCount.Should().Be(0);
            vm.DifferenceCount.Should().Be(0);
            vm.TextFilter.Should().BeEmpty();
            vm.ShowDifferencesOnly.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void AddCharacter_ReturnsTrue_WhenUnderLimit()
        {
            var vm = new CharacterComparisonViewModel();
            var character = CreateTestCharacter(1, "Test Pilot");
            vm.AddCharacter(character).Should().BeTrue();
            vm.SelectedCharacters.Should().HaveCount(1);
            vm.Dispose();
        }

        [Fact]
        public void AddCharacter_RejectsDuplicate()
        {
            var vm = new CharacterComparisonViewModel();
            var character = CreateTestCharacter(1, "Test Pilot");
            vm.AddCharacter(character).Should().BeTrue();
            vm.AddCharacter(character).Should().BeFalse();
            vm.SelectedCharacters.Should().HaveCount(1);
            vm.Dispose();
        }

        [Fact]
        public void AddCharacter_RejectsOver10()
        {
            var vm = new CharacterComparisonViewModel();
            for (int i = 1; i <= 10; i++)
                vm.AddCharacter(CreateTestCharacter(i, $"Pilot {i}")).Should().BeTrue();

            vm.AddCharacter(CreateTestCharacter(11, "Pilot 11")).Should().BeFalse();
            vm.SelectedCharacters.Should().HaveCount(10);
            vm.Dispose();
        }

        [Fact]
        public void RemoveCharacter_RemovesFromList()
        {
            var vm = new CharacterComparisonViewModel();
            var character = CreateTestCharacter(1, "Test Pilot");
            vm.AddCharacter(character);
            vm.RemoveCharacter(character);
            vm.SelectedCharacters.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new CharacterComparisonViewModel();
            var act = () => vm.TextFilter = "Navigation";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ShowDifferencesOnly_DoesNotThrow()
        {
            var vm = new CharacterComparisonViewModel();
            var act = () => vm.ShowDifferencesOnly = true;
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_DoesNotThrow()
        {
            var vm = new CharacterComparisonViewModel();
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_DoesNotThrow()
        {
            var vm = new CharacterComparisonViewModel();
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Rebuild_WithNoCharacters_ProducesEmptyGroups()
        {
            var vm = new CharacterComparisonViewModel();
            vm.Rebuild();
            vm.Groups.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_IsSafe()
        {
            var vm = new CharacterComparisonViewModel();
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        private static Character CreateTestCharacter(long id, string name)
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(id, name);
            return new CCPCharacter(identity, services);
        }
    }
}
