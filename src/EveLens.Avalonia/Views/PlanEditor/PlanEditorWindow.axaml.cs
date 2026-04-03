// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EveLens.Common;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Common.Interfaces;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.PlanEditor
{
    public partial class PlanEditorWindow : Window
    {
        private PlanEditorViewModel? _viewModel;
        private PlanUnifiedView? _unifiedView;
        private PlanSkillBrowserView? _skillBrowserView;
        private PlanShipBrowserView? _shipBrowserView;
        private PlanItemBrowserView? _itemBrowserView;
        private PlanBlueprintBrowserView? _blueprintBrowserView;
        public PlanEditorWindow()
        {
            InitializeComponent();
        }

        public void Initialize(Plan plan, Character character)
        {
            plan.LastActivity = DateTime.UtcNow;

            _viewModel = new PlanEditorViewModel();
            _viewModel.Character = character;
            _viewModel.Plan = plan;

            PlanNameText.Text = plan.Name;
            CharacterNameText.Text = character.Name;

            _unifiedView = new PlanUnifiedView();

            ShowPlanTab();
            UpdateStatusBar();
        }

        private void ShowPlanTab()
        {
            if (_viewModel == null || _unifiedView == null) return;
            ContentPanel.Children.Clear();
            _unifiedView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_unifiedView);
            SetActiveTab(PlanTab);
        }

        private void ShowSkillsTab()
        {
            if (_viewModel == null) return;
            _skillBrowserView ??= new PlanSkillBrowserView();
            ContentPanel.Children.Clear();
            _skillBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_skillBrowserView);
            SetActiveTab(SkillsTab);
        }

        private void ShowShipsTab()
        {
            if (_viewModel == null) return;
            _shipBrowserView ??= new PlanShipBrowserView();
            ContentPanel.Children.Clear();
            _shipBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_shipBrowserView);
            SetActiveTab(ShipsTab);
        }

        private void ShowItemsTab()
        {
            if (_viewModel == null) return;
            _itemBrowserView ??= new PlanItemBrowserView();
            ContentPanel.Children.Clear();
            _itemBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_itemBrowserView);
            SetActiveTab(ItemsTab);
        }

        private void ShowBlueprintsTab()
        {
            if (_viewModel == null) return;
            _blueprintBrowserView ??= new PlanBlueprintBrowserView();
            ContentPanel.Children.Clear();
            _blueprintBrowserView.SetViewModel(_viewModel);
            ContentPanel.Children.Add(_blueprintBrowserView);
            SetActiveTab(BlueprintsTab);
        }

        /// <summary>
        /// Switches to the Skills browser tab. Called from dashboard "Add Skills" button.
        /// </summary>
        internal void SwitchToSkillBrowser()
        {
            ShowSkillsTab();
        }

        private void SetActiveTab(ToggleButton active)
        {
            PlanTab.IsChecked = active == PlanTab;
            SkillsTab.IsChecked = active == SkillsTab;
            ShipsTab.IsChecked = active == ShipsTab;
            ItemsTab.IsChecked = active == ItemsTab;
            BlueprintsTab.IsChecked = active == BlueprintsTab;
        }

        private void OnTabClicked(object? sender, RoutedEventArgs e)
        {
            if (sender == PlanTab) ShowPlanTab();
            else if (sender == SkillsTab) ShowSkillsTab();
            else if (sender == ShipsTab) ShowShipsTab();
            else if (sender == ItemsTab) ShowItemsTab();
            else if (sender == BlueprintsTab) ShowBlueprintsTab();
        }

        private void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text?.Trim() ?? "";
            ClearSearchBtn.IsVisible = !string.IsNullOrEmpty(filter);
            if (_unifiedView != null && ContentPanel.Children.Contains(_unifiedView))
                _unifiedView.RefreshSkillList(filter);
        }

        private void OnClearSearch(object? sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            ClearSearchBtn.IsVisible = false;
            if (_unifiedView != null && ContentPanel.Children.Contains(_unifiedView))
                _unifiedView.RefreshSkillList("");
        }

        private void OnExportMenu(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var menu = new ContextMenu();

            var copyItem = new MenuItem { Header = "Copy to Clipboard" };
            copyItem.Click += async (_, _) =>
            {
                try
                {
                    if (_viewModel?.Plan == null) return;
                    // Game-compatible format: "Skill Name N" per line
                    var sb = new System.Text.StringBuilder();
                    foreach (var entry in _viewModel.Plan)
                    {
                        sb.AppendLine($"{entry.Skill.Name} {entry.Level}");
                    }
                    if (AppServices.ClipboardService != null)
                        await AppServices.ClipboardService.SetTextAsync(sb.ToString());
                    Title = $"Plan Editor — Copied {_viewModel.Plan.Count()} skills";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Copy failed: {ex.Message}");
                }
            };
            menu.Items.Add(copyItem);

            var saveItem = new MenuItem { Header = "Save to File..." };
            saveItem.Click += async (_, _) =>
            {
                try
                {
                    if (_viewModel?.Plan == null) return;

                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) return;

                    var result = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Export Plan",
                        SuggestedFileName = _viewModel.Plan.Name,
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } },
                            new FilePickerFileType("Plan Files") { Patterns = new[] { "*.emp" } },
                        }
                    });

                    if (result == null) return;

                    string path = result.Path.LocalPath;
                    string content;

                    if (path.EndsWith(".emp", StringComparison.OrdinalIgnoreCase))
                    {
                        content = PlanIOHelper.ExportAsXML(_viewModel.Plan);
                    }
                    else
                    {
                        var settings = new PlanExportSettings
                        {
                            EntryNumber = true,
                            EntryTrainingTimes = true,
                            FooterCount = true,
                            FooterTotalTime = true,
                        };
                        content = PlanIOHelper.ExportAsText(_viewModel.Plan, settings);
                    }

                    await System.IO.File.WriteAllTextAsync(path, content);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
                }
            };
            menu.Items.Add(saveItem);

            btn.ContextMenu = menu;
            menu.Open(btn);
        }

        private void OnImportFitMenu(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            var menu = new ContextMenu();

            var clipItem = new MenuItem { Header = "From Clipboard (EFT/XML/DNA)" };
            clipItem.Click += async (_, _) =>
            {
                try
                {
                    string? clipText = await (AppServices.ClipboardService?.GetTextAsync() ?? Task.FromResult<string?>(null));
                    if (string.IsNullOrWhiteSpace(clipText))
                        return;

                    // Try plan text format first ("1. Skill V (time)")
                    if (TryImportPlanTextFormat(clipText))
                        return;

                    // Try simple skill list ("Skill Name 3" — game clipboard format)
                    if (TryImportSimpleSkillList(clipText))
                        return;

                    // Fall through to fitting parser
                    await ImportFitFromText(clipText);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Import from clipboard failed: {ex.Message}");
                }
            };
            menu.Items.Add(clipItem);

            var fileItem = new MenuItem { Header = "From File..." };
            fileItem.Click += async (_, _) =>
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) return;

                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Import Fitting",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new FilePickerFileType("Fitting Files") { Patterns = new[] { "*.txt", "*.xml", "*.clf", "*.fit", "*.emp" } },
                            new FilePickerFileType("All Files") { Patterns = new[] { "*" } },
                        }
                    });

                    if (files.Count == 0) return;

                    string filePath = files[0].Path.LocalPath;

                    // .emp and plan .xml files use PlanIOHelper, not fitting parser
                    if (filePath.EndsWith(".emp", StringComparison.OrdinalIgnoreCase))
                    {
                        ImportPlanFromFile(filePath);
                        return;
                    }

                    await using var stream = await files[0].OpenReadAsync();
                    using var reader = new System.IO.StreamReader(stream);
                    string fitText = await reader.ReadToEndAsync();

                    // Check if it's a plan XML (starts with <?xml and contains <plan)
                    if (fitText.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
                        && fitText.Contains("<plan", StringComparison.OrdinalIgnoreCase))
                    {
                        // Save to temp file for PlanIOHelper
                        string tempPath = System.IO.Path.GetTempFileName();
                        await System.IO.File.WriteAllTextAsync(tempPath, fitText);
                        try { ImportPlanFromFile(tempPath); }
                        finally { System.IO.File.Delete(tempPath); }
                        return;
                    }

                    // Check if it's a plan text export (lines like "1. Skill Name I (time)")
                    if (TryImportPlanTextFormat(fitText))
                        return;

                    // Check if it's a simple skill list ("Skill Name 3" — game format)
                    if (TryImportSimpleSkillList(fitText))
                        return;

                    await ImportFitFromText(fitText);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Import from file failed: {ex.Message}");
                }
            };
            menu.Items.Add(fileItem);

            btn.ContextMenu = menu;
            menu.Open(btn);
        }

        private void ImportPlanFromFile(string filePath)
        {
            try
            {
                if (_viewModel?.Plan == null)
                {
                    AppServices.TraceService?.Trace($"ImportPlan: no plan/viewmodel");
                    return;
                }

                AppServices.TraceService?.Trace($"ImportPlan: reading {filePath}");

                var serialPlan = PlanIOHelper.ImportFromXML(filePath);
                if (serialPlan == null)
                {
                    AppServices.TraceService?.Trace("ImportPlan: ImportFromXML returned null");
                    Title = "Import failed — file could not be parsed";
                    return;
                }

                AppServices.TraceService?.Trace($"ImportPlan: {serialPlan.Entries.Count} entries found");

                if (serialPlan.Entries.Count == 0)
                {
                    Title = "Import failed — no skill entries in file";
                    return;
                }

                var plan = _viewModel.Plan;
                int added = 0;
                foreach (var entry in serialPlan.Entries)
                {
                    var skill = Common.Data.StaticSkills.GetSkillByID(entry.ID);
                    if (skill == null || skill == Common.Data.StaticSkill.UnknownStaticSkill)
                        continue;

                    plan.PlanTo(skill, entry.Level);
                    added++;
                }

                AppServices.TraceService?.Trace($"ImportPlan: planned {added} skills");

                _viewModel.UpdateDisplayPlan();
                if (_unifiedView != null)
                    _unifiedView.SetViewModel(_viewModel);
                UpdateStatusBar();

                Title = $"Plan Editor — Imported {added} skills";
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"ImportPlan ERROR: {ex.Message}");
                Title = $"Import failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Tries to parse plan text export format: "N. Skill Name Level (time)"
        /// Returns true if it looks like a plan text and was imported.
        /// </summary>
        private bool TryImportPlanTextFormat(string text)
        {
            if (_viewModel?.Plan == null || string.IsNullOrWhiteSpace(text))
                return false;

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            // Heuristic: plan text starts with "1."
            if (lines.Length == 0 || !lines[0].TrimStart().StartsWith("1."))
                return false;

            var romanToInt = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["I"] = 1, ["II"] = 2, ["III"] = 3, ["IV"] = 4, ["V"] = 5
            };

            var plan = _viewModel.Plan;
            int added = 0;

            foreach (var line in lines)
            {
                // Strip "N. " prefix and "(time)" suffix
                string trimmed = line.Trim();
                int dotPos = trimmed.IndexOf(". ");
                if (dotPos < 0) continue;
                trimmed = trimmed.Substring(dotPos + 2);

                int parenPos = trimmed.LastIndexOf('(');
                if (parenPos > 0)
                    trimmed = trimmed.Substring(0, parenPos).Trim();

                // Last word should be roman numeral (level)
                int lastSpace = trimmed.LastIndexOf(' ');
                if (lastSpace < 0) continue;

                string levelStr = trimmed.Substring(lastSpace + 1);
                string skillName = trimmed.Substring(0, lastSpace).Trim();

                if (!romanToInt.TryGetValue(levelStr, out int level))
                    continue;

                var skill = Common.Data.StaticSkills.GetSkillByName(skillName);
                if (skill == null) continue;

                plan.PlanTo(skill, level);
                added++;
            }

            if (added == 0)
                return false;

            _viewModel.UpdateDisplayPlan();
            if (_unifiedView != null)
                _unifiedView.SetViewModel(_viewModel);
            UpdateStatusBar();
            Title = $"Plan Editor — Imported {added} skills from text";
            return true;
        }

        /// <summary>
        /// Tries to parse simple skill list: "Skill Name N" (one per line).
        /// This is the format from the EVE game client clipboard.
        /// </summary>
        private bool TryImportSimpleSkillList(string text)
        {
            if (_viewModel?.Plan == null || string.IsNullOrWhiteSpace(text))
                return false;

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return false;

            var plan = _viewModel.Plan;
            int added = 0;
            int matched = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Find the last space — everything after is the level number
                int lastSpace = line.LastIndexOf(' ');
                if (lastSpace < 0) continue;

                string levelStr = line.Substring(lastSpace + 1);
                string skillName = line.Substring(0, lastSpace).Trim();

                if (!int.TryParse(levelStr, out int level) || level < 1 || level > 5)
                    continue;

                matched++;

                var skill = Common.Data.StaticSkills.GetSkillByName(skillName);
                if (skill == null) continue;

                plan.PlanTo(skill, level);
                added++;
            }

            // Only count as a match if most lines parsed successfully
            if (matched < lines.Length / 2)
                return false;

            if (added == 0)
                return false;

            _viewModel.UpdateDisplayPlan();
            if (_unifiedView != null)
                _unifiedView.SetViewModel(_viewModel);
            UpdateStatusBar();
            Title = $"Plan Editor — Imported {added} skills";
            return true;
        }

        private async System.Threading.Tasks.Task ImportFitFromText(string fitText)
        {
            if (_viewModel?.Plan == null || string.IsNullOrWhiteSpace(fitText))
                return;

            if (!LoadoutHelper.IsLoadout(fitText, out LoadoutFormat format))
            {
                System.Diagnostics.Debug.WriteLine("Text does not contain a valid fitting");
                return;
            }

            // Parse the fitting
            ILoadoutInfo loadout = format switch
            {
                LoadoutFormat.EFT => LoadoutHelper.DeserializeEftFormat(fitText),
                LoadoutFormat.XML => LoadoutHelper.DeserializeXmlFormat(fitText),
                LoadoutFormat.DNA => LoadoutHelper.DeserializeDnaFormat(fitText),
                LoadoutFormat.CLF => LoadoutHelper.DeserializeClfFormat(fitText),
                _ => new LoadoutInfo()
            };

            if (loadout.Ship == null || loadout.Loadouts.Count == 0)
                return;

            // Collect all required skills from ship + modules
            var requiredSkills = new Dictionary<StaticSkill, long>();

            // Ship prerequisites
            foreach (var prereq in loadout.Ship.Prerequisites)
            {
                if (requiredSkills.TryGetValue(prereq.Skill, out long existing))
                    requiredSkills[prereq.Skill] = Math.Max(existing, prereq.Level);
                else
                    requiredSkills[prereq.Skill] = prereq.Level;
            }

            // Module/item prerequisites
            foreach (var fit in loadout.Loadouts)
            {
                foreach (var item in fit.Items)
                {
                    foreach (var prereq in item.Prerequisites)
                    {
                        if (requiredSkills.TryGetValue(prereq.Skill, out long existing))
                            requiredSkills[prereq.Skill] = Math.Max(existing, prereq.Level);
                        else
                            requiredSkills[prereq.Skill] = prereq.Level;
                    }
                }
            }

            // Add all required skills to the plan
            var skillsToAdd = requiredSkills
                .Select(kv => new StaticSkillLevel(kv.Key, kv.Value))
                .ToList();

            if (skillsToAdd.Count == 0) return;

            string fitName = loadout.Loadouts.Count > 0
                ? loadout.Loadouts[0].Name
                : loadout.Ship.Name;

            var op = _viewModel.Plan.TryAddSet(skillsToAdd, $"Fit: {fitName}");
            op.PerformAddition(PlanEntry.DefaultPriority);

            _viewModel.UpdateDisplayPlan();
            _unifiedView?.SetViewModel(_viewModel);
            UpdateStatusBar();
        }

        private void OnClearPlan(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.Plan == null || _viewModel.Plan.Count == 0) return;

            // Remove all entries via TryRemoveSet (handles cleanup properly)
            var allEntries = _viewModel.Plan.ToArray();
            var op = _viewModel.Plan.TryRemoveSet(allEntries);
            op.Perform();

            _viewModel.UpdateDisplayPlan();
            _unifiedView?.SetViewModel(_viewModel);
            UpdateStatusBar();
        }

        internal void UpdateStatusBar()
        {
            if (_viewModel == null) return;
            var stats = _viewModel.PlanStats;
            StatusText.Text = $"Training time: {FormatTime(stats.TrainingTime)} | " +
                              $"Skills: {_viewModel.EntryCount} | " +
                              $"SP: {stats.TotalSkillPoints:N0}";
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Ctrl+W closes the plan editor window
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.W)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (_unifiedView == null || !ContentPanel.Children.Contains(_unifiedView))
                return;

            if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Up)
            {
                _unifiedView.MoveSelectionUp();
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Down)
            {
                _unifiedView.MoveSelectionDown();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None)
            {
                _unifiedView.DeleteSelected();
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
