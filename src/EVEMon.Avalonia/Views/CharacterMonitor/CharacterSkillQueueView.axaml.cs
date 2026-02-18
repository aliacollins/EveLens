using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterSkillQueueView : UserControl
    {
        private SkillQueueViewModel? _viewModel;

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
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character is not CCPCharacter) return;

            _viewModel ??= new SkillQueueViewModel();
            _viewModel.Character = character;

            // Wrap VM entries with display entries for AXAML color binding
            var displayEntries = _viewModel.QueueEntries
                .Select(e => new QueueDisplayEntry(e))
                .ToList();

            QueueItems.ItemsSource = displayEntries;

            var status = this.FindControl<TextBlock>("StatusText");
            if (status != null)
            {
                status.Text = $"Skills in queue: {_viewModel.QueueEntries.Count}" +
                    (_viewModel.TrainingCount > 0 ? $"  |  Currently training: {_viewModel.CurrentTrainingText}" : "  |  Paused");
            }
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
