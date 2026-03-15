// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using EveLens.Avalonia.Converters;
using EveLens.Common.Events;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorHeader : UserControl
    {
        private static readonly SolidColorBrush OmegaBorderBrush = new(Color.Parse("#FFE6A817"));
        private static readonly SolidColorBrush AlphaBorderBrush = new(Color.Parse("#FFFF6D00"));
        private static readonly SolidColorBrush OmegaTextBrush = new(Color.Parse("#FF00C853"));
        private static readonly SolidColorBrush AlphaTextBrush = new(Color.Parse("#FFFF6D00"));

        private static readonly string[] OverrideLabels = { "Auto-detect", "Alpha override", "Omega override" };

        private ObservableCharacter? _observable;
        private IDisposable? _privacySub;

        public CharacterMonitorHeader()
        {
            InitializeComponent();

            // Wire flyout menu items
            AutoDetectItem.Click += (_, _) => SetAccountOverride(0);
            EmulateAlphaItem.Click += (_, _) => SetAccountOverride(1);
            EmulateOmegaItem.Click += (_, _) => SetAccountOverride(2);
        }

        protected override async void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            try
            {
                if (_observable != null)
                    _observable.PropertyChanged -= OnObservableChanged;

                if (DataContext is ObservableCharacter oc && oc.CharacterID > 0)
                {
                    _observable = oc;

                    // Portrait
                    var image = await ImageService.GetCharacterImageAsync(oc.CharacterID);
                    if (image != null)
                    {
                        var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                            image, typeof(Bitmap), null, CultureInfo.InvariantCulture);
                        if (converted is Bitmap bitmap)
                        {
                            (PortraitImage.Source as IDisposable)?.Dispose();
                            PortraitImage.Source = bitmap;
                        }
                    }

                    // Race/Bloodline (static — set once)
                    var c = oc.Character;
                    bool hasRace = !string.IsNullOrEmpty(c.Race)
                                   && !c.Race.Equals("(unset)", StringComparison.OrdinalIgnoreCase);
                    RaceText.Text = hasRace ? $"{c.Gender} - {c.Race} - {c.Bloodline}" : "";
                    RaceText.IsVisible = hasRace;

                    RefreshDisplay(oc);
                    oc.PropertyChanged += OnObservableChanged;

                    _privacySub?.Dispose();
                    _privacySub = AppServices.EventAggregator?.Subscribe<PrivacyModeChangedEvent>(
                        _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (_observable != null) RefreshDisplay(_observable);
                        }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading header: {ex}");
            }
        }

        private void OnObservableChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is ObservableCharacter oc)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() => RefreshDisplay(oc));
        }

        private void RefreshDisplay(ObservableCharacter oc)
        {
            try
            {
                // Location
                LocationText.Text = $"Located in: {oc.LocationText}";
                DockedText.Text = !string.IsNullOrEmpty(oc.DockedText)
                    ? $"Docked at: {oc.DockedText}"
                    : "In space";

                // Alliance (always shown — em dash when none to prevent layout shift)
                if (PrivacyHelper.IsCorpAllianceHidden)
                {
                    AllianceText.Text = $"Alliance: {PrivacyHelper.Mask}";
                }
                else
                {
                    bool hasAlliance = !string.IsNullOrEmpty(oc.AllianceName)
                                       && !oc.AllianceName.Equals("(None)", StringComparison.OrdinalIgnoreCase);
                    AllianceText.Text = hasAlliance
                        ? $"Alliance: {oc.AllianceName}"
                        : "Alliance: \u2014";
                }

                // Balance change flash
                if (oc.BalanceDirection != 0 && BalanceChangeIndicator != null)
                {
                    BalanceChangeIndicator.Foreground = oc.BalanceDirection > 0
                        ? Brushes.LimeGreen : Brushes.OrangeRed;
                    BalanceChangeIndicator.Opacity = 1.0;
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(
                        () => BalanceChangeIndicator.Opacity = 0.7,
                        global::Avalonia.Threading.DispatcherPriority.Background);
                }

                // Account status — portrait border + label
                var effectiveStatus = oc.Character.EffectiveCharacterStatus;
                bool isOmega = effectiveStatus == AccountStatus.Omega;
                PortraitBorder.BorderBrush = isOmega ? OmegaBorderBrush : AlphaBorderBrush;
                AccountStatusLabel.Text = isOmega ? "\u03A9 Omega" : "\u03B1 Alpha";
                AccountStatusLabel.Foreground = isOmega ? OmegaTextBrush : AlphaTextBrush;

                // Override mode label
                int overrideIndex = (int)oc.Character.AccountStatusSettings;
                OverrideModeLabel.Text = overrideIndex >= 0 && overrideIndex < OverrideLabels.Length
                    ? OverrideLabels[overrideIndex] : OverrideLabels[0];

                // Inline stats — per-category privacy
                {
                    var inv = CultureInfo.InvariantCulture;
                    string skills = PrivacyHelper.IsSkillPointsHidden
                        ? $"Skills: {PrivacyHelper.Mask}" : $"Skills: {oc.KnownSkillCount.ToString("N0", inv)}";
                    string sp = PrivacyHelper.IsSkillPointsHidden
                        ? $"SP: {PrivacyHelper.Mask}" : $"SP: {ObservableCharacter.FormatLargeNumber(oc.SkillPoints)}";
                    string freeSp = PrivacyHelper.IsSkillPointsHidden
                        ? $"Free SP: {PrivacyHelper.Mask}" : $"Free SP: {ObservableCharacter.FormatLargeNumber(oc.FreeSkillPoints)}";
                    string remaps = PrivacyHelper.IsRemapsHidden
                        ? $"Remaps: {PrivacyHelper.Mask}" : $"Remaps: {oc.AvailableRemaps}";
                    StatsLine.Text = string.Join("  \u00b7  ", skills, sp, freeSp, remaps);
                }
            }
            catch
            {
                // Non-critical display
            }
        }

        private void SetAccountOverride(int index)
        {
            try
            {
                if (_observable == null) return;
                _observable.Character.AccountStatusSettings = (AccountStatusMode)index;
                RefreshDisplay(_observable);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing account status: {ex}");
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _privacySub?.Dispose();
            _privacySub = null;
            if (_observable != null)
            {
                _observable.PropertyChanged -= OnObservableChanged;
                _observable = null;
            }
        }
    }
}
