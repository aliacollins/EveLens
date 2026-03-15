// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using EveLens.Common;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class ManageGroupsWindow : Window
    {
        private bool _suppressSelection;

        public ManageGroupsWindow()
        {
            InitializeComponent();
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            RefreshGroupList();
        }

        private void RefreshGroupList()
        {
            try
            {
                var groups = Settings.CharacterGroups;
                var items = groups.Select(g =>
                    $"{g.Name} ({g.CharacterGuids.Count} characters)").ToList();
                GroupListBox.ItemsSource = items;

                if (GroupListBox.SelectedIndex < 0 && items.Count > 0)
                    GroupListBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error refreshing group list: {ex}");
            }
        }

        private void OnGroupSelected(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_suppressSelection) return;
                int index = GroupListBox.SelectedIndex;
                if (index < 0 || index >= Settings.CharacterGroups.Count)
                {
                    CharacterHeader.Text = "Select a group";
                    CharacterPanel.Children.Clear();
                    return;
                }

                var group = Settings.CharacterGroups[index];
                CharacterHeader.Text = $"Characters in \"{group.Name}\"";
                BuildCharacterCheckboxes(group);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on group selection: {ex}");
            }
        }

        private void BuildCharacterCheckboxes(CharacterGroupSettings group)
        {
            CharacterPanel.Children.Clear();
            var allChars = AppServices.Characters.Where(c => c.Monitored).ToList();

            // Group members in order — clean rows with ▲ ▼ and ✕
            var membersInOrder = new List<Character>();
            foreach (var guid in group.CharacterGuids)
            {
                var ch = allChars.FirstOrDefault(c => c.Guid == guid);
                if (ch != null) membersInOrder.Add(ch);
            }

            for (int i = 0; i < membersInOrder.Count; i++)
            {
                var character = membersInOrder[i];

                var row = new Border
                {
                    Background = GetBrush("EveBackgroundMediumBrush"),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4),
                    Margin = new Thickness(0, 1),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var grid = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,Auto,Auto"),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var nameLabel = new TextBlock
                {
                    Text = character.Name,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = GetBrush("EveAccentPrimaryBrush"),
                    TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(nameLabel, 0);
                grid.Children.Add(nameLabel);

                var upBtn = new Button
                {
                    Content = "\u25B2",
                    FontSize = 10,
                    Padding = new Thickness(8, 2),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(4, 0, 0, 0),
                    MinWidth = 28,
                    IsEnabled = i > 0,
                    Tag = character.Guid
                };
                upBtn.Click += (s, _) => MoveCharacterInGroup(group, (Guid)((Button)s!).Tag!, -1);
                Grid.SetColumn(upBtn, 1);
                grid.Children.Add(upBtn);

                var downBtn = new Button
                {
                    Content = "\u25BC",
                    FontSize = 10,
                    Padding = new Thickness(8, 2),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(2, 0, 0, 0),
                    MinWidth = 28,
                    IsEnabled = i < membersInOrder.Count - 1,
                    Tag = character.Guid
                };
                downBtn.Click += (s, _) => MoveCharacterInGroup(group, (Guid)((Button)s!).Tag!, +1);
                Grid.SetColumn(downBtn, 2);
                grid.Children.Add(downBtn);

                var removeBtn = new Button
                {
                    Content = "\u2715",
                    FontSize = 10,
                    Padding = new Thickness(6, 2),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(6, 0, 0, 0),
                    MinWidth = 28,
                    Foreground = GetBrush("EveErrorRedBrush"),
                    Tag = character.Guid
                };
                removeBtn.Click += (s, _) => RemoveCharacterFromGroup(group, (Guid)((Button)s!).Tag!);
                Grid.SetColumn(removeBtn, 3);
                grid.Children.Add(removeBtn);

                row.Child = grid;
                CharacterPanel.Children.Add(row);
            }

            // "Add character" section — characters not in this group
            var available = allChars.Where(c => !group.CharacterGuids.Contains(c.Guid)).ToList();
            if (available.Count > 0)
            {
                CharacterPanel.Children.Add(new Separator { Margin = new Thickness(0, 8) });
                CharacterPanel.Children.Add(new TextBlock
                {
                    Text = "Add to group",
                    FontSize = 10,
                    Foreground = GetBrush("EveTextDisabledBrush"),
                    Margin = new Thickness(0, 0, 0, 4)
                });

                foreach (var character in available)
                {
                    var addBtn = new Button
                    {
                        FontSize = 10,
                        Padding = new Thickness(8, 3),
                        CornerRadius = new CornerRadius(10),
                        Margin = new Thickness(0, 1),
                        Tag = character.Guid,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };

                    var btnContent = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
                    btnContent.Children.Add(new TextBlock { Text = "+", FontSize = 11, Foreground = GetBrush("EveSuccessGreenBrush") });
                    btnContent.Children.Add(new TextBlock { Text = character.Name, FontSize = 11 });
                    addBtn.Content = btnContent;

                    addBtn.Click += (s, _) => AddCharacterToGroup(group, (Guid)((Button)s!).Tag!);
                    CharacterPanel.Children.Add(addBtn);
                }
            }
        }

        private void MoveCharacterInGroup(CharacterGroupSettings group, Guid charGuid, int direction)
        {
            try
            {
                int index = -1;
                for (int i = 0; i < group.CharacterGuids.Count; i++)
                {
                    if (group.CharacterGuids[i] == charGuid) { index = i; break; }
                }
                if (index < 0) return;

                int newIndex = index + direction;
                if (newIndex < 0 || newIndex >= group.CharacterGuids.Count) return;

                // Swap
                group.CharacterGuids[index] = group.CharacterGuids[newIndex];
                group.CharacterGuids[newIndex] = charGuid;

                Settings.Save();
                NotifyGroupsChanged();
                BuildCharacterCheckboxes(group);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error moving character in group: {ex}");
            }
        }

        private void AddCharacterToGroup(CharacterGroupSettings group, Guid charGuid)
        {
            try
            {
                // Remove from any other group first
                foreach (var g in Settings.CharacterGroups)
                    g.CharacterGuids.Remove(charGuid);

                group.CharacterGuids.Add(charGuid);
                Settings.Save();
                NotifyGroupsChanged();

                int selectedIdx = GroupListBox.SelectedIndex;
                _suppressSelection = true;
                RefreshGroupList();
                GroupListBox.SelectedIndex = selectedIdx;
                _suppressSelection = false;
                BuildCharacterCheckboxes(group);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding character to group: {ex}");
            }
        }

        private void RemoveCharacterFromGroup(CharacterGroupSettings group, Guid charGuid)
        {
            try
            {
                group.CharacterGuids.Remove(charGuid);
                Settings.Save();
                NotifyGroupsChanged();

                int selectedIdx = GroupListBox.SelectedIndex;
                _suppressSelection = true;
                RefreshGroupList();
                GroupListBox.SelectedIndex = selectedIdx;
                _suppressSelection = false;
                BuildCharacterCheckboxes(group);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error removing character from group: {ex}");
            }
        }

        private static void NotifyGroupsChanged()
        {
            AppServices.EventAggregator?.Publish(
                EveLens.Common.Events.SettingsChangedEvent.Instance);
        }

        private global::Avalonia.Media.IBrush? GetBrush(string key)
        {
            if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is global::Avalonia.Media.IBrush brush)
                return brush;
            return null;
        }

        private async void OnAddGroupClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? name = await ShowNameInputDialog("New Group", "Group " + (Settings.CharacterGroups.Count + 1));
                if (string.IsNullOrWhiteSpace(name)) return;

                var group = new CharacterGroupSettings { Name = name };
                Settings.CharacterGroups.Add(group);
                Settings.Save();
                RefreshGroupList();
                GroupListBox.SelectedIndex = Settings.CharacterGroups.Count - 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding group: {ex}");
            }
        }

        private async void OnRenameGroupClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                int index = GroupListBox.SelectedIndex;
                if (index < 0 || index >= Settings.CharacterGroups.Count) return;

                var group = Settings.CharacterGroups[index];
                string? name = await ShowNameInputDialog("Rename Group", group.Name);
                if (string.IsNullOrWhiteSpace(name)) return;

                group.Name = name;
                Settings.Save();
                RefreshGroupList();
                GroupListBox.SelectedIndex = index;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error renaming group: {ex}");
            }
        }

        private void OnDeleteGroupClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                int index = GroupListBox.SelectedIndex;
                if (index < 0 || index >= Settings.CharacterGroups.Count) return;

                Settings.CharacterGroups.RemoveAt(index);
                Settings.Save();
                RefreshGroupList();
                CharacterPanel.Children.Clear();
                CharacterHeader.Text = "Select a group";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting group: {ex}");
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            // Notify UI to refresh overview with new group order
            AppServices.EventAggregator?.Publish(EveLens.Common.Events.SettingsChangedEvent.Instance);
            Close();
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

            nameBox.AttachedToVisualTree += (_, _) => nameBox.SelectAll();
            okBtn.Click += (_, _) =>
            {
                result = nameBox.Text?.Trim();
                if (!string.IsNullOrEmpty(result))
                    dialog.Close();
            };

            await dialog.ShowDialog(this);
            return result;
        }
    }
}
