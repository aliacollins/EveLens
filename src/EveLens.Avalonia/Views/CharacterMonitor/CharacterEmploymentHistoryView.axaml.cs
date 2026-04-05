// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common.Models;
using EveLens.Common.ViewModels;
using Avalonia.Interactivity;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Events;
using EveLens.Common;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterEmploymentHistoryView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private EmploymentTimelineViewModel? _viewModel;
        private bool _isPanning;
        private Point _panStart;
        private double _panStartOffset;

        public CharacterEmploymentHistoryView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterUpdatedEvent>(OnDataUpdated);
            TimelineScroller.PointerPressed += OnScrollerPointerPressed;
            TimelineScroller.PointerMoved += OnScrollerPointerMoved;
            TimelineScroller.PointerReleased += OnScrollerPointerReleased;
            TimelineScroller.PointerWheelChanged += OnScrollerPointerWheel;
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
            TimelineScroller.PointerPressed -= OnScrollerPointerPressed;
            TimelineScroller.PointerMoved -= OnScrollerPointerMoved;
            TimelineScroller.PointerReleased -= OnScrollerPointerReleased;
            TimelineScroller.PointerWheelChanged -= OnScrollerPointerWheel;
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;
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

            _characterId = character.CharacterID;

            // Check if on-demand endpoint is enabled
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var scopePrompt = this.FindControl<Border>("ScopePrompt");
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.EmploymentHistory))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.EmploymentHistory))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new EmploymentTimelineViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            var timeline = _viewModel.TimelineEntries;

            TimelineItems.ItemsSource = timeline;
            DateLabels.ItemsSource = timeline;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Corporations: {_viewModel.CorporationCount}";

            // Load corp logos after rendering
            global::Avalonia.Threading.Dispatcher.UIThread.Post(
                () => LoadCorpLogos(timeline),
                global::Avalonia.Threading.DispatcherPriority.Background);

            // Restore saved view preference
            if (Settings.UI.EmploymentHistoryListView)
                ApplyViewMode(true);
        }

        private void LoadCorpLogos(System.Collections.Generic.List<EmploymentTimelineEntry> timeline)
        {
            try
            {
                // Find Image controls in the timeline cards
                var images = TimelineItems.GetVisualDescendants().OfType<Image>().ToList();

                for (int i = 0; i < Math.Min(images.Count, timeline.Count); i++)
                {
                    var entry = timeline[i];
                    var img = images[i];

                    // CorporationImage triggers async download, returns System.Drawing.Image
                    var corpImage = entry.Record.CorporationImage;
                    if (corpImage != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            corpImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                        {
                            (img.Source as IDisposable)?.Dispose();
                            img.Source = bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Corp logo load error: {ex}");
            }
        }

        private void OnScrollerPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(TimelineScroller).Properties;
            if (props.IsLeftButtonPressed || props.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _panStart = e.GetPosition(TimelineScroller);
                _panStartOffset = TimelineScroller.Offset.X;
                e.Pointer.Capture(TimelineScroller);
                e.Handled = true;
            }
        }

        private void OnScrollerPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning) return;
            var current = e.GetPosition(TimelineScroller);
            var delta = _panStart.X - current.X;
            TimelineScroller.Offset = new Vector(
                Math.Max(0, Math.Min(_panStartOffset + delta, TimelineScroller.Extent.Width - TimelineScroller.Viewport.Width)),
                0);
            e.Handled = true;
        }

        private void OnScrollerPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isPanning) return;
            _isPanning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        private void OnScrollerPointerWheel(object? sender, PointerWheelEventArgs e)
        {
            // Convert vertical wheel to horizontal scroll
            var scrollAmount = -e.Delta.Y * 48;
            var newOffset = TimelineScroller.Offset.X + scrollAmount;
            newOffset = Math.Max(0, Math.Min(newOffset, TimelineScroller.Extent.Width - TimelineScroller.Viewport.Width));
            TimelineScroller.Offset = new Vector(newOffset, 0);
            e.Handled = true;
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.EmploymentHistory);
            LoadData();
        }

        private void OnViewToggle(object? sender, RoutedEventArgs e)
        {
            bool showList = sender == ListViewBtn;
            ApplyViewMode(showList);
        }

        private void ApplyViewMode(bool showList)
        {
            TimelineViewBtn.IsChecked = !showList;
            ListViewBtn.IsChecked = showList;
            TimelineScroller.IsVisible = !showList;
            ListItems.IsVisible = showList;

            // Save preference
            Settings.UI.EmploymentHistoryListView = showList;

            if (showList)
            {
                ListItems.ItemsSource = TimelineItems.ItemsSource;
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (TimelineItems.ItemsSource is System.Collections.Generic.List<EmploymentTimelineEntry> timeline)
                        LoadListCorpLogos(timeline);
                }, global::Avalonia.Threading.DispatcherPriority.Render);
            }
        }

        private void LoadListCorpLogos(System.Collections.Generic.List<EmploymentTimelineEntry> timeline)
        {
            try
            {
                var images = ListItems.GetVisualDescendants().OfType<Image>().ToList();
                for (int i = 0; i < Math.Min(images.Count, timeline.Count); i++)
                {
                    var entry = timeline[i];
                    var img = images[i];

                    var corpImage = entry.Record.CorporationImage;
                    if (corpImage != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            corpImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                        {
                            (img.Source as IDisposable)?.Dispose();
                            img.Source = bitmap;
                        }
                    }
                }
            }
            catch { }
        }

        private void OnDataUpdated(CharacterUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }
}
