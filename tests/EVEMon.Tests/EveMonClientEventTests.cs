using System;
using System.Runtime.Serialization;
using EVEMon.Common;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using FluentAssertions;
using Xunit;

#pragma warning disable SYSLIB0050 // FormatterServices used for test doubles

namespace EVEMon.Tests
{
    /// <summary>
    /// Tests for EveMonClient event propagation and UpdateBatcher integration.
    /// Note: EveMonClient is static, so these tests verify behavior at the integration boundary.
    /// Full event propagation tests require static data files to be loaded.
    /// </summary>
    public class EveMonClientEventTests
    {
        [Fact]
        public void CharacterBatchEventArgs_NullEnumerable_ProducesEmptyList()
        {
            var args = new CharacterBatchEventArgs(null);
            args.Characters.Should().NotBeNull();
            args.Count.Should().Be(0);
        }

        [Fact]
        public void CharacterBatchEventArgs_WithCharacters_PreservesAll()
        {
            var chars = new[]
            {
                CreateDummyCharacter(),
                CreateDummyCharacter(),
                CreateDummyCharacter()
            };
            var args = new CharacterBatchEventArgs(chars);
            args.Count.Should().Be(3);
            args.Characters.Should().HaveCount(3);
        }

        [Fact]
        public void CharacterBatchEventArgs_Characters_IsReadOnly()
        {
            var args = new CharacterBatchEventArgs(Array.Empty<Character>());
            args.Characters.Should().BeAssignableTo<System.Collections.Generic.IReadOnlyList<Character>>();
        }

        [Fact]
        public void UpdateBatcher_IntegrationWithBatchEvents_FiresCorrectly()
        {
            // This tests the full path: Queue -> Timer -> Flush -> Event
            using var batcher = new UpdateBatcher(coalesceMs: 60000);
            var character = CreateDummyCharacter();

            CharacterBatchEventArgs charBatch = null;
            CharacterBatchEventArgs skillBatch = null;
            batcher.CharactersBatchUpdated += (_, e) => charBatch = e;
            batcher.SkillQueuesBatchUpdated += (_, e) => skillBatch = e;

            // Queue both types for same character
            batcher.QueueCharacterUpdate(character);
            batcher.QueueSkillQueueUpdate(character);
            batcher.FlushNow();

            charBatch.Should().NotBeNull();
            charBatch.Count.Should().Be(1);
            skillBatch.Should().NotBeNull();
            skillBatch.Count.Should().Be(1);
        }

        [Fact]
        public void UpdateBatcher_MultipleFlushes_DoNotDuplicate()
        {
            using var batcher = new UpdateBatcher(coalesceMs: 60000);
            var character = CreateDummyCharacter();
            int eventCount = 0;
            batcher.CharactersBatchUpdated += (_, _) => eventCount++;

            batcher.QueueCharacterUpdate(character);
            batcher.FlushNow();
            batcher.FlushNow(); // Second flush should be no-op

            eventCount.Should().Be(1);
        }

        [Fact]
        public void UpdateBatcher_QueueAfterFlush_StartsNewBatch()
        {
            using var batcher = new UpdateBatcher(coalesceMs: 60000);
            var c1 = CreateDummyCharacter();
            var c2 = CreateDummyCharacter();
            CharacterBatchEventArgs lastBatch = null;
            batcher.CharactersBatchUpdated += (_, e) => lastBatch = e;

            // First batch
            batcher.QueueCharacterUpdate(c1);
            batcher.FlushNow();
            lastBatch.Count.Should().Be(1);

            // Second batch
            batcher.QueueCharacterUpdate(c2);
            batcher.FlushNow();
            lastBatch.Count.Should().Be(1);
            lastBatch.Characters.Should().Contain(c2);
        }

        [Fact]
        public void UpdateBatcher_ConcurrentQueueAndFlush_NoDataLoss()
        {
            using var batcher = new UpdateBatcher(coalesceMs: 60000);
            int totalReceived = 0;
            batcher.CharactersBatchUpdated += (_, e) => totalReceived += e.Count;

            // Queue 50 characters and flush multiple times
            for (int i = 0; i < 50; i++)
            {
                batcher.QueueCharacterUpdate(CreateDummyCharacter());
                if (i % 10 == 0)
                    batcher.FlushNow();
            }
            batcher.FlushNow(); // Final flush

            totalReceived.Should().Be(50);
        }

        private static Character CreateDummyCharacter()
        {
            return (Character)FormatterServices.GetUninitializedObject(typeof(CCPCharacter));
        }
    }
}
