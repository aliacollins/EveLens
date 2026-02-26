// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Scheduling;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Scheduling
{
    public class FetchJobTests
    {
        [Fact]
        public void Key_ReturnsTupleOfCharacterIdAndMethod()
        {
            var job = new FetchJob
            {
                CharacterId = 42L,
                EndpointMethod = 7,
                ExecuteAsync = _ => Task.FromResult(new EveLens.Core.Interfaces.FetchOutcome()),
            };

            job.Key.Should().Be((42L, 7));
        }

        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var job = new FetchJob
            {
                CharacterId = 1L,
                EndpointMethod = 0,
                ExecuteAsync = _ => Task.FromResult(new EveLens.Core.Interfaces.FetchOutcome()),
            };

            job.Generation.Should().Be(0);
            job.Priority.Should().Be(FetchPriority.Active); // default enum value
            job.RateGroup.Should().BeNull();
            job.ETag.Should().BeNull();
            job.CachedUntil.Should().Be(default(DateTime));
            job.ConsecutiveNotModified.Should().Be(0);
            job.IsRemoved.Should().BeFalse();
        }
    }
}
