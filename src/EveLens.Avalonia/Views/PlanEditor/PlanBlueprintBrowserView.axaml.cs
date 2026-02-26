// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using EveLens.Common.Data;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.PlanEditor
{
    public partial class PlanBlueprintBrowserView : UserControl
    {
        private BlueprintBrowserViewModel? _viewModel;
        private Border? _selectedBorder;

        public PlanBlueprintBrowserView()
        {
            InitializeComponent();
        }

        public void SetViewModel(PlanEditorViewModel planEditor)
        {
            _viewModel = new BlueprintBrowserViewModel(planEditor);
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

            var displayGroups = _viewModel.Groups.Select(g => new BlueprintGroupDisplay(g)).ToList();
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

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is BlueprintGroupDisplay group)
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

        private void OnBlueprintClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not BlueprintDisplayItem displayItem) return;

            // Update selection highlight
            _selectedBorder?.Classes.Remove("selected");
            border.Classes.Add("selected");
            _selectedBorder = border;

            _viewModel?.SelectBlueprint(displayItem.Entry.Blueprint);
            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            var detail = _viewModel?.SelectedBlueprintDetail;
            if (detail == null)
            {
                DetailPanel.IsVisible = false;
                return;
            }

            DetailPanel.IsVisible = true;
            DetailName.Text = detail.Name;
            DetailProduces.Text = $"Produces: {detail.ProducesItemName}";
            DetailTime.Text = $"Production Time: {detail.ProductionTimeText}";
            DetailDescription.Text = detail.Description;
            PlanSkillsBtn.IsVisible = !detail.CanBuild;

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
                    var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*") };
                    grid.Children.Add(new TextBlock
                    {
                        Text = prereq.IsMet ? "\u2713 " : "\u2717 ",
                        FontSize = 11,
                        Foreground = prereq.IsMet
                            ? (IBrush)this.FindResource("EveSuccessGreenBrush")!
                            : (IBrush)this.FindResource("EveErrorRedBrush")!,
                        [Grid.ColumnProperty] = 0
                    });
                    grid.Children.Add(new TextBlock
                    {
                        Text = prereq.DisplayText,
                        FontSize = 11,
                        Foreground = prereq.IsMet
                            ? (IBrush)this.FindResource("EveTextSecondaryBrush")!
                            : (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                        [Grid.ColumnProperty] = 1
                    });
                    PrereqsList.Children.Add(grid);
                }
            }

            MaterialsList.Children.Clear();
            foreach (var material in detail.Materials)
            {
                var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };
                grid.Children.Add(new TextBlock
                {
                    Text = material.Name,
                    FontSize = 11,
                    Foreground = (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                    [Grid.ColumnProperty] = 0
                });
                grid.Children.Add(new TextBlock
                {
                    Text = material.QuantityText,
                    FontSize = 11,
                    Foreground = (IBrush)this.FindResource("EveTextSecondaryBrush")!,
                    [Grid.ColumnProperty] = 1
                });
                MaterialsList.Children.Add(grid);
            }
        }

        private void OnPlanSkills(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedBlueprint == null) return;
            _viewModel.PlanToBuild(_viewModel.SelectedBlueprint);
            _viewModel.Refresh();
            RefreshGroupsList();
            UpdateDetailPanel();

            if (this.VisualRoot is PlanEditorWindow window)
                window.UpdateStatusBar();
        }
    }

    internal sealed class BlueprintGroupDisplay : INotifyPropertyChanged
    {
        private readonly BlueprintBrowserViewModel.BlueprintGroupEntry _entry;

        public string Name => _entry.Name;
        public string CountText => _entry.CountText;
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
        public List<BlueprintDisplayItem> VisibleBlueprints { get; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public BlueprintGroupDisplay(BlueprintBrowserViewModel.BlueprintGroupEntry entry)
        {
            _entry = entry;
            VisibleBlueprints = entry.VisibleBlueprints
                .Select(b => new BlueprintDisplayItem(b))
                .ToList();
        }
    }

    internal sealed class BlueprintDisplayItem
    {
        public BlueprintBrowserViewModel.BlueprintEntry Entry { get; }
        public string Name => Entry.Name;
        public string ProducesText => Entry.ProducesItemName;

        public BlueprintDisplayItem(BlueprintBrowserViewModel.BlueprintEntry entry) => Entry = entry;
    }
}
