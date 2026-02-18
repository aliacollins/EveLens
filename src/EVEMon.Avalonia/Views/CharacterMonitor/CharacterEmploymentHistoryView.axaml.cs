using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterEmploymentHistoryView : UserControl
    {
        private EmploymentTimelineViewModel? _viewModel;

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
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character == null) return;

            _viewModel ??= new EmploymentTimelineViewModel();
            _viewModel.Character = character;

            var timeline = _viewModel.TimelineEntries;

            TimelineItems.ItemsSource = timeline;
            DateLabels.ItemsSource = timeline;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Corporations: {_viewModel.CorporationCount}";

            // Load corp logos after rendering
            global::Avalonia.Threading.Dispatcher.UIThread.Post(
                () => LoadCorpLogos(timeline),
                global::Avalonia.Threading.DispatcherPriority.Background);
        }

        private void LoadCorpLogos(System.Collections.Generic.List<EmploymentTimelineEntry> timeline)
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
}
