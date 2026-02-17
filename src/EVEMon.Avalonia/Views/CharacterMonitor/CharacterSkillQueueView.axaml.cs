using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterSkillQueueView : UserControl
    {
        public CharacterSkillQueueView()
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
            if (character is not CCPCharacter ccp) return;

            var entries = ccp.SkillQueue
                .Select(q => new QueueSkillEntry(q))
                .ToList();

            QueueItems.ItemsSource = entries;

            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
            {
                int training = entries.Count(e => e.IsTraining);
                status.Text = $"Skills in queue: {entries.Count}" +
                    (training > 0 ? $"  |  Currently training: {entries.First(e => e.IsTraining).SkillText}" : "  |  Paused");
            }
        }
    }

    internal sealed class QueueSkillEntry
    {
        private static readonly IBrush TrainingBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
        private static readonly IBrush CompletedBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush PendingBrush = new SolidColorBrush(Color.Parse("#FFAAAAAA"));
        private static readonly IBrush TrainingBg = new SolidColorBrush(Color.Parse("#FF1A2A2E"));
        private static readonly IBrush NormalBg = new SolidColorBrush(Color.Parse("#FF1A1A2E"));

        public string SkillText { get; }
        public string RankText { get; }
        public string StatusText { get; }
        public string TimeText { get; }
        public double Progress { get; }
        public bool IsTraining { get; }
        public IBrush NameBrush { get; }
        public IBrush StatusBrush { get; }
        public IBrush ProgressBrush { get; }
        public IBrush RowBackground { get; }

        public QueueSkillEntry(QueuedSkill q)
        {
            SkillText = $"{q.SkillName} {ToRoman(q.Level)}";
            RankText = $"×{q.Rank}";
            IsTraining = q.IsTraining;
            Progress = q.FractionCompleted * 100.0;

            if (q.IsCompleted)
            {
                StatusText = "Completed";
                StatusBrush = CompletedBrush;
                NameBrush = CompletedBrush;
                ProgressBrush = CompletedBrush;
                RowBackground = NormalBg;
                TimeText = "";
            }
            else if (q.IsTraining)
            {
                var rem = q.RemainingTime;
                string timeStr = rem.TotalDays >= 1 ? $"{(int)rem.TotalDays}d {rem.Hours}h {rem.Minutes}m"
                    : rem.TotalHours >= 1 ? $"{(int)rem.TotalHours}h {rem.Minutes}m"
                    : $"{rem.Minutes}m {rem.Seconds}s";
                StatusText = "Training";
                TimeText = $"{timeStr} remaining — ends {q.EndTime:dd MMM HH:mm}";
                StatusBrush = TrainingBrush;
                NameBrush = TrainingBrush;
                ProgressBrush = TrainingBrush;
                RowBackground = TrainingBg;
            }
            else
            {
                StatusText = "Queued";
                TimeText = q.EndTime > DateTime.MinValue ? $"Starts {q.StartTime:dd MMM HH:mm}" : "";
                StatusBrush = PendingBrush;
                NameBrush = PendingBrush;
                ProgressBrush = new SolidColorBrush(Color.Parse("#FF0F3460"));
                RowBackground = NormalBg;
            }
        }

        private static string ToRoman(int level) => level switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => level.ToString()
        };
    }
}
