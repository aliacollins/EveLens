// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.ViewModels;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class ItemPropertiesViewModelTests
    {
        [Fact]
        public void CanInstantiate_WithNullItem()
        {
            var vm = new ItemPropertiesViewModel(null);
            vm.Should().NotBeNull();
        }

        [Fact]
        public void Sections_WithNullItem_IsEmpty()
        {
            var vm = new ItemPropertiesViewModel(null);
            vm.Sections.Should().NotBeNull();
            vm.Sections.Should().BeEmpty();
        }

        [Fact]
        public void Sections_IsNotNull()
        {
            var vm = new ItemPropertiesViewModel(null);
            vm.Sections.Should().NotBeNull();
        }
    }
}
