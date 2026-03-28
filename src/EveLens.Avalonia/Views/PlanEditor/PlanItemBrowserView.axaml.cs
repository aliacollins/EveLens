// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using EveLens.Avalonia.ViewModels;
using EveLens.Common.Data;
using EveLens.Common.ViewModels;

using EveLens.Common.ViewModels;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.PlanEditor
{
    public partial class PlanItemBrowserView : UserControl
    {
        private ItemBrowserViewModel? _viewModel;
        private Border? _selectedBorder;

        public PlanItemBrowserView()
        {
            InitializeComponent();
        }

        public void SetViewModel(PlanEditorViewModel planEditor)
        {
            _viewModel = new ItemBrowserViewModel(planEditor);
            _viewModel.Character = planEditor.Character;
            _viewModel.Refresh();
            RefreshTreeList();
        }

        private void RefreshTreeList()
        {
            if (_viewModel == null)
            {
                TreeList.ItemsSource = null;
                return;
            }

            var displayNodes = _viewModel.FlattenedNodes
                .Select(n => new BrowserTreeNodeDisplay(n))
                .ToList();

            TreeList.ItemsSource = displayNodes;
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            string filter = FilterBox.Text?.Trim() ?? "";
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(filter);
            _viewModel.TextFilter = filter;
            RefreshTreeList();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            FilterBox.Text = "";
            ClearFilterBtn.IsVisible = false;
            if (_viewModel != null)
            {
                _viewModel.TextFilter = "";
                RefreshTreeList();
            }
        }

        private void OnCanUseToggled(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ShowCanUseOnly = CanUseToggle.IsChecked == true;
            RefreshTreeList();
        }

        private void OnNodeToggle(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is BrowserTreeNodeDisplay display)
            {
                _viewModel?.ToggleNode(display.Node);
                RefreshTreeList();
            }
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            _viewModel?.CollapseAll();
            RefreshTreeList();
        }

        private void OnItemClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not BrowserTreeNodeDisplay displayItem) return;
            if (!displayItem.IsLeaf || displayItem.Node.Item == null) return;

            // Update selection highlight
            _selectedBorder?.Classes.Remove("selected");
            border.Classes.Add("selected");
            _selectedBorder = border;

            _viewModel?.SelectItem(displayItem.Node.Item);
            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            var detail = _viewModel?.SelectedItemDetail;
            if (detail == null)
            {
                DetailPanel.IsVisible = false;
                return;
            }

            DetailPanel.IsVisible = true;
            DetailName.Text = detail.Name;
            DetailSlot.Text = $"Slot: {detail.SlotType}";
            DetailDescription.Text = detail.Description;
            PlanToUseBtn.IsVisible = !detail.CanUse;

            PrereqsList.Children.Clear();
            if (detail.Prerequisites.Count == 0)
            {
                PrereqsList.Children.Add(new TextBlock
                {
                    Text = "None",
                    FontSize = FontScaleService.Body,
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

            // Show item properties
            UpdateProperties(_viewModel?.SelectedItem);
        }

        private void UpdateProperties(Item? item)
        {
            PropertiesList.Children.Clear();

            if (item == null)
            {
                PropertiesDivider.IsVisible = false;
                PropertiesHeader.IsVisible = false;
                return;
            }

            var propsVm = new ItemPropertiesViewModel(item);
            if (propsVm.Sections.Count == 0)
            {
                PropertiesDivider.IsVisible = false;
                PropertiesHeader.IsVisible = false;
                return;
            }

            PropertiesDivider.IsVisible = true;
            PropertiesHeader.IsVisible = true;

            foreach (var section in propsVm.Sections)
            {
                PropertiesList.Children.Add(new TextBlock
                {
                    Text = section.CategoryName,
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = (IBrush)this.FindResource("EveAccentPrimaryBrush")!,
                    Margin = new global::Avalonia.Thickness(0, 4, 0, 2)
                });

                foreach (var prop in section.Properties)
                {
                    var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };
                    grid.Children.Add(new TextBlock
                    {
                        Text = prop.Name,
                        FontSize = FontScaleService.Small,
                        Foreground = (IBrush)this.FindResource("EveTextSecondaryBrush")!,
                        [Grid.ColumnProperty] = 0
                    });
                    grid.Children.Add(new TextBlock
                    {
                        Text = prop.FormattedValue,
                        FontSize = FontScaleService.Small,
                        Foreground = (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                        [Grid.ColumnProperty] = 1
                    });
                    PropertiesList.Children.Add(grid);
                }
            }
        }

        private Grid CreatePrereqRow(ItemBrowserViewModel.PrerequisiteInfo prereq)
        {
            var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*") };

            grid.Children.Add(new TextBlock
            {
                Text = prereq.IsMet ? "\u2713 " : "\u2717 ",
                FontSize = FontScaleService.Body,
                Foreground = prereq.IsMet
                    ? (IBrush)this.FindResource("EveSuccessGreenBrush")!
                    : (IBrush)this.FindResource("EveErrorRedBrush")!,
                [Grid.ColumnProperty] = 0
            });

            grid.Children.Add(new TextBlock
            {
                Text = prereq.DisplayText,
                FontSize = FontScaleService.Body,
                Foreground = prereq.IsMet
                    ? (IBrush)this.FindResource("EveTextSecondaryBrush")!
                    : (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                [Grid.ColumnProperty] = 1
            });

            return grid;
        }

        private void OnPlanToUse(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedItem == null) return;
            _viewModel.PlanToUse(_viewModel.SelectedItem);
            _viewModel.Refresh();
            RefreshTreeList();
            UpdateDetailPanel();

            if (this.VisualRoot is PlanEditorWindow window)
                window.UpdateStatusBar();
        }
    }
}
