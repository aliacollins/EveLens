// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EveLens.Avalonia.Converters;
using EveLens.Avalonia.Services;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class CharacterComparisonWindow : Window
    {
        private readonly CharacterComparisonViewModel _vm = new();

        // Level block colors — resolved from theme at render time
        private IBrush TrainedBrush => FindBrush("EveAccentPrimaryBrush") ?? Brushes.Gold;

        private IBrush EmptyBlockBrush
        {
            get
            {
                // Dim version of accent color (20% opacity) so empty blocks are clearly
                // "part of the same scale" but obviously untrained
                if (this.TryFindResource("EveAccentPrimary", this.ActualThemeVariant, out var res) && res is Color c)
                    return new SolidColorBrush(new Color(50, c.R, c.G, c.B));
                return new SolidColorBrush(Color.Parse("#33707070"));
            }
        }

        private IBrush DiffHighlightBrush
        {
            get
            {
                // Use a subtle tint of the theme's accent color for difference rows
                if (this.TryFindResource("EveAccentPrimary", this.ActualThemeVariant, out var res) && res is Color c)
                    return new SolidColorBrush(new Color(25, c.R, c.G, c.B));
                return new SolidColorBrush(Color.Parse("#18FFFFFF"));
            }
        }

        private IDisposable? _fontScaleSub;

        public CharacterComparisonWindow()
        {
            InitializeComponent();
            BuildCharacterPicker();
            UpdateStatus();

            _fontScaleSub = AppServices.EventAggregator?.Subscribe<Common.Events.FontScaleChangedEvent>(
                _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(RefreshAll));
        }

        protected override void OnClosed(EventArgs e)
        {
            _fontScaleSub?.Dispose();
            _vm.Dispose();
            base.OnClosed(e);
        }

        #region Character Picker

        private void BuildCharacterPicker()
        {
            CharacterPickerPanel.Children.Clear();

            // Show selected characters with portraits and ✕
            foreach (var character in _vm.SelectedCharacters)
            {
                var capturedChar = character;
                var card = new StackPanel { Spacing = 2, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };

                var portraitImage = new Image { Width = 48, Height = 48, Stretch = Stretch.UniformToFill };
                portraitImage.Tag = character.CharacterID;
                var portraitBorder = new Border
                {
                    Width = 48, Height = 48,
                    CornerRadius = new CornerRadius(4),
                    ClipToBounds = true,
                    Background = FindBrush("EveBackgroundDarkestBrush"),
                    Child = portraitImage
                };
                card.Children.Add(portraitBorder);

                var nameRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 2 };
                nameRow.Children.Add(new TextBlock
                {
                    Text = character.Name,
                    FontSize = FontScaleService.Caption,
                    Foreground = FindBrush("EveAccentPrimaryBrush"),
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 80,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    [ToolTip.TipProperty] = character.Name
                });
                var removeBtn = new Button
                {
                    Content = "\u2715",
                    FontSize = FontScaleService.Tiny,
                    Padding = new Thickness(2, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = FindBrush("EveErrorRedBrush"),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    MinWidth = 0, MinHeight = 0,
                    VerticalAlignment = VerticalAlignment.Center
                };
                removeBtn.Click += (_, _) => { _vm.RemoveCharacter(capturedChar); RefreshAll(); };
                nameRow.Children.Add(removeBtn);
                card.Children.Add(nameRow);

                card.Children.Add(new TextBlock
                {
                    Text = $"{character.SkillPoints:N0} SP",
                    FontSize = FontScaleService.Tiny,
                    Foreground = FindBrush("EveTextDisabledBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                CharacterPickerPanel.Children.Add(card);
                LoadPortraitAsync(portraitImage, character.CharacterID);
            }

            // "+ Add" button
            if (_vm.SelectedCharacters.Count < 10)
            {
                var addBtn = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = FindBrush("EveTextDisabledBrush"),
                    CornerRadius = new CornerRadius(4),
                    Width = 48, Height = 48,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    Content = new TextBlock
                    {
                        Text = "+",
                        FontSize = FontScaleService.Title,
                        Foreground = FindBrush("EveAccentPrimaryBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                addBtn.Click += OnAddCharacterClick;
                CharacterPickerPanel.Children.Add(addBtn);
            }
        }

        private void OnAddCharacterClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var allChars = AppServices.Characters.Where(c => c.Monitored).ToList();
            var selectedIds = _vm.SelectedCharacters.Select(c => c.CharacterID).ToHashSet();

            var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
            var panel = new StackPanel { Spacing = 1, MinWidth = 180 };

            panel.Children.Add(new TextBlock
            {
                Text = "Add character:",
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextDisabledBrush"),
                Margin = new Thickness(8, 4, 0, 4)
            });

            foreach (var character in allChars.Where(c => !selectedIds.Contains(c.CharacterID)))
            {
                var capturedChar = character;
                var charBtn = new Button
                {
                    Content = character.Name,
                    FontSize = FontScaleService.Body,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                charBtn.Click += (_, _) =>
                {
                    flyout.Hide();
                    _vm.AddCharacter(capturedChar);
                    RefreshAll();
                };
                panel.Children.Add(charBtn);
            }

            flyout.Content = panel;
            flyout.ShowAt(btn);
        }

        #endregion

        #region Comparison Grid

        private void BuildComparisonGrid()
        {
            ComparisonPanel.Children.Clear();

            if (_vm.SelectedCharacters.Count < 2)
            {
                EmptyState.IsVisible = true;
                ComparisonScroller.IsVisible = false;
                return;
            }

            EmptyState.IsVisible = false;
            ComparisonScroller.IsVisible = true;

            // Sticky header row
            ComparisonPanel.Children.Add(BuildHeaderRow());

            // Skill groups
            foreach (var group in _vm.Groups)
            {
                ComparisonPanel.Children.Add(BuildGroupHeader(group));

                if (group.IsExpanded)
                {
                    foreach (var skill in group.VisibleSkills)
                    {
                        ComparisonPanel.Children.Add(BuildSkillRow(skill));
                    }
                }
            }
        }

        private Border BuildHeaderRow()
        {
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            grid.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));

            for (int i = 0; i < _vm.SelectedCharacters.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });
                var nameBlock = new TextBlock
                {
                    Text = _vm.SelectedCharacters[i].Name,
                    FontSize = FontScaleService.Small,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = FindBrush("EveAccentPrimaryBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    [ToolTip.TipProperty] = _vm.SelectedCharacters[i].Name,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    [Grid.ColumnProperty] = i + 1
                };
                grid.Children.Add(nameBlock);
            }

            grid.Children.Add(new TextBlock
            {
                Text = "Skill",
                FontSize = FontScaleService.Small,
                FontWeight = FontWeight.SemiBold,
                Foreground = FindBrush("EveTextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                [Grid.ColumnProperty] = 0
            });

            return new Border
            {
                Background = FindBrush("EveBackgroundMediumBrush"),
                Padding = new Thickness(8, 4),
                BorderBrush = FindBrush("EveBorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid
            };
        }

        private Control BuildGroupHeader(ComparisonGroupEntry group)
        {
            var capturedGroup = group;
            var header = new Border
            {
                Background = FindBrush("EveBackgroundMediumBrush"),
                Padding = new Thickness(8, 5),
                Margin = new Thickness(0, 4, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            panel.Children.Add(new TextBlock
            {
                Text = group.IsExpanded ? "\u25BE" : "\u25B8",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveTextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = group.GroupName,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                Foreground = FindBrush("EveAccentPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = group.CountText,
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextDisabledBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Child = panel;
            header.PointerPressed += (_, _) =>
            {
                capturedGroup.IsExpanded = !capturedGroup.IsExpanded;
                BuildComparisonGrid();
            };

            return header;
        }

        private Border BuildSkillRow(ComparisonSkillEntry skill)
        {
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            grid.ColumnDefinitions.Add(new ColumnDefinition(200, GridUnitType.Pixel));

            // Skill name
            grid.Children.Add(new TextBlock
            {
                Text = skill.SkillName,
                FontSize = FontScaleService.Small,
                Foreground = FindBrush("EveTextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(16, 0, 8, 0),
                [Grid.ColumnProperty] = 0
            });

            // Level blocks per character
            for (int i = 0; i < skill.Levels.Length; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 90 });

                if (!skill.IsKnown[i])
                {
                    // Dash for not injected
                    grid.Children.Add(new TextBlock
                    {
                        Text = "\u2014",
                        FontSize = FontScaleService.Small,
                        Foreground = FindBrush("EveTextDisabledBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        [Grid.ColumnProperty] = i + 1
                    });
                }
                else
                {
                    // 5 level blocks
                    var blocks = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 2,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    for (int lvl = 1; lvl <= 5; lvl++)
                    {
                        blocks.Children.Add(new Border
                        {
                            Width = 10, Height = 12,
                            CornerRadius = new CornerRadius(2),
                            Background = lvl <= skill.Levels[i] ? TrainedBrush : EmptyBlockBrush
                        });
                    }

                    grid.Children.Add(new Border
                    {
                        Child = blocks,
                        [Grid.ColumnProperty] = i + 1
                    });
                }
            }

            return new Border
            {
                Background = skill.IsDifferent ? DiffHighlightBrush : Brushes.Transparent,
                Padding = new Thickness(8, 3),
                BorderBrush = FindBrush("EveBorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid
            };
        }

        #endregion

        #region Event Handlers

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            string filter = FilterBox.Text?.Trim() ?? "";
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(filter);
            _vm.TextFilter = filter;
            BuildComparisonGrid();
            UpdateStatus();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            FilterBox.Text = "";
            ClearFilterBtn.IsVisible = false;
            _vm.TextFilter = "";
            BuildComparisonGrid();
            UpdateStatus();
        }

        private void OnDifferencesToggled(object? sender, RoutedEventArgs e)
        {
            _vm.ShowDifferencesOnly = DifferencesToggle.IsChecked == true;
            BuildComparisonGrid();
            UpdateStatus();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            _vm.CollapseAll();
            BuildComparisonGrid();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            _vm.ExpandAll();
            BuildComparisonGrid();
        }

        #endregion

        #region Helpers

        private void RefreshAll()
        {
            BuildCharacterPicker();
            BuildComparisonGrid();
            UpdateStatus();

            // Auto-size window width based on character count
            int chars = _vm.SelectedCharacters.Count;
            double neededWidth = 220 + (chars * 110); // skill col + ~110px per character
            if (neededWidth > Width)
                Width = Math.Min(neededWidth, 1600);
        }

        private void UpdateStatus()
        {
            if (_vm.SelectedCharacters.Count < 2)
            {
                StatusText.Text = "Select at least 2 characters to begin comparison";
                return;
            }

            StatusText.Text = $"Skills shown: {_vm.VisibleSkillCount}  |  " +
                              $"Differences: {_vm.DifferenceCount}  |  " +
                              $"Characters: {_vm.SelectedCharacters.Count}";
        }

        private async void LoadPortraitAsync(Image image, long characterId)
        {
            try
            {
                var drawingImage = await ImageService.GetCharacterImageAsync(characterId);
                if (drawingImage != null)
                {
                    var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                        drawingImage, typeof(Bitmap), null!, CultureInfo.InvariantCulture);
                    if (converted is Bitmap bitmap)
                        image.Source = bitmap;
                }
            }
            catch { }
        }

        private IBrush? FindBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var res) && res is IBrush b)
                return b;
            return null;
        }

        #endregion
    }
}
