using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EVEMon.Avalonia.Converters;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterOverviewView : UserControl
    {
        public CharacterOverviewView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var characters = AppServices.Characters.Where(c => c.Monitored).ToList();
                CharacterCards.ItemsSource = characters;

                // Load portraits and training info after items are rendered
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => LoadPortraitsAndTraining(),
                    global::Avalonia.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview load error: {ex}");
            }
        }

        private async void LoadPortraitsAndTraining()
        {
            try
            {
                // Find all Image controls with CharacterID tags and load their portraits
                var images = this.GetVisualDescendants().OfType<Image>()
                    .Where(img => img.Tag is long).ToList();

                foreach (var img in images)
                {
                    long charId = (long)img.Tag!;
                    try
                    {
                        var drawingImage = await ImageService.GetCharacterImageAsync(charId);
                        if (drawingImage != null)
                        {
                            var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                                drawingImage, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                            if (converted is Bitmap bitmap)
                                img.Source = bitmap;
                        }
                    }
                    catch { /* portrait load failure is non-fatal */ }
                }

                // Set training status text on each card
                var trainingTexts = this.GetVisualDescendants().OfType<TextBlock>()
                    .Where(t => t.Name == "TrainingText").ToList();

                var characters = AppServices.Characters.Where(c => c.Monitored).ToList();
                for (int i = 0; i < Math.Min(trainingTexts.Count, characters.Count); i++)
                {
                    var character = characters[i];
                    var tb = trainingTexts[i];

                    if (character is CCPCharacter ccp && ccp.IsTraining && ccp.CurrentlyTrainingSkill != null)
                    {
                        var skill = ccp.CurrentlyTrainingSkill;
                        var remaining = skill.RemainingTime;
                        string timeStr = remaining.TotalHours >= 24
                            ? $"{(int)remaining.TotalDays}d {remaining.Hours}h"
                            : remaining.TotalHours >= 1
                                ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                                : $"{remaining.Minutes}m {remaining.Seconds}s";
                        tb.Text = $"Training: {skill.SkillName} {skill.Level} ({timeStr})";
                    }
                    else
                    {
                        tb.Text = "Paused";
                        tb.Foreground = global::Avalonia.Media.Brushes.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portrait/training load error: {ex}");
            }
        }

        private void OnDeleteCharacter(object? sender, RoutedEventArgs e)
        {
            try
            {
                Character? character = null;
                if (sender is MenuItem { Tag: Character c })
                    character = c;

                if (character == null) return;

                var mainWindow = this.FindAncestorOfType<MainWindow>();
                mainWindow?.DeleteCharacterWithConfirmation(character);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex}");
            }
        }

        private void OnCharacterCardClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Find which character was clicked
                if (sender is Button btn && btn.DataContext is Character character)
                {
                    // Navigate to that character's tab in the main TabControl
                    var mainWindow = this.FindAncestorOfType<MainWindow>();
                    if (mainWindow != null)
                    {
                        var tabControl = mainWindow.FindControl<TabControl>("MainTabControl");
                        if (tabControl != null)
                        {
                            // Character tabs start at index 1 (0 = Overview)
                            var characters = AppServices.Characters.Where(c => c.Monitored).ToList();
                            int charIndex = characters.IndexOf(character);
                            if (charIndex >= 0 && charIndex + 1 < tabControl.Items.Count)
                            {
                                tabControl.SelectedIndex = charIndex + 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Card click error: {ex}");
            }
        }
    }
}
