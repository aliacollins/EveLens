// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using EveLens.Common.Models;

using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class ManagePlansWindow : Window
    {
        private Character? _character;
        private IDisposable? _fontScaleSub;

        public ManagePlansWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the plan selected for opening, or null if dialog was closed without opening a plan.
        /// </summary>
        public Plan? SelectedPlan { get; private set; }

        public void Initialize(Character character)
        {
            _character = character;
            Title = $"Manage Plans \u2014 {character.Name}";
            RefreshGrid();

            _fontScaleSub = AppServices.EventAggregator?.Subscribe<EveLens.Common.Events.FontScaleChangedEvent>(
                _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(RefreshGrid));
        }

        protected override void OnClosed(EventArgs e)
        {
            _fontScaleSub?.Dispose();
            base.OnClosed(e);
        }

        private void RefreshGrid()
        {
            if (_character == null) return;

            var items = _character.Plans.Select(p => new PlanGridItem
            {
                Plan = p,
                Name = p.Name,
                SkillCount = p.Count(),
                TrainingTime = FormatTime(p.TotalTrainingTime)
            }).ToList();

            PlanGrid.ItemsSource = items;

            if (items.Count > 0 && PlanGrid.SelectedIndex < 0)
                PlanGrid.SelectedIndex = 0;
        }

        private PlanGridItem? GetSelectedItem()
        {
            return PlanGrid.SelectedItem as PlanGridItem;
        }

        private void OnOpenClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var item = GetSelectedItem();
                if (item == null) return;

                SelectedPlan = item.Plan;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening plan: {ex}");
            }
        }

        private async void OnNewClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character == null) return;

                string? name = await ShowNameInputDialog("New Plan",
                    $"Plan {_character.Plans.Count + 1}");
                if (string.IsNullOrWhiteSpace(name)) return;

                var plan = new Plan(_character) { Name = name };
                _character.Plans.Add(plan);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating plan: {ex}");
            }
        }

        private void OnDuplicateClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character == null) return;
                var item = GetSelectedItem();
                if (item == null) return;

                var clone = item.Plan.Clone();
                clone.Name = _character.Plans.GetUniqueName($"Copy of {item.Plan.Name}");
                _character.Plans.Add(clone);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error duplicating plan: {ex}");
            }
        }

        private async void OnRenameClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var item = GetSelectedItem();
                if (item == null) return;

                string? name = await ShowNameInputDialog("Rename Plan", item.Plan.Name, item.Plan);
                if (string.IsNullOrWhiteSpace(name)) return;

                item.Plan.Name = name;
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error renaming plan: {ex}");
            }
        }

        private async void OnDeleteClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character == null) return;
                var item = GetSelectedItem();
                if (item == null) return;

                bool confirmed = await ShowConfirmationDialog(
                    "Delete Plan",
                    $"Delete \"{item.Plan.Name}\"? This cannot be undone.");
                if (!confirmed) return;

                _character.Plans.Remove(item.Plan);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting plan: {ex}");
            }
        }

        private async void OnImportClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character == null) return;

                var fileTypes = new[]
                {
                    new FilePickerFileType("EveLens Plan") { Patterns = new[] { "*.emp", "*.xml" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                };

                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Import Plan",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                });

                if (files.Count == 0) return;

                string path = files[0].Path.LocalPath;
                string planName = Path.GetFileNameWithoutExtension(path);
                planName = _character.Plans.GetUniqueName(planName);
                var plan = new Plan(_character) { Name = planName };
                _character.Plans.Add(plan);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error importing plan: {ex}");
            }
        }

        private void OnFromQueueClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_character is not CCPCharacter ccp) return;

                string planName = _character.Plans.GetUniqueName("From Skill Queue");
                var plan = new Plan(_character) { Name = planName };
                foreach (var queueItem in ccp.SkillQueue)
                {
                    if (queueItem.Skill?.StaticData != null)
                    {
                        plan.PlanTo(queueItem.Skill.StaticData, queueItem.Level);
                    }
                }
                _character.Plans.Add(plan);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating from queue: {ex}");
            }
        }

        private static string FormatTime(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }

        private async Task<string?> ShowNameInputDialog(string title, string defaultName,
            Plan? excludeFromCheck = null)
        {
            if (_character == null) return null;
            var plans = _character.Plans;

            string? result = null;
            var nameBox = new TextBox
            {
                Text = defaultName,
                FontSize = FontScaleService.Subheading,
                Margin = new Thickness(0, 8, 0, 0),
                Watermark = "Enter plan name..."
            };

            var errorText = new TextBlock
            {
                Text = "A plan with this name already exists.",
                FontSize = FontScaleService.Small,
                Foreground = (global::Avalonia.Media.IBrush?)Application.Current?.FindResource("EveErrorRedBrush")
                             ?? global::Avalonia.Media.Brushes.Red,
                Margin = new Thickness(0, 4, 0, 0),
                IsVisible = false
            };

            var okBtn = new Button
            {
                Content = "OK",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            nameBox.TextChanged += (_, _) =>
            {
                string? current = nameBox.Text?.Trim();
                bool isEmpty = string.IsNullOrEmpty(current);
                bool isDuplicate = !isEmpty && plans.ContainsName(current!, excludeFromCheck);
                errorText.IsVisible = isDuplicate;
                okBtn.IsEnabled = !isEmpty && !isDuplicate;
            };

            var dialog = new Window
            {
                Title = title,
                Width = 320, Height = 175,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new TextBlock { Text = "Plan name:", FontSize = FontScaleService.Subheading },
                        nameBox,
                        errorText,
                        okBtn
                    }
                }
            };

            nameBox.AttachedToVisualTree += (_, _) => { nameBox.Focus(); nameBox.SelectAll(); };
            okBtn.Click += (_, _) =>
            {
                result = nameBox.Text?.Trim();
                if (!string.IsNullOrEmpty(result) && !plans.ContainsName(result, excludeFromCheck))
                    dialog.Close();
            };

            await dialog.ShowDialog(this);
            return result;
        }

        private async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            bool result = false;

            var okBtn = new Button
            {
                Content = "Delete",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12),
                Foreground = global::Avalonia.Media.Brushes.Red
            };
            var cancelBtn = new Button
            {
                Content = "Cancel",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12)
            };

            var dialog = new Window
            {
                Title = title,
                Width = 380, Height = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new DockPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 12, 0, 0),
                            [DockPanel.DockProperty] = Dock.Bottom,
                            Children = { okBtn, cancelBtn }
                        },
                        new TextBlock
                        {
                            Text = message,
                            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                            FontSize = FontScaleService.Subheading,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

            okBtn.Click += (_, _) => { result = true; dialog.Close(); };
            cancelBtn.Click += (_, _) => { dialog.Close(); };

            await dialog.ShowDialog(this);
            return result;
        }
    }

    internal class PlanGridItem
    {
        public Plan Plan { get; set; } = null!;
        public string Name { get; set; } = "";
        public int SkillCount { get; set; }
        public string TrainingTime { get; set; } = "";
    }
}
