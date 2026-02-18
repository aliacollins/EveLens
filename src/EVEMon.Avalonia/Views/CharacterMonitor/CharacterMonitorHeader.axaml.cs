using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorHeader : UserControl
    {
        private IDisposable? _infoSub;
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

                    // Subscribe to info updates (location, ship, etc.)
                    _infoSub?.Dispose();
                    _infoSub = AppServices.EventAggregator?.Subscribe<CharacterInfoUpdatedEvent>(
                        evt =>
                        {
                            if (evt.Character == _character)
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

                string location = _character.GetLastKnownLocationText();
                string docked = _character.GetLastKnownDockedText();

                LocationText.Text = $"Located in: {location}";
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
            _infoSub?.Dispose();
            _infoSub = null;
        }
    }
}
