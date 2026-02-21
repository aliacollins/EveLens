using System;
using System.IO;
using System.Threading.Tasks;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Serialization.Esi;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    /// <summary>
    /// Tests for the CharacterDataCache feature — persisting live ESI data to disk.
    /// Covers: ICharacterDataCache interface, CharacterDataCacheService implementation,
    /// AppServices registration, and round-trip serialization for ESI types.
    /// </summary>
    [Collection("AppServices")]
    public class CharacterDataCacheTests : IDisposable
    {
        private readonly string _tempDir;

        public CharacterDataCacheTests()
        {
            AppServices.Reset();

            // Set up a temp directory so the cache service writes there instead of real AppData
            _tempDir = Path.Combine(Path.GetTempPath(), "EVEMonTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var mockPaths = Substitute.For<IApplicationPaths>();
            mockPaths.DataDirectory.Returns(_tempDir);
            AppServices.SetApplicationPaths(mockPaths);
        }

        public void Dispose()
        {
            AppServices.Reset();
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        #region AppServices Registration

        [Fact]
        public void CharacterDataCache_ReturnsNonNull()
        {
            AppServices.CharacterDataCache.Should().NotBeNull();
            AppServices.CharacterDataCache.Should().BeAssignableTo<ICharacterDataCache>();
        }

        [Fact]
        public void CharacterDataCache_IsSingleton()
        {
            var first = AppServices.CharacterDataCache;
            var second = AppServices.CharacterDataCache;

            first.Should().BeSameAs(second);
        }

        [Fact]
        public void SetCharacterDataCache_OverridesDefault()
        {
            var mock = Substitute.For<ICharacterDataCache>();
            AppServices.SetCharacterDataCache(mock);

            AppServices.CharacterDataCache.Should().BeSameAs(mock);
        }

        [Fact]
        public void Reset_RestoresDefaultCharacterDataCache()
        {
            var mock = Substitute.For<ICharacterDataCache>();
            AppServices.SetCharacterDataCache(mock);

            AppServices.Reset();

            AppServices.CharacterDataCache.Should().NotBeSameAs(mock);
            AppServices.CharacterDataCache.Should().NotBeNull();
        }

        #endregion

        #region SaveAsync / LoadAsync Round-Trip

        [Fact]
        public async Task SaveAsync_ThenLoadAsync_ReturnsEquivalentData()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 12345L;

            var original = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 1000, StandingValue = 5.0, Group = StandingGroup.Factions }
            };

            await cache.SaveAsync(charId, "standings", original);
            var loaded = await cache.LoadAsync<EsiAPIStandings>(charId, "standings");

            loaded.Should().NotBeNull();
            loaded.Should().HaveCount(1);
            loaded![0].ID.Should().Be(1000);
            loaded[0].StandingValue.Should().Be(5.0);
        }

        [Fact]
        public async Task SaveAsync_CreatesFileOnDisk()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 99999L;

            var data = new EsiAPIAssetList
            {
                new EsiAssetListItem { ItemID = 42, TypeID = 100, Quantity = 10 }
            };

            await cache.SaveAsync(charId, "assets", data);

            string expectedPath = Path.Combine(_tempDir, "cache", "characters", "99999", "assets.json");
            File.Exists(expectedPath).Should().BeTrue();
        }

        [Fact]
        public async Task SaveAsync_AtomicWrite_NoTmpFileRemains()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 11111L;

            var data = new EsiAPIContactsList
            {
                new EsiContactListItem { ContactID = 1, Standing = 10.0f }
            };

            await cache.SaveAsync(charId, "contacts", data);

            string tmpPath = Path.Combine(_tempDir, "cache", "characters", "11111", "contacts.json.tmp");
            File.Exists(tmpPath).Should().BeFalse("atomic write should rename .tmp to .json");
        }

        [Fact]
        public async Task SaveAsync_OverwritesExistingFile()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 22222L;

            var first = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 1, StandingValue = 1.0 }
            };
            await cache.SaveAsync(charId, "standings", first);

            var second = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 2, StandingValue = 9.0 }
            };
            await cache.SaveAsync(charId, "standings", second);

            var loaded = await cache.LoadAsync<EsiAPIStandings>(charId, "standings");
            loaded.Should().HaveCount(1);
            loaded![0].ID.Should().Be(2);
            loaded[0].StandingValue.Should().Be(9.0);
        }

        #endregion

        #region LoadAsync Edge Cases

        [Fact]
        public async Task LoadAsync_NoCache_ReturnsNull()
        {
            var cache = AppServices.CharacterDataCache;

            var result = await cache.LoadAsync<EsiAPIStandings>(77777L, "standings");

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoadAsync_CorruptFile_ReturnsNull()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 33333L;

            // Write corrupt JSON manually
            string dir = Path.Combine(_tempDir, "cache", "characters", charId.ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, "standings.json"), "{{not valid json!!");

            var result = await cache.LoadAsync<EsiAPIStandings>(charId, "standings");

            result.Should().BeNull("corrupt cache files should be handled gracefully");
        }

        [Fact]
        public async Task LoadAsync_EmptyFile_ReturnsNull()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 44444L;

            string dir = Path.Combine(_tempDir, "cache", "characters", charId.ToString());
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, "contacts.json"), "");

            var result = await cache.LoadAsync<EsiAPIContactsList>(charId, "contacts");

            result.Should().BeNull();
        }

        [Fact]
        public async Task LoadAsync_WrongEndpointKey_ReturnsNull()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 55555L;

            var data = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 1, StandingValue = 1.0 }
            };
            await cache.SaveAsync(charId, "standings", data);

            var result = await cache.LoadAsync<EsiAPIStandings>(charId, "contacts");

            result.Should().BeNull("loading a different endpoint key should not find data");
        }

        #endregion

        #region ClearCharacterAsync

        [Fact]
        public async Task ClearCharacterAsync_RemovesAllCachedData()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 66666L;

            await cache.SaveAsync(charId, "standings", new EsiAPIStandings());
            await cache.SaveAsync(charId, "contacts", new EsiAPIContactsList());
            await cache.SaveAsync(charId, "assets", new EsiAPIAssetList());

            await cache.ClearCharacterAsync(charId);

            string charDir = Path.Combine(_tempDir, "cache", "characters", charId.ToString());
            Directory.Exists(charDir).Should().BeFalse();
        }

        [Fact]
        public async Task ClearCharacterAsync_DoesNotAffectOtherCharacters()
        {
            var cache = AppServices.CharacterDataCache;
            long charA = 10001L;
            long charB = 10002L;

            await cache.SaveAsync(charA, "standings", new EsiAPIStandings());
            await cache.SaveAsync(charB, "standings", new EsiAPIStandings());

            await cache.ClearCharacterAsync(charA);

            var loadedB = await cache.LoadAsync<EsiAPIStandings>(charB, "standings");
            loadedB.Should().NotBeNull("other character's cache should be untouched");
        }

        [Fact]
        public async Task ClearCharacterAsync_NonExistentCharacter_DoesNotThrow()
        {
            var cache = AppServices.CharacterDataCache;

            Func<Task> act = () => cache.ClearCharacterAsync(99999999L);

            await act.Should().NotThrowAsync();
        }

        #endregion

        #region ClearAllAsync

        [Fact]
        public async Task ClearAllAsync_RemovesAllCachedData()
        {
            var cache = AppServices.CharacterDataCache;

            await cache.SaveAsync(10001L, "standings", new EsiAPIStandings());
            await cache.SaveAsync(10002L, "contacts", new EsiAPIContactsList());

            await cache.ClearAllAsync();

            string root = Path.Combine(_tempDir, "cache", "characters");
            Directory.Exists(root).Should().BeFalse();
        }

        [Fact]
        public async Task ClearAllAsync_WhenEmpty_DoesNotThrow()
        {
            var cache = AppServices.CharacterDataCache;

            Func<Task> act = () => cache.ClearAllAsync();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ClearAllAsync_ThenLoadAsync_ReturnsNull()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 88888L;

            await cache.SaveAsync(charId, "standings", new EsiAPIStandings { new EsiStandingsListItem { ID = 1 } });
            await cache.ClearAllAsync();

            var result = await cache.LoadAsync<EsiAPIStandings>(charId, "standings");
            result.Should().BeNull();
        }

        #endregion

        #region Multiple Endpoint Round-Trips

        [Fact]
        public async Task RoundTrip_Assets()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIAssetList
            {
                new EsiAssetListItem { ItemID = 100, TypeID = 34, Quantity = 5000, LocationID = 60003760 }
            };

            await cache.SaveAsync(charId, "assets", data);
            var loaded = await cache.LoadAsync<EsiAPIAssetList>(charId, "assets");

            loaded.Should().HaveCount(1);
            loaded![0].ItemID.Should().Be(100);
            loaded[0].TypeID.Should().Be(34);
            loaded[0].Quantity.Should().Be(5000);
        }

        [Fact]
        public async Task RoundTrip_Contacts()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIContactsList
            {
                new EsiContactListItem { ContactID = 200, Standing = 5.0f }
            };

            await cache.SaveAsync(charId, "contacts", data);
            var loaded = await cache.LoadAsync<EsiAPIContactsList>(charId, "contacts");

            loaded.Should().HaveCount(1);
            loaded![0].ContactID.Should().Be(200);
            loaded[0].Standing.Should().Be(5.0f);
        }

        [Fact]
        public async Task RoundTrip_KillLog()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIKillLog
            {
                new EsiKillLogListItem { KillID = 999, Hash = "abc123" }
            };

            await cache.SaveAsync(charId, "kill_log", data);
            var loaded = await cache.LoadAsync<EsiAPIKillLog>(charId, "kill_log");

            loaded.Should().HaveCount(1);
            loaded![0].KillID.Should().Be(999);
            loaded[0].Hash.Should().Be("abc123");
        }

        [Fact]
        public async Task RoundTrip_ResearchPoints()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIResearchPoints
            {
                new EsiResearchListItem { AgentID = 500, SkillID = 11450 }
            };

            await cache.SaveAsync(charId, "research", data);
            var loaded = await cache.LoadAsync<EsiAPIResearchPoints>(charId, "research");

            loaded.Should().HaveCount(1);
            loaded![0].AgentID.Should().Be(500);
            loaded[0].SkillID.Should().Be(11450);
        }

        [Fact]
        public async Task RoundTrip_Loyalty()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPILoyality
            {
                new EsiLoyaltyListItem { CorpID = 1000125, LoyaltyPoints = 42000 }
            };

            await cache.SaveAsync(charId, "loyalty", data);
            var loaded = await cache.LoadAsync<EsiAPILoyality>(charId, "loyalty");

            loaded.Should().HaveCount(1);
            loaded![0].CorpID.Should().Be(1000125);
            loaded[0].LoyaltyPoints.Should().Be(42000);
        }

        [Fact]
        public async Task RoundTrip_WalletJournal()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIWalletJournal
            {
                new EsiWalletJournalListItem { ID = 1001, Amount = 5000.50m }
            };

            await cache.SaveAsync(charId, "wallet_journal", data);
            var loaded = await cache.LoadAsync<EsiAPIWalletJournal>(charId, "wallet_journal");

            loaded.Should().HaveCount(1);
            loaded![0].ID.Should().Be(1001);
            loaded[0].Amount.Should().Be(5000.50m);
        }

        [Fact]
        public async Task RoundTrip_WalletTransactions()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIWalletTransactions
            {
                new EsiWalletTransactionsListItem { ID = 2002, Price = 100.0m }
            };

            await cache.SaveAsync(charId, "wallet_transactions", data);
            var loaded = await cache.LoadAsync<EsiAPIWalletTransactions>(charId, "wallet_transactions");

            loaded.Should().HaveCount(1);
            loaded![0].ID.Should().Be(2002);
            loaded[0].Price.Should().Be(100.0m);
        }

        [Fact]
        public async Task RoundTrip_PlanetaryColonies()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIPlanetaryColoniesList
            {
                new EsiPlanetaryColonyListItem { PlanetID = 40001, SolarSystemID = 30000142 }
            };

            await cache.SaveAsync(charId, "planetary", data);
            var loaded = await cache.LoadAsync<EsiAPIPlanetaryColoniesList>(charId, "planetary");

            loaded.Should().HaveCount(1);
            loaded![0].PlanetID.Should().Be(40001);
            loaded[0].SolarSystemID.Should().Be(30000142);
        }

        [Fact]
        public async Task RoundTrip_MarketOrders()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIMarketOrders
            {
                new EsiOrderListItem { OrderID = 3003, ItemID = 34, UnitaryPrice = 10.5m }
            };

            await cache.SaveAsync(charId, "market_orders", data);
            var loaded = await cache.LoadAsync<EsiAPIMarketOrders>(charId, "market_orders");

            loaded.Should().HaveCount(1);
            loaded![0].OrderID.Should().Be(3003);
            loaded[0].ItemID.Should().Be(34);
            loaded[0].UnitaryPrice.Should().Be(10.5m);
        }

        [Fact]
        public async Task RoundTrip_Contracts()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIContracts
            {
                new EsiContractListItem { ContractID = 4004, Type = "item_exchange" }
            };

            await cache.SaveAsync(charId, "contracts", data);
            var loaded = await cache.LoadAsync<EsiAPIContracts>(charId, "contracts");

            loaded.Should().HaveCount(1);
            loaded![0].ContractID.Should().Be(4004);
        }

        [Fact]
        public async Task RoundTrip_IndustryJobs()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIIndustryJobs
            {
                new EsiJobListItem { JobID = 5005, BlueprintTypeID = 1000 }
            };

            await cache.SaveAsync(charId, "industry_jobs", data);
            var loaded = await cache.LoadAsync<EsiAPIIndustryJobs>(charId, "industry_jobs");

            loaded.Should().HaveCount(1);
            loaded![0].JobID.Should().Be(5005);
        }

        [Fact]
        public async Task RoundTrip_Medals()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPIMedals
            {
                new EsiMedalsListItem { MedalID = 6006 }
            };

            await cache.SaveAsync(charId, "medals", data);
            var loaded = await cache.LoadAsync<EsiAPIMedals>(charId, "medals");

            loaded.Should().HaveCount(1);
            loaded![0].MedalID.Should().Be(6006);
        }

        [Fact]
        public async Task RoundTrip_CalendarEvents()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 1L;

            var data = new EsiAPICalendarEvents
            {
                new EsiCalendarEventListItem { EventID = 7007, EventTitle = "Fleet Op" }
            };

            await cache.SaveAsync(charId, "calendar", data);
            var loaded = await cache.LoadAsync<EsiAPICalendarEvents>(charId, "calendar");

            loaded.Should().HaveCount(1);
            loaded![0].EventID.Should().Be(7007);
            loaded[0].EventTitle.Should().Be("Fleet Op");
        }

        #endregion

        #region Multiple Characters Isolation

        [Fact]
        public async Task SaveAsync_MultipleCharacters_DataIsolated()
        {
            var cache = AppServices.CharacterDataCache;

            var dataA = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 1, StandingValue = 1.0 }
            };
            var dataB = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 2, StandingValue = 9.0 }
            };

            await cache.SaveAsync(1001L, "standings", dataA);
            await cache.SaveAsync(1002L, "standings", dataB);

            var loadedA = await cache.LoadAsync<EsiAPIStandings>(1001L, "standings");
            var loadedB = await cache.LoadAsync<EsiAPIStandings>(1002L, "standings");

            loadedA![0].ID.Should().Be(1);
            loadedB![0].ID.Should().Be(2);
        }

        [Fact]
        public async Task SaveAsync_MultipleEndpoints_SameCharacter_Coexist()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 2001L;

            await cache.SaveAsync(charId, "standings", new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 10 }
            });
            await cache.SaveAsync(charId, "contacts", new EsiAPIContactsList
            {
                new EsiContactListItem { ContactID = 20 }
            });
            await cache.SaveAsync(charId, "assets", new EsiAPIAssetList
            {
                new EsiAssetListItem { ItemID = 30 }
            });

            var standings = await cache.LoadAsync<EsiAPIStandings>(charId, "standings");
            var contacts = await cache.LoadAsync<EsiAPIContactsList>(charId, "contacts");
            var assets = await cache.LoadAsync<EsiAPIAssetList>(charId, "assets");

            standings.Should().HaveCount(1);
            contacts.Should().HaveCount(1);
            assets.Should().HaveCount(1);
            standings![0].ID.Should().Be(10);
            contacts![0].ContactID.Should().Be(20);
            assets![0].ItemID.Should().Be(30);
        }

        #endregion

        #region ICharacterDataCache Interface Contract (Mock Tests)

        [Fact]
        public async Task MockCache_SaveAsync_CanBeVerified()
        {
            var mock = Substitute.For<ICharacterDataCache>();
            AppServices.SetCharacterDataCache(mock);

            await AppServices.CharacterDataCache.SaveAsync(123L, "standings", new EsiAPIStandings());

            await mock.Received(1).SaveAsync(123L, "standings", Arg.Any<EsiAPIStandings>());
        }

        [Fact]
        public async Task MockCache_LoadAsync_ReturnsConfiguredValue()
        {
            var mock = Substitute.For<ICharacterDataCache>();
            var expected = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 42 }
            };
            mock.LoadAsync<EsiAPIStandings>(123L, "standings").Returns(expected);
            AppServices.SetCharacterDataCache(mock);

            var result = await AppServices.CharacterDataCache.LoadAsync<EsiAPIStandings>(123L, "standings");

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task MockCache_ClearCharacterAsync_CanBeVerified()
        {
            var mock = Substitute.For<ICharacterDataCache>();
            AppServices.SetCharacterDataCache(mock);

            await AppServices.CharacterDataCache.ClearCharacterAsync(123L);

            await mock.Received(1).ClearCharacterAsync(123L);
        }

        [Fact]
        public async Task MockCache_ClearAllAsync_CanBeVerified()
        {
            var mock = Substitute.For<ICharacterDataCache>();
            AppServices.SetCharacterDataCache(mock);

            await AppServices.CharacterDataCache.ClearAllAsync();

            await mock.Received(1).ClearAllAsync();
        }

        #endregion

        #region Large Data Sets

        [Fact]
        public async Task RoundTrip_LargeAssetList_PreservesAllItems()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 5001L;

            var data = new EsiAPIAssetList();
            for (int i = 0; i < 1000; i++)
            {
                data.Add(new EsiAssetListItem
                {
                    ItemID = i + 1,
                    TypeID = 34 + (i % 10),
                    Quantity = i * 100,
                    LocationID = 60003760
                });
            }

            await cache.SaveAsync(charId, "assets", data);
            var loaded = await cache.LoadAsync<EsiAPIAssetList>(charId, "assets");

            loaded.Should().HaveCount(1000);
            loaded![0].ItemID.Should().Be(1);
            loaded[999].ItemID.Should().Be(1000);
        }

        #endregion

        #region Idempotency

        [Fact]
        public async Task SaveAsync_SameDataTwice_SecondLoadStillWorks()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 7001L;

            var data = new EsiAPIStandings
            {
                new EsiStandingsListItem { ID = 42, StandingValue = 3.5 }
            };

            await cache.SaveAsync(charId, "standings", data);
            await cache.SaveAsync(charId, "standings", data);

            var loaded = await cache.LoadAsync<EsiAPIStandings>(charId, "standings");
            loaded.Should().HaveCount(1);
            loaded![0].ID.Should().Be(42);
        }

        [Fact]
        public async Task ClearCharacterAsync_CalledTwice_DoesNotThrow()
        {
            var cache = AppServices.CharacterDataCache;
            long charId = 7002L;

            await cache.SaveAsync(charId, "standings", new EsiAPIStandings());
            await cache.ClearCharacterAsync(charId);

            Func<Task> act = () => cache.ClearCharacterAsync(charId);
            await act.Should().NotThrowAsync();
        }

        #endregion
    }
}
