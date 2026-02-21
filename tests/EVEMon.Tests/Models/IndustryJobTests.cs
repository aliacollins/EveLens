// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for industry job serialization and business rules.
    /// Full IndustryJob construction from API requires EsiJobListItem.
    /// The deserialization constructor from SerializableJob is internal.
    /// These tests cover the serializable layer and the IndustryJob model's
    /// static helper methods and constants.
    /// </summary>
    public class IndustryJobTests
    {
        #region SerializableJob basics

        [Fact]
        public void SerializableJob_DefaultConstructor_HasDefaults()
        {
            var job = new SerializableJob();
            job.JobID.Should().Be(0);
            job.State.Should().Be(JobState.Active);
            job.StartDate.Should().Be(default(DateTime));
            job.EndDate.Should().Be(default(DateTime));
            job.PauseDate.Should().Be(default(DateTime));
        }

        [Fact]
        public void SerializableJob_SetProperties_Preserves()
        {
            var start = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2025, 6, 2, 10, 0, 0, DateTimeKind.Utc);
            var job = new SerializableJob
            {
                JobID = 98765,
                State = JobState.Active,
                StartDate = start,
                EndDate = end,
                IssuedFor = IssuedFor.Character,
                LastStateChange = start
            };

            job.JobID.Should().Be(98765);
            job.State.Should().Be(JobState.Active);
            job.StartDate.Should().Be(start);
            job.EndDate.Should().Be(end);
            job.IssuedFor.Should().Be(IssuedFor.Character);
        }

        #endregion

        #region XML round-trip

        [Fact]
        public void SerializableJob_XmlRoundTrip_PreservesAllFields()
        {
            var start = new DateTime(2025, 3, 15, 8, 30, 0, DateTimeKind.Utc);
            var end = new DateTime(2025, 3, 16, 8, 30, 0, DateTimeKind.Utc);
            var pause = new DateTime(2025, 3, 15, 20, 0, 0, DateTimeKind.Utc);
            var stateChange = new DateTime(2025, 3, 15, 20, 0, 0, DateTimeKind.Utc);

            var job = new SerializableJob
            {
                JobID = 12345,
                State = JobState.Paused,
                StartDate = start,
                EndDate = end,
                PauseDate = pause,
                IssuedFor = IssuedFor.Corporation,
                LastStateChange = stateChange
            };

            var result = XmlRoundTrip(job);
            result.JobID.Should().Be(12345);
            result.State.Should().Be(JobState.Paused);
            result.StartDate.Should().Be(start);
            result.EndDate.Should().Be(end);
            result.PauseDate.Should().Be(pause);
            result.IssuedFor.Should().Be(IssuedFor.Corporation);
            result.LastStateChange.Should().Be(stateChange);
        }

        #endregion

        #region Job state values

        [Theory]
        [InlineData(JobState.Active)]
        [InlineData(JobState.Delivered)]
        [InlineData(JobState.Canceled)]
        [InlineData(JobState.Paused)]
        [InlineData(JobState.Failed)]
        public void SerializableJob_State_RoundTrips(JobState state)
        {
            var job = new SerializableJob
            {
                JobID = 1,
                State = state
            };

            var result = XmlRoundTrip(job);
            result.State.Should().Be(state);
        }

        #endregion

        #region ActiveJobState enum values

        [Fact]
        public void ActiveJobState_None_IsDefault()
        {
            ActiveJobState.None.Should().Be(default(ActiveJobState));
        }

        [Fact]
        public void ActiveJobState_HasAllExpectedValues()
        {
            // Document the progression: None -> Pending -> InProgress -> Ready
            ((int)ActiveJobState.None).Should().BeLessThan((int)ActiveJobState.Pending);
            ((int)ActiveJobState.Pending).Should().BeLessThan((int)ActiveJobState.InProgress);
            ((int)ActiveJobState.InProgress).Should().BeLessThan((int)ActiveJobState.Ready);
        }

        #endregion

        #region IssuedFor values

        [Theory]
        [InlineData(IssuedFor.Character)]
        [InlineData(IssuedFor.Corporation)]
        [InlineData(IssuedFor.None)]
        public void SerializableJob_IssuedFor_RoundTrips(IssuedFor issuedFor)
        {
            var job = new SerializableJob
            {
                JobID = 1,
                IssuedFor = issuedFor
            };

            var result = XmlRoundTrip(job);
            result.IssuedFor.Should().Be(issuedFor);
        }

        #endregion

        #region CCPCharacter industry jobs collection

        [Fact]
        public void SerializableCCPCharacter_IndustryJobs_Empty_RoundTrips()
        {
            var character = new SerializableCCPCharacter();
            character.IndustryJobs.Should().BeEmpty();

            var result = XmlRoundTrip(character);
            result.IndustryJobs.Should().BeEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_IndustryJobs_Multiple_Preserves()
        {
            var character = new SerializableCCPCharacter();
            var now = DateTime.UtcNow;

            character.IndustryJobs.Add(new SerializableJob
            {
                JobID = 1,
                State = JobState.Active,
                StartDate = now.AddHours(-2),
                EndDate = now.AddHours(22),
                IssuedFor = IssuedFor.Character
            });
            character.IndustryJobs.Add(new SerializableJob
            {
                JobID = 2,
                State = JobState.Delivered,
                StartDate = now.AddDays(-3),
                EndDate = now.AddDays(-2),
                IssuedFor = IssuedFor.Corporation
            });

            var result = XmlRoundTrip(character);
            result.IndustryJobs.Should().HaveCount(2);
            result.IndustryJobs[0].JobID.Should().Be(1);
            result.IndustryJobs[0].State.Should().Be(JobState.Active);
            result.IndustryJobs[1].JobID.Should().Be(2);
            result.IndustryJobs[1].State.Should().Be(JobState.Delivered);
        }

        #endregion

        #region Time-based calculations (serializable level)

        [Fact]
        public void SerializableJob_ActiveJob_EndDateInFuture()
        {
            var now = DateTime.UtcNow;
            var job = new SerializableJob
            {
                JobID = 1,
                State = JobState.Active,
                StartDate = now.AddHours(-1),
                EndDate = now.AddHours(23)
            };

            job.EndDate.Should().BeAfter(DateTime.UtcNow);
            (job.EndDate - job.StartDate).TotalHours.Should().BeApproximately(24, 0.1);
        }

        [Fact]
        public void SerializableJob_CompletedJob_EndDateInPast()
        {
            var job = new SerializableJob
            {
                JobID = 1,
                State = JobState.Active,
                StartDate = DateTime.UtcNow.AddDays(-2),
                EndDate = DateTime.UtcNow.AddDays(-1)
            };

            job.EndDate.Should().BeBefore(DateTime.UtcNow);
        }

        [Fact]
        public void SerializableJob_PausedJob_HasPauseDate()
        {
            var start = DateTime.UtcNow.AddHours(-6);
            var pause = DateTime.UtcNow.AddHours(-2);
            var end = DateTime.UtcNow.AddHours(18);

            var job = new SerializableJob
            {
                JobID = 1,
                State = JobState.Paused,
                StartDate = start,
                EndDate = end,
                PauseDate = pause
            };

            job.PauseDate.Should().BeAfter(job.StartDate);
            job.PauseDate.Should().BeBefore(job.EndDate);

            // Time remaining when paused should be based on EndDate - PauseDate
            var timeRemaining = job.EndDate - job.PauseDate;
            timeRemaining.TotalHours.Should().BeApproximately(20, 0.1);
        }

        #endregion

        #region MaxEndedDays constant

        [Fact]
        public void IndustryJob_MaxEndedDays_IsSeven()
        {
            // This constant determines how long ended jobs are kept
            IndustryJob.MaxEndedDays.Should().Be(7);
        }

        #endregion

        #region Large job IDs

        [Fact]
        public void SerializableJob_LargeJobID_Preserves()
        {
            var job = new SerializableJob
            {
                JobID = long.MaxValue
            };

            var result = XmlRoundTrip(job);
            result.JobID.Should().Be(long.MaxValue);
        }

        #endregion

        #region Job duration patterns

        [Theory]
        [InlineData(1)]    // 1 hour manufacturing job
        [InlineData(24)]   // 1 day
        [InlineData(168)]  // 1 week
        [InlineData(720)]  // 30 days
        public void SerializableJob_VariousDurations_PreserveCorrectly(int durationHours)
        {
            var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddHours(durationHours);

            var job = new SerializableJob
            {
                JobID = 1,
                State = JobState.Active,
                StartDate = start,
                EndDate = end
            };

            var result = XmlRoundTrip(job);
            (result.EndDate - result.StartDate).TotalHours.Should().BeApproximately(durationHours, 0.01);
        }

        #endregion

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }
    }
}
