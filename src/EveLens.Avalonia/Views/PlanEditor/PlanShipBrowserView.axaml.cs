// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EveLens.Avalonia.Converters;
using EveLens.Avalonia.ViewModels;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.PlanEditor
{
    public partial class PlanShipBrowserView : UserControl
    {
        private ShipBrowserViewModel? _viewModel;
        private PlanEditorViewModel? _planEditor;
        private Border? _selectedBorder;
        private long _lastRequestedShipId;

        public PlanShipBrowserView()
        {
            InitializeComponent();
        }

        public void SetViewModel(PlanEditorViewModel planEditor)
        {
            _planEditor = planEditor;
            _viewModel = new ShipBrowserViewModel(planEditor);
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

            // Use flattened hierarchical tree
            var flatNodes = _viewModel.FlattenedNodes;

            // Apply race filter to leaf nodes
            var displayNodes = flatNodes
                .Where(n => !n.IsLeaf || MatchesRaceFilter(n.Item))
                .Select(n => new BrowserTreeNodeDisplay(n))
                .ToList();

            TreeList.ItemsSource = displayNodes;
        }

        private bool MatchesRaceFilter(Item? item)
        {
            if (item == null) return true;

            bool amarr = AmarrToggle.IsChecked == true;
            bool caldari = CaldariToggle.IsChecked == true;
            bool gallente = GallenteToggle.IsChecked == true;
            bool minmatar = MinmatarToggle.IsChecked == true;

            // If all are checked, skip filtering
            if (amarr && caldari && gallente && minmatar)
                return true;

            var race = item.Race;
            if (race.HasFlag(Race.Amarr) && amarr) return true;
            if (race.HasFlag(Race.Caldari) && caldari) return true;
            if (race.HasFlag(Race.Gallente) && gallente) return true;
            if (race.HasFlag(Race.Minmatar) && minmatar) return true;
            // Always show ships with no race, faction, ORE, etc.
            if (race == Race.None || race.HasFlag(Race.Faction) || race.HasFlag(Race.Ore)
                || race.HasFlag(Race.Jove) || race.HasFlag(Race.Sleepers))
                return true;
            return false;
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

        private void OnNodeToggle(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is BrowserTreeNodeDisplay display)
            {
                _viewModel?.ToggleNode(display.Node);
                RefreshTreeList();
            }
        }

        private void OnCanFlyToggled(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ShowCanFlyOnly = CanFlyToggle.IsChecked == true;
            RefreshTreeList();
        }

        private void OnRaceFilterChanged(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.Refresh();
            RefreshTreeList();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            _viewModel?.CollapseAll();
            RefreshTreeList();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            _viewModel?.ExpandAll();
            RefreshTreeList();
        }

        private void OnShipItemClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not BrowserTreeNodeDisplay displayItem) return;
            if (!displayItem.IsLeaf || displayItem.Node.Item == null) return;

            // Update selection highlight
            _selectedBorder?.Classes.Remove("selected");
            border.Classes.Add("selected");
            _selectedBorder = border;

            _viewModel?.SelectShip(displayItem.Node.Item);
            UpdateDetailPanel();
        }

        private void UpdateDetailPanel()
        {
            var detail = _viewModel?.SelectedShipDetail;
            if (detail == null)
            {
                DetailPanel.IsVisible = false;
                return;
            }

            DetailPanel.IsVisible = true;
            DetailName.Text = detail.Name;
            DetailRace.Text = $"Race: {detail.Race}";
            DetailGroupPath.Text = detail.GroupPath;
            DetailGroupPath.IsVisible = !string.IsNullOrEmpty(detail.GroupPath);
            DetailDescription.Text = detail.Description;
            PlanToFlyBtn.IsVisible = !detail.CanFly;

            // Load ship image async
            LoadShipImage(detail.TypeId);

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
                // Build a lookup from the ship's StaticSkillLevel prerequisites
                // so we can compute training time and planned status
                var shipPrereqs = _viewModel?.SelectedShip?.Prerequisites?.ToList();
                var character = _viewModel?.Character as Character;
                var plan = _planEditor?.Plan;

                foreach (var prereq in detail.Prerequisites)
                {
                    // Find the matching StaticSkillLevel for this prereq
                    var staticPrereq = shipPrereqs?.FirstOrDefault(
                        p => p.Skill.Name == prereq.Name && p.Level == prereq.RequiredLevel);

                    string? trainingTimeText = null;
                    bool isPlanned = false;

                    if (staticPrereq != null && !prereq.IsMet)
                    {
                        // Compute training time
                        if (character != null)
                        {
                            try
                            {
                                var scratchpad = new CharacterScratchpad(character);
                                scratchpad.Train(new StaticSkillLevel(staticPrereq.Skill, staticPrereq.Level));
                                trainingTimeText = FormatTrainingTime(scratchpad.TrainingTime);
                            }
                            catch
                            {
                                // Graceful fallback if scratchpad fails
                            }
                        }

                        // Check if skill is already planned
                        if (plan != null)
                        {
                            int plannedLevel = plan.GetPlannedLevel(staticPrereq.Skill);
                            isPlanned = plannedLevel >= staticPrereq.Level;
                        }
                    }

                    PrereqsList.Children.Add(CreatePrereqRow(prereq, trainingTimeText, isPlanned));
                }
            }

            // Show item properties
            UpdateProperties(_viewModel?.SelectedShip);
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
                // Category header
                PropertiesList.Children.Add(new TextBlock
                {
                    Text = section.CategoryName,
                    FontSize = 11,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = (IBrush)this.FindResource("EveAccentPrimaryBrush")!,
                    Margin = new global::Avalonia.Thickness(0, 4, 0, 2)
                });

                // Property rows
                foreach (var prop in section.Properties)
                {
                    var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };
                    grid.Children.Add(new TextBlock
                    {
                        Text = prop.Name,
                        FontSize = 10,
                        Foreground = (IBrush)this.FindResource("EveTextSecondaryBrush")!,
                        [Grid.ColumnProperty] = 0
                    });
                    grid.Children.Add(new TextBlock
                    {
                        Text = prop.FormattedValue,
                        FontSize = 10,
                        Foreground = (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                        [Grid.ColumnProperty] = 1
                    });
                    PropertiesList.Children.Add(grid);
                }
            }
        }

        private async void LoadShipImage(long typeId)
        {
            if (typeId <= 0) return;

            _lastRequestedShipId = typeId;
            ShipImage.Source = null;

            try
            {
                var url = ImageHelper.GetTypeRenderURL(typeId, 64);
                var drawingImage = await ImageService.GetImageAsync(url);
                if (drawingImage != null && _lastRequestedShipId == typeId)
                {
                    var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                        drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                    if (converted is Bitmap bitmap)
                        ShipImage.Source = bitmap;
                }
            }
            catch
            {
                // Image loading is best-effort
            }
        }

        private Grid CreatePrereqRow(ShipPrerequisiteInfo prereq, string? trainingTimeText, bool isPlanned)
        {
            var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto") };

            // Check/cross indicator
            grid.Children.Add(new TextBlock
            {
                Text = prereq.IsMet ? "\u2713 " : "\u2717 ",
                FontSize = 11,
                Foreground = prereq.IsMet
                    ? (IBrush)this.FindResource("EveSuccessGreenBrush")!
                    : (IBrush)this.FindResource("EveErrorRedBrush")!,
                VerticalAlignment = VerticalAlignment.Center,
                [Grid.ColumnProperty] = 0
            });

            // Skill name and level
            grid.Children.Add(new TextBlock
            {
                Text = prereq.DisplayText,
                FontSize = 11,
                Foreground = prereq.IsMet
                    ? (IBrush)this.FindResource("EveTextSecondaryBrush")!
                    : (IBrush)this.FindResource("EveTextPrimaryBrush")!,
                VerticalAlignment = VerticalAlignment.Center,
                [Grid.ColumnProperty] = 1
            });

            // Training time (only for unmet skills)
            if (!prereq.IsMet && trainingTimeText != null)
            {
                grid.Children.Add(new TextBlock
                {
                    Text = trainingTimeText,
                    FontSize = 10,
                    Foreground = (IBrush)this.FindResource("EveTextSecondaryBrush")!,
                    Margin = new global::Avalonia.Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    [Grid.ColumnProperty] = 2
                });
            }

            // "Planned" badge (only for unmet skills already in the plan)
            if (!prereq.IsMet && isPlanned)
            {
                grid.Children.Add(new TextBlock
                {
                    Text = "Planned",
                    FontSize = 10,
                    Foreground = (IBrush)this.FindResource("EveWarningYellowBrush")!,
                    Margin = new global::Avalonia.Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    [Grid.ColumnProperty] = 3
                });
            }

            return grid;
        }

        private static string FormatTrainingTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        private void OnPlanToFly(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.SelectedShip == null) return;
            _viewModel.PlanToFly(_viewModel.SelectedShip);
            _viewModel.Refresh();
            RefreshTreeList();
            UpdateDetailPanel();

            if (this.VisualRoot is PlanEditorWindow window)
                window.UpdateStatusBar();
        }
    }
}
