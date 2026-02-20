using System;
using Avalonia.Controls;
using Avalonia.Media;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class SchedulerSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public SchedulerSettingsPage()
        {
            InitializeComponent();
        }

        public SchedulerSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
            WireColorTextBoxes();
        }

        private void LoadFromSettings()
        {
            var sched = _settings.UI.Scheduler;

            SetColorRow(TextColorHex, TextColorPreview, sched.TextColor);
            SetColorRow(BlockingColorHex, BlockingColorPreview, sched.BlockingColor);
            SetColorRow(RecurStartColorHex, RecurStartColorPreview, sched.RecurringEventGradientStart);
            SetColorRow(RecurEndColorHex, RecurEndColorPreview, sched.RecurringEventGradientEnd);
            SetColorRow(SimpleStartColorHex, SimpleStartColorPreview, sched.SimpleEventGradientStart);
            SetColorRow(SimpleEndColorHex, SimpleEndColorPreview, sched.SimpleEventGradientEnd);
        }

        private void WireColorTextBoxes()
        {
            WireTextBoxToPreview(TextColorHex, TextColorPreview);
            WireTextBoxToPreview(BlockingColorHex, BlockingColorPreview);
            WireTextBoxToPreview(RecurStartColorHex, RecurStartColorPreview);
            WireTextBoxToPreview(RecurEndColorHex, RecurEndColorPreview);
            WireTextBoxToPreview(SimpleStartColorHex, SimpleStartColorPreview);
            WireTextBoxToPreview(SimpleEndColorHex, SimpleEndColorPreview);
        }

        private static void WireTextBoxToPreview(TextBox textBox, Border preview)
        {
            textBox.TextChanged += (_, _) =>
            {
                try
                {
                    var c = FromHex(textBox.Text ?? string.Empty);
                    preview.Background = new SolidColorBrush(
                        Color.FromArgb(c.A, c.R, c.G, c.B));
                }
                catch
                {
                    // Invalid hex, ignore
                }
            };
        }

        private static void SetColorRow(TextBox textBox, Border preview, SerializableColor c)
        {
            textBox.Text = ToHex(c);
            preview.Background = new SolidColorBrush(
                Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        private static string ToHex(SerializableColor c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static SerializableColor FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length != 6)
                return new SerializableColor { A = 255 };

            try
            {
                return new SerializableColor
                {
                    A = 255,
                    R = Convert.ToByte(hex.Substring(0, 2), 16),
                    G = Convert.ToByte(hex.Substring(2, 2), 16),
                    B = Convert.ToByte(hex.Substring(4, 2), 16)
                };
            }
            catch
            {
                return new SerializableColor { A = 255 };
            }
        }

        public void ApplyToSettings()
        {
            var sched = _settings.UI.Scheduler;

            sched.TextColor = FromHex(TextColorHex.Text ?? string.Empty);
            sched.BlockingColor = FromHex(BlockingColorHex.Text ?? string.Empty);
            sched.RecurringEventGradientStart = FromHex(RecurStartColorHex.Text ?? string.Empty);
            sched.RecurringEventGradientEnd = FromHex(RecurEndColorHex.Text ?? string.Empty);
            sched.SimpleEventGradientStart = FromHex(SimpleStartColorHex.Text ?? string.Empty);
            sched.SimpleEventGradientEnd = FromHex(SimpleEndColorHex.Text ?? string.Empty);
        }
    }
}
