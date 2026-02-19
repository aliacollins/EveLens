using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanGoalCardViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void ProgressPercent_ComputedCorrectly()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.PlanName = "Test Plan";
            vm.TotalSkills = 8;
            vm.SkillsTrained = 5;
            vm.SkillsMissing = 3;
            vm.ProgressPercent.Should().BeApproximately(62.5, 0.1);
            vm.Dispose();
        }

        [Fact]
        public void ProgressPercent_ZeroSkills_ReturnsZero()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.PlanName = "Empty Plan";
            vm.TotalSkills = 0;
            vm.SkillsTrained = 0;
            vm.SkillsMissing = 0;
            vm.ProgressPercent.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void ProgressFraction_ComputedCorrectly()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.TotalSkills = 4;
            vm.SkillsTrained = 3;
            vm.SkillsMissing = 1;
            vm.ProgressFraction.Should().BeApproximately(0.75, 0.01);
            vm.Dispose();
        }

        [Fact]
        public void Properties_SetCorrectly()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.PlanName = "My Plan";
            vm.SkillsTrained = 10;
            vm.SkillsMissing = 5;
            vm.TotalSkills = 15;
            vm.PlanName.Should().Be("My Plan");
            vm.SkillsTrained.Should().Be(10);
            vm.SkillsMissing.Should().Be(5);
            vm.TotalSkills.Should().Be(15);
            vm.Dispose();
        }

        [Fact]
        public void ProgressText_FormattedCorrectly()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.TotalSkills = 10;
            vm.SkillsTrained = 5;
            vm.SkillsMissing = 5;
            vm.ProgressText.Should().Contain("5").And.Contain("10");
            vm.Dispose();
        }

        [Fact]
        public void FullyTrained_ReturnsHundredPercent()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.TotalSkills = 10;
            vm.SkillsTrained = 10;
            vm.SkillsMissing = 0;
            vm.ProgressPercent.Should().Be(100);
            vm.ProgressFraction.Should().Be(1.0);
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_EmptyValues()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.PlanName.Should().BeEmpty();
            vm.SkillsTrained.Should().Be(0);
            vm.SkillsMissing.Should().Be(0);
            vm.TotalSkills.Should().Be(0);
            vm.ProgressPercent.Should().Be(0);
            vm.ProgressFraction.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanGoalCardViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
