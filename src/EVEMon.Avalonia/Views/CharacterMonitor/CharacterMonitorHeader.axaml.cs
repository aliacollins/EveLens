using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Models;
using EVEMon.Common.Service;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorHeader : UserControl
    {
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
                    // Portrait
                    var image = await ImageService.GetCharacterImageAsync(character.CharacterID);
                    if (image != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            image, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                            PortraitImage.Source = bitmap;
                    }

                    // Location + docked status (methods now work without API key check)
                    string location = character.GetLastKnownLocationText();
                    string docked = character.GetLastKnownDockedText();

                    LocationText.Text = $"Located in: {location}";
                    DockedText.Text = !string.IsNullOrEmpty(docked)
                        ? $"Docked at: {docked}"
                        : "In space";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading header: {ex}");
            }
        }
    }
}
