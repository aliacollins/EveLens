using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Settings
{
    public class EnabledEndpointsRoundTripTests
    {
        [Fact]
        public void CharacterUISettings_EnabledEndpoints_RoundTrips_Xml()
        {
            var settings = new CharacterUISettings();
            settings.EnabledEndpoints.Add("AssetList");
            settings.EnabledEndpoints.Add("MarketOrders");
            settings.EnabledEndpoints.Add("Contracts");

            var result = XmlRoundTrip(settings);

            result.EnabledEndpoints.Should().HaveCount(3);
            result.EnabledEndpoints.Should().Contain("AssetList");
            result.EnabledEndpoints.Should().Contain("MarketOrders");
            result.EnabledEndpoints.Should().Contain("Contracts");
        }

        [Fact]
        public void CharacterUISettings_EnabledEndpoints_Empty_By_Default()
        {
            var settings = new CharacterUISettings();
            settings.EnabledEndpoints.Should().BeEmpty();
        }

        [Fact]
        public void CharacterUISettings_EnabledEndpoints_RoundTrips_Empty()
        {
            var settings = new CharacterUISettings();

            var result = XmlRoundTrip(settings);

            result.EnabledEndpoints.Should().BeEmpty();
        }

        [Fact]
        public void CharacterUISettings_EnabledEndpoints_Setter_Replaces_Collection()
        {
            var settings = new CharacterUISettings();
            settings.EnabledEndpoints.Add("AssetList");

            var newEndpoints = new System.Collections.ObjectModel.Collection<string>
            {
                "MarketOrders",
                "WalletJournal"
            };
            settings.EnabledEndpoints = newEndpoints;

            settings.EnabledEndpoints.Should().HaveCount(2);
            settings.EnabledEndpoints.Should().Contain("MarketOrders");
            settings.EnabledEndpoints.Should().Contain("WalletJournal");
            settings.EnabledEndpoints.Should().NotContain("AssetList");
        }

        [Fact]
        public void MonitoredCharacterSettings_Preserves_EnabledEndpoints()
        {
            var charSettings = new CharacterUISettings();
            charSettings.EnabledEndpoints.Add("KillLog");
            charSettings.EnabledEndpoints.Add("Notifications");

            var monitored = new MonitoredCharacterSettings
            {
                CharacterGuid = System.Guid.NewGuid(),
                Name = "Test Pilot",
                Settings = charSettings,
            };

            var result = XmlRoundTrip(monitored);

            result.Settings.EnabledEndpoints.Should().HaveCount(2);
            result.Settings.EnabledEndpoints.Should().Contain("KillLog");
            result.Settings.EnabledEndpoints.Should().Contain("Notifications");
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
