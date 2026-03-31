// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// Tests for plan serialization DTOs.
    /// Full Plan construction requires BaseCharacter + static skill data.
    /// These tests cover the serializable layer.
    /// </summary>
    public class PlanSerializationTests
    {
        [Fact]
        public void SerializablePlan_DefaultConstructor_InitializesCollections()
        {
            var plan = new SerializablePlan();
            plan.Entries.Should().NotBeNull().And.BeEmpty();
            plan.InvalidEntries.Should().NotBeNull().And.BeEmpty();
            plan.SortingPreferences.Should().NotBeNull();
        }

        [Fact]
        public void SerializablePlan_XmlRoundTrip_PreservesName()
        {
            var plan = new SerializablePlan { Name = "Cruiser Training" };
            var result = XmlRoundTrip(plan);
            result.Name.Should().Be("Cruiser Training");
        }

        [Fact]
        public void SerializablePlan_XmlRoundTrip_PreservesOwner()
        {
            var guid = Guid.NewGuid();
            var plan = new SerializablePlan { Name = "Test", Owner = guid };
            var result = XmlRoundTrip(plan);
            result.Owner.Should().Be(guid);
        }

        [Fact]
        public void SerializablePlan_XmlRoundTrip_PreservesDescription()
        {
            var plan = new SerializablePlan
            {
                Name = "Test",
                Description = "Train into a Raven Navy Issue for L4 missions"
            };
            var result = XmlRoundTrip(plan);
            result.Description.Should().Be("Train into a Raven Navy Issue for L4 missions");
        }

        [Fact]
        public void SerializablePlan_WithEntries_PreservesCount()
        {
            var plan = new SerializablePlan { Name = "Test" };
            plan.Entries.Add(new SerializablePlanEntry { ID = 3300, SkillName = "Spaceship Command", Level = 5 });
            plan.Entries.Add(new SerializablePlanEntry { ID = 3301, SkillName = "Gallente Frigate", Level = 3 });

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(2);
        }

        [Fact]
        public void SerializablePlan_EmptyEntries_RoundTrips()
        {
            var plan = new SerializablePlan { Name = "Empty Plan" };
            var result = XmlRoundTrip(plan);
            result.Entries.Should().BeEmpty();
        }

        [Fact]
        public void SerializablePlanEntry_DefaultPriority_IsThree()
        {
            new SerializablePlanEntry().Priority.Should().Be(3);
        }

        [Fact]
        public void SerializablePlanEntry_XmlRoundTrip_PreservesAllFields()
        {
            var entry = new SerializablePlanEntry
            {
                ID = 3350,
                SkillName = "Caldari Battleship",
                Level = 4,
                Priority = 1,
                Notes = "For Raven"
            };
            var result = XmlRoundTrip(entry);
            result.ID.Should().Be(3350);
            result.SkillName.Should().Be("Caldari Battleship");
            result.Level.Should().Be(4);
            result.Priority.Should().Be(1);
            result.Notes.Should().Be("For Raven");
        }

        [Fact]
        public void SerializablePlanEntry_PlanGroups_Initialized()
        {
            var entry = new SerializablePlanEntry();
            entry.PlanGroups.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SerializablePlan_MultipleEntries_PreservesOrder()
        {
            var plan = new SerializablePlan { Name = "Ordered" };
            for (int i = 0; i < 10; i++)
            {
                plan.Entries.Add(new SerializablePlanEntry
                {
                    ID = 1000 + i,
                    SkillName = $"Skill {i}",
                    Level = (i % 5) + 1
                });
            }

            var result = XmlRoundTrip(plan);
            result.Entries.Should().HaveCount(10);
            for (int i = 0; i < 10; i++)
            {
                result.Entries[i].ID.Should().Be(1000 + i);
                result.Entries[i].Level.Should().Be((i % 5) + 1);
            }
        }

        [Fact]
        public void SerializablePlan_XmlRoundTrip_PreservesLastActivity()
        {
            var timestamp = new DateTime(2026, 3, 30, 14, 0, 0, DateTimeKind.Utc);
            var plan = new SerializablePlan
            {
                Name = "Test Plan",
                LastActivity = timestamp
            };

            var result = XmlRoundTrip(plan);
            result.LastActivity.Should().Be(timestamp);
        }

        [Fact]
        public void SerializablePlan_XmlRoundTrip_DefaultLastActivity_IsMinValue()
        {
            var plan = new SerializablePlan { Name = "New Plan" };

            var result = XmlRoundTrip(plan);
            result.LastActivity.Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void JsonPlan_LastActivity_NullForNewPlans()
        {
            // Existing plans without LastActivity should deserialize as null
            var json = new EveLens.Common.Helpers.JsonPlan
            {
                Name = "Legacy Plan"
            };
            json.LastActivity.Should().BeNull();
        }

        [Fact]
        public void JsonPlan_LastActivity_PreservesTimestamp()
        {
            var timestamp = new DateTime(2026, 3, 30, 14, 0, 0, DateTimeKind.Utc);
            var json = new EveLens.Common.Helpers.JsonPlan
            {
                Name = "Recent Plan",
                LastActivity = timestamp
            };
            json.LastActivity.Should().Be(timestamp);
        }

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
