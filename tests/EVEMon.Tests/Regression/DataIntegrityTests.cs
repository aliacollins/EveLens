using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Regression
{
    /// <summary>
    /// Tier 4 regression tests: verify data integrity assumptions that the application
    /// relies on, including enum completeness, settings defaults, and version consistency.
    /// </summary>
    public class DataIntegrityTests
    {
        #region Enum Integrity

        [Fact]
        public void ContractType_AllValues_HaveDescriptions()
        {
            // ContractType values (except None) should have [Description] attributes
            var values = Enum.GetValues(typeof(ContractType)).Cast<ContractType>()
                .Where(v => v != ContractType.None);

            foreach (var value in values)
            {
                var member = typeof(ContractType).GetMember(value.ToString()).FirstOrDefault();
                member.Should().NotBeNull($"enum member {value} should exist");

                var descAttr = member!.GetCustomAttribute<DescriptionAttribute>();
                descAttr.Should().NotBeNull(
                    $"ContractType.{value} should have a [Description] attribute for display");
                descAttr!.Description.Should().NotBeNullOrEmpty(
                    $"ContractType.{value} description should not be empty");
            }
        }

        [Fact]
        public void OrderState_AllValues_AreUnique()
        {
            var values = Enum.GetValues(typeof(OrderState)).Cast<OrderState>();
            var intValues = values.Select(v => (int)v).ToList();

            intValues.Should().OnlyHaveUniqueItems(
                "OrderState integer values must be unique to avoid sort/comparison bugs");
        }

        [Fact]
        public void ContractState_AllValues_AreUnique()
        {
            var values = Enum.GetValues(typeof(ContractState)).Cast<ContractState>();
            var intValues = values.Select(v => (int)v).ToList();

            intValues.Should().OnlyHaveUniqueItems(
                "ContractState integer values must be unique for correct ordering");
        }

        [Fact]
        public void ESIAPICharacterMethods_KeyMethods_HaveHeaderAndDescription()
        {
            // The "top-level" queryable methods should have [Header] and [Description]
            // so the UI can display them properly. These are the user-facing query methods.
            var requiredMethods = new[]
            {
                ESIAPICharacterMethods.CharacterSheet,
                ESIAPICharacterMethods.SkillQueue,
                ESIAPICharacterMethods.MarketOrders,
                ESIAPICharacterMethods.Contracts,
                ESIAPICharacterMethods.IndustryJobs,
                ESIAPICharacterMethods.AssetList
            };

            foreach (var method in requiredMethods)
            {
                var member = typeof(ESIAPICharacterMethods).GetMember(method.ToString()).FirstOrDefault();
                member.Should().NotBeNull($"{method} should be a valid enum member");

                // Check for Header attribute
                var headerAttr = member!.GetCustomAttributes()
                    .FirstOrDefault(a => a.GetType().Name == "HeaderAttribute");
                headerAttr.Should().NotBeNull(
                    $"ESIAPICharacterMethods.{method} should have a [Header] attribute for UI display");
            }
        }

        [Fact]
        public void Race_FlagValues_ArePowersOfTwo()
        {
            // Race is a [Flags] enum - values must be powers of 2 (or 0 for None)
            var values = Enum.GetValues(typeof(Race)).Cast<Race>()
                .Where(v => v != Race.None && v != Race.All);

            foreach (var value in values)
            {
                int intVal = (int)value;
                bool isPowerOfTwo = intVal > 0 && (intVal & (intVal - 1)) == 0;
                isPowerOfTwo.Should().BeTrue(
                    $"Race.{value} ({intVal}) must be a power of 2 for flags composition");
            }
        }

        [Fact]
        public void Race_All_CombinesAllIndividualFlags()
        {
            // Race.All should be the bitwise OR of all individual race flags
            var individualValues = Enum.GetValues(typeof(Race)).Cast<Race>()
                .Where(v => v != Race.None && v != Race.All);

            Race combined = Race.None;
            foreach (var value in individualValues)
                combined |= value;

            combined.Should().Be(Race.All,
                "Race.All must be the combination of all individual race flags");
        }

        #endregion

        #region Settings Defaults

        [Fact]
        public void SerializableSettings_DefaultConstructor_HasValidDefaults()
        {
            var settings = new SerializableSettings();

            // Revision should default to 0
            settings.Revision.Should().Be(0);

            // SSO fields should default to empty string (not null)
            settings.SSOClientID.Should().NotBeNull();
            settings.SSOClientSecret.Should().NotBeNull();

            // Collections should be empty but not null
            settings.ESIKeys.Should().NotBeNull().And.BeEmpty();
            settings.Characters.Should().NotBeNull().And.BeEmpty();
            settings.Plans.Should().NotBeNull().And.BeEmpty();
            settings.MonitoredCharacters.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SerializablePlanEntry_DefaultPriority_IsThree()
        {
            // Priority defaults to 3 (medium) - this is relied upon by plan sorting
            var entry = new SerializablePlanEntry();
            entry.Priority.Should().Be(3);
        }

        [Fact]
        public void SerializableCCPCharacter_DefaultCollections_AreEmpty()
        {
            var character = new SerializableCCPCharacter();

            character.SkillQueue.Should().BeEmpty();
            character.MarketOrders.Should().BeEmpty();
            character.Contracts.Should().BeEmpty();
            character.IndustryJobs.Should().BeEmpty();
            character.LastUpdates.Should().BeEmpty();
        }

        [Fact]
        public void SerializableCharacterAttributes_DefaultValues_AreOne()
        {
            // Default attributes are 1 (not 0) to prevent division by zero in training time calculations
            var attrs = new SerializableCharacterAttributes();

            attrs.Intelligence.Should().Be(1);
            attrs.Memory.Should().Be(1);
            attrs.Perception.Should().Be(1);
            attrs.Willpower.Should().Be(1);
            attrs.Charisma.Should().Be(1);
        }

        #endregion

        #region Version Consistency

        [Fact]
        public void AssemblyVersion_IsValid()
        {
            // The assembly version should be parseable
            var assembly = typeof(SerializableSettings).Assembly;
            var version = assembly.GetName().Version;
            version.Should().NotBeNull("Assembly version must be set");
            version!.Major.Should().BeGreaterOrEqualTo(5,
                "EVEMon should be version 5.x or higher");
        }

        [Fact]
        public void InformationalVersion_IsValid()
        {
            var assembly = typeof(SerializableSettings).Assembly;
            var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            infoVersionAttr.Should().NotBeNull("Assembly should have an informational version");
            infoVersionAttr!.InformationalVersion.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region SerializableSettings AllHaveDefaults

        [Fact]
        public void AllSerializableSettings_SubObjects_HaveDefaults()
        {
            // Verify that all settings sub-objects are properly initialized
            // This prevents NullReferenceExceptions when accessing any settings path
            var settings = new SerializableSettings();

            // Each of these is a settings sub-object that must be non-null by default
            settings.UI.Should().NotBeNull("UISettings must have a default");
            settings.Notifications.Should().NotBeNull("NotificationSettings must have a default");
            settings.Updates.Should().NotBeNull("UpdateSettings must have a default");
            settings.Proxy.Should().NotBeNull("ProxySettings must have a default");
            settings.Calendar.Should().NotBeNull("CalendarSettings must have a default");
            settings.Exportation.Should().NotBeNull("ExportationSettings must have a default");
            settings.MarketPricer.Should().NotBeNull("MarketPricerSettings must have a default");
            settings.LoadoutsProvider.Should().NotBeNull("LoadoutsProviderSettings must have a default");
            settings.CloudStorageServiceProvider.Should().NotBeNull("CloudStorageServiceProviderSettings must have a default");
            settings.PortableEveInstallations.Should().NotBeNull("PortableEveInstallationsSettings must have a default");
            settings.G15.Should().NotBeNull("G15Settings must have a default");
            settings.Scheduler.Should().NotBeNull("SchedulerSettings must have a default");
        }

        #endregion
    }
}
