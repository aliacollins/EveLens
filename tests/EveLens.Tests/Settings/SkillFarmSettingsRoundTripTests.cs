// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// The skill farm account model (issue #124) is persisted state: the per-account
    /// divisor and the per-character account labels must survive a settings
    /// round-trip, and old files without them must load with the correct defaults.
    /// </summary>
    public class SkillFarmSettingsRoundTripTests
    {
        [Fact]
        public void AccountModel_SurvivesXmlRoundTrip()
        {
            var settings = new SkillFarmSettings { CharactersPerAccount = 2 };
            settings.FarmCharacters.Add(new SkillFarmCharacterSettings
            {
                CharacterGuid = Guid.NewGuid(),
                ExtractionThreshold = 5_500_000,
                AccountLabel = "Farm Acct A"
            });

            var result = XmlRoundTrip(settings);

            result.CharactersPerAccount.Should().Be(2);
            result.FarmCharacters.Should().HaveCount(1);
            result.FarmCharacters[0].AccountLabel.Should().Be("Farm Acct A");
        }

        [Fact]
        public void OldFiles_DefaultToThreePerAccount_AndNoLabel()
        {
            // Pre-#124 settings carry neither element; loading them must produce the
            // real-world default (3 characters per Omega account) and empty labels.
            const string oldXml =
                "<SkillFarmSettings><defaultThreshold>5000000</defaultThreshold>" +
                "<farmCharacters><character guid=\"9f0e97fb-3a67-40f0-a3b5-111111111111\">" +
                "<threshold>5000000</threshold></character></farmCharacters>" +
                "</SkillFarmSettings>";

            var serializer = new XmlSerializer(typeof(SkillFarmSettings));
            using var reader = new StringReader(oldXml);
            var result = (SkillFarmSettings)serializer.Deserialize(reader)!;

            result.CharactersPerAccount.Should().Be(3);
            result.FarmCharacters[0].AccountLabel.Should().BeEmpty();
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
