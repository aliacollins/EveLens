// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterSkillQueueView : UserControl
    {
        private SkillQueueViewModel? _viewModel;
        private IDisposable? _queueUpdatedSub;
        private long _characterId;

        public CharacterSkillQueueView()
        {
            InitializeComponent();
            ExportCsvBtn.Content = Loc.Get("Queue.ExportCsv");
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // Subscribe to skill queue updates so we refresh when the scheduler fetches new data
            _queueUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterSkillQueueUpdatedEvent>(OnSkillQueueUpdated);

            LoadData();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _queueUpdatedSub?.Dispose();
            _queueUpdatedSub = null;
            _viewModel?.Dispose();
            _viewModel = null;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            LoadData();
        }

        private void OnSkillQueueUpdated(CharacterSkillQueueUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
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
            if (character is not CCPCharacter) return;

            _characterId = character.CharacterID;
            _viewModel ??= new SkillQueueViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            // Wrap VM entries with display entries for AXAML color binding
            var displayEntries = _viewModel.QueueEntries
                .Select(e => new QueueDisplayEntry(e))
                .ToList();

            QueueItems.ItemsSource = displayEntries;

            // Toggle empty state vs queue list
            var emptyState = this.FindControl<UserControl>("EmptyState");
            var scroller = this.FindControl<ScrollViewer>("QueueScroller");
            bool isEmpty = _viewModel.QueueEntries.Count == 0;
            if (emptyState != null) emptyState.IsVisible = isEmpty;
            if (scroller != null) scroller.IsVisible = !isEmpty;

            var ccp = character as CCPCharacter;
            var statusParts = new System.Collections.Generic.List<string>();

            if (isEmpty)
            {
                StatusText.Text = Loc.Get("Queue.NoSkillsInTraining");
                return;
            }

            statusParts.Add($"{Loc.Get("Queue.SkillsInQueue")}: {_viewModel.QueueEntries.Count}");

            if (_viewModel.TrainingCount > 0 && ccp?.SkillQueue != null && ccp.IsTraining)
            {
                // Queue end countdown
                var endTime = ccp.SkillQueue.EndTime;
                var remaining = endTime - DateTime.UtcNow;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

                string countdown;
                if (remaining.TotalDays >= 1)
                    countdown = $"{(int)remaining.TotalDays}d {remaining.Hours}h {remaining.Minutes}m";
                else if (remaining.TotalHours >= 1)
                    countdown = $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
                else
                    countdown = $"{remaining.Minutes}m {remaining.Seconds}s";

                statusParts.Add($"{Loc.Get("Queue.EndsIn")}: {countdown}");
                statusParts.Add($"{Loc.Get("Queue.EndsOn")}: {endTime:ddd dd MMM HH:mm} EVE");
            }
            else
            {
                statusParts.Add(Loc.Get("Status.Paused"));
            }

            StatusText.Text = string.Join("  |  ", statusParts);
        }

        private async void OnExportCsv(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_viewModel == null || !_viewModel.HasEntries) return;

            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var result = await topLevel.StorageProvider.SaveFilePickerAsync(
                    new global::Avalonia.Platform.Storage.FilePickerSaveOptions
                    {
                        Title = "Export Queue to CSV",
                        SuggestedFileName = "skill_queue.csv",
                        FileTypeChoices = new[]
                        {
                            new global::Avalonia.Platform.Storage.FilePickerFileType("CSV Files")
                                { Patterns = new[] { "*.csv" } },
                        }
                    });

                if (result == null) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Skill,Rank,Status,Time,Progress %");

                foreach (var entry in _viewModel.QueueEntries)
                {
                    string time = entry.TimeText.Replace(",", " ");
                    sb.AppendLine($"\"{entry.SkillText}\",\"{entry.RankText}\",\"{entry.StatusText}\",\"{time}\",{entry.Progress:F1}");
                }

                await System.IO.File.WriteAllTextAsync(result.Path.LocalPath, sb.ToString());
            }
            catch { }
        }
    }

    /// <summary>Avalonia display wrapper for queued skill with IBrush color properties.</summary>
    internal sealed class QueueDisplayEntry
    {
        private static readonly IBrush TrainingBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush CompletedBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush PendingBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));
        private static readonly IBrush TrainingBg = new SolidColorBrush(Color.Parse("#FF1A2A2E"));
        private static readonly IBrush NormalBg = new SolidColorBrush(Color.Parse("#FF1A1A2E"));

        public SkillQueueEntry Data { get; }

        // Delegate all data properties
        public string SkillText => Data.SkillText;
        public string RankText => Data.RankText;
        public string StatusText => Data.StatusText;
        public string TimeText => Data.TimeText;
        public double Progress => Data.Progress;
        public bool IsTraining => Data.IsTraining;

        // Avalonia-specific color properties
        public IBrush NameBrush { get; }
        public IBrush StatusBrush { get; }
        public IBrush ProgressBrush { get; }
        public IBrush RowBackground { get; }

        public QueueDisplayEntry(SkillQueueEntry data)
        {
            Data = data;

            if (data.IsCompleted)
            {
                StatusBrush = CompletedBrush;
                NameBrush = CompletedBrush;
                ProgressBrush = CompletedBrush;
                RowBackground = NormalBg;
            }
            else if (data.IsTraining)
            {
                StatusBrush = TrainingBrush;
                NameBrush = TrainingBrush;
                ProgressBrush = TrainingBrush;
                RowBackground = TrainingBg;
            }
            else
            {
                StatusBrush = PendingBrush;
                NameBrush = PendingBrush;
                ProgressBrush = new SolidColorBrush(Color.Parse("#FF0F3460"));
                RowBackground = NormalBg;
            }
        }
    }
}
