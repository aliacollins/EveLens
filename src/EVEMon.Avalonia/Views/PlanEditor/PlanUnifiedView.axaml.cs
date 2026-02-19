using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels;
using EVEMon.Avalonia.ViewModels;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanUnifiedView : UserControl
    {
        private PlanEditorViewModel? _viewModel;
        private string _currentFilter = "";
        private List<PlanEntryDisplayItem>? _currentItems;
        private HashSet<string> _previousEntryKeys = new();
        private HashSet<string> _recentlyMovedKeys = new();
        private MenuItem? _moveUpCtxItem;
        private MenuItem? _moveDownCtxItem;

        public PlanUnifiedView()
        {
            InitializeComponent();
            PlanGrid.Sorting += OnDataGridSorting;

            // Right-click should select the row so context menu acts on the clicked item
            PlanGrid.AddHandler(InputElement.PointerPressedEvent, OnGridPointerPressed,
                RoutingStrategies.Tunnel);

            // Cache context menu items and update state on selection change
            if (PlanGrid.ContextMenu != null)
            {
                _moveUpCtxItem = PlanGrid.ContextMenu.Items.OfType<MenuItem>()
                    .FirstOrDefault(mi => mi.Header?.ToString() == "Move Up");
                _moveDownCtxItem = PlanGrid.ContextMenu.Items.OfType<MenuItem>()
                    .FirstOrDefault(mi => mi.Header?.ToString() == "Move Down");
            }
            PlanGrid.SelectionChanged += (_, _) => UpdateContextMenuState();
        }

        private void UpdateContextMenuState()
        {
            var selected = GetSelectedItem();
            if (_moveUpCtxItem != null)
                _moveUpCtxItem.IsEnabled = selected?.CanMoveUp == true;
            if (_moveDownCtxItem != null)
                _moveDownCtxItem.IsEnabled = selected?.CanMoveDown == true;
        }

        private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(PlanGrid).Properties.IsRightButtonPressed) return;

            // Walk up visual tree from event source to find the DataGridRow
            var current = e.Source as global::Avalonia.Visual;
            while (current != null)
            {
                if (current is DataGridRow row)
                {
                    PlanGrid.SelectedItem = row.DataContext;
                    break;
                }
                current = current.GetVisualParent() as global::Avalonia.Visual;
            }
        }

        public void SetViewModel(PlanEditorViewModel viewModel)
        {
            _viewModel = viewModel;
            UpdateCards();
            UpdateStatsHeader();
            Refresh();
        }

        public void RefreshSkillList(string filter)
        {
            _currentFilter = filter;
            Refresh();
        }

        private void Refresh()
        {
            if (_viewModel?.DisplayPlan == null)
            {
                PlanGrid.ItemsSource = null;
                return;
            }

            Character? character = _viewModel.Character;
            PlanEntry[] entries = _viewModel.DisplayPlan.ToArray();

            var displayItems = entries.Select(entry =>
            {
                PlanEntryStatus status = DetermineStatus(entry, character);
                return new PlanEntryDisplayItem(entry, status);
            });

            // Apply text filter
            if (!string.IsNullOrEmpty(_currentFilter))
            {
                string lowerFilter = _currentFilter.ToLowerInvariant();
                displayItems = displayItems.Where(d =>
                    d.SkillName.ToLowerInvariant().Contains(lowerFilter) ||
                    d.GroupName.ToLowerInvariant().Contains(lowerFilter));
            }

            // Law 20: Always call .ToList() before binding to DataGrid
            _currentItems = displayItems.ToList();

            // Set position flags and prerequisite-aware move availability
            for (int i = 0; i < _currentItems.Count; i++)
            {
                var item = _currentItems[i];
                item.IsFirstItem = (i == 0);
                item.IsLastItem = (i == _currentItems.Count - 1);

                // Can move up? Not first, and entry above is not a prerequisite
                bool canUp = i > 0;
                if (canUp)
                {
                    var above = _currentItems[i - 1];
                    if (item.Entry.IsDependentOf(above.Entry))
                        canUp = false;
                }
                item.CanMoveUp = canUp;

                // Can move down? Not last, and entry below doesn't depend on this one
                bool canDown = i < _currentItems.Count - 1;
                if (canDown)
                {
                    var below = _currentItems[i + 1];
                    if (below.Entry.IsDependentOf(item.Entry))
                        canDown = false;
                }
                item.CanMoveDown = canDown;
            }

            // Apply move highlight if items were recently moved
            if (_recentlyMovedKeys.Count > 0)
            {
                foreach (var item in _currentItems)
                {
                    string key = $"{item.Entry.Skill.ID}_{item.Entry.Level}";
                    if (_recentlyMovedKeys.Contains(key))
                        item.IsRecentlyMoved = true;
                }
                _recentlyMovedKeys = new();

                var movedRef = _currentItems;
                global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
                {
                    if (movedRef != null)
                    {
                        foreach (var item in movedRef)
                            item.IsRecentlyMoved = false;
                        PlanGrid.ItemsSource = null;
                        PlanGrid.ItemsSource = movedRef;
                    }
                }, TimeSpan.FromSeconds(1.5));
            }

            // 1c: Highlight newly-added skills
            var newKeys = new HashSet<string>(_currentItems.Select(d => $"{d.Entry.Skill.ID}_{d.Entry.Level}"));
            if (_previousEntryKeys.Count > 0)
            {
                foreach (var item in _currentItems)
                {
                    string key = $"{item.Entry.Skill.ID}_{item.Entry.Level}";
                    if (!_previousEntryKeys.Contains(key))
                        item.IsNewlyAdded = true;
                }

                // Clear highlight after 2 seconds
                if (_currentItems.Any(i => i.IsNewlyAdded))
                {
                    var itemsRef = _currentItems;
                    global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
                    {
                        if (itemsRef != null)
                        {
                            foreach (var item in itemsRef)
                                item.IsNewlyAdded = false;
                            PlanGrid.ItemsSource = null;
                            PlanGrid.ItemsSource = itemsRef;
                        }
                    }, TimeSpan.FromSeconds(2));
                }
            }
            _previousEntryKeys = newKeys;

            PlanGrid.ItemsSource = _currentItems;

            UpdateStatsHeader();
        }

        private void UpdateCards()
        {
            if (_viewModel == null) return;

            var stats = _viewModel.PlanStats;
            var plan = _viewModel.Plan;

            GoalCard.Update(
                plan?.Name ?? "No Plan",
                _viewModel.EntryCount,
                stats.NotKnownSkillsCount,
                stats.UniqueSkillsCount);

            TimeCard.Update(stats.TrainingTime);
            CostCard.Update(stats.BooksCost, stats.NotKnownBooksCost);
        }

        private void UpdateStatsHeader()
        {
            if (_viewModel == null) return;

            var stats = _viewModel.PlanStats;
            SkillCountText.Text = $"{_viewModel.EntryCount} skills \u00B7 {stats.UniqueSkillsCount} unique";
            TrainingTimeText.Text = FormatTime(stats.TrainingTime);
            TotalSpText.Text = FormatSP(stats.TotalSkillPoints);

            // Completion date
            if (stats.TrainingTime > TimeSpan.Zero)
            {
                var finishDate = DateTime.UtcNow + stats.TrainingTime;
                FinishDateText.Text = $"Finishes {finishDate:yyyy-MM-dd}";
            }
            else
            {
                FinishDateText.Text = string.Empty;
            }

            // Injector estimate
            Character? character = _viewModel.Character;
            long characterSP = (character as Character)?.SkillPoints ?? 0;
            long missingSP = stats.TotalSkillPoints;
            if (missingSP > 0 && characterSP > 0)
            {
                int spPerInjector = GetSpPerInjector(characterSP);
                int injectorCount = (int)Math.Ceiling((double)missingSP / spPerInjector);
                double costBillions = injectorCount * 0.9; // 900M ISK per injector
                InjectorText.Text = $"~{injectorCount} injectors (~{costBillions:F1}B ISK)";
            }
            else
            {
                InjectorText.Text = string.Empty;
            }
        }

        /// <summary>
        /// Returns SP per large skill injector based on character total SP brackets.
        /// </summary>
        private static int GetSpPerInjector(long characterSP) => characterSP switch
        {
            < 5_000_000 => 500_000,
            < 50_000_000 => 400_000,
            < 80_000_000 => 300_000,
            _ => 150_000
        };

        private static string FormatSP(long sp)
        {
            if (sp >= 1_000_000) return $"{sp / 1_000_000.0:F1}M SP";
            if (sp >= 1_000) return $"{sp / 1_000.0:F0}K SP";
            return $"{sp:N0} SP";
        }

        private static PlanEntryStatus DetermineStatus(PlanEntry entry, Character? character)
        {
            if (character == null)
                return PlanEntryStatus.Missing;

            Skill? charSkill = entry.CharacterSkill;
            if (charSkill == null)
                return PlanEntryStatus.Missing;

            if (charSkill.IsTraining && charSkill.Level + 1 == entry.Level)
                return PlanEntryStatus.Training;

            if (charSkill.Level >= entry.Level)
                return PlanEntryStatus.Trained;

            return PlanEntryStatus.Missing;
        }

        private PlanEntryDisplayItem? GetSelectedItem()
        {
            return PlanGrid.SelectedItem as PlanEntryDisplayItem;
        }

        #region Cards Toggle

        private void OnCardsToggle(object? sender, RoutedEventArgs e)
        {
            CardsPanel.IsVisible = CardsToggle.IsChecked == true;
        }

        #endregion

        #region Add Skills

        private void OnAddSkills(object? sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is PlanEditorWindow window)
            {
                window.SwitchToSkillBrowser();
            }
        }

        #endregion

        #region Context Menu Actions

        private void OnPlanToLevel(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.Tag is not string tagStr || !int.TryParse(tagStr, out int level)) return;

            var selected = GetSelectedItem();
            if (selected == null || _viewModel?.Plan == null) return;

            _viewModel.Plan.PlanTo(selected.Entry.Skill, level);
            _viewModel.UpdateDisplayPlan();
            UpdateCards();
            Refresh();
            UpdateParentStatusBar();
        }

        private void OnMoveToTop(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedItem();
            if (selected == null || _viewModel?.Plan == null) return;

            var plan = _viewModel.Plan;
            var entry = plan.FirstOrDefault(pe =>
                pe.Skill == selected.Entry.Skill && pe.Level == selected.Entry.Level);
            if (entry != null)
            {
                entry.Priority = 1;
                _viewModel.UpdateDisplayPlan();
                UpdateCards();
                Refresh();
                UpdateParentStatusBar();
            }
        }

        private void OnChangePriority(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedItem();
            if (selected == null || _viewModel?.Plan == null) return;

            int newPriority = (selected.Entry.Priority % 5) + 1;

            var plan = _viewModel.Plan;
            var entry = plan.FirstOrDefault(pe =>
                pe.Skill == selected.Entry.Skill && pe.Level == selected.Entry.Level);
            if (entry != null)
            {
                entry.Priority = newPriority;
                _viewModel.UpdateDisplayPlan();
                Refresh();
            }
        }

        private void OnChangeNote(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelectedItem();
            if (selected == null || _viewModel?.Plan == null) return;

            var plan = _viewModel.Plan;
            var entry = plan.FirstOrDefault(pe =>
                pe.Skill == selected.Entry.Skill && pe.Level == selected.Entry.Level);
            if (entry != null)
            {
                entry.Notes = string.IsNullOrEmpty(entry.Notes)
                    ? "User note"
                    : string.Empty;
            }
        }

        private void OnRemoveFromPlan(object? sender, RoutedEventArgs e)
        {
            DeleteSelected();
        }

        #endregion

        #region Copy to Clipboard

        private void OnCopyToClipboard(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var selectedItems = PlanGrid.SelectedItems.OfType<PlanEntryDisplayItem>().ToList();
            IEnumerable<PlanEntry> entries;
            if (selectedItems.Count > 0)
                entries = selectedItems.Select(d => d.Entry);
            else if (_currentItems != null && _currentItems.Count > 0)
                entries = _currentItems.Select(d => d.Entry);
            else
                return;

            var settings = new PlanExportSettings
            {
                EntryNumber = true,
                EntryTrainingTimes = true,
                FooterCount = true,
                FooterTotalTime = true
            };

            string text = _viewModel.ExportSelectedAsText(entries, settings);
            if (!string.IsNullOrEmpty(text))
                AppServices.ClipboardService?.SetText(text);
        }

        #endregion

        #region Column Sorting

        private void OnDataGridSorting(object? sender, DataGridColumnEventArgs e)
        {
            var criteria = e.Column.Header?.ToString() switch
            {
                "SKILL NAME" => PlanEntrySort.Name,
                "TRAINING TIME" => PlanEntrySort.TrainingTime,
                "RANK" => PlanEntrySort.Rank,
                "PRIMARY" => PlanEntrySort.PrimaryAttribute,
                "SECONDARY" => PlanEntrySort.SecondaryAttribute,
                "GROUP" => PlanEntrySort.PlanGroup,
                "SP/HOUR" => PlanEntrySort.SPPerHour,
                _ => PlanEntrySort.None
            };

            if (criteria == PlanEntrySort.None || _viewModel == null) return;

            _viewModel.ToggleSortColumn(criteria);
            Refresh();
        }

        #endregion

        #region Move & Delete

        private List<int> GetSelectedIndices()
        {
            if (_viewModel?.DisplayPlan == null || _currentItems == null)
                return new List<int>();

            var displayEntries = _viewModel.DisplayPlan.ToArray();
            var selected = PlanGrid.SelectedItems.OfType<PlanEntryDisplayItem>().ToList();
            var indices = new List<int>();

            foreach (var item in selected)
            {
                for (int i = 0; i < displayEntries.Length; i++)
                {
                    if (displayEntries[i] == item.Entry)
                    {
                        indices.Add(i);
                        break;
                    }
                }
            }

            indices.Sort();
            return indices;
        }

        private void OnMoveUp(object? sender, RoutedEventArgs e) => MoveSelectionUp();

        private void OnMoveDown(object? sender, RoutedEventArgs e) => MoveSelectionDown();

        internal void MoveSelectionUp()
        {
            if (_viewModel == null) return;
            var indices = GetSelectedIndices();
            if (indices.Count == 0 || !_viewModel.CanMoveUp(indices)) return;

            // Track moved items for highlight
            var selectedItems = PlanGrid.SelectedItems.OfType<PlanEntryDisplayItem>().ToList();
            _recentlyMovedKeys = new HashSet<string>(
                selectedItems.Select(d => $"{d.Entry.Skill.ID}_{d.Entry.Level}"));

            _viewModel.MoveSelectedUp(indices);
            _viewModel.UpdateDisplayPlan();
            UpdateCards();
            Refresh();
            UpdateParentStatusBar();
        }

        internal void MoveSelectionDown()
        {
            if (_viewModel == null) return;
            var indices = GetSelectedIndices();
            if (indices.Count == 0 || !_viewModel.CanMoveDown(indices)) return;

            // Track moved items for highlight
            var selectedItems = PlanGrid.SelectedItems.OfType<PlanEntryDisplayItem>().ToList();
            _recentlyMovedKeys = new HashSet<string>(
                selectedItems.Select(d => $"{d.Entry.Skill.ID}_{d.Entry.Level}"));

            _viewModel.MoveSelectedDown(indices);
            _viewModel.UpdateDisplayPlan();
            UpdateCards();
            Refresh();
            UpdateParentStatusBar();
        }

        private void OnMoveItemUp(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not PlanEntryDisplayItem item) return;
            if (_viewModel?.DisplayPlan == null) return;

            var displayEntries = _viewModel.DisplayPlan.ToArray();
            int index = -1;
            for (int i = 0; i < displayEntries.Length; i++)
            {
                if (displayEntries[i] == item.Entry) { index = i; break; }
            }
            if (index <= 0) return;

            _recentlyMovedKeys = new HashSet<string> { $"{item.Entry.Skill.ID}_{item.Entry.Level}" };
            _viewModel.MoveSelectedUp(new List<int> { index });
            _viewModel.UpdateDisplayPlan();
            UpdateCards();
            Refresh();
            UpdateParentStatusBar();
            e.Handled = true;
        }

        private void OnMoveItemDown(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not PlanEntryDisplayItem item) return;
            if (_viewModel?.DisplayPlan == null) return;

            var displayEntries = _viewModel.DisplayPlan.ToArray();
            int index = -1;
            for (int i = 0; i < displayEntries.Length; i++)
            {
                if (displayEntries[i] == item.Entry) { index = i; break; }
            }
            if (index < 0 || index >= displayEntries.Length - 1) return;

            _recentlyMovedKeys = new HashSet<string> { $"{item.Entry.Skill.ID}_{item.Entry.Level}" };
            _viewModel.MoveSelectedDown(new List<int> { index });
            _viewModel.UpdateDisplayPlan();
            UpdateCards();
            Refresh();
            UpdateParentStatusBar();
            e.Handled = true;
        }

        internal void DeleteSelected()
        {
            if (_viewModel?.Plan == null) return;
            var selectedItems = PlanGrid.SelectedItems.OfType<PlanEntryDisplayItem>().ToList();
            if (selectedItems.Count == 0) return;

            var plan = _viewModel.Plan;
            foreach (var item in selectedItems)
            {
                var skill = item.Entry.CharacterSkill;
                if (skill != null)
                {
                    // Remove the specific level, not the entire skill
                    var planEntry = plan.GetEntry(item.Entry.Skill, item.Entry.Level);
                    if (planEntry != null)
                    {
                        var op = plan.TryRemoveSet(new[] { planEntry });
                        op.Perform();
                    }
                }
            }

            _viewModel.UpdateDisplayPlan();
            UpdateCards();
            Refresh();
            UpdateParentStatusBar();
        }

        #endregion

        private void UpdateParentStatusBar()
        {
            if (this.VisualRoot is PlanEditorWindow window)
                window.UpdateStatusBar();
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }
    }
}
