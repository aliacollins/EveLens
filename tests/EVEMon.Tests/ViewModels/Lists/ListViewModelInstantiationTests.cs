using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels.Lists
{
    /// <summary>
    /// Tests that all list ViewModels can be instantiated, disposed, and have correct defaults.
    /// Domain-specific filter/sort/group tests are tested via the base ListViewModel tests.
    /// </summary>
    public class ListViewModelInstantiationTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void MarketOrdersListViewModel_CanInstantiate()
        {
            var vm = new MarketOrdersListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void MarketOrdersListViewModel_DisposeUnsubscribes()
        {
            var vm = new MarketOrdersListViewModel(CreateAggregator());
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void MarketOrdersListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new MarketOrdersListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void ContractsListViewModel_CanInstantiate()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void ContractsListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new ContractsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void IndustryJobsListViewModel_CanInstantiate()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void IndustryJobsListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void WalletJournalListViewModel_CanInstantiate()
        {
            var vm = new WalletJournalListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void WalletJournalListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new WalletJournalListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void WalletTransactionsListViewModel_CanInstantiate()
        {
            var vm = new WalletTransactionsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void WalletTransactionsListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new WalletTransactionsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void MailMessagesListViewModel_CanInstantiate()
        {
            var vm = new MailMessagesListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void MailMessagesListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new MailMessagesListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void NotificationsListViewModel_CanInstantiate()
        {
            var vm = new NotificationsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void NotificationsListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new NotificationsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void KillLogListViewModel_CanInstantiate()
        {
            var vm = new KillLogListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void KillLogListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new KillLogListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void PlanetaryListViewModel_CanInstantiate()
        {
            var vm = new PlanetaryListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void PlanetaryListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new PlanetaryListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void ResearchPointsListViewModel_CanInstantiate()
        {
            var vm = new ResearchPointsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void ResearchPointsListViewModel_RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new ResearchPointsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void AllListViewModels_SortAscendingDefault_IsTrue()
        {
            var agg = CreateAggregator();
            new MarketOrdersListViewModel(agg).SortAscending.Should().BeTrue();
            new ContractsListViewModel(agg).SortAscending.Should().BeTrue();
            new IndustryJobsListViewModel(agg).SortAscending.Should().BeTrue();
            new WalletJournalListViewModel(agg).SortAscending.Should().BeTrue();
            new WalletTransactionsListViewModel(agg).SortAscending.Should().BeTrue();
            new MailMessagesListViewModel(agg).SortAscending.Should().BeTrue();
            new NotificationsListViewModel(agg).SortAscending.Should().BeTrue();
            new KillLogListViewModel(agg).SortAscending.Should().BeTrue();
            new PlanetaryListViewModel(agg).SortAscending.Should().BeTrue();
            new ResearchPointsListViewModel(agg).SortAscending.Should().BeTrue();
        }

        [Fact]
        public void AllListViewModels_TextFilterDefault_IsEmpty()
        {
            var agg = CreateAggregator();
            new MarketOrdersListViewModel(agg).TextFilter.Should().BeEmpty();
            new ContractsListViewModel(agg).TextFilter.Should().BeEmpty();
            new IndustryJobsListViewModel(agg).TextFilter.Should().BeEmpty();
            new WalletJournalListViewModel(agg).TextFilter.Should().BeEmpty();
            new WalletTransactionsListViewModel(agg).TextFilter.Should().BeEmpty();
            new MailMessagesListViewModel(agg).TextFilter.Should().BeEmpty();
            new NotificationsListViewModel(agg).TextFilter.Should().BeEmpty();
            new KillLogListViewModel(agg).TextFilter.Should().BeEmpty();
            new PlanetaryListViewModel(agg).TextFilter.Should().BeEmpty();
            new ResearchPointsListViewModel(agg).TextFilter.Should().BeEmpty();
        }

        #region Cross-Cutting Behavior Tests

        private static object[] CreateAllListViewModels(IEventAggregator agg) => new object[]
        {
            new AssetsListViewModel(agg),
            new MarketOrdersListViewModel(agg),
            new ContractsListViewModel(agg),
            new IndustryJobsListViewModel(agg),
            new WalletJournalListViewModel(agg),
            new WalletTransactionsListViewModel(agg),
            new MailMessagesListViewModel(agg),
            new NotificationsListViewModel(agg),
            new KillLogListViewModel(agg),
            new PlanetaryListViewModel(agg),
            new ResearchPointsListViewModel(agg),
        };

        [Fact]
        public void AllListViewModels_GroupedItems_NeverNull_AfterRefresh()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                vm.GetType().GetMethod("Refresh")!.Invoke(vm, null);
                var groupedItems = vm.GetType().GetProperty("GroupedItems")!.GetValue(vm);
                groupedItems.Should().NotBeNull(
                    $"{vm.GetType().Name} should have non-null GroupedItems after Refresh");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_GroupedItems_AlwaysHasAtLeastOneGroup()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                vm.GetType().GetMethod("Refresh")!.Invoke(vm, null);
                var groupedItems = vm.GetType().GetProperty("GroupedItems")!.GetValue(vm)!;
                var count = ((ICollection)groupedItems).Count;
                count.Should().BeGreaterOrEqualTo(1,
                    $"{vm.GetType().Name} should have at least one group after Refresh");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_Dispose_SafeToCallMultipleTimes()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                ((IDisposable)vm).Dispose();
                var act = () => ((IDisposable)vm).Dispose();
                act.Should().NotThrow($"{vm.GetType().Name} should be safe to Dispose multiple times");
            }
        }

        [Fact]
        public void AllListViewModels_TextFilter_RaisesPropertyChanged()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                bool raised = false;
                ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "TextFilter") raised = true;
                };

                vm.GetType().GetProperty("TextFilter")!.SetValue(vm, "test");

                raised.Should().BeTrue(
                    $"{vm.GetType().Name} should raise PropertyChanged for TextFilter");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_SortColumn_RaisesPropertyChanged()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                bool raised = false;
                ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "SortColumn") raised = true;
                };

                var prop = vm.GetType().GetProperty("SortColumn")!;
                var values = Enum.GetValues(prop.PropertyType);
                if (values.Length > 1)
                    prop.SetValue(vm, values.GetValue(1));

                raised.Should().BeTrue(
                    $"{vm.GetType().Name} should raise PropertyChanged for SortColumn");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_SortAscending_RaisesPropertyChanged()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                bool raised = false;
                ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "SortAscending") raised = true;
                };

                vm.GetType().GetProperty("SortAscending")!.SetValue(vm, false);

                raised.Should().BeTrue(
                    $"{vm.GetType().Name} should raise PropertyChanged for SortAscending");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_Grouping_RaisesPropertyChanged()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                bool raised = false;
                ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Grouping") raised = true;
                };

                var prop = vm.GetType().GetProperty("Grouping")!;
                var values = Enum.GetValues(prop.PropertyType);
                if (values.Length > 1)
                    prop.SetValue(vm, values.GetValue(1));

                raised.Should().BeTrue(
                    $"{vm.GetType().Name} should raise PropertyChanged for Grouping");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_ToggleSort_SameColumn_ReversesDirection()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                // Ensure ascending
                vm.GetType().GetProperty("SortAscending")!.SetValue(vm, true);

                // Get current sort column
                var sortColumnProp = vm.GetType().GetProperty("SortColumn")!;
                var currentColumn = sortColumnProp.GetValue(vm)!;

                // Toggle sort on same column
                vm.GetType().GetMethod("ToggleSort")!.Invoke(vm, new[] { currentColumn });

                var ascending = (bool)vm.GetType().GetProperty("SortAscending")!.GetValue(vm)!;
                ascending.Should().BeFalse(
                    $"{vm.GetType().Name} should reverse direction when toggling same column");
                ((IDisposable)vm).Dispose();
            }
        }

        [Fact]
        public void AllListViewModels_ToggleSort_DifferentColumn_SetsAscending()
        {
            var agg = CreateAggregator();
            foreach (var vm in CreateAllListViewModels(agg))
            {
                // Set descending first
                vm.GetType().GetProperty("SortAscending")!.SetValue(vm, false);

                var sortColumnProp = vm.GetType().GetProperty("SortColumn")!;
                var values = Enum.GetValues(sortColumnProp.PropertyType);
                if (values.Length > 1)
                {
                    // Toggle to a different column
                    var differentColumn = values.GetValue(1)!;
                    vm.GetType().GetMethod("ToggleSort")!.Invoke(vm, new[] { differentColumn });

                    var newColumn = sortColumnProp.GetValue(vm);
                    newColumn.Should().Be(differentColumn,
                        $"{vm.GetType().Name} should switch to the new column");

                    var ascending = (bool)vm.GetType().GetProperty("SortAscending")!.GetValue(vm)!;
                    ascending.Should().BeTrue(
                        $"{vm.GetType().Name} should set ascending when switching columns");
                }

                ((IDisposable)vm).Dispose();
            }
        }

        #endregion
    }
}
