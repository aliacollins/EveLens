// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVEMon.Common.Scheduling;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Scheduling
{
    public class ColdStartPlannerTests
    {
        private static EndpointRegistration MakeReg(int method, string? rateGroup = null) => new()
        {
            Method = method,
            ExecuteAsync = _ => Task.FromResult(new FetchOutcome { StatusCode = 200, CachedUntil = DateTime.UtcNow.AddMinutes(5) }),
            RequiredScope = 0,
            RateGroup = rateGroup,
        };

        [Fact]
        public void Plan_VisibleChar_Phase1EndpointsScheduledFirst()
        {
            var regs = new List<EndpointRegistration>
            {
                MakeReg(0),  // CharSheet — phase 1
                MakeReg(14), // MarketOrders — phase 3
                MakeReg(30), // Other — phase 4
            };

            var plan = ColdStartPlanner.Plan(1L, 0, isVisible: true, regs, null);

            plan.Should().HaveCount(3);

            // Phase 1 (method 0) should have earliest due time
            var phase1Due = plan.First(p => p.Job.EndpointMethod == 0).DueTime;
            var phase3Due = plan.First(p => p.Job.EndpointMethod == 14).DueTime;
            var phase4Due = plan.First(p => p.Job.EndpointMethod == 30).DueTime;

            phase1Due.Should().BeBefore(phase3Due);
            phase3Due.Should().BeBefore(phase4Due);
        }

        [Fact]
        public void Plan_AllCharsStaggered_NoTwoAtSameTime()
        {
            var regs = new List<EndpointRegistration> { MakeReg(4) }; // Phase 2 endpoint

            var plan0 = ColdStartPlanner.Plan(1L, 0, false, regs, null);
            var plan1 = ColdStartPlanner.Plan(2L, 1, false, regs, null);
            var plan2 = ColdStartPlanner.Plan(3L, 2, false, regs, null);

            // Different character indices produce different due times
            var times = new[]
            {
                plan0[0].DueTime,
                plan1[0].DueTime,
                plan2[0].DueTime,
            };

            times.Distinct().Should().HaveCount(3, "each character index should yield a different due time");
        }

        [Fact]
        public void Plan_WarmStart_UsesPersistedCachedUntil()
        {
            var futureCache = DateTime.UtcNow.AddMinutes(10);
            var regs = new List<EndpointRegistration> { MakeReg(0) };
            var persisted = new List<CachedEndpointState>
            {
                new() { Method = 0, CachedUntil = futureCache, ETag = "\"abc\"" }
            };

            var plan = ColdStartPlanner.Plan(1L, 0, true, regs, persisted);

            // Due time should be based on persisted CachedUntil (+ jitter), not cold-start delay
            plan[0].DueTime.Should().BeAfter(futureCache);
        }

        [Fact]
        public void Plan_WarmStart_SetsPersistedETags()
        {
            var regs = new List<EndpointRegistration> { MakeReg(0) };
            var persisted = new List<CachedEndpointState>
            {
                new() { Method = 0, CachedUntil = DateTime.UtcNow.AddMinutes(-1), ETag = "\"etag-123\"" }
            };

            var plan = ColdStartPlanner.Plan(1L, 0, true, regs, persisted);

            plan[0].Job.ETag.Should().Be("\"etag-123\"");
        }

        [Fact]
        public void Plan_100Characters_NoThunderingHerd()
        {
            var regs = new List<EndpointRegistration> { MakeReg(4) }; // Phase 2

            var allDueTimes = new List<DateTime>();
            for (int i = 0; i < 100; i++)
            {
                var plan = ColdStartPlanner.Plan(i + 1, i, false, regs, null);
                allDueTimes.Add(plan[0].DueTime);
            }

            // All due times should be distinct (staggered by characterIndex)
            allDueTimes.Distinct().Should().HaveCount(100);

            // Time spread should be non-trivial (at least 1 second between first and last)
            var spread = allDueTimes.Max() - allDueTimes.Min();
            spread.TotalMilliseconds.Should().BeGreaterThan(1000);
        }

        [Fact]
        public void Plan_EmptyPersistedStates_UsesDefaultTiming()
        {
            var regs = new List<EndpointRegistration> { MakeReg(0) };

            var plan = ColdStartPlanner.Plan(1L, 0, true, regs, new List<CachedEndpointState>());

            plan.Should().HaveCount(1);
            plan[0].Job.ETag.Should().BeNull();
            plan[0].Job.Priority.Should().Be(FetchPriority.Active);
        }
    }
}
