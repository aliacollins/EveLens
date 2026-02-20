using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using EVEMon.Avalonia.Converters;
using EVEMon.Common;
using EVEMon.Common.Models;
using EVEMon.Common.Service;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;

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

        /// <summary>
        /// Public method to refresh the view (called after group changes).
        /// </summary>
        public void RefreshView()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                OverviewPanel.Children.Clear();
                var characters = AppServices.Characters.Where(c => c.Monitored).ToList();
                var groups = Settings.CharacterGroups;

                if (groups.Count > 0)
                {
                    // Render grouped characters
                    var assignedGuids = new HashSet<Guid>();

                    foreach (var group in groups)
                    {
                        var groupChars = characters
                            .Where(c => group.CharacterGuids.Contains(c.Guid))
                            .ToList();

                        if (groupChars.Count == 0) continue;

                        foreach (var guid in group.CharacterGuids)
                            assignedGuids.Add(guid);

                        BuildGroupSection(group.Name, groupChars);
                    }

                    // Ungrouped characters
                    var ungrouped = characters.Where(c => !assignedGuids.Contains(c.Guid)).ToList();
                    if (ungrouped.Count > 0)
                    {
                        BuildGroupSection("Ungrouped", ungrouped);
                    }
                }
                else
                {
                    // No groups — flat card layout
                    var wrap = BuildCardWrapPanel(characters);
                    OverviewPanel.Children.Add(wrap);
                }

                // Load portraits and training info after items are rendered
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => LoadPortraitsAndTraining(),
                    global::Avalonia.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Overview load error: {ex}");
            }
        }

        private void BuildGroupSection(string groupName, List<Character> characters)
        {
            // Thin gold divider with group name
            var divider = new DockPanel { Margin = new Thickness(0, 4, 0, 2) };

            var label = new TextBlock
            {
                Text = groupName,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var count = new TextBlock
            {
                Text = $"{characters.Count}",
                FontSize = 10,
                Foreground = (IBrush?)Application.Current?.FindResource("EveTextDisabledBrush") ?? Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                [DockPanel.DockProperty] = Dock.Left
            };

            var line = new Border
            {
                Height = 1,
                Background = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold,
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Center
            };

            divider.Children.Add(label);
            divider.Children.Add(count);
            divider.Children.Add(line);

            OverviewPanel.Children.Add(divider);
            OverviewPanel.Children.Add(BuildCardWrapPanel(characters));
        }

        private WrapPanel BuildCardWrapPanel(List<Character> characters)
        {
            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };

            foreach (var character in characters)
            {
                wrap.Children.Add(BuildCharacterCard(character));
            }

            return wrap;
        }

        private Button BuildCharacterCard(Character character)
        {
            // Portrait
            var portraitImage = new Image { Width = 56, Height = 56, Stretch = Stretch.UniformToFill };
            portraitImage.Tag = character.CharacterID;

            var portraitBorder = new Border
            {
                Width = 56, Height = 56,
                Background = (IBrush?)Application.Current?.FindResource("EveBackgroundDarkestBrush") ?? Brushes.Black,
                BorderBrush = (IBrush?)Application.Current?.FindResource("EveBorderBrush") ?? Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 12, 0),
                Child = portraitImage
            };

            // Info panel
            var infoPanel = new StackPanel { Spacing = 1 };

            infoPanel.Children.Add(new TextBlock
            {
                Text = character.Name,
                FontSize = 13, FontWeight = FontWeight.Bold,
                Foreground = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = $"{character.Balance:N2} ISK",
                FontSize = 11,
                Foreground = (IBrush?)Application.Current?.FindResource("EveSuccessGreenBrush") ?? Brushes.LimeGreen,
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = $"{character.SkillPoints:N0} SP",
                FontSize = 11,
                Foreground = (IBrush?)Application.Current?.FindResource("EveTextSecondaryBrush") ?? Brushes.Gray,
            });

            // Account status badge
            bool isOmega = character.EffectiveCharacterStatus == AccountStatus.Omega;
            var badgeText = new TextBlock
            {
                Text = isOmega ? "\u03A9 Omega" : "\u03B1 Alpha",
                FontSize = 9, FontWeight = FontWeight.SemiBold,
                Foreground = isOmega
                    ? new SolidColorBrush(Color.Parse("#FF00C853"))
                    : new SolidColorBrush(Color.Parse("#FFFF6D00"))
            };
            var badge = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 1, 0, 0),
                Background = isOmega
                    ? new SolidColorBrush(Color.Parse("#2200C853"))
                    : new SolidColorBrush(Color.Parse("#22FF6D00")),
                Child = badgeText
            };
            infoPanel.Children.Add(badge);

            // Training status
            var trainingText = new TextBlock
            {
                FontSize = 10,
                Foreground = (IBrush?)Application.Current?.FindResource("EveWarningYellowBrush") ?? Brushes.Yellow,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            trainingText.Tag = "TrainingText";

            if (character is CCPCharacter ccp && ccp.IsTraining && ccp.CurrentlyTrainingSkill != null)
            {
                var skill = ccp.CurrentlyTrainingSkill;
                var remaining = skill.RemainingTime;
                string timeStr = remaining.TotalHours >= 24
                    ? $"{(int)remaining.TotalDays}d {remaining.Hours}h"
                    : remaining.TotalHours >= 1
                        ? $"{(int)remaining.TotalHours}h {remaining.Minutes}m"
                        : $"{remaining.Minutes}m {remaining.Seconds}s";
                trainingText.Text = $"Training: {skill.SkillName} {skill.Level} ({timeStr})";
            }
            else
            {
                trainingText.Text = "Paused";
                trainingText.Foreground = Brushes.Gray;
            }
            infoPanel.Children.Add(trainingText);

            var cardGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("68,*") };
            Grid.SetColumn(portraitBorder, 0);
            Grid.SetColumn(infoPanel, 1);
            cardGrid.Children.Add(portraitBorder);
            cardGrid.Children.Add(infoPanel);

            var cardBorder = new Border
            {
                Background = (IBrush?)Application.Current?.FindResource("EveBackgroundMediumBrush") ?? Brushes.DarkGray,
                BorderBrush = (IBrush?)Application.Current?.FindResource("EveBorderBrush") ?? Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10),
                Width = 300,
                MinHeight = 90,
                Child = cardGrid
            };

            var btn = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Margin = new Thickness(4),
                Cursor = new global::Avalonia.Input.Cursor(global::Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Content = cardBorder,
                DataContext = character
            };

            // Context menu
            var deleteItem = new MenuItem { Header = "Delete Character...", Tag = character };
            deleteItem.Click += OnDeleteCharacter;
            btn.ContextMenu = new ContextMenu { Items = { deleteItem } };

            btn.Click += OnCharacterCardClick;

            return btn;
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Portrait load error: {ex}");
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
                if (sender is Button btn && btn.DataContext is Character character)
                {
                    var mainWindow = this.FindAncestorOfType<MainWindow>();
                    if (mainWindow != null)
                    {
                        var tabControl = mainWindow.FindControl<TabControl>("MainTabControl");
                        if (tabControl != null)
                        {
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
