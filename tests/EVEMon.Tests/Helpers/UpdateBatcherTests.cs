// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

#pragma warning disable SYSLIB0050 // FormatterServices.GetUninitializedObject is used intentionally for test doubles
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Helpers
{
    public class UpdateBatcherTests : IDisposable
    {
        private readonly UpdateBatcher _batcher;

        public UpdateBatcherTests()
        {
            // Use a long coalesce window so timer doesn't fire during tests
            // (we use FlushNow() for deterministic control)
            _batcher = new UpdateBatcher(coalesceMs: 60000);
        }

        public void Dispose()
        {
            _batcher.Dispose();
        }

        /// <summary>
        /// Creates a Character instance without calling its constructor.
        /// UpdateBatcher only uses reference equality in HashSet, so constructor state is irrelevant.
        /// </summary>
        private static Character CreateDummyCharacter()
        {
            return (Character)FormatterServices.GetUninitializedObject(typeof(CCPCharacter));
        }

        [Fact]
        public void FlushNow_SingleCharacter_FiresEventWithOneCharacter()
        {
            // Arrange
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? received = null;
            _batcher.CharactersBatchUpdated += (_, e) => received = e;

            // Act
            _batcher.QueueCharacterUpdate(character);
            _batcher.FlushNow();

            // Assert
            received.Should().NotBeNull();
            received!.Count.Should().Be(1);
            received!.Characters.Should().Contain(character);
        }

        [Fact]
        public void FlushNow_SameCharacterTwice_DeduplicatesToOne()
        {
            // Arrange
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? received = null;
            _batcher.CharactersBatchUpdated += (_, e) => received = e;

            // Act
            _batcher.QueueCharacterUpdate(character);
            _batcher.QueueCharacterUpdate(character);
            _batcher.FlushNow();

            // Assert
            received.Should().NotBeNull();
            received!.Count.Should().Be(1);
        }

        [Fact]
        public void FlushNow_ThreeDistinctCharacters_FiresEventWithThree()
        {
            // Arrange
            var chars = new[] { CreateDummyCharacter(), CreateDummyCharacter(), CreateDummyCharacter() };
            CharacterBatchEventArgs? received = null;
            _batcher.CharactersBatchUpdated += (_, e) => received = e;

            // Act
            foreach (var c in chars)
                _batcher.QueueCharacterUpdate(c);
            _batcher.FlushNow();

            // Assert
            received.Should().NotBeNull();
            received!.Count.Should().Be(3);
            received!.Characters.Should().BeEquivalentTo(chars);
        }

        [Fact]
        public void FlushNow_NoPendingUpdates_DoesNotFireEvent()
        {
            // Arrange
            bool eventFired = false;
            _batcher.CharactersBatchUpdated += (_, _) => eventFired = true;

            // Act
            _batcher.FlushNow();

            // Assert
            eventFired.Should().BeFalse();
        }

        [Fact]
        public void CharacterAndSkillQueue_TrackedSeparately()
        {
            // Arrange
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? charReceived = null;
            CharacterBatchEventArgs? skillReceived = null;
            _batcher.CharactersBatchUpdated += (_, e) => charReceived = e;
            _batcher.SkillQueuesBatchUpdated += (_, e) => skillReceived = e;

            // Act
            _batcher.QueueCharacterUpdate(character);
            _batcher.FlushNow();

            // Assert
            charReceived.Should().NotBeNull("character batch event should fire");
            skillReceived.Should().BeNull("skill queue batch should NOT fire");
        }

        [Fact]
        public void SkillQueueUpdate_FiresSeparateEvent()
        {
            // Arrange
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? charReceived = null;
            CharacterBatchEventArgs? skillReceived = null;
            _batcher.CharactersBatchUpdated += (_, e) => charReceived = e;
            _batcher.SkillQueuesBatchUpdated += (_, e) => skillReceived = e;

            // Act
            _batcher.QueueSkillQueueUpdate(character);
            _batcher.FlushNow();

            // Assert
            charReceived.Should().BeNull("character batch should NOT fire");
            skillReceived.Should().NotBeNull("skill queue batch event should fire");
            skillReceived!.Count.Should().Be(1);
        }

        [Fact]
        public void AfterDispose_QueueIsNoOp()
        {
            // Arrange
            var batcher = new UpdateBatcher(coalesceMs: 60000);
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? received = null;
            batcher.CharactersBatchUpdated += (_, e) => received = e;

            // Act
            batcher.Dispose();
            batcher.QueueCharacterUpdate(character);
            batcher.FlushNow();

            // Assert
            received.Should().BeNull("disposed batcher should not accept new updates");
        }

        [Fact]
        public void PendingCharacterUpdateCount_ReflectsQueuedCount()
        {
            // Arrange
            var c1 = CreateDummyCharacter();
            var c2 = CreateDummyCharacter();

            // Act & Assert
            _batcher.PendingCharacterUpdateCount.Should().Be(0);
            _batcher.QueueCharacterUpdate(c1);
            _batcher.PendingCharacterUpdateCount.Should().Be(1);
            _batcher.QueueCharacterUpdate(c2);
            _batcher.PendingCharacterUpdateCount.Should().Be(2);
            _batcher.FlushNow();
            _batcher.PendingCharacterUpdateCount.Should().Be(0);
        }

        [Fact]
        public void PendingSkillQueueUpdateCount_ReflectsQueuedCount()
        {
            // Arrange
            var c1 = CreateDummyCharacter();

            // Act & Assert
            _batcher.PendingSkillQueueUpdateCount.Should().Be(0);
            _batcher.QueueSkillQueueUpdate(c1);
            _batcher.PendingSkillQueueUpdateCount.Should().Be(1);
            _batcher.FlushNow();
            _batcher.PendingSkillQueueUpdateCount.Should().Be(0);
        }

        [Fact]
        public void CoalesceWindow_AutoFlushesAfterTimeout()
        {
            // Arrange - use short coalesce for this test
            using var shortBatcher = new UpdateBatcher(coalesceMs: 50);
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? received = null;
            using var signal = new ManualResetEventSlim(false);
            shortBatcher.CharactersBatchUpdated += (_, e) =>
            {
                received = e;
                signal.Set();
            };

            // Act
            shortBatcher.QueueCharacterUpdate(character);

            // Assert - wait up to 500ms for the auto-flush
            signal.Wait(TimeSpan.FromMilliseconds(500)).Should().BeTrue("coalesce timer should auto-flush");
            received.Should().NotBeNull();
            received!.Count.Should().Be(1);
        }

        [Fact]
        public void QueueNullCharacter_IsIgnored()
        {
            // Arrange
            CharacterBatchEventArgs? received = null;
            _batcher.CharactersBatchUpdated += (_, e) => received = e;

            // Act
            _batcher.QueueCharacterUpdate(null!);
            _batcher.FlushNow();

            // Assert
            received.Should().BeNull("null character should be ignored");
            _batcher.PendingCharacterUpdateCount.Should().Be(0);
        }

        [Fact]
        public void Dispose_FlushesRemainingUpdates()
        {
            // Arrange
            var batcher = new UpdateBatcher(coalesceMs: 60000);
            var character = CreateDummyCharacter();
            CharacterBatchEventArgs? received = null;
            batcher.CharactersBatchUpdated += (_, e) => received = e;
            batcher.QueueCharacterUpdate(character);

            // Act
            batcher.Dispose();

            // Assert
            received.Should().NotBeNull("dispose should flush pending updates");
            received!.Count.Should().Be(1);
        }
    }

    public class CharacterBatchEventArgsTests
    {
        [Fact]
        public void Constructor_NullEnumerable_CreatesEmptyList()
        {
            // Act
            var args = new CharacterBatchEventArgs(null!);

            // Assert
            args.Characters.Should().NotBeNull();
            args.Count.Should().Be(0);
        }

        [Fact]
        public void Count_MatchesCharactersList()
        {
            // Arrange
            var chars = new List<Character>
            {
                (Character)FormatterServices.GetUninitializedObject(typeof(CCPCharacter)),
                (Character)FormatterServices.GetUninitializedObject(typeof(CCPCharacter))
            };

            // Act
            var args = new CharacterBatchEventArgs(chars);

            // Assert
            args.Count.Should().Be(2);
            args.Characters.Should().HaveCount(2);
        }

        [Fact]
        public void Characters_IsReadOnly()
        {
            // Arrange
            var args = new CharacterBatchEventArgs(new List<Character>());

            // Assert
            args.Characters.Should().BeAssignableTo<IReadOnlyList<Character>>();
        }
    }
}
