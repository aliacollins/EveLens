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
        private readonly List<ContactDisplayGroup> _displayGroups = new();

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
            _displayGroups.Clear();
            foreach (var g in groups)
            {
                var displayName = string.IsNullOrEmpty(g.Key) ? "All Contacts" : g.Key;
                _displayGroups.Add(new ContactDisplayGroup(displayName, g.Items));
            }

            ContactGroupsList.ItemsSource = null;
            ContactGroupsList.ItemsSource = _displayGroups;

            var isEmpty = _viewModel.TotalItemCount == 0;
            EmptyState.IsVisible = isEmpty;
            MainScroller.IsVisible = !isEmpty;

            // Trigger async portrait loading
            foreach (var group in _displayGroups)
                foreach (var entry in group.Items)
                    entry.LoadPortrait();

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

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            foreach (var g in _displayGroups)
                g.IsExpanded = false;
            ContactGroupsList.ItemsSource = null;
            ContactGroupsList.ItemsSource = _displayGroups;
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            foreach (var g in _displayGroups)
                g.IsExpanded = true;
            ContactGroupsList.ItemsSource = null;
            ContactGroupsList.ItemsSource = _displayGroups;
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.ContactList);
            LoadData();
        }
    }

    internal sealed class ContactDisplayGroup
    {
        public string Name { get; }
        public string CountText { get; }
        public bool IsExpanded { get; set; } = true;
        public List<ContactDisplayEntry> Items { get; }

        public ContactDisplayGroup(string name, IReadOnlyList<Contact> contacts)
        {
            Name = name;
            CountText = $"{contacts.Count} contacts";
            Items = contacts.Select(c => new ContactDisplayEntry(c)).ToList();
        }
    }

    internal sealed class ContactDisplayEntry : INotifyPropertyChanged
    {
        private static readonly IBrush PositiveBrush = new SolidColorBrush(Color.Parse("#FF64B5F6"));
        private static readonly IBrush NegativeBrush = new SolidColorBrush(Color.Parse("#FFCF6679"));
        private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        private readonly Contact _contact;
        private Bitmap? _portrait;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ContactDisplayEntry(Contact contact)
        {
            _contact = contact;
        }

        public string Name => _contact.Name;
        public string Initial => string.IsNullOrEmpty(_contact.Name) ? "?" : _contact.Name[..1].ToUpperInvariant();
        public double StandingValue => _contact.Standing;
        public string StandingText => _contact.Standing.ToString("+0.00;-0.00;0.00");
        public bool IsPositive => _contact.Standing > 0;
        public bool IsNegative => _contact.Standing < 0;
        public string WatchlistText => _contact.IsInWatchlist ? "\u2605" : string.Empty;
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

        public IBrush StandingBrush
        {
            get
            {
                if (_contact.Standing > 0) return PositiveBrush;
                if (_contact.Standing < 0) return NegativeBrush;
                return NeutralBrush;
            }
        }

        public void LoadPortrait()
        {
            // Try to convert the current image immediately
            var entityImage = _contact.EntityImage;
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
            _contact.ContactImageUpdated += OnContactImageUpdated;
        }

        private void OnContactImageUpdated(object? sender, EventArgs e)
        {
            var entityImage = _contact.EntityImage;
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
