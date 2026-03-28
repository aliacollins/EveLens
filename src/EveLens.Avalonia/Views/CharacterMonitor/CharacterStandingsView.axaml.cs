// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Models;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;

using EveLens.Common.ViewModels.Lists;
using EveLens.Common.Services;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterStandingsView : UserControl
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));
        private static readonly IBrush ExcellentBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush GoodBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush BadBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush TerribleBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));

        private StandingsListViewModel? _viewModel;
        private FlattenedTreeSource<object>? _treeSource;
        private bool _isRebuilding;
        private long _characterId;
        private IDisposable? _fontScaleSub;
        private readonly List<StandingDisplayEntry> _activeEntries = new();

        public CharacterStandingsView()
        {
            InitializeComponent();
            StandingItemsControl.ItemTemplate = CreateNodeTemplate();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _fontScaleSub ??= AppServices.EventAggregator?.Subscribe<EveLens.Common.Events.FontScaleChangedEvent>(
                _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData));
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
            _fontScaleSub?.Dispose();
            _fontScaleSub = null;

            if (_treeSource != null)
            {
                _treeSource.Changed -= OnTreeChanged;
                _treeSource = null;
            }

            CleanupEntries();

            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void CleanupEntries()
        {
            foreach (var entry in _activeEntries)
                entry.Detach();
            _activeEntries.Clear();
        }

        private void LoadData()
        {
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character == null) return;

            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var scopePrompt = this.FindControl<Border>("ScopePrompt");
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.Standings))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.Standings))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _characterId = character.CharacterID;
            _viewModel ??= new StandingsListViewModel();
            _viewModel.Character = character;
            _viewModel.Refresh();

            bool firstLoad = _treeSource == null;
            if (_treeSource == null)
            {
                _treeSource = new FlattenedTreeSource<object>();
                _treeSource.Changed += OnTreeChanged;
            }

            // Load persisted expand state (standings default to all expanded)
            var expandState = CollapseStateHelper.LoadExpandState(_characterId, "Standings");
            _treeSource.SetExpandState(expandState);

            _isRebuilding = true;
            PopulateTree();
            if (!CollapseStateHelper.HasSavedState(_characterId, "Standings")) _treeSource.ExpandAll();
            _isRebuilding = false;
            UpdateItemsSource();
            UpdateStatus();
        }

        private void PopulateTree()
        {
            if (_viewModel == null || _treeSource == null) return;

            CleanupEntries();

            var grouped = _viewModel.GroupedItems;
            var groups = new List<GroupData<object>>();

            foreach (var g in grouped)
            {
                string name = string.IsNullOrEmpty(g.Key) ? "All Standings" : g.Key;
                var entries = g.Items.Select(s =>
                {
                    var entry = new StandingDisplayEntry(s, OnPortraitLoaded);
                    _activeEntries.Add(entry);
                    return (object)entry;
                }).ToList();

                if (entries.Count > 0)
                {
                    groups.Add(new GroupData<object>(
                        name,
                        new StandingGroupInfo(name, entries.Count),
                        entries));
                }
            }

            _treeSource.SetData(groups);

            var isEmpty = _viewModel.TotalItemCount == 0;
            EmptyState.IsVisible = isEmpty;
            MainScroller.IsVisible = !isEmpty;

            foreach (var entry in _activeEntries)
                entry.LoadPortrait();
        }

        private void OnPortraitLoaded()
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateItemsSource();
            });
        }

        private void OnTreeChanged()
        {
            if (!_isRebuilding && _characterId != 0 && _treeSource != null)
                CollapseStateHelper.SaveExpandState(_characterId, "Standings", _treeSource.GetExpandState());

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateItemsSource();
                UpdateStatus();
            });
        }

        private void UpdateItemsSource()
        {
            if (StandingItemsControl.ItemsSource != _treeSource)
                StandingItemsControl.ItemsSource = _treeSource;
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Standings: {_viewModel?.TotalItemCount ?? 0}";
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
                return BuildStandingRow(node);
            });
        }

        private Control BuildGroupHeader(FlatTreeNode<object> node)
        {
            var groupInfo = node.Data as StandingGroupInfo;
            string groupName = groupInfo?.Name ?? node.GroupKey;
            string countText = groupInfo?.CountText ?? "";

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"),
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

            var border = new Border
            {
                Classes = { "group-header" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = grid
            };

            border.PointerPressed += OnGroupHeaderClicked;

            return border;
        }

        private Control BuildStandingRow(FlatTreeNode<object> node)
        {
            var entry = node.Data as StandingDisplayEntry;
            if (entry == null)
                return new Border();

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinHeight = 32
            };

            // Portrait
            var portraitBorder = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = FindBrush("EveBackgroundMediumBrush"),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                ClipToBounds = true
            };

            var portraitPanel = new Panel();

            var initialTb = new TextBlock
            {
                Text = entry.Initial,
                FontSize = FontScaleService.Title,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = !entry.HasPortrait
            };
            initialTb.Foreground = FindBrush("EveTextSecondaryBrush");
            portraitPanel.Children.Add(initialTb);

            if (entry.HasPortrait)
            {
                var portraitImage = new Image
                {
                    Source = entry.Portrait,
                    Width = 32,
                    Height = 32,
                    Stretch = Stretch.UniformToFill
                };
                portraitPanel.Children.Add(portraitImage);
            }

            portraitBorder.Child = portraitPanel;
            Grid.SetColumn(portraitBorder, 0);
            grid.Children.Add(portraitBorder);

            // Entity name
            var nameTb = new TextBlock
            {
                Text = entry.EntityName,
                FontSize = FontScaleService.Body,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            nameTb.Foreground = FindBrush("EveAccentPrimaryBrush");
            Grid.SetColumn(nameTb, 1);
            grid.Children.Add(nameTb);

            // Standing bar: 120px total, center origin
            var barBackground = new Border
            {
                Width = 120,
                Height = 10,
                CornerRadius = new CornerRadius(3),
                Background = FindBrush("EveBackgroundDarkestBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0)
            };

            var barPanel = new Panel();

            // Center line
            barPanel.Children.Add(new Border
            {
                Width = 1,
                Height = 10,
                Background = FindBrush("EveTextDisabledBrush"),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            double barWidth = Math.Abs(entry.StandingNumeric) / 10.0 * 60.0;

            if (entry.IsNegative)
            {
                barPanel.Children.Add(new Border
                {
                    Width = barWidth,
                    Height = 8,
                    CornerRadius = new CornerRadius(2),
                    Background = FindBrush("EveStandingNegativeBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(-60, 0, 0, 0)
                });
            }

            if (entry.IsPositive)
            {
                barPanel.Children.Add(new Border
                {
                    Width = barWidth,
                    Height = 8,
                    CornerRadius = new CornerRadius(2),
                    Background = FindBrush("EveStandingPositiveBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(60, 0, 0, 0)
                });
            }

            barBackground.Child = barPanel;
            Grid.SetColumn(barBackground, 2);
            grid.Children.Add(barBackground);

            // Standing value
            var standingTb = new TextBlock
            {
                Text = entry.StandingText,
                FontSize = FontScaleService.Body,
                Foreground = entry.StandingBrush,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 40,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(4, 0)
            };
            Grid.SetColumn(standingTb, 3);
            grid.Children.Add(standingTb);

            // Effective standing
            var effectiveTb = new TextBlock
            {
                Text = entry.EffectiveText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 50,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(4, 0)
            };
            effectiveTb.Foreground = FindBrush("EveTextSecondaryBrush");
            Grid.SetColumn(effectiveTb, 4);
            grid.Children.Add(effectiveTb);

            // Status label
            var statusTb = new TextBlock
            {
                Text = entry.StatusText,
                FontSize = FontScaleService.Small,
                Foreground = entry.StatusBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                MinWidth = 60
            };
            Grid.SetColumn(statusTb, 5);
            grid.Children.Add(statusTb);

            return new Border
            {
                Classes = { "item-row" },
                Padding = new Thickness(20, 4, 10, 4),
                Background = FindBrush("EveBackgroundDarkBrush"),
                BorderBrush = FindBrush("EveBorderBrush"),
                BorderThickness = new Thickness(0, 0, 0, 0.5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = grid
            };
        }

        private IBrush? FindBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is IBrush brush)
                return brush;
            return null;
        }

        #endregion

        #region Toolbar Handlers

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (_treeSource == null || sender is not Control control) return;
            if (control.DataContext is FlatTreeNode<object> node && node.IsGroup)
            {
                int index = _treeSource.IndexOfGroup(node.GroupKey);
                if (index >= 0) _treeSource.ToggleExpand(index);
            }
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e) => _treeSource?.CollapseAll();

        private void OnExpandAll(object? sender, RoutedEventArgs e) => _treeSource?.ExpandAll();

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            var idx = GroupByCombo.SelectedIndex;
            if (idx < 0) return;
            _viewModel.Grouping = (StandingGrouping)idx;
            PopulateTree();
            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.TextFilter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.TextFilter);
            PopulateTree();
            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.TextFilter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            PopulateTree();
            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.Standings);
            LoadData();
        }

        #endregion
    }

    internal sealed class StandingGroupInfo
    {
        public string Name { get; }
        public string CountText { get; }

        public StandingGroupInfo(string name, int count)
        {
            Name = name;
            CountText = $"{count} standings";
        }
    }

    internal sealed class StandingDisplayEntry
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));
        private static readonly IBrush ExcellentBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush GoodBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush BadBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush TerribleBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));

        private readonly Standing _standing;
        private readonly Action? _onPortraitLoaded;
        private Bitmap? _portrait;
        private bool _subscribed;

        public StandingDisplayEntry(Standing standing, Action? onPortraitLoaded)
        {
            _standing = standing;
            _onPortraitLoaded = onPortraitLoaded;
        }

        public string EntityName => _standing.EntityName;
        public string Initial => string.IsNullOrEmpty(_standing.EntityName) ? "?" : _standing.EntityName[..1].ToUpperInvariant();
        public double StandingNumeric => _standing.StandingValue;
        public string StandingText => _standing.StandingValue.ToString("+0.00;-0.00;0.00");
        public string EffectiveText => $"({_standing.EffectiveStanding:+0.00;-0.00;0.00})";
        public bool IsPositive => _standing.StandingValue > 0;
        public bool IsNegative => _standing.StandingValue < 0;
        public bool HasPortrait => _portrait != null;
        public Bitmap? Portrait => _portrait;

        public string StatusText
        {
            get
            {
                var status = Standing.Status(_standing.StandingValue);
                return status.ToString();
            }
        }

        public IBrush StandingBrush
        {
            get
            {
                if (_standing.StandingValue > 0) return PositiveBrush;
                if (_standing.StandingValue < 0) return NegativeBrush;
                return NeutralBrush;
            }
        }

        public IBrush StatusBrush
        {
            get
            {
                var status = Standing.Status(_standing.StandingValue);
                return status switch
                {
                    StandingStatus.Excellent => ExcellentBrush,
                    StandingStatus.Good => GoodBrush,
                    StandingStatus.Neutral => NeutralBrush,
                    StandingStatus.Bad => BadBrush,
                    StandingStatus.Terrible => TerribleBrush,
                    _ => NeutralBrush
                };
            }
        }

        public void LoadPortrait()
        {
            var entityImage = _standing.EntityImage;
            if (entityImage != null)
            {
                var bmp = DrawingImageToAvaloniaConverter.Instance.Convert(
                    entityImage, typeof(Bitmap), null, CultureInfo.InvariantCulture) as Bitmap;
                if (bmp != null)
                {
                    _portrait = bmp;
                    return;
                }
            }

            if (!_subscribed)
            {
                _standing.StandingImageUpdated += OnStandingImageUpdated;
                _subscribed = true;
            }
        }

        public void Detach()
        {
            if (_subscribed)
            {
                _standing.StandingImageUpdated -= OnStandingImageUpdated;
                _subscribed = false;
            }
        }

        private void OnStandingImageUpdated(object? sender, EventArgs e)
        {
            var entityImage = _standing.EntityImage;
            if (entityImage == null) return;

            var bmp = DrawingImageToAvaloniaConverter.Instance.Convert(
                entityImage, typeof(Bitmap), null, CultureInfo.InvariantCulture) as Bitmap;

            if (bmp != null)
            {
                _portrait = bmp;
                _onPortraitLoaded?.Invoke();
            }
        }
    }
}
