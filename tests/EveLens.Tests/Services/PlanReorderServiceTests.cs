// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for PlanReorderService — prerequisite-aware drag-and-drop validation.
    /// Uses NullCharacterServices since we need real Plan + PlanEntry objects.
    /// </summary>
    public class PlanReorderServiceTests
    {
        [Fact]
        public void CanMove_EmptyList_ReturnsFalse()
        {
            var entries = new List<PlanEntry>();
            PlanReorderService.CanMove(entries, new[] { 0 }, 1).Should().BeFalse();
        }

        [Fact]
        public void CanMove_EmptySelection_ReturnsFalse()
        {
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(1L, "Test");
            var character = new CCPCharacter(identity, services);
            var plan = new Plan(character);

            PlanReorderService.CanMove(plan.ToList(), new int[0], 0).Should().BeFalse();
        }
    }
}
