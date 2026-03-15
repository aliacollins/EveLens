// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class ClonesViewModelTests
    {
        private static ClonesViewModel CreateViewModel()
        {
            var aggregator = new EventAggregator();
            var dispatcher = Substitute.For<IDispatcher>();
            dispatcher.When(d => d.Post(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());
            return new ClonesViewModel(aggregator, dispatcher);
        }

        private static CCPCharacter CreateCharacter()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(1L, "Test Pilot");
            return new CCPCharacter(identity, services);
        }

        [Fact]
        public void Initial_State_Has_Empty_JumpClones()
        {
            var vm = CreateViewModel();
            vm.JumpClones.Should().BeEmpty();
            vm.JumpCloneCount.Should().Be(0);
        }

        [Fact]
        public void Setting_Character_Populates_ActiveClone()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            vm.ActiveClone.Should().NotBeNull();
            vm.ActiveClone!.Name.Should().Be("Active Clone");
            vm.ActiveClone.IsActive.Should().BeTrue();
        }

        [Fact]
        public void CloneJump_Available_When_NeverJumped()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            vm.CloneJumpAvailable.Should().BeTrue();
            vm.CloneJumpStatusText.Should().Be("Ready");
            vm.LastCloneJumpText.Should().Be("Never");
        }

        [Fact]
        public void ForceRefresh_Updates_Data()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            vm.ForceRefresh();

            // Should not throw and should have valid data
            vm.ActiveClone.Should().NotBeNull();
        }

        [Fact]
        public void Null_Character_Does_Not_Throw()
        {
            var vm = CreateViewModel();
            vm.Character = null;

            vm.ActiveClone.Should().BeNull();
            vm.JumpClones.Should().BeEmpty();
        }

        [Fact]
        public void ActiveClone_Shows_No_Implants_For_Fresh_Character()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            // Fresh character has no implants — all slots are empty (ID <= 0)
            vm.ActiveClone.Should().NotBeNull();
            vm.ActiveClone!.ImplantSummary.Should().Be("No attribute implants");
        }

        [Fact]
        public void HomeStationName_Defaults_To_Unknown_For_Fresh_Character()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            vm.HomeStationName.Should().Be("Unknown");
        }

        [Fact]
        public void HomeStationChanged_Shows_Never_For_Fresh_Character()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            vm.HomeStationChangedText.Should().Be("Never");
        }

        [Fact]
        public void JumpCloneCount_Is_Zero_For_Fresh_Character()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            vm.JumpCloneCount.Should().Be(0);
            vm.JumpClones.Should().BeEmpty();
        }

        [Fact]
        public void Dispose_Does_Not_Throw()
        {
            var vm = CreateViewModel();
            var character = CreateCharacter();
            vm.Character = character;

            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
