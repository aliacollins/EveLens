// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using EveLens.Common.Models;
using EveLens.Common.ViewModels;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Events;
using EveLens.Common.Services;

using EveLens.Common.Services;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterAssetsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private AssetBrowserViewModel? _viewModel;
        private FlattenedTreeSource<object>? _treeSource;

        public CharacterAssetsView()
        {
            InitializeComponent();
            AssetItemsControl.ItemTemplate = CreateNodeTemplate();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterAssetsUpdatedEvent>(OnDataUpdated);
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

            if (_treeSource != null)
            {
                _treeSource.Changed -= OnTreeChanged;
                _treeSource = null;
            }

            _viewModel?.Dispose();
            _viewModel = null;
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
            if (character is not CCPCharacter) return;

            _characterId = character.CharacterID;

            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.AssetList))
            {
                EnablePrompt.IsVisible = true;
                EmptyState.IsVisible = false;
                DataContent.IsVisible = false;
                return;
            }
            EnablePrompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.AssetList))
            {
                ScopePrompt.IsVisible = true;
                return;
            }
            ScopePrompt.IsVisible = false;

            _viewModel ??= new AssetBrowserViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            bool hasData = _viewModel.TotalItems > 0;
            EmptyState.IsVisible = !hasData;
            DataContent.IsVisible = hasData;
            if (!hasData) return;

            if (_treeSource == null)
            {
                _treeSource = new FlattenedTreeSource<object>();
                _treeSource.Changed += OnTreeChanged;
            }

            // Load persisted expand state (assets default to collapsed)
            var expandState = CollapseStateHelper.LoadExpandState(_characterId, "Assets");
            _treeSource.SetExpandState(expandState);

            _isRebuilding = true;
            PopulateTree();
            _isRebuilding = false;
            UpdateItemsSource();
            UpdateStatus();
        }

        private void PopulateTree()
        {
            if (_viewModel == null || _treeSource == null) return;

            if (_viewModel.IsHierarchical)
            {
                var groups = new List<GroupData<object>>();
                foreach (var region in _viewModel.HierarchicalGroups)
                {
                    var systemSubgroups = new List<GroupData<object>>();
                    foreach (var system in region.Systems)
                    {
                        var stationSubgroups = new List<GroupData<object>>();
                        foreach (var station in system.Stations)
                        {
                            stationSubgroups.Add(new GroupData<object>(
                                $"{region.Name}/{system.Name}/{station.Name}",
                                station,
                                station.Items.Cast<object>().ToList()));
                        }
                        systemSubgroups.Add(new GroupData<object>(
                            $"{region.Name}/{system.Name}",
                            system,
                            new List<object>(),
                            stationSubgroups));
                    }
                    groups.Add(new GroupData<object>(
                        region.Name,
                        region,
                        new List<object>(),
                        systemSubgroups));
                }
                _treeSource.SetData(groups);
            }
            else
            {
                var groups = _viewModel.Groups.Select(g =>
                    new GroupData<object>(g.Name, g, g.Items.Cast<object>().ToList()))
                    .ToList();
                _treeSource.SetData(groups);
            }
        }

        private bool _isRebuilding;

        private void OnTreeChanged()
        {
            // Save expand state immediately (synchronous) before any potential detach
            if (!_isRebuilding && _characterId != 0 && _treeSource != null)
                CollapseStateHelper.SaveExpandState(_characterId, "Assets", _treeSource.GetExpandState());

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateItemsSource();
                UpdateStatus();
            });
        }

        private void UpdateItemsSource()
        {
            if (AssetItemsControl.ItemsSource != _treeSource)
                AssetItemsControl.ItemsSource = _treeSource;
        }

        private void UpdateStatus()
        {
            if (_viewModel != null)
                StatusText.Text = $"Items: {_viewModel.TotalItems:N0} in {_viewModel.GroupCount} {(_viewModel.IsHierarchical ? "regions" : "groups")}  |  Est. Value: {_viewModel.TotalValue:N0} ISK";
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
                return BuildItemRow(node);
            });
        }

        private Control BuildGroupHeader(FlatTreeNode<object> node)
        {
            int leftPad = 8 + node.Depth * 16;

            string groupName = "";
            string countText = "";
            string summaryText = "";

            switch (node.Data)
            {
                case AssetRegionGroup region:
                    groupName = region.Name;
                    countText = region.CountText;
                    summaryText = region.SummaryText;
                    break;
                case AssetSystemGroup system:
                    groupName = system.Name;
                    countText = system.CountText;
                    summaryText = "";
                    break;
                case AssetStationGroup station:
                    groupName = station.Name;
                    countText = station.CountText;
                    summaryText = station.SummaryText;
                    break;
                case AssetBrowserGroupEntry group:
                    groupName = group.Name;
                    countText = group.CountText;
                    summaryText = group.SummaryText;
                    break;
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

            if (!string.IsNullOrEmpty(summaryText))
            {
                var valTb = new TextBlock
                {
                    Text = summaryText,
                    FontSize = FontScaleService.Small,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0)
                };
                valTb.Foreground = FindBrush("EveTextDisabledBrush");
                Grid.SetColumn(valTb, 3);
                grid.Children.Add(valTb);
            }

            var border = new Border
            {
                Classes = { "group-header" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(leftPad, 6, 8, 6),
                Child = grid
            };

            border.PointerPressed += OnGroupHeaderClicked;

            return border;
        }

        private Control BuildItemRow(FlatTreeNode<object> node)
        {
            var item = node.Data as AssetBrowserItemEntry;
            if (item == null)
                return new Border();

            int leftPad = 8 + node.Depth * 16;

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var nameTb = new TextBlock
            {
                Text = item.ItemName,
                FontSize = FontScaleService.Body,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            nameTb.Foreground = FindBrush("EveTextPrimaryBrush");
            Grid.SetColumn(nameTb, 0);
            grid.Children.Add(nameTb);

            var qtyTb = new TextBlock
            {
                Text = item.QuantityText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
                MinWidth = 50,
                TextAlignment = TextAlignment.Right
            };
            qtyTb.Foreground = FindBrush("EveTextSecondaryBrush");
            Grid.SetColumn(qtyTb, 1);
            grid.Children.Add(qtyTb);

            var valTb = new TextBlock
            {
                Text = item.ValueText,
                FontSize = FontScaleService.Small,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80,
                TextAlignment = TextAlignment.Right
            };
            valTb.Foreground = FindBrush("EveSuccessGreenBrush");
            Grid.SetColumn(valTb, 2);
            grid.Children.Add(valTb);

            return new Border
            {
                Classes = { "item-row" },
                Padding = new Thickness(leftPad, 3, 8, 3),
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
            _viewModel.GroupMode = GroupByCombo.SelectedIndex;
            PopulateTree();
            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.Filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.Filter);
            PopulateTree();
            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.Filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            PopulateTree();
            UpdateItemsSource();
            UpdateStatus();
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.AssetList);
            LoadData();
        }

        private void OnDataUpdated(CharacterAssetsUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        #endregion
    }
}
