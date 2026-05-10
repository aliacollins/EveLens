// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EveLens.Avalonia.Services;
using EveLens.Common;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class WhatsNewWindow : Window
    {
        public WhatsNewWindow()
        {
            InitializeComponent();
            BuildContent();
        }

        private void BuildContent()
        {
            var root = new Border
            {
                Background = FindBrush("EveBackgroundDarkBrush"),
                Child = new DockPanel()
            };

            var dock = (DockPanel)root.Child;

            // Header
            var header = new Border
            {
                Padding = new Thickness(24, 20, 24, 12),
                Child = new StackPanel { Spacing = 4 }
            };
            var headerPanel = (StackPanel)header.Child;
            headerPanel.Children.Add(new TextBlock
            {
                Text = "Welcome back!",
                FontSize = FontScaleService.Heading,
                FontWeight = FontWeight.Bold,
                Foreground = FindBrush("EveAccentPrimaryBrush")
            });
            var ver = AppServices.FileVersionInfo?.ProductVersion ?? "";
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"Here's what's new in EveLens {ver}",
                FontSize = FontScaleService.Body,
                Foreground = FindBrush("EveTextSecondaryBrush")
            });
            DockPanel.SetDock(header, Dock.Top);
            dock.Children.Add(header);

            // Footer with button
            var footer = new Border
            {
                Padding = new Thickness(24, 12, 24, 20),
                Child = new Button
                {
                    Content = "Let's go!",
                    FontSize = FontScaleService.Body,
                    Padding = new Thickness(24, 8),
                    Background = FindBrush("EveAccentPrimaryBrush"),
                    Foreground = FindBrush("EveBackgroundDarkestBrush"),
                    CornerRadius = new CornerRadius(14),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Cursor = new Cursor(StandardCursorType.Hand),
                    FontWeight = FontWeight.SemiBold
                }
            };
            ((Button)footer.Child).Click += (_, _) => Close();
            DockPanel.SetDock(footer, Dock.Bottom);
            dock.Children.Add(footer);

            // Scrollable release notes
            var scroll = new ScrollViewer
            {
                Padding = new Thickness(24, 0),
                Content = BuildReleaseNotes()
            };
            dock.Children.Add(scroll);

            Content = root;
        }

        private StackPanel BuildReleaseNotes()
        {
            var panel = new StackPanel { Spacing = 12 };
            var notes = GetCurrentVersionNotes();

            foreach (var section in notes)
            {
                // Section header (Added, Changed, Fixed)
                var sectionHeader = new TextBlock
                {
                    Text = section.Category,
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = GetCategoryBrush(section.Category),
                    Margin = new Thickness(0, 8, 0, 4)
                };
                panel.Children.Add(sectionHeader);

                foreach (var item in section.Items)
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                    row.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveTextDisabledBrush"),
                        VerticalAlignment = VerticalAlignment.Top
                    });
                    row.Children.Add(new TextBlock
                    {
                        Text = item,
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveTextPrimaryBrush"),
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 520
                    });
                    panel.Children.Add(row);
                }
            }

            if (notes.Length == 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Bug fixes and performance improvements.",
                    FontSize = FontScaleService.Body,
                    Foreground = FindBrush("EveTextSecondaryBrush")
                });
            }

            return panel;
        }

        private IBrush? GetCategoryBrush(string category)
        {
            return category switch
            {
                "Added" => FindBrush("EveSuccessGreenBrush"),
                "Changed" => FindBrush("EveWarningYellowBrush"),
                "Fixed" => FindBrush("EveAccentPrimaryBrush"),
                "Removed" => FindBrush("EveErrorRedBrush"),
                _ => FindBrush("EveTextPrimaryBrush")
            };
        }

        private static ReleaseSection[] GetCurrentVersionNotes()
        {
            try
            {
                var changelogPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "CHANGELOG.md");

                if (!File.Exists(changelogPath))
                    return Array.Empty<ReleaseSection>();

                var lines = File.ReadAllLines(changelogPath);
                return ParseCurrentVersion(lines);
            }
            catch
            {
                return Array.Empty<ReleaseSection>();
            }
        }

        private static ReleaseSection[] ParseCurrentVersion(string[] lines)
        {
            // Strategy: try to match current version to a changelog section.
            // If no match found, use [Unreleased] (we're on a dev/beta branch).
            var version = AppServices.FileVersionInfo?.ProductVersion ?? "";
            var versionBase = version.Split('-')[0]; // "1.3.0-beta.3" -> "1.3.0"
            bool isPreRelease = version.Contains("alpha") || version.Contains("beta");

            // If [Unreleased] has content, prefer it (we're shipping new stuff).
            // Otherwise fall back to the matching version section.
            bool unreleasedHasContent = false;
            bool inUnreleased = false;
            foreach (var l in lines)
            {
                if (l.StartsWith("## ") && l.Contains("[Unreleased]")) { inUnreleased = true; continue; }
                if (l.StartsWith("## ") && inUnreleased) break;
                if (inUnreleased && l.StartsWith("- ")) { unreleasedHasContent = true; break; }
            }

            bool useUnreleased = unreleasedHasContent;

            var sections = new System.Collections.Generic.List<ReleaseSection>();
            string? currentCategory = null;
            var currentItems = new System.Collections.Generic.List<string>();
            bool inTargetSection = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Detect version headers
                if (line.StartsWith("## "))
                {
                    if (inTargetSection)
                        break; // We've reached the next version, stop

                    if (useUnreleased && line.Contains("[Unreleased]"))
                        inTargetSection = true;
                    else if (!useUnreleased && line.Contains($"[{versionBase}]"))
                        inTargetSection = true;
                    continue;
                }

                if (!inTargetSection) continue;

                // Detect category headers (### Added, ### Fixed, etc.)
                if (line.StartsWith("### "))
                {
                    if (currentCategory != null && currentItems.Count > 0)
                        sections.Add(new ReleaseSection(currentCategory, currentItems.ToArray()));

                    currentCategory = line.Substring(4).Trim();
                    currentItems.Clear();
                    continue;
                }

                // Detect list items
                if (line.StartsWith("- ") && currentCategory != null)
                {
                    var item = line.Substring(2).Trim();
                    // Strip markdown bold
                    item = item.Replace("**", "");
                    // Strip trailing issue refs
                    var issueIdx = item.LastIndexOf("(#");
                    if (issueIdx > 0) item = item.Substring(0, issueIdx).TrimEnd();
                    // Strip trailing issue refs with "Issue" prefix
                    var issueIdx2 = item.LastIndexOf("(Issue");
                    if (issueIdx2 > 0) item = item.Substring(0, issueIdx2).TrimEnd();

                    currentItems.Add(item);
                }
            }

            if (currentCategory != null && currentItems.Count > 0)
                sections.Add(new ReleaseSection(currentCategory, currentItems.ToArray()));

            return sections.ToArray();
        }

        private IBrush? FindBrush(string name)
        {
            return Application.Current?.FindResource(name) as IBrush;
        }

        private record ReleaseSection(string Category, string[] Items);
    }
}
