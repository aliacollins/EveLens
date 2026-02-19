using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanCostCardViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultInjectorPrice_Is900M()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.EstimatedInjectorPrice.Should().Be(900_000_000);
            vm.Dispose();
        }

        // Injector bracket boundary tests
        [Theory]
        [InlineData(0, 500_000)]            // Zero SP
        [InlineData(4_999_999, 500_000)]    // Just under 5M
        [InlineData(5_000_000, 400_000)]    // Exactly 5M
        [InlineData(49_999_999, 400_000)]   // Just under 50M
        [InlineData(50_000_000, 300_000)]   // Exactly 50M
        [InlineData(79_999_999, 300_000)]   // Just under 80M
        [InlineData(80_000_000, 150_000)]   // Exactly 80M
        [InlineData(200_000_000, 150_000)]  // Well above 80M
        public void GetSpPerInjector_ReturnsCorrectBracket(long characterSP, int expectedSP)
        {
            var result = PlanCostCardViewModel.GetSpPerInjector(characterSP);
            result.Should().Be(expectedSP);
        }

        [Fact]
        public void InjectorCount_ComputedCorrectly()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            // 1,000,000 SP needed at < 5M SP bracket = 500K per injector = 2 injectors
            vm.TotalMissingSP = 1_000_000;
            vm.CharacterTotalSP = 0;
            vm.InjectorCount.Should().Be(2);
            vm.Dispose();
        }

        [Fact]
        public void InjectorCount_RoundsUp()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            // 750,000 SP needed at < 5M bracket = 500K per = 1.5 -> rounds up to 2
            vm.TotalMissingSP = 750_000;
            vm.CharacterTotalSP = 0;
            vm.InjectorCount.Should().Be(2);
            vm.Dispose();
        }

        [Fact]
        public void InjectorCostEstimate_UsesDefaultPrice()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.TotalMissingSP = 500_000;
            vm.CharacterTotalSP = 0;
            // 1 injector at 900M = 900M
            vm.InjectorCostEstimate.Should().Be(900_000_000);
            vm.Dispose();
        }

        [Fact]
        public void BooksCost_SetCorrectly()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.BooksCost = 5_000_000;
            vm.NotKnownBooksCost = 3_000_000;
            vm.BooksCost.Should().Be(5_000_000);
            vm.NotKnownBooksCost.Should().Be(3_000_000);
            vm.Dispose();
        }

        [Fact]
        public void ZeroMissingSP_ZeroInjectors()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.TotalMissingSP = 0;
            vm.CharacterTotalSP = 0;
            vm.InjectorCount.Should().Be(0);
            vm.InjectorCostEstimate.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void HighSP_Bracket_ReducesSpPerInjector()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            // At 80M SP, each injector gives 150K SP
            // 1,000,000 SP needed / 150,000 per injector = 6.67 -> rounds up to 7
            vm.CharacterTotalSP = 80_000_000;
            vm.TotalMissingSP = 1_000_000;
            vm.InjectorCount.Should().Be(7);
            vm.Dispose();
        }

        [Fact]
        public void BooksCostText_IncludesISK()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.BooksCost = 1_000_000;
            vm.BooksCostText.Should().Contain("ISK");
            vm.Dispose();
        }

        [Fact]
        public void InjectorCostText_EmptyWhenNoInjectors()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.TotalMissingSP = 0;
            vm.InjectorCostText.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void InjectorCostText_NonEmptyWhenInjectors()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.TotalMissingSP = 500_000;
            vm.InjectorCostText.Should().NotBeEmpty();
            vm.InjectorCostText.Should().Contain("injector");
            vm.Dispose();
        }

        [Fact]
        public void CustomInjectorPrice_AffectsEstimate()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.EstimatedInjectorPrice = 1_000_000_000;
            vm.TotalMissingSP = 500_000;
            vm.CharacterTotalSP = 0;
            // 1 injector at 1B = 1B
            vm.InjectorCostEstimate.Should().Be(1_000_000_000);
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanCostCardViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
