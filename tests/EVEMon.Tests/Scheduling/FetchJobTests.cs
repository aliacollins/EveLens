using System;
using System.Threading.Tasks;
using EVEMon.Common.Scheduling;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Scheduling
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
                ExecuteAsync = _ => Task.FromResult(new EVEMon.Core.Interfaces.FetchOutcome()),
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
                ExecuteAsync = _ => Task.FromResult(new EVEMon.Core.Interfaces.FetchOutcome()),
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
