// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;
using EveLens.Common;
using EveLens.Common.Enumerations.UISettings;

namespace EveLens.Avalonia.Views.Dialogs
{
    /// <summary>
    /// One-time opt-in ask for automatic update installation (Discussion #100).
    /// Shown once when AutoInstallUpdates is still NotAsked; either choice is stored so the
    /// user is never asked again, and both remain editable in Settings > Data.
    /// </summary>
    public partial class AutoUpdateOptInWindow : Window
    {
        public AutoUpdateOptInWindow()
        {
            InitializeComponent();

            OptInButton.Click += (_, _) => CloseWith(AutoInstallUpdates.Automatic);
            NotifyOnlyButton.Click += (_, _) => CloseWith(AutoInstallUpdates.NotifyOnly);

            // Closing the window without choosing = notify-only; the conservative default,
            // still recorded so we never re-ask (no spam). Only counts if the window actually
            // OPENED — a dialog that failed to show must not burn the one-time ask (that
            // silently recorded an answer the user never saw).
            bool wasShown = false;
            Opened += (_, _) => wasShown = true;
            Closing += (_, _) =>
            {
                if (wasShown &&
                    Settings.Updates.AutoInstallUpdates == AutoInstallUpdates.NotAsked)
                {
                    Settings.Updates.AutoInstallUpdates = AutoInstallUpdates.NotifyOnly;
                    Settings.Save();
                }
            };
        }

        private void CloseWith(AutoInstallUpdates choice)
        {
            Settings.Updates.AutoInstallUpdates = choice;
            Settings.Save();
            Close();
        }
    }
}
