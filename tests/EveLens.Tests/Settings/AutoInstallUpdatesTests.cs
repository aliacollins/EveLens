// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// Tests for the auto-update opt-in setting (Discussion #100). Transparency contract:
    /// the setting starts as NotAsked (the user is asked once at startup, never spammed),
    /// and the stored choice round-trips so the ask never repeats.
    /// </summary>
    public class AutoInstallUpdatesTests
    {
        [Fact]
        public void Default_IsNotAsked()
        {
            new UpdateSettings().AutoInstallUpdates.Should().Be(AutoInstallUpdates.NotAsked,
                "the user must be asked on first launch, not silently defaulted");
        }

        [Theory]
        [InlineData(AutoInstallUpdates.NotAsked)]
        [InlineData(AutoInstallUpdates.Automatic)]
        [InlineData(AutoInstallUpdates.NotifyOnly)]
        public void AutoInstallUpdates_RoundTrips_Xml(AutoInstallUpdates choice)
        {
            var settings = new UpdateSettings { AutoInstallUpdates = choice };

            var serializer = new XmlSerializer(typeof(UpdateSettings));
            using var writer = new StringWriter();
            serializer.Serialize(writer, settings);
            using var reader = new StringReader(writer.ToString());
            var result = (UpdateSettings)serializer.Deserialize(reader)!;

            result.AutoInstallUpdates.Should().Be(choice,
                "losing the stored answer would re-ask the user (spam)");
        }

        [Theory]
        [InlineData(AutoInstallUpdates.Automatic)]
        [InlineData(AutoInstallUpdates.NotifyOnly)]
        public void AutoInstallUpdates_RoundTrips_Json(AutoInstallUpdates choice)
        {
            var settings = new UpdateSettings { AutoInstallUpdates = choice };
            string json = JsonSerializer.Serialize(settings, JsonDirectSerializationTests.JsonOptions);
            var result = JsonSerializer.Deserialize<UpdateSettings>(json, JsonDirectSerializationTests.JsonOptions);
            result!.AutoInstallUpdates.Should().Be(choice);
        }

        [Fact]
        public void LegacySettingsWithoutElement_DeserializeToNotAsked()
        {
            // Settings files saved before this feature have no autoInstallUpdates element —
            // those users get the one-time ask, exactly like a fresh install.
            const string legacyXml = "<UpdateSettings><checkEveLensVersion>true</checkEveLensVersion></UpdateSettings>";
            var serializer = new XmlSerializer(typeof(UpdateSettings));
            using var reader = new StringReader(legacyXml);
            var result = (UpdateSettings)serializer.Deserialize(reader)!;

            result.AutoInstallUpdates.Should().Be(AutoInstallUpdates.NotAsked);
        }
    }
}
