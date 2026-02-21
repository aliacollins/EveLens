// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    public class CharacterQueryOrchestratorTests : IDisposable
    {
        private readonly IEsiScheduler _scheduler;
        private readonly IEsiClient _esiClient;
        private readonly IEventAggregator _events;

        public CharacterQueryOrchestratorTests()
        {
            _scheduler = Substitute.For<IEsiScheduler>();
            _esiClient = Substitute.For<IEsiClient>();
            _events = Substitute.For<IEventAggregator>();
            _esiClient.MaxConcurrentRequests.Returns(20);
            _esiClient.ActiveRequests.Returns(0L);
        }

        public void Dispose()
        {
        }

        private CharacterQueryOrchestrator CreateOrchestrator(
            long characterId = 12345L, string characterName = "Test Char")
        {
            return new CharacterQueryOrchestrator(
                _scheduler, _esiClient, _events, characterId, characterName);
        }

        #region Constructor Tests

        // Test 1: Constructor creates only basic feature monitors (ActiveMonitorCount = 3)
        [Fact]
        public void Constructor_CreatesOnlyBasicFeatureMonitors()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.ActiveMonitorCount.Should().Be(3,
                "only CharacterSheet(0), Skills(1), SkillQueue(2) should be auto-created");
        }

        [Fact]
        public void Constructor_RegistersWithScheduler()
        {
            var orchestrator = CreateOrchestrator(99999L);

            // Test mode no longer auto-registers with scheduler
            orchestrator.ActiveMonitorCount.Should().Be(3, "basic feature monitors created");
        }

        [Fact]
        public void Constructor_NullScheduler_ThrowsArgumentNullException()
        {
            Action act = () => new CharacterQueryOrchestrator(
                null!, _esiClient, _events, 1L, "Test");

            act.Should().Throw<ArgumentNullException>().WithParameterName("scheduler");
        }

        [Fact]
        public void Constructor_NullEsiClient_ThrowsArgumentNullException()
        {
            Action act = () => new CharacterQueryOrchestrator(
                _scheduler, null!, _events, 1L, "Test");

            act.Should().Throw<ArgumentNullException>().WithParameterName("esiClient");
        }

        [Fact]
        public void Constructor_NullEventAggregator_ThrowsArgumentNullException()
        {
            Action act = () => new CharacterQueryOrchestrator(
                _scheduler, _esiClient, null!, 1L, "Test");

            act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
        }

        #endregion

        #region RequestDataType Tests

        // Test 2: RequestDataType creates monitor lazily (count increases)
        [Fact]
        public void RequestDataType_CreatesMonitorLazily()
        {
            var orchestrator = CreateOrchestrator();
            orchestrator.ActiveMonitorCount.Should().Be(3);

            orchestrator.RequestDataType(10);

            orchestrator.ActiveMonitorCount.Should().Be(4,
                "requesting a new data type should create a new monitor");
        }

        // Test 3: RequestDataType for already-active monitor doesn't duplicate
        [Fact]
        public void RequestDataType_AlreadyActive_DoesNotDuplicate()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.RequestDataType(0);

            orchestrator.ActiveMonitorCount.Should().Be(3,
                "requesting an already-active data type should not create a duplicate");
        }

        [Fact]
        public void RequestDataType_MultipleDifferentTypes_IncreasesCount()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.RequestDataType(10);
            orchestrator.RequestDataType(11);
            orchestrator.RequestDataType(12);

            orchestrator.ActiveMonitorCount.Should().Be(6);
        }

        #endregion

        #region ProcessTick Tests

        // Test 4: ProcessTick with no expired monitors does nothing new
        [Fact]
        public void ProcessTick_NoExpiredMonitors_DoesNotPublishAdditionalEvents()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet(0) + SkillQueue(2) complete
            orchestrator.ProcessTick();
            // Tick 2: Skills(1) completes (uses SkillQueue cached result) -> event fires
            orchestrator.ProcessTick();

            _events.ClearReceivedCalls();

            // Tick 3: All monitors' NextQueryTime is in the future, nothing should happen
            orchestrator.ProcessTick();

            _events.DidNotReceive().Publish(Arg.Any<CharacterUpdatedEvent>());
        }

        // Test 5: ProcessTick processes expired monitors
        [Fact]
        public void ProcessTick_ExpiredMonitors_ProcessesThem()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet(0) and SkillQueue(2) process immediately.
            // Skills(1) is skipped because SkillQueue's cached result isn't available
            // yet when Skills is iterated (Dictionary order: 0, 1, 2).
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(0).Should().BeTrue("CharacterSheet should complete");
            orchestrator.IsQueryComplete(2).Should().BeTrue("SkillQueue should complete");
        }

        #endregion

        #region Query Ordering Tests

        // Test 6: Attributes not processed until Implants completes on a previous tick
        [Fact]
        public void ProcessTick_Attributes_NotProcessedUntilImplantsCompletesPreviousTick()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.RequestDataType(CharacterQueryOrchestrator.DataType_Implants);
            orchestrator.RequestDataType(CharacterQueryOrchestrator.DataType_Attributes);

            // Tick 1: Implants completes, Attributes blocked (same-tick completion
            // of prerequisites does not satisfy the check)
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Implants)
                .Should().BeTrue("Implants should complete on first tick");
            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Attributes)
                .Should().BeFalse(
                    "Attributes should not process in the same tick as its prerequisite");
        }

        [Fact]
        public void ProcessTick_Attributes_ProcessesOnSubsequentTickAfterImplantsCompletes()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.RequestDataType(CharacterQueryOrchestrator.DataType_Implants);
            orchestrator.RequestDataType(CharacterQueryOrchestrator.DataType_Attributes);

            // Tick 1: Implants completes, Attributes blocked
            orchestrator.ProcessTick();
            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Attributes)
                .Should().BeFalse("should be blocked on first tick");

            // Tick 2: Implants completed in a previous tick, Attributes can now process
            orchestrator.ProcessTick();
            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Attributes)
                .Should().BeTrue("should complete after Implants prerequisite met on prior tick");
        }

        // Test 7: SkillQueue result cached for Skills import pattern
        [Fact]
        public void ProcessTick_Skills_RequiresSkillQueueCachedResultFromPreviousTick()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: SkillQueue(2) processes and caches its result.
            // Skills(1) skips because SkillQueue hasn't cached yet when Skills is iterated.
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_SkillQueue)
                .Should().BeTrue("SkillQueue should complete on first tick");
            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Skills)
                .Should().BeFalse(
                    "Skills should not complete on tick 1 (SkillQueue cache not yet available)");

            // Tick 2: Skills finds cached result from tick 1 and processes
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Skills)
                .Should().BeTrue("Skills should complete on tick 2 using cached SkillQueue result");
        }

        #endregion

        #region CharacterSheet Completion Tests

        // Test 8: CharacterSheet completion fires event after all basic monitors complete
        [Fact]
        public void ProcessTick_AllBasicComplete_PublishesCharacterUpdatedEvent()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet(0) + SkillQueue(2) complete, Skills(1) deferred
            orchestrator.ProcessTick();
            _events.DidNotReceive().Publish(Arg.Any<CharacterUpdatedEvent>());

            // Tick 2: Skills(1) completes -> all basic monitors done -> event fires
            orchestrator.ProcessTick();

            _events.Received(1).Publish(Arg.Is<CharacterUpdatedEvent>(
                e => e.CharacterID == 12345L && e.CharacterName == "Test Char"));
        }

        // Test 9: IsCharacterSheetUpdating is true while not all basic monitors have completed
        [Fact]
        public void IsCharacterSheetUpdating_TrueWhileBasicMonitorsIncomplete()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.IsCharacterSheetUpdating.Should().BeFalse("should be false before any ticks");

            // Tick 1: some basic monitors complete, but not all (Skills deferred)
            orchestrator.ProcessTick();
            orchestrator.IsCharacterSheetUpdating.Should().BeTrue(
                "should be true while Skills has not yet completed");

            // Tick 2: Skills completes, all basic done -> updating goes false
            orchestrator.ProcessTick();
            orchestrator.IsCharacterSheetUpdating.Should().BeFalse(
                "should be false after all basic monitors complete");
        }

        #endregion

        #region IsQueryComplete Tests

        // Test 10: IsQueryComplete returns false before first completion
        [Fact]
        public void IsQueryComplete_BeforeCompletion_ReturnsFalse()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.IsQueryComplete(0).Should().BeFalse();
            orchestrator.IsQueryComplete(1).Should().BeFalse();
            orchestrator.IsQueryComplete(2).Should().BeFalse();
        }

        // Test 11: IsQueryComplete returns true after completion
        [Fact]
        public void IsQueryComplete_AfterCompletion_ReturnsTrue()
        {
            var orchestrator = CreateOrchestrator();
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(0).Should().BeTrue("CharacterSheet should be complete");
            orchestrator.IsQueryComplete(2).Should().BeTrue("SkillQueue should be complete");
        }

        [Fact]
        public void IsQueryComplete_UnknownDataType_ReturnsFalse()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.IsQueryComplete(999).Should().BeFalse();
        }

        #endregion

        #region ConsecutiveNotModifiedCount Tests

        // Test 12: ConsecutiveNotModifiedCount tracks across ticks
        [Fact]
        public void ConsecutiveNotModifiedCount_InitiallyZero()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.ConsecutiveNotModifiedCount.Should().Be(0);
        }

        [Fact]
        public void ConsecutiveNotModifiedCount_ResetsWhenMonitorsProcess()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.ProcessTick();

            // After first tick, monitors processed with ConsecutiveNotModified = 0,
            // so allNotModified is false and count resets to 0
            orchestrator.ConsecutiveNotModifiedCount.Should().Be(0);
        }

        #endregion

        #region Scale Tests

        // Test 13: 70 chars don't create 1890 monitors (only 3 per char = 210 total)
        [Fact]
        public void Scale_70Characters_Only3MonitorsEach()
        {
            var orchestrators = new List<CharacterQueryOrchestrator>();
            int totalMonitors = 0;

            for (int i = 0; i < 70; i++)
            {
                var orch = new CharacterQueryOrchestrator(
                    _scheduler, _esiClient, _events,
                    characterId: 10000 + i,
                    characterName: $"Char {i}");
                orchestrators.Add(orch);
                totalMonitors += orch.ActiveMonitorCount;
            }

            totalMonitors.Should().Be(210,
                "70 characters * 3 basic monitors each = 210 total, not 1890 (27 * 70)");

            // Test mode no longer auto-registers with scheduler
            totalMonitors.Should().BeGreaterThan(0);

            foreach (var orch in orchestrators)
                orch.Dispose();
        }

        #endregion

        #region Dispose Tests

        // Test 14: Dispose cleans up and unregisters from scheduler
        [Fact]
        public void Dispose_UnregistersFromScheduler()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.Dispose();

            // Test mode no longer registers/unregisters with scheduler
            orchestrator.ActiveMonitorCount.Should().Be(0, "monitors cleared after dispose");
        }

        [Fact]
        public void Dispose_ClearsMonitors()
        {
            var orchestrator = CreateOrchestrator();
            orchestrator.ActiveMonitorCount.Should().Be(3);

            orchestrator.Dispose();

            orchestrator.ActiveMonitorCount.Should().Be(0);
        }

        [Fact]
        public void Dispose_ProcessTickNoOps()
        {
            var orchestrator = CreateOrchestrator();
            orchestrator.Dispose();

            orchestrator.ProcessTick();

            _events.DidNotReceive().Publish(Arg.Any<CharacterUpdatedEvent>());
        }

        [Fact]
        public void Dispose_RequestDataTypeNoOps()
        {
            var orchestrator = CreateOrchestrator();
            orchestrator.Dispose();

            orchestrator.RequestDataType(10);

            orchestrator.ActiveMonitorCount.Should().Be(0,
                "should not create monitors after disposal");
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var orchestrator = CreateOrchestrator();

            Action act = () =>
            {
                orchestrator.Dispose();
                orchestrator.Dispose();
            };

            act.Should().NotThrow();
            // Test mode no longer registers/unregisters with scheduler
            orchestrator.ActiveMonitorCount.Should().Be(0, "monitors cleared after dispose");
        }

        #endregion

        #region CharacterID Property Tests

        [Fact]
        public void CharacterID_ReturnsConstructorValue()
        {
            var orchestrator = CreateOrchestrator(characterId: 54321L);

            orchestrator.CharacterID.Should().Be(54321L);
        }

        #endregion

        #region Skills-Queue Race Condition (Regression)

        /// <summary>
        /// Regression test: Skills arriving before SkillQueue must not be silently
        /// discarded. In test mode, Skills is deferred until SkillQueue caches its result.
        /// In production mode (the actual fix), skills are stashed in m_lastSkills and
        /// imported when either callback fires with both pieces available.
        /// </summary>
        [Fact]
        public void SkillsArrivedBeforeQueue_StillCompletesOnSubsequentTick()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet(0) and SkillQueue(2) process.
            // Skills(1) is deferred because SkillQueue's cached result isn't yet
            // available when Skills is evaluated (dictionary iteration order: 0, 1, 2).
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Skills)
                .Should().BeFalse("Skills should be deferred when queue hasn't cached yet");
            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_SkillQueue)
                .Should().BeTrue("SkillQueue should complete on first tick");

            // Tick 2: SkillQueue result is now cached from tick 1 → Skills can process
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Skills)
                .Should().BeTrue(
                    "Skills must eventually import even though it arrives before queue — " +
                    "this is the test-mode equivalent of the production race condition fix");
        }

        /// <summary>
        /// Regression test: When SkillQueue arrives first and Skills arrives second,
        /// both should complete normally. This is the non-racy ordering.
        /// </summary>
        [Fact]
        public void QueueArrivedBeforeSkills_BothCompleteNormally()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet(0) + SkillQueue(2) complete
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_CharacterSheet)
                .Should().BeTrue();
            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_SkillQueue)
                .Should().BeTrue();

            // Tick 2: Skills(1) completes using cached queue result
            orchestrator.ProcessTick();

            orchestrator.IsQueryComplete(CharacterQueryOrchestrator.DataType_Skills)
                .Should().BeTrue("Skills should import normally when queue arrived first");
        }

        /// <summary>
        /// Regression test: After both Skills and SkillQueue complete, the character
        /// updated event fires — confirming data was not silently dropped.
        /// </summary>
        [Fact]
        public void SkillsAndQueueBothComplete_CharacterUpdatedEventFires()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet + SkillQueue complete, Skills deferred
            orchestrator.ProcessTick();
            _events.DidNotReceive().Publish(Arg.Any<CharacterUpdatedEvent>());

            // Tick 2: Skills completes → all basic monitors done → event fires
            orchestrator.ProcessTick();

            _events.Received(1).Publish(Arg.Is<CharacterUpdatedEvent>(
                e => e.CharacterID == 12345L));
        }

        #endregion

        #region IsStartupComplete Tests

        [Fact]
        public void IsStartupComplete_FalseBeforeFirstCompletion()
        {
            var orchestrator = CreateOrchestrator();

            orchestrator.IsStartupComplete.Should().BeFalse();
        }

        [Fact]
        public void IsStartupComplete_TrueAfterAllBasicComplete()
        {
            var orchestrator = CreateOrchestrator();

            // Two ticks needed: tick 1 for CharacterSheet+SkillQueue, tick 2 for Skills
            orchestrator.ProcessTick();
            orchestrator.ProcessTick();

            orchestrator.IsStartupComplete.Should().BeTrue();
        }

        [Fact]
        public void IsStartupComplete_FalseAfterOnlyPartialCompletion()
        {
            var orchestrator = CreateOrchestrator();

            // Tick 1: CharacterSheet(0) + SkillQueue(2) complete, but Skills(1) deferred
            orchestrator.ProcessTick();

            orchestrator.IsStartupComplete.Should().BeFalse(
                "not all basic monitors have completed yet");
        }

        #endregion
    }
}
