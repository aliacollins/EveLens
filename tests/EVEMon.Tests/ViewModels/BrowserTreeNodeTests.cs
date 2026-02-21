// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EVEMon.Common.ViewModels;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class BrowserTreeNodeTests
    {
        [Fact]
        public void FlattenVisible_EmptyList_ReturnsEmpty()
        {
            var result = BrowserTreeNode.FlattenVisible(new List<BrowserTreeNode>());
            result.Should().BeEmpty();
        }

        [Fact]
        public void FlattenVisible_EmptyEnumerable_ReturnsNotNull()
        {
            var result = BrowserTreeNode.FlattenVisible(new List<BrowserTreeNode>());
            result.Should().NotBeNull();
        }
    }
}
