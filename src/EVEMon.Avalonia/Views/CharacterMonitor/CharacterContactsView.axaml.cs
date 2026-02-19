using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using EVEMon.Common.ViewModels.Lists;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterContactsView : UserControl
    {
        private ContactsListViewModel? _viewModel;

        public CharacterContactsView()
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
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.ContactList))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new ContactsListViewModel();
            _viewModel.Character = character;
            _viewModel.Refresh();
            RebuildDisplay();
        }

        private void RebuildDisplay()
        {
            if (_viewModel == null) return;

            var groups = _viewModel.GroupedItems;

            // Flatten all groups into a single list for the DataGrid (Law 20: .ToList())
            var flatItems = new List<ContactDisplayEntry>();
            foreach (var g in groups)
            {
                string groupName = string.IsNullOrEmpty(g.Key) ? "All Contacts" : g.Key;
                foreach (var contact in g.Items)
                    flatItems.Add(new ContactDisplayEntry(contact, groupName));
            }

            ContactsGrid.ItemsSource = flatItems;

            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
                status.Text = $"Contacts: {_viewModel.TotalItemCount}";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            var idx = GroupByCombo.SelectedIndex;
            if (idx < 0) return;
            _viewModel.Grouping = (ContactGrouping)idx;
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

        private void OnCopyName(object? sender, RoutedEventArgs e)
        {
            var item = ContactsGrid.SelectedItem as ContactDisplayEntry;
            if (item != null)
                TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(item.Name);
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.ContactList);
            LoadData();
        }
    }

    internal sealed class ContactDisplayEntry
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        private readonly Contact _contact;

        public ContactDisplayEntry(Contact contact, string groupName)
        {
            _contact = contact;
            GroupName = groupName;
        }

        public string Name => _contact.Name;
        public double StandingValue => _contact.Standing;
        public string StandingText => _contact.Standing.ToString("+0.00;-0.00;0.00");
        public string WatchlistText => _contact.IsInWatchlist ? "\u2605" : string.Empty;
        public string GroupName { get; }

        public IBrush StandingBrush
        {
            get
            {
                if (_contact.Standing > 0) return PositiveBrush;
                if (_contact.Standing < 0) return NegativeBrush;
                return NeutralBrush;
            }
        }
    }
}
