// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Events;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterAssetsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private AssetBrowserViewModel? _viewModel;

        // Brushes
        private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush TextSecBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));
        private static readonly IBrush TextDimBrush = new SolidColorBrush(Color.Parse("#FF707070"));
        private static readonly IBrush TextPrimBrush = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush MediumBg = new SolidColorBrush(Color.Parse("#FF16213E"));
        private static readonly IBrush DarkBg = new SolidColorBrush(Color.Parse("#FF1A1A2E"));
        private static readonly IBrush BorderBr = new SolidColorBrush(Color.Parse("#FF2A2A4A"));

        public CharacterAssetsView()
        {
            InitializeComponent();
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

            _viewModel ??= new AssetBrowserViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            bool hasData = _viewModel.TotalItems > 0;
            EmptyState.IsVisible = !hasData;
            DataContent.IsVisible = hasData;
            if (!hasData) return;

            BuildTree();

            StatusText.Text = $"Items: {_viewModel.TotalItems:N0} in {_viewModel.GroupCount} {(_viewModel.IsHierarchical ? "regions" : "groups")}  |  Est. Value: {_viewModel.TotalValue:N0} ISK";
        }

        private void BuildTree()
        {
            if (_viewModel == null) return;
            AssetTree.Children.Clear();

            if (_viewModel.IsHierarchical)
                BuildHierarchicalTree();
            else
                BuildFlatTree();
        }

        private void BuildHierarchicalTree()
        {
            foreach (var region in _viewModel!.HierarchicalGroups)
            {
                // Region header (level 0)
                AssetTree.Children.Add(BuildChevronHeader(region, 0, region.SummaryText));

                // Systems panel (visible when region expanded)
                var systemsPanel = new StackPanel { Spacing = 0 };
                systemsPanel.Bind(IsVisibleProperty, new global::Avalonia.Data.Binding("IsExpanded") { Source = region });

                foreach (var system in region.Systems)
                {
                    // System header (level 1, indented)
                    systemsPanel.Children.Add(BuildChevronHeader(system, 1, ""));

                    // Stations panel
                    var stationsPanel = new StackPanel { Spacing = 0 };
                    stationsPanel.Bind(IsVisibleProperty, new global::Avalonia.Data.Binding("IsExpanded") { Source = system });

                    foreach (var station in system.Stations)
                    {
                        // Station header (level 2, indented more)
                        stationsPanel.Children.Add(BuildChevronHeader(station, 2, station.SummaryText));

                        // Items panel
                        var itemsPanel = new StackPanel { Spacing = 0 };
                        itemsPanel.Bind(IsVisibleProperty, new global::Avalonia.Data.Binding("IsExpanded") { Source = station });

                        foreach (var item in station.Items)
                            itemsPanel.Children.Add(BuildItemRow(item, 3));

                        stationsPanel.Children.Add(itemsPanel);
                    }

                    systemsPanel.Children.Add(stationsPanel);
                }

                AssetTree.Children.Add(systemsPanel);
            }
        }

        private void BuildFlatTree()
        {
            foreach (var group in _viewModel!.Groups)
            {
                AssetTree.Children.Add(BuildChevronHeader(group, 0, group.SummaryText));

                var itemsPanel = new StackPanel { Spacing = 0 };
                itemsPanel.Bind(IsVisibleProperty, new global::Avalonia.Data.Binding("IsExpanded") { Source = group });

                foreach (var item in group.Items)
                    itemsPanel.Children.Add(BuildItemRow(item, 1));

                AssetTree.Children.Add(itemsPanel);
            }
        }

        /// <summary>Builds a chevron group header at the given indent level.</summary>
        private Border BuildChevronHeader(INotifyPropertyChanged groupModel, int indent, string summary)
        {
            int leftPad = 8 + indent * 16;
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var chevron = new TextBlock
            {
                FontSize = 11, Width = 16,
                Foreground = TextSecBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            chevron.Bind(TextBlock.TextProperty, new global::Avalonia.Data.Binding("Chevron") { Source = groupModel });
            grid.Children.Add(chevron);

            var name = new TextBlock
            {
                FontSize = 11, FontWeight = FontWeight.SemiBold,
                Foreground = AccentBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                [Grid.ColumnProperty] = 1
            };
            name.Bind(TextBlock.TextProperty, new global::Avalonia.Data.Binding("Name") { Source = groupModel });
            grid.Children.Add(name);

            var count = new TextBlock
            {
                FontSize = 10, Foreground = TextSecBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
                [Grid.ColumnProperty] = 2
            };
            count.Bind(TextBlock.TextProperty, new global::Avalonia.Data.Binding("CountText") { Source = groupModel });
            grid.Children.Add(count);

            if (!string.IsNullOrEmpty(summary))
            {
                var val = new TextBlock
                {
                    FontSize = 10, Foreground = TextDimBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0),
                    [Grid.ColumnProperty] = 3
                };
                val.Bind(TextBlock.TextProperty, new global::Avalonia.Data.Binding("SummaryText") { Source = groupModel });
                grid.Children.Add(val);
            }

            var header = new Border
            {
                Classes = { "group-header" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(leftPad, 6, 8, 6),
                Child = grid
            };
            header.DataContext = groupModel;
            header.PointerPressed += OnGroupHeaderClicked;

            return header;
        }

        /// <summary>Builds an item row at the given indent level.</summary>
        private static Border BuildItemRow(AssetBrowserItemEntry item, int indent)
        {
            int leftPad = 8 + indent * 16;
            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            grid.Children.Add(new TextBlock
            {
                Text = item.ItemName,
                FontSize = 11, Foreground = TextPrimBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            grid.Children.Add(new TextBlock
            {
                Text = item.QuantityText,
                FontSize = 10, Foreground = TextSecBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
                MinWidth = 50, TextAlignment = TextAlignment.Right,
                [Grid.ColumnProperty] = 1
            });

            grid.Children.Add(new TextBlock
            {
                Text = item.ValueText,
                FontSize = 10, Foreground = GreenBrush,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80, TextAlignment = TextAlignment.Right,
                [Grid.ColumnProperty] = 2
            });

            return new Border
            {
                Classes = { "item-row" },
                Padding = new Thickness(leftPad, 3, 8, 3),
                Background = DarkBg,
                BorderBrush = BorderBr,
                BorderThickness = new Thickness(0, 0, 0, 0.5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Child = grid
            };
        }

        private static void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            switch (border.DataContext)
            {
                case AssetRegionGroup r: r.IsExpanded = !r.IsExpanded; break;
                case AssetSystemGroup s: s.IsExpanded = !s.IsExpanded; break;
                case AssetStationGroup st: st.IsExpanded = !st.IsExpanded; break;
                case AssetBrowserGroupEntry g: g.IsExpanded = !g.IsExpanded; break;
            }
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (_viewModel.IsHierarchical)
            {
                foreach (var r in _viewModel.HierarchicalGroups)
                {
                    r.IsExpanded = false;
                    foreach (var s in r.Systems) { s.IsExpanded = false; foreach (var st in s.Stations) st.IsExpanded = false; }
                }
            }
            else
            {
                foreach (var g in _viewModel.Groups) g.IsExpanded = false;
            }
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (_viewModel.IsHierarchical)
            {
                foreach (var r in _viewModel.HierarchicalGroups)
                {
                    r.IsExpanded = true;
                    foreach (var s in r.Systems) { s.IsExpanded = true; foreach (var st in s.Stations) st.IsExpanded = true; }
                }
            }
            else
            {
                foreach (var g in _viewModel.Groups) g.IsExpanded = true;
            }
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.GroupMode = GroupByCombo.SelectedIndex;
            LoadData();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.Filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.Filter);
            LoadData();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.Filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            LoadData();
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
    }
}
