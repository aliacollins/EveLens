using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterEmploymentHistoryView : UserControl
    {
        public CharacterEmploymentHistoryView()
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

            var records = character.EmploymentHistory.ToList();

            // Build timeline entries with duration (newest first = left to right)
            var timeline = new List<EmploymentTimelineEntry>();
            var sorted = records.OrderByDescending(r => r.StartDate).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var record = sorted[i];
                DateTime endDate = (i == 0) ? DateTime.UtcNow : sorted[i - 1].StartDate;
                var duration = endDate - record.StartDate;
                timeline.Add(new EmploymentTimelineEntry(record, duration, isCurrent: i == 0));
            }

            TimelineItems.ItemsSource = timeline;
            DateLabels.ItemsSource = timeline;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Corporations: {records.Count}";

            // Load corp logos after rendering
            global::Avalonia.Threading.Dispatcher.UIThread.Post(
                () => LoadCorpLogos(timeline),
                global::Avalonia.Threading.DispatcherPriority.Background);
        }

        private void LoadCorpLogos(List<EmploymentTimelineEntry> timeline)
        {
            try
            {
                // Find Image controls in the timeline cards
                var images = TimelineItems.GetVisualDescendants().OfType<Image>().ToList();

                for (int i = 0; i < Math.Min(images.Count, timeline.Count); i++)
                {
                    var entry = timeline[i];
                    var img = images[i];

                    // CorporationImage triggers async download, returns System.Drawing.Image
                    var corpImage = entry.Record.CorporationImage;
                    if (corpImage != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            corpImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                            img.Source = bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Corp logo load error: {ex}");
            }
        }
    }

    internal sealed class EmploymentTimelineEntry
    {
        public EmploymentRecord Record { get; }
        public TimeSpan Duration { get; }
        public bool IsCurrent { get; }
        public string CorporationName => Record.CorporationName;
        public DateTime StartDate => Record.StartDate;

        public string DurationText
        {
            get
            {
                string prefix = IsCurrent ? "Current - " : "";
                if (Duration.TotalDays >= 365)
                    return $"{prefix}{(int)(Duration.TotalDays / 365)}y {(int)(Duration.TotalDays % 365 / 30)}m";
                if (Duration.TotalDays >= 30)
                    return $"{prefix}{(int)(Duration.TotalDays / 30)}m {(int)(Duration.TotalDays % 30)}d";
                return $"{prefix}{(int)Duration.TotalDays}d";
            }
        }

        public EmploymentTimelineEntry(EmploymentRecord record, TimeSpan duration, bool isCurrent = false)
        {
            Record = record;
            Duration = duration;
            IsCurrent = isCurrent;
        }
    }
}
