// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for Issue #66 — Planetary Interaction pin state and output resolution.
    ///
    /// Two root-cause fixes are pinned here:
    ///  - "Unknown" final product: an actively-extracting ECU routes material downstream, so its
    ///    <c>contents</c> array is empty. The pin must fall back to <c>extractor_details.product_type_id</c>
    ///    for its output type instead of leaving it 0 (which rendered as "Unknown").
    ///  - Idle indicator going stale: <see cref="PlanetaryPin.State"/> is computed live from
    ///    <see cref="PlanetaryPin.ExpiryTime"/> rather than cached at construction, so a pin that
    ///    goes idle mid-session reports Idle without waiting for the next ESI refresh.
    ///
    /// Pins are built from real ESI JSON via the production DataContractJsonSerializer path so the
    /// test exercises deserialization (including the time fields) exactly as the app does.
    /// </summary>
    [Collection("StaticData")]
    public class PlanetaryPinOutputTests
    {
        public PlanetaryPinOutputTests()
        {
            PlanTestFixture.EnsureGameDataLoaded();
        }

        private static T DeserializeEsi<T>(string json) where T : class
        {
            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(json));
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(stream)!;
        }

        /// <summary>Builds an extractor-pin ESI JSON payload. type 2848 = a real Barren ECU.</summary>
        private static string ExtractorPinJson(int productTypeId, string expiryIso, bool withContents)
        {
            string contents = withContents
                ? $"\"contents\":[{{\"type_id\":{productTypeId},\"amount\":500}}],"
                : "";
            return $@"{{
                ""pin_id"":1001,
                ""type_id"":{DBConstants.BarrenExtractorControlUnit},
                ""install_time"":""2026-01-01T00:00:00Z"",
                ""expiry_time"":""{expiryIso}"",
                ""last_cycle_start"":""2026-01-01T00:00:00Z"",
                {contents}
                ""extractor_details"":{{""product_type_id"":{productTypeId},""cycle_time"":3600,""qty_per_cycle"":5000}}
            }}";
        }

        [Fact]
        public void ActiveExtractor_WithEmptyContents_ResolvesOutputFromProductTypeId()
        {
            // A P0 resource type id (Aqueous Liquids). Any valid product id proves the wiring.
            const int productTypeId = 2268;
            var src = DeserializeEsi<EsiPlanetaryPin>(
                ExtractorPinJson(productTypeId, "2099-01-01T00:00:00Z", withContents: false));

            var pin = new PlanetaryPin(null!, src);

            pin.ContentTypeID.Should().Be(productTypeId,
                "an extracting ECU with empty contents must fall back to the declared product_type_id (Issue #66)");
            pin.ContentTypeName.Should().NotBe(EveLensConstants.UnknownText,
                "the output must resolve to a real item name, not 'Unknown'");
            pin.ContentTypeName.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Extractor_WithContents_StillPrefersContents()
        {
            const int contentsTypeId = 2268;
            // product_type_id differs from contents so we can tell which one wins.
            string json = $@"{{
                ""pin_id"":1002,
                ""type_id"":{DBConstants.BarrenExtractorControlUnit},
                ""expiry_time"":""2099-01-01T00:00:00Z"",
                ""contents"":[{{""type_id"":{contentsTypeId},""amount"":500}}],
                ""extractor_details"":{{""product_type_id"":2270,""cycle_time"":3600,""qty_per_cycle"":5000}}
            }}";
            var src = DeserializeEsi<EsiPlanetaryPin>(json);

            var pin = new PlanetaryPin(null!, src);

            pin.ContentTypeID.Should().Be(contentsTypeId,
                "when contents are present they remain the source of truth; the fallback only fills the empty case");
        }

        [Fact]
        public void State_IsComputedLive_FromExpiryTime_Idle()
        {
            // Expiry in the past → reports Idle immediately. Historically this was frozen at
            // construction, so a pin built while running never flipped to Idle mid-session.
            var src = DeserializeEsi<EsiPlanetaryPin>(
                ExtractorPinJson(2268, "2000-01-01T00:00:00Z", withContents: false));

            var pin = new PlanetaryPin(null!, src);

            pin.State.Should().Be(PlanetaryPinState.Idle,
                "State must be derived live from ExpiryTime, not cached at construction (Issue #66)");
        }

        [Fact]
        public void State_IsComputedLive_FromExpiryTime_Extracting()
        {
            var src = DeserializeEsi<EsiPlanetaryPin>(
                ExtractorPinJson(2268, "2099-01-01T00:00:00Z", withContents: false));

            var pin = new PlanetaryPin(null!, src);

            pin.State.Should().Be(PlanetaryPinState.Extracting,
                "an ECU with a future expiry is still extracting");
        }
    }
}
