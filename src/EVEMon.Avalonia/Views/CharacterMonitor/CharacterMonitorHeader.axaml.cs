using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Service;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorHeader : UserControl
    {
        private ObservableCharacter? _observable;

        public CharacterMonitorHeader()
        {
            InitializeComponent();
        }

        protected override async void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            try
            {
                // Unsubscribe from previous observable
                if (_observable != null)
                    _observable.PropertyChanged -= OnObservableChanged;

                if (DataContext is ObservableCharacter oc && oc.CharacterID > 0)
                {
                    _observable = oc;

                    // Portrait
                    var image = await ImageService.GetCharacterImageAsync(oc.CharacterID);
                    if (image != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            image, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                            PortraitImage.Source = bitmap;
                    }

                    // Initial display + live updates via INPC
                    RefreshDisplay(oc);
                    oc.PropertyChanged += OnObservableChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading header: {ex}");
            }
        }

        private void OnObservableChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ObservableCharacter oc)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => RefreshDisplay(oc));
        }

        private void RefreshDisplay(ObservableCharacter oc)
        {
            try
            {
                // Location
                LocationText.Text = $"Located in: {oc.LocationText}";
                DockedText.Text = !string.IsNullOrEmpty(oc.DockedText)
                    ? $"Docked at: {oc.DockedText}"
                    : "In space";

                // Balance change indicator color + flash
                if (oc.BalanceDirection != 0 && BalanceChangeIndicator != null)
                {
                    BalanceChangeIndicator.Foreground = oc.BalanceDirection > 0
                        ? global::Avalonia.Media.Brushes.LimeGreen
                        : global::Avalonia.Media.Brushes.OrangeRed;

                    // Flash: set opacity to 1, then fade to 0.6 via transition
                    BalanceChangeIndicator.Opacity = 1.0;
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        BalanceChangeIndicator.Opacity = 0.7;
                    }, global::Avalonia.Threading.DispatcherPriority.Background);
                }
            }
            catch
            {
                // Non-critical display
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (_observable != null)
            {
                _observable.PropertyChanged -= OnObservableChanged;
                _observable = null;
            }
        }
    }
}
