using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Integration
{
    public class LargeScaleStartupTests : IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly IEsiClient _esiClient;
        private Action? _scheduledCallback;
        private SmartQueryScheduler? _scheduler;

        public LargeScaleStartupTests()
        {
            _dispatcher = Substitute.For<IDispatcher>();
            _esiClient = Substitute.For<IEsiClient>();
            _esiClient.MaxConcurrentRequests.Returns(20);
            _esiClient.ActiveRequests.Returns(0L);

            _dispatcher.When(d => d.Schedule(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
                .Do(ci => _scheduledCallback = ci.ArgAt<Action>(1));
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
        }

        private void SimulateTick()
        {
            _scheduledCallback?.Invoke();
        }

        private void SimulateTicks(int count)
        {
            for (int i = 0; i < count; i++)
                SimulateTick();
        }

        private static IScheduledQueryable CreateQueryable(long characterId, int consecutiveNotModified = 0)
        {
            var queryable = Substitute.For<IScheduledQueryable>();
            queryable.CharacterID.Returns(characterId);
            queryable.IsStartupComplete.Returns(true);
            queryable.ConsecutiveNotModifiedCount.Returns(consecutiveNotModified);
            return queryable;
        }

        private static int GetProcessTickCount(IScheduledQueryable queryable)
        {
            return queryable.ReceivedCalls()
                .Count(c => c.GetMethodInfo().Name == "ProcessTick");
        }

        // --- Test 1: 70 characters all register successfully ---

        [Fact]
        public void Scheduler_Handles70Characters_AllRegistered()
        {
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(0);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);

            var queryables = new List<IScheduledQueryable>();
            for (int i = 0; i < 70; i++)
            {
                var q = CreateQueryable(i + 1);
                _scheduler.Register(q);
                queryables.Add(q);
            }

            _scheduler.RegisteredCount.Should().Be(70);
        }

        // --- Test 2: Staggered startup prevents thundering herd ---

        [Fact]
        public void Scheduler_70Characters_StaggeredStartup_NotAllProcessedOnFirstTick()
        {
            // Use max random delay so each entry gets index*75 + 250 ms delay
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(250);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);

            var queryables = new List<IScheduledQueryable>();
            for (int i = 0; i < 70; i++)
            {
                var q = CreateQueryable(i + 1);
                _scheduler.Register(q);
                queryables.Add(q);
            }

            // No visible character set -- all are background
            // First tick: elapsed = 5000ms
            // Entry[i] delay = i*75 + 250.
            // Entry[0] delay = 250ms (5000 >= 250, eligible)
            // Entry[66] delay = 66*75+250 = 5200ms (5000 < 5200, NOT eligible)
            // So entries 0..65 are eligible but only 1 background per tick
            SimulateTick();

            int processedCount = queryables.Count(q => GetProcessTickCount(q) > 0);

            // Only 1 background character should be processed per tick (round-robin)
            processedCount.Should().Be(1,
                "staggered startup + round-robin should process only 1 background per tick");
        }

        // --- Test 3: All 70 characters eventually get processed ---

        [Fact]
        public void Scheduler_70Characters_AllEventuallyProcessed()
        {
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(0);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);

            var queryables = new List<IScheduledQueryable>();
            for (int i = 0; i < 70; i++)
            {
                var q = CreateQueryable(i + 1);
                _scheduler.Register(q);
                queryables.Add(q);
            }

            // Set character 1 as visible
            _scheduler.SetVisibleCharacter(1);

            // Run enough ticks for all background chars to be processed
            // With zero delays: 69 background chars, 1 per tick round-robin
            // Plus staggered startup: entry[i] needs i*75 ms elapsed.
            // entry[69] needs 69*75 = 5175ms. Tick 1 = 5000ms < 5175, tick 2 = 10000 >= 5175
            // So by tick 70 at minimum we should have cycled through all
            SimulateTicks(140);

            // Visible character gets every tick
            GetProcessTickCount(queryables[0]).Should().Be(140);

            // Every background character should be processed at least once
            for (int i = 1; i < 70; i++)
            {
                GetProcessTickCount(queryables[i]).Should().BeGreaterThan(0,
                    $"background character {i + 1} should be processed at least once in 140 ticks");
            }
        }

        // --- Test 4: Factory handles 100 characters ---

        [Fact]
        public void Factory_Creates100Characters_TracksAll()
        {
            var repo = Substitute.For<ICharacterRepository>();
            var events = Substitute.For<IEventAggregator>();
            var factory = new CharacterFactory(repo, events);

            for (int i = 0; i < 100; i++)
            {
                var identity = Substitute.For<ICharacterIdentity>();
                identity.CharacterID.Returns((long)(i + 1));
                identity.Name.Returns($"Char {i}");
                identity.Guid.Returns(Guid.NewGuid());
                factory.CreateNew(identity);
            }

            factory.ManagedCount.Should().Be(100);
        }

        // --- Test 5: EventAggregator handles 100 rapid publishes ---

        [Fact]
        public void EventAggregator_100RapidPublishes_AllDelivered()
        {
            var aggregator = new EventAggregator();
            int received = 0;
            aggregator.Subscribe<CharacterUpdatedEvent>(e => received++);

            for (int i = 0; i < 100; i++)
                aggregator.Publish(new CharacterUpdatedEvent(i, $"Char{i}"));

            received.Should().Be(100);
        }

        // --- Test 6: Factory publishes creation events for each character ---

        [Fact]
        public void Factory_Creates70Characters_PublishesAllEvents()
        {
            var repo = Substitute.For<ICharacterRepository>();
            var aggregator = new EventAggregator();
            var factory = new CharacterFactory(repo, aggregator);

            int createdCount = 0;
            aggregator.Subscribe<CharacterCreatedEvent>(e => createdCount++);

            for (int i = 0; i < 70; i++)
            {
                var identity = Substitute.For<ICharacterIdentity>();
                identity.CharacterID.Returns((long)(i + 1));
                identity.Name.Returns($"Char {i}");
                identity.Guid.Returns(Guid.NewGuid());
                factory.CreateNew(identity);
            }

            createdCount.Should().Be(70);
            factory.ManagedCount.Should().Be(70);
        }

        // --- Test 7: Scheduler + unregister at scale ---

        [Fact]
        public void Scheduler_RegisterAndUnregister70Characters_CountTracksCorrectly()
        {
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(0);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);

            var queryables = new List<IScheduledQueryable>();
            for (int i = 0; i < 70; i++)
            {
                var q = CreateQueryable(i + 1);
                _scheduler.Register(q);
                queryables.Add(q);
            }

            _scheduler.RegisteredCount.Should().Be(70);

            // Unregister half
            for (int i = 0; i < 35; i++)
                _scheduler.Unregister(queryables[i]);

            _scheduler.RegisteredCount.Should().Be(35);

            // Unregister remaining
            for (int i = 35; i < 70; i++)
                _scheduler.Unregister(queryables[i]);

            _scheduler.RegisteredCount.Should().Be(0);
        }
    }
}
