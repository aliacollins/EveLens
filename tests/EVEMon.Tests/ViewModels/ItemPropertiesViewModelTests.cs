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
