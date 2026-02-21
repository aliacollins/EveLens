// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Settings
{
    /// <summary>
    /// Diagnostic and round-trip tests for direct JSON serialization of SerializableSettings.
    /// These tests verify that System.Text.Json can serialize/deserialize the same Serializable*
    /// classes that XmlSerializer uses, with zero translation layer.
    /// </summary>
    public class JsonDirectSerializationTests
    {
        /// <summary>
        /// The canonical JSON options for settings serialization.
        /// No camelCase — property names match C# names exactly.
        /// </summary>
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            Converters = { new JsonStringEnumConverter() }
        };

        #region Step 0: Diagnostic Round-Trip

        [Fact]
        public void RoundTrip_SerializableSettings_AllFieldsPreserved()
        {
            // Arrange: Create a fully-populated SerializableSettings
            var original = CreateFullyPopulatedSettings();

            // Act: JSON round-trip
            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            // Assert: Every field matches
            result.Should().NotBeNull();
            result!.Revision.Should().Be(original.Revision);
            result.ForkId.Should().Be(original.ForkId);
            result.ForkVersion.Should().Be(original.ForkVersion);
            result.SSOClientID.Should().Be(original.SSOClientID);
            result.SSOClientSecret.Should().Be(original.SSOClientSecret);
            result.Compatibility.Should().Be(original.Compatibility);

            // ESI Keys
            result.ESIKeys.Should().HaveCount(original.ESIKeys.Count);
            result.ESIKeys[0].ID.Should().Be(original.ESIKeys[0].ID);
            result.ESIKeys[0].RefreshToken.Should().Be(original.ESIKeys[0].RefreshToken);
            result.ESIKeys[0].AccessMask.Should().Be(original.ESIKeys[0].AccessMask);
            result.ESIKeys[0].Monitored.Should().Be(original.ESIKeys[0].Monitored);

            // Characters
            result.Characters.Should().HaveCount(original.Characters.Count);

            // Plans
            result.Plans.Should().HaveCount(original.Plans.Count);
            result.Plans[0].Name.Should().Be(original.Plans[0].Name);

            // Monitored characters
            result.MonitoredCharacters.Should().HaveCount(original.MonitoredCharacters.Count);
        }

        [Fact]
        public void RoundTrip_SerializableCCPCharacter_PolymorphicPreserved()
        {
            // Arrange
            var original = CreateFullyPopulatedSettings();

            // Act
            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            // Assert: Characters should deserialize as the correct derived types
            result!.Characters.Should().HaveCount(2);

            var ccpChar = result.Characters[0].Should().BeOfType<SerializableCCPCharacter>().Subject;
            ccpChar.Name.Should().Be("Test Pilot");
            ccpChar.ID.Should().Be(95000001);

            var uriChar = result.Characters[1].Should().BeOfType<SerializableUriCharacter>().Subject;
            uriChar.Name.Should().Be("Imported Char");
            uriChar.Address.Should().Be("https://example.com/char.xml");
        }

        [Fact]
        public void RoundTrip_SerializableCCPCharacter_SkillQueuePreserved()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var ccpChar = (SerializableCCPCharacter)result!.Characters[0];
            var origChar = (SerializableCCPCharacter)original.Characters[0];

            ccpChar.SkillQueue.Should().HaveCount(origChar.SkillQueue.Count);
            for (int i = 0; i < origChar.SkillQueue.Count; i++)
            {
                ccpChar.SkillQueue[i].ID.Should().Be(origChar.SkillQueue[i].ID);
                ccpChar.SkillQueue[i].Level.Should().Be(origChar.SkillQueue[i].Level);
                ccpChar.SkillQueue[i].StartSP.Should().Be(origChar.SkillQueue[i].StartSP);
                ccpChar.SkillQueue[i].EndSP.Should().Be(origChar.SkillQueue[i].EndSP);
                ccpChar.SkillQueue[i].StartTime.Should().BeCloseTo(origChar.SkillQueue[i].StartTime, TimeSpan.FromSeconds(1));
                ccpChar.SkillQueue[i].EndTime.Should().BeCloseTo(origChar.SkillQueue[i].EndTime, TimeSpan.FromSeconds(1));
            }
        }

        [Fact]
        public void RoundTrip_ImplantSets_NamesPreserved()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var origChar = (SerializableCCPCharacter)original.Characters[0];
            var resultChar = (SerializableCCPCharacter)result!.Characters[0];

            resultChar.ImplantSets.Should().NotBeNull();
            resultChar.ImplantSets!.ActiveClone.Name.Should().Be(origChar.ImplantSets!.ActiveClone.Name);
            resultChar.ImplantSets.ActiveClone.Intelligence.Should().Be(origChar.ImplantSets.ActiveClone.Intelligence);

            resultChar.ImplantSets.JumpClones.Should().HaveCount(origChar.ImplantSets.JumpClones.Count);
            resultChar.ImplantSets.JumpClones[0].Name.Should().Be("Clone in Jita");

            resultChar.ImplantSets.CustomSets.Should().HaveCount(origChar.ImplantSets.CustomSets.Count);
            resultChar.ImplantSets.CustomSets[0].Name.Should().Be("PvP Set");
        }

        [Fact]
        public void RoundTrip_Plans_EntriesAndRemappingPreserved()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            result!.Plans.Should().HaveCount(original.Plans.Count);
            var plan = result.Plans[0];
            var origPlan = original.Plans[0];

            plan.Name.Should().Be(origPlan.Name);
            plan.Description.Should().Be(origPlan.Description);
            plan.Owner.Should().Be(origPlan.Owner);

            plan.Entries.Should().HaveCount(origPlan.Entries.Count);
            plan.Entries[0].ID.Should().Be(origPlan.Entries[0].ID);
            plan.Entries[0].SkillName.Should().Be(origPlan.Entries[0].SkillName);
            plan.Entries[0].Level.Should().Be(origPlan.Entries[0].Level);
            plan.Entries[0].Type.Should().Be(origPlan.Entries[0].Type);
            plan.Entries[0].Priority.Should().Be(origPlan.Entries[0].Priority);
            plan.Entries[0].Notes.Should().Be(origPlan.Entries[0].Notes);
            plan.Entries[0].PlanGroups.Should().BeEquivalentTo(origPlan.Entries[0].PlanGroups);

            // Remapping point
            plan.Entries[1].Remapping.Should().NotBeNull();
            plan.Entries[1].Remapping!.Status.Should().Be(origPlan.Entries[1].Remapping!.Status);
            plan.Entries[1].Remapping.Perception.Should().Be(origPlan.Entries[1].Remapping.Perception);

            // Invalid entries
            plan.InvalidEntries.Should().HaveCount(origPlan.InvalidEntries.Count);
            plan.InvalidEntries[0].SkillName.Should().Be(origPlan.InvalidEntries[0].SkillName);
        }

        [Fact]
        public void RoundTrip_MarketOrders_PolymorphicPreserved()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var ccpChar = (SerializableCCPCharacter)result!.Characters[0];
            var origChar = (SerializableCCPCharacter)original.Characters[0];

            ccpChar.MarketOrders.Should().HaveCount(origChar.MarketOrders.Count);
            ccpChar.MarketOrders[0].Should().BeOfType<SerializableBuyOrder>();
            ccpChar.MarketOrders[0].OrderID.Should().Be(origChar.MarketOrders[0].OrderID);
            ccpChar.MarketOrders[1].Should().BeOfType<SerializableSellOrder>();
            ccpChar.MarketOrders[1].OrderID.Should().Be(origChar.MarketOrders[1].OrderID);
        }

        [Fact]
        public void RoundTrip_Contracts_AllFieldsPreserved()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var ccpChar = (SerializableCCPCharacter)result!.Characters[0];
            var origChar = (SerializableCCPCharacter)original.Characters[0];

            ccpChar.Contracts.Should().HaveCount(origChar.Contracts.Count);
            ccpChar.Contracts[0].ContractID.Should().Be(origChar.Contracts[0].ContractID);
            ccpChar.Contracts[0].ContractState.Should().Be(origChar.Contracts[0].ContractState);
            ccpChar.Contracts[0].ContractType.Should().Be(origChar.Contracts[0].ContractType);
        }

        [Fact]
        public void RoundTrip_IndustryJobs_AllFieldsPreserved()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var ccpChar = (SerializableCCPCharacter)result!.Characters[0];
            var origChar = (SerializableCCPCharacter)original.Characters[0];

            ccpChar.IndustryJobs.Should().HaveCount(origChar.IndustryJobs.Count);
            ccpChar.IndustryJobs[0].JobID.Should().Be(origChar.IndustryJobs[0].JobID);
            ccpChar.IndustryJobs[0].State.Should().Be(origChar.IndustryJobs[0].State);
        }

        [Fact]
        public void RoundTrip_CharacterBaseProperties_XmlIgnoreFieldsPreserved()
        {
            // These fields are [XmlIgnore] with proxy properties for XML.
            // JSON must serialize them directly.
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var origChar = original.Characters[0];
            var resultChar = result!.Characters[0];

            resultChar.Name.Should().Be(origChar.Name);
            resultChar.CorporationName.Should().Be(origChar.CorporationName);
            resultChar.AllianceName.Should().Be(origChar.AllianceName);
            resultChar.Birthday.Should().BeCloseTo(origChar.Birthday, TimeSpan.FromSeconds(1));
            resultChar.CloneJumpDate.Should().BeCloseTo(origChar.CloneJumpDate, TimeSpan.FromSeconds(1));
            resultChar.LastRespecDate.Should().BeCloseTo(origChar.LastRespecDate, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RoundTrip_Skills_PreservesAllFields()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var origChar = original.Characters[0];
            var resultChar = result!.Characters[0];

            resultChar.Skills.Should().HaveCount(origChar.Skills.Count);
            resultChar.Skills[0].ID.Should().Be(origChar.Skills[0].ID);
            resultChar.Skills[0].Name.Should().Be(origChar.Skills[0].Name);
            resultChar.Skills[0].Level.Should().Be(origChar.Skills[0].Level);
            resultChar.Skills[0].ActiveLevel.Should().Be(origChar.Skills[0].ActiveLevel);
            resultChar.Skills[0].Skillpoints.Should().Be(origChar.Skills[0].Skillpoints);
            resultChar.Skills[0].IsKnown.Should().Be(origChar.Skills[0].IsKnown);
            resultChar.Skills[0].OwnsBook.Should().Be(origChar.Skills[0].OwnsBook);
        }

        [Fact]
        public void RoundTrip_EmploymentHistory_PreservesAllFields()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var origChar = original.Characters[0];
            var resultChar = result!.Characters[0];

            resultChar.EmploymentHistory.Should().HaveCount(origChar.EmploymentHistory.Count);
            resultChar.EmploymentHistory[0].CorporationID.Should().Be(origChar.EmploymentHistory[0].CorporationID);
            resultChar.EmploymentHistory[0].CorporationName.Should().Be(origChar.EmploymentHistory[0].CorporationName);
            resultChar.EmploymentHistory[0].StartDate.Should().BeCloseTo(
                origChar.EmploymentHistory[0].StartDate, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void RoundTrip_Attributes_PreservesAllFields()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var origChar = original.Characters[0];
            var resultChar = result!.Characters[0];

            resultChar.Attributes.Intelligence.Should().Be(origChar.Attributes.Intelligence);
            resultChar.Attributes.Memory.Should().Be(origChar.Attributes.Memory);
            resultChar.Attributes.Perception.Should().Be(origChar.Attributes.Perception);
            resultChar.Attributes.Willpower.Should().Be(origChar.Attributes.Willpower);
            resultChar.Attributes.Charisma.Should().Be(origChar.Attributes.Charisma);
        }

        [Fact]
        public void RoundTrip_MonitoredCharacters_PreservesAllFields()
        {
            var original = CreateFullyPopulatedSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            result!.MonitoredCharacters.Should().HaveCount(original.MonitoredCharacters.Count);
            result.MonitoredCharacters[0].CharacterGuid.Should().Be(original.MonitoredCharacters[0].CharacterGuid);
            result.MonitoredCharacters[0].Name.Should().Be(original.MonitoredCharacters[0].Name);
        }

        [Fact]
        public void RoundTrip_DefaultSettings_PreservesStructure()
        {
            var original = new SerializableSettings();

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            result.Should().NotBeNull();
            result!.ESIKeys.Should().NotBeNull().And.BeEmpty();
            result.Characters.Should().NotBeNull().And.BeEmpty();
            result.Plans.Should().NotBeNull().And.BeEmpty();
            result.MonitoredCharacters.Should().NotBeNull().And.BeEmpty();
            result.UI.Should().NotBeNull();
            result.G15.Should().NotBeNull();
        }

        #endregion

        #region Additional Round-Trip Tests

        [Fact]
        public void XmlMigration_ToJson_AllFieldsPreserved()
        {
            // Simulate XML migration: create settings as if loaded from XML, then round-trip through JSON
            var original = CreateFullyPopulatedSettings();

            // XML would set NameXml instead of Name directly — verify both paths work
            var ccpChar = (SerializableCCPCharacter)original.Characters[0];
            ccpChar.NameXml = "Test Pilot via XML";  // Sets Name through the proxy

            string json = JsonSerializer.Serialize(original, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            // Name should be preserved via the [JsonInclude] property, not the [JsonIgnore] proxy
            result!.Characters[0].Name.Should().Be("Test Pilot via XML");
        }

        [Fact]
        public void XmlMigration_ImplantNames_NotLostInTranslation()
        {
            // The old translation layer renamed implant sets to "Custom" or "Jump Clone N".
            // Direct serialization preserves the original names.
            var settings = new SerializableSettings();
            var character = new SerializableCCPCharacter();
            character.ID = 1;
            character.Name = "Implant Test";
            character.ImplantSets = new SerializableImplantSetCollection
            {
                ActiveClone = new SerializableSettingsImplantSet { Name = "Active Clone" }
            };
            character.ImplantSets.JumpClones.Add(new SerializableSettingsImplantSet
            {
                Name = "Clone in Jita 4-4"  // Real location-based name from ESI
            });
            character.ImplantSets.JumpClones.Add(new SerializableSettingsImplantSet
            {
                Name = "Clone in Amarr"
            });
            character.ImplantSets.CustomSets.Add(new SerializableSettingsImplantSet
            {
                Name = "My PvP Set"
            });
            settings.Characters.Add(character);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var resultChar = (SerializableCCPCharacter)result!.Characters[0];
            resultChar.ImplantSets!.ActiveClone.Name.Should().Be("Active Clone");
            resultChar.ImplantSets.JumpClones[0].Name.Should().Be("Clone in Jita 4-4");
            resultChar.ImplantSets.JumpClones[1].Name.Should().Be("Clone in Amarr");
            resultChar.ImplantSets.CustomSets[0].Name.Should().Be("My PvP Set");
        }

        [Fact]
        public void RoundTrip_50SkillQueue_AllPreserved()
        {
            var settings = new SerializableSettings();
            var character = new SerializableCCPCharacter();
            character.ID = 1;
            character.Name = "Queue Test";

            for (int i = 0; i < 50; i++)
            {
                character.SkillQueue.Add(new SerializableQueuedSkill
                {
                    ID = 3300 + i,
                    Level = (i % 5) + 1,
                    StartSP = i * 25000,
                    EndSP = (i + 1) * 25000,
                    StartTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i),
                    EndTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(i + 1)
                });
            }
            settings.Characters.Add(character);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var resultChar = (SerializableCCPCharacter)result!.Characters[0];
            resultChar.SkillQueue.Should().HaveCount(50);
            for (int i = 0; i < 50; i++)
            {
                resultChar.SkillQueue[i].ID.Should().Be(3300 + i);
                resultChar.SkillQueue[i].Level.Should().Be((i % 5) + 1);
            }
        }

        [Fact]
        public void RoundTrip_50MarketOrders_PolymorphicPreserved()
        {
            var settings = new SerializableSettings();
            var character = new SerializableCCPCharacter();
            character.ID = 1;
            character.Name = "Market Test";

            for (int i = 0; i < 50; i++)
            {
                SerializableOrderBase order = i % 2 == 0
                    ? new SerializableBuyOrder()
                    : new SerializableSellOrder();
                order.OrderID = 5000000 + i;
                order.UnitaryPrice = 100.50m * (i + 1);
                order.RemainingVolume = 100 * (i + 1);
                character.MarketOrders.Add(order);
            }
            settings.Characters.Add(character);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var resultChar = (SerializableCCPCharacter)result!.Characters[0];
            resultChar.MarketOrders.Should().HaveCount(50);
            for (int i = 0; i < 50; i++)
            {
                if (i % 2 == 0)
                    resultChar.MarketOrders[i].Should().BeOfType<SerializableBuyOrder>();
                else
                    resultChar.MarketOrders[i].Should().BeOfType<SerializableSellOrder>();
                resultChar.MarketOrders[i].OrderID.Should().Be(5000000 + i);
            }
        }

        [Fact]
        public void RoundTrip_100PlanEntries_WithRemapping()
        {
            var settings = new SerializableSettings();
            var plan = new SerializablePlan
            {
                Name = "Big Plan",
                Owner = Guid.NewGuid()
            };

            for (int i = 0; i < 100; i++)
            {
                var entry = new SerializablePlanEntry
                {
                    ID = 3300 + i,
                    SkillName = $"Skill {i}",
                    Level = (i % 5) + 1,
                    Priority = (i % 3) + 1
                };

                if (i % 10 == 0)
                {
                    entry.Remapping = new SerializableRemappingPoint
                    {
                        Status = RemappingPointStatus.UpToDate,
                        Perception = 27,
                        Intelligence = 21
                    };
                }

                entry.PlanGroups.Add($"Group{i % 5}");
                plan.Entries.Add(entry);
            }
            settings.Plans.Add(plan);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            result!.Plans[0].Entries.Should().HaveCount(100);
            result.Plans[0].Entries[0].Remapping.Should().NotBeNull();
            result.Plans[0].Entries[1].Remapping.Should().BeNull();
            result.Plans[0].Entries[0].PlanGroups.Should().Contain("Group0");
        }

        [Fact]
        public void RoundTrip_LastUpdates_Preserved()
        {
            var settings = new SerializableSettings();
            var character = new SerializableCCPCharacter();
            character.ID = 1;
            character.Name = "Update Test";

            character.LastUpdates.Add(new SerializableAPIUpdate
            {
                Method = "CharacterSheet",
                Time = new DateTime(2024, 8, 1, 12, 0, 0, DateTimeKind.Utc)
            });
            character.LastUpdates.Add(new SerializableAPIUpdate
            {
                Method = "SkillQueue",
                Time = new DateTime(2024, 8, 1, 12, 5, 0, DateTimeKind.Utc)
            });
            settings.Characters.Add(character);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var resultChar = (SerializableCCPCharacter)result!.Characters[0];
            resultChar.LastUpdates.Should().HaveCount(2);
            resultChar.LastUpdates[0].Method.Should().Be("CharacterSheet");
            resultChar.LastUpdates[1].Method.Should().Be("SkillQueue");
        }

        [Fact]
        public void RoundTrip_UriCharacter_AddressPreserved()
        {
            var settings = new SerializableSettings();
            var uriChar = new SerializableUriCharacter
            {
                Address = "https://eveboard.com/char.xml"
            };
            uriChar.ID = 2;
            uriChar.Name = "Imported Character";
            settings.Characters.Add(uriChar);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            var resultChar = result!.Characters[0].Should().BeOfType<SerializableUriCharacter>().Subject;
            resultChar.Address.Should().Be("https://eveboard.com/char.xml");
            resultChar.Name.Should().Be("Imported Character");
        }

        [Fact]
        public void RoundTrip_BlankCharacter_NullAddress_PreservesAsUriCharacter()
        {
            var settings = new SerializableSettings();
            var uriChar = new SerializableUriCharacter();
            uriChar.Guid = Guid.NewGuid();
            uriChar.ID = 9999999;
            uriChar.Name = "Blank Pilot";
            uriChar.Race = "Amarr";
            // Address is null — this is a blank (local) character
            settings.Characters.Add(uriChar);

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            result!.Characters.Should().HaveCount(1);
            var resultChar = result.Characters[0].Should().BeOfType<SerializableUriCharacter>().Subject;
            resultChar.Name.Should().Be("Blank Pilot");
            resultChar.ID.Should().Be(9999999);
            resultChar.Address.Should().BeNull();
        }

        [Fact]
        public void RoundTrip_MixedCharacterTypes_OrderPreserved()
        {
            var settings = new SerializableSettings();

            // Add alternating CCP and URI characters
            for (int i = 0; i < 10; i++)
            {
                if (i % 3 == 0)
                {
                    var uri = new SerializableUriCharacter { Address = $"http://example.com/{i}" };
                    uri.ID = i;
                    uri.Name = $"URI Char {i}";
                    settings.Characters.Add(uri);
                }
                else
                {
                    var ccp = new SerializableCCPCharacter();
                    ccp.ID = i;
                    ccp.Name = $"CCP Char {i}";
                    settings.Characters.Add(ccp);
                }
            }

            string json = JsonSerializer.Serialize(settings, JsonOptions);
            var result = JsonSerializer.Deserialize<SerializableSettings>(json, JsonOptions);

            result!.Characters.Should().HaveCount(10);
            for (int i = 0; i < 10; i++)
            {
                result.Characters[i].ID.Should().Be(i);
                if (i % 3 == 0)
                    result.Characters[i].Should().BeOfType<SerializableUriCharacter>();
                else
                    result.Characters[i].Should().BeOfType<SerializableCCPCharacter>();
            }
        }

        #endregion

        #region Helper: Create Fully Populated Settings

        private static SerializableSettings CreateFullyPopulatedSettings()
        {
            var charGuid = Guid.NewGuid();
            var settings = new SerializableSettings
            {
                Revision = 42,
                ForkId = "aliacollins",
                ForkVersion = "5.2.0",
                SSOClientID = "test-client-id",
                SSOClientSecret = "test-secret",
                Compatibility = CompatibilityMode.Default
            };

            // ESI Keys
            settings.ESIKeys.Add(new SerializableESIKey
            {
                ID = 95000001,
                RefreshToken = "rt-token-abc",
                AccessMask = 4096,
                Monitored = true
            });

            // CCP Character
            var ccpChar = new SerializableCCPCharacter();
            ccpChar.Guid = charGuid;
            ccpChar.ID = 95000001;
            ccpChar.Name = "Test Pilot";
            ccpChar.Birthday = new DateTime(2020, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            ccpChar.Race = "Caldari";
            ccpChar.BloodLine = "Deteis";
            ccpChar.Ancestry = "Scientists";
            ccpChar.Gender = "Male";
            ccpChar.CorporationID = 98000001;
            ccpChar.CorporationName = "Test Corp";
            ccpChar.AllianceID = 99000001;
            ccpChar.AllianceName = "Test Alliance";
            ccpChar.FactionID = 0;
            ccpChar.FactionName = "";
            ccpChar.FreeSkillPoints = 500000;
            ccpChar.FreeRespecs = 2;
            ccpChar.Balance = 1234567.89m;
            ccpChar.HomeStationID = 60003760;
            ccpChar.ShipName = "My Raven";
            ccpChar.ShipTypeName = "Raven";
            ccpChar.SecurityStatus = 2.5;
            ccpChar.CloneState = "Omega";
            ccpChar.Label = "Main";
            ccpChar.CloneJumpDate = new DateTime(2024, 6, 1, 10, 0, 0, DateTimeKind.Utc);
            ccpChar.LastRespecDate = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ccpChar.LastTimedRespec = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            ccpChar.RemoteStationDate = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            ccpChar.JumpActivationDate = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            ccpChar.JumpFatigueDate = new DateTime(2024, 7, 1, 1, 0, 0, DateTimeKind.Utc);
            ccpChar.JumpLastUpdateDate = new DateTime(2024, 7, 1, 0, 30, 0, DateTimeKind.Utc);

            ccpChar.Attributes = new SerializableCharacterAttributes
            {
                Intelligence = 22,
                Memory = 20,
                Perception = 25,
                Willpower = 23,
                Charisma = 19
            };

            ccpChar.LastKnownLocation = new SerializableLocation
            {
                SolarSystemID = 30000142,
                StationID = 60003760
            };

            // Skills
            ccpChar.Skills.Add(new SerializableCharacterSkill
            {
                ID = 3386,
                Name = "Mining",
                Level = 5,
                ActiveLevel = 5,
                Skillpoints = 256000,
                IsKnown = true,
                OwnsBook = true
            });
            ccpChar.Skills.Add(new SerializableCharacterSkill
            {
                ID = 3300,
                Name = "Gunnery",
                Level = 4,
                ActiveLevel = 4,
                Skillpoints = 135765,
                IsKnown = true,
                OwnsBook = false
            });

            // Skill queue
            ccpChar.SkillQueue.Add(new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 5,
                StartSP = 135765,
                EndSP = 256000,
                StartTime = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc)
            });

            // Employment history
            ccpChar.EmploymentHistory.Add(new SerializableEmploymentHistory
            {
                CorporationID = 98000001,
                CorporationName = "Test Corp",
                StartDate = new DateTime(2020, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            // Market orders (polymorphic)
            ccpChar.MarketOrders.Add(new SerializableBuyOrder
            {
                OrderID = 5000001,
                State = OrderState.Active,
                UnitaryPrice = 100.50m,
                RemainingVolume = 500,
                Issued = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                IssuedFor = IssuedFor.Character
            });
            ccpChar.MarketOrders.Add(new SerializableSellOrder
            {
                OrderID = 5000002,
                State = OrderState.Expired,
                UnitaryPrice = 200.75m,
                RemainingVolume = 0,
                Issued = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                IssuedFor = IssuedFor.Corporation
            });

            // Contracts
            ccpChar.Contracts.Add(new SerializableContract
            {
                ContractID = 6000001,
                ContractState = ContractState.Created,
                ContractType = ContractType.ItemExchange,
                IssuedFor = IssuedFor.Character
            });

            // Industry jobs
            ccpChar.IndustryJobs.Add(new SerializableJob
            {
                JobID = 7000001,
                State = JobState.Active,
                StartDate = new DateTime(2024, 7, 10, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2024, 7, 11, 0, 0, 0, DateTimeKind.Utc),
                IssuedFor = IssuedFor.Character
            });

            // Last updates
            ccpChar.LastUpdates.Add(new SerializableAPIUpdate
            {
                Method = "CharacterSheet",
                Time = new DateTime(2024, 8, 1, 12, 0, 0, DateTimeKind.Utc)
            });

            // Implant sets
            ccpChar.ImplantSets = new SerializableImplantSetCollection
            {
                ActiveClone = new SerializableSettingsImplantSet
                {
                    Name = "Active Clone",
                    Intelligence = "Cybernetic Subprocessor - Standard",
                    Memory = "Memory Augmentation - Standard"
                },
                SelectedIndex = 0
            };
            ccpChar.ImplantSets.JumpClones.Add(new SerializableSettingsImplantSet
            {
                Name = "Clone in Jita",
                Intelligence = "Cybernetic Subprocessor - Improved"
            });
            ccpChar.ImplantSets.CustomSets.Add(new SerializableSettingsImplantSet
            {
                Name = "PvP Set",
                Perception = "Snake Alpha"
            });

            settings.Characters.Add(ccpChar);

            // URI Character
            var uriChar = new SerializableUriCharacter
            {
                Address = "https://example.com/char.xml"
            };
            uriChar.Guid = Guid.NewGuid();
            uriChar.ID = 95000002;
            uriChar.Name = "Imported Char";
            uriChar.Race = "Gallente";
            settings.Characters.Add(uriChar);

            // Plans
            var plan = new SerializablePlan
            {
                Name = "Training Plan Alpha",
                Description = "My main training plan",
                Owner = charGuid
            };

            var entry1 = new SerializablePlanEntry
            {
                ID = 3386,
                SkillName = "Mining",
                Level = 5,
                Type = PlanEntryType.Planned,
                Priority = 1,
                Notes = "Need for barges"
            };
            entry1.PlanGroups.Add("Industrial");
            entry1.PlanGroups.Add("Mining");
            plan.Entries.Add(entry1);

            var entry2 = new SerializablePlanEntry
            {
                ID = 3300,
                SkillName = "Gunnery",
                Level = 5,
                Type = PlanEntryType.Planned,
                Priority = 2,
                Remapping = new SerializableRemappingPoint
                {
                    Status = RemappingPointStatus.UpToDate,
                    Perception = 27,
                    Intelligence = 21,
                    Memory = 17,
                    Willpower = 21,
                    Charisma = 17,
                    Description = "Gunnery remap"
                }
            };
            plan.Entries.Add(entry2);

            plan.InvalidEntries.Add(new SerializableInvalidPlanEntry
            {
                SkillName = "Obsolete Skill",
                PlannedLevel = 3,
                Acknowledged = false
            });

            settings.Plans.Add(plan);

            // Monitored characters
            settings.MonitoredCharacters.Add(new MonitoredCharacterSettings
            {
                CharacterGuid = charGuid,
                Name = "Test Pilot"
            });

            return settings;
        }

        #endregion
    }
}
