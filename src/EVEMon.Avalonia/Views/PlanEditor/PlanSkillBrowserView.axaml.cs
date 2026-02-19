using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using EVEMon.Common.Data;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using EVEMon.Avalonia.ViewModels;

namespace EVEMon.Avalonia.Views.PlanEditor
{
    public partial class PlanSkillBrowserView : UserControl
    {
        private PlanSkillBrowserViewModel? _viewModel;
        private PlanEditorViewModel? _planEditor;
        private Border? _selectedBorder;

        public PlanSkillBrowserView()
        {
            InitializeComponent();
        }

        public void SetViewModel(PlanEditorViewModel planEditor)
        {
            _planEditor = planEditor;
            _viewModel = new PlanSkillBrowserViewModel(planEditor);
            _viewModel.Character = planEditor.Character;
            _viewModel.Refresh();
            RefreshGroupsList();
        }

        private void RefreshGroupsList()
        {
            if (_viewModel == null)
            {
                GroupsList.ItemsSource = null;
                return;
            }

            // Wrap entries in display items for IBrush support
            var displayGroups = _viewModel.Groups.Select(g => new SkillBrowserGroupDisplay(g)).ToList();
            GroupsList.ItemsSource = displayGroups;
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            string filter = FilterBox.Text?.Trim() ?? "";
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(filter);
            _viewModel.TextFilter = filter;
            RefreshGroupsList();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            FilterBox.Text = "";
            ClearFilterBtn.IsVisible = false;
            if (_viewModel != null)
            {
                _viewModel.TextFilter = "";
                RefreshGroupsList();
            }
        }

        private void OnShowAllToggled(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ShowAll = ShowAllToggle.IsChecked == true;
            RefreshGroupsList();
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is SkillBrowserGroupDisplay group)
                group.IsExpanded = !group.IsExpanded;
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            _viewModel?.CollapseAll();
            RefreshGroupsList();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            _viewModel?.ExpandAll();
            RefreshGroupsList();
        }

        private void OnSkillItemClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not PlanSkillBrowserDisplayItem displayItem) return;

            // Update selection highlight
            _selectedBorder?.Classes.Remove("selected");
            border.Classes.Add("selected");
            _selectedBorder = border;

            _viewModel?.SelectSkill(displayItem.Entry.StaticSkill);
            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            var detail = _viewModel?.SelectedSkillDetail;
            if (detail == null)
            {
                DetailPanel.IsVisible = false;
                return;
            }

            DetailPanel.IsVisible = true;
            DetailName.Text = detail.Name;
            DetailRank.Text = detail.Rank.ToString();
            DetailPrimary.Text = detail.PrimaryAttribute.ToString();
            DetailSecondary.Text = detail.SecondaryAttribute.ToString();
            DetailDescription.Text = detail.Description;

            // Build level rows
            LevelsList.Children.Clear();
            foreach (var level in detail.LevelDetails)
            {
                var row = new Border
                {
                    Padding = new global::Avalonia.Thickness(0, 2),
                    Child = CreateLevelRow(level)
                };
                LevelsList.Children.Add(row);
            }

            // Build prerequisite rows
            PrereqsList.Children.Clear();
            if (detail.Prerequisites.Count == 0)
            {
                PrereqsList.Children.Add(new TextBlock
                {
                    Text = "None",
                    FontSize = 11,
                    Foreground = (IBrush)this.FindResource("EveTextDisabledBrush")!
                });
            }
            else
            {
                foreach (var prereq in detail.Prerequisites)
                {
                    PrereqsList.Children.Add(CreatePrereqRow(prereq));
                }
            }
        }

        private Grid CreateLevelRow(SkillLevelDetail level)
        {
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("60,*,Auto")
            };

            // Level label
            var levelText = new TextBlock
            {
                Text = $"Level {level.LevelText}:",
                FontSize = 11,
                Foreground = level.IsTrained
                    ? (IBrush)this.FindResource("EveAccentPrimaryBrush")!
                    : (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(levelText, 0);
            grid.Children.Add(levelText);

            // Training time
            var timeText = new TextBlock
            {
                Text = level.IsTrained ? "Trained" : level.TrainingTimeText,
                FontSize = 11,
                Foreground = level.IsTrained
                    ? (IBrush)this.FindResource("EveTextDisabledBrush")!
                    : (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(timeText, 1);
            grid.Children.Add(timeText);

            // Plan button (only for untrained, unplanned levels)
            if (level.CanPlan && !level.IsPlanned)
            {
                var planBtn = new Button
                {
                    Content = $"Plan to {level.LevelText}",
                    FontSize = 10,
                    Padding = new global::Avalonia.Thickness(8, 2),
                    CornerRadius = new global::Avalonia.CornerRadius(10),
                    Tag = level.Level
                };
                planBtn.Click += OnPlanToLevelClicked;
                Grid.SetColumn(planBtn, 2);
                grid.Children.Add(planBtn);
            }
            else if (level.IsPlanned && !level.IsTrained)
            {
                var plannedLabel = new TextBlock
                {
                    Text = "Planned",
                    FontSize = 10,
                    Foreground = (IBrush)this.FindResource("EveWarningYellowBrush")!,
                    VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
                };
                Grid.SetColumn(plannedLabel, 2);
                grid.Children.Add(plannedLabel);
            }

            return grid;
        }

        private Grid CreatePrereqRow(SkillPrerequisiteInfo prereq)
        {
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*")
            };

            var indicator = new TextBlock
            {
                Text = prereq.IsMet ? "\u2713 " : "\u2717 ",
                FontSize = 11,
                Foreground = prereq.IsMet
                    ? (IBrush)this.FindResource("EveSuccessGreenBrush")!
                    : (IBrush)this.FindResource("EveErrorRedBrush")!,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(indicator, 0);
            grid.Children.Add(indicator);

            var nameText = new TextBlock
            {
                Text = prereq.DisplayText,
                FontSize = 11,
                Foreground = prereq.IsMet
                    ? (IBrush)this.FindResource("EveTextSecondaryBrush")!
                    : (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            return grid;
        }

        private void OnPlanToLevelClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not long level) return;
            if (_viewModel?.SelectedSkill == null) return;

            _viewModel.PlanToLevel(_viewModel.SelectedSkill, level);

            // Refresh the detail panel to show updated planned status
            _viewModel.Refresh();
            RefreshGroupsList();
            UpdateDetailPanel();

            // Update status bar in parent window
            if (this.VisualRoot is PlanEditorWindow window)
            {
                window.UpdateStatusBar();
            }
        }
    }

    /// <summary>
    /// Display wrapper for skill groups in the browser.
    /// Implements INPC for IsExpanded/Chevron so lightweight collapsible sections work.
    /// </summary>
    internal sealed class SkillBrowserGroupDisplay : INotifyPropertyChanged
    {
        private readonly PlanSkillGroupEntry _entry;

        public string Name => _entry.Name;
        public string TrainedCountText => _entry.TrainedCountText;
        public bool IsExpanded
        {
            get => _entry.IsExpanded;
            set
            {
                if (_entry.IsExpanded != value)
                {
                    _entry.IsExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
                }
            }
        }
        public string Chevron => _entry.IsExpanded ? "\u25BE" : "\u25B8";
        public List<PlanSkillBrowserDisplayItem> VisibleSkills { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public SkillBrowserGroupDisplay(PlanSkillGroupEntry entry)
        {
            _entry = entry;
            VisibleSkills = entry.VisibleSkills
                .Select(s => new PlanSkillBrowserDisplayItem(s))
                .ToList();
        }
    }
}
