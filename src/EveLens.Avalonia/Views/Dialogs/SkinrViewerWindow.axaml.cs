// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.Dialogs
{
    /// <summary>
    /// SKINR Viewer (experimental): scope-gated per character, lists owned SKINR
    /// licenses, resolves design recipes from ESI. The render pane is reserved for
    /// the Trinity-based high-fidelity renderer; until that sidecar lands it shows
    /// an honest placeholder while the data pipeline is fully live.
    /// </summary>
    public partial class SkinrViewerWindow : Window
    {
        private readonly SkinrViewerViewModel _vm = new();
        private bool _suppressSelection;

        public SkinrViewerWindow()
        {
            InitializeComponent();
            ApplyPlatformSupport();

            _vm.StateChanged += () => Dispatcher.UIThread.Post(RefreshFromViewModel);

            var characters = AppServices.Characters.Where(c => c.Monitored)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            CharacterCombo.ItemsSource = characters.Select(c => c.Name).ToList();
            CharacterCombo.Tag = characters;
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.Dispose();
            base.OnClosed(e);
        }

        private async void OnCharacterChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CharacterCombo.Tag is not System.Collections.Generic.List<Character> characters)
                    return;
                int index = CharacterCombo.SelectedIndex;
                var character = index >= 0 && index < characters.Count ? characters[index] : null;
                await _vm.SelectCharacterAsync(character);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: character select failed: {ex.Message}");
            }
        }

        private async void OnLicenseSelected(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_suppressSelection) return;
                var entry = (LicenseList.SelectedItem as ListBoxItem)?.Tag as SkinrLicenseEntry;
                await _vm.SelectDesignAsync(entry?.SkinrId);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: design select failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Honest platform messaging in the render pane: DirectX renderer on Windows
        /// x64, Metal planned on Apple Silicon, explicit "not available" for Linux
        /// and Intel Macs. The data half of the window works on every platform.
        /// </summary>
        private void ApplyPlatformSupport()
        {
            switch (SkinrRenderPlatform.Current)
            {
                case SkinrRenderSupport.Supported:
                    // Windows: keep the standard placeholder until the renderer lands
                    break;
                case SkinrRenderSupport.MacArmPlanned:
                    RenderTitle.Text = Loc.Get("Skinr.RenderMacTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderMacDesc");
                    break;
                case SkinrRenderSupport.UnsupportedMacIntel:
                    RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderMacIntelDesc");
                    break;
                case SkinrRenderSupport.UnsupportedLinux:
                    RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderLinuxDesc");
                    break;
                default:
                    RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderLinuxDesc");
                    break;
            }
        }

        /// <summary>
        /// Drives the status strip during asset downloads — called by the renderer
        /// pipeline with 0.0–1.0 progress (cache hits jump straight to done).
        /// </summary>
        internal void ReportDownload(string what, double fraction)
        {
            Dispatcher.UIThread.Post(() =>
            {
                bool active = fraction < 1.0;
                DownloadProgress.IsVisible = active;
                DownloadProgress.Value = fraction * 100;
                RenderStatusText.Text = active
                    ? string.Format(Loc.Get("Skinr.StatusDownloading"), what, (int)(fraction * 100))
                    : Loc.Get("Skinr.StatusReady");
            });
        }

        private void RefreshFromViewModel()
        {
            var state = _vm.State;
            ScopeMissingPanel.IsVisible = state == SkinrViewerViewModel.ViewState.ScopeMissing;
            LoadingText.IsVisible = state == SkinrViewerViewModel.ViewState.LoadingInventory;
            ErrorText.IsVisible = state == SkinrViewerViewModel.ViewState.Error;
            InventoryPanel.IsVisible = state == SkinrViewerViewModel.ViewState.Loaded;

            if (state == SkinrViewerViewModel.ViewState.Error)
                ErrorText.Text = _vm.ErrorMessage;

            if (state == SkinrViewerViewModel.ViewState.Loaded)
            {
                InventoryHeader.Text = string.Format(
                    Loc.Get("Skinr.InventoryCount"), _vm.Licenses.Count);

                _suppressSelection = true;
                LicenseList.ItemsSource = _vm.Licenses
                    .Select(l => new ListBoxItem
                    {
                        Content = $"{l.ShortId}   ({l.StatusText})",
                        Tag = l
                    })
                    .ToList();
                LicenseList.SelectedIndex = -1;
                _suppressSelection = false;
            }

            DesignSummary.Text = _vm.SelectedRecipe != null
                ? _vm.DescribeSelectedRecipe()
                : Loc.Get("Skinr.SelectDesign");
        }
    }
}
