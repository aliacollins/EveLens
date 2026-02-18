using System;
using System.Collections.Generic;
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
    public partial class CharacterMedalsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        public CharacterMedalsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterMedalsUpdatedEvent>(OnDataUpdated);
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
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.Medals))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            var items = ccp.CharacterMedals.ToList();
            var entries = items.Select(m => new MedalDisplayEntry(m)).ToList();

            var emptyState = this.FindControl<UserControl>("EmptyState");
            var scroller = this.FindControl<ScrollViewer>("MedalScroller");

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

            var list = this.FindControl<ItemsControl>("MedalsList");
            if (list != null)
                list.ItemsSource = entries;

            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
                status.Text = $"Medals: {entries.Count}";
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.Medals);
            LoadData();
        }

        private void OnDataUpdated(CharacterMedalsUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }

    internal sealed class MedalDisplayEntry
    {
        public string Title { get; }
        public string Reason { get; }
        public bool HasReason { get; }
        public string IssuedText { get; }
        public string IssuerCorpText { get; }

        public MedalDisplayEntry(Medal medal)
        {
            Title = !string.IsNullOrEmpty(medal.Title) ? medal.Title : "(Untitled Medal)";
            Reason = medal.Reason ?? string.Empty;
            HasReason = !string.IsNullOrWhiteSpace(medal.Reason);
            IssuedText = medal.Issued.ToString("yyyy-MM-dd");

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(medal.Issuer))
                parts.Add($"Issuer: {medal.Issuer}");
            if (!string.IsNullOrWhiteSpace(medal.CorporationName))
                parts.Add($"Corp: {medal.CorporationName}");
            IssuerCorpText = parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
        }
    }
}
