using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    /// <summary>
    /// Verifies that <see cref="AppServices"/> correctly routes to underlying services,
    /// supports overrides via Set* methods, and restores defaults via Reset().
    /// </summary>
    [Collection("AppServices")]
    public class AppServicesRoutingTests
    {
        public AppServicesRoutingTests()
        {
            // Ensure test isolation: reset all services to default lazy factories
            AppServices.Reset();
        }

        #region Singleton / Identity Tests

        [Fact]
        public void EventAggregator_IsSingleton_ReturnsSameInstanceOnMultipleCalls()
        {
            // Arrange & Act
            var first = AppServices.EventAggregator;
            var second = AppServices.EventAggregator;

            // Assert
            first.Should().NotBeNull();
            first.Should().BeSameAs(second, "Lazy<T> should return the same instance");
        }

        [Fact]
        public void Dispatcher_IsSingleton_ReturnsSameInstanceOnMultipleCalls()
        {
            // Arrange & Act
            var first = AppServices.Dispatcher;
            var second = AppServices.Dispatcher;

            // Assert
            first.Should().NotBeNull();
            first.Should().BeSameAs(second);
        }

        [Fact]
        public void Settings_IsSingleton_ReturnsSameInstanceOnMultipleCalls()
        {
            // Arrange & Act
            var first = AppServices.Settings;
            var second = AppServices.Settings;

            // Assert
            first.Should().NotBeNull();
            first.Should().BeSameAs(second);
        }

        [Fact]
        public void EsiClient_IsSingleton_ReturnsSameInstanceOnMultipleCalls()
        {
            // Arrange & Act
            var first = AppServices.EsiClient;
            var second = AppServices.EsiClient;

            // Assert
            first.Should().NotBeNull();
            first.Should().BeSameAs(second);
        }

        [Fact]
        public void CharacterRepository_IsSingleton_ReturnsSameInstanceOnMultipleCalls()
        {
            // Arrange & Act
            var first = AppServices.CharacterRepository;
            var second = AppServices.CharacterRepository;

            // Assert
            first.Should().NotBeNull();
            first.Should().BeSameAs(second);
        }

        #endregion

        #region SetDispatcher Override Tests

        [Fact]
        public void SetDispatcher_OverridesDefault_ReturnsInjectedInstance()
        {
            // Arrange
            var mock = Substitute.For<IDispatcher>();

            // Act
            AppServices.SetDispatcher(mock);

            // Assert
            AppServices.Dispatcher.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetDispatcher_CalledTwice_SecondOverrideWins()
        {
            // Arrange
            var first = Substitute.For<IDispatcher>();
            var second = Substitute.For<IDispatcher>();

            // Act
            AppServices.SetDispatcher(first);
            AppServices.SetDispatcher(second);

            // Assert
            AppServices.Dispatcher.Should().BeSameAs(second);
        }

        #endregion

        #region SetEventAggregator Override Tests

        [Fact]
        public void SetEventAggregator_OverridesDefault_ReturnsInjectedInstance()
        {
            // Arrange
            var mock = Substitute.For<IEventAggregator>();

            // Act
            AppServices.SetEventAggregator(mock);

            // Assert
            AppServices.EventAggregator.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetEventAggregator_CalledTwice_SecondOverrideWins()
        {
            // Arrange
            var first = Substitute.For<IEventAggregator>();
            var second = Substitute.For<IEventAggregator>();

            // Act
            AppServices.SetEventAggregator(first);
            AppServices.SetEventAggregator(second);

            // Assert
            AppServices.EventAggregator.Should().BeSameAs(second);
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void Reset_RestoresDefaults_DispatcherIsNewInstance()
        {
            // Arrange
            var mock = Substitute.For<IDispatcher>();
            AppServices.SetDispatcher(mock);
            AppServices.Dispatcher.Should().BeSameAs(mock);

            // Act
            AppServices.Reset();

            // Assert
            AppServices.Dispatcher.Should().NotBeSameAs(mock,
                "Reset should create a new default Lazy<IDispatcher>");
            AppServices.Dispatcher.Should().NotBeNull();
        }

        [Fact]
        public void Reset_RestoresDefaults_EventAggregatorIsNewInstance()
        {
            // Arrange
            var mock = Substitute.For<IEventAggregator>();
            AppServices.SetEventAggregator(mock);
            AppServices.EventAggregator.Should().BeSameAs(mock);

            // Act
            AppServices.Reset();

            // Assert
            AppServices.EventAggregator.Should().NotBeSameAs(mock);
            AppServices.EventAggregator.Should().NotBeNull();
        }

        [Fact]
        public void Reset_RestoresDefaults_SettingsIsNewInstance()
        {
            // Arrange
            var mock = Substitute.For<ISettingsProvider>();
            AppServices.SetSettings(mock);
            AppServices.Settings.Should().BeSameAs(mock);

            // Act
            AppServices.Reset();

            // Assert
            AppServices.Settings.Should().NotBeSameAs(mock);
            AppServices.Settings.Should().NotBeNull();
        }

        [Fact]
        public void Reset_RestoresDefaults_EsiClientIsNewInstance()
        {
            // Arrange
            var mock = Substitute.For<IEsiClient>();
            AppServices.SetEsiClient(mock);
            AppServices.EsiClient.Should().BeSameAs(mock);

            // Act
            AppServices.Reset();

            // Assert
            AppServices.EsiClient.Should().NotBeSameAs(mock);
            AppServices.EsiClient.Should().NotBeNull();
        }

        [Fact]
        public void Reset_RestoresDefaults_CharacterRepositoryIsNewInstance()
        {
            // Arrange
            var mock = Substitute.For<ICharacterRepository>();
            AppServices.SetCharacterRepository(mock);
            AppServices.CharacterRepository.Should().BeSameAs(mock);

            // Act
            AppServices.Reset();

            // Assert
            AppServices.CharacterRepository.Should().NotBeSameAs(mock);
            AppServices.CharacterRepository.Should().NotBeNull();
        }

        #endregion

        #region Interface Conformance Tests

        [Fact]
        public void Dispatcher_ReturnsNonNull_ImplementsIDispatcher()
        {
            // Act
            var dispatcher = AppServices.Dispatcher;

            // Assert
            dispatcher.Should().NotBeNull();
            dispatcher.Should().BeAssignableTo<IDispatcher>();
        }

        [Fact]
        public void EventAggregator_ReturnsNonNull_ImplementsIEventAggregator()
        {
            // Act
            var aggregator = AppServices.EventAggregator;

            // Assert
            aggregator.Should().NotBeNull();
            aggregator.Should().BeAssignableTo<IEventAggregator>();
        }

        [Fact]
        public void Settings_ReturnsNonNull_ImplementsISettingsProvider()
        {
            // Act
            var settings = AppServices.Settings;

            // Assert
            settings.Should().NotBeNull();
            settings.Should().BeAssignableTo<ISettingsProvider>();
        }

        [Fact]
        public void EsiClient_ReturnsNonNull_ImplementsIEsiClient()
        {
            // Act
            var esiClient = AppServices.EsiClient;

            // Assert
            esiClient.Should().NotBeNull();
            esiClient.Should().BeAssignableTo<IEsiClient>();
        }

        [Fact]
        public void CharacterRepository_ReturnsNonNull_ImplementsICharacterRepository()
        {
            // Act
            var repo = AppServices.CharacterRepository;

            // Assert
            repo.Should().NotBeNull();
            repo.Should().BeAssignableTo<ICharacterRepository>();
        }

        [Fact]
        public void TraceService_ReturnsNonNull_ImplementsITraceService()
        {
            // Act
            var trace = AppServices.TraceService;

            // Assert
            trace.Should().NotBeNull();
            trace.Should().BeAssignableTo<ITraceService>();
        }

        [Fact]
        public void TraceService_DefaultInstance_IsTraceService()
        {
            // Act
            var trace = AppServices.TraceService;

            // Assert — the default should be the standalone TraceService, not the legacy adapter
            trace.Should().BeOfType<TraceService>(
                "AppServices should now use the standalone TraceService, not TraceServiceAdapter");
        }

        [Fact]
        public void ApplicationPaths_ReturnsNonNull_ImplementsIApplicationPaths()
        {
            // Act
            var paths = AppServices.ApplicationPaths;

            // Assert
            paths.Should().NotBeNull();
            paths.Should().BeAssignableTo<IApplicationPaths>();
        }

        [Fact]
        public void ResourceProvider_ReturnsNonNull_ImplementsIResourceProvider()
        {
            // Act
            var provider = AppServices.ResourceProvider;

            // Assert
            provider.Should().NotBeNull();
            provider.Should().BeAssignableTo<IResourceProvider>();
        }

        #endregion

        #region LoggerFactory Tests

        [Fact]
        public void LoggerFactory_ReturnsNonNull_ImplementsILoggerFactory()
        {
            // Act
            var factory = AppServices.LoggerFactory;

            // Assert
            factory.Should().NotBeNull();
            factory.Should().BeAssignableTo<Microsoft.Extensions.Logging.ILoggerFactory>();
        }

        [Fact]
        public void Reset_RecreatesLoggerFactory()
        {
            // Arrange
            var first = AppServices.LoggerFactory;
            first.Should().NotBeNull();

            // Act
            AppServices.Reset();
            var second = AppServices.LoggerFactory;

            // Assert
            second.Should().NotBeNull();
            second.Should().NotBeSameAs(first, "Reset should create a new LoggerFactory instance");
        }

        #endregion

        #region Additional Override Tests

        [Fact]
        public void SetTraceService_OverridesDefault()
        {
            // Arrange
            var mock = Substitute.For<ITraceService>();

            // Act
            AppServices.SetTraceService(mock);

            // Assert
            AppServices.TraceService.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetApplicationPaths_OverridesDefault()
        {
            // Arrange
            var mock = Substitute.For<IApplicationPaths>();

            // Act
            AppServices.SetApplicationPaths(mock);

            // Assert
            AppServices.ApplicationPaths.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetResourceProvider_OverridesDefault()
        {
            // Arrange
            var mock = Substitute.For<IResourceProvider>();

            // Act
            AppServices.SetResourceProvider(mock);

            // Assert
            AppServices.ResourceProvider.Should().BeSameAs(mock);
        }

        #endregion
    }
}
