using EVEMon.Common.Services;
using EVEMon.Core;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Services
{
    /// <summary>
    /// Verifies that AppServices.SyncToServiceLocator() populates the Core ServiceLocator
    /// with non-null implementations for all registered services.
    /// </summary>
    public class ServiceLocatorSyncTests
    {
        public ServiceLocatorSyncTests()
        {
            AppServices.Reset();
            // Clear ServiceLocator so each test starts clean
            ServiceLocator.TraceService = null!;
            ServiceLocator.ApplicationPaths = null!;
            ServiceLocator.NameResolver = null!;
            ServiceLocator.StationResolver = null!;
            ServiceLocator.FlagResolver = null!;
            ServiceLocator.ImageService = null!;
            ServiceLocator.NotificationTypeResolver = null!;
            ServiceLocator.EventAggregator = null!;
            ServiceLocator.Dispatcher = null!;
            ServiceLocator.CharacterRepository = null!;
        }

        [Fact]
        public void SyncToServiceLocator_NameResolver_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.NameResolver.Should().NotBeNull();
            ServiceLocator.NameResolver.Should().BeAssignableTo<INameResolver>();
        }

        [Fact]
        public void SyncToServiceLocator_StationResolver_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.StationResolver.Should().NotBeNull();
            ServiceLocator.StationResolver.Should().BeAssignableTo<IStationResolver>();
        }

        [Fact]
        public void SyncToServiceLocator_FlagResolver_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.FlagResolver.Should().NotBeNull();
            ServiceLocator.FlagResolver.Should().BeAssignableTo<IFlagResolver>();
        }

        [Fact]
        public void SyncToServiceLocator_TraceService_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.TraceService.Should().NotBeNull();
            ServiceLocator.TraceService.Should().BeAssignableTo<ITraceService>();
        }

        [Fact]
        public void SyncToServiceLocator_ApplicationPaths_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.ApplicationPaths.Should().NotBeNull();
            ServiceLocator.ApplicationPaths.Should().BeAssignableTo<IApplicationPaths>();
        }

        [Fact]
        public void SyncToServiceLocator_ImageService_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.ImageService.Should().NotBeNull();
            ServiceLocator.ImageService.Should().BeAssignableTo<IImageService>();
        }

        [Fact]
        public void SyncToServiceLocator_NotificationTypeResolver_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.NotificationTypeResolver.Should().NotBeNull();
            ServiceLocator.NotificationTypeResolver.Should().BeAssignableTo<INotificationTypeResolver>();
        }

        [Fact]
        public void SyncToServiceLocator_EventAggregator_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.EventAggregator.Should().NotBeNull();
            ServiceLocator.EventAggregator.Should().BeAssignableTo<IEventAggregator>();
        }

        [Fact]
        public void SyncToServiceLocator_Dispatcher_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.Dispatcher.Should().NotBeNull();
            ServiceLocator.Dispatcher.Should().BeAssignableTo<IDispatcher>();
        }

        [Fact]
        public void SyncToServiceLocator_CharacterRepository_IsNotNull()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.CharacterRepository.Should().NotBeNull();
            ServiceLocator.CharacterRepository.Should().BeAssignableTo<ICharacterRepository>();
        }

        [Fact]
        public void BeforeSync_ServiceLocatorProperties_AreNull()
        {
            // Verify that before SyncToServiceLocator, the ServiceLocator is empty
            ServiceLocator.NameResolver.Should().BeNull();
            ServiceLocator.StationResolver.Should().BeNull();
            ServiceLocator.FlagResolver.Should().BeNull();
            ServiceLocator.TraceService.Should().BeNull();
        }
    }
}
