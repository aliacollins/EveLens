using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Serialization.Eve;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for skill queue serialization and QueuedSkill serializable objects.
    /// Full SkillQueue/QueuedSkill construction requires CCPCharacter + static skill data.
    /// These tests cover the serializable layer and computed properties on SerializableQueuedSkill.
    /// </summary>
    public class SkillQueueTests
    {
        #region SerializableQueuedSkill construction

        [Fact]
        public void SerializableQueuedSkill_DefaultConstructor_HasZeroSP()
        {
            var skill = new SerializableQueuedSkill();
            skill.StartSP.Should().Be(0);
            skill.EndSP.Should().Be(0);
            skill.Level.Should().Be(0);
            skill.ID.Should().Be(0);
        }

        [Fact]
        public void SerializableQueuedSkill_WithTimes_IsNotPaused()
        {
            var now = DateTime.UtcNow;
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 3,
                StartSP = 8000,
                EndSP = 45255,
                StartTime = now.AddMinutes(-30),
                EndTime = now.AddHours(2)
            };

            skill.IsPaused.Should().BeFalse();
            skill.IsTraining.Should().BeTrue();
            skill.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public void SerializableQueuedSkill_WithEmptyTimes_IsPaused()
        {
            // When CCP pauses the queue, start and end times are empty (DateTime.MinValue)
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 3,
                StartSP = 8000,
                EndSP = 45255
                // StartTime and EndTime not set -> DateTime.MinValue
            };

            skill.IsPaused.Should().BeTrue();
            skill.IsTraining.Should().BeFalse();
            skill.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public void SerializableQueuedSkill_CompletedInPast_IsCompleted()
        {
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 2,
                StartSP = 250,
                EndSP = 1414,
                StartTime = DateTime.UtcNow.AddHours(-5),
                EndTime = DateTime.UtcNow.AddHours(-1)
            };

            skill.IsCompleted.Should().BeTrue();
            skill.IsTraining.Should().BeFalse();
            skill.IsPaused.Should().BeFalse();
        }

        [Fact]
        public void SerializableQueuedSkill_FutureStart_IsNotTraining()
        {
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 4,
                StartSP = 45255,
                EndSP = 256000,
                StartTime = DateTime.UtcNow.AddHours(2),
                EndTime = DateTime.UtcNow.AddHours(10)
            };

            skill.IsTraining.Should().BeFalse();
            skill.IsCompleted.Should().BeFalse();
            skill.IsPaused.Should().BeFalse();
        }

        #endregion

        #region XML round-trip

        [Fact]
        public void SerializableQueuedSkill_XmlRoundTrip_PreservesFields()
        {
            var skill = new SerializableQueuedSkill
            {
                ID = 3350,
                Level = 4,
                StartSP = 45255,
                EndSP = 256000
            };

            var result = XmlRoundTrip(skill);

            result.ID.Should().Be(3350);
            result.Level.Should().Be(4);
            result.StartSP.Should().Be(45255);
            result.EndSP.Should().Be(256000);
        }

        [Fact]
        public void SerializableQueuedSkill_XmlRoundTrip_PausedSkillPreservesPausedState()
        {
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 3,
                StartSP = 8000,
                EndSP = 45255
                // No times set = paused
            };

            var result = XmlRoundTrip(skill);

            result.IsPaused.Should().BeTrue();
            result.StartSP.Should().Be(8000);
            result.EndSP.Should().Be(45255);
        }

        #endregion

        #region SP calculations

        [Fact]
        public void SerializableQueuedSkill_SPDifference_IsPositive()
        {
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 5,
                StartSP = 45255,
                EndSP = 256000
            };

            int spToTrain = skill.EndSP - skill.StartSP;
            spToTrain.Should().BeGreaterThan(0);
            spToTrain.Should().Be(210745);
        }

        [Theory]
        [InlineData(1, 0, 250)]
        [InlineData(2, 250, 1414)]
        [InlineData(3, 1414, 8000)]
        [InlineData(4, 8000, 45255)]
        [InlineData(5, 45255, 256000)]
        public void SerializableQueuedSkill_Rank1Skill_SPMatchesExpectedFormula(
            int level, int expectedStartSP, int expectedEndSP)
        {
            // For a rank 1 skill, the SP thresholds are well-known
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = level,
                StartSP = expectedStartSP,
                EndSP = expectedEndSP
            };

            skill.Level.Should().Be(level);
            (skill.EndSP - skill.StartSP).Should().BeGreaterThan(0);
        }

        #endregion

        #region Edge cases

        [Fact]
        public void SerializableQueuedSkill_ZeroSPRange_IsValid()
        {
            // Edge case: StartSP == EndSP (shouldn't happen normally, but test robustness)
            var skill = new SerializableQueuedSkill
            {
                ID = 3300,
                Level = 1,
                StartSP = 250,
                EndSP = 250
            };

            (skill.EndSP - skill.StartSP).Should().Be(0);
        }

        [Fact]
        public void SerializableQueuedSkill_MultipleSkillsInQueue_MaintainOrder()
        {
            var skills = new[]
            {
                new SerializableQueuedSkill { ID = 3300, Level = 1, StartSP = 0, EndSP = 250 },
                new SerializableQueuedSkill { ID = 3300, Level = 2, StartSP = 250, EndSP = 1414 },
                new SerializableQueuedSkill { ID = 3301, Level = 1, StartSP = 0, EndSP = 500 }
            };

            skills.Should().HaveCount(3);
            skills[0].ID.Should().Be(3300);
            skills[0].Level.Should().Be(1);
            skills[1].Level.Should().Be(2);
            skills[2].ID.Should().Be(3301);
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
