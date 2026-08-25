// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Xml.Serialization;
using EveLens.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// The plan's optimized-for clone marker is persisted state (Law 13): it must
    /// survive the XML round-trip, and plans saved before it existed must load
    /// with it unset.
    /// </summary>
    public class PlanOptimizedForRoundTripTests
    {
        [Fact]
        public void OptimizedFor_SurvivesRoundTrip()
        {
            var plan = new SerializablePlan { Name = "Farm", OptimizedFor = "Omega" };

            var serializer = new XmlSerializer(typeof(SerializablePlan));
            using var writer = new StringWriter();
            serializer.Serialize(writer, plan);
            using var reader = new StringReader(writer.ToString());
            var result = (SerializablePlan)serializer.Deserialize(reader)!;

            result.OptimizedFor.Should().Be("Omega");
        }

        [Fact]
        public void OldPlans_LoadWithNoOptimizationMarker()
        {
            const string oldXml = "<SerializablePlan name=\"Legacy\" />";
            var serializer = new XmlSerializer(typeof(SerializablePlan));
            using var reader = new StringReader(oldXml);
            var result = (SerializablePlan)serializer.Deserialize(reader)!;

            result.OptimizedFor.Should().BeNull(
                "a plan saved before the marker existed was never optimized");
        }
    }
}
