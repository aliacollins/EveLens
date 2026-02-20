using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Common.ViewModels;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorHeader : UserControl
    {
        private ObservableCharacter? _observable;
        private bool _suppressComboChange;

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

                    // Set initial ComboBox selection
                    _suppressComboChange = true;
                    StatusOverrideCombo.SelectedIndex = (int)oc.Character.AccountStatusSettings;
                    _suppressComboChange = false;

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

                // Account status badge
                var effectiveStatus = oc.Character.EffectiveCharacterStatus;
                bool isOmega = effectiveStatus == AccountStatus.Omega;
                StatusBadgeText.Text = isOmega ? "\u03A9 Omega" : "\u03B1 Alpha";
                StatusBadge.Background = isOmega
                    ? new SolidColorBrush(Color.Parse("#2200C853"))
                    : new SolidColorBrush(Color.Parse("#22FF6D00"));
                StatusBadgeText.Foreground = isOmega
                    ? new SolidColorBrush(Color.Parse("#FF00C853"))
                    : new SolidColorBrush(Color.Parse("#FFFF6D00"));
            }
            catch
            {
                // Non-critical display
            }
        }

        private void OnStatusOverrideChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_suppressComboChange || _observable == null) return;
                int index = StatusOverrideCombo.SelectedIndex;
                if (index < 0) return;
                _observable.Character.AccountStatusSettings = (AccountStatusMode)index;
                RefreshDisplay(_observable);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing account status: {ex}");
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
