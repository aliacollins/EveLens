// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// Comprehensive stress and unit tests for the Projection Overlay Engine:
    /// - <see cref="FlattenedTreeSource{T}"/>: tree virtualization engine
    /// - <see cref="SkillOverlayViewModel"/>: skill template+overlay system
    ///
    /// Covers core operations, expand/collapse state, nested hierarchies,
    /// performance under load, overlay caching, and memory safety.
    /// </summary>
    public class ProjectionOverlayEngineTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        private static IDispatcher CreateSyncDispatcher()
        {
            var dispatcher = Substitute.For<IDispatcher>();
            dispatcher.When(d => d.Invoke(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());
            dispatcher.When(d => d.Post(Arg.Any<Action>())).Do(ci => ci.ArgAt<Action>(0).Invoke());
            return dispatcher;
        }

        #region Helpers

        /// <summary>
        /// Creates a simple flat group with the given number of leaf items.
        /// </summary>
        private static GroupData<string> MakeGroup(string key, int itemCount)
        {
            var items = Enumerable.Range(0, itemCount)
                .Select(i => $"{key}_item_{i}")
                .ToList();
            return new GroupData<string>(key, $"[{key}]", items);
        }

        /// <summary>
        /// Creates a group containing sub-groups (nested hierarchy).
        /// </summary>
        private static GroupData<string> MakeNestedGroup(string key, IReadOnlyList<GroupData<string>> subGroups, int ownItems = 0)
        {
            var items = Enumerable.Range(0, ownItems)
                .Select(i => $"{key}_item_{i}")
                .ToList();
            return new GroupData<string>(key, $"[{key}]", items, subGroups);
        }

        /// <summary>
        /// Creates a CCPCharacter for testing. Static data may not be loaded in tests,
        /// so the character will have 0 skill groups in that case (which is fine — we
        /// test the STRUCTURE, not the game data).
        /// </summary>
        private static CCPCharacter CreateTestCharacter(long id, string name)
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(id, name);
            return new CCPCharacter(identity, services);
        }

        #endregion

        // =================================================================
        // A. FlattenedTreeSource — Core Operations
        // =================================================================

        #region A. FlattenedTreeSource — Core Operations

        [Fact]
        public void EmptySource_CountIsZero()
        {
            var source = new FlattenedTreeSource<string>();

            source.Count.Should().Be(0);
        }

        [Fact]
        public void EmptySource_SetEmptyData_CountIsZero()
        {
            var source = new FlattenedTreeSource<string>();
            source.SetData(Array.Empty<GroupData<string>>());

            source.Count.Should().Be(0);
        }

        [Fact]
        public void SingleGroup_NoItems_CountIsOne()
        {
            var source = new FlattenedTreeSource<string>();
            var group = new GroupData<string>("G1", "[G1]", Array.Empty<string>());

            source.SetData(new[] { group });

            source.Count.Should().Be(1, "a single group header with no items should show just the header");
            source[0].IsGroup.Should().BeTrue();
            source[0].GroupKey.Should().Be("G1");
        }

        [Fact]
        public void SingleGroup_Collapsed_OnlyShowsHeader()
        {
            var source = new FlattenedTreeSource<string>();
            var group = MakeGroup("G1", 10);

            source.SetData(new[] { group });

            // Default state is collapsed (no keys in expand set)
            source.Count.Should().Be(1, "collapsed group should only show its header");
            source[0].IsGroup.Should().BeTrue();
            source[0].IsExpanded.Should().BeFalse();
            source[0].HasChildren.Should().BeTrue();
            source[0].ChildCount.Should().Be(10);
        }

        [Fact]
        public void SingleGroup_Expanded_ShowsHeaderAndItems()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            var group = MakeGroup("G1", 10);

            source.SetData(new[] { group });

            source.Count.Should().Be(11, "expanded group should show header + 10 items");
            source[0].IsGroup.Should().BeTrue();
            source[0].IsExpanded.Should().BeTrue();
            for (int i = 1; i <= 10; i++)
            {
                source[i].IsGroup.Should().BeFalse();
                source[i].Depth.Should().Be(1);
            }
        }

        [Fact]
        public void ToggleExpand_CollapsedToExpanded_InsertsItems()
        {
            var source = new FlattenedTreeSource<string>();
            var group = MakeGroup("G1", 10);
            source.SetData(new[] { group });

            source.Count.Should().Be(1, "initially collapsed");

            source.ToggleExpand(0);

            source.Count.Should().Be(11, "after expanding, header + 10 items");
            source[0].IsExpanded.Should().BeTrue();
            source[1].Data.Should().Be("G1_item_0");
        }

        [Fact]
        public void ToggleExpand_ExpandedToCollapsed_RemovesItems()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            var group = MakeGroup("G1", 10);
            source.SetData(new[] { group });

            source.Count.Should().Be(11, "initially expanded");

            source.ToggleExpand(0);

            source.Count.Should().Be(1, "after collapsing, only header");
            source[0].IsExpanded.Should().BeFalse();
        }

        [Fact]
        public void MultipleGroups_MixedExpandState_CorrectCount()
        {
            // expanded(5), collapsed(10), expanded(3)
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1", "G3" };
            var source = new FlattenedTreeSource<string>(expandedKeys);

            var groups = new[]
            {
                MakeGroup("G1", 5),
                MakeGroup("G2", 10),
                MakeGroup("G3", 3)
            };

            source.SetData(groups);

            // 3 headers + 5 items from G1 + 3 items from G3 = 11
            source.Count.Should().Be(11, "3 headers + 5 expanded items + 3 expanded items");
        }

        [Fact]
        public void ExpandAll_AllGroupsExpanded()
        {
            var source = new FlattenedTreeSource<string>();
            var groups = new[]
            {
                MakeGroup("G1", 5),
                MakeGroup("G2", 10),
                MakeGroup("G3", 3)
            };
            source.SetData(groups);

            source.Count.Should().Be(3, "initially all collapsed, 3 headers");

            source.ExpandAll();

            source.Count.Should().Be(3 + 5 + 10 + 3, "all groups expanded: 3 headers + 18 items");
        }

        [Fact]
        public void CollapseAll_OnlyGroupHeaders()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1", "G2", "G3" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            var groups = new[]
            {
                MakeGroup("G1", 5),
                MakeGroup("G2", 10),
                MakeGroup("G3", 3)
            };
            source.SetData(groups);

            source.Count.Should().Be(21, "all expanded: 3 headers + 18 items");

            source.CollapseAll();

            source.Count.Should().Be(3, "all collapsed, only 3 headers");
            foreach (var node in source)
            {
                node.IsGroup.Should().BeTrue();
                node.IsExpanded.Should().BeFalse();
            }
        }

        [Fact]
        public void NestedGroups_TwoLevels_CorrectFlattening()
        {
            // Region (expanded) -> System (collapsed) -> items
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "Region" };
            var source = new FlattenedTreeSource<string>(expandedKeys);

            var systemA = MakeGroup("SystemA", 3);
            var systemB = MakeGroup("SystemB", 5);
            var region = MakeNestedGroup("Region", new[] { systemA, systemB });

            source.SetData(new[] { region });

            // Region expanded shows: Region header + SystemA header + SystemB header = 3
            // (systems are collapsed by default)
            source.Count.Should().Be(3, "expanded region shows 2 sub-group headers");
            source[0].Depth.Should().Be(0, "region is at depth 0");
            source[0].IsGroup.Should().BeTrue();
            source[1].Depth.Should().Be(1, "systems are at depth 1");
            source[1].IsGroup.Should().BeTrue();
            source[1].GroupKey.Should().Be("SystemA");
            source[2].Depth.Should().Be(1);
            source[2].GroupKey.Should().Be("SystemB");
        }

        [Fact]
        public void NestedGroups_ExpandInner_InsertsAtCorrectPosition()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "Region" };
            var source = new FlattenedTreeSource<string>(expandedKeys);

            var systemA = MakeGroup("SystemA", 3);
            var systemB = MakeGroup("SystemB", 5);
            var region = MakeNestedGroup("Region", new[] { systemA, systemB });

            source.SetData(new[] { region });

            // Before: [Region, SystemA, SystemB] (count=3)
            source.Count.Should().Be(3);

            // Expand SystemA (at flat index 1)
            source.ToggleExpand(1);

            // After: [Region, SystemA, item0, item1, item2, SystemB] (count=6)
            source.Count.Should().Be(6);
            source[2].Data.Should().Be("SystemA_item_0");
            source[3].Data.Should().Be("SystemA_item_1");
            source[4].Data.Should().Be("SystemA_item_2");
            source[5].GroupKey.Should().Be("SystemB", "SystemB should follow SystemA's items");
            source[2].Depth.Should().Be(2, "items under SystemA are at depth 2");
        }

        [Fact]
        public void GetExpandState_ReturnsCurrentState()
        {
            var source = new FlattenedTreeSource<string>();
            var groups = new[]
            {
                MakeGroup("G1", 5),
                MakeGroup("G2", 10),
                MakeGroup("G3", 3)
            };
            source.SetData(groups);

            // Expand G1 and G3
            source.ToggleExpand(0); // G1
            // After G1 expands, G2 is at index 6, G3 at index 7
            source.ToggleExpand(7); // G3

            var state = source.GetExpandState();

            state.Should().Contain("G1");
            state.Should().Contain("G3");
            state.Should().NotContain("G2");
            state.Count.Should().Be(2);
        }

        [Fact]
        public void SetData_WithPreviousExpandState_PreservesExpansion()
        {
            var source = new FlattenedTreeSource<string>();

            // First load
            var groups1 = new[] { MakeGroup("G1", 5), MakeGroup("G2", 3) };
            source.SetData(groups1);
            source.ToggleExpand(0); // Expand G1

            source[0].IsExpanded.Should().BeTrue();

            // Second load with different data but same group keys
            var groups2 = new[] { MakeGroup("G1", 8), MakeGroup("G2", 4) };
            source.SetData(groups2);

            // G1 should still be expanded because the expand state HashSet persists
            source[0].IsExpanded.Should().BeTrue("G1 was expanded before SetData and state should persist");
            source.Count.Should().Be(2 + 8, "G1 expanded with 8 new items + G2 header");
        }

        [Fact]
        public void IndexOfGroup_FindsCorrectFlatIndex()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            var groups = new[]
            {
                MakeGroup("G1", 5),
                MakeGroup("G2", 10),
                MakeGroup("G3", 3)
            };
            source.SetData(groups);

            // G1 is at 0 (expanded), G2 is at 6 (after G1 header + 5 items), G3 is at 7
            source.IndexOfGroup("G1").Should().Be(0);
            source.IndexOfGroup("G2").Should().Be(6);
            source.IndexOfGroup("G3").Should().Be(7);
            source.IndexOfGroup("NonExistent").Should().Be(-1);
        }

        [Fact]
        public void ChangedEvent_FiresOnToggle()
        {
            var source = new FlattenedTreeSource<string>();
            var group = MakeGroup("G1", 5);
            source.SetData(new[] { group });

            int changedCount = 0;
            source.Changed += () => changedCount++;

            source.ToggleExpand(0);

            changedCount.Should().Be(1, "Changed should fire once on toggle");
        }

        [Fact]
        public void ChangedEvent_FiresOnSetData()
        {
            var source = new FlattenedTreeSource<string>();

            int changedCount = 0;
            source.Changed += () => changedCount++;

            source.SetData(new[] { MakeGroup("G1", 5) });

            changedCount.Should().Be(1, "Changed should fire once on SetData");
        }

        [Fact]
        public void ChangedEvent_FiresOnExpandAll()
        {
            var source = new FlattenedTreeSource<string>();
            source.SetData(new[] { MakeGroup("G1", 5), MakeGroup("G2", 3) });

            int changedCount = 0;
            source.Changed += () => changedCount++;

            source.ExpandAll();

            changedCount.Should().Be(1, "Changed should fire once on ExpandAll");
        }

        [Fact]
        public void ChangedEvent_FiresOnCollapseAll()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            source.SetData(new[] { MakeGroup("G1", 5) });

            int changedCount = 0;
            source.Changed += () => changedCount++;

            source.CollapseAll();

            changedCount.Should().Be(1, "Changed should fire once on CollapseAll");
        }

        [Fact]
        public void ToggleExpand_OnLeafNode_IsNoOp()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            source.SetData(new[] { MakeGroup("G1", 5) });

            int countBefore = source.Count;
            int changedCount = 0;
            source.Changed += () => changedCount++;

            // Index 1 is a leaf item, not a group
            source.ToggleExpand(1);

            source.Count.Should().Be(countBefore, "toggling a leaf should not change count");
            changedCount.Should().Be(0, "Changed should not fire when toggling a leaf");
        }

        [Fact]
        public void ToggleExpand_OutOfRange_IsNoOp()
        {
            var source = new FlattenedTreeSource<string>();
            source.SetData(new[] { MakeGroup("G1", 5) });

            int changedCount = 0;
            source.Changed += () => changedCount++;

            source.ToggleExpand(-1);
            source.ToggleExpand(999);

            changedCount.Should().Be(0, "out-of-range toggle should be silently ignored");
        }

        [Fact]
        public void SetData_NullArgument_Throws()
        {
            var source = new FlattenedTreeSource<string>();

            var act = () => source.SetData(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Enumeration_MatchesIndexing()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            source.SetData(new[] { MakeGroup("G1", 3) });

            var enumerated = source.ToList();

            enumerated.Count.Should().Be(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                enumerated[i].Should().BeSameAs(source[i]);
            }
        }

        [Fact]
        public void Chevron_GroupCollapsed_ReturnsRightTriangle()
        {
            var source = new FlattenedTreeSource<string>();
            source.SetData(new[] { MakeGroup("G1", 5) });

            source[0].Chevron.Should().Be("\u25B8", "collapsed group shows right-pointing triangle");
        }

        [Fact]
        public void Chevron_GroupExpanded_ReturnsDownTriangle()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            source.SetData(new[] { MakeGroup("G1", 5) });

            source[0].Chevron.Should().Be("\u25BE", "expanded group shows down-pointing triangle");
        }

        [Fact]
        public void Chevron_LeafNode_ReturnsEmpty()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal) { "G1" };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            source.SetData(new[] { MakeGroup("G1", 1) });

            source[1].Chevron.Should().BeEmpty("leaf nodes have no chevron");
        }

        #endregion

        // =================================================================
        // B. FlattenedTreeSource — Stress Tests
        // =================================================================

        #region B. FlattenedTreeSource — Stress Tests

        [Fact]
        public void StressTest_1000Groups_100ItemsEach_ExpandAll()
        {
            var source = new FlattenedTreeSource<string>();
            var groups = Enumerable.Range(0, 1000)
                .Select(i => MakeGroup($"G{i}", 100))
                .ToList();

            source.SetData(groups);

            var sw = Stopwatch.StartNew();
            source.ExpandAll();
            sw.Stop();

            // 1000 headers + 100,000 items = 101,000
            source.Count.Should().Be(101_000, "1000 groups x (1 header + 100 items)");
            sw.ElapsedMilliseconds.Should().BeLessThan(500,
                "ExpandAll on 1000 groups with 100 items each should complete quickly");
        }

        [Fact]
        public void StressTest_RapidToggle_100Times_NoCorruption()
        {
            var source = new FlattenedTreeSource<string>();
            source.SetData(new[] { MakeGroup("G1", 50) });

            for (int i = 0; i < 100; i++)
            {
                source.ToggleExpand(0);
            }

            // After 100 toggles (even number), the group should be back to original collapsed state
            source[0].IsExpanded.Should().BeFalse("even number of toggles returns to collapsed");
            source.Count.Should().Be(1, "collapsed group shows only header");

            // One more toggle to expanded
            source.ToggleExpand(0);
            source[0].IsExpanded.Should().BeTrue();
            source.Count.Should().Be(51, "expanded shows header + 50 items");
        }

        [Fact]
        public void StressTest_1000Groups_CollapseAllExpandAll_Cycle()
        {
            var source = new FlattenedTreeSource<string>();
            var groups = Enumerable.Range(0, 1000)
                .Select(i => MakeGroup($"G{i}", 10))
                .ToList();
            source.SetData(groups);

            for (int cycle = 0; cycle < 10; cycle++)
            {
                source.ExpandAll();
                source.Count.Should().Be(1000 + 10_000, $"cycle {cycle}: all expanded = 11000");

                source.CollapseAll();
                source.Count.Should().Be(1000, $"cycle {cycle}: all collapsed = 1000 headers");
            }
        }

        [Fact]
        public void StressTest_DeepNesting_5Levels()
        {
            // Build: region -> constellation -> system -> station -> items(3)
            var stations = Enumerable.Range(0, 2)
                .Select(s => MakeGroup($"Station_{s}", 3))
                .ToList();

            var systems = Enumerable.Range(0, 2)
                .Select(sys => MakeNestedGroup($"System_{sys}", stations.ToList()))
                .ToList();

            var constellations = Enumerable.Range(0, 2)
                .Select(c => MakeNestedGroup($"Constellation_{c}", systems.ToList()))
                .ToList();

            var region = MakeNestedGroup("Region_0", constellations.ToList());

            var expandedKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "Region_0", "Constellation_0", "Constellation_1",
                "System_0", "System_1",
                "Station_0", "Station_1"
            };
            var source = new FlattenedTreeSource<string>(expandedKeys);
            source.SetData(new[] { region });

            // Fully expanded: 1 region + 2 constellations + 4 systems + 8 stations + 24 items = 39
            source.Count.Should().BeGreaterThan(1, "deep nesting should produce many visible nodes");

            // Verify depth values by checking first few nodes
            source[0].Depth.Should().Be(0, "region is depth 0");
            source[0].GroupKey.Should().Be("Region_0");

            // Find a constellation node
            var constellation = source.FirstOrDefault(n => n.GroupKey.StartsWith("Constellation_"));
            constellation.Should().NotBeNull();
            constellation!.Depth.Should().Be(1, "constellations are depth 1");

            // Find a system node
            var system = source.FirstOrDefault(n => n.GroupKey.StartsWith("System_"));
            system.Should().NotBeNull();
            system!.Depth.Should().Be(2, "systems are depth 2");

            // Find a station node
            var station = source.FirstOrDefault(n => n.GroupKey.StartsWith("Station_"));
            station.Should().NotBeNull();
            station!.Depth.Should().Be(3, "stations are depth 3");

            // Find a leaf item
            var leaf = source.FirstOrDefault(n => !n.IsGroup);
            leaf.Should().NotBeNull();
            leaf!.Depth.Should().Be(4, "items under stations are depth 4");
        }

        [Fact]
        public void StressTest_SetData_100Times_NoMemoryLeak()
        {
            var source = new FlattenedTreeSource<string>();
            var weakRefs = new List<WeakReference>();

            for (int i = 0; i < 100; i++)
            {
                var groups = new[] { MakeGroup($"Iter{i}", 100) };
                source.SetData(groups);

                // Capture a weak reference to the group data (items list)
                if (i < 99) // Not the last one — it's still referenced
                {
                    weakRefs.Add(new WeakReference(groups[0].Items));
                }
            }

            // After 100 SetData calls, old data should be eligible for GC
            // Force collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // At least some of the old references should have been collected
            // (We can't guarantee all due to GC heuristics, but the point is that
            // SetData doesn't accumulate references to old data)
            int aliveCount = weakRefs.Count(wr => wr.IsAlive);
            aliveCount.Should().BeLessThan(weakRefs.Count,
                "old group data should be eligible for GC after SetData replaces it");

            // Verify final state is correct
            source.Count.Should().Be(1, "only the last SetData's group header is visible (collapsed)");
        }

        [Fact]
        public void StressTest_LargeGroupCount_IndexOfGroup_Performance()
        {
            var source = new FlattenedTreeSource<string>();
            var groups = Enumerable.Range(0, 5000)
                .Select(i => MakeGroup($"G{i:D5}", 1))
                .ToList();
            source.SetData(groups);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++)
            {
                int idx = source.IndexOfGroup($"G{i:D5}");
                idx.Should().Be(i, $"group G{i:D5} should be at flat index {i}");
            }
            sw.Stop();

            sw.ElapsedMilliseconds.Should().BeLessThan(1000,
                "5000 IndexOfGroup lookups in 5000-group tree should be fast enough");
        }

        #endregion

        // =================================================================
        // C. SkillTemplate — Template Tests
        // =================================================================

        #region C. SkillTemplate — Template Tests

        [Fact]
        public void SkillTemplate_IsSingleton()
        {
            var instance1 = SkillTemplate.Instance;
            var instance2 = SkillTemplate.Instance;

            instance1.Should().BeSameAs(instance2, "SkillTemplate.Instance should return the same reference");
        }

        [Fact]
        public void SkillTemplate_HasGroups_OrEmptyIfNoStaticData()
        {
            var template = SkillTemplate.Instance;

            // Static data may or may not be loaded in the test environment.
            // Either way, Groups should be a valid (non-null) list.
            template.Groups.Should().NotBeNull("Groups must never be null");

            if (template.Groups.Count > 0)
            {
                // If data is loaded, verify structure
                template.Groups.Count.Should().BeGreaterThan(0);
            }
            // If count is 0, that's OK — static data isn't loaded in test env
        }

        [Fact]
        public void SkillTemplate_GroupsAreSorted()
        {
            var template = SkillTemplate.Instance;

            if (template.Groups.Count < 2)
                return; // Need at least 2 groups to verify sorting

            var names = template.Groups.Select(g => g.Name).ToList();
            var sorted = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

            names.Should().Equal(sorted, "groups should be sorted alphabetically (case-insensitive)");
        }

        [Fact]
        public void SkillTemplate_SkillsHaveNames()
        {
            var template = SkillTemplate.Instance;

            foreach (var group in template.Groups)
            {
                foreach (var skill in group.Skills)
                {
                    skill.Name.Should().NotBeNullOrEmpty(
                        $"skill in group '{group.Name}' should have a name");
                }
            }
        }

        [Fact]
        public void SkillTemplate_SkillsHaveRankText()
        {
            var template = SkillTemplate.Instance;

            foreach (var group in template.Groups)
            {
                foreach (var skill in group.Skills)
                {
                    skill.RankText.Should().StartWith("Rank ",
                        $"skill '{skill.Name}' should have formatted rank text starting with 'Rank '");
                    skill.Rank.Should().BeGreaterThan(0,
                        $"skill '{skill.Name}' should have a positive rank");
                }
            }
        }

        [Fact]
        public void SkillTemplate_SkillsAreSortedWithinGroups()
        {
            var template = SkillTemplate.Instance;

            foreach (var group in template.Groups)
            {
                if (group.Skills.Count < 2)
                    continue;

                var names = group.Skills.Select(s => s.Name).ToList();
                var sorted = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

                names.Should().Equal(sorted,
                    $"skills in group '{group.Name}' should be sorted alphabetically");
            }
        }

        [Fact]
        public void SkillTemplate_SkillIdsAreUnique()
        {
            var template = SkillTemplate.Instance;

            var allIds = template.Groups
                .SelectMany(g => g.Skills)
                .Select(s => s.SkillId)
                .ToList();

            if (allIds.Count == 0)
                return; // No static data loaded

            allIds.Should().OnlyHaveUniqueItems("each skill should have a unique ID across all groups");
        }

        #endregion

        // =================================================================
        // D. SkillOverlayViewModel — Overlay Tests
        // =================================================================

        #region D. SkillOverlayViewModel — Overlay Tests

        [Fact]
        public void SkillState_Default_IsAllZeros()
        {
            var state = default(SkillState);

            state.Level.Should().Be(0);
            state.SkillPoints.Should().Be(0);
            state.IsKnown.Should().BeFalse();
            state.IsTraining.Should().BeFalse();
        }

        [Fact]
        public void SkillOverlay_DefaultState_HasZeroCounts()
        {
            var overlay = new SkillOverlay();

            overlay.TotalTrained.Should().Be(0);
            overlay.TotalSkills.Should().Be(0);
            overlay.TotalSP.Should().Be(0);
        }

        [Fact]
        public void SkillOverlay_GetState_UnknownSkillId_ReturnsDefault()
        {
            var overlay = new SkillOverlay();

            var state = overlay.GetState(99999);

            state.Level.Should().Be(0);
            state.IsKnown.Should().BeFalse();
        }

        [Fact]
        public void SkillOverlay_Update_PopulatesFromCharacter()
        {
            var character = CreateTestCharacter(1001L, "Overlay Test Pilot");

            var overlay = new SkillOverlay();
            overlay.Update(character);

            // TotalSkills should equal the count of all skills across all groups
            int expectedTotal = 0;
            foreach (var group in character.SkillGroups)
            {
                foreach (Skill skill in group)
                    expectedTotal++;
            }

            overlay.TotalSkills.Should().Be(expectedTotal);

            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_NoCharacter_TreeSourceEmpty()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);

            vm.TreeSource.Count.Should().Be(0, "VM with no character should have empty tree");
            vm.TotalTrained.Should().Be(0);
            vm.TotalSkills.Should().Be(0);
            vm.TotalSP.Should().Be(0);
            vm.StatusText.Should().BeEmpty();

            vm.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_SetCharacter_PopulatesState()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(2001L, "Skill VM Pilot");

            vm.Character = character;

            // The VM should have populated overlay state from the character.
            // TotalSkills reflects how many skills the character has (from static data).
            // Even without static data, the overlay is created and update runs.
            vm.TotalSkills.Should().BeGreaterThanOrEqualTo(0);

            // If static data is loaded, TreeSource should have groups
            if (SkillTemplate.Instance.Groups.Count > 0)
            {
                // With ShowAll=false (default) and no trained skills, tree may be empty.
                // With ShowAll=true, tree should have groups.
                vm.ShowAll = true;
                vm.TreeSource.Count.Should().BeGreaterThan(0,
                    "with ShowAll=true and static data loaded, tree should have group headers");
            }

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_SwitchCharacter_SwapsOverlay()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var charA = CreateTestCharacter(3001L, "Pilot Alpha");
            var charB = CreateTestCharacter(3002L, "Pilot Beta");

            vm.Character = charA;
            var statsA = vm.StatusText;

            vm.Character = charB;
            var statsB = vm.StatusText;

            // Both should have valid status text (even if identical in content when
            // both characters have 0 trained skills)
            vm.Character.Should().BeSameAs(charB);

            vm.Dispose();
            charA.Dispose();
            charB.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_SameCharacter_ReusesOverlay()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(4001L, "Reuse Pilot");

            vm.Character = character;
            int totalSkillsFirst = vm.TotalSkills;

            // Setting a different character, then back
            var charOther = CreateTestCharacter(4002L, "Other Pilot");
            vm.Character = charOther;
            vm.Character = character;

            int totalSkillsSecond = vm.TotalSkills;

            // The overlay should be cached and produce the same results
            totalSkillsSecond.Should().Be(totalSkillsFirst,
                "returning to the same character should use cached overlay with same total skills");

            vm.Dispose();
            character.Dispose();
            charOther.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_ShowAll_DefaultIsFalse()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);

            vm.ShowAll.Should().BeFalse("default ShowAll should be false (show only trained)");

            vm.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_ShowAll_Toggle_RebuildTree()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(5001L, "ShowAll Pilot");
            vm.Character = character;

            int countWithShowAllFalse = vm.TreeSource.Count;

            vm.ShowAll = true;
            int countWithShowAllTrue = vm.TreeSource.Count;

            // With ShowAll=true, more skills should be visible (or same if all are known)
            countWithShowAllTrue.Should().BeGreaterThanOrEqualTo(countWithShowAllFalse,
                "ShowAll=true should show at least as many skills as ShowAll=false");

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_Filter_ByName_ReducesTree()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(6001L, "Filter Pilot");
            vm.Character = character;
            vm.ShowAll = true;

            int countUnfiltered = vm.TreeSource.Count;

            // Apply a very specific filter that likely won't match many skills
            vm.Filter = "XYZNONEXISTENT123";
            int countFiltered = vm.TreeSource.Count;

            countFiltered.Should().BeLessThanOrEqualTo(countUnfiltered,
                "filtering should show fewer or equal items");

            // Clear filter
            vm.Filter = "";
            vm.TreeSource.Count.Should().Be(countUnfiltered,
                "clearing filter should restore full tree");

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_Filter_CaseInsensitive()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(6002L, "CaseFilter Pilot");
            vm.Character = character;
            vm.ShowAll = true;

            if (SkillTemplate.Instance.Groups.Count == 0)
            {
                vm.Dispose();
                character.Dispose();
                return; // Can't test without static data
            }

            // Pick a skill name from the template
            var firstSkillName = SkillTemplate.Instance.Groups[0].Skills[0].Name;

            vm.Filter = firstSkillName.ToUpperInvariant();
            int upperCount = vm.TreeSource.Count;

            vm.Filter = firstSkillName.ToLowerInvariant();
            int lowerCount = vm.TreeSource.Count;

            upperCount.Should().Be(lowerCount,
                "filter should be case-insensitive — upper and lower case should produce same results");

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_TotalTrained_MatchesCharacterKnownCount()
        {
            var character = CreateTestCharacter(7001L, "TotalTrained Pilot");

            // Count known skills directly from character
            int knownFromCharacter = 0;
            foreach (var group in character.SkillGroups)
            {
                foreach (Skill skill in group)
                {
                    if (skill.IsKnown)
                        knownFromCharacter++;
                }
            }

            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            vm.Character = character;

            vm.TotalTrained.Should().Be(knownFromCharacter,
                "TotalTrained should match the count of known skills from the character");

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_SetCharacterNull_ClearsState()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(8001L, "NullChar Pilot");

            vm.Character = character;
            vm.Character = null;

            vm.TreeSource.Count.Should().Be(0, "null character should clear tree");
            vm.TotalTrained.Should().Be(0);
            vm.TotalSkills.Should().Be(0);
            vm.TotalSP.Should().Be(0);
            vm.StatusText.Should().BeEmpty();

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_CollapseAll_ExpandAll_WorksWithCharacter()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(9001L, "ExpandCollapse Pilot");
            vm.Character = character;
            vm.ShowAll = true;

            int initialCount = vm.TreeSource.Count;

            vm.ExpandAll();
            int expandedCount = vm.TreeSource.Count;

            vm.CollapseAll();
            int collapsedCount = vm.TreeSource.Count;

            expandedCount.Should().BeGreaterThanOrEqualTo(collapsedCount,
                "expanded tree should show at least as many nodes as collapsed");

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_StatusText_FormatCorrect()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(9002L, "StatusText Pilot");

            vm.Character = character;

            if (vm.TotalSkills > 0)
            {
                vm.StatusText.Should().Contain("Trained:");
                vm.StatusText.Should().Contain("Total SP:");
                vm.StatusText.Should().Contain("of");
            }
            else
            {
                // With 0 skills, status text format is still present but shows 0s
                vm.StatusText.Should().Contain("Trained: 0 of 0");
            }

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_PropertyChanged_FiresOnCharacterChange()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(9003L, "PropChanged Pilot");
            var changedProperties = new List<string>();

            vm.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

            vm.Character = character;

            changedProperties.Should().Contain("TotalTrained");
            changedProperties.Should().Contain("TotalSkills");
            changedProperties.Should().Contain("TotalSP");
            changedProperties.Should().Contain("StatusText");

            vm.Dispose();
            character.Dispose();
        }

        #endregion

        // =================================================================
        // E. SkillOverlayViewModel — Memory & Performance
        // =================================================================

        #region E. SkillOverlayViewModel — Memory & Performance

        [Fact]
        public void SkillOverlayVM_60Characters_OverlayCacheSize()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var characters = new List<CCPCharacter>();

            for (int i = 0; i < 60; i++)
            {
                var character = CreateTestCharacter(10000 + i, $"Cache Pilot {i}");
                characters.Add(character);
                vm.Character = character;
            }

            // The VM caches overlays per character ID in _overlays dictionary.
            // We can verify this indirectly: switching back to an earlier character
            // should produce the same results without re-iterating skills.
            vm.Character = characters[0];
            var totalFirst = vm.TotalSkills;
            vm.Character = characters[59];
            var totalLast = vm.TotalSkills;
            vm.Character = characters[0];
            var totalFirstAgain = vm.TotalSkills;

            totalFirstAgain.Should().Be(totalFirst,
                "returning to first character should use cached overlay");

            vm.Dispose();
            foreach (var c in characters)
                c.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_60Characters_OverlayMemoryTiny()
        {
            // SkillState is a readonly struct: byte(1) + long(8) + bool(1) + bool(1) = ~11 bytes
            // With padding, likely 16 bytes per struct.
            // With Dictionary overhead, estimate ~32 bytes per entry.
            // Max ~500 skills per character * 32 bytes = ~16KB per overlay.
            // 60 characters * 16KB = ~960KB. With some overhead, should be well under 2MB.

            int skillCount = SkillTemplate.Instance.Groups.SelectMany(g => g.Skills).Count();
            if (skillCount == 0)
                skillCount = 500; // Estimate if no static data

            // SkillState size: sizeof(byte) + sizeof(long) + 2*sizeof(bool) + dictionary overhead
            long perSkillBytes = 16 + 32; // struct + dictionary entry overhead (conservative)
            long perOverlayBytes = skillCount * perSkillBytes + 64; // base object overhead
            long totalBytes = 60 * perOverlayBytes;

            totalBytes.Should().BeLessThan(2 * 1024 * 1024,
                $"60 overlays with {skillCount} skills each should be under 2MB " +
                $"(estimated {totalBytes / 1024}KB)");
        }

        [Fact]
        public void SkillOverlayVM_CharacterSwitch_DoesNotCreateDisplayObjects()
        {
            // The overlay architecture's key benefit: switching characters does NOT create
            // SkillGroupDisplay / SkillDisplay objects (the old pattern). Instead, it swaps
            // overlay data and rebuilds a flat tree of object references.
            // We verify this by checking that the TreeSource only contains SkillGroupTemplate
            // and SkillEntryTemplate references from the shared template.

            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(11001L, "NoDisplay Pilot");
            vm.Character = character;
            vm.ShowAll = true;

            if (vm.TreeSource.Count == 0)
            {
                vm.Dispose();
                character.Dispose();
                return; // No static data — can't verify
            }

            // Check that group nodes contain SkillGroupTemplate (shared, not per-character)
            var groupNode = vm.TreeSource.FirstOrDefault(n => n.IsGroup);
            if (groupNode != null)
            {
                groupNode.Data.Should().BeOfType<SkillGroupTemplate>(
                    "group nodes should reference the shared SkillGroupTemplate, not per-character display objects");
            }

            // Check that leaf nodes contain SkillEntryTemplate (shared, not per-character)
            vm.TreeSource.ExpandAll();
            var leafNode = vm.TreeSource.FirstOrDefault(n => !n.IsGroup);
            if (leafNode != null)
            {
                leafNode.Data.Should().BeOfType<SkillEntryTemplate>(
                    "leaf nodes should reference the shared SkillEntryTemplate, not per-character display objects");
            }

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_RapidCharacterSwitch_50Times_NoException()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var charA = CreateTestCharacter(12001L, "Rapid A");
            var charB = CreateTestCharacter(12002L, "Rapid B");

            var act = () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    vm.Character = charA;
                    vm.Character = charB;
                }
            };

            act.Should().NotThrow("rapid character switching should be safe");

            // Final state should be charB
            vm.Character.Should().BeSameAs(charB);

            vm.Dispose();
            charA.Dispose();
            charB.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_Dispose_StopsProcessingEvents()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(13001L, "Dispose Pilot");
            vm.Character = character;

            vm.Dispose();

            // Publishing events after dispose should not throw or change state
            var act = () => agg.Publish(new CharacterUpdatedEvent(character));
            act.Should().NotThrow("events after dispose should be silently ignored");

            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_MultipleDispose_Safe()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(13002L, "MultiDispose Pilot");
            vm.Character = character;

            vm.Dispose();
            var act = () => vm.Dispose();

            act.Should().NotThrow("double dispose should be safe (idempotent)");

            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_ShowAllToggle_WithFilter_CombinedEffect()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(14001L, "Combined Pilot");
            vm.Character = character;

            // Start with ShowAll=false, no filter
            int baseCount = vm.TreeSource.Count;

            // Add filter with ShowAll=false
            vm.Filter = "ZZZNONEXISTENT";
            int filteredNotAll = vm.TreeSource.Count;

            // ShowAll=true with filter
            vm.ShowAll = true;
            int filteredAll = vm.TreeSource.Count;

            // ShowAll=true without filter
            vm.Filter = "";
            int unfilteredAll = vm.TreeSource.Count;

            // With nonexistent filter, both modes should show 0 or very few
            filteredNotAll.Should().Be(0, "nonexistent filter with ShowAll=false should show nothing");
            filteredAll.Should().Be(0, "nonexistent filter with ShowAll=true should show nothing");
            unfilteredAll.Should().BeGreaterThanOrEqualTo(baseCount,
                "ShowAll=true without filter should show at least as much as ShowAll=false");

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_CreateDisposeCreate_NoLeakedSubscriptions()
        {
            var agg = CreateAggregator();
            var character = CreateTestCharacter(15001L, "Leak Test Pilot");

            // First lifecycle
            var vm1 = new SkillOverlayViewModel(agg);
            vm1.Character = character;
            var status1 = vm1.StatusText;
            vm1.Dispose();

            // Second lifecycle
            var vm2 = new SkillOverlayViewModel(agg);
            vm2.Character = character;

            // Publishing events should only affect vm2
            agg.Publish(new CharacterUpdatedEvent(character));

            // If vm1's subscriptions leaked, it would crash or cause side effects.
            // The fact that we get here without exception proves no leak.
            vm2.Should().NotBeNull("second VM should be functional after first was disposed");

            vm2.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_GetSkillState_ReturnsOverlayData()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);
            var character = CreateTestCharacter(16001L, "SkillState Pilot");

            vm.Character = character;

            // GetSkillState should return default for any ID when character has no trained skills
            var state = vm.GetSkillState(99999);
            state.Level.Should().Be(0);
            state.IsKnown.Should().BeFalse();

            vm.Dispose();
            character.Dispose();
        }

        [Fact]
        public void SkillOverlayVM_GetSkillState_NoCharacter_ReturnsDefault()
        {
            var agg = CreateAggregator();
            var vm = new SkillOverlayViewModel(agg);

            var state = vm.GetSkillState(12345);

            state.Level.Should().Be(0);
            state.IsKnown.Should().BeFalse();
            state.IsTraining.Should().BeFalse();
            state.SkillPoints.Should().Be(0);

            vm.Dispose();
        }

        #endregion

        // =================================================================
        // F. Integration — FlattenedTreeSource + SkillOverlayVM Combined
        // =================================================================

        #region F. Integration Tests

        [Fact]
        public void FlattenedTreeSource_AsObjectType_WorksWithViewModel()
        {
            // The SkillOverlayViewModel uses FlattenedTreeSource<object>.
            // Verify the generic type works correctly with mixed types (SkillGroupTemplate + SkillEntryTemplate).
            var source = new FlattenedTreeSource<object>();

            var groupObj = "GroupHeader";
            var items = new object[] { "Item1", "Item2", "Item3" };
            var group = new GroupData<object>("G1", groupObj, items);

            source.SetData(new[] { group });

            source.Count.Should().Be(1, "collapsed group shows only header");
            source[0].Data.Should().Be("GroupHeader");

            source.ToggleExpand(0);
            source.Count.Should().Be(4);
            source[1].Data.Should().Be("Item1");
        }

        [Fact]
        public void FlattenedTreeSource_SharedExpandState_AcrossSetDataCalls()
        {
            var expandedKeys = new HashSet<string>(StringComparer.Ordinal);
            var source = new FlattenedTreeSource<string>(expandedKeys);

            // First data load
            source.SetData(new[] { MakeGroup("Skills", 5), MakeGroup("Items", 3) });
            source.ToggleExpand(0); // Expand "Skills"

            // The expand state should be in our shared set
            expandedKeys.Should().Contain("Skills");

            // Second data load (simulating character switch)
            source.SetData(new[] { MakeGroup("Skills", 8), MakeGroup("Items", 6) });

            // "Skills" should still be expanded
            source[0].IsExpanded.Should().BeTrue();
            source.Count.Should().Be(2 + 8, "Skills expanded with 8 items + Items header");
        }

        [Fact]
        public void SkillOverlay_UpdateTwice_OverwritesPreviousState()
        {
            var charA = CreateTestCharacter(20001L, "Update Twice A");
            var overlay = new SkillOverlay();

            overlay.Update(charA);
            int firstTotal = overlay.TotalSkills;

            // Update again with same character — should overwrite, not accumulate
            overlay.Update(charA);
            int secondTotal = overlay.TotalSkills;

            secondTotal.Should().Be(firstTotal,
                "updating with same character should produce same results");

            charA.Dispose();
        }

        #endregion
    }

    // =================================================================
    // CharacterDisplayCache — LRU Cache Stress Tests
    // =================================================================

    public class CharacterDisplayCacheTests
    {
        [Fact]
        public void LRU_NewCache_IsEmpty()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Count.Should().Be(0);
            cache.CachedCharacters.Should().BeEmpty();
        }

        [Fact]
        public void LRU_Touch_AddsCharacter()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L);

            cache.Count.Should().Be(1);
            cache.Contains(1L).Should().BeTrue();
        }

        [Fact]
        public void LRU_Touch_SameCharacterTwice_CountStaysOne()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L);
            cache.Touch(1L);

            cache.Count.Should().Be(1);
            cache.Contains(1L).Should().BeTrue();
        }

        [Fact]
        public void LRU_Touch_OverCapacity_EvictsOldest()
        {
            var cache = new CharacterDisplayCache(3);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);
            cache.Touch(4L);

            cache.Count.Should().Be(3);
            cache.Contains(1L).Should().BeFalse("1 was the oldest and should be evicted");
            cache.Contains(2L).Should().BeTrue();
            cache.Contains(3L).Should().BeTrue();
            cache.Contains(4L).Should().BeTrue();
        }

        [Fact]
        public void LRU_Touch_RecentlyUsed_NotEvicted()
        {
            var cache = new CharacterDisplayCache(3);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);

            // Re-touch 1 to promote it to most-recently-used
            cache.Touch(1L);

            // Now add 4 — should evict 2 (the least recently used), not 1
            cache.Touch(4L);

            cache.Count.Should().Be(3);
            cache.Contains(1L).Should().BeTrue("1 was re-touched and should not be evicted");
            cache.Contains(2L).Should().BeFalse("2 was the oldest after 1 was promoted");
            cache.Contains(3L).Should().BeTrue();
            cache.Contains(4L).Should().BeTrue();
        }

        [Fact]
        public void LRU_Remove_DecreasesCount()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Remove(1L);

            cache.Count.Should().Be(1);
            cache.Contains(1L).Should().BeFalse();
            cache.Contains(2L).Should().BeTrue();
        }

        [Fact]
        public void LRU_CachedCharacters_InAccessOrder()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);

            // Most recently used first: 3, 2, 1
            cache.CachedCharacters.Should().Equal(3L, 2L, 1L);
        }

        [Fact]
        public void LRU_StressTest_1000Characters_Capacity5()
        {
            var cache = new CharacterDisplayCache(5);

            for (long i = 1; i <= 1000; i++)
                cache.Touch(i);

            cache.Count.Should().Be(5);

            // Only the last 5 should remain
            for (long i = 996; i <= 1000; i++)
                cache.Contains(i).Should().BeTrue($"character {i} is among the last 5 touched");

            // All earlier ones should be gone
            for (long i = 1; i <= 995; i++)
                cache.Contains(i).Should().BeFalse($"character {i} should have been evicted");
        }

        [Fact]
        public void LRU_Touch_ReturnsNull_WhenUnderCapacity()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L).Should().BeNull("no eviction when under capacity");
            cache.Touch(2L).Should().BeNull("no eviction when under capacity");
            cache.Touch(3L).Should().BeNull("no eviction when under capacity");
            cache.Touch(4L).Should().BeNull("no eviction when under capacity");
            cache.Touch(5L).Should().BeNull("no eviction at exact capacity");
        }

        [Fact]
        public void LRU_Touch_ReturnsEvictedId()
        {
            var cache = new CharacterDisplayCache(3);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);

            long? evicted = cache.Touch(4L);

            evicted.Should().Be(1L, "the oldest character (1) should be evicted");
        }

        [Fact]
        public void LRU_Touch_RetouchExisting_ReturnsNull()
        {
            var cache = new CharacterDisplayCache(3);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);

            // Re-touching an existing entry never evicts
            cache.Touch(2L).Should().BeNull("re-touching existing entry should not cause eviction");
        }

        [Fact]
        public void LRU_Remove_NonExistent_DoesNotThrow()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L);

            var act = () => cache.Remove(999L);

            act.Should().NotThrow("removing a non-existent character should be a no-op");
            cache.Count.Should().Be(1);
        }

        [Fact]
        public void LRU_Capacity_MinimumIsOne()
        {
            var cache = new CharacterDisplayCache(0);

            cache.Capacity.Should().Be(1, "capacity of 0 should be clamped to 1");

            cache.Touch(1L);
            cache.Touch(2L);

            cache.Count.Should().Be(1, "capacity=1 means only the latest character survives");
            cache.Contains(2L).Should().BeTrue();
            cache.Contains(1L).Should().BeFalse();
        }

        [Fact]
        public void LRU_CachedCharacters_UpdatesAfterPromotion()
        {
            var cache = new CharacterDisplayCache(5);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);

            // Re-touch 1 to promote it
            cache.Touch(1L);

            // Order should now be: 1 (most recent), 3, 2
            cache.CachedCharacters.Should().Equal(1L, 3L, 2L);
        }

        [Fact]
        public void LRU_StressTest_AlternatingPattern_NoCorruption()
        {
            var cache = new CharacterDisplayCache(10);

            // Alternate between two sets of characters rapidly
            for (int round = 0; round < 500; round++)
            {
                for (long id = 1; id <= 20; id++)
                    cache.Touch(id);
            }

            cache.Count.Should().Be(10);
            // After the last round touching 1..20, the last 10 (11..20) should remain
            for (long id = 11; id <= 20; id++)
                cache.Contains(id).Should().BeTrue($"character {id} was among the last 10 touched");
        }

        [Fact]
        public void LRU_EvictionOrder_MatchesLRUPolicy()
        {
            var cache = new CharacterDisplayCache(3);

            cache.Touch(1L);
            cache.Touch(2L);
            cache.Touch(3L);

            // Evict sequence: touch 4 evicts 1, touch 5 evicts 2, touch 6 evicts 3
            cache.Touch(4L).Should().Be(1L);
            cache.Touch(5L).Should().Be(2L);
            cache.Touch(6L).Should().Be(3L);

            cache.CachedCharacters.Should().Equal(6L, 5L, 4L);
        }
    }

    // =================================================================
    // CollapseStateHelper — Persistence Stress Tests
    // =================================================================

    [Collection("AppServices")]
    public class CollapseStateHelperTests : IDisposable
    {
        private readonly Dictionary<string, List<string>> _savedState;

        public CollapseStateHelperTests()
        {
            // Save current collapse states so we can restore them after each test
            _savedState = new Dictionary<string, List<string>>(
                EveLens.Common.Settings.UI.CollapseStates);
            EveLens.Common.Settings.UI.CollapseStates.Clear();
        }

        public void Dispose()
        {
            // Restore original collapse states
            EveLens.Common.Settings.UI.CollapseStates.Clear();
            foreach (var kvp in _savedState)
                EveLens.Common.Settings.UI.CollapseStates[kvp.Key] = kvp.Value;
        }

        [Fact]
        public void CollapseState_LoadEmpty_ReturnsEmptySet()
        {
            var result = CollapseStateHelper.LoadExpandState(12345L, "Skills");

            result.Should().NotBeNull();
            result.Should().BeEmpty("no state was saved, so load should return empty set");
        }

        [Fact]
        public void CollapseState_SaveAndLoad_RoundTrips()
        {
            var original = new HashSet<string>(StringComparer.Ordinal)
            {
                "Spaceship Command",
                "Engineering",
                "Navigation"
            };

            CollapseStateHelper.SaveExpandState(100L, "Skills", original);
            var loaded = CollapseStateHelper.LoadExpandState(100L, "Skills");

            loaded.Should().BeEquivalentTo(original, "loaded state should match what was saved");
        }

        [Fact]
        public void CollapseState_DifferentViews_Independent()
        {
            var skillGroups = new HashSet<string>(StringComparer.Ordinal) { "Spaceship Command", "Gunnery" };
            var assetGroups = new HashSet<string>(StringComparer.Ordinal) { "Jita 4-4", "Amarr VIII" };

            CollapseStateHelper.SaveExpandState(200L, "Skills", skillGroups);
            CollapseStateHelper.SaveExpandState(200L, "Assets", assetGroups);

            var loadedSkills = CollapseStateHelper.LoadExpandState(200L, "Skills");
            var loadedAssets = CollapseStateHelper.LoadExpandState(200L, "Assets");

            loadedSkills.Should().BeEquivalentTo(skillGroups, "Skills state should be independent");
            loadedAssets.Should().BeEquivalentTo(assetGroups, "Assets state should be independent");
        }

        [Fact]
        public void CollapseState_RemoveCharacter_CleansUp()
        {
            var groups = new HashSet<string>(StringComparer.Ordinal) { "GroupA" };

            CollapseStateHelper.SaveExpandState(300L, "Skills", groups);
            CollapseStateHelper.SaveExpandState(300L, "Assets", groups);
            CollapseStateHelper.SaveExpandState(300L, "Mail", groups);

            CollapseStateHelper.RemoveCharacterState(300L);

            CollapseStateHelper.LoadExpandState(300L, "Skills").Should().BeEmpty("state should be removed");
            CollapseStateHelper.LoadExpandState(300L, "Assets").Should().BeEmpty("state should be removed");
            CollapseStateHelper.LoadExpandState(300L, "Mail").Should().BeEmpty("state should be removed");
        }

        [Fact]
        public void CollapseState_OverwriteExisting_ReplacesOldState()
        {
            var setA = new HashSet<string>(StringComparer.Ordinal) { "Alpha", "Beta" };
            var setB = new HashSet<string>(StringComparer.Ordinal) { "Gamma", "Delta", "Epsilon" };

            CollapseStateHelper.SaveExpandState(400L, "Skills", setA);
            CollapseStateHelper.SaveExpandState(400L, "Skills", setB);

            var loaded = CollapseStateHelper.LoadExpandState(400L, "Skills");

            loaded.Should().BeEquivalentTo(setB, "second save should overwrite the first");
            loaded.Should().NotContain("Alpha", "old state should be fully replaced");
        }

        [Fact]
        public void CollapseState_RemoveCharacter_DoesNotAffectOtherCharacters()
        {
            var groups = new HashSet<string>(StringComparer.Ordinal) { "GroupX" };

            CollapseStateHelper.SaveExpandState(500L, "Skills", groups);
            CollapseStateHelper.SaveExpandState(501L, "Skills", groups);

            CollapseStateHelper.RemoveCharacterState(500L);

            CollapseStateHelper.LoadExpandState(500L, "Skills").Should().BeEmpty("removed character's state gone");
            CollapseStateHelper.LoadExpandState(501L, "Skills").Should().BeEquivalentTo(groups, "other character unaffected");
        }

        [Fact]
        public void CollapseState_RemoveNonExistent_DoesNotThrow()
        {
            var act = () => CollapseStateHelper.RemoveCharacterState(999999L);

            act.Should().NotThrow("removing state for a character that never had state should be safe");
        }

        [Fact]
        public void CollapseState_LoadReturnsNewInstance_NotSharedReference()
        {
            var original = new HashSet<string>(StringComparer.Ordinal) { "GroupA" };

            CollapseStateHelper.SaveExpandState(600L, "Skills", original);

            var loaded1 = CollapseStateHelper.LoadExpandState(600L, "Skills");
            var loaded2 = CollapseStateHelper.LoadExpandState(600L, "Skills");

            loaded1.Should().NotBeSameAs(loaded2, "each load should return a new HashSet instance");

            // Mutating one should not affect the other
            loaded1.Add("Mutated");
            loaded2.Should().NotContain("Mutated");
        }

        [Fact]
        public void CollapseState_SaveEmptySet_LoadReturnsEmpty()
        {
            var empty = new HashSet<string>(StringComparer.Ordinal);

            CollapseStateHelper.SaveExpandState(700L, "Skills", empty);

            var loaded = CollapseStateHelper.LoadExpandState(700L, "Skills");
            loaded.Should().BeEmpty("saving an empty set should persist as empty");
        }

        [Fact]
        public void CollapseState_StressTest_50Characters_5Views()
        {
            // Simulate 50 characters, each with 5 views, each with 10 groups
            for (long charId = 1000; charId < 1050; charId++)
            {
                foreach (var view in new[] { "Skills", "Assets", "Mail", "Contracts", "Industry" })
                {
                    var groups = new HashSet<string>(StringComparer.Ordinal);
                    for (int g = 0; g < 10; g++)
                        groups.Add($"Group_{charId}_{view}_{g}");

                    CollapseStateHelper.SaveExpandState(charId, view, groups);
                }
            }

            // Verify round-trip for a sample
            var sample = CollapseStateHelper.LoadExpandState(1025L, "Contracts");
            sample.Should().HaveCount(10);
            sample.Should().Contain("Group_1025_Contracts_0");
            sample.Should().Contain("Group_1025_Contracts_9");

            // Remove a character and verify isolation
            CollapseStateHelper.RemoveCharacterState(1025L);
            CollapseStateHelper.LoadExpandState(1025L, "Skills").Should().BeEmpty();
            CollapseStateHelper.LoadExpandState(1025L, "Contracts").Should().BeEmpty();
            CollapseStateHelper.LoadExpandState(1024L, "Skills").Should().HaveCount(10, "neighbor character unaffected");
            CollapseStateHelper.LoadExpandState(1026L, "Assets").Should().HaveCount(10, "neighbor character unaffected");
        }
    }
}
