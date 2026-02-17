using System;
using EVEMon.Common.Scheduling;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Scheduling
{
    public class TokenBucketTests
    {
        [Fact]
        public void HasBudget_WhenRemaining_AboveSafetyMargin_ReturnsTrue()
        {
            var bucket = new TokenBucket { Remaining = 100 };

            bucket.HasBudget.Should().BeTrue();
        }

        [Theory]
        [InlineData(15)]
        [InlineData(10)]
        [InlineData(0)]
        public void HasBudget_WhenRemaining_AtOrBelowSafetyMargin_ReturnsFalse(int remaining)
        {
            var bucket = new TokenBucket { Remaining = remaining };

            bucket.HasBudget.Should().BeFalse();
        }

        [Fact]
        public void Update_SetsRemainingAndLimit()
        {
            var bucket = new TokenBucket();
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            bucket.Update(remaining: 42, limit: 200, now);

            bucket.Remaining.Should().Be(42);
            bucket.Limit.Should().Be(200);
            bucket.LastUpdated.Should().Be(now);
        }

        [Fact]
        public void CheckRefill_WhenWindowExpired_ResetsRemaining()
        {
            var bucket = new TokenBucket
            {
                Remaining = 5,
                Limit = 150,
                LastUpdated = DateTime.UtcNow.AddMinutes(-20),
                Window = TimeSpan.FromMinutes(15)
            };

            bucket.CheckRefill(DateTime.UtcNow);

            bucket.Remaining.Should().Be(150);
        }

        [Fact]
        public void CheckRefill_WhenWindowNotExpired_DoesNotReset()
        {
            var now = DateTime.UtcNow;
            var bucket = new TokenBucket
            {
                Remaining = 5,
                Limit = 150,
                LastUpdated = now,
                Window = TimeSpan.FromMinutes(15)
            };

            bucket.CheckRefill(now.AddMinutes(10));

            bucket.Remaining.Should().Be(5);
        }

        [Fact]
        public void NextRefillTime_CalculatesCorrectly()
        {
            var lastUpdated = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var window = TimeSpan.FromMinutes(15);
            var bucket = new TokenBucket
            {
                LastUpdated = lastUpdated,
                Window = window
            };

            bucket.NextRefillTime.Should().Be(lastUpdated + window);
        }
    }
}
