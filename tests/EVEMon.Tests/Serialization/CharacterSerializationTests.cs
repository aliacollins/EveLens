using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Serialization
{
    /// <summary>
    /// Tier 5 serialization tests: character DTO round-trip serialization.
    /// Tests SerializableCCPCharacter, SerializableSettingsCharacter, and their
    /// child collections survive XML serialization without data loss.
    /// </summary>
    public class CharacterSerializationTests
    {
        #region SerializableCCPCharacter Full Round-Trip

        [Fact]
        public void SerializableCCPCharacter_FullyPopulated_RoundTrip()
        {
            var guid = Guid.NewGuid();
            var character = new SerializableCCPCharacter
            {
                Guid = guid,
                Label = "Main PvP Pilot",
                ID = 2119000001,
                Name = "Alia Collins",
                Race = "Caldari",
                BloodLine = "Deteis",
                Gender = "Female",
                CorporationName = "Test Corp",
                CorporationID = 98000001,
                AllianceName = "Test Alliance",
                AllianceID = 99000001,
                Balance = 5000000000.50m,
                SecurityStatus = -2.5,
                FreeSkillPoints = 500000,
                FreeRespecs = 2,
                ShipName = "Raven Navy Issue",
                ShipTypeName = "Raven Navy Issue",
                EveMailMessagesIDs = "100,200,300,400",
                EveNotificationsIDs = "500,600"
            };

            // Add skills
            character.Skills.Add(new SerializableCharacterSkill
            {
                ID = 3350,
                Name = "Caldari Battleship",
                Level = 5,
                ActiveLevel = 5,
                Skillpoints = 1280000,
                OwnsBook = true,
                IsKnown = true
            });
            character.Skills.Add(new SerializableCharacterSkill
            {
                ID = 3420,
                Name = "Missile Launcher Operation",
                Level = 4,
                ActiveLevel = 4,
                Skillpoints = 256000,
                OwnsBook = true,
                IsKnown = true
            });

            // Add queued skills
            character.SkillQueue.Add(new SerializableQueuedSkill
            {
                ID = 3350,
                Level = 5,
                StartSP = 1024000,
                EndSP = 1280000
            });

            // Add market orders
            character.MarketOrders.Add(new SerializableBuyOrder
            {
                OrderID = 9999001,
                State = OrderState.Active,
                UnitaryPrice = 150000000.00m,
                RemainingVolume = 10,
                IssuedFor = IssuedFor.Character
            });
            character.MarketOrders.Add(new SerializableSellOrder
            {
                OrderID = 9999002,
                State = OrderState.Expired,
                UnitaryPrice = 200000000.00m,
                RemainingVolume = 0,
                IssuedFor = IssuedFor.Character
            });

            // Add contracts
            character.Contracts.Add(new SerializableContract
            {
                ContractID = 55001,
                ContractState = ContractState.Created,
                ContractType = ContractType.ItemExchange,
                IssuedFor = IssuedFor.Character
            });

            // Add industry jobs
            character.IndustryJobs.Add(new SerializableJob
            {
                JobID = 77001,
                State = JobState.Active,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                IssuedFor = IssuedFor.Character
            });

            // Add last updates
            character.LastUpdates.Add(new SerializableAPIUpdate
            {
                Method = "CharacterSheet",
                Time = new DateTime(2026, 2, 15, 10, 30, 0, DateTimeKind.Utc)
            });
            character.LastUpdates.Add(new SerializableAPIUpdate
            {
                Method = "SkillQueue",
                Time = new DateTime(2026, 2, 15, 10, 35, 0, DateTimeKind.Utc)
            });

            // Add attributes
            character.Attributes = new SerializableCharacterAttributes
            {
                Intelligence = 27,
                Memory = 24,
                Perception = 20,
                Willpower = 23,
                Charisma = 17
            };

            var result = XmlRoundTrip(character);

            // Identity
            result.Guid.Should().Be(guid);
            result.Label.Should().Be("Main PvP Pilot");
            result.ID.Should().Be(2119000001);
            result.Name.Should().Be("Alia Collins");
            result.Race.Should().Be("Caldari");
            result.BloodLine.Should().Be("Deteis");
            result.Gender.Should().Be("Female");

            // Corporation/Alliance
            result.CorporationName.Should().Be("Test Corp");
            result.CorporationID.Should().Be(98000001);
            result.AllianceName.Should().Be("Test Alliance");
            result.AllianceID.Should().Be(99000001);

            // Financial
            result.Balance.Should().Be(5000000000.50m);
            result.SecurityStatus.Should().BeApproximately(-2.5, 0.001);

            // Skills
            result.FreeSkillPoints.Should().Be(500000);
            result.Skills.Should().HaveCount(2);
            result.Skills[0].ID.Should().Be(3350);
            result.Skills[0].Name.Should().Be("Caldari Battleship");
            result.Skills[0].Level.Should().Be(5);
            result.Skills[0].Skillpoints.Should().Be(1280000);
            result.Skills[0].OwnsBook.Should().BeTrue();
            result.Skills[0].IsKnown.Should().BeTrue();

            // Skill Queue
            result.SkillQueue.Should().HaveCount(1);
            result.SkillQueue[0].ID.Should().Be(3350);
            result.SkillQueue[0].Level.Should().Be(5);
            result.SkillQueue[0].StartSP.Should().Be(1024000);
            result.SkillQueue[0].EndSP.Should().Be(1280000);

            // Market Orders (polymorphic)
            result.MarketOrders.Should().HaveCount(2);
            result.MarketOrders[0].Should().BeOfType<SerializableBuyOrder>();
            result.MarketOrders[0].OrderID.Should().Be(9999001);
            result.MarketOrders[0].UnitaryPrice.Should().Be(150000000.00m);
            result.MarketOrders[1].Should().BeOfType<SerializableSellOrder>();
            result.MarketOrders[1].State.Should().Be(OrderState.Expired);

            // Contracts
            result.Contracts.Should().HaveCount(1);
            result.Contracts[0].ContractID.Should().Be(55001);
            result.Contracts[0].ContractType.Should().Be(ContractType.ItemExchange);

            // Industry Jobs
            result.IndustryJobs.Should().HaveCount(1);
            result.IndustryJobs[0].JobID.Should().Be(77001);
            result.IndustryJobs[0].State.Should().Be(JobState.Active);

            // Last Updates
            result.LastUpdates.Should().HaveCount(2);
            result.LastUpdates[0].Method.Should().Be("CharacterSheet");
            result.LastUpdates[1].Method.Should().Be("SkillQueue");

            // Attributes
            result.Attributes.Intelligence.Should().Be(27);
            result.Attributes.Memory.Should().Be(24);
            result.Attributes.Perception.Should().Be(20);
            result.Attributes.Willpower.Should().Be(23);
            result.Attributes.Charisma.Should().Be(17);

            // Mail/Notification IDs
            result.EveMailMessagesIDs.Should().Be("100,200,300,400");
            result.EveNotificationsIDs.Should().Be("500,600");
        }

        #endregion

        #region Key Field Preservation

        [Fact]
        public void CharacterID_PreservesLargeValues()
        {
            var character = new SerializableCCPCharacter
            {
                ID = 2119000999
            };

            var result = XmlRoundTrip(character);
            result.ID.Should().Be(2119000999);
        }

        [Fact]
        public void CharacterBalance_PreservesDecimalPrecision()
        {
            var character = new SerializableCCPCharacter
            {
                Balance = 99999999999.99m
            };

            var result = XmlRoundTrip(character);
            result.Balance.Should().Be(99999999999.99m);
        }

        [Fact]
        public void CharacterSecurityStatus_PreservesNegativeValues()
        {
            var character = new SerializableCCPCharacter
            {
                SecurityStatus = -10.0
            };

            var result = XmlRoundTrip(character);
            result.SecurityStatus.Should().BeApproximately(-10.0, 0.0001);
        }

        [Fact]
        public void Skill_PreservesAllFields()
        {
            var skill = new SerializableCharacterSkill
            {
                ID = 12345,
                Name = "Navigation",
                Level = 5,
                ActiveLevel = 4,
                Skillpoints = 256000,
                OwnsBook = true,
                IsKnown = true
            };

            var character = new SerializableCCPCharacter();
            character.Skills.Add(skill);

            var result = XmlRoundTrip(character);
            var resultSkill = result.Skills[0];

            resultSkill.ID.Should().Be(12345);
            resultSkill.Name.Should().Be("Navigation");
            resultSkill.Level.Should().Be(5);
            resultSkill.ActiveLevel.Should().Be(4);
            resultSkill.Skillpoints.Should().Be(256000);
            resultSkill.OwnsBook.Should().BeTrue();
            resultSkill.IsKnown.Should().BeTrue();
        }

        [Fact]
        public void QueuedSkill_PreservesAllFields()
        {
            var now = DateTime.UtcNow;
            var future = now.AddHours(2);

            var queuedSkill = new SerializableQueuedSkill
            {
                ID = 3350,
                Level = 5,
                StartSP = 100000,
                EndSP = 256000,
                StartTime = now,
                EndTime = future
            };

            var character = new SerializableCCPCharacter();
            character.SkillQueue.Add(queuedSkill);

            var result = XmlRoundTrip(character);
            var resultSkill = result.SkillQueue[0];

            resultSkill.ID.Should().Be(3350);
            resultSkill.Level.Should().Be(5);
            resultSkill.StartSP.Should().Be(100000);
            resultSkill.EndSP.Should().Be(256000);
            // Times are serialized as strings, some precision may be lost
            resultSkill.StartTime.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            resultSkill.EndTime.Should().BeCloseTo(future, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void MarketOrder_BuyAndSell_PolymorphismPreserved()
        {
            var character = new SerializableCCPCharacter();

            character.MarketOrders.Add(new SerializableBuyOrder
            {
                OrderID = 1,
                UnitaryPrice = 100m,
                State = OrderState.Active
            });
            character.MarketOrders.Add(new SerializableSellOrder
            {
                OrderID = 2,
                UnitaryPrice = 200m,
                State = OrderState.Fulfilled
            });

            var result = XmlRoundTrip(character);

            result.MarketOrders.Should().HaveCount(2);
            result.MarketOrders[0].Should().BeOfType<SerializableBuyOrder>();
            result.MarketOrders[1].Should().BeOfType<SerializableSellOrder>();
        }

        #endregion

        #region SerializableUriCharacter

        [Fact]
        public void SerializableUriCharacter_RoundTrip_PreservesUri()
        {
            var character = new SerializableUriCharacter
            {
                Guid = Guid.NewGuid(),
                Label = "External Char",
                Address = "https://example.com/character.xml"
            };

            var result = XmlRoundTrip(character);

            result.Label.Should().Be("External Char");
            result.Address.Should().Be("https://example.com/character.xml");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EmptyCharacter_RoundTrips()
        {
            var character = new SerializableCCPCharacter();
            var result = XmlRoundTrip(character);

            result.Should().NotBeNull();
            result.Skills.Should().BeEmpty();
            result.SkillQueue.Should().BeEmpty();
            result.MarketOrders.Should().BeEmpty();
            result.Contracts.Should().BeEmpty();
            result.IndustryJobs.Should().BeEmpty();
            result.LastUpdates.Should().BeEmpty();
        }

        [Fact]
        public void Character_WithManySkills_RoundTrips()
        {
            var character = new SerializableCCPCharacter();

            // Add 50 skills to test collection serialization at scale
            for (int i = 0; i < 50; i++)
            {
                character.Skills.Add(new SerializableCharacterSkill
                {
                    ID = 10000 + i,
                    Name = $"Skill {i}",
                    Level = (i % 5) + 1,
                    Skillpoints = (i + 1) * 10000,
                    IsKnown = true
                });
            }

            var result = XmlRoundTrip(character);

            result.Skills.Should().HaveCount(50);
            result.Skills[0].ID.Should().Be(10000);
            result.Skills[49].ID.Should().Be(10049);
            result.Skills[25].Name.Should().Be("Skill 25");
        }

        [Fact]
        public void Character_WithLargeSkillQueue_RoundTrips()
        {
            var character = new SerializableCCPCharacter();

            // EVE allows up to 50 skills in the queue
            for (int i = 0; i < 50; i++)
            {
                character.SkillQueue.Add(new SerializableQueuedSkill
                {
                    ID = 20000 + i,
                    Level = (i % 5) + 1,
                    StartSP = i * 1000,
                    EndSP = (i + 1) * 1000
                });
            }

            var result = XmlRoundTrip(character);
            result.SkillQueue.Should().HaveCount(50);
        }

        [Fact]
        public void Contract_AllTypes_RoundTrip()
        {
            var character = new SerializableCCPCharacter();

            foreach (ContractType type in Enum.GetValues(typeof(ContractType)))
            {
                character.Contracts.Add(new SerializableContract
                {
                    ContractID = (long)type + 1000,
                    ContractType = type,
                    ContractState = ContractState.Created,
                    IssuedFor = IssuedFor.Character
                });
            }

            var result = XmlRoundTrip(character);
            result.Contracts.Should().HaveCount(Enum.GetValues(typeof(ContractType)).Length);

            foreach (ContractType type in Enum.GetValues(typeof(ContractType)))
            {
                result.Contracts.Should().Contain(c => c.ContractType == type,
                    $"ContractType.{type} should survive round-trip");
            }
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
