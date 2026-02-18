using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using Avalonia.Interactivity;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Events;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterLoyaltyView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        public CharacterLoyaltyView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterLoyaltyPointsUpdatedEvent>(OnDataUpdated);
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
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character is not CCPCharacter ccp) return;

            _characterId = character.CharacterID;

            // Check if on-demand endpoint is enabled
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.LoyaltyPoints))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            var entries = ccp.LoyaltyPoints
                .OrderByDescending(lp => lp.LoyaltyPoints)
                .Select(lp => new LoyaltyDisplayEntry(lp))
                .ToList();

            var emptyState = this.FindControl<UserControl>("EmptyState");
            var scroller = this.FindControl<ScrollViewer>("LoyaltyScroller");

            if (entries.Count == 0)
            {
                if (emptyState != null) emptyState.IsVisible = true;
                if (scroller != null) scroller.IsVisible = false;
            }
            else
            {
                if (emptyState != null) emptyState.IsVisible = false;
                if (scroller != null) scroller.IsVisible = true;
            }

            var list = this.FindControl<ItemsControl>("LoyaltyList");
            if (list != null)
                list.ItemsSource = entries;

            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
                status.Text = $"Corporations: {entries.Count}";
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.LoyaltyPoints);
            LoadData();
        }

        private void OnDataUpdated(CharacterLoyaltyPointsUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }

    internal sealed class LoyaltyDisplayEntry
    {
        public string CorporationName { get; }
        public string PointsText { get; }
        public string Initial { get; }

        public LoyaltyDisplayEntry(Loyalty loyalty)
        {
            CorporationName = loyalty.CorporationName ?? string.Empty;
            PointsText = loyalty.LoyaltyPoints.ToString("N0");
            Initial = !string.IsNullOrEmpty(CorporationName) ? CorporationName[0].ToString() : "?";
        }
    }
}
