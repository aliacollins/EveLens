// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EveLens.Avalonia.Converters;
using EveLens.Common;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class ManageGroupsWindow : Window
    {
        private static readonly string[] TagColors =
        {
            "#FF4A9EE8", "#FFE8A44A", "#FF6DBA6D", "#FFC75D5D",
            "#FFB07DC7", "#FF5DD5C7", "#FFE86DA4", "#FFA4C75D",
        };

        public ManageGroupsWindow()
        {
            InitializeComponent();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            RebuildUI();
        }

        private void RebuildUI()
        {
            try
            {
                BuildCharacterList();
                BuildGroupChips();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error rebuilding group UI: {ex}");
            }
        }

        #region Character List with Tags

        private void BuildCharacterList()
        {
            CharacterListPanel.Children.Clear();
            var allChars = AppServices.Characters.Where(c => c.Monitored).ToList();
            var groups = Settings.CharacterGroups;

            foreach (var character in allChars)
            {
                var group = groups.FirstOrDefault(g => g.CharacterGuids.Contains(character.Guid));
                CharacterListPanel.Children.Add(BuildCharacterRow(character, group));
            }

            // Load portraits after building
            Dispatcher.UIThread.Post(() => LoadPortraits(allChars),
                DispatcherPriority.Background);
        }

        private Border BuildCharacterRow(Character character, CharacterGroupSettings? currentGroup)
        {
            var row = new Border
            {
                Background = GetBrush("EveBackgroundMediumBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 4),
                Margin = new Thickness(0, 1)
            };

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("Auto,*,Auto"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Portrait (32x32)
            var portraitImage = new Image { Width = 32, Height = 32, Stretch = Stretch.UniformToFill };
            portraitImage.Tag = character.CharacterID;
            var portraitBorder = new Border
            {
                Width = 32, Height = 32,
                CornerRadius = new CornerRadius(4),
                ClipToBounds = true,
                Background = GetBrush("EveBackgroundDarkestBrush"),
                Child = portraitImage,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(portraitBorder, 0);
            grid.Children.Add(portraitBorder);

            // Name
            var nameText = new TextBlock
            {
                Text = character.Name,
                FontSize = 11,
                Foreground = GetBrush("EveAccentPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            // Tag area (right side)
            var tagPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 4
            };

            if (currentGroup != null)
            {
                tagPanel.Children.Add(BuildGroupTag(currentGroup, character));
            }

            // "+ Add" / "+ Assign" button
            var addBtn = new Button
            {
                Content = currentGroup == null ? "+ Assign" : "\u21C4",
                FontSize = currentGroup == null ? 10 : 11,
                Padding = new Thickness(6, 2),
                CornerRadius = new CornerRadius(10),
                Background = Brushes.Transparent,
                Foreground = GetBrush("EveTextDisabledBrush"),
                BorderThickness = new Thickness(1),
                BorderBrush = GetBrush("EveTextDisabledBrush"),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = character,
                [ToolTip.TipProperty] = currentGroup == null ? "Assign to group" : "Change group",
                MinWidth = 0, MinHeight = 0
            };
            addBtn.Click += OnAddTagClick;
            tagPanel.Children.Add(addBtn);

            Grid.SetColumn(tagPanel, 2);
            grid.Children.Add(tagPanel);

            row.Child = grid;
            return row;
        }

        private Border BuildGroupTag(CharacterGroupSettings group, Character character)
        {
            int colorIndex = Settings.CharacterGroups.IndexOf(group) % TagColors.Length;
            var tagColor = Color.Parse(TagColors[colorIndex]);
            var bgColor = new Color(40, tagColor.R, tagColor.G, tagColor.B);

            var tagPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };

            tagPanel.Children.Add(new TextBlock
            {
                Text = group.Name,
                FontSize = 10,
                Foreground = new SolidColorBrush(tagColor),
                VerticalAlignment = VerticalAlignment.Center
            });

            var removeBtn = new Button
            {
                Content = "\u2715",
                FontSize = 8,
                Padding = new Thickness(2, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(tagColor),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand),
                MinWidth = 0, MinHeight = 0,
                Tag = new GroupCharacterPair(group, character)
            };
            removeBtn.Click += OnRemoveTagClick;
            tagPanel.Children.Add(removeBtn);

            return new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 4, 2),
                Child = tagPanel
            };
        }

        private void OnRemoveTagClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not GroupCharacterPair pair) return;
                pair.Group.CharacterGuids.Remove(pair.Character.Guid);
                Settings.Save();
                NotifyGroupsChanged();
                RebuildUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing tag: {ex}");
            }
        }

        private void OnAddTagClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn || btn.Tag is not Character character) return;

                var groups = Settings.CharacterGroups;
                if (groups.Count == 0) return;

                var currentGroup = groups.FirstOrDefault(g => g.CharacterGuids.Contains(character.Guid));

                // Build a flyout with radio buttons (single group only)
                var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedRight };
                var panel = new StackPanel { Spacing = 2, MinWidth = 160 };

                // Header
                panel.Children.Add(new TextBlock
                {
                    Text = "Assign to one group:",
                    FontSize = 10,
                    Foreground = GetBrush("EveTextDisabledBrush"),
                    Margin = new Thickness(4, 2, 0, 4)
                });

                // "No group" radio
                var noneRadio = new RadioButton
                {
                    GroupName = "GroupPicker",
                    IsChecked = currentGroup == null,
                    Padding = new Thickness(4, 2),
                    Content = new TextBlock
                    {
                        Text = "No group",
                        FontSize = 11,
                        Foreground = GetBrush("EveTextSecondaryBrush")
                    }
                };
                noneRadio.IsCheckedChanged += (_, _) =>
                {
                    if (noneRadio.IsChecked == true)
                    {
                        flyout.Hide();
                        AssignCharacterToGroup(character, null);
                    }
                };
                panel.Children.Add(noneRadio);

                panel.Children.Add(new Separator { Margin = new Thickness(0, 2) });

                foreach (var group in groups)
                {
                    int colorIndex = groups.IndexOf(group) % TagColors.Length;
                    var tagColor = Color.Parse(TagColors[colorIndex]);
                    bool isCurrent = group == currentGroup;

                    var capturedGroup = group;
                    var radio = new RadioButton
                    {
                        GroupName = "GroupPicker",
                        IsChecked = isCurrent,
                        Padding = new Thickness(4, 2),
                        Content = new TextBlock
                        {
                            Text = group.Name,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(tagColor)
                        }
                    };
                    radio.IsCheckedChanged += (_, _) =>
                    {
                        if (radio.IsChecked == true)
                        {
                            flyout.Hide();
                            AssignCharacterToGroup(character, capturedGroup);
                        }
                    };
                    panel.Children.Add(radio);
                }

                // "New Group..." option
                panel.Children.Add(new Separator { Margin = new Thickness(0, 2) });
                var newGroupBtn = new Button
                {
                    Content = "+ New Group...",
                    FontSize = 10,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8, 4),
                    Foreground = GetBrush("EveTextDisabledBrush"),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                newGroupBtn.Click += async (_, _) =>
                {
                    flyout.Hide();
                    await CreateNewGroup();
                };
                panel.Children.Add(newGroupBtn);

                flyout.Content = panel;
                flyout.ShowAt(btn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing group picker: {ex}");
            }
        }

        private void AssignCharacterToGroup(Character character, CharacterGroupSettings? targetGroup)
        {
            try
            {
                // Remove from all groups first (single-group only)
                foreach (var g in Settings.CharacterGroups)
                    g.CharacterGuids.Remove(character.Guid);

                // Add to target if specified
                if (targetGroup != null)
                    targetGroup.CharacterGuids.Add(character.Guid);

                Settings.Save();
                NotifyGroupsChanged();
                RebuildUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error assigning character to group: {ex}");
            }
        }

        #endregion

        #region Portraits

        private async void LoadPortraits(List<Character> characters)
        {
            foreach (var character in characters)
            {
                try
                {
                    // Find the Image control by character ID tag
                    var image = FindPortraitImage(CharacterListPanel, character.CharacterID);
                    if (image == null) continue;

                    var drawingImage = await ImageService.GetCharacterImageAsync(character.CharacterID);
                    if (drawingImage != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            drawingImage, typeof(Bitmap), null!, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                            image.Source = bitmap;
                    }
                }
                catch
                {
                    // Portrait loading is best-effort
                }
            }
        }

        private static Image? FindPortraitImage(Panel panel, long characterId)
        {
            foreach (var child in panel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is Border pb && pb.Child is Image img &&
                            img.Tag is long id && id == characterId)
                            return img;
                    }
                }
            }
            return null;
        }

        #endregion

        #region Group Chips (bottom bar)

        private void BuildGroupChips()
        {
            GroupChipsPanel.Children.Clear();
            var groups = Settings.CharacterGroups;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                int colorIndex = i % TagColors.Length;
                var tagColor = Color.Parse(TagColors[colorIndex]);
                var bgColor = new Color(30, tagColor.R, tagColor.G, tagColor.B);
                int memberCount = group.CharacterGuids.Count;

                var chipContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

                // Group name + count
                chipContent.Children.Add(new TextBlock
                {
                    Text = $"{group.Name} ({memberCount})",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(tagColor),
                    VerticalAlignment = VerticalAlignment.Center
                });

                // Rename icon
                var capturedGroup = group;
                var renameBtn = new Button
                {
                    Content = "\u270E",
                    FontSize = 9,
                    Padding = new Thickness(2, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(tagColor),
                    Opacity = 0.6,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    MinWidth = 0, MinHeight = 0,
                    VerticalAlignment = VerticalAlignment.Center
                };
                renameBtn.Click += async (_, _) =>
                {
                    try
                    {
                        string? name = await ShowNameInputDialog("Rename Group", capturedGroup.Name);
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            capturedGroup.Name = name;
                            Settings.Save();
                            RebuildUI();
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine($"Error renaming: {ex}"); }
                };
                chipContent.Children.Add(renameBtn);

                // Delete icon
                var deleteBtn = new Button
                {
                    Content = "\u2715",
                    FontSize = 9,
                    Padding = new Thickness(2, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = GetBrush("EveErrorRedBrush"),
                    Opacity = 0.6,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    MinWidth = 0, MinHeight = 0,
                    VerticalAlignment = VerticalAlignment.Center
                };
                deleteBtn.Click += (_, _) =>
                {
                    try
                    {
                        Settings.CharacterGroups.Remove(capturedGroup);
                        Settings.Save();
                        NotifyGroupsChanged();
                        RebuildUI();
                    }
                    catch (Exception ex) { Debug.WriteLine($"Error deleting: {ex}"); }
                };
                chipContent.Children.Add(deleteBtn);

                var chip = new Border
                {
                    Background = new SolidColorBrush(bgColor),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(10, 3, 6, 3),
                    Margin = new Thickness(0, 0, 4, 4),
                    Child = chipContent
                };
                GroupChipsPanel.Children.Add(chip);
            }

            if (groups.Count == 0)
            {
                GroupChipsPanel.Children.Add(new TextBlock
                {
                    Text = "No groups yet — click '+ New Group' to create one.",
                    FontSize = 10,
                    Foreground = GetBrush("EveTextDisabledBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
        }

        #endregion

        #region Group Lifecycle

        private async void OnNewGroupClick(object? sender, RoutedEventArgs e)
        {
            try { await CreateNewGroup(); }
            catch (Exception ex) { Debug.WriteLine($"Error creating group: {ex}"); }
        }

        private async System.Threading.Tasks.Task CreateNewGroup()
        {
            string? name = await ShowNameInputDialog("New Group",
                "Group " + (Settings.CharacterGroups.Count + 1));
            if (string.IsNullOrWhiteSpace(name)) return;

            var group = new CharacterGroupSettings { Name = name };
            Settings.CharacterGroups.Add(group);
            Settings.Save();
            RebuildUI();
        }

        #endregion

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            AppServices.EventAggregator?.Publish(EveLens.Common.Events.SettingsChangedEvent.Instance);
            Close();
        }

        private static void NotifyGroupsChanged()
        {
            AppServices.EventAggregator?.Publish(EveLens.Common.Events.SettingsChangedEvent.Instance);
        }

        private IBrush? GetBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is IBrush brush)
                return brush;
            return null;
        }

        private async System.Threading.Tasks.Task<string?> ShowNameInputDialog(string title, string defaultName)
        {
            string? result = null;
            var nameBox = new TextBox
            {
                Text = defaultName,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0),
                Watermark = "Enter name..."
            };

            var okBtn = new Button
            {
                Content = "OK",
                FontSize = 11,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var dialog = new Window
            {
                Title = title,
                Width = 320, Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new TextBlock { Text = "Name:", FontSize = 12 },
                        nameBox,
                        okBtn
                    }
                }
            };

            nameBox.AttachedToVisualTree += (_, _) => { nameBox.Focus(); nameBox.SelectAll(); };
            okBtn.Click += (_, _) =>
            {
                result = nameBox.Text?.Trim();
                if (!string.IsNullOrEmpty(result))
                    dialog.Close();
            };

            await dialog.ShowDialog(this);
            return result;
        }

        private sealed record GroupCharacterPair(CharacterGroupSettings Group, Character Character);
    }
}
