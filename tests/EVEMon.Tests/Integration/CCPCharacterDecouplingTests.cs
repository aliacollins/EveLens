using System;
using System.Collections.Generic;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Integration
{
    /// <summary>
    /// Validates that CCPCharacter can be instantiated with NullCharacterServices
    /// without requiring EveMonClient initialization, game data, or API connectivity.
    /// </summary>
    public class CCPCharacterDecouplingTests
    {
        [Fact]
        public void SingleCharacter_CreatesWithNullServices_NoEveMonClient()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(1001, "Test Pilot");

            var character = new CCPCharacter(identity, services);

            character.Should().NotBeNull();
            character.CharacterID.Should().Be(1001);
            character.Name.Should().Be("Test Pilot");
        }

        [Fact]
        public void SingleCharacter_DisposesCleanly()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(1002, "Dispose Test");
            var character = new CCPCharacter(identity, services);

            // Should not throw
            character.Dispose();
        }

        [Fact]
        public void SingleCharacter_LazyCollectionsAccessible()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(1003, "Collection Test");
            var character = new CCPCharacter(identity, services);

            // Access lazy collections — should not throw
            character.Standings.Should().NotBeNull();
            character.Assets.Should().NotBeNull();
            character.CharacterMarketOrders.Should().NotBeNull();
            character.CorporationMarketOrders.Should().NotBeNull();
            character.CharacterContracts.Should().NotBeNull();
            character.CorporationContracts.Should().NotBeNull();
            character.CharacterIndustryJobs.Should().NotBeNull();
            character.CorporationIndustryJobs.Should().NotBeNull();

            character.Dispose();
        }

        [Fact]
        public void HundredCharacters_AllCreateSuccessfully()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 100; i++)
            {
                var identity = new CharacterIdentity(2000 + i, $"Pilot {i}");
                characters.Add(new CCPCharacter(identity, services));
            }

            characters.Should().HaveCount(100);
            characters[0].CharacterID.Should().Be(2000);
            characters[99].CharacterID.Should().Be(2099);
            characters[50].Name.Should().Be("Pilot 50");
        }

        [Fact]
        public void HundredCharacters_AllDisposeCleanly()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 100; i++)
            {
                var identity = new CharacterIdentity(3000 + i, $"Dispose Pilot {i}");
                characters.Add(new CCPCharacter(identity, services));
            }

            // Dispose all — should not throw
            foreach (var c in characters)
            {
                c.Dispose();
            }
        }

        [Fact]
        public void HundredCharacters_WithLazyCollectionAccess_AllWork()
        {
            var services = new NullCharacterServices();
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 100; i++)
            {
                var identity = new CharacterIdentity(4000 + i, $"Full Pilot {i}");
                var character = new CCPCharacter(identity, services);
                characters.Add(character);

                // Access various lazy collections on each character
                character.Standings.Should().NotBeNull();
                character.Assets.Should().NotBeNull();
                character.CharacterMarketOrders.Should().NotBeNull();
            }

            characters.Should().HaveCount(100);

            foreach (var c in characters)
            {
                c.Dispose();
            }
        }

        [Fact]
        public void ServicesProperty_ExposesInjectedServices()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(5001, "Services Test");
            var character = new CCPCharacter(identity, services);

            character.Services.Should().BeSameAs(services);

            character.Dispose();
        }
    }
}
