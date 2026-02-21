// EVEMon NexT — Character Intelligence for EVE Online
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
using EVEMon.Common;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Avalonia.Views.Dialogs
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
            var characters = AppServices.Characters.Where(c => c.Monitored).ToList();

            foreach (var character in characters)
            {
                bool isInGroup = group.CharacterGuids.Contains(character.Guid);
                var cb = new CheckBox
                {
                    Content = character.Name,
                    IsChecked = isInGroup,
                    FontSize = 11,
                    Tag = character.Guid
                };

                cb.IsCheckedChanged += (s, _) =>
                {
                    try
                    {
                        if (s is not CheckBox checkbox) return;
                        bool isChecked = checkbox.IsChecked == true;
                        var charGuid = (Guid)checkbox.Tag!;

                        if (isChecked)
                        {
                            // Remove from any other group first
                            foreach (var g in Settings.CharacterGroups)
                                g.CharacterGuids.Remove(charGuid);

                            group.CharacterGuids.Add(charGuid);
                        }
                        else
                        {
                            group.CharacterGuids.Remove(charGuid);
                        }

                        Settings.Save();

                        // Refresh the group list to update counts
                        int selectedIdx = GroupListBox.SelectedIndex;
                        _suppressSelection = true;
                        RefreshGroupList();
                        GroupListBox.SelectedIndex = selectedIdx;
                        _suppressSelection = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating character group: {ex}");
                    }
                };

                CharacterPanel.Children.Add(cb);
            }
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
