// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using EveLens.Common;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class AboutWindow : Window
    {
        private readonly Dictionary<string, Button> _tabButtons = new();

        // Theme brushes resolved at construction
        private readonly IBrush _accentBrush;
        private readonly IBrush _accentSecondaryBrush;
        private readonly IBrush _textPrimaryBrush;
        private readonly IBrush _textSecondaryBrush;
        private readonly IBrush _textDisabledBrush;
        private readonly IBrush _bgMediumBrush;
        private readonly IBrush _borderBrush;
        private readonly IBrush _successGreenBrush;

        public AboutWindow()
        {
            InitializeComponent();

            _accentBrush = FindBrush("EveAccentPrimaryBrush", Brushes.Gold);
            _accentSecondaryBrush = FindBrush("EveAccentSecondaryBrush", Brushes.DarkGoldenrod);
            _textPrimaryBrush = FindBrush("EveTextPrimaryBrush", Brushes.White);
            _textSecondaryBrush = FindBrush("EveTextSecondaryBrush", Brushes.Gray);
            _textDisabledBrush = FindBrush("EveTextDisabledBrush", Brushes.DarkGray);
            _bgMediumBrush = FindBrush("EveBackgroundMediumBrush", Brushes.DarkSlateGray);
            _borderBrush = FindBrush("EveBorderBrush", Brushes.DimGray);
            _successGreenBrush = FindBrush("EveSuccessGreenBrush", Brushes.Green);

            PopulateVersionInfo();
            BuildTabs();
            ShowTab("about");
            OkButton.Click += (_, _) => Close();
        }

        private static IBrush FindBrush(string key, IBrush fallback)
        {
            return Application.Current?.FindResource(key) as IBrush ?? fallback;
        }

        private void PopulateVersionInfo()
        {
            try
            {
                var fvi = AppServices.FileVersionInfo;
                string version = AppServices.IsDebugBuild
                    ? $"{fvi.FileVersion} (Debug)"
                    : fvi.ProductVersion ?? fvi.FileVersion ?? "Unknown";
                VersionText.Text = $"v{version}";
            }
            catch
            {
                VersionText.Text = "v1.0.0";
            }
        }

        private void BuildTabs()
        {
            var tabs = new[] { ("about", "About"), ("contributors", "Contributors"), ("legal", "License") };
            foreach (var (id, label) in tabs)
            {
                var btn = new Button
                {
                    Content = label,
                    FontSize = 11,
                    FontWeight = FontWeight.Medium,
                    Padding = new Thickness(16, 6),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0, 0, 0, 2),
                    BorderBrush = Brushes.Transparent,
                    Foreground = _textDisabledBrush,
                    CornerRadius = new CornerRadius(0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Tag = id,
                };
                btn.Click += (_, _) => ShowTab((string)btn.Tag!);
                _tabButtons[id] = btn;
                TabBar.Children.Add(btn);
            }
        }

        private void ShowTab(string tabId)
        {
            foreach (var (id, btn) in _tabButtons)
            {
                if (id == tabId)
                {
                    btn.Foreground = _accentBrush;
                    btn.BorderBrush = _accentBrush;
                }
                else
                {
                    btn.Foreground = _textDisabledBrush;
                    btn.BorderBrush = Brushes.Transparent;
                }
            }

            TabContent.Children.Clear();
            Control content = tabId switch
            {
                "about" => BuildAboutTab(),
                "contributors" => BuildContributorsTab(),
                "legal" => BuildLegalTab(),
                _ => new Panel(),
            };
            TabContent.Children.Add(content);
        }

        private Control BuildAboutTab()
        {
            var stack = new StackPanel { Spacing = 0 };

            // Description
            stack.Children.Add(new TextBlock
            {
                Text = "EveLens is a complete rebuild of EVEMon \u2014 the character monitoring and "
                     + "skill planning tool that EVE Online players have relied on since 2006. "
                     + "Dark theme, modern plan editor, cross-platform architecture, and the same "
                     + "skill optimization engine under the hood.",
                FontSize = 12, Foreground = _textSecondaryBrush, TextWrapping = TextWrapping.Wrap,
                LineHeight = 22, Margin = new Thickness(0, 0, 0, 20),
            });

            // Maintainer
            AddSectionHeader(stack, "MAINTAINER");
            stack.Children.Add(new TextBlock
            {
                Text = "Alia Collins", FontSize = 13, FontWeight = FontWeight.SemiBold,
                Foreground = _accentBrush, Margin = new Thickness(0, 2, 0, 0),
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Active Developer", FontSize = 11,
                Foreground = _textSecondaryBrush, Margin = new Thickness(0, 0, 0, 16),
            });

            // External APIs
            AddSectionHeader(stack, "EXTERNAL APIs");
            foreach (var api in new[] { "CCP Games - ESI API", "EVEMarketer - Market Data", "Fuzzwork - Static Data" })
            {
                stack.Children.Add(new TextBlock
                {
                    Text = api, FontSize = 11, Foreground = _textSecondaryBrush,
                    Margin = new Thickness(0, 1, 0, 1),
                });
            }

            // Build Tools
            AddSectionHeader(stack, "BUILD TOOLS", 16);
            foreach (var tool in new[] { ".NET 8.0 - Runtime", "Avalonia UI - Cross-platform", "Claude Code - AI Tool (Anthropic)" })
            {
                stack.Children.Add(new TextBlock
                {
                    Text = tool, FontSize = 11, Foreground = _textSecondaryBrush,
                    Margin = new Thickness(0, 1, 0, 1),
                });
            }

            // Support
            AddSectionHeader(stack, "SUPPORT THE PROJECT", 16);
            stack.Children.Add(new TextBlock
            {
                Text = "I don't accept donations. Please donate to Peter Han or the original "
                     + "EVEMon Dev Team who built this tool.",
                FontSize = 11, Foreground = _textSecondaryBrush, TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
            });

            // Links
            var linksPanel = new StackPanel { Spacing = 4, Margin = new Thickness(0, 16, 0, 0) };
            AddLinkButton(linksPanel, "evelens.dev", BuildInfo.Website);
            AddLinkButton(linksPanel, "github.com/aliacollins/evelens", BuildInfo.Repository);
            stack.Children.Add(linksPanel);

            return stack;
        }

        private Control BuildContributorsTab()
        {
            var stack = new StackPanel { Spacing = 0 };

            stack.Children.Add(new TextBlock
            {
                Text = "Every person who contributed to EVEMon across its history.",
                FontSize = 11, Foreground = _textDisabledBrush, TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12),
            });

            // Original Creator
            AddSectionHeader(stack, "ORIGINAL CREATOR");
            stack.Children.Add(BuildChipWrap(new[] { "Six Anari" }, _accentBrush));
            AddSpacer(stack, 8);

            // Active Developer
            AddSectionHeader(stack, "ACTIVE DEVELOPER");
            stack.Children.Add(BuildChipWrap(new[] { "Alia Collins" }, _accentBrush));
            AddSpacer(stack, 8);

            // Developers (Retired)
            AddSectionHeader(stack, "DEVELOPERS (RETIRED)");
            stack.Children.Add(BuildChipWrap(new[]
            {
                "Peter Han", "Blitz Bandis", "Jimi", "Araan Sunn",
                "Six Anari", "Anders Chydenius", "Brad Stone",
                "Eewec Ourbyni", "Richard Slater", "Vehlin",
                "Collin Grady", "DCShadow", "DonQuiche", "Grauw",
                "Jalon Mevek", "Labogh", "romanl", "Safrax",
                "Stevil Knevil", "TheBelgarion"
            }, _textPrimaryBrush));
            AddSpacer(stack, 8);

            // Consultants
            AddSectionHeader(stack, "CONSULTANTS");
            stack.Children.Add(BuildChipWrap(new[]
            {
                "Desmont McCallock", "Tonto Aansen", "Saeka Tansen",
                "MrCue", "Candle"
            }, _textPrimaryBrush));
            AddSpacer(stack, 8);

            // Community
            AddSectionHeader(stack, "COMMUNITY");
            stack.Children.Add(BuildChipWrap(new[]
            {
                "Adrienne Adler", "Torgo",
                "alebrophy", "DiagonalyStraight", "Lukas Friedrichsen"
            }, _textPrimaryBrush));
            AddSpacer(stack, 8);

            // AI Tool
            AddSectionHeader(stack, "AI TOOL");
            stack.Children.Add(BuildChipWrap(new[] { "Claude Code (Anthropic)" }, _successGreenBrush));

            return stack;
        }

        private WrapPanel BuildChipWrap(string[] names, IBrush foreground)
        {
            var wrap = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
            foreach (var name in names)
                wrap.Children.Add(BuildChip(name, foreground));
            return wrap;
        }

        private Control BuildChip(string name, IBrush foreground)
        {
            var avatarColor = foreground is SolidColorBrush scb ? scb.Color : Colors.Gray;

            var border = new Border
            {
                CornerRadius = new CornerRadius(12), Padding = new Thickness(3, 2, 10, 2),
                Margin = new Thickness(0, 2, 6, 2),
                Background = _bgMediumBrush, BorderBrush = _borderBrush,
                BorderThickness = new Thickness(1),
            };

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };

            var avatar = new Border
            {
                Width = 16, Height = 16, CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(avatarColor, 0.15),
                BorderBrush = new SolidColorBrush(avatarColor, 0.3),
                BorderThickness = new Thickness(1),
            };
            avatar.Child = new TextBlock
            {
                Text = name[..1], FontSize = 7, FontWeight = FontWeight.Bold,
                Foreground = foreground, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(avatar);

            panel.Children.Add(new TextBlock
            {
                Text = name, FontSize = 10, Foreground = foreground,
                VerticalAlignment = VerticalAlignment.Center,
            });

            border.Child = panel;
            return border;
        }

        private void AddSectionHeader(StackPanel parent, string text, double topMargin = 0)
        {
            parent.Children.Add(new TextBlock
            {
                Text = text, FontSize = 10, FontWeight = FontWeight.SemiBold,
                Foreground = _accentBrush, LetterSpacing = 1.5,
                Margin = new Thickness(0, topMargin, 0, 2),
            });
        }

        private static void AddSpacer(StackPanel parent, double height)
        {
            parent.Children.Add(new Border { Height = height });
        }

        private void AddLinkButton(StackPanel parent, string label, string url)
        {
            var btn = new Button
            {
                Content = label, FontSize = 11, Padding = new Thickness(0),
                Background = Brushes.Transparent, BorderThickness = new Thickness(0),
                Foreground = _accentSecondaryBrush, Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            btn.Click += (_, _) => OpenUrl(url);
            parent.Children.Add(btn);
        }

        private Control BuildLegalTab()
        {
            var stack = new StackPanel { Spacing = 0 };

            // GPL box
            var gplBox = new Border
            {
                CornerRadius = new CornerRadius(6), Padding = new Thickness(16, 14),
                Background = _bgMediumBrush, BorderBrush = _borderBrush,
                BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 0, 16),
            };
            var gplStack = new StackPanel { Spacing = 6 };
            gplStack.Children.Add(new TextBlock
            {
                Text = "GNU General Public License v2", FontSize = 12,
                FontWeight = FontWeight.SemiBold, Foreground = _textPrimaryBrush,
            });
            gplStack.Children.Add(new TextBlock
            {
                Text = "EveLens is free software: you can redistribute it and/or modify it under the "
                     + "terms of the GNU General Public License as published by the Free Software Foundation, "
                     + "either version 2 of the License, or (at your option) any later version.",
                FontSize = 11, Foreground = _textSecondaryBrush,
                TextWrapping = TextWrapping.Wrap, LineHeight = 20,
            });
            gplStack.Children.Add(new TextBlock
            {
                Text = "This program is distributed in the hope that it will be useful, but WITHOUT ANY "
                     + "WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR "
                     + "A PARTICULAR PURPOSE.",
                FontSize = 11, Foreground = _textSecondaryBrush,
                TextWrapping = TextWrapping.Wrap, LineHeight = 20,
                Margin = new Thickness(0, 4, 0, 0),
            });
            gplBox.Child = gplStack;
            stack.Children.Add(gplBox);

            // Attribution
            AddSectionHeader(stack, "ATTRIBUTION");
            var attrStack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 16) };
            var attributions = new[]
            {
                ("Originally created by ", "EVEMon Dev Team", " (2006)"),
                ("Maintained by ", "Jimi, Peter Han", " and community contributors"),
                ("EveLens by ", "Alia Collins", " (2026)"),
            };
            foreach (var (prefix, highlight, suffix) in attributions)
            {
                var line = new StackPanel { Orientation = Orientation.Horizontal };
                line.Children.Add(new TextBlock { Text = prefix, FontSize = 11, Foreground = _textSecondaryBrush });
                line.Children.Add(new TextBlock { Text = highlight, FontSize = 11, Foreground = _textPrimaryBrush });
                if (!string.IsNullOrEmpty(suffix))
                    line.Children.Add(new TextBlock { Text = suffix, FontSize = 11, Foreground = _textSecondaryBrush });
                attrStack.Children.Add(line);
            }
            stack.Children.Add(attrStack);

            // Copyright
            stack.Children.Add(new TextBlock
            {
                Text = BuildInfo.Copyright, FontSize = 9,
                Foreground = _textDisabledBrush, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4),
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Licensed under GPL v2", FontSize = 9, Foreground = _textDisabledBrush,
            });
            stack.Children.Add(new TextBlock
            {
                Text = "EVE Online and all related logos are trademarks of CCP hf.",
                FontSize = 9, Foreground = _textDisabledBrush, TextWrapping = TextWrapping.Wrap,
            });

            // Links
            var linksWrap = new WrapPanel { Margin = new Thickness(0, 16, 0, 0) };
            var links = new[]
            {
                ("Source Code", BuildInfo.Repository),
                ("Website", BuildInfo.Website),
                ("Report a Bug", BuildInfo.Repository + "/issues"),
            };
            foreach (var (label, url) in links)
            {
                var linkBtn = new Border
                {
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12, 5),
                    Margin = new Thickness(0, 0, 8, 8),
                    Background = _bgMediumBrush, BorderBrush = _borderBrush,
                    BorderThickness = new Thickness(1),
                    Cursor = new Cursor(StandardCursorType.Hand),
                };
                var linkPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                linkPanel.Children.Add(new TextBlock { Text = label, FontSize = 10, Foreground = _successGreenBrush });
                linkPanel.Children.Add(new TextBlock { Text = "\u2197", FontSize = 10, Foreground = _textDisabledBrush });
                linkBtn.Child = linkPanel;

                if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    linkBtn.PointerPressed += (_, _) => OpenUrl(url);
                }
                linksWrap.Children.Add(linkBtn);
            }
            stack.Children.Add(linksWrap);

            return stack;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Util.OpenURL(new Uri(url));
            }
            catch
            {
                // Silently fail if browser cannot be opened
            }
        }
    }
}
