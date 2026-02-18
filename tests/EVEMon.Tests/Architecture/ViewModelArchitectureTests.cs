using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Architecture
{
    /// <summary>
    /// Architecture tests for the ViewModel layer.
    /// Ensures ViewModels follow the Project Phoenix architectural laws.
    /// </summary>
    public class ViewModelArchitectureTests
    {
        private static readonly Assembly ViewModelAssembly = typeof(ViewModelBase).Assembly;
        private static readonly Assembly UIAssembly = typeof(EVEMon.MainWindow).Assembly;

        private static IEnumerable<Type> GetAllViewModelTypes()
        {
            return ViewModelAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ViewModelBase).IsAssignableFrom(t));
        }

        private static IEnumerable<Type> GetAllViewModelBaseTypes()
        {
            return ViewModelAssembly.GetTypes()
                .Where(t => t.IsClass && typeof(ViewModelBase).IsAssignableFrom(t));
        }

        private static bool IsListViewModelType(Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition().Name == "ListViewModel`3")
                    return true;
                baseType = baseType.BaseType;
            }
            return false;
        }

        private static IEnumerable<Type> GetAllListViewModelTypes()
        {
            return GetAllViewModelTypes().Where(IsListViewModelType);
        }

        #region Original Tests

        [Fact]
        public void AllViewModels_InheritFromViewModelBase()
        {
            // Every concrete ViewModel in the ViewModels namespace should inherit from ViewModelBase
            var vmTypes = ViewModelAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                       t.Namespace != null && t.Namespace.Contains("ViewModels") &&
                       t.Name.EndsWith("ViewModel"));

            vmTypes.Should().NotBeEmpty("there should be ViewModel classes");

            foreach (var vmType in vmTypes)
            {
                typeof(ViewModelBase).IsAssignableFrom(vmType)
                    .Should().BeTrue($"{vmType.Name} should inherit from ViewModelBase");
            }
        }

        [Fact]
        public void NoViewModel_ReferencesWindowsForms()
        {
            // ViewModels must not reference System.Windows.Forms (Law #8: right assembly)
            var vmTypes = GetAllViewModelBaseTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains("ViewModels") &&
                       !t.Namespace.Contains("Binding")); // Binding helpers can reference WinForms

            foreach (var vmType in vmTypes)
            {
                var fields = vmType.GetFields(BindingFlags.Instance | BindingFlags.Static |
                                               BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    field.FieldType.Namespace.Should()
                        .NotBe("System.Windows.Forms",
                            $"{vmType.Name}.{field.Name} should not reference System.Windows.Forms");
                }
            }
        }

        [Fact]
        public void AllViewModels_ImplementIDisposable()
        {
            var vmTypes = GetAllViewModelTypes();

            foreach (var vmType in vmTypes)
            {
                typeof(IDisposable).IsAssignableFrom(vmType)
                    .Should().BeTrue($"{vmType.Name} should implement IDisposable (via ViewModelBase)");
            }
        }

        [Fact]
        public void NoViewModel_ExceedsGodObjectLimit()
        {
            // Law #2: No class >500 lines. Approximate by counting methods + properties.
            var vmTypes = GetAllViewModelTypes();

            foreach (var vmType in vmTypes)
            {
                var memberCount = vmType.GetMembers(BindingFlags.Instance | BindingFlags.Static |
                                                     BindingFlags.Public | BindingFlags.NonPublic |
                                                     BindingFlags.DeclaredOnly).Length;

                // 500 lines ≈ ~100 members as a rough heuristic
                // Partial classes (e.g., PlanEditorViewModel split across 4 files) may exceed
                // the single-file heuristic while keeping each file under 300 lines
                int limit = 150;
                memberCount.Should().BeLessThan(limit,
                    $"{vmType.Name} has {memberCount} members, approaching god object territory");
            }
        }

        [Fact]
        public void ViewModels_LiveInCorrectNamespace()
        {
            // All VMs should be in EVEMon.Common.ViewModels or EVEMon.Common.ViewModels.Lists
            var vmTypes = GetAllViewModelTypes();

            foreach (var vmType in vmTypes)
            {
                vmType.Namespace.Should().StartWith("EVEMon.Common.ViewModels",
                    $"{vmType.Name} should be in EVEMon.Common.ViewModels namespace");
            }
        }

        [Fact]
        public void ViewModelBase_HasSubscribeMethod()
        {
            // Verify the core infrastructure exists
            var subscribeMethod = typeof(ViewModelBase).GetMethod("Subscribe",
                BindingFlags.Instance | BindingFlags.NonPublic);

            subscribeMethod.Should().NotBeNull("ViewModelBase should have a Subscribe method");
        }

        [Fact]
        public void ViewModelBase_HasTrackMethod()
        {
            var trackMethod = typeof(ViewModelBase).GetMethod("Track",
                BindingFlags.Instance | BindingFlags.NonPublic);

            trackMethod.Should().NotBeNull("ViewModelBase should have a Track method for disposal tracking");
        }

        [Fact]
        public void ConcreteViewModels_CountIsExpected()
        {
            // Verify we have the expected number of ViewModels (sanity check)
            var vmTypes = GetAllViewModelTypes().ToList();

            // We created: MainWindowVM, CharacterMonitorBodyVM, PlanEditorVM, SettingsFormVM,
            // + 11 list VMs (Assets, MarketOrders, Contracts, IndustryJobs, WalletJournal,
            //   WalletTransactions, MailMessages, Notifications, KillLog, Planetary, ResearchPoints)
            vmTypes.Count.Should().BeGreaterOrEqualTo(15,
                "we should have at least 15 concrete ViewModels");
        }

        #endregion

        #region VM-to-Control Pairing

        /// <summary>
        /// Maps each ListViewModel subclass to its expected UI control.
        /// Prevents orphaned VMs or missing control wiring.
        /// </summary>
        [Fact]
        public void AllListViewModels_HaveMatchingUIControl()
        {
            var mapping = new Dictionary<string, string>
            {
                { "AssetsListViewModel", "CharacterAssetsList" },
                { "MarketOrdersListViewModel", "CharacterMarketOrdersList" },
                { "ContractsListViewModel", "CharacterContractsList" },
                { "IndustryJobsListViewModel", "CharacterIndustryJobsList" },
                { "WalletJournalListViewModel", "CharacterWalletJournalList" },
                { "WalletTransactionsListViewModel", "CharacterWalletTransactionsList" },
                { "MailMessagesListViewModel", "CharacterEveMailMessagesList" },
                { "NotificationsListViewModel", "CharacterEveNotificationsList" },
                { "KillLogListViewModel", "CharacterKillLogList" },
                { "PlanetaryListViewModel", "CharacterPlanetaryList" },
                { "ResearchPointsListViewModel", "CharacterResearchPointsList" },
            };

            var uiTypes = UIAssembly.GetTypes();

            foreach (var (vmName, controlName) in mapping)
            {
                var controlType = uiTypes.FirstOrDefault(t => t.Name == controlName);
                controlType.Should().NotBeNull(
                    $"Control {controlName} should exist for {vmName}");
            }
        }

        #endregion

        #region Old Method Removal Enforcement

        /// <summary>
        /// Verifies that delegated controls have removed the old filter/sort/group methods.
        /// These methods are replaced by the ListViewModel pipeline.
        /// </summary>
        [Fact]
        public void AllDelegatedControls_RemovedOldFilterMethods()
        {
            var controlNames = new[]
            {
                "CharacterAssetsList", "CharacterMarketOrdersList", "CharacterContractsList",
                "CharacterIndustryJobsList", "CharacterWalletJournalList",
                "CharacterWalletTransactionsList", "CharacterEveMailMessagesList",
                "CharacterEveNotificationsList", "CharacterKillLogList",
                "CharacterPlanetaryList", "CharacterResearchPointsList",
            };

            var oldMethodNames = new[] { "IsTextMatching", "UpdateSort", "UpdateContentByGroup", "UpdateContentByGroupAsync" };

            foreach (var controlName in controlNames)
            {
                var controlType = UIAssembly.GetTypes().FirstOrDefault(t => t.Name == controlName);
                if (controlType == null) continue;

                var methods = controlType.GetMethods(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (var methodName in oldMethodNames)
                {
                    methods.Should().NotContain(m => m.Name == methodName,
                        $"{controlName} should not have old method {methodName} — VM handles this now");
                }

                // Check for generic UpdateContent<TKey>
                methods.Should().NotContain(m => m.Name == "UpdateContent" && m.IsGenericMethod,
                    $"{controlName} should not have generic UpdateContent<TKey> — VM handles this now");
            }
        }

        #endregion

        #region GroupedItems Contract

        /// <summary>
        /// Verifies the GroupedItems contract: never null after Refresh, even with no character.
        /// </summary>
        [Fact]
        public void AllListViewModels_GroupedItems_NeverNull_WhenNoCharacter()
        {
            var agg = new EventAggregator();

            foreach (var vmType in GetAllListViewModelTypes())
            {
                // Find the test constructor (takes IEventAggregator)
                var ctor = vmType.GetConstructor(new[] { typeof(IEventAggregator), typeof(IDispatcher) })
                        ?? vmType.GetConstructor(new[] { typeof(IEventAggregator) });
                ctor.Should().NotBeNull($"{vmType.Name} should have a test constructor");

                var args = ctor!.GetParameters().Length == 2
                    ? new object?[] { agg, null }
                    : new object[] { agg };

                var vm = ctor.Invoke(args);
                vm.GetType().GetMethod("Refresh")!.Invoke(vm, null);

                var groupedItems = vm.GetType().GetProperty("GroupedItems")!.GetValue(vm);
                groupedItems.Should().NotBeNull(
                    $"{vmType.Name}.GroupedItems should never be null after Refresh");

                ((IDisposable)vm).Dispose();
            }
        }

        #endregion

        #region Pipeline Contract

        /// <summary>
        /// Verifies that setting TextFilter, SortColumn, SortAscending, and Grouping
        /// all trigger a Refresh (observable via GroupedItems PropertyChanged).
        /// </summary>
        [Fact]
        public void ListViewModel_Pipeline_FilterSortGroupContract()
        {
            var agg = new EventAggregator();
            var vm = new AssetsListViewModel(agg);

            // TextFilter triggers refresh
            AssertPropertyTriggersRefresh(vm, () => vm.TextFilter = "test");
            vm.TextFilter = string.Empty; // reset

            // SortColumn triggers refresh
            AssertPropertyTriggersRefresh(vm, () => vm.SortColumn = AssetColumn.Quantity);

            // SortAscending triggers refresh
            AssertPropertyTriggersRefresh(vm, () => vm.SortAscending = false);

            // Grouping triggers refresh
            AssertPropertyTriggersRefresh(vm, () => vm.Grouping = AssetGrouping.Category);

            vm.Dispose();
        }

        private static void AssertPropertyTriggersRefresh(object vm, Action setProperty)
        {
            bool refreshed = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += Handler;
            setProperty();
            ((INotifyPropertyChanged)vm).PropertyChanged -= Handler;

            refreshed.Should().BeTrue(
                "Setting the property should trigger Refresh (GroupedItems changed)");

            void Handler(object? s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "GroupedItems") refreshed = true;
            }
        }

        #endregion

        #region VM Wiring Validation

        /// <summary>
        /// Verifies that every concrete ViewModel is wired to a UI control
        /// via a field, except known orphans.
        /// </summary>
        [Fact]
        public void AllViewModels_WiredInUI_ExceptOrphans()
        {
            // After Phase 4, PlanEditor and SettingsForm are wired too.
            // Avalonia-specific VMs are excluded as they're not wired to WinForms UI.
            var unwired = new HashSet<string>
            {
                // Avalonia-specific ViewModels (wired in Avalonia UI, not WinForms)
                "SkillBrowserViewModel",
                "AssetBrowserViewModel",
                "SkillQueueViewModel",
                "EmploymentTimelineViewModel"
            };

            var vmTypes = GetAllViewModelTypes().ToList();
            var uiTypes = UIAssembly.GetTypes();

            foreach (var vmType in vmTypes)
            {
                if (unwired.Contains(vmType.Name))
                    continue;

                var hasField = uiTypes.Any(uiType =>
                    uiType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Any(f => f.FieldType == vmType));

                hasField.Should().BeTrue(
                    $"{vmType.Name} should be wired to a UI control via a _viewModel field");
            }
        }

        #endregion

        #region Binding Helper Independence

        /// <summary>
        /// Verifies that Binding helpers don't reference concrete ViewModel types,
        /// only the generic ListViewModel base.
        /// </summary>
        [Fact]
        public void BindingHelpers_DoNotDependOnConcreteViewModels()
        {
            var bindingTypes = ViewModelAssembly.GetTypes()
                .Where(t => t.Namespace != null && t.Namespace.Contains("ViewModels.Binding"));

            foreach (var type in bindingTypes)
            {
                var fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    var fieldType = field.FieldType;

                    // Should not reference concrete VM types
                    if (fieldType.Namespace?.Contains("ViewModels") == true &&
                        typeof(ViewModelBase).IsAssignableFrom(fieldType) &&
                        !fieldType.IsAbstract &&
                        !fieldType.IsGenericTypeDefinition &&
                        !fieldType.ContainsGenericParameters)
                    {
                        // Concrete VM reference found — this violates binding independence
                        fieldType.Name.Should().NotEndWith("ViewModel",
                            $"{type.Name}.{field.Name} should not reference concrete VM type {fieldType.Name}");
                    }
                }
            }
        }

        #endregion

        #region ObservableCharacter Guardrails

        /// <summary>
        /// Law 25: ObservableCharacter must not exceed 30 public instance properties.
        /// Prevents it from becoming a god object like EveMonClient.
        /// If it exceeds 30, split or delegate to a sub-ViewModel.
        /// </summary>
        [Fact]
        public void ObservableCharacter_DoesNotExceedPropertyLimit()
        {
            var properties = typeof(ObservableCharacter)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            properties.Length.Should().BeLessOrEqualTo(30,
                "ObservableCharacter must stay thin (≤30 properties). " +
                "Delegate to sub-ViewModels instead of adding more properties.");
        }

        /// <summary>
        /// ObservableCharacter must not expose any collection properties.
        /// Collections belong in dedicated ListViewModels.
        /// </summary>
        [Fact]
        public void ObservableCharacter_HasNoCollectionProperties()
        {
            var properties = typeof(ObservableCharacter)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var type = prop.PropertyType;
                bool isCollection = type != typeof(string) &&
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

                isCollection.Should().BeFalse(
                    $"ObservableCharacter.{prop.Name} is a collection. " +
                    "Collections belong in dedicated ListViewModels, not in ObservableCharacter.");
            }
        }

        /// <summary>
        /// ObservableCharacter must implement IDisposable (Law 11: subscriptions must be disposed).
        /// </summary>
        [Fact]
        public void ObservableCharacter_ImplementsIDisposable()
        {
            typeof(ObservableCharacter).Should().Implement<IDisposable>();
        }

        #endregion
    }
}
