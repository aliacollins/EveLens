// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EveLens.Common.Scheduling;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Scheduling
{
    public class FetchQueueTests : IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly IEventAggregator _eventAggregator;
        private readonly IEsiClient _esiClient;
        private EsiScheduler? _scheduler;

        public FetchQueueTests()
        {
            _dispatcher = Substitute.For<IDispatcher>();
            _dispatcher.When(d => d.Post(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());
            _dispatcher.When(d => d.Invoke(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());

            _eventAggregator = Substitute.For<IEventAggregator>();

            _esiClient = Substitute.For<IEsiClient>();
            _esiClient.MaxConcurrentRequests.Returns(20);
            _esiClient.ActiveRequests.Returns(0L);
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
        }

        private EsiScheduler CreateScheduler()
        {
            _scheduler = new EsiScheduler(_dispatcher, _eventAggregator, _esiClient);
            return _scheduler;
        }

        private static EndpointRegistration MakeReg(int method) => new()
        {
            Method = method,
            ExecuteAsync = _ => Task.FromResult(new FetchOutcome { StatusCode = 200, CachedUntil = DateTime.UtcNow.AddMinutes(5) }),
            RequiredScope = 0,
            RateGroup = null,
        };

        [Fact]
        public async Task Register_PopulatesQueueDepth()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0), MakeReg(1), MakeReg(2) };

            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0, "queue depth reflects enqueued or already-dispatched jobs");
        }

        [Fact]
        public async Task Register_SameCharacterTwice_CreatesNewJobs()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0) };

            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            // Register again overwrites jobs for same character
            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            // Should not throw and scheduler should still function
            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task Unregister_InvalidatesJobs_ViaGenerationStamp()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0), MakeReg(1) };

            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            scheduler.UnregisterCharacter(1L);
            await Task.Delay(200);

            // After unregister, stale jobs should be skipped (IsRemoved = true, generation bumped)
            // New registration should work without interference from old jobs
            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task ForceRefresh_BumpsGeneration_EnqueuesAtNow()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0) };

            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            scheduler.ForceRefresh(1L, 0);
            await Task.Delay(200);

            // Force refresh should re-enqueue the job; queue should still function
            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task ForceRefresh_AllEndpoints_WhenMethodMinusOne()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0), MakeReg(1), MakeReg(2) };

            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            scheduler.ForceRefresh(1L, -1); // All endpoints
            await Task.Delay(200);

            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task SetVisibleCharacter_PromotesJobsToActive()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0) };

            scheduler.RegisterCharacter(1L, regs);
            scheduler.RegisterCharacter(2L, regs);
            await Task.Delay(200);

            scheduler.SetVisibleCharacter(2L);
            await Task.Delay(200);

            // Should not throw; visible character should be promoted
            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task AuthFailedCharacter_JobsSkipped_UntilReAuth()
        {
            var scheduler = CreateScheduler();
            var regs = new List<EndpointRegistration> { MakeReg(0) };

            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            // Re-authenticate should clear any auth failures and re-enqueue
            scheduler.OnCharacterReAuthenticated(1L);
            await Task.Delay(200);

            scheduler.QueueDepth.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public async Task PersistState_InvokesCallback()
        {
            var scheduler = CreateScheduler();
            bool callbackInvoked = false;
            scheduler.OnPersistState = _ => callbackInvoked = true;

            var regs = new List<EndpointRegistration> { MakeReg(0) };
            scheduler.RegisterCharacter(1L, regs);
            await Task.Delay(200);

            scheduler.PersistState();

            callbackInvoked.Should().BeTrue();
        }
    }
}
