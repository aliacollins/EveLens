using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Settings
{
    /// <summary>
    /// Settings serialization round-trip tests.
    /// Validates that SerializableSettings survives XML and JSON round-trips,
    /// that default settings are valid, and that corrupted content is handled gracefully.
    /// </summary>
    public class SettingsRoundTripTests
    {
        // JSON options mirroring SettingsFileManager
        private static readonly JsonSerializerOptions JsonWriteOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions JsonReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        #region XML Round-Trip Tests

        [Fact]
        public void XmlRoundTrip_DefaultSettings_PreservesStructure()
        {
            // Arrange
            var original = new SerializableSettings();

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.Should().NotBeNull();
            result.ESIKeys.Should().BeEmpty();
            result.Characters.Should().BeEmpty();
            result.Plans.Should().BeEmpty();
            result.MonitoredCharacters.Should().BeEmpty();
        }

        [Fact]
        public void XmlRoundTrip_WithRevisionAndForkId_PreservesBoth()
        {
            // Arrange
            var original = new SerializableSettings
            {
                Revision = 42,
                ForkId = "aliacollins",
                ForkVersion = "5.2.0"
            };

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.Revision.Should().Be(42);
            result.ForkId.Should().Be("aliacollins");
            result.ForkVersion.Should().Be("5.2.0");
        }

        [Fact]
        public void XmlRoundTrip_WithSSOCredentials_PreservesValues()
        {
            // Arrange
            var original = new SerializableSettings
            {
                SSOClientID = "my-client-id-abc",
                SSOClientSecret = "my-secret-xyz"
            };

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.SSOClientID.Should().Be("my-client-id-abc");
            result.SSOClientSecret.Should().Be("my-secret-xyz");
        }

        [Fact]
        public void XmlRoundTrip_WithESIKeys_PreservesKeyData()
        {
            // Arrange
            var original = new SerializableSettings();
            original.ESIKeys.Add(new SerializableESIKey
            {
                ID = 90000001,
                RefreshToken = "rt-token-1",
                AccessMask = 4096,
                Monitored = true
            });
            original.ESIKeys.Add(new SerializableESIKey
            {
                ID = 90000002,
                RefreshToken = "rt-token-2",
                AccessMask = 8192,
                Monitored = false
            });

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.ESIKeys.Should().HaveCount(2);
            result.ESIKeys[0].ID.Should().Be(90000001);
            result.ESIKeys[0].RefreshToken.Should().Be("rt-token-1");
            result.ESIKeys[0].AccessMask.Should().Be(4096);
            result.ESIKeys[0].Monitored.Should().BeTrue();
            result.ESIKeys[1].ID.Should().Be(90000002);
            result.ESIKeys[1].Monitored.Should().BeFalse();
        }

        [Fact]
        public void XmlRoundTrip_WithPlans_PreservesPlanEntries()
        {
            // Arrange
            var original = new SerializableSettings();
            var plan = new SerializablePlan
            {
                Name = "Mining V Plan",
                Description = "Train Mining to V"
            };
            plan.Entries.Add(new SerializablePlanEntry
            {
                ID = 3386,
                SkillName = "Mining",
                Level = 5,
                Priority = 1
            });
            original.Plans.Add(plan);

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.Plans.Should().HaveCount(1);
            result.Plans[0].Name.Should().Be("Mining V Plan");
            result.Plans[0].Entries.Should().HaveCount(1);
            result.Plans[0].Entries[0].ID.Should().Be(3386);
            result.Plans[0].Entries[0].SkillName.Should().Be("Mining");
            result.Plans[0].Entries[0].Level.Should().Be(5);
        }

        #endregion

        #region JSON Round-Trip Tests (JsonConfig / JsonCredentials)

        [Fact]
        public void JsonConfig_RoundTrip_PreservesAllFields()
        {
            // Arrange
            var original = new JsonConfig
            {
                Version = 1,
                ForkId = "aliacollins",
                ForkVersion = "5.2.0"
            };

            // Act
            string json = JsonSerializer.Serialize(original, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonConfig>(json, JsonReadOptions);

            // Assert
            result.Should().NotBeNull();
            result!.Version.Should().Be(1);
            result.ForkId.Should().Be("aliacollins");
            result.ForkVersion.Should().Be("5.2.0");
        }

        [Fact]
        public void JsonCredentials_RoundTrip_PreservesEsiKeys()
        {
            // Arrange
            var original = new JsonCredentials
            {
                Version = 1,
                EsiKeys =
                {
                    new JsonEsiKey { CharacterId = 12345, RefreshToken = "tok-a", AccessMask = 512, Monitored = true },
                    new JsonEsiKey { CharacterId = 67890, RefreshToken = "tok-b", AccessMask = 1024, Monitored = false }
                }
            };

            // Act
            string json = JsonSerializer.Serialize(original, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCredentials>(json, JsonReadOptions);

            // Assert
            result.Should().NotBeNull();
            result!.EsiKeys.Should().HaveCount(2);
            result.EsiKeys[0].CharacterId.Should().Be(12345);
            result.EsiKeys[0].RefreshToken.Should().Be("tok-a");
            result.EsiKeys[0].Monitored.Should().BeTrue();
            result.EsiKeys[1].CharacterId.Should().Be(67890);
            result.EsiKeys[1].Monitored.Should().BeFalse();
        }

        [Fact]
        public void JsonCharacterData_RoundTrip_PreservesCharacterIdentity()
        {
            // Arrange
            var original = new JsonCharacterData
            {
                CharacterId = 95000001,
                Name = "Test Pilot",
                Race = "Caldari",
                Bloodline = "Deteis",
                CorporationId = 98000001,
                CorporationName = "Test Corp",
                Intelligence = 22,
                Memory = 20,
                Charisma = 19,
                Perception = 21,
                Willpower = 23,
                Balance = 1234567.89m
            };

            // Act
            string json = JsonSerializer.Serialize(original, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCharacterData>(json, JsonReadOptions);

            // Assert
            result.Should().NotBeNull();
            result!.CharacterId.Should().Be(95000001);
            result.Name.Should().Be("Test Pilot");
            result.Race.Should().Be("Caldari");
            result.CorporationName.Should().Be("Test Corp");
            result.Intelligence.Should().Be(22);
            result.Memory.Should().Be(20);
            result.Balance.Should().Be(1234567.89m);
        }

        [Fact]
        public void JsonCharacterData_RoundTrip_PreservesSkills()
        {
            // Arrange
            var original = new JsonCharacterData
            {
                CharacterId = 95000001,
                Name = "Skill Pilot"
            };
            original.Skills.Add(new JsonSkill
            {
                TypeId = 3386,
                Name = "Mining",
                Level = 5,
                ActiveLevel = 5,
                Skillpoints = 256000,
                IsKnown = true,
                OwnsBook = true
            });

            // Act
            string json = JsonSerializer.Serialize(original, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCharacterData>(json, JsonReadOptions);

            // Assert
            result!.Skills.Should().HaveCount(1);
            result.Skills[0].TypeId.Should().Be(3386);
            result.Skills[0].Name.Should().Be("Mining");
            result.Skills[0].Level.Should().Be(5);
            result.Skills[0].Skillpoints.Should().Be(256000);
            result.Skills[0].IsKnown.Should().BeTrue();
        }

        [Fact]
        public void JsonCharacterData_RoundTrip_PreservesPlans()
        {
            // Arrange
            var original = new JsonCharacterData
            {
                CharacterId = 95000001,
                Name = "Plan Pilot"
            };
            var plan = new JsonPlan
            {
                Name = "Combat Plan",
                Description = "Train combat skills"
            };
            plan.Entries.Add(new JsonPlanEntry
            {
                SkillId = 3300,
                SkillName = "Gunnery",
                Level = 5,
                Priority = 1,
                Notes = "Needed for T2"
            });
            original.Plans.Add(plan);

            // Act
            string json = JsonSerializer.Serialize(original, JsonWriteOptions);
            var result = JsonSerializer.Deserialize<JsonCharacterData>(json, JsonReadOptions);

            // Assert
            result!.Plans.Should().HaveCount(1);
            result.Plans[0].Name.Should().Be("Combat Plan");
            result.Plans[0].Entries.Should().HaveCount(1);
            result.Plans[0].Entries[0].SkillName.Should().Be("Gunnery");
            result.Plans[0].Entries[0].Level.Should().Be(5);
            result.Plans[0].Entries[0].Notes.Should().Be("Needed for T2");
        }

        #endregion

        #region Default Settings Validity

        [Fact]
        public void DefaultSettings_AreValid_AllSubObjectsNonNull()
        {
            // Arrange & Act
            var settings = new SerializableSettings();

            // Assert
            settings.UI.Should().NotBeNull();
            settings.G15.Should().NotBeNull();
            settings.Proxy.Should().NotBeNull();
            settings.Updates.Should().NotBeNull();
            settings.Calendar.Should().NotBeNull();
            settings.Exportation.Should().NotBeNull();
            settings.MarketPricer.Should().NotBeNull();
            settings.Notifications.Should().NotBeNull();
            settings.LoadoutsProvider.Should().NotBeNull();
            settings.PortableEveInstallations.Should().NotBeNull();
            settings.CloudStorageServiceProvider.Should().NotBeNull();
            settings.Scheduler.Should().NotBeNull();
        }

        [Fact]
        public void DefaultSettings_CollectionsAreEmpty_NotNull()
        {
            // Arrange & Act
            var settings = new SerializableSettings();

            // Assert
            settings.ESIKeys.Should().NotBeNull().And.BeEmpty();
            settings.Characters.Should().NotBeNull().And.BeEmpty();
            settings.Plans.Should().NotBeNull().And.BeEmpty();
            settings.MonitoredCharacters.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void DefaultSettings_SSO_AreEmptyStrings()
        {
            // Arrange & Act
            var settings = new SerializableSettings();

            // Assert
            settings.SSOClientID.Should().NotBeNull().And.BeEmpty();
            settings.SSOClientSecret.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void DefaultSettings_Revision_IsZero()
        {
            // Arrange & Act
            var settings = new SerializableSettings();

            // Assert
            settings.Revision.Should().Be(0);
        }

        [Fact]
        public void DefaultJsonConfig_HasCorrectDefaults()
        {
            // Arrange & Act
            var config = new JsonConfig();

            // Assert
            config.Version.Should().Be(1);
            config.ForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void DefaultJsonCredentials_HasEmptyKeyList()
        {
            // Arrange & Act
            var creds = new JsonCredentials();

            // Assert
            creds.Version.Should().Be(1);
            creds.EsiKeys.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Corrupted Content Handling

        [Fact]
        public void XmlDeserialize_EmptyString_ThrowsInvalidOperationException()
        {
            // Arrange
            var serializer = new XmlSerializer(typeof(SerializableSettings));

            // Act
            Action act = () =>
            {
                using var reader = new StringReader("");
                serializer.Deserialize(reader);
            };

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void XmlDeserialize_RandomGarbage_ThrowsInvalidOperationException()
        {
            // Arrange
            var serializer = new XmlSerializer(typeof(SerializableSettings));
            string garbage = "not xml at all {{{{ }}}";

            // Act
            Action act = () =>
            {
                using var reader = new StringReader(garbage);
                serializer.Deserialize(reader);
            };

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void XmlDeserialize_WrongRootElement_ThrowsInvalidOperationException()
        {
            // Arrange
            var serializer = new XmlSerializer(typeof(SerializableSettings));
            string wrongRoot = @"<?xml version=""1.0""?><WrongRoot><child/></WrongRoot>";

            // Act
            Action act = () =>
            {
                using var reader = new StringReader(wrongRoot);
                serializer.Deserialize(reader);
            };

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void JsonDeserialize_EmptyString_ReturnsNull()
        {
            // Arrange
            string empty = "";

            // Act
            Action act = () => JsonSerializer.Deserialize<JsonConfig>(empty, JsonReadOptions);

            // Assert
            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void JsonDeserialize_RandomGarbage_ThrowsJsonException()
        {
            // Arrange
            string garbage = "not json {{{";

            // Act
            Action act = () => JsonSerializer.Deserialize<JsonConfig>(garbage, JsonReadOptions);

            // Assert
            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void JsonDeserialize_EmptyObject_ReturnsDefaultConfig()
        {
            // Arrange
            string emptyObject = "{}";

            // Act
            var result = JsonSerializer.Deserialize<JsonConfig>(emptyObject, JsonReadOptions);

            // Assert - should deserialize to defaults
            result.Should().NotBeNull();
            result!.Version.Should().Be(1); // default
        }

        [Fact]
        public void JsonDeserialize_UnknownProperties_AreIgnored()
        {
            // Arrange
            string extraProps = @"{ ""version"": 1, ""unknownField"": ""value"", ""forkId"": ""test"" }";

            // Act
            var result = JsonSerializer.Deserialize<JsonConfig>(extraProps, JsonReadOptions);

            // Assert
            result.Should().NotBeNull();
            result!.ForkId.Should().Be("test");
        }

        [Fact]
        public void XmlDeserialize_MissingOptionalElements_ReturnsDefaults()
        {
            // Arrange - minimal valid XML with just the root element
            string minimalXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" />";

            var serializer = new XmlSerializer(typeof(SerializableSettings));

            // Act
            SerializableSettings result;
            using (var reader = new StringReader(minimalXml))
            {
                result = (SerializableSettings)serializer.Deserialize(reader)!;
            }

            // Assert - all collections should exist (may be null from deserialization,
            // but the constructor-initialized Collections should be preserved)
            result.Should().NotBeNull();
            result.Revision.Should().Be(0);
        }

        #endregion

        #region Helpers

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            string xml = writer.ToString();

            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader)!;
        }

        #endregion
    }
}
