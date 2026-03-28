// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

using EveLens.Common.ViewModels;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterSkillsView : UserControl
    {
        private static readonly IBrush FilledBlock = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush EmptyBlock = new SolidColorBrush(Color.Parse("#FF2A2A4A"));
        private static readonly IBrush TrainingBlock = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush TrainedNameColor = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush UntrainedNameColor = new SolidColorBrush(Color.Parse("#FF505060"));
        private static readonly IBrush TrainingNameColor = new SolidColorBrush(Color.Parse("#FF81C784"));

        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private SkillOverlayViewModel? _viewModel;

        public CharacterSkillsView()
        {
            InitializeComponent();
            SkillItemsControl.ItemTemplate = CreateNodeTemplate();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterUpdatedEvent>(OnDataUpdated);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            LoadData();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;

            if (_viewModel != null)
            {
                _viewModel.TreeSource.Changed -= OnTreeChanged;
                _viewModel.Dispose();
                _viewModel = null;
            }
        }

        private void LoadData()
        {
            if (this.GetVisualRoot() == null) return;

            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character == null) return;

            _characterId = character.CharacterID;

            if (_viewModel == null)
            {
                _viewModel = new SkillOverlayViewModel();
                _viewModel.TreeSource.Changed += OnTreeChanged;
            }

            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnDataUpdated(CharacterUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnTreeChanged()
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateItemsSource();
                UpdateStatus();
            });
        }

        private void UpdateItemsSource()
        {
            // Assign once — FlattenedTreeSource fires INotifyCollectionChanged
            // for granular Add/Remove on expand/collapse, Reset on full rebuild.
            if (SkillItemsControl.ItemsSource != _viewModel?.TreeSource)
                SkillItemsControl.ItemsSource = _viewModel?.TreeSource;
        }

        private void UpdateStatus()
        {
            StatusText.Text = _viewModel?.StatusText ?? "";
            UpdateLevelBreakdown();
        }

        private void UpdateLevelBreakdown()
        {
            if (_viewModel == null) return;

            AllSkillsBtn.Content = $"All Skills ({_viewModel.TotalPublicSkills})";
            AllTrainedBtn.Content = $"All Trained ({_viewModel.TotalTrained})";
            LevelVBtn.Content = $"Level V ({_viewModel.GetSkillsAtLevel(5)})";
            LevelIVBtn.Content = $"Level IV ({_viewModel.GetSkillsAtLevel(4)})";
            LevelIIIBtn.Content = $"Level III ({_viewModel.GetSkillsAtLevel(3)})";
            LevelIIBtn.Content = $"Level II ({_viewModel.GetSkillsAtLevel(2)})";
            LevelIBtn.Content = $"Level I ({_viewModel.GetSkillsAtLevel(1)})";
            LevelZeroBtn.Content = $"Injected ({_viewModel.GetSkillsAtLevel(0)})";
        }

        private ToggleButton?[] LevelButtons => new[]
        {
            AllSkillsBtn, AllTrainedBtn, LevelVBtn, LevelIVBtn, LevelIIIBtn,
            LevelIIBtn, LevelIBtn, LevelZeroBtn
        };

        private void OnLevelFilterClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton clicked || _viewModel == null) return;

            int tag = clicked.Tag is int t ? t : int.Parse(clicked.Tag?.ToString() ?? "-1");

            // Radio-button behavior: uncheck all others, keep clicked checked
            foreach (var btn in LevelButtons)
            {
                if (btn != null && btn != clicked)
                    btn.IsChecked = false;
            }
            clicked.IsChecked = true;

            _viewModel.LevelFilter = tag;
        }

        #region Template Builder

        private FuncDataTemplate<FlatTreeNode<object>> CreateNodeTemplate()
        {
            return new FuncDataTemplate<FlatTreeNode<object>>((node, _) =>
            {
                if (node == null)
                    return new Border();
                if (node.IsGroup)
                    return BuildGroupHeader(node);
                return BuildSkillRow(node);
            });
        }

        private Control BuildGroupHeader(FlatTreeNode<object> node)
        {
            var groupTemplate = node.Data as SkillGroupTemplate;
            var groupName = groupTemplate?.Name ?? node.GroupKey;

            // Compute group stats from the overlay
            string countText = "";
            string spText = "";
            if (groupTemplate != null && _viewModel != null)
            {
                int trained = 0;
                long sp = 0;
                foreach (var skill in groupTemplate.Skills)
                {
                    var state = _viewModel.GetSkillState(skill.SkillId);
                    if (state.IsKnown)
                    {
                        trained++;
                        sp += state.SkillPoints;
                    }
                }
                countText = $"{trained} / {groupTemplate.Skills.Count}";
                spText = FormatSP(sp);
            }

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var chevronTb = new TextBlock
            {
                Text = node.Chevron,
                FontSize = FontScaleService.Body,
                Width = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            chevronTb.Foreground = FindBrush("EveTextSecondaryBrush");
            Grid.SetColumn(chevronTb, 0);
            grid.Children.Add(chevronTb);

            var nameTb = new TextBlock
            {
                Text = groupName,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            nameTb.Foreground = FindBrush("EveAccentPrimaryBrush");
            Grid.SetColumn(nameTb, 1);
            grid.Children.Add(nameTb);

            var countTb = new TextBlock
            {
                Text = countText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0)
            };
            countTb.Foreground = FindBrush("EveTextSecondaryBrush");
            Grid.SetColumn(countTb, 2);
            grid.Children.Add(countTb);

            var spTb = new TextBlock
            {
                Text = spText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0)
            };
            spTb.Foreground = FindBrush("EveTextDisabledBrush");
            Grid.SetColumn(spTb, 3);
            grid.Children.Add(spTb);

            var border = new Border
            {
                Classes = { "group-header" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = grid
            };

            border.PointerPressed += OnGroupHeaderClicked;

            return border;
        }

        private Control BuildSkillRow(FlatTreeNode<object> node)
        {
            var skillTemplate = node.Data as SkillEntryTemplate;
            if (skillTemplate == null)
                return new Border();

            var state = _viewModel?.GetSkillState(skillTemplate.SkillId) ?? default;

            // Name brush
            IBrush nameBrush = state.IsTraining ? TrainingNameColor
                : state.IsKnown ? TrainedNameColor : UntrainedNameColor;

            // SP text
            string spText = state.IsKnown ? $"{state.SkillPoints:N0} SP" : "";

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Skill name
            var nameTb = new TextBlock
            {
                Text = skillTemplate.Name,
                FontSize = FontScaleService.Body,
                Foreground = nameBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameTb, 0);
            grid.Children.Add(nameTb);

            // Level blocks (5 blocks)
            var blocksPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0)
            };
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                blocksPanel.Children.Add(new Border
                {
                    Width = 8,
                    Height = 10,
                    CornerRadius = new CornerRadius(1.5),
                    Background = BlockColor(lvl, state)
                });
            }
            Grid.SetColumn(blocksPanel, 1);
            grid.Children.Add(blocksPanel);

            // Rank text
            var rankTb = new TextBlock
            {
                Text = skillTemplate.RankText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 55,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(4, 0)
            };
            rankTb.Foreground = FindBrush("EveTextDisabledBrush");
            Grid.SetColumn(rankTb, 2);
            grid.Children.Add(rankTb);

            // SP text
            var spTb = new TextBlock
            {
                Text = spText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 75,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(4, 0, 0, 0)
            };
            spTb.Foreground = FindBrush("EveTextSecondaryBrush");
            Grid.SetColumn(spTb, 3);
            grid.Children.Add(spTb);

            var border = new Border
            {
                Classes = { "item-row" },
                Padding = new Thickness(24, 3, 8, 3),
                Background = FindBrush("EveBackgroundDarkBrush"),
                BorderBrush = FindBrush("EveBorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 0.5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = grid
            };

            return border;
        }

        private IBrush? FindBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is IBrush brush)
                return brush;
            return null;
        }

        private static IBrush BlockColor(int lvl, SkillState state)
        {
            if (lvl <= state.Level) return FilledBlock;
            if (state.IsTraining && lvl == state.Level + 1) return TrainingBlock;
            return EmptyBlock;
        }

        private static string FormatSP(long sp)
        {
            if (sp >= 1_000_000) return $"{sp / 1_000_000.0:N1}M SP";
            if (sp >= 1_000) return $"{sp / 1_000.0:N0}K SP";
            if (sp > 0) return $"{sp:N0} SP";
            return "";
        }

        #endregion

        #region Toolbar Handlers

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel == null || sender is not Control control) return;
            if (control.DataContext is FlatTreeNode<object> node && node.IsGroup)
            {
                int index = _viewModel.TreeSource.IndexOfGroup(node.GroupKey);
                if (index >= 0) _viewModel.TreeSource.ToggleExpand(index);
            }
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e) => _viewModel?.CollapseAll();

        private void OnExpandAll(object? sender, RoutedEventArgs e) => _viewModel?.ExpandAll();

        private void OnToggleShowAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ShowAll = ShowAllToggle.IsChecked == true;
            ShowAllToggle.Content = _viewModel.ShowAll ? "All Skills" : "Trained Only";
            UpdateStatus();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.Filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.Filter);
            UpdateStatus();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.Filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            UpdateStatus();
        }

        #endregion
    }
}
