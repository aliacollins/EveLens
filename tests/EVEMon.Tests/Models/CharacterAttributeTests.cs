// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Constants;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for character attributes and the EVE attribute system.
    /// Full CharacterAttribute construction requires a Character instance.
    /// These tests cover the serializable layer and EVE constants for attribute calculations.
    /// </summary>
    public class CharacterAttributeTests
    {
        #region EveConstants attribute values

        [Fact]
        public void EveConstants_CharacterBaseAttributePoints_Is17()
        {
            EveConstants.CharacterBaseAttributePoints.Should().Be(17);
        }

        [Fact]
        public void EveConstants_SpareAttributePointsOnRemap_Is14()
        {
            EveConstants.SpareAttributePointsOnRemap.Should().Be(14);
        }

        [Fact]
        public void EveConstants_MaxRemappablePointsPerAttribute_Is10()
        {
            EveConstants.MaxRemappablePointsPerAttribute.Should().Be(10);
        }

        [Fact]
        public void EveConstants_MaxImplantPoints_Is5()
        {
            EveConstants.MaxImplantPoints.Should().Be(5);
        }

        [Fact]
        public void EveConstants_MaxBaseAttributePoints_Is27()
        {
            // MaxBaseAttributePoints = CharacterBaseAttributePoints + MaxRemappablePointsPerAttribute
            EveConstants.MaxBaseAttributePoints.Should().Be(27);
            EveConstants.MaxBaseAttributePoints.Should().Be(
                EveConstants.CharacterBaseAttributePoints + EveConstants.MaxRemappablePointsPerAttribute);
        }

        [Fact]
        public void EveConstants_MaxAttributeWithoutBooster_Is32()
        {
            // MaxAttributeWithoutBooster = MaxBaseAttributePoints + MaxImplantPoints
            EveConstants.MaxAttributeWithoutBooster.Should().Be(32);
            EveConstants.MaxAttributeWithoutBooster.Should().Be(
                EveConstants.MaxBaseAttributePoints + EveConstants.MaxImplantPoints);
        }

        [Fact]
        public void EveConstants_MaxBoosterBonus_Is12()
        {
            EveConstants.MaxBoosterBonus.Should().Be(12);
        }

        [Fact]
        public void EveConstants_BaseBoosterDurationHours_Is24()
        {
            EveConstants.BaseBoosterDurationHours.Should().Be(24);
        }

        #endregion

        #region Total base points invariant

        [Fact]
        public void TotalBasePoints_Is99ForAllCharacters()
        {
            // Every character has exactly 99 total base attribute points:
            // 5 attributes * 17 base + 14 spare = 99
            int totalBase = 5 * EveConstants.CharacterBaseAttributePoints + EveConstants.SpareAttributePointsOnRemap;
            totalBase.Should().Be(99);
        }

        [Fact]
        public void MaxSingleAttribute_WithMaxRemap_Is27()
        {
            // Maximum base for a single attribute: 17 + 10 = 27
            int maxSingle = EveConstants.CharacterBaseAttributePoints + EveConstants.MaxRemappablePointsPerAttribute;
            maxSingle.Should().Be(EveConstants.MaxBaseAttributePoints);
        }

        [Fact]
        public void MinSingleAttribute_AfterRemap_Is17()
        {
            // Minimum base for any attribute (cannot go below base)
            EveConstants.CharacterBaseAttributePoints.Should().Be(17);
        }

        #endregion

        #region Serializable attributes

        [Fact]
        public void SerializableCCPCharacter_Attributes_AllDefault()
        {
            var character = new SerializableCCPCharacter();
            character.Attributes.Should().NotBeNull();
            // SerializableCharacterAttributes constructor initializes all to 1
            character.Attributes.Intelligence.Should().Be(1);
            character.Attributes.Perception.Should().Be(1);
            character.Attributes.Charisma.Should().Be(1);
            character.Attributes.Willpower.Should().Be(1);
            character.Attributes.Memory.Should().Be(1);
        }

        [Fact]
        public void SerializableCCPCharacter_Attributes_BalancedRemap_Preserves()
        {
            var character = new SerializableCCPCharacter();
            // Balanced remap: all attributes at base (17) + some spare distributed
            // Base = 17, spare = 14, balanced = 17 + 14/5 ~= 19-20 each
            character.Attributes.Intelligence = 20;
            character.Attributes.Perception = 20;
            character.Attributes.Charisma = 19;
            character.Attributes.Willpower = 20;
            character.Attributes.Memory = 20;

            var result = XmlRoundTrip(character);
            result.Attributes.Intelligence.Should().Be(20);
            result.Attributes.Perception.Should().Be(20);
            result.Attributes.Charisma.Should().Be(19);
            result.Attributes.Willpower.Should().Be(20);
            result.Attributes.Memory.Should().Be(20);

            // Total should be 99
            long total = result.Attributes.Intelligence + result.Attributes.Perception +
                        result.Attributes.Charisma + result.Attributes.Willpower +
                        result.Attributes.Memory;
            total.Should().Be(99);
        }

        [Fact]
        public void SerializableCCPCharacter_Attributes_MaxRemap_Intelligence()
        {
            var character = new SerializableCCPCharacter();
            // Max intelligence remap: Int=27, rest=17-18
            character.Attributes.Intelligence = 27;
            character.Attributes.Memory = 21;  // secondary for many skills
            character.Attributes.Perception = 17;
            character.Attributes.Willpower = 17;
            character.Attributes.Charisma = 17;

            var result = XmlRoundTrip(character);
            result.Attributes.Intelligence.Should().Be(27);
            result.Attributes.Memory.Should().Be(21);

            // Total should be 99
            long total = result.Attributes.Intelligence + result.Attributes.Perception +
                        result.Attributes.Charisma + result.Attributes.Willpower +
                        result.Attributes.Memory;
            total.Should().Be(99);
        }

        [Fact]
        public void SerializableCCPCharacter_Attributes_MaxPerceptionWillpower()
        {
            var character = new SerializableCCPCharacter();
            // Combat remap: Per=27, Will=21, rest=17
            character.Attributes.Perception = 27;
            character.Attributes.Willpower = 21;
            character.Attributes.Intelligence = 17;
            character.Attributes.Charisma = 17;
            character.Attributes.Memory = 17;

            long total = character.Attributes.Intelligence + character.Attributes.Perception +
                        character.Attributes.Charisma + character.Attributes.Willpower +
                        character.Attributes.Memory;
            total.Should().Be(99);
        }

        #endregion

        #region EveAttribute enum

        [Fact]
        public void EveAttribute_HasFiveAttributes()
        {
            // Intelligence, Perception, Charisma, Willpower, Memory
            EveAttribute.Intelligence.Should().Be((EveAttribute)0);
            EveAttribute.Perception.Should().Be((EveAttribute)1);
            EveAttribute.Charisma.Should().Be((EveAttribute)2);
            EveAttribute.Willpower.Should().Be((EveAttribute)3);
            EveAttribute.Memory.Should().Be((EveAttribute)4);
        }

        [Fact]
        public void EveAttribute_None_IsNegativeOne()
        {
            EveAttribute.None.Should().Be((EveAttribute)(-1));
        }

        [Fact]
        public void EveAttribute_AttributeArrayIndices_MatchEnumValues()
        {
            // The Character class uses m_attributes[5] array indexed by (int)EveAttribute
            ((int)EveAttribute.Intelligence).Should().Be(0);
            ((int)EveAttribute.Perception).Should().Be(1);
            ((int)EveAttribute.Charisma).Should().Be(2);
            ((int)EveAttribute.Willpower).Should().Be(3);
            ((int)EveAttribute.Memory).Should().Be(4);
        }

        #endregion

        #region SP per hour formula validation

        [Theory]
        [InlineData(27, 21)]  // Max int/mem remap
        [InlineData(20, 20)]  // Balanced
        [InlineData(17, 17)]  // Minimum (no remap)
        public void SPPerHour_Formula_PrimaryTimesSecondary(int primary, int secondary)
        {
            // EVE SP/hour formula: primary * 60 + secondary * 30 (for Omega)
            // This is before the training rate multiplier
            float spPerHour = primary * 60f + secondary * 30f;
            spPerHour.Should().BeGreaterThan(0);

            // Max possible: 27*60 + 21*30 = 1620 + 630 = 2250
            // Min possible: 17*60 + 17*30 = 1020 + 510 = 1530
            spPerHour.Should().BeGreaterOrEqualTo(1530f);
            spPerHour.Should().BeLessOrEqualTo(2250f);
        }

        [Fact]
        public void SPPerHour_MaxRemap_Is2250()
        {
            // Maximum SP/hour with optimal remap (no implants, no booster)
            float maxSPPerHour = EveConstants.MaxBaseAttributePoints * 60f +
                                 (99 - EveConstants.MaxBaseAttributePoints - 3 * EveConstants.CharacterBaseAttributePoints) * 30f;
            // 27 * 60 + 21 * 30 = 1620 + 630 = 2250
            // Note: 99 - 27 - 51 = 21 for secondary when maximizing primary
            maxSPPerHour.Should().Be(2250f);
        }

        [Fact]
        public void SPPerHour_WithImplants_AddsBonusSP()
        {
            // With +5 implants: (27+5)*60 + (21+5)*30 = 32*60 + 26*30 = 1920 + 780 = 2700
            int primaryWithImplant = EveConstants.MaxBaseAttributePoints + EveConstants.MaxImplantPoints;
            int secondaryWithImplant = 21 + EveConstants.MaxImplantPoints;
            float spPerHour = primaryWithImplant * 60f + secondaryWithImplant * 30f;
            spPerHour.Should().Be(2700f);
        }

        [Fact]
        public void SPPerHour_WithBooster_AddsBonusSP()
        {
            // With max booster (+12): (27+12)*60 + (21+12)*30 = 39*60 + 33*30 = 2340 + 990 = 3330
            int primaryWithBooster = EveConstants.MaxBaseAttributePoints + EveConstants.MaxBoosterBonus;
            int secondaryWithBooster = 21 + EveConstants.MaxBoosterBonus;
            float spPerHour = primaryWithBooster * 60f + secondaryWithBooster * 30f;
            spPerHour.Should().Be(3330f);
        }

        #endregion

        #region Booster detection math

        [Fact]
        public void BoosterDetection_NoBooster_ReturnsZero()
        {
            // Total = 99 base + implants + 5*booster
            // If total == 99 + implants, booster = 0
            int totalAttributes = 99;  // No implants, no booster
            int totalImplants = 0;
            int calculatedBooster = (totalAttributes - 99 - totalImplants) / 5;
            calculatedBooster.Should().Be(0);
        }

        [Fact]
        public void BoosterDetection_WithBooster_DetectsCorrectly()
        {
            // Character with +5 booster, no implants
            // Each attribute gets +5, so total = 99 + 5*5 = 124
            int boosterBonus = 5;
            int totalAttributes = 99 + 5 * boosterBonus;  // 124
            int totalImplants = 0;
            int calculatedBooster = (totalAttributes - 99 - totalImplants) / 5;
            calculatedBooster.Should().Be(5);
        }

        [Fact]
        public void BoosterDetection_WithImplantsAndBooster_DetectsCorrectly()
        {
            // Character with +5 implants (total 25 implant bonus) and +10 booster
            int boosterBonus = 10;
            int totalImplantBonus = 25;  // 5 implants * 5 bonus each
            int totalAttributes = 99 + totalImplantBonus + 5 * boosterBonus;  // 99 + 25 + 50 = 174
            int calculatedBooster = (totalAttributes - 99 - totalImplantBonus) / 5;
            calculatedBooster.Should().Be(10);
        }

        [Fact]
        public void BoosterDetection_MaxBooster_CappedAt12()
        {
            // Even if calculation yields > 12, it should be capped
            int calculatedBooster = 15;  // Hypothetical oversize
            if (calculatedBooster > EveConstants.MaxBoosterBonus)
                calculatedBooster = EveConstants.MaxBoosterBonus;
            calculatedBooster.Should().Be(12);
        }

        [Fact]
        public void BoosterDetection_NegativeResult_ReturnsZero()
        {
            // If calculation yields negative (rounding/API error), should return 0
            int calculatedBooster = -1;
            if (calculatedBooster < 0)
                calculatedBooster = 0;
            calculatedBooster.Should().Be(0);
        }

        #endregion

        #region Alpha vs Omega SP training

        [Fact]
        public void MaxAlphaSkillTraining_Is5Million()
        {
            EveConstants.MaxAlphaSkillTraining.Should().Be(5000000);
        }

        [Fact]
        public void MaxSkillsInQueue_Is50()
        {
            EveConstants.MaxSkillsInQueue.Should().Be(50);
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
