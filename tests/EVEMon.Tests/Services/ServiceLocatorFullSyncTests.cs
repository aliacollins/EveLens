// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Reflection;
using EVEMon.Common.Services;
using EVEMon.Core;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    /// <summary>
    /// Full sync verification tests for ServiceLocator.
    /// Complements ServiceLocatorSyncTests which tests individual properties.
    /// These tests verify structural correctness: all 11 ServiceLocator properties
    /// are synced by SyncToServiceLocator(), and the property set matches expectations.
    /// </summary>
    [Collection("AppServices")]
    public class ServiceLocatorFullSyncTests
    {
        public ServiceLocatorFullSyncTests()
        {
            AppServices.Reset();
            ClearServiceLocator();
        }

        private static void ClearServiceLocator()
        {
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
            ServiceLocator.ResourceProvider = null!;
        }

        #region AllServiceLocatorProperties_MatchAppServices

        [Fact]
        public void SyncToServiceLocator_PopulatesAll11Properties()
        {
            ClearServiceLocator();

            AppServices.SyncToServiceLocator();

            // Verify all 11 properties are non-null after sync
            ServiceLocator.TraceService.Should().NotBeNull("TraceService should be synced");
            ServiceLocator.ApplicationPaths.Should().NotBeNull("ApplicationPaths should be synced");
            ServiceLocator.NameResolver.Should().NotBeNull("NameResolver should be synced");
            ServiceLocator.StationResolver.Should().NotBeNull("StationResolver should be synced");
            ServiceLocator.FlagResolver.Should().NotBeNull("FlagResolver should be synced");
            ServiceLocator.ImageService.Should().NotBeNull("ImageService should be synced");
            ServiceLocator.NotificationTypeResolver.Should().NotBeNull("NotificationTypeResolver should be synced");
            ServiceLocator.EventAggregator.Should().NotBeNull("EventAggregator should be synced");
            ServiceLocator.Dispatcher.Should().NotBeNull("Dispatcher should be synced");
            ServiceLocator.CharacterRepository.Should().NotBeNull("CharacterRepository should be synced");
            ServiceLocator.ResourceProvider.Should().NotBeNull("ResourceProvider should be synced");
        }

        [Fact]
        public void ServiceLocator_HasExactly11PublicStaticProperties()
        {
            // Verify the expected count of properties on ServiceLocator,
            // so that if a new property is added, this test catches the missing sync.
            var properties = typeof(ServiceLocator).GetProperties(
                BindingFlags.Public | BindingFlags.Static);

            properties.Should().HaveCount(11,
                "ServiceLocator should have exactly 11 public static properties. " +
                "If you added a new one, update SyncToServiceLocator() in AppServices.cs " +
                "and add a test for it.");
        }

        #endregion

        #region SyncToServiceLocator_UsesAppServicesInstances

        [Fact]
        public void SyncToServiceLocator_TraceService_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.TraceService.Should().BeSameAs(AppServices.TraceService);
        }

        [Fact]
        public void SyncToServiceLocator_ApplicationPaths_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.ApplicationPaths.Should().BeSameAs(AppServices.ApplicationPaths);
        }

        [Fact]
        public void SyncToServiceLocator_NameResolver_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.NameResolver.Should().BeSameAs(AppServices.NameResolver);
        }

        [Fact]
        public void SyncToServiceLocator_StationResolver_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.StationResolver.Should().BeSameAs(AppServices.StationResolver);
        }

        [Fact]
        public void SyncToServiceLocator_FlagResolver_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.FlagResolver.Should().BeSameAs(AppServices.FlagResolver);
        }

        [Fact]
        public void SyncToServiceLocator_ImageService_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.ImageService.Should().BeSameAs(AppServices.ImageService);
        }

        [Fact]
        public void SyncToServiceLocator_NotificationTypeResolver_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.NotificationTypeResolver.Should().BeSameAs(AppServices.NotificationTypeResolver);
        }

        [Fact]
        public void SyncToServiceLocator_EventAggregator_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.EventAggregator.Should().BeSameAs(AppServices.EventAggregator);
        }

        [Fact]
        public void SyncToServiceLocator_Dispatcher_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.Dispatcher.Should().BeSameAs(AppServices.Dispatcher);
        }

        [Fact]
        public void SyncToServiceLocator_CharacterRepository_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.CharacterRepository.Should().BeSameAs(AppServices.CharacterRepository);
        }

        [Fact]
        public void SyncToServiceLocator_ResourceProvider_IsSameInstanceAsAppServices()
        {
            AppServices.SyncToServiceLocator();

            ServiceLocator.ResourceProvider.Should().BeSameAs(AppServices.ResourceProvider);
        }

        #endregion

        #region SyncToServiceLocator_OverwritesPreviousValues

        [Fact]
        public void SyncToServiceLocator_OverwritesPreviouslySetValues()
        {
            // Set up a mock first
            var mockResolver = Substitute.For<INameResolver>();
            ServiceLocator.NameResolver = mockResolver;
            ServiceLocator.NameResolver.Should().BeSameAs(mockResolver);

            // Now sync - should overwrite with AppServices value
            AppServices.SyncToServiceLocator();

            ServiceLocator.NameResolver.Should().NotBeSameAs(mockResolver);
            ServiceLocator.NameResolver.Should().BeSameAs(AppServices.NameResolver);
        }

        #endregion

        #region SyncToServiceLocator_CalledTwice_IsIdempotent

        [Fact]
        public void SyncToServiceLocator_CalledTwice_ProducesIdenticalState()
        {
            AppServices.SyncToServiceLocator();

            var trace1 = ServiceLocator.TraceService;
            var paths1 = ServiceLocator.ApplicationPaths;
            var names1 = ServiceLocator.NameResolver;

            AppServices.SyncToServiceLocator();

            ServiceLocator.TraceService.Should().BeSameAs(trace1);
            ServiceLocator.ApplicationPaths.Should().BeSameAs(paths1);
            ServiceLocator.NameResolver.Should().BeSameAs(names1);
        }

        #endregion

        #region AppServices.Reset_ClearsLazyInstances

        [Fact]
        public void AppServices_Reset_NewSyncProducesNewInstances()
        {
            AppServices.SyncToServiceLocator();
            var eventAgg1 = ServiceLocator.EventAggregator;

            AppServices.Reset();
            AppServices.SyncToServiceLocator();

            // After Reset, new lazy instances are created
            ServiceLocator.EventAggregator.Should().NotBeSameAs(eventAgg1);
        }

        #endregion

        #region All ServiceLocator Properties Are Interface Types

        [Fact]
        public void ServiceLocator_AllProperties_AreInterfaceTypes()
        {
            var properties = typeof(ServiceLocator).GetProperties(
                BindingFlags.Public | BindingFlags.Static);

            foreach (var prop in properties)
            {
                prop.PropertyType.IsInterface.Should().BeTrue(
                    $"ServiceLocator.{prop.Name} should be an interface type, but is {prop.PropertyType.Name}");
            }
        }

        #endregion

        #region All ServiceLocator Properties Have Public Getter and Setter

        [Fact]
        public void ServiceLocator_AllProperties_HavePublicGetterAndSetter()
        {
            var properties = typeof(ServiceLocator).GetProperties(
                BindingFlags.Public | BindingFlags.Static);

            foreach (var prop in properties)
            {
                prop.GetMethod.Should().NotBeNull($"{prop.Name} should have a getter");
                prop.GetMethod!.IsPublic.Should().BeTrue($"{prop.Name} getter should be public");
                prop.SetMethod.Should().NotBeNull($"{prop.Name} should have a setter");
                prop.SetMethod!.IsPublic.Should().BeTrue($"{prop.Name} setter should be public");
            }
        }

        #endregion
    }
}
