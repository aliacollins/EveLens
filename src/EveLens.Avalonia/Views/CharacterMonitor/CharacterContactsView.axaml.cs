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
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Models;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;

using EveLens.Common.ViewModels.Lists;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterContactsView : UserControl
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        private ContactsListViewModel? _viewModel;
        private FlattenedTreeSource<object>? _treeSource;
        private bool _isRebuilding;
        private long _characterId;

        public CharacterContactsView()
        {
            InitializeComponent();
            ContactItemsControl.ItemTemplate = CreateNodeTemplate();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
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
            if (character == null) return;

            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var scopePrompt = this.FindControl<Border>("ScopePrompt");
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.ContactList))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.ContactList))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _characterId = character.CharacterID;
            _viewModel ??= new ContactsListViewModel();
            _viewModel.Character = character;
            _viewModel.Refresh();

            bool firstLoad = _treeSource == null;
            if (_treeSource == null)
            {
                _treeSource = new FlattenedTreeSource<object>();
                _treeSource.Changed += OnTreeChanged;
            }

            // Load persisted expand state (contacts default to all expanded)
            var expandState = CollapseStateHelper.LoadExpandState(_characterId, "Contacts");
            _treeSource.SetExpandState(expandState);

            _isRebuilding = true;
            PopulateTree();
            if (!CollapseStateHelper.HasSavedState(_characterId, "Contacts")) _treeSource.ExpandAll();
            _isRebuilding = false;
            UpdateItemsSource();
            UpdateStatus();
        }

        private void PopulateTree()
        {
            if (_viewModel == null || _treeSource == null) return;

            var grouped = _viewModel.GroupedItems;
            var groups = new List<GroupData<object>>();

            foreach (var g in grouped)
            {
                string name = string.IsNullOrEmpty(g.Key) ? "All Contacts" : g.Key;
                if (g.Items.Count > 0)
                {
                    groups.Add(new GroupData<object>(
                        name,
                        new ContactGroupInfo(name, g.Items.Count),
                        g.Items.Cast<object>().ToList()));
                }
            }

            _treeSource.SetData(groups);
        }

        private void OnTreeChanged()
        {
            if (!_isRebuilding && _characterId != 0 && _treeSource != null)
                CollapseStateHelper.SaveExpandState(_characterId, "Contacts", _treeSource.GetExpandState());

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                UpdateItemsSource();
                UpdateStatus();
            });
        }

        private void UpdateItemsSource()
        {
            if (ContactItemsControl.ItemsSource != _treeSource)
                ContactItemsControl.ItemsSource = _treeSource;
        }

        private void UpdateStatus()
        {
            StatusText.Text = $"Contacts: {_viewModel?.TotalItemCount ?? 0}";
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
                return BuildContactRow(node);
            });
        }

        private Control BuildGroupHeader(FlatTreeNode<object> node)
        {
            var groupInfo = node.Data as ContactGroupInfo;
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

        private Control BuildContactRow(FlatTreeNode<object> node)
        {
            var contact = node.Data as Contact;
            if (contact == null)
                return new Border();

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Contact name with standing-based color
            var nameTb = new TextBlock
            {
                Text = contact.Name,
                FontSize = FontScaleService.Body,
                Foreground = GetNameBrush(contact.Standing),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameTb, 0);
            grid.Children.Add(nameTb);

            // Standing value
            var standingTb = new TextBlock
            {
                Text = contact.Standing.ToString("+0.00;-0.00;0.00"),
                FontSize = FontScaleService.Body,
                Foreground = GetStandingBrush(contact.Standing),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 50,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(8, 0)
            };
            Grid.SetColumn(standingTb, 1);
            grid.Children.Add(standingTb);

            // Watchlist star
            var watchlistTb = new TextBlock
            {
                Text = contact.IsInWatchlist ? "\u2605" : string.Empty,
                FontSize = FontScaleService.Body,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 20,
                TextAlignment = TextAlignment.Center
            };
            watchlistTb.Foreground = FindBrush("EveWarningYellowBrush");
            Grid.SetColumn(watchlistTb, 2);
            grid.Children.Add(watchlistTb);

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

        private static IBrush GetStandingBrush(double standing)
        {
            if (standing > 0) return PositiveBrush;
            if (standing < 0) return NegativeBrush;
            return NeutralBrush;
        }

        private static IBrush GetNameBrush(double standing)
        {
            if (standing <= -10)
                return new SolidColorBrush(Color.Parse("#FFAD3030"));
            if (standing >= 10)
                return new SolidColorBrush(Color.Parse("#FF2196F3"));

            if (standing < -5)
            {
                double t = (standing + 10) / 5.0;
                return InterpolateColor(
                    Color.Parse("#FFAD3030"),
                    Color.Parse("#FFCF6679"),
                    t);
            }
            else if (standing < 0)
            {
                double t = (standing + 5) / 5.0;
                return InterpolateColor(
                    Color.Parse("#FFCF6679"),
                    Color.Parse("#FF8B949E"),
                    t);
            }
            else if (standing <= 5)
            {
                double t = standing / 5.0;
                return InterpolateColor(
                    Color.Parse("#FF8B949E"),
                    Color.Parse("#FF64B5F6"),
                    t);
            }
            else
            {
                double t = (standing - 5) / 5.0;
                return InterpolateColor(
                    Color.Parse("#FF64B5F6"),
                    Color.Parse("#FF2196F3"),
                    t);
            }
        }

        private static IBrush InterpolateColor(Color c1, Color c2, double t)
        {
            byte r = (byte)(c1.R + (c2.R - c1.R) * t);
            byte g = (byte)(c1.G + (c2.G - c1.G) * t);
            byte b = (byte)(c1.B + (c2.B - c1.B) * t);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
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
            int idx = GroupByCombo.SelectedIndex;
            if (idx < 0) return;
            _viewModel.Grouping = (ContactGrouping)idx;
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
            oc?.EnableEndpoint(ESIAPICharacterMethods.ContactList);
            LoadData();
        }

        #endregion
    }

    internal sealed class ContactGroupInfo
    {
        public string Name { get; }
        public string CountText { get; }

        public ContactGroupInfo(string name, int count)
        {
            Name = name;
            CountText = $"{count}";
        }
    }
}
