using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

            _characterId = character.CharacterID;
            _viewModel ??= new SkillBrowserViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            SkillGroupsList.ItemsSource = _viewModel.VisibleGroups
                .Select(g => new SkillGroupDisplay(g))
                .ToList();

            StatusText.Text = $"Trained: {_viewModel.TotalTrained} of {_viewModel.TotalSkills} skills  |  Total SP: {_viewModel.TotalSP:N0}";
        }

        private void OnDataUpdated(CharacterUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is SkillGroupDisplay group)
            {
                group.IsExpanded = !group.IsExpanded;
            }
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
            if (SkillGroupsList.ItemsSource is not IEnumerable<SkillGroupDisplay> groups) return;
            foreach (var g in groups) g.IsExpanded = false;
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (SkillGroupsList.ItemsSource is not IEnumerable<SkillGroupDisplay> groups) return;
            foreach (var g in groups) g.IsExpanded = true;
        }
    }

    /// <summary>
    /// Display model for a skill group — compact chevron header with INPC for expand/collapse.
    /// </summary>
    internal sealed class SkillGroupDisplay : INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; }
        public string CountText { get; }
        public string SPText { get; }
        public List<SkillDisplay> Skills { get; }

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

        /// <summary>▸ collapsed, ▾ expanded — same as plan editor sidebar.</summary>
        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";

        public event PropertyChangedEventHandler? PropertyChanged;

        public SkillGroupDisplay(SkillBrowserGroupEntry group)
        {
            Name = group.Name;
            CountText = $"{group.TrainedCount} / {group.TotalCount}";
            SPText = FormatSP(group.TrainedSP);
            _isExpanded = group.IsExpanded;
            Skills = group.VisibleSkills.Select(s => new SkillDisplay(s)).ToList();
        }

        private static string FormatSP(long sp)
        {
            if (sp >= 1_000_000) return $"{sp / 1_000_000.0:N1}M SP";
            if (sp >= 1_000) return $"{sp / 1_000.0:N0}K SP";
            if (sp > 0) return $"{sp:N0} SP";
            return "";
        }
    }

    /// <summary>
    /// Display model for a single skill row.
    /// </summary>
    internal sealed class SkillDisplay
    {
        private static readonly IBrush FilledBlock = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush EmptyBlock = new SolidColorBrush(Color.Parse("#FF2A2A4A"));
        private static readonly IBrush TrainingBlock = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush TrainedColor = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush UntrainedColor = new SolidColorBrush(Color.Parse("#FF505060"));
        private static readonly IBrush TrainingColor = new SolidColorBrush(Color.Parse("#FF81C784"));

        public string Name { get; }
        public string RankText { get; }
        public string SPText { get; }
        public IBrush NameBrush { get; }
        public IBrush Block1 { get; }
        public IBrush Block2 { get; }
        public IBrush Block3 { get; }
        public IBrush Block4 { get; }
        public IBrush Block5 { get; }

        public SkillDisplay(SkillBrowserSkillEntry skill)
        {
            Name = skill.Name;
            RankText = skill.RankText;
            SPText = skill.SPText;
            NameBrush = skill.IsTraining ? TrainingColor
                : skill.IsKnown ? TrainedColor : UntrainedColor;
            Block1 = BlockColor(1, skill);
            Block2 = BlockColor(2, skill);
            Block3 = BlockColor(3, skill);
            Block4 = BlockColor(4, skill);
            Block5 = BlockColor(5, skill);
        }

        private static IBrush BlockColor(int lvl, SkillBrowserSkillEntry s)
        {
            if (lvl <= s.Level) return FilledBlock;
            if (s.IsTraining && lvl == s.Level + 1) return TrainingBlock;
            return EmptyBlock;
        }
    }
}
