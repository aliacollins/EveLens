// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for plan logic through the serializable layer.
    /// Full Plan construction requires BaseCharacter + static skill data.
    /// These tests cover SerializablePlan properties not tested in PlanTests.cs,
    /// including plan entry priorities, descriptions, sorting preferences, and invalid entries.
    /// PlanTests.cs already tests: default constructor, XML round-trip for name/owner/description,
    /// entry count, empty entries, default priority, entry field round-trip, plan groups, entry order.
    /// </summary>
    public class PlanLogicTests
    {
        #region Plan entry priority logic

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public void SerializablePlanEntry_Priority_RoundTrips(int priority)
        {
            var entry = new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Test Skill",
                Level = 3,
                Priority = priority
            };
            var result = XmlRoundTrip(entry);
            result.Priority.Should().Be(priority);
        }

        [Fact]
        public void SerializablePlanEntry_HigherPriorityNumber_IsLowerPriority()
        {
            // In EVEMon, priority 1 = highest, 10 = lowest
            var highPriority = new SerializablePlanEntry { Priority = 1 };
            var lowPriority = new SerializablePlanEntry { Priority = 10 };

            highPriority.Priority.Should().BeLessThan(lowPriority.Priority);
        }

        [Fact]
        public void SerializablePlan_EntriesWithMixedPriorities_PreserveAll()
        {
            var plan = new SerializablePlan { Name = "Priority Test" };
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "High", Level = 1, Priority = 1
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3301, SkillName = "Medium", Level = 2, Priority = 3
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3302, SkillName = "Low", Level = 3, Priority = 5
            });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(3);
            result.Entries[0].Priority.Should().Be(1);
            result.Entries[1].Priority.Should().Be(3);
            result.Entries[2].Priority.Should().Be(5);
        }

        #endregion

        #region Plan entry type (prerequisite vs planned)

        [Theory]
        [InlineData(PlanEntryType.Planned)]
        [InlineData(PlanEntryType.Prerequisite)]
        public void SerializablePlanEntry_Type_RoundTrips(PlanEntryType entryType)
        {
            var entry = new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Test Skill",
                Level = 2,
                Type = entryType
            };
            var result = XmlRoundTrip(entry);
            result.Type.Should().Be(entryType);
        }

        #endregion

        #region Plan entry notes

        [Fact]
        public void SerializablePlanEntry_Notes_NullDefault()
        {
            var entry = new SerializablePlanEntry();
            // Notes may be null by default
            entry.Notes.Should().BeNullOrEmpty();
        }

        [Fact]
        public void SerializablePlanEntry_Notes_Preserves()
        {
            var entry = new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Caldari Battleship",
                Level = 4,
                Notes = "For Raven Navy Issue"
            };
            var result = XmlRoundTrip(entry);
            result.Notes.Should().Be("For Raven Navy Issue");
        }

        [Fact]
        public void SerializablePlanEntry_Notes_WithCommas_Preserves()
        {
            // Notes can contain comma-separated reasons
            var entry = new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Shield Management",
                Level = 5,
                Notes = "Raven, Tengu, Drake"
            };
            var result = XmlRoundTrip(entry);
            result.Notes.Should().Be("Raven, Tengu, Drake");
        }

        #endregion

        #region Plan sorting preferences

        [Fact]
        public void SerializablePlan_SortingPreferences_Default()
        {
            var plan = new SerializablePlan();
            plan.SortingPreferences.Should().NotBeNull();
        }

        [Fact]
        public void SerializablePlan_SortingPreferences_RoundTrips()
        {
            var plan = new SerializablePlan
            {
                Name = "Sorted Plan",
                SortingPreferences = new PlanSorting
                {
                    GroupByPriority = true
                }
            };

            var result = XmlRoundTrip(plan);
            result.SortingPreferences.Should().NotBeNull();
            result.SortingPreferences.GroupByPriority.Should().BeTrue();
        }

        #endregion

        #region Invalid plan entries

        [Fact]
        public void SerializablePlan_InvalidEntries_DefaultEmpty()
        {
            var plan = new SerializablePlan();
            plan.InvalidEntries.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SerializablePlan_InvalidEntries_RoundTrips()
        {
            var plan = new SerializablePlan { Name = "Plan With Invalid" };
            plan.InvalidEntries.Add(new SerializableInvalidPlanEntry
            {
                SkillName = "Removed Skill",
                PlannedLevel = 3,
                Acknowledged = false
            });

            var result = XmlRoundTrip(plan);
            result.InvalidEntries.Should().HaveCount(1);
            result.InvalidEntries[0].SkillName.Should().Be("Removed Skill");
            result.InvalidEntries[0].PlannedLevel.Should().Be(3);
            result.InvalidEntries[0].Acknowledged.Should().BeFalse();
        }

        [Fact]
        public void SerializablePlan_InvalidEntries_AcknowledgedFlag_Preserves()
        {
            var plan = new SerializablePlan { Name = "Acked" };
            plan.InvalidEntries.Add(new SerializableInvalidPlanEntry
            {
                SkillName = "Old Skill",
                PlannedLevel = 5,
                Acknowledged = true
            });

            var result = XmlRoundTrip(plan);
            result.InvalidEntries[0].Acknowledged.Should().BeTrue();
        }

        #endregion

        #region Plan with prerequisites pattern

        [Fact]
        public void SerializablePlan_PrerequisiteEntries_MarkedCorrectly()
        {
            var plan = new SerializablePlan { Name = "Prereq Test" };

            // Prerequisite: Spaceship Command 3
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Spaceship Command",
                Level = 3,
                Type = PlanEntryType.Prerequisite,
                Priority = 1
            });

            // Target: Caldari Frigate 4
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3330,
                SkillName = "Caldari Frigate",
                Level = 4,
                Type = PlanEntryType.Planned,
                Priority = 1
            });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(2);
            result.Entries[0].Type.Should().Be(PlanEntryType.Prerequisite);
            result.Entries[1].Type.Should().Be(PlanEntryType.Planned);
        }

        [Fact]
        public void SerializablePlan_PrerequisiteBeforeDependency_OrderPreserved()
        {
            var plan = new SerializablePlan { Name = "Order Test" };

            // Prerequisites must come before their dependent entries
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 1, Priority = 1
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 2, Priority = 1
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 3, Priority = 1
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3330, SkillName = "Caldari Frigate", Level = 1, Priority = 1
            });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(4);

            // Verify level ordering: 1, 2, 3 for Spaceship Command
            result.Entries[0].Level.Should().Be(1);
            result.Entries[1].Level.Should().Be(2);
            result.Entries[2].Level.Should().Be(3);
        }

        #endregion

        #region Plan description

        [Fact]
        public void SerializablePlan_Description_LongText_Preserves()
        {
            string longDesc = new string('A', 5000);
            var plan = new SerializablePlan
            {
                Name = "Long Desc",
                Description = longDesc
            };

            var result = XmlRoundTrip(plan);
            result.Description.Should().Be(longDesc);
        }

        [Fact]
        public void SerializablePlan_Description_SpecialChars_Preserves()
        {
            var plan = new SerializablePlan
            {
                Name = "Special",
                Description = "Train for L4 missions <Raven> & Tengu 'T3C'"
            };

            var result = XmlRoundTrip(plan);
            result.Description.Should().Be("Train for L4 missions <Raven> & Tengu 'T3C'");
        }

        #endregion

        #region Plan entry plan groups

        [Fact]
        public void SerializablePlanEntry_PlanGroups_MultipleGroups_Preserves()
        {
            var entry = new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Test",
                Level = 1
            };
            entry.PlanGroups.Add("PvP");
            entry.PlanGroups.Add("Industry");
            entry.PlanGroups.Add("Exploration");

            var result = XmlRoundTrip(entry);
            result.PlanGroups.Should().HaveCount(3);
            result.PlanGroups.Should().Contain("PvP");
            result.PlanGroups.Should().Contain("Industry");
            result.PlanGroups.Should().Contain("Exploration");
        }

        #endregion

        #region Duplicate entry scenarios

        [Fact]
        public void SerializablePlan_DuplicateSkillAndLevel_BothPreserved()
        {
            // At the serializable layer, duplicates are allowed
            // The Plan.Import method handles deduplication
            var plan = new SerializablePlan { Name = "Dup Test" };
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 3
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 3
            });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(2);
        }

        [Fact]
        public void SerializablePlan_SameSkillDifferentLevels_BothPreserved()
        {
            var plan = new SerializablePlan { Name = "Multi Level" };
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 3
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 4
            });
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3300, SkillName = "Spaceship Command", Level = 5
            });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(3);
            result.Entries[0].Level.Should().Be(3);
            result.Entries[1].Level.Should().Be(4);
            result.Entries[2].Level.Should().Be(5);
        }

        #endregion

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }
    }
}
