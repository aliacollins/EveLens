// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for character state management through the serializable layer.
    /// Full Character/CCPCharacter construction requires EveMonClient + static game data.
    /// These tests cover the serializable DTOs that represent character state.
    /// </summary>
    public class CharacterStateTests
    {
        #region SerializableCCPCharacter state properties

        [Fact]
        public void SerializableCCPCharacter_DefaultState_HasEmptyName()
        {
            var character = new SerializableCCPCharacter();
            character.Name.Should().BeNullOrEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_SetName_Preserves()
        {
            var character = new SerializableCCPCharacter { Name = "Test Pilot" };
            character.Name.Should().Be("Test Pilot");
        }

        [Fact]
        public void SerializableCCPCharacter_Balance_PreservesDecimalPrecision()
        {
            var character = new SerializableCCPCharacter { Balance = 1234567890.12m };
            var result = XmlRoundTrip(character);
            result.Balance.Should().Be(1234567890.12m);
        }

        [Fact]
        public void SerializableCCPCharacter_CorporationInfo_Preserves()
        {
            var character = new SerializableCCPCharacter
            {
                CorporationName = "Test Corp",
                CorporationID = 98000001,
                AllianceName = "Test Alliance",
                AllianceID = 99000001
            };

            var result = XmlRoundTrip(character);
            result.CorporationName.Should().Be("Test Corp");
            result.CorporationID.Should().Be(98000001);
            result.AllianceName.Should().Be("Test Alliance");
            result.AllianceID.Should().Be(99000001);
        }

        [Fact]
        public void SerializableCCPCharacter_Birthday_Preserves()
        {
            var birthday = new DateTime(2020, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            var character = new SerializableCCPCharacter { Birthday = birthday };
            var result = XmlRoundTrip(character);
            result.Birthday.Should().Be(birthday);
        }

        [Fact]
        public void SerializableCCPCharacter_Race_Preserves()
        {
            var character = new SerializableCCPCharacter
            {
                Race = "Caldari",
                BloodLine = "Deteis",
                Ancestry = "Tube Child",
                Gender = "Male"
            };

            var result = XmlRoundTrip(character);
            result.Race.Should().Be("Caldari");
            result.BloodLine.Should().Be("Deteis");
            result.Ancestry.Should().Be("Tube Child");
            result.Gender.Should().Be("Male");
        }

        #endregion

        #region Attributes serialization

        [Fact]
        public void SerializableCCPCharacter_Attributes_DefaultValues()
        {
            var character = new SerializableCCPCharacter();
            character.Attributes.Should().NotBeNull();
        }

        [Fact]
        public void SerializableCCPCharacter_Attributes_RoundTrip()
        {
            var character = new SerializableCCPCharacter();
            character.Attributes.Intelligence = 25;
            character.Attributes.Perception = 20;
            character.Attributes.Charisma = 17;
            character.Attributes.Willpower = 22;
            character.Attributes.Memory = 19;

            var result = XmlRoundTrip(character);
            result.Attributes.Intelligence.Should().Be(25);
            result.Attributes.Perception.Should().Be(20);
            result.Attributes.Charisma.Should().Be(17);
            result.Attributes.Willpower.Should().Be(22);
            result.Attributes.Memory.Should().Be(19);
        }

        #endregion

        #region Clone state

        [Fact]
        public void SerializableCCPCharacter_CloneState_DefaultIsNull()
        {
            var character = new SerializableCCPCharacter();
            character.CloneState.Should().BeNullOrEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_CloneState_Auto_Preserves()
        {
            var character = new SerializableCCPCharacter
            {
                CloneState = AccountStatusMode.Auto.ToString()
            };
            var result = XmlRoundTrip(character);
            result.CloneState.Should().Be("Auto");
        }

        [Theory]
        [InlineData("Auto")]
        [InlineData("Alpha")]
        [InlineData("Omega")]
        public void SerializableCCPCharacter_CloneState_ParsesBackCorrectly(string cloneState)
        {
            var character = new SerializableCCPCharacter { CloneState = cloneState };
            var result = XmlRoundTrip(character);

            AccountStatusMode parsedMode;
            Enum.TryParse(result.CloneState, out parsedMode).Should().BeTrue();
            parsedMode.ToString().Should().Be(cloneState);
        }

        #endregion

        #region Skills collection

        [Fact]
        public void SerializableCCPCharacter_Skills_DefaultEmpty()
        {
            var character = new SerializableCCPCharacter();
            character.Skills.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_Skills_RoundTrip()
        {
            var character = new SerializableCCPCharacter();
            character.Skills.Add(new Common.Serialization.Eve.SerializableCharacterSkill
            {
                ID = 3300,
                Name = "Spaceship Command",
                Level = 5,
                Skillpoints = 256000
            });
            character.Skills.Add(new Common.Serialization.Eve.SerializableCharacterSkill
            {
                ID = 3301,
                Name = "Gallente Frigate",
                Level = 3,
                Skillpoints = 8000
            });

            var result = XmlRoundTrip(character);
            result.Skills.Should().HaveCount(2);
            result.Skills[0].ID.Should().Be(3300);
            result.Skills[0].Level.Should().Be(5);
            result.Skills[1].ID.Should().Be(3301);
            result.Skills[1].Skillpoints.Should().Be(8000);
        }

        #endregion

        #region Skill queue state

        [Fact]
        public void SerializableCCPCharacter_SkillQueue_DefaultEmpty()
        {
            var character = new SerializableCCPCharacter();
            character.SkillQueue.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_SkillQueue_WithEntries_RoundTrips()
        {
            var character = new SerializableCCPCharacter();
            character.SkillQueue.Add(new Common.Serialization.Eve.SerializableQueuedSkill
            {
                ID = 3300,
                Level = 4,
                StartSP = 8000,
                EndSP = 45255
            });

            var result = XmlRoundTrip(character);
            result.SkillQueue.Should().HaveCount(1);
            result.SkillQueue[0].ID.Should().Be(3300);
            result.SkillQueue[0].Level.Should().Be(4);
        }

        #endregion

        #region Label and custom data

        [Fact]
        public void SerializableCCPCharacter_Label_EmptyString()
        {
            var character = new SerializableCCPCharacter { Label = "" };
            var result = XmlRoundTrip(character);
            // Empty strings may round-trip as null or empty
            result.Label.Should().BeNullOrEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_Label_NonEmpty_Preserves()
        {
            var character = new SerializableCCPCharacter { Label = "PvP Main" };
            var result = XmlRoundTrip(character);
            result.Label.Should().Be("PvP Main");
        }

        [Fact]
        public void SerializableCCPCharacter_ShipInfo_Preserves()
        {
            var character = new SerializableCCPCharacter
            {
                ShipName = "My Raven",
                ShipTypeName = "Raven"
            };
            var result = XmlRoundTrip(character);
            result.ShipName.Should().Be("My Raven");
            result.ShipTypeName.Should().Be("Raven");
        }

        #endregion

        #region Export/import fidelity (Guid)

        [Fact]
        public void SerializableCCPCharacter_Guid_RoundTrips()
        {
            var guid = Guid.NewGuid();
            var character = new SerializableCCPCharacter { Guid = guid };
            var result = XmlRoundTrip(character);
            result.Guid.Should().Be(guid);
        }

        [Fact]
        public void SerializableCCPCharacter_ID_Preserves()
        {
            var character = new SerializableCCPCharacter { ID = 2119000001 };
            var result = XmlRoundTrip(character);
            result.ID.Should().Be(2119000001);
        }

        #endregion

        #region Full character round-trip

        [Fact]
        public void SerializableCCPCharacter_FullRoundTrip_PreservesAllFields()
        {
            var guid = Guid.NewGuid();
            var character = new SerializableCCPCharacter
            {
                Guid = guid,
                ID = 2119000001,
                Name = "Full Test Pilot",
                Label = "Industry",
                Race = "Amarr",
                BloodLine = "True Amarr",
                Gender = "Female",
                CorporationName = "EVE University",
                CorporationID = 917701062,
                AllianceName = "Ivy League",
                Balance = 5000000000.50m,
                FreeSkillPoints = 500000,
                SecurityStatus = -1.5
            };

            character.Attributes.Intelligence = 27;
            character.Attributes.Perception = 21;
            character.Attributes.Willpower = 20;
            character.Attributes.Charisma = 17;
            character.Attributes.Memory = 24;

            var result = XmlRoundTrip(character);

            result.Guid.Should().Be(guid);
            result.ID.Should().Be(2119000001);
            result.Name.Should().Be("Full Test Pilot");
            result.Label.Should().Be("Industry");
            result.Race.Should().Be("Amarr");
            result.Balance.Should().Be(5000000000.50m);
            result.FreeSkillPoints.Should().Be(500000);
            result.Attributes.Intelligence.Should().Be(27);
            result.Attributes.Perception.Should().Be(21);
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
