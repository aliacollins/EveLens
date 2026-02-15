using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    [Collection("AppServices")]
    public class AppServicesTests
    {
        public AppServicesTests()
        {
            // Reset to defaults before each test
            AppServices.Reset();
        }

        [Fact]
        public void Dispatcher_ReturnsNonNull()
        {
            AppServices.Dispatcher.Should().NotBeNull();
            AppServices.Dispatcher.Should().BeAssignableTo<IDispatcher>();
        }

        [Fact]
        public void Settings_ReturnsNonNull()
        {
            AppServices.Settings.Should().NotBeNull();
            AppServices.Settings.Should().BeAssignableTo<ISettingsProvider>();
        }

        [Fact]
        public void EsiClient_ReturnsNonNull()
        {
            AppServices.EsiClient.Should().NotBeNull();
            AppServices.EsiClient.Should().BeAssignableTo<IEsiClient>();
        }

        [Fact]
        public void EventAggregator_ReturnsNonNull()
        {
            AppServices.EventAggregator.Should().NotBeNull();
            AppServices.EventAggregator.Should().BeAssignableTo<IEventAggregator>();
        }

        [Fact]
        public void CharacterRepository_ReturnsNonNull()
        {
            AppServices.CharacterRepository.Should().NotBeNull();
            AppServices.CharacterRepository.Should().BeAssignableTo<ICharacterRepository>();
        }

        [Fact]
        public void SetDispatcher_OverridesDefault()
        {
            // Arrange
            var mock = Substitute.For<IDispatcher>();

            // Act
            AppServices.SetDispatcher(mock);

            // Assert
            AppServices.Dispatcher.Should().BeSameAs(mock);
        }

        [Fact]
        public void Reset_ClearsOverrides()
        {
            // Arrange
            var mock = Substitute.For<IDispatcher>();
            AppServices.SetDispatcher(mock);

            // Act
            AppServices.Reset();

            // Assert
            AppServices.Dispatcher.Should().NotBeSameAs(mock);
        }
    }
}
