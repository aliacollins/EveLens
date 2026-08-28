// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    /// <summary>
    /// Offers to repair a Gatekeeper-translocated macOS install.
    /// </summary>
    /// <remarks>
    /// Shown at startup whenever the app is running from a read-only App Translocation
    /// mount — the state in which every in-place update silently fails after the app
    /// has already exited (so no error can ever be shown at update time). The repair
    /// is user-consented: nothing happens unless Fix is clicked. "Not Now" defers to
    /// the next launch rather than being remembered — this is a broken install, not a
    /// preference, and it stays broken until repaired.
    /// </remarks>
    public partial class MacInstallRepairWindow : Window
    {
        public MacInstallRepairWindow()
        {
            InitializeComponent();

            NotNowButton.Click += (_, _) => Close();
            FixButton.Click += (_, _) =>
            {
                if (AppServices.MacInstall.HealAndRelaunch())
                {
                    // A fresh, un-translocated instance is starting (with a startup
                    // delay so this one can release the single-instance signal).
                    AppServices.ApplicationLifecycle.Exit();
                    return;
                }

                // The repair could not complete (copy or xattr failed) — swap the
                // explanation for manual instructions instead of closing silently.
                BodyText.Text = Loc.Get("MacInstall.HealFailed");
                FixButton.IsVisible = false;
            };
        }
    }
}
