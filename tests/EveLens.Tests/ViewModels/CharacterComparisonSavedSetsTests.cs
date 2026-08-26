// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// Saved comparison sets (Discussion #105). Separate class from
    /// CharacterComparisonViewModelTests because these mutate static Settings state.
    /// </summary>
    [Collection("AppServices")]
    public class CharacterComparisonSavedSetsTests : IDisposable
    {
        public CharacterComparisonSavedSetsTests()
        {
            AppServices.Reset();
            EveLens.Common.Settings.SavedComparisons = new List<SavedComparisonSettings>();
        }

        public void Dispose()
        {
            EveLens.Common.Settings.SavedComparisons = new List<SavedComparisonSettings>();
            AppServices.Reset();
        }

        [Fact]
        public void SaveCurrentSet_PersistsSelectedCharacterIds()
        {
            var vm = new CharacterComparisonViewModel();
            vm.AddCharacter(CreateTestCharacter(11, "Alpha"));
            vm.AddCharacter(CreateTestCharacter(22, "Bravo"));

            vm.SaveCurrentSet("Mains").Should().BeTrue();

            var saved = EveLens.Common.Settings.SavedComparisons.Single();
            saved.Name.Should().Be("Mains");
            saved.CharacterIDs.Should().Equal(11, 22);
            vm.Dispose();
        }

        [Fact]
        public void SaveCurrentSet_ReplacesSetWithSameName_CaseInsensitive()
        {
            var vm = new CharacterComparisonViewModel();
            vm.AddCharacter(CreateTestCharacter(11, "Alpha"));
            vm.SaveCurrentSet("Mains").Should().BeTrue();

            vm.AddCharacter(CreateTestCharacter(22, "Bravo"));
            vm.SaveCurrentSet("MAINS").Should().BeTrue();

            var saved = EveLens.Common.Settings.SavedComparisons.Single();
            saved.CharacterIDs.Should().Equal(11, 22);
            vm.Dispose();
        }

        [Fact]
        public void SaveCurrentSet_RejectsBlankName_AndEmptySelection()
        {
            var vm = new CharacterComparisonViewModel();
            vm.SaveCurrentSet("Anything").Should().BeFalse("no characters are selected");

            vm.AddCharacter(CreateTestCharacter(11, "Alpha"));
            vm.SaveCurrentSet("   ").Should().BeFalse("name is blank");

            EveLens.Common.Settings.SavedComparisons.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void LoadSet_RestoresCharactersInSavedOrder()
        {
            var alpha = CreateTestCharacter(11, "Alpha");
            var bravo = CreateTestCharacter(22, "Bravo");
            var set = new SavedComparisonSettings { Name = "Mains", CharacterIDs = { 22, 11 } };

            var vm = new CharacterComparisonViewModel();
            vm.AddCharacter(CreateTestCharacter(99, "Replaced"));

            int loaded = vm.LoadSet(set, new[] { alpha, bravo });

            loaded.Should().Be(2);
            vm.SelectedCharacters.Select(c => c.CharacterID).Should().Equal(22, 11);
            vm.Dispose();
        }

        [Fact]
        public void LoadSet_SkipsCharactersDeletedSinceSave()
        {
            var alpha = CreateTestCharacter(11, "Alpha");
            var set = new SavedComparisonSettings { Name = "Mains", CharacterIDs = { 11, 404 } };

            var vm = new CharacterComparisonViewModel();
            int loaded = vm.LoadSet(set, new[] { alpha });

            loaded.Should().Be(1, "character 404 no longer exists and must be skipped silently");
            vm.SelectedCharacters.Single().CharacterID.Should().Be(11);
            vm.Dispose();
        }

        [Fact]
        public void DeleteSet_RemovesFromSettings()
        {
            var set = new SavedComparisonSettings { Name = "Mains" };
            EveLens.Common.Settings.SavedComparisons.Add(set);

            var vm = new CharacterComparisonViewModel();
            vm.DeleteSet(set);

            EveLens.Common.Settings.SavedComparisons.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void SavedSets_AreSortedByName()
        {
            EveLens.Common.Settings.SavedComparisons.Add(new SavedComparisonSettings { Name = "zulu" });
            EveLens.Common.Settings.SavedComparisons.Add(new SavedComparisonSettings { Name = "Alpha" });

            var vm = new CharacterComparisonViewModel();
            vm.SavedSets.Select(s => s.Name).Should().Equal("Alpha", "zulu");
            vm.Dispose();
        }

        private static Character CreateTestCharacter(long id, string name)
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(id, name);
            return new CCPCharacter(identity, services);
        }
    }
}
