// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using EVEMon.Common.ViewModels.Lists;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterStandingsView : UserControl
    {
        private StandingsListViewModel? _viewModel;
        private readonly List<StandingDisplayGroup> _displayGroups = new();

        public CharacterStandingsView()
        {
            InitializeComponent();
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
            _viewModel?.Dispose();
            _viewModel = null;
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

            // Check if on-demand endpoint is enabled
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

            _viewModel ??= new StandingsListViewModel();
            _viewModel.Character = character;
            _viewModel.Refresh();
            RebuildDisplay();
        }

        private void RebuildDisplay()
        {
            if (_viewModel == null) return;

            var groups = _viewModel.GroupedItems;
            _displayGroups.Clear();
            foreach (var g in groups)
            {
                var displayName = string.IsNullOrEmpty(g.Key) ? "All Standings" : g.Key;
                _displayGroups.Add(new StandingDisplayGroup(displayName, g.Items));
            }

            StandingGroupsList.ItemsSource = null;
            StandingGroupsList.ItemsSource = _displayGroups;

            var isEmpty = _viewModel.TotalItemCount == 0;
            EmptyState.IsVisible = isEmpty;
            MainScroller.IsVisible = !isEmpty;

            // Trigger async portrait loading
            foreach (var group in _displayGroups)
                foreach (var entry in group.Items)
                    entry.LoadPortrait();

            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
                status.Text = $"Standings: {_viewModel.TotalItemCount}";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            var idx = GroupByCombo.SelectedIndex;
            if (idx < 0) return;
            _viewModel.Grouping = (StandingGrouping)idx;
            RebuildDisplay();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.TextFilter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.TextFilter);
            RebuildDisplay();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.TextFilter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            RebuildDisplay();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            foreach (var g in _displayGroups)
                g.IsExpanded = false;
            StandingGroupsList.ItemsSource = null;
            StandingGroupsList.ItemsSource = _displayGroups;
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            foreach (var g in _displayGroups)
                g.IsExpanded = true;
            StandingGroupsList.ItemsSource = null;
            StandingGroupsList.ItemsSource = _displayGroups;
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is StandingDisplayGroup group)
                group.IsExpanded = !group.IsExpanded;
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.Standings);
            LoadData();
        }
    }

    internal sealed class StandingDisplayGroup : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string CountText { get; }
        public List<StandingDisplayEntry> Items { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
            }
        }

        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";

        public event PropertyChangedEventHandler? PropertyChanged;

        public StandingDisplayGroup(string name, IReadOnlyList<Standing> standings)
        {
            Name = name;
            CountText = $"{standings.Count} standings";
            Items = standings.Select(s => new StandingDisplayEntry(s)).ToList();
        }
    }

    internal sealed class StandingDisplayEntry : INotifyPropertyChanged
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));
        private static readonly IBrush ExcellentBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush GoodBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush BadBrush = new SolidColorBrush(Color.Parse("#FFFFD54F"));
        private static readonly IBrush TerribleBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));

        private readonly Standing _standing;
        private Bitmap? _portrait;

        public event PropertyChangedEventHandler? PropertyChanged;

        public StandingDisplayEntry(Standing standing)
        {
            _standing = standing;
        }

        public string EntityName => _standing.EntityName;
        public string Initial => string.IsNullOrEmpty(_standing.EntityName) ? "?" : _standing.EntityName[..1].ToUpperInvariant();
        public double StandingNumeric => _standing.StandingValue;
        public string StandingText => _standing.StandingValue.ToString("+0.00;-0.00;0.00");
        public string EffectiveText => $"({_standing.EffectiveStanding:+0.00;-0.00;0.00})";
        public bool IsPositive => _standing.StandingValue > 0;
        public bool IsNegative => _standing.StandingValue < 0;
        public bool HasPortrait => _portrait != null;

        public Bitmap? Portrait
        {
            get => _portrait;
            private set
            {
                if (_portrait != value)
                {
                    _portrait = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPortrait));
                }
            }
        }

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
            // Try to convert the current image immediately
            var entityImage = _standing.EntityImage;
            if (entityImage != null)
            {
                var bmp = DrawingImageToAvaloniaConverter.Instance.Convert(
                    entityImage, typeof(Bitmap), null, CultureInfo.InvariantCulture) as Bitmap;
                if (bmp != null)
                {
                    Portrait = bmp;
                    return;
                }
            }

            // Subscribe for async image updates
            _standing.StandingImageUpdated += OnStandingImageUpdated;
        }

        private void OnStandingImageUpdated(object? sender, EventArgs e)
        {
            var entityImage = _standing.EntityImage;
            if (entityImage == null) return;

            var bmp = DrawingImageToAvaloniaConverter.Instance.Convert(
                entityImage, typeof(Bitmap), null, CultureInfo.InvariantCulture) as Bitmap;

            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Portrait = bmp;
            });
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
