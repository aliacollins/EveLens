using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Settings
{
    /// <summary>
    /// Tests for GitHub Issue #27: Custom ESI client credentials (SSOClientID, SSOClientSecret)
    /// must survive the full save → load → Import() round-trip in both XML and JSON formats.
    ///
    /// The old bug was: a translation layer (SerializableSettings → JsonConfig) dropped the SSO
    /// fields during save. The new direct JSON serialization path should fix this.
    /// </summary>
    public class SSOCredentialPersistenceTests
    {
        // Mirror the DirectJsonOptions from SettingsFileManager
        private static readonly JsonSerializerOptions DirectJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
            Converters = { new JsonStringEnumConverter() }
        };

        #region Direct JSON Serialization (New Path)

        [Fact]
        public void DirectJson_CustomSSOCredentials_SurviveRoundTrip()
        {
            // Arrange — simulate Export() writing custom credentials
            var settings = new SerializableSettings
            {
                SSOClientID = "my-custom-app-id-12345",
                SSOClientSecret = "my-custom-secret-abcdef"
            };

            // Act — serialize then deserialize (what SaveFromSerializableSettingsAsync → LoadToSerializableSettingsAsync does)
            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            // Assert — custom values survive the round-trip
            loaded.Should().NotBeNull();
            loaded!.SSOClientID.Should().Be("my-custom-app-id-12345");
            loaded.SSOClientSecret.Should().Be("my-custom-secret-abcdef");
        }

        [Fact]
        public void DirectJson_CustomSSOCredentials_PresentInJsonOutput()
        {
            // Arrange
            var settings = new SerializableSettings
            {
                SSOClientID = "user-custom-id",
                SSOClientSecret = "user-custom-secret"
            };

            // Act
            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);

            // Assert — the values must physically appear in the JSON file
            json.Should().Contain("user-custom-id");
            json.Should().Contain("user-custom-secret");
        }

        [Fact]
        public void DirectJson_EmptySSOCredentials_SurviveRoundTrip()
        {
            // Arrange — default empty strings (no custom credentials set)
            var settings = new SerializableSettings
            {
                SSOClientID = string.Empty,
                SSOClientSecret = string.Empty
            };

            // Act
            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            // Assert — empty strings survive (not null, not dropped)
            loaded.Should().NotBeNull();
            loaded!.SSOClientID.Should().BeEmpty();
            loaded.SSOClientSecret.Should().BeEmpty();
        }

        [Fact]
        public void DirectJson_DefaultConstructor_SSOAreEmptyStrings()
        {
            // Arrange & Act
            var settings = new SerializableSettings();

            // Assert — constructor initializes to empty strings
            settings.SSOClientID.Should().NotBeNull().And.BeEmpty();
            settings.SSOClientSecret.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void DirectJson_WhenWritingNull_DoesNotDropEmptyStrings()
        {
            // Arrange — verify WhenWritingNull doesn't affect empty strings
            var settings = new SerializableSettings
            {
                SSOClientID = "",
                SSOClientSecret = ""
            };

            // Act
            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);

            // Assert — empty strings MUST be in the JSON (not null, so not dropped)
            // The key names should appear in the output
            json.Should().Contain("SSOClientID", because: "empty strings are NOT null and should not be dropped by WhenWritingNull");
        }

        #endregion

        #region Full Export → Save → Load → Import Simulation

        [Fact]
        public void FullCycle_CustomCredentials_SurviveExportSaveLoadImport()
        {
            // This simulates the complete lifecycle:
            // 1. User sets custom credentials
            // 2. Settings.Export() captures them
            // 3. SettingsFileManager.SaveFromSerializableSettingsAsync() writes JSON
            // 4. SettingsFileManager.LoadToSerializableSettingsAsync() reads JSON
            // 5. Settings.Import() should restore the custom values

            // Step 1-2: Simulate Export() with custom credentials
            var exported = new SerializableSettings
            {
                SSOClientID = "my-private-app",
                SSOClientSecret = "my-secret-key"
            };

            // Step 3: Simulate save (serialize)
            string json = JsonSerializer.Serialize(exported, DirectJsonOptions);

            // Step 4: Simulate load (deserialize)
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            // Step 5: Simulate Import() logic
            // This is the critical check — Import() at Settings.cs:179-184
            string defaultClientID = "e87550c5642e4de0bac3b124d110ca7a";
            string resultClientID;

            if (!string.IsNullOrEmpty(loaded!.SSOClientID)
                && loaded.SSOClientID != defaultClientID)
                resultClientID = loaded.SSOClientID;
            else
                resultClientID = defaultClientID;

            // Assert — custom value must survive the full cycle
            resultClientID.Should().Be("my-private-app",
                because: "custom SSO credentials must persist through export → JSON → import");
        }

        [Fact]
        public void FullCycle_DefaultCredentials_FallBackToDefaults()
        {
            // When SSO credentials are empty (user hasn't set custom ones),
            // Import() should correctly fall back to defaults

            var exported = new SerializableSettings
            {
                SSOClientID = string.Empty,
                SSOClientSecret = string.Empty
            };

            string json = JsonSerializer.Serialize(exported, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            string defaultClientID = "e87550c5642e4de0bac3b124d110ca7a";
            string resultClientID;

            if (!string.IsNullOrEmpty(loaded!.SSOClientID)
                && loaded.SSOClientID != defaultClientID)
                resultClientID = loaded.SSOClientID;
            else
                resultClientID = defaultClientID;

            // Assert — empty strings should fall back to default
            resultClientID.Should().Be(defaultClientID,
                because: "empty SSO credentials should fall back to embedded defaults");
        }

        [Fact]
        public void FullCycle_DefaultCredentials_ExplicitlySet_AreIgnored()
        {
            // If the user hasn't changed from defaults, Import() should use defaults
            // (this is correct behavior — defaults should work with esi-credentials.json)

            string defaultClientID = "e87550c5642e4de0bac3b124d110ca7a";

            var exported = new SerializableSettings
            {
                SSOClientID = defaultClientID,
                SSOClientSecret = "eat_qpDb4LCQRKRcGWKNfoLhcrRlqQo75Aes_3fgYhF"
            };

            string json = JsonSerializer.Serialize(exported, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            string resultClientID;
            if (!string.IsNullOrEmpty(loaded!.SSOClientID)
                && loaded.SSOClientID != defaultClientID)
                resultClientID = loaded.SSOClientID;
            else
                resultClientID = defaultClientID;

            // Assert — default values should be treated as "not custom"
            resultClientID.Should().Be(defaultClientID);
        }

        #endregion

        #region XML → JSON Migration Path

        [Fact]
        public void XmlMigration_CustomCredentials_PreservedInJsonOutput()
        {
            // Simulate: XML backup with custom credentials → deserialize → save to JSON
            var xmlSettings = new SerializableSettings
            {
                SSOClientID = "xml-custom-app-id",
                SSOClientSecret = "xml-custom-secret"
            };

            // XML round-trip (simulates loading from XML backup)
            var afterXml = XmlRoundTrip(xmlSettings);

            // Save to JSON (simulates MigrateFromXmlAsync → SaveFromSerializableSettingsAsync)
            string json = JsonSerializer.Serialize(afterXml, DirectJsonOptions);

            // Load from JSON (simulates next startup)
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            // Assert — custom credentials survive XML → JSON migration
            loaded.Should().NotBeNull();
            loaded!.SSOClientID.Should().Be("xml-custom-app-id");
            loaded.SSOClientSecret.Should().Be("xml-custom-secret");
        }

        [Fact]
        public void XmlMigration_EmptyCredentials_StillEmptyInJson()
        {
            // XML backup without custom credentials
            var xmlSettings = new SerializableSettings
            {
                SSOClientID = "",
                SSOClientSecret = ""
            };

            var afterXml = XmlRoundTrip(xmlSettings);
            string json = JsonSerializer.Serialize(afterXml, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            loaded.Should().NotBeNull();
            loaded!.SSOClientID.Should().BeEmpty();
            loaded.SSOClientSecret.Should().BeEmpty();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void DirectJson_NullSSOFields_DroppedByWhenWritingNull()
        {
            // If somehow SSO fields are null, WhenWritingNull drops them.
            // On deserialization, the constructor default (empty string) should apply.
            var settings = new SerializableSettings();
            // Force null via reflection or direct assignment
            settings.SSOClientID = null!;
            settings.SSOClientSecret = null!;

            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            // Constructor defaults should apply
            loaded.Should().NotBeNull();
            // Null fields dropped from JSON → constructor defaults to empty string
            loaded!.SSOClientID.Should().BeEmpty();
            loaded.SSOClientSecret.Should().BeEmpty();
        }

        [Fact]
        public void DirectJson_WhitespaceOnlyCredentials_PreservedExactly()
        {
            // Edge case: user enters whitespace
            var settings = new SerializableSettings
            {
                SSOClientID = "  ",
                SSOClientSecret = "  "
            };

            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            loaded!.SSOClientID.Should().Be("  ");
            loaded.SSOClientSecret.Should().Be("  ");
        }

        [Fact]
        public void DirectJson_VeryLongCredentials_PreservedExactly()
        {
            // Edge case: very long credentials
            string longId = new string('x', 500);
            string longSecret = new string('y', 500);

            var settings = new SerializableSettings
            {
                SSOClientID = longId,
                SSOClientSecret = longSecret
            };

            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            loaded!.SSOClientID.Should().Be(longId);
            loaded.SSOClientSecret.Should().Be(longSecret);
        }

        [Fact]
        public void DirectJson_SpecialCharactersInCredentials_PreservedExactly()
        {
            // Edge case: special characters in credentials
            var settings = new SerializableSettings
            {
                SSOClientID = "app-id/with+special=chars&more",
                SSOClientSecret = "secret!@#$%^&*()_+-=[]{}|;':\",./<>?"
            };

            string json = JsonSerializer.Serialize(settings, DirectJsonOptions);
            var loaded = JsonSerializer.Deserialize<SerializableSettings>(json, DirectJsonOptions);

            loaded!.SSOClientID.Should().Be("app-id/with+special=chars&more");
            loaded.SSOClientSecret.Should().Be("secret!@#$%^&*()_+-=[]{}|;':\",./<>?");
        }

        #endregion

        #region Helpers

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }

        #endregion
    }
}
