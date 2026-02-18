using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Events;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterSkillsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private SkillBrowserViewModel? _viewModel;

        public CharacterSkillsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterUpdatedEvent>(OnDataUpdated);
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
            if (character == null) return;

            _characterId = character.CharacterID;
            _viewModel ??= new SkillBrowserViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            // Wrap VM groups with display entries for AXAML color binding
            var displayGroups = _viewModel.VisibleGroups
                .Select(g => new SkillDisplayGroup(g))
                .ToList();

            SkillGroupsList.ItemsSource = displayGroups;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Trained: {_viewModel.TotalTrained} of {_viewModel.TotalSkills} skills  |  Total SP: {_viewModel.TotalSP:N0}";
        }


        private void OnDataUpdated(CharacterUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnToggleShowAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ShowAll = ShowAllToggle.IsChecked == true;
            ShowAllToggle.Content = _viewModel.ShowAll ? "All Skills" : "Trained Only";
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

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.CollapseAll();
            LoadData();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.ExpandAll();
            LoadData();
        }
    }

    /// <summary>Avalonia display wrapper for skill group with color properties.</summary>
    internal sealed class SkillDisplayGroup
    {
        public SkillBrowserGroupEntry Data { get; }
        public string Name => Data.Name;
        public string TrainedCountText => Data.TrainedCountText;
        public string SPText => Data.SPText;
        public bool IsExpanded => Data.IsExpanded;
        public List<SkillDisplayEntry> VisibleSkills { get; }

        public SkillDisplayGroup(SkillBrowserGroupEntry data)
        {
            Data = data;
            VisibleSkills = data.VisibleSkills.Select(s => new SkillDisplayEntry(s)).ToList();
        }
    }

    /// <summary>Avalonia display wrapper for skill with IBrush color properties.</summary>
    internal sealed class SkillDisplayEntry
    {
        private static readonly IBrush FilledBlock = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush EmptyBlock = new SolidColorBrush(Color.Parse("#FF2A2A4A"));
        private static readonly IBrush TrainingBlock = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush TrainedColor = new SolidColorBrush(Color.Parse("#FFE0E0E0"));
        private static readonly IBrush UntrainedColor = new SolidColorBrush(Color.Parse("#FF555555"));

        public SkillBrowserSkillEntry Data { get; }

        // Delegate all data properties
        public string Name => Data.Name;
        public string RankText => Data.RankText;
        public string SPText => Data.SPText;

        // Avalonia-specific color properties
        public IBrush NameColor => Data.IsKnown ? TrainedColor : UntrainedColor;
        public IBrush Block1Color => GetBlockColor(1);
        public IBrush Block2Color => GetBlockColor(2);
        public IBrush Block3Color => GetBlockColor(3);
        public IBrush Block4Color => GetBlockColor(4);
        public IBrush Block5Color => GetBlockColor(5);

        public SkillDisplayEntry(SkillBrowserSkillEntry data)
        {
            Data = data;
        }

        private IBrush GetBlockColor(int lvl)
        {
            if (lvl <= Data.Level) return FilledBlock;
            if (Data.IsTraining && lvl == Data.Level + 1) return TrainingBlock;
            return EmptyBlock;
        }
    }
}
