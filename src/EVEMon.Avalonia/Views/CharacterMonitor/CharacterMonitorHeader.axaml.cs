using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Common.Services;
using EVEMon.Core.Events;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorHeader : UserControl
    {
        private IDisposable? _fetchSub;
        private Character? _character;

        public CharacterMonitorHeader()
        {
            InitializeComponent();
        }

        protected override async void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            try
            {
                if (DataContext is Character character && character.CharacterID > 0)
                {
                    _character = character;

                    // Portrait
                    var image = await ImageService.GetCharacterImageAsync(character.CharacterID);
                    if (image != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            image, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                            PortraitImage.Source = bitmap;
                    }

                    // Initial display
                    RefreshInfo();

                    // Subscribe to ALL fetch completions — fires on 304 and 200
                    _fetchSub?.Dispose();
                    _fetchSub = AppServices.EventAggregator?.Subscribe<MonitorFetchCompletedEvent>(
                        evt =>
                        {
                            if (evt.CharacterId == _character.CharacterID)
                                global::Avalonia.Threading.Dispatcher.UIThread.Post(RefreshInfo);
                        });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading header: {ex}");
            }
        }

        private void RefreshInfo()
        {
            try
            {
                if (_character == null) return;

                LocationText.Text = $"Located in: {_character.GetLastKnownLocationText()}";

                string docked = _character.GetLastKnownDockedText();
                DockedText.Text = !string.IsNullOrEmpty(docked)
                    ? $"Docked at: {docked}"
                    : "In space";
            }
            catch
            {
                // Non-critical display
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _fetchSub?.Dispose();
            _fetchSub = null;
        }
    }
}
