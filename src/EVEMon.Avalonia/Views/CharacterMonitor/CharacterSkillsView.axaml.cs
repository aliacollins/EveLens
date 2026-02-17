using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterSkillsView : UserControl
    {
        private Character? _character;
        private bool _showAll;
        private string _filter = string.Empty;
        private List<SkillGroupEntry>? _allGroups;

        public CharacterSkillsView()
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

        private void LoadData()
        {
            Character? character = DataContext as Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = parent?.DataContext as Character;
            }
            if (character == null) return;

            _character = character;
            BuildGroupData();
            ApplyFilter();
        }

        private void BuildGroupData()
        {
            if (_character == null) return;

            _allGroups = _character.SkillGroups
                .Where(g => g.Any()) // only groups with skills
                .OrderBy(g => g.Name)
                .Select(g => new SkillGroupEntry(g))
                .ToList();
        }

        private void ApplyFilter()
        {
            if (_allGroups == null) return;

            int totalTrained = 0;
            int totalSkills = 0;
            long totalSP = 0;

            var visible = new List<SkillGroupEntry>();

            foreach (var group in _allGroups)
            {
                group.UpdateVisibility(_showAll, _filter);

                if (group.VisibleSkills.Count > 0)
                    visible.Add(group);

                totalTrained += group.TrainedCount;
                totalSkills += group.TotalCount;
                totalSP += group.TrainedSP;
            }

            SkillGroupsList.ItemsSource = visible;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Trained: {totalTrained} of {totalSkills} skills  |  Total SP: {totalSP:N0}";
        }

        private void OnToggleShowAll(object? sender, RoutedEventArgs e)
        {
            _showAll = ShowAllToggle.IsChecked == true;
            ShowAllToggle.Content = _showAll ? "All Skills" : "Trained Only";
            ApplyFilter();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            _filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_filter);
            ApplyFilter();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            FilterBox.Text = string.Empty;
            _filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            ApplyFilter();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (_allGroups == null) return;
            foreach (var g in _allGroups) g.IsExpanded = false;
            ApplyFilter(); // re-render
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (_allGroups == null) return;
            foreach (var g in _allGroups) g.IsExpanded = true;
            ApplyFilter(); // re-render
        }
    }

    /// <summary>View model for a skill group in the skills list.</summary>
    internal sealed class SkillGroupEntry
    {
        private static readonly IBrush TrainedColor = new SolidColorBrush(Color.Parse("#FFE0E0E0"));
        private static readonly IBrush UntrainedColor = new SolidColorBrush(Color.Parse("#FF555555"));
        private static readonly IBrush FilledBlock = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush EmptyBlock = new SolidColorBrush(Color.Parse("#FF2A2A4A"));
        private static readonly IBrush TrainingBlock = new SolidColorBrush(Color.Parse("#FF81C784"));

        private readonly List<SkillEntry> _allSkills;

        public string Name { get; }
        public int TotalCount { get; }
        public int TrainedCount { get; }
        public long TotalSP { get; }
        public long TrainedSP { get; }
        public bool IsExpanded { get; set; }
        public List<SkillEntry> VisibleSkills { get; private set; }

        public string TrainedCountText => $"{TrainedCount} / {TotalCount} skills";
        public string SPText => $"{TrainedSP:N0} SP";

        public SkillGroupEntry(SkillGroup group)
        {
            Name = group.Name;
            _allSkills = group.Select(s => new SkillEntry(s)).ToList();
            TotalCount = _allSkills.Count;
            TrainedCount = _allSkills.Count(s => s.IsKnown);
            TotalSP = group.TotalSP;
            TrainedSP = _allSkills.Where(s => s.IsKnown).Sum(s => s.SkillPoints);
            IsExpanded = TrainedCount > 0;
            VisibleSkills = _allSkills.Where(s => s.IsKnown).ToList();
        }

        public void UpdateVisibility(bool showAll, string filter)
        {
            var filtered = showAll ? _allSkills : _allSkills.Where(s => s.IsKnown);

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(s =>
                    s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

            VisibleSkills = filtered.ToList();
        }
    }

    /// <summary>View model for a single skill.</summary>
    internal sealed class SkillEntry
    {
        private static readonly IBrush TrainedColor = new SolidColorBrush(Color.Parse("#FFE0E0E0"));
        private static readonly IBrush UntrainedColor = new SolidColorBrush(Color.Parse("#FF555555"));
        private static readonly IBrush FilledBlock = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush EmptyBlock = new SolidColorBrush(Color.Parse("#FF2A2A4A"));
        private static readonly IBrush TrainingBlock = new SolidColorBrush(Color.Parse("#FF81C784"));

        public string Name { get; }
        public long Level { get; }
        public long SkillPoints { get; }
        public long Rank { get; }
        public bool IsKnown { get; }
        public bool IsTraining { get; }

        public string RankText => $"(Rank {Rank})";
        public string SPText => IsKnown ? $"{SkillPoints:N0} SP" : "";
        public IBrush NameColor => IsKnown ? TrainedColor : UntrainedColor;

        // Level blocks: filled (gold), training (green), empty (dark)
        public IBrush Block1Color => GetBlockColor(1);
        public IBrush Block2Color => GetBlockColor(2);
        public IBrush Block3Color => GetBlockColor(3);
        public IBrush Block4Color => GetBlockColor(4);
        public IBrush Block5Color => GetBlockColor(5);

        public SkillEntry(Skill skill)
        {
            Name = skill.Name;
            Level = skill.LastConfirmedLvl;
            SkillPoints = skill.SkillPoints;
            Rank = skill.Rank;
            IsKnown = skill.IsKnown;
            IsTraining = skill.IsTraining;
        }

        private IBrush GetBlockColor(int blockLevel)
        {
            if (blockLevel <= Level) return FilledBlock;
            if (IsTraining && blockLevel == Level + 1) return TrainingBlock;
            return EmptyBlock;
        }
    }
}
