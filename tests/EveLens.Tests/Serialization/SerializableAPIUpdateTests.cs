// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EveLens.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Serialization
{
    public class SerializableAPIUpdateTests
    {
        [Fact]
        public void RoundTrip_WithETagAndCachedUntil_PreservesValues()
        {
            var original = new SerializableAPIUpdate
            {
                Method = "Skills",
                Time = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
                ETag = "\"abc-123-def\"",
                CachedUntil = new DateTime(2025, 6, 15, 12, 5, 0, DateTimeKind.Utc),
            };

            var serializer = new XmlSerializer(typeof(SerializableAPIUpdate));
            using var writer = new StringWriter();
            serializer.Serialize(writer, original);

            using var reader = new StringReader(writer.ToString());
            var deserialized = (SerializableAPIUpdate)serializer.Deserialize(reader)!;

            deserialized.Method.Should().Be("Skills");
            deserialized.ETag.Should().Be("\"abc-123-def\"");
            deserialized.CachedUntil.Should().Be(original.CachedUntil);
        }

        [Fact]
        public void RoundTrip_WithoutETagAndCachedUntil_DefaultsCorrectly()
        {
            // Simulate old settings file without the new fields
            var xml = "<?xml version=\"1.0\"?>\n" +
                "<SerializableAPIUpdate xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" " +
                "method=\"Skills\" time=\"2025-06-15T12:00:00\" />";

            var serializer = new XmlSerializer(typeof(SerializableAPIUpdate));
            using var reader = new StringReader(xml);
            var deserialized = (SerializableAPIUpdate)serializer.Deserialize(reader)!;

            deserialized.Method.Should().Be("Skills");
            deserialized.ETag.Should().BeNull();
            deserialized.CachedUntil.Should().Be(default(DateTime));
        }

        [Fact]
        public void RoundTrip_ETagWithQuotes_PreservesQuotes()
        {
            var original = new SerializableAPIUpdate
            {
                Method = "CharacterSheet",
                Time = DateTime.UtcNow,
                ETag = "\"W/etag-with-weak-validator\"",
                CachedUntil = DateTime.UtcNow.AddMinutes(5),
            };

            var serializer = new XmlSerializer(typeof(SerializableAPIUpdate));
            using var writer = new StringWriter();
            serializer.Serialize(writer, original);

            using var reader = new StringReader(writer.ToString());
            var deserialized = (SerializableAPIUpdate)serializer.Deserialize(reader)!;

            deserialized.ETag.Should().Be("\"W/etag-with-weak-validator\"");
        }
    }
}
