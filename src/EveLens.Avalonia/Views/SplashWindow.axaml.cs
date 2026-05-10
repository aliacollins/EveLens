// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            TaglineText.Text = Loc.Get("Splash.Tagline");
        }

        public void UpdateStatus(string text)
        {
            StatusTextBlock.Text = text;
        }
    }
}
