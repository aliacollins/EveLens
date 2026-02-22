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
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
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

            _viewModel ??= new ContactsListViewModel();
            _viewModel.Character = character;
            _viewModel.Refresh();
            RebuildDisplay();
        }

        private void RebuildDisplay()
        {
            if (_viewModel == null) return;

            var grouped = _viewModel.GroupedItems;
            var displayGroups = new List<ContactGroupDisplay>();

            foreach (var g in grouped)
            {
                string name = string.IsNullOrEmpty(g.Key) ? "All Contacts" : g.Key;
                var contacts = g.Items.Select(c => new ContactDisplay(c)).ToList();
                if (contacts.Count > 0)
                    displayGroups.Add(new ContactGroupDisplay(name, contacts));
            }

            ContactGroupsList.ItemsSource = displayGroups;
            StatusText.Text = $"Contacts: {_viewModel.TotalItemCount}";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            int idx = GroupByCombo.SelectedIndex;
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

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (ContactGroupsList.ItemsSource is IEnumerable<ContactGroupDisplay> groups)
            {
                foreach (var g in groups)
                    g.IsExpanded = false;
                ContactGroupsList.ItemsSource = null;
                ContactGroupsList.ItemsSource = groups;
            }
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (ContactGroupsList.ItemsSource is IEnumerable<ContactGroupDisplay> groups)
            {
                foreach (var g in groups)
                    g.IsExpanded = true;
                ContactGroupsList.ItemsSource = null;
                ContactGroupsList.ItemsSource = groups;
            }
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is ContactGroupDisplay group)
                group.IsExpanded = !group.IsExpanded;
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.ContactList);
            LoadData();
        }
    }

    internal sealed class ContactGroupDisplay : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string CountText { get; }
        public List<ContactDisplay> Contacts { get; }

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

        public ContactGroupDisplay(string name, List<ContactDisplay> contacts)
        {
            Name = name;
            Contacts = contacts;
            CountText = $"{contacts.Count}";
        }
    }

    internal sealed class ContactDisplay
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        private readonly Contact _contact;

        public ContactDisplay(Contact contact) { _contact = contact; }

        public string Name => _contact.Name;
        public string StandingText => _contact.Standing.ToString("+0.00;-0.00;0.00");
        public string WatchlistText => _contact.IsInWatchlist ? "\u2605" : string.Empty;

        public IBrush StandingBrush
        {
            get
            {
                if (_contact.Standing > 0) return PositiveBrush;
                if (_contact.Standing < 0) return NegativeBrush;
                return NeutralBrush;
            }
        }

        public IBrush NameBrush
        {
            get
            {
                // Linear interpolation from standing -10 to +10 with softer color anchors
                double standing = _contact.Standing;

                // Clamp to -10/+10 range
                if (standing <= -10)
                    return new SolidColorBrush(Color.Parse("#FFAD3030")); // Muted dark red
                if (standing >= 10)
                    return new SolidColorBrush(Color.Parse("#FF2196F3")); // Brighter medium blue

                // Map standing to color gradient
                if (standing < -5)
                {
                    // -10 to -5: muted dark red to softer pink-red
                    double t = (standing + 10) / 5.0; // 0 at -10, 1 at -5
                    return InterpolateColor(
                        Color.Parse("#FFAD3030"),  // -10: muted dark red
                        Color.Parse("#FFCF6679"),  // -5: EVE error red (softer)
                        t);
                }
                else if (standing < 0)
                {
                    // -5 to 0: softer pink-red to neutral gray
                    double t = (standing + 5) / 5.0; // 0 at -5, 1 at 0
                    return InterpolateColor(
                        Color.Parse("#FFCF6679"),  // -5: EVE error red
                        Color.Parse("#FF8B949E"),  // 0: neutral light gray
                        t);
                }
                else if (standing <= 5)
                {
                    // 0 to +5: neutral gray to EVE standing blue
                    double t = standing / 5.0; // 0 at 0, 1 at +5
                    return InterpolateColor(
                        Color.Parse("#FF8B949E"),  // 0: neutral light gray
                        Color.Parse("#FF64B5F6"),  // +5: EVE standing blue
                        t);
                }
                else
                {
                    // +5 to +10: EVE standing blue to brighter medium blue
                    double t = (standing - 5) / 5.0; // 0 at +5, 1 at +10
                    return InterpolateColor(
                        Color.Parse("#FF64B5F6"),  // +5: EVE standing blue
                        Color.Parse("#FF2196F3"),  // +10: brighter medium blue
                        t);
                }
            }
        }

        private static IBrush InterpolateColor(Color c1, Color c2, double t)
        {
            byte r = (byte)(c1.R + (c2.R - c1.R) * t);
            byte g = (byte)(c1.G + (c2.G - c1.G) * t);
            byte b = (byte)(c1.B + (c2.B - c1.B) * t);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
