// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.QueryMonitor;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Integration tests for CharacterQueryOrchestrator.
    /// Tests interface implementations and lifecycle.
    /// </summary>
    public class CharacterQueryOrchestratorIntegrationTests
    {

        #region Interface Implementation Tests

        [Fact]
        public void CQO_ImplementsICharacterDataQuerying()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            orchestrator.Should().BeAssignableTo<ICharacterDataQuerying>(
                "CQO must implement ICharacterDataQuerying to replace CDQ");
        }

        [Fact]
        public void CQO_ImplementsICharacterQueryManager()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            orchestrator.Should().BeAssignableTo<ICharacterQueryManager>(
                "CQO must implement ICharacterQueryManager for scheduling");
        }

        [Fact]
        public void CQO_ImplementsIDisposable()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            orchestrator.Should().BeAssignableTo<IDisposable>(
                "CQO must implement IDisposable for proper cleanup");
        }

        #endregion

        #region ICharacterDataQuerying Property Tests (Test Mode)

        [Fact]
        public void HasCharacterSheetError_InTestMode_ReturnsFalse()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            // In test mode, CharacterSheetMonitor is null, so HasCharacterSheetError defaults to false
            orchestrator.HasCharacterSheetError.Should().BeFalse(
                "no real monitor exists in test mode");
        }

        [Fact]
        public void CharacterMarketOrdersQueried_InTestMode_ReturnsTrue()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            // In test mode, m_charMarketOrdersMonitor is null, defaults to true
            orchestrator.CharacterMarketOrdersQueried.Should().BeTrue(
                "no real monitor exists in test mode, defaults to true");
        }

        [Fact]
        public void CharacterContractsQueried_InTestMode_ReturnsTrue()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            orchestrator.CharacterContractsQueried.Should().BeTrue(
                "no real monitor exists in test mode, defaults to true");
        }

        [Fact]
        public void CharacterIndustryJobsQueried_InTestMode_ReturnsTrue()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            orchestrator.CharacterIndustryJobsQueried.Should().BeTrue(
                "no real monitor exists in test mode, defaults to true");
        }

        #endregion

        #region Dual Interface Tests

        [Fact]
        public void CQO_SameInstance_ServesAsBothInterfaces()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            // The same instance can be used as both interfaces
            ICharacterDataQuerying dataQuerying = orchestrator;
            ICharacterQueryManager queryManager = orchestrator;

            dataQuerying.Should().BeSameAs(queryManager,
                "both interfaces should be on the same object, not wrapped");
        }

        [Fact]
        public void CQO_ProcessTick_SharedBetweenInterfaces()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            // ProcessTick from ICharacterDataQuerying should be callable
            ICharacterDataQuerying asDataQuerying = orchestrator;

            // Should be callable without exceptions
            asDataQuerying.ProcessTick();
        }

        [Fact]
        public void CQO_Dispose_SharedBetweenInterfaces()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            // Dispose from ICharacterDataQuerying (IDisposable) should work
            ICharacterDataQuerying asDataQuerying = orchestrator;
            asDataQuerying.Dispose();

            // After disposal, further ProcessTick should be no-op
            orchestrator.ProcessTick();
            orchestrator.ActiveMonitorCount.Should().Be(0);
        }

        #endregion

        #region Test Mode vs Production Mode Tests

        [Fact]
        public void TestConstructor_SetsTestMode()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            using var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            // Test mode creates 3 abstract monitors
            orchestrator.ActiveMonitorCount.Should().Be(3,
                "test constructor should create 3 basic feature monitors");

            // Test mode properties default safely
            orchestrator.HasCharacterSheetError.Should().BeFalse();
            orchestrator.CharacterMarketOrdersQueried.Should().BeTrue();
            orchestrator.CharacterContractsQueried.Should().BeTrue();
            orchestrator.CharacterIndustryJobsQueried.Should().BeTrue();
        }

        #endregion

        #region Lifecycle Tests

        [Fact]
        public void CQO_DisposeTwice_DoesNotThrow()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            Action act = () =>
            {
                orchestrator.Dispose();
                orchestrator.Dispose();
            };

            act.Should().NotThrow("double dispose should be safe");
        }

        [Fact]
        public void CQO_ProcessTickAfterDispose_IsNoOp()
        {
            var scheduler = Substitute.For<IEsiScheduler>();
            var esiClient = Substitute.For<IEsiClient>();
            var events = Substitute.For<IEventAggregator>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            var orchestrator = new CharacterQueryOrchestrator(
                scheduler, esiClient, events, 12345L, "Test Char");

            orchestrator.Dispose();

            Action act = () => orchestrator.ProcessTick();
            act.Should().NotThrow("ProcessTick should be safe after disposal");
        }

        #endregion
    }
}
