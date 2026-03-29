// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections.Global;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Issue #47: Queue Health flyout did not display all characters.
    /// Root cause: characters not in the saved MonitoredCharacters list were never
    /// set to Monitored=true, making them invisible to Queue Health and other UI.
    /// Fix: GlobalMonitoredCharacterCollection.Import() now ensures every character
    /// in AppServices.Characters is monitored after import.
    /// </summary>
    [Collection("AppServices")]
    public class Issue47QueueHealthTests
    {
        public Issue47QueueHealthTests()
        {
            AppServices.Reset();
            AppServices.SyncToServiceLocator();
        }

        [Fact]
        public void Import_UnmonitoredCharacters_BecomeMonitored()
        {
            // Arrange: 5 characters in the global collection
            var services = new NullCharacterServices();
            var characters = new GlobalCharacterCollection();
            var guids = new Guid[5];

            for (int i = 0; i < 5; i++)
            {
                var identity = new CharacterIdentity((long)(i + 1), $"Pilot {i + 1}");
                var character = new CCPCharacter(identity, services);
                guids[i] = character.Guid;
                characters.Add(character, notify: false, monitor: false);
            }

            AppServices.SetCharacters(characters);

            // Only 3 of the 5 are in the monitored settings (simulates migrated EVEMon data)
            var monitoredSettings = new List<MonitoredCharacterSettings>
            {
                new() { CharacterGuid = guids[0], Name = "Pilot 1" },
                new() { CharacterGuid = guids[2], Name = "Pilot 3" },
                new() { CharacterGuid = guids[4], Name = "Pilot 5" },
            };

            var monitored = new GlobalMonitoredCharacterCollection();
            AppServices.SetMonitoredCharacters(monitored);

            // Act: import the partial monitored list
            monitored.Import(monitoredSettings);

            // Assert: ALL 5 characters should be monitored, not just the 3
            monitored.Count.Should().Be(5, "all characters should be monitored even if not in saved settings");
            foreach (var character in AppServices.Characters)
            {
                character.Monitored.Should().BeTrue($"character '{character.Name}' should be monitored");
            }
        }

        [Fact]
        public void Import_EmptyMonitoredList_AllCharactersStillMonitored()
        {
            // Arrange: characters exist but monitored list is empty (fresh migration)
            var services = new NullCharacterServices();
            var characters = new GlobalCharacterCollection();

            for (int i = 0; i < 3; i++)
            {
                var identity = new CharacterIdentity((long)(i + 1), $"Pilot {i + 1}");
                var character = new CCPCharacter(identity, services);
                characters.Add(character, notify: false, monitor: false);
            }

            AppServices.SetCharacters(characters);

            var monitored = new GlobalMonitoredCharacterCollection();
            AppServices.SetMonitoredCharacters(monitored);

            // Act: import empty list
            monitored.Import(new List<MonitoredCharacterSettings>());

            // Assert: all 3 should still be picked up
            monitored.Count.Should().Be(3, "all characters should be monitored even with empty saved list");
        }

        [Fact]
        public void Import_AllAlreadyMonitored_NoDuplicates()
        {
            // Arrange: all characters are in the monitored settings (normal case)
            var services = new NullCharacterServices();
            var characters = new GlobalCharacterCollection();
            var guids = new Guid[3];

            for (int i = 0; i < 3; i++)
            {
                var identity = new CharacterIdentity((long)(i + 1), $"Pilot {i + 1}");
                var character = new CCPCharacter(identity, services);
                guids[i] = character.Guid;
                characters.Add(character, notify: false, monitor: false);
            }

            AppServices.SetCharacters(characters);

            var monitoredSettings = guids.Select((g, i) => new MonitoredCharacterSettings
            {
                CharacterGuid = g,
                Name = $"Pilot {i + 1}"
            }).ToList();

            var monitored = new GlobalMonitoredCharacterCollection();
            AppServices.SetMonitoredCharacters(monitored);

            // Act
            monitored.Import(monitoredSettings);

            // Assert: exactly 3, no duplicates
            monitored.Count.Should().Be(3, "no duplicates when all characters are already monitored");
        }
    }
}
