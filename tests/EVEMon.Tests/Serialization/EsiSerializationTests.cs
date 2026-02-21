// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using EVEMon.Common.Serialization.Esi;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Serialization
{
    /// <summary>
    /// Tier 5 serialization tests: ESI DTO deserialization from sample JSON.
    /// The ESI DTOs use DataContractJsonSerializer (not System.Text.Json) because
    /// they were designed for the DataContract/DataMember attribute pattern.
    /// </summary>
    public class EsiSerializationTests
    {
        #region Character Sheet Deserialization

        [Fact]
        public void EsiAPICharacterSheet_Deserialize_BasicFields()
        {
            string json = @"{
                ""name"": ""Alia Collins"",
                ""description"": ""A test character"",
                ""birthday"": ""2010-06-01T12:00:00Z"",
                ""race_id"": 1,
                ""bloodline_id"": 1,
                ""ancestry_id"": 1,
                ""gender"": ""female"",
                ""corporation_id"": 98000001,
                ""alliance_id"": 99000001,
                ""security_status"": -2.5
            }";

            var result = DeserializeDataContract<EsiAPICharacterSheet>(json);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Alia Collins");
            result.Description.Should().Be("A test character");
            result.Gender.Should().Be("female");
            result.CorporationID.Should().Be(98000001);
            result.AllianceID.Should().Be(99000001);
            result.SecurityStatus.Should().BeApproximately(-2.5, 0.001);
        }

        [Fact]
        public void EsiAPICharacterSheet_Deserialize_MinimalFields()
        {
            // ESI may return only required fields
            string json = @"{
                ""name"": ""Minimal Pilot"",
                ""birthday"": ""2020-01-01T00:00:00Z"",
                ""race_id"": 4,
                ""bloodline_id"": 7,
                ""gender"": ""male"",
                ""corporation_id"": 1000125,
                ""security_status"": 0.0
            }";

            var result = DeserializeDataContract<EsiAPICharacterSheet>(json);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Minimal Pilot");
            result.CorporationID.Should().Be(1000125);
            result.AllianceID.Should().Be(0, "optional field should default to 0");
        }

        [Fact]
        public void EsiAPICharacterSheet_Deserialize_NegativeSecurityStatus()
        {
            string json = @"{
                ""name"": ""Pirate"",
                ""birthday"": ""2015-06-01T00:00:00Z"",
                ""race_id"": 2,
                ""bloodline_id"": 4,
                ""gender"": ""male"",
                ""corporation_id"": 98000002,
                ""security_status"": -10.0
            }";

            var result = DeserializeDataContract<EsiAPICharacterSheet>(json);

            result!.SecurityStatus.Should().BeApproximately(-10.0, 0.001);
        }

        #endregion

        #region Skill Queue Deserialization

        [Fact]
        public void EsiAPISkillQueue_Deserialize_MultipleEntries()
        {
            string json = @"[
                {
                    ""skill_id"": 3350,
                    ""finished_level"": 5,
                    ""queue_position"": 0,
                    ""training_start_sp"": 1024000,
                    ""level_start_sp"": 1024000,
                    ""level_end_sp"": 1280000,
                    ""start_date"": ""2026-02-15T10:00:00Z"",
                    ""finish_date"": ""2026-02-16T10:00:00Z""
                },
                {
                    ""skill_id"": 3420,
                    ""finished_level"": 4,
                    ""queue_position"": 1,
                    ""training_start_sp"": 128000,
                    ""level_start_sp"": 128000,
                    ""level_end_sp"": 256000,
                    ""start_date"": ""2026-02-16T10:00:00Z"",
                    ""finish_date"": ""2026-02-17T06:00:00Z""
                }
            ]";

            var result = DeserializeDataContract<EsiAPISkillQueue>(json);

            result.Should().NotBeNull();
            result!.Should().HaveCount(2);
            result[0].ID.Should().Be(3350);
            result[0].Level.Should().Be(5);
            result[0].QueuePosition.Should().Be(0);
            result[0].StartSP.Should().Be(1024000);
            result[0].EndSP.Should().Be(1280000);
            result[0].StartTime.Should().BeAfter(DateTime.MinValue);

            result[1].ID.Should().Be(3420);
            result[1].Level.Should().Be(4);
            result[1].QueuePosition.Should().Be(1);
        }

        [Fact]
        public void EsiAPISkillQueue_Deserialize_EmptyQueue()
        {
            string json = @"[]";

            var result = DeserializeDataContract<EsiAPISkillQueue>(json);

            result.Should().NotBeNull();
            result!.Should().BeEmpty();
        }

        [Fact]
        public void EsiAPISkillQueue_Deserialize_PausedSkill()
        {
            // When queue is paused, start_date and finish_date may be missing
            string json = @"[
                {
                    ""skill_id"": 3350,
                    ""finished_level"": 3,
                    ""queue_position"": 0,
                    ""training_start_sp"": 8000,
                    ""level_start_sp"": 8000,
                    ""level_end_sp"": 45255
                }
            ]";

            var result = DeserializeDataContract<EsiAPISkillQueue>(json);

            result.Should().NotBeNull();
            result!.Should().HaveCount(1);
            result[0].ID.Should().Be(3350);
            // Without dates, the item's EndTime defaults to MaxValue (from constructor)
            // which means it's not "paused" by the SerializableQueuedSkill definition but
            // that's fine - the conversion handles it
        }

        [Fact]
        public void EsiAPISkillQueue_CreateSkillQueue_ConvertsAllItems()
        {
            string json = @"[
                {
                    ""skill_id"": 100,
                    ""finished_level"": 2,
                    ""queue_position"": 0,
                    ""training_start_sp"": 0,
                    ""level_start_sp"": 0,
                    ""level_end_sp"": 1000,
                    ""start_date"": ""2026-01-01T00:00:00Z"",
                    ""finish_date"": ""2026-01-02T00:00:00Z""
                },
                {
                    ""skill_id"": 200,
                    ""finished_level"": 3,
                    ""queue_position"": 1,
                    ""training_start_sp"": 1000,
                    ""level_start_sp"": 1000,
                    ""level_end_sp"": 5000,
                    ""start_date"": ""2026-01-02T00:00:00Z"",
                    ""finish_date"": ""2026-01-05T00:00:00Z""
                }
            ]";

            var esiQueue = DeserializeDataContract<EsiAPISkillQueue>(json);
            var xmlQueue = esiQueue!.CreateSkillQueue();

            xmlQueue.Should().HaveCount(2);
        }

        #endregion

        #region Market Orders Deserialization

        [Fact]
        public void EsiAPIMarketOrders_Deserialize_BuyAndSellOrders()
        {
            string json = @"[
                {
                    ""order_id"": 5000001,
                    ""type_id"": 34,
                    ""location_id"": 60003760,
                    ""volume_total"": 10000,
                    ""volume_remain"": 5000,
                    ""min_volume"": 1,
                    ""price"": 6.50,
                    ""is_buy_order"": true,
                    ""duration"": 90,
                    ""range"": ""station"",
                    ""issued"": ""2026-02-01T00:00:00Z""
                },
                {
                    ""order_id"": 5000002,
                    ""type_id"": 35,
                    ""location_id"": 60003760,
                    ""volume_total"": 500,
                    ""volume_remain"": 500,
                    ""min_volume"": 1,
                    ""price"": 100.25,
                    ""is_buy_order"": false,
                    ""duration"": 30,
                    ""range"": ""region"",
                    ""issued"": ""2026-02-10T12:00:00Z""
                }
            ]";

            var result = DeserializeDataContract<EsiAPIMarketOrders>(json);

            result.Should().NotBeNull();
            result!.Should().HaveCount(2);

            // Buy order
            result[0].OrderID.Should().Be(5000001);
            result[0].ItemID.Should().Be(34);
            result[0].StationID.Should().Be(60003760);
            result[0].InitialVolume.Should().Be(10000);
            result[0].RemainingVolume.Should().Be(5000);
            result[0].UnitaryPrice.Should().Be(6.50m);
            result[0].IsBuyOrder.Should().BeTrue();
            result[0].Duration.Should().Be(90);
            result[0].Range.Should().Be(-1, "station range maps to -1");

            // Sell order
            result[1].OrderID.Should().Be(5000002);
            result[1].IsBuyOrder.Should().BeFalse();
            result[1].UnitaryPrice.Should().Be(100.25m);
        }

        [Fact]
        public void EsiAPIMarketOrders_Deserialize_EmptyList()
        {
            string json = @"[]";
            var result = DeserializeDataContract<EsiAPIMarketOrders>(json);

            result.Should().NotBeNull();
            result!.Should().BeEmpty();
        }

        [Fact]
        public void EsiAPIMarketOrders_SetAllIssuedBy_SetsEveryItem()
        {
            string json = @"[
                { ""order_id"": 1, ""type_id"": 34, ""price"": 10, ""is_buy_order"": true, ""duration"": 1, ""range"": ""station"", ""volume_total"": 1, ""volume_remain"": 1, ""min_volume"": 1, ""location_id"": 1, ""issued"": ""2026-01-01T00:00:00Z"" },
                { ""order_id"": 2, ""type_id"": 35, ""price"": 20, ""is_buy_order"": false, ""duration"": 1, ""range"": ""region"", ""volume_total"": 1, ""volume_remain"": 1, ""min_volume"": 1, ""location_id"": 1, ""issued"": ""2026-01-01T00:00:00Z"" }
            ]";

            var orders = DeserializeDataContract<EsiAPIMarketOrders>(json);
            orders!.SetAllIssuedBy(2119000001);

            orders[0].IssuedBy.Should().Be(2119000001);
            orders[1].IssuedBy.Should().Be(2119000001);
        }

        [Fact]
        public void EsiOrderListItem_RangeMapping_Station()
        {
            string json = @"[{
                ""order_id"": 1, ""type_id"": 34, ""location_id"": 1,
                ""volume_total"": 1, ""volume_remain"": 1, ""min_volume"": 1,
                ""price"": 10, ""is_buy_order"": true, ""duration"": 1,
                ""range"": ""station"",
                ""issued"": ""2026-01-01T00:00:00Z""
            }]";

            var orders = DeserializeDataContract<EsiAPIMarketOrders>(json);
            orders![0].Range.Should().Be(-1, "station maps to range -1");
        }

        [Fact]
        public void EsiOrderListItem_RangeMapping_SolarSystem()
        {
            string json = @"[{
                ""order_id"": 1, ""type_id"": 34, ""location_id"": 1,
                ""volume_total"": 1, ""volume_remain"": 1, ""min_volume"": 1,
                ""price"": 10, ""is_buy_order"": true, ""duration"": 1,
                ""range"": ""solarsystem"",
                ""issued"": ""2026-01-01T00:00:00Z""
            }]";

            var orders = DeserializeDataContract<EsiAPIMarketOrders>(json);
            orders![0].Range.Should().Be(0, "solarsystem maps to range 0");
        }

        [Fact]
        public void EsiOrderListItem_StateMapping_Active()
        {
            string json = @"[{
                ""order_id"": 1, ""type_id"": 34, ""location_id"": 1,
                ""volume_total"": 1, ""volume_remain"": 1, ""min_volume"": 1,
                ""price"": 10, ""is_buy_order"": false, ""duration"": 1,
                ""range"": ""station"",
                ""issued"": ""2026-01-01T00:00:00Z""
            }]";

            var orders = DeserializeDataContract<EsiAPIMarketOrders>(json);
            // No "state" field in active orders from ESI
            orders![0].State.Should().Be(0, "missing state defaults to 0 (active)");
        }

        [Fact]
        public void EsiOrderListItem_StateMapping_Expired()
        {
            string json = @"[{
                ""order_id"": 1, ""type_id"": 34, ""location_id"": 1,
                ""volume_total"": 1, ""volume_remain"": 1, ""min_volume"": 1,
                ""price"": 10, ""is_buy_order"": false, ""duration"": 1,
                ""range"": ""station"",
                ""state"": ""expired"",
                ""issued"": ""2026-01-01T00:00:00Z""
            }]";

            var orders = DeserializeDataContract<EsiAPIMarketOrders>(json);
            orders![0].State.Should().Be(2, "expired maps to state 2");
        }

        #endregion

        #region Skills Deserialization

        [Fact]
        public void EsiAPISkills_Deserialize_FullResponse()
        {
            string json = @"{
                ""total_sp"": 50000000,
                ""unallocated_sp"": 500000,
                ""skills"": [
                    {
                        ""skill_id"": 3350,
                        ""trained_skill_level"": 5,
                        ""active_skill_level"": 5,
                        ""skillpoints_in_skill"": 1280000
                    },
                    {
                        ""skill_id"": 3420,
                        ""trained_skill_level"": 4,
                        ""active_skill_level"": 4,
                        ""skillpoints_in_skill"": 256000
                    },
                    {
                        ""skill_id"": 3300,
                        ""trained_skill_level"": 3,
                        ""active_skill_level"": 2,
                        ""skillpoints_in_skill"": 45255
                    }
                ]
            }";

            var result = DeserializeDataContract<EsiAPISkills>(json);

            result.Should().NotBeNull();
            result!.TotalSP.Should().Be(50000000);
            result.UnallocatedSP.Should().Be(500000);
            result.Skills.Should().HaveCount(3);
            result.Skills[0].ID.Should().Be(3350);
            result.Skills[0].Level.Should().Be(5);
            result.Skills[0].ActiveLevel.Should().Be(5);
            result.Skills[0].Skillpoints.Should().Be(1280000);

            // Alpha clone - active level less than trained level
            result.Skills[2].Level.Should().Be(3);
            result.Skills[2].ActiveLevel.Should().Be(2);
        }

        [Fact]
        public void EsiAPISkills_Deserialize_EmptySkillList()
        {
            string json = @"{
                ""total_sp"": 0,
                ""skills"": []
            }";

            var result = DeserializeDataContract<EsiAPISkills>(json);

            result.Should().NotBeNull();
            result!.TotalSP.Should().Be(0);
            result.Skills.Should().BeEmpty();
        }

        [Fact]
        public void EsiSkillListItem_ToXMLItem_ConvertsCorrectly()
        {
            var esiSkill = new EsiSkillListItem
            {
                ID = 3350,
                Level = 5,
                ActiveLevel = 5,
                Skillpoints = 1280000
            };

            var xmlSkill = esiSkill.ToXMLItem();

            xmlSkill.ID.Should().Be(3350);
            xmlSkill.Level.Should().Be(5);
            xmlSkill.ActiveLevel.Should().Be(5);
            xmlSkill.Skillpoints.Should().Be(1280000);
            xmlSkill.OwnsBook.Should().BeTrue();
            xmlSkill.IsKnown.Should().BeTrue();
        }

        #endregion

        #region Attributes Deserialization

        [Fact]
        public void EsiAPIAttributes_Deserialize_AllAttributes()
        {
            string json = @"{
                ""intelligence"": 27,
                ""memory"": 24,
                ""perception"": 20,
                ""willpower"": 23,
                ""charisma"": 17,
                ""bonus_remaps"": 2,
                ""last_remap_date"": ""2025-06-01T00:00:00Z"",
                ""accrued_remap_cooldown_date"": ""2026-06-01T00:00:00Z""
            }";

            var result = DeserializeDataContract<EsiAPIAttributes>(json);

            result.Should().NotBeNull();
            result!.Intelligence.Should().Be(27);
            result.Memory.Should().Be(24);
            result.Perception.Should().Be(20);
            result.Willpower.Should().Be(23);
            result.Charisma.Should().Be(17);
            result.BonusRemaps.Should().Be(2);
            result.LastRemap.Should().BeAfter(DateTime.MinValue);
            result.RemapCooldownDate.Should().BeAfter(DateTime.MinValue);
        }

        [Fact]
        public void EsiAPIAttributes_Deserialize_MinimalAttributes()
        {
            string json = @"{
                ""intelligence"": 17,
                ""memory"": 17,
                ""perception"": 17,
                ""willpower"": 17,
                ""charisma"": 17
            }";

            var result = DeserializeDataContract<EsiAPIAttributes>(json);

            result.Should().NotBeNull();
            result!.Intelligence.Should().Be(17);
            result.BonusRemaps.Should().Be(0, "optional field defaults to 0");
        }

        [Fact]
        public void EsiAPIAttributes_DefaultConstructor_HasSafeDefaults()
        {
            var attrs = new EsiAPIAttributes();

            // All attributes default to 1 (prevents division by zero)
            attrs.Intelligence.Should().Be(1);
            attrs.Memory.Should().Be(1);
            attrs.Perception.Should().Be(1);
            attrs.Willpower.Should().Be(1);
            attrs.Charisma.Should().Be(1);
        }

        #endregion

        #region EsiSkillQueueListItem ToXMLItem Conversion

        [Fact]
        public void EsiSkillQueueListItem_ToXMLItem_PreservesAllFields()
        {
            var esiItem = new EsiSkillQueueListItem
            {
                ID = 3350,
                Level = 5,
                StartSP = 1024000,
                EndSP = 1280000
            };

            var xmlItem = esiItem.ToXMLItem();

            xmlItem.ID.Should().Be(3350);
            xmlItem.Level.Should().Be(5);
            xmlItem.StartSP.Should().Be(1024000);
            xmlItem.EndSP.Should().Be(1280000);
        }

        [Fact]
        public void EsiSkillQueueListItem_DefaultTimes_AreExtreme()
        {
            var item = new EsiSkillQueueListItem();

            // Default: startTime = MinValue, endTime = MaxValue (from constructor)
            item.StartTime.Should().Be(DateTime.MinValue);
            item.EndTime.Should().Be(DateTime.MaxValue);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Deserialize using DataContractJsonSerializer, matching the production code path.
        /// </summary>
        private static T? DeserializeDataContract<T>(string json) where T : class
        {
            using var stream = new MemoryStream(Encoding.Unicode.GetBytes(json));
            var settings = new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            };
            var serializer = new DataContractJsonSerializer(typeof(T), settings);
            return serializer.ReadObject(stream) as T;
        }

        #endregion
    }
}
