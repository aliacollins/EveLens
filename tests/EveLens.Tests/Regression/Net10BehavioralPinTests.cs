// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EveLens.Common;
using EveLens.Common.Extensions;
using EveLens.Common.Helpers;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Pinning tests for the .NET 10 migration's MEDIUM audit findings (M1–M4 in
    /// NET10-MIGRATION.md), authored and green on net8 BEFORE the retarget. Each region
    /// pins a behavior that either changes on .NET 10 (M1: XmlSerializer starts writing
    /// [Obsolete] members) or was a latent locale/overflow hazard the audit surfaced
    /// (M2 culture asymmetry, M3 silent settings-load failure, M4 timeout overflow).
    /// </summary>
    public class Net10BehavioralPinTests
    {
        #region M1 — accessMask XML data contract (Law 13)

        [Fact]
        public void EsiKey_XmlRoundTrip_PreservesFieldsAndNeverProducesMigrationSentinel()
        {
            // .NET 10's XmlSerializer serializes [Obsolete] members, so accessMask starts
            // appearing in XML backup exports. Reimport logic only acts on ulong.MaxValue,
            // so the round-trip must (a) never throw, (b) preserve the real contract
            // fields, and (c) yield the written value or the default — never the sentinel.
            var key = new SerializableESIKey
            {
                ID = 42L,
                RefreshToken = "token",
                Monitored = true,
            };
#pragma warning disable CS0618 // intentionally exercising the obsolete legacy member
            key.AccessMask = 12345UL;

            var result = XmlRoundTrip(key);

            result.ID.Should().Be(42L);
            result.RefreshToken.Should().Be("token");
            result.Monitored.Should().BeTrue();
            result.AccessMask.Should().BeOneOf(new[] { 0UL, 12345UL },
                "net8 skips [Obsolete] members (reads back 0), net10 writes them " +
                "(reads back 12345); both are benign — ulong.MaxValue is not");
#pragma warning restore CS0618
        }

        [Fact]
        public void EsiKey_XmlWithAccessMaskAttribute_StillDeserializes()
        {
            // Old settings files carry accessMask. Discovered while pinning: net8's
            // XmlSerializer skips [Obsolete] members on READ as well, so the attribute
            // deserializes to 0 today; .NET 10 reads the real value. Both must parse
            // without error. The net10 behavior is an improvement — a legacy full-access
            // key (ulong.MaxValue) then correctly migrates to all scopes in ESIKey's
            // constructor instead of silently losing its mask.
            const string xml =
                "<SerializableESIKey id=\"7\" accessMask=\"18446744073709551615\" monitored=\"true\" />";

            var serializer = new XmlSerializer(typeof(SerializableESIKey));
            using var reader = new StringReader(xml);
            var result = (SerializableESIKey)serializer.Deserialize(reader)!;

            result.ID.Should().Be(7L);
#pragma warning disable CS0618
            result.AccessMask.Should().BeOneOf(new[] { 0UL, ulong.MaxValue },
                "net8 skips [Obsolete] members on read (0), net10 reads the sentinel — " +
                "either way deserialization must succeed");
#pragma warning restore CS0618
        }

        #endregion

        #region M2 — date string round-trip must be culture-proof

        [Theory]
        [InlineData("de-DE")]
        [InlineData("tr-TR")]
        [InlineData("ar-SA")] // non-Gregorian default calendar — the hardest case
        public void TimeString_RoundTrip_SurvivesHostileCulture(string cultureName)
        {
            // DateTimeToTimeString writes invariant; TimeStringToDateTime used to read
            // with CurrentCulture, silently returning default(DateTime) under locales
            // that parse "yyyy-MM-dd HH:mm:ss" differently (~25 deserialization sites,
            // including CachedUntil timestamps).
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(cultureName);

                var time = new DateTime(2026, 8, 12, 14, 30, 45, DateTimeKind.Utc);
                string written = time.DateTimeToTimeString();
                DateTime read = written.TimeStringToDateTime();

                read.Should().Be(time,
                    $"a value we wrote ourselves must read back identically under {cultureName}");
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void TimeString_Garbage_StillReturnsDefault()
        {
            "not a date".TimeStringToDateTime().Should().Be(default(DateTime),
            "unparseable input keeps the long-standing silent-default contract");
        }

        #endregion

        #region M3 — settings JSON load failures must not be silent or throw

        [Fact]
        public async Task TryLoadJson_CorruptFile_ReturnsNullWithoutThrowing()
        {
            string path = Path.Combine(Path.GetTempPath(), $"evelens-test-{Guid.NewGuid():N}.json");
            try
            {
                await File.WriteAllTextAsync(path, "{ this is not json !!!");

                var result = await SettingsFileManager.TryLoadJsonAsync<UpdateSettings>(path);

                result.Should().BeNull("corrupt settings fall back to defaults rather than crash");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task TryLoadJson_MissingFile_ReturnsNull()
        {
            var result = await SettingsFileManager.TryLoadJsonAsync<UpdateSettings>(
                Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.json"));
            result.Should().BeNull();
        }

        [Fact]
        public void LegacyJsonOptions_WriteCamelCase_ReadCaseInsensitive_RoundTrips()
        {
            // The legacy component files are written camelCase and read case-insensitively.
            // Pin that this asymmetric option pair actually round-trips a settings object,
            // so an STJ behavior change on either side surfaces here instead of as
            // "my settings reset" in the field (Law 13).
            var settings = new UpdateSettings { HttpTimeout = 45, UpdateFrequency = 900 };

            string json = JsonSerializer.Serialize(settings, SettingsFileManager.s_jsonOptions);
            json.Should().Contain("\"httpTimeout\"", "legacy files are camelCase on disk");

            var result = JsonSerializer.Deserialize<UpdateSettings>(
                json, SettingsFileManager.s_jsonReadOptions);

            result.Should().NotBeNull();
            result!.HttpTimeout.Should().Be(45);
            result.UpdateFrequency.Should().Be(900);
        }

        #endregion

        #region M4 — HTTP timeout is clamped at the source

        [Theory]
        [InlineData(int.MaxValue, 3600)] // hand-edited huge value → ceiling
        [InlineData(999999, 3600)]
        [InlineData(0, 20)]              // unset/zero → default
        [InlineData(-5, 20)]             // garbage → default
        [InlineData(45, 45)]             // sane values untouched
        [InlineData(20, 20)]
        public void HttpTimeout_IsClampedToSaneRange(int stored, int expected)
        {
            var settings = new UpdateSettings { HttpTimeout = stored };
            settings.HttpTimeout.Should().Be(expected);
        }

        [Fact]
        public void HttpTimeout_Ceiling_FitsInMillisecondInt()
        {
            // The Emailer converts seconds → int milliseconds; the clamp ceiling must
            // keep that conversion inside int range on any runtime (finding M4: the raw
            // cast wrapped on net8 and silently became an infinite timeout on net10).
            var settings = new UpdateSettings { HttpTimeout = int.MaxValue };
            double ms = TimeSpan.FromSeconds(settings.HttpTimeout).TotalMilliseconds;
            ms.Should().BeLessThanOrEqualTo(int.MaxValue);
        }

        #endregion

        #region Update check backoff stays bounded

        [Fact]
        public void UpdateManager_BackoffDelay_IsBoundedAtHighRetryCounts()
        {
            // From retry 31 on, 2^n exceeds int range. The old (int)Math.Pow cast wrapped
            // negative on net8 — a latent Task.Delay crash — and saturates on net10.
            // The clamp now happens in double space; delays must stay in (0, 60] minutes
            // no matter how long EveLens has been retrying.
            var manager = typeof(UpdateManager);
            var counter = manager.GetField("s_errorRetryCount",
                BindingFlags.NonPublic | BindingFlags.Static);
            var method = manager.GetMethod("GetBackoffDelay",
                BindingFlags.NonPublic | BindingFlags.Static);
            counter.Should().NotBeNull();
            method.Should().NotBeNull();

            object original = counter!.GetValue(null)!;
            try
            {
                foreach (int retry in new[] { 0, 5, 30, 31, 40, 100 })
                {
                    counter.SetValue(null, retry);
                    var delay = (TimeSpan)method!.Invoke(null, null)!;

                    delay.Should().BeGreaterThan(TimeSpan.Zero,
                        $"retry {retry} must never schedule a negative delay");
                    delay.Should().BeLessThanOrEqualTo(TimeSpan.FromMinutes(60),
                        $"retry {retry} must respect the 60-minute cap");
                }
            }
            finally
            {
                counter.SetValue(null, original);
            }
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
