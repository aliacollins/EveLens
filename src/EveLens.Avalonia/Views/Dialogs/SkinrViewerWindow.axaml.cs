// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EveLens.Avalonia.Converters;
using EveLens.Avalonia.Services;
using EveLens.Common;
using EveLens.Common.Events;
using EveLens.Common.Helpers;
using EveLens.Common.Service;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.Dialogs
{
    /// <summary>
    /// The SKINR Hub: a full-bleed 3D stage with the design carousel, search, hull identity
    /// overlay, and environment presets — the app's storefront for a character's designs.
    /// Scope-gated per character; recipes resolve from ESI; the Trinity sidecar renders.
    /// </summary>
    public partial class SkinrViewerWindow : Window
    {
        // Offered in the picker cheapest-first, so the list reads as a cost order.
        private static readonly SkinrRenderQuality[] s_qualities =
        {
            SkinrRenderQuality.Preview,
            SkinrRenderQuality.Balanced,
            SkinrRenderQuality.High
        };

        // The two measured options lead, because they are the ones that cannot be wrong: they take
        // their aspect from something real. The fixed sizes follow smallest-first.
        private static readonly SkinrRenderResolution[] s_resolutions =
        {
            SkinrRenderResolution.MatchViewport,
            SkinrRenderResolution.MatchDisplay,
            SkinrRenderResolution.Hd720,
            SkinrRenderResolution.Fhd1080,
            SkinrRenderResolution.Qhd1440,
            SkinrRenderResolution.Uhd2160
        };

        private readonly SkinrHubViewModel _hub = new();
        private readonly SkinrRenderViewModel _render = new();
        private readonly SkinrMarketViewModel _market = new();

        // The market pane redraws at most this often while the recipe walk streams in
        // names — a full grid rebuild per resolved recipe would churn ~6 times a second.
        private static readonly TimeSpan MarketRefreshDebounce = TimeSpan.FromMilliseconds(400);
        private bool _marketRefreshPending;

        private bool _suppressQuality;
        private bool _suppressResolution;
        private WriteableBitmap? _surface;
        private Point? _dragOrigin;
        private string? _thumbSavedFor;
        private bool _thumbArmed;
        private IDisposable? _idNamesSub;
        private string? _selectedSkinrId;
        private SkinrThumbnailPrerenderer? _prerenderer;
        private readonly SkinrCardBitmapCache _cardBitmaps = new();
        private readonly SkinrHubPreferences _hubPrefs = SkinrHubPreferences.Load();
        private bool _suppressCdnToggle;

        public SkinrViewerWindow()
        {
            InitializeComponent();
            _suppressCdnToggle = true;
            CdnToggle.IsChecked = _hubPrefs.CommunityPreviews == true;
            _suppressCdnToggle = false;
            ApplyDisplayTypography();
            ApplyPlatformSupport();
            PopulateQualities();
            PopulateResolutions();
            BuildEnvironmentSwitcher();
            InitMarketList();

            _market.CommunityCatalog = () => _hubPrefs.CommunityPreviews == true;
            _hub.Data.StateChanged += () => Dispatcher.UIThread.Post(RefreshFromViewModel);
            _hub.CarouselChanged += () => Dispatcher.UIThread.Post(RefreshCarousel);
            _render.FrameReady += frame => Dispatcher.UIThread.Post(() => ShowFrame(frame));
            _render.StatusChanged += text => Dispatcher.UIThread.Post(() =>
            {
                // The discovery report names environment variables and local paths —
                // developer material for the trace, never the status pill. This
                // regressed once already (macOS, verbatim paths in the strip).
                string shown = text != null && text.Contains("Searched:")
                    ? Loc.Get("Skinr.RenderFailedShort")
                    : text ?? string.Empty;
                RenderStatusText.Text = shown;
                // While the stage is empty and a build is in flight, the engine's own
                // narration ("Starting the renderer — this takes up to a minute the
                // first time...") belongs front and center, not in the corner pill.
                if (_stageWaiting && !_overlayFromDownload)
                {
                    LoadingOverlayText.Text = shown;
                    LoadingOverlay.IsVisible = !MarketPane.IsVisible;
                    // The render VM reports failures as status text (it never throws
                    // across the event boundary); a failure ends the wait.
                    if (text != null &&
                        text.StartsWith("Render failed", StringComparison.Ordinal))
                        EndStageWait();
                }
            });
            _render.DownloadProgress += fraction => ReportDownload(
                Loc.Get("Skinr.RenderPlaceholderTitle"), fraction);
            _render.DiagnosticsChanged += () => Dispatcher.UIThread.Post(RefreshRenderDiagnostics);
            // The ship-spin counter: the docked capsuleer's oldest pastime, kept alive.
            // Marshalled because the inertia glide spins off the UI thread too.
            _render.Camera.SpinCountChanged += spins => Dispatcher.UIThread.Post(() =>
            {
                SpinCounterText.IsVisible = true;
                SpinCounterText.Text = string.Format(Loc.Get("Skinr.SpinsFmt"), spins);
            });
            // Recipe creators resolve through the shared ID→name pipeline; when a batch
            // of names lands, a visible details panel owes its Designer row a refresh —
            // and so do the market cards' "by <creator>" lines.
            _idNamesSub = AppServices.EventAggregator?.Subscribe<EveIDToNameUpdatedEvent>(
                _ => Dispatcher.UIThread.Post(() =>
                {
                    if (DetailsPanel.IsVisible)
                        RefreshDetailsPanel();
                    if (MarketPane.IsVisible)
                        QueueMarketRefresh();
                }));
            _market.MarketChanged += () => Dispatcher.UIThread.Post(QueueMarketRefresh);

            // The placeholder scroller must stop where the bottom band begins, and
            // the band's height varies (carousel, env switcher, notice, font scale)
            // — so the margin tracks the measured height instead of guessing one.
            LayoutUpdated += (_, _) =>
            {
                double bottom = BottomBand.IsVisible ? BottomBand.Bounds.Height + 6 : 0;
                var margin = new Thickness(0, 0, 0, bottom);
                if (PlaceholderScroller.Margin != margin)
                    PlaceholderScroller.Margin = margin;
            };

            CharacterCombo.ItemTemplate = BuildCharacterOptionTemplate();

            // Fresh SKINR license counts land per character on the monitor's cadence;
            // a visible landing keeps its card grid current as they do.
            _skinrDataSub = AppServices.EventAggregator?.Subscribe<CharacterSkinrUpdatedEvent>(
                _ => Dispatcher.UIThread.Post(() =>
                {
                    if (LandingPane.IsVisible)
                        BuildLandingGrid();
                }));

            // First open (or the remembered character is gone): the landing asks WHOSE
            // collection instead of silently guessing first-alphabetical — the guess is
            // exactly what read as "not working" (Issue #139).
            long remembered = Settings.UI.SkinrLastCharacterId;
            string rememberedName = remembered == 0 ? null : AppServices.Characters
                .Where(c => c.Monitored)
                .OfType<CCPCharacter>()
                .FirstOrDefault(c => c.CharacterID == remembered)?.Name;
            RebuildCharacterPicker(rememberedName, allowNone: rememberedName == null);
            if (rememberedName == null)
                ShowLanding();
        }

        private IDisposable? _skinrDataSub;

        /// <summary>A picker row: the character, and whether their token carries the
        /// SKINR scope. Display-only.</summary>
        private sealed record CharacterOption(Character Character, bool HasScope)
        {
            public string Name => Character.Name;
        }

        /// <summary>
        /// Populates the character picker with access markers — characters whose tokens
        /// lack the SKINR scope show dimmed with a "needs access" tag, so it's visible
        /// BEFORE selecting them why their collection would be gated (Issue #139).
        /// </summary>
        /// <param name="selectName">Character to land on; null keeps/starts at the first.
        /// Auto-selecting the first character: an empty picker meant an empty Collection
        /// that read as a bug ("where are my ships?"), and the Hub half works without a
        /// character anyway — this only ever adds data.</param>
        /// <param name="allowNone">When true, no fallback selection is made — used while
        /// the landing is the chooser, so no character's collection loads before the user
        /// has actually chosen one.</param>
        private void RebuildCharacterPicker(string? selectName = null, bool allowNone = false)
        {
            var characters = AppServices.Characters.Where(c => c.Monitored)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            CharacterCombo.ItemsSource = characters
                .Select(c => new CharacterOption(c, SkinrViewerViewModel.HasSkinrScope(c)))
                .ToList();
            CharacterCombo.Tag = characters;

            int index = selectName == null
                ? -1
                : characters.FindIndex(c => string.Equals(
                    c.Name, selectName, StringComparison.OrdinalIgnoreCase));
            if (index < 0 && characters.Count > 0 && !allowNone)
                index = 0;
            CharacterCombo.SelectedIndex = index;
        }

        private global::Avalonia.Controls.Templates.FuncDataTemplate<CharacterOption>
            BuildCharacterOptionTemplate() => new((option, _) =>
        {
            if (option == null)
                return new TextBlock();

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
            panel.Children.Add(new TextBlock
            {
                Text = option.Name,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (IBrush?)Resources[option.HasScope
                    ? "SkinrTextBrush" : "SkinrTextDimBrush"],
            });
            if (!option.HasScope)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = Loc.Get("Skinr.NeedsAccess"),
                    FontSize = FontScaleService.Caption,
                    FontStyle = FontStyle.Italic,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = (IBrush?)Resources["SkinrAccentBrush"],
                });
            }
            return panel;
        });

        // --- landing (whose collection?) ----------------------------------------

        private void ShowLanding()
        {
            LandingPane.IsVisible = true;
            // One search at a time: the top bar's design search is meaningless while
            // the landing (with its character search) owns the stage
            SearchPill.IsVisible = false;
            BuildLandingGrid();
        }

        private void HideLanding()
        {
            // Entering the studio mid-wizard (a scoped card was clicked) completes
            // it — the wizard informs, it never traps.
            if (_wizardStep != WizardStep.None)
            {
                _wizardStep = WizardStep.None;
                _wizardDone = true;
                SetRailLocked(false);
            }
            LandingPane.IsVisible = false;
            SearchPill.IsVisible = true;
        }

        private void OnLandingSearchChanged(object? sender, TextChangedEventArgs e)
            => BuildLandingGrid();

        /// <summary>
        /// The landing's card grid: every character with SKINR access, up front and
        /// unconditionally — access is the membership test, not the owned-design count
        /// (a count of 0 may just mean "not fetched yet", and hiding people behind the
        /// search box read as an empty page, #139). Characters without access follow,
        /// dimmed, as their own grant-access entry. Search filters by name.
        /// </summary>
        private void BuildLandingGrid()
        {
            LandingGrid.Children.Clear();
            LandingNeedsGrid.Children.Clear();

            var all = AppServices.Characters.Where(c => c.Monitored)
                .OfType<CCPCharacter>()
                .ToList();
            long lastUsed = Settings.UI.SkinrLastCharacterId;
            string filter = LandingSearchBox.Text?.Trim() ?? string.Empty;

            // True first run — nobody has the SKINR scope yet: the landing becomes a
            // three-step wizard (meet SKINR → grant access → 3D renderer) and the
            // rail stays locked until it finishes. Once anyone has access, the
            // landing opens straight on the chooser.
            bool firstRun = !all.Any(SkinrViewerViewModel.HasSkinrScope);
            if (_wizardStep == WizardStep.None && firstRun && !_wizardDone)
                _wizardStep = WizardStep.Intro;

            List<CCPCharacter> shown = all
                .Where(c => filter.Length == 0 ||
                    c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var withAccess = shown
                .Where(SkinrViewerViewModel.HasSkinrScope)
                .OrderByDescending(c => c.CharacterID == lastUsed)
                .ThenByDescending(c => c.SkinrLicenses.Count)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var needsAccess = shown
                .Where(c => !SkinrViewerViewModel.HasSkinrScope(c))
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var character in withAccess)
                LandingGrid.Children.Add(BuildLandingCard(character));
            foreach (var character in needsAccess)
                LandingNeedsGrid.Children.Add(BuildLandingCard(character));

            LandingAccessHeader.IsVisible = withAccess.Count > 0 && needsAccess.Count > 0;
            LandingNeedsHeader.IsVisible = needsAccess.Count > 0;

            // An empty grid needs an explanation only when there ARE characters to
            // explain away. On a brand-new install the grant row below already says
            // "no characters yet" — "grant access to a character below" would be
            // pointing at nobody.
            if (shown.Count == 0 && all.Count > 0)
            {
                LandingGrid.Children.Add(new TextBlock
                {
                    Text = Loc.Get(filter.Length > 0
                        ? "Skinr.LandingNoMatch" : "Skinr.LandingEmpty"),
                    FontSize = FontScaleService.Body,
                    Foreground = (IBrush?)Resources["SkinrTextDimBrush"],
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    MaxWidth = 420,
                    Margin = new Thickness(0, 30),
                });
            }

            // Search only earns its pixels on a big roster — with a handful of
            // characters (or none at all) every card is already on screen, and a
            // "find a character" box over an empty page read as absurd (#139).
            LandingSearchPill.IsVisible = all.Count > 8;

            int unscoped = all.Count(c => !SkinrViewerViewModel.HasSkinrScope(c));
            if (all.Count == 0)
            {
                // Brand-new install: no characters at all. The old logic proudly
                // reported "every character has granted access" over an empty page
                // and HID the only button that could fix it.
                LandingGrantText.Text = Loc.Get("Skinr.LandingNoCharactersYet");
                LandingGrantButton.IsVisible = true;
            }
            else
            {
                LandingGrantText.Text = unscoped > 0
                    ? string.Format(Loc.Get("Skinr.LandingGrantFmt"), unscoped)
                    : Loc.Get("Skinr.LandingGrantAll");
                LandingGrantButton.IsVisible = unscoped > 0;
            }

            ApplyWizardState(all.Count);
        }

        private Control BuildLandingCard(CCPCharacter character)
        {
            bool hasScope = SkinrViewerViewModel.HasSkinrScope(character);
            int designs = hasScope ? character.SkinrLicenses.Count : 0;

            var card = new Border
            {
                Width = 168,
                Background = (IBrush?)Resources["SkinrPanelBrush"],
                BorderBrush = (IBrush?)Resources["SkinrBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 14, 14, 12),
                Margin = new Thickness(6),
                Cursor = new Cursor(StandardCursorType.Hand),
            };

            var portrait = new Image { Width = 64, Height = 64, Stretch = Stretch.UniformToFill };
            var portraitFrame = new Border
            {
                Width = 64, Height = 64,
                CornerRadius = new CornerRadius(32),
                ClipToBounds = true,
                Background = (IBrush?)Resources["SkinrPillBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = portrait,
            };
            LoadLandingPortraitAsync(portrait, character);

            // "no designs yet" is a claim we can only make AFTER a fetch — an
            // unfetched collection shows a neutral counting state instead (#139)
            string stateText = !hasScope
                ? Loc.Get("Skinr.NeedsAccess")
                : !character.SkinrLicenses.IsFetched
                    ? Loc.Get("Skinr.LandingCounting")
                    : designs > 0
                        ? string.Format(Loc.Get("Skinr.LandingDesignsFmt"), designs)
                        : Loc.Get("Skinr.LandingNoDesigns");
            IBrush? stateBrush = !hasScope
                ? (IBrush?)Resources["SkinrAccentBrush"]
                : designs > 0
                    ? (IBrush?)Resources["SkinrTextSecBrush"]
                    : (IBrush?)Resources["SkinrTextDimBrush"];

            var stack = new StackPanel { Spacing = 7 };
            stack.Children.Add(portraitFrame);
            stack.Children.Add(new TextBlock
            {
                Text = character.Name,
                FontSize = FontScaleService.Body,
                FontWeight = FontWeight.SemiBold,
                Foreground = (IBrush?)Resources["SkinrTextBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            stack.Children.Add(new TextBlock
            {
                Text = stateText,
                FontSize = FontScaleService.Caption,
                Foreground = stateBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            card.Child = stack;

            card.PointerPressed += (_, _) =>
            {
                if (hasScope)
                    PickCharacter(character);
                else
                    OnScopeUpdate(card, new global::Avalonia.Interactivity.RoutedEventArgs());
            };
            return card;
        }

        /// <summary>The landing's exit: remember the choice, drop the pane, and let the
        /// picker's selection change drive the load (index always changes — the landing
        /// keeps the combo unselected).</summary>
        private void PickCharacter(CCPCharacter character)
        {
            Settings.UI.SkinrLastCharacterId = character.CharacterID;
            Settings.Save();
            HideLanding();

            if (CharacterCombo.Tag is List<Character> characters)
            {
                int index = characters.FindIndex(c => c == character);
                if (index >= 0 && CharacterCombo.SelectedIndex != index)
                    CharacterCombo.SelectedIndex = index;
            }
        }

        private async void LoadLandingPortraitAsync(Image image, CCPCharacter character)
        {
            try
            {
                var drawingImage = await ImageService.GetCharacterImageAsync(character.CharacterID);
                if (drawingImage == null) return;
                var converted = DrawingImageToAvaloniaConverter.Instance.Convert(
                    drawingImage, typeof(Bitmap), null!, System.Globalization.CultureInfo.InvariantCulture);
                if (converted is Bitmap bitmap)
                    image.Source = bitmap;
            }
            catch { }
        }

        // --- first-run wizard ----------------------------------------------------

        private enum WizardStep { None, Intro, Access, Renderer }

        private WizardStep _wizardStep = WizardStep.None;
        private bool _wizardDone;

        /// <summary>Applies the wizard step to the landing chrome. Runs at the end
        /// of every BuildLandingGrid, so grant events re-evaluate the same truth.</summary>
        private void ApplyWizardState(int rosterCount)
        {
            bool wizard = _wizardStep != WizardStep.None;
            WizardStepper.IsVisible = wizard;
            WizardNav.IsVisible = wizard;
            WizardRendererPanel.IsVisible = _wizardStep == WizardStep.Renderer;
            SetRailLocked(wizard);

            if (!wizard)
            {
                LandingIntro.IsVisible = false;
                LandingGrid.IsVisible = true;
                LandingNeedsGrid.IsVisible = true;
                // Chooser mode always shows the grant row — it is the only route to
                // adding a character or granting the scope, and the wizard's own
                // step-scoped hiding must not survive the wizard.
                LandingGrantRow.IsVisible = true;
                LandingTitleText.Text = Loc.Get("Skinr.LandingTitle");
                LandingSubText.Text = Loc.Get("Skinr.LandingSub");
                return;
            }

            bool access = _wizardStep == WizardStep.Access;
            LandingIntro.IsVisible = _wizardStep == WizardStep.Intro;
            LandingGrid.IsVisible = access;
            LandingNeedsGrid.IsVisible = access;
            LandingAccessHeader.IsVisible = LandingAccessHeader.IsVisible && access;
            LandingNeedsHeader.IsVisible = LandingNeedsHeader.IsVisible && access;
            LandingSearchPill.IsVisible = access && rosterCount > 8;
            LandingGrantRow.IsVisible = access;

            HighlightWizardPill(WizardPill1, _wizardStep == WizardStep.Intro);
            HighlightWizardPill(WizardPill2, access);
            HighlightWizardPill(WizardPill3, _wizardStep == WizardStep.Renderer);
            WizardBackButton.IsVisible = _wizardStep != WizardStep.Intro;
            WizardNextButton.Content = Loc.Get(_wizardStep == WizardStep.Renderer
                ? "Skinr.WizardFinish" : "Skinr.WizardNext");

            switch (_wizardStep)
            {
                case WizardStep.Intro:
                    LandingTitleText.Text = Loc.Get("Skinr.RenderPlaceholderTitle");
                    LandingSubText.Text = Loc.Get("Skinr.LandingIntroSub");
                    break;
                case WizardStep.Access:
                    LandingTitleText.Text = Loc.Get("Skinr.WizardStep2");
                    LandingSubText.Text = Loc.Get("Skinr.LandingAuthPrompt");
                    break;
                case WizardStep.Renderer:
                    LandingTitleText.Text = Loc.Get("Skinr.WizardStep3");
                    LandingSubText.Text = Loc.Get("Skinr.WizardRendererSub");
                    break;
            }
        }

        private void HighlightWizardPill(Border pill, bool active)
        {
            pill.Background = (IBrush?)Resources[active ? "SkinrAccentDimBrush" : "SkinrPillBrush"];
            if (pill.Child is TextBlock label)
                label.Foreground = (IBrush?)Resources[active ? "SkinrTextBrush" : "SkinrTextSecBrush"];
        }

        private void OnWizardBack(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            _wizardStep = _wizardStep switch
            {
                WizardStep.Renderer => WizardStep.Access,
                WizardStep.Access => WizardStep.Intro,
                _ => _wizardStep,
            };
            BuildLandingGrid();
        }

        private void OnWizardNext(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            switch (_wizardStep)
            {
                case WizardStep.Intro:
                    _wizardStep = WizardStep.Access;
                    BuildLandingGrid();
                    break;
                case WizardStep.Access:
                    _wizardStep = WizardStep.Renderer;
                    BuildLandingGrid();
                    PrepareWizardRendererStep();
                    break;
                case WizardStep.Renderer:
                    FinishWizard();
                    break;
            }
        }

        /// <summary>The wizard's exit: chooser mode, rail unlocked — and the Hub
        /// pill pulses so "the studio is open" is SEEN, not inferred.</summary>
        private void FinishWizard()
        {
            _wizardStep = WizardStep.None;
            _wizardDone = true;
            BuildLandingGrid();
            PulseRailHubAsync();
        }

        private void SetRailLocked(bool locked)
        {
            RailCollection.Opacity = locked ? 0.35 : 1.0;
            RailCollection.IsHitTestVisible = !locked;
            RailHub.Opacity = locked ? 0.35 : 1.0;
            RailHub.IsHitTestVisible = !locked;
        }

        private async void PulseRailHubAsync()
        {
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    RailHub.Background = (IBrush?)Resources["SkinrAccentDimBrush"];
                    RailHubGlyph.Foreground = (IBrush?)Resources["SkinrAccentBrush"];
                    await System.Threading.Tasks.Task.Delay(300);
                    RailHub.Background = Brushes.Transparent;
                    RailHubGlyph.Foreground = (IBrush?)Resources["SkinrTextDimBrush"];
                    await System.Threading.Tasks.Task.Delay(220);
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: rail pulse failed: {ex.Message}");
            }
        }

        /// <summary>Arms wizard step 3: a renderer that's already running reports
        /// ready; otherwise the verified release facts and the Install button.</summary>
        private async void PrepareWizardRendererStep()
        {
            try
            {
                if (_render.IsAvailable)
                {
                    WizardRendererStatus.Text = Loc.Get("Skinr.WizardRendererReady");
                    WizardInstallButton.IsVisible = false;
                    return;
                }
                WizardInstallButton.IsVisible = true;
                WizardInstallButton.IsEnabled = true;
                WizardRendererStatus.Text = Loc.Get("Skinr.RuntimeChecking");
                _runtimeRelease ??= await SkinrRuntimeInstaller.GetLatestAsync();
                WizardRendererStatus.Text = _runtimeRelease == null
                    ? Loc.Get("Skinr.RuntimeUnreachable")
                    : string.Format(Loc.Get("Skinr.RuntimeVerifiedFmt"),
                        _runtimeRelease.Version,
                        _runtimeRelease.SizeBytes / (1024.0 * 1024.0));
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: wizard renderer step failed: {ex.Message}");
            }
        }

        private async void OnWizardInstall(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                WizardInstallButton.IsEnabled = false;
                bool ok = await RunRuntimeInstallAsync(s => WizardRendererStatus.Text = s);
                if (ok)
                {
                    WizardRendererStatus.Text = Loc.Get("Skinr.WizardRendererReady");
                    WizardInstallButton.IsVisible = false;
                    // The stage's own offer is now moot
                    RuntimeInstallPanel.IsVisible = false;
                    if (_hub.Data.SelectedRecipe != null)
                        await _render.LoadRecipeAsync(_hub.Data.SelectedRecipe);
                }
                else
                    WizardInstallButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: wizard install failed: {ex.Message}");
                WizardRendererStatus.Text = ex.Message;
                WizardInstallButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// The hull display name wants a poster size no shared tier provides. Computed from
        /// the scale service rather than written as a literal, so the 80–150%% font scale
        /// still applies and the no-hardcoded-sizes architecture test stays honest.
        /// </summary>
        private void ApplyDisplayTypography()
        {
            HullNameText.FontSize = FontScaleService.Title * 2.1;
            DesignNameText.FontSize = FontScaleService.Title * 1.35;
        }

        /// <summary>
        /// Settles whether this machine can render before the user asks it to.
        /// </summary>
        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            try
            {
                ReportDisplaySize();
                await _render.InitializeAsync();

                if (SkinrRenderPlatform.Current == SkinrRenderSupport.Supported)
                {
                    string? installed = SkinrRuntimeInstaller.InstalledVersion();
                    if (installed == null && !_render.IsAvailable)
                    {
                        // First-run case — offer the add-on download instead of an
                        // error. The offer method itself decides between the real
                        // offer and "coming" copy on platforms whose runtime is
                        // not yet published (server-driven availability).
                        await ShowRuntimeOfferAsync();
                    }
                    else if (installed != null)
                    {
                        // A runtime is installed: is a newer one announced? An
                        // update can also be the FIX for an installed-but-broken
                        // runtime, so this outranks the failure message below.
                        _runtimeRelease = await SkinrRuntimeInstaller.GetLatestAsync();
                        if (_runtimeRelease?.Version != null &&
                            _runtimeRelease.Version != installed)
                        {
                            await ShowRuntimeOfferAsync(update: true,
                                installedVersion: installed);
                        }
                        else if (!_render.IsAvailable)
                        {
                            // Installed, current, and still not starting: a short
                            // sentence for the user, the path-laden discovery
                            // report for the trace.
                            RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                            RenderDesc.Text = Loc.Get("Skinr.RenderFailedShort");
                            AppServices.TraceService?.Trace(
                                "SkinrViewer: renderer unavailable: " +
                                _render.UnavailableReason);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: renderer discovery failed: {ex.Message}");
            }
        }

        private SkinrRuntimeRelease? _runtimeRelease;

        /// <summary>
        /// Everything the download IS, shown before consent: exact version, size,
        /// host and SHA-256. The install button uses this same fetched release, so
        /// what the user verified is byte-for-byte what gets installed.
        /// </summary>
        private async System.Threading.Tasks.Task ShowRuntimeReleaseDetailsAsync()
        {
            try
            {
                _runtimeRelease = await SkinrRuntimeInstaller.GetLatestAsync();
                if (_runtimeRelease == null || !RuntimeInstallPanel.IsVisible)
                    return;
                RuntimeVerifiedText.Text = string.Format(
                    Loc.Get("Skinr.RuntimeVerifiedFmt"),
                    _runtimeRelease.Version,
                    _runtimeRelease.SizeBytes / (1024.0 * 1024.0));
                RuntimeVerifiedText.IsVisible = true;
                // Upgrade the description with the real size — a static "232 MB"
                // goes stale the first time a release grows.
                if (RuntimeInstallPanel.IsVisible)
                    RenderDesc.Text = string.Format(Loc.Get("Skinr.RuntimeDescFmt"),
                        _runtimeRelease.SizeBytes / (1024.0 * 1024.0));
                RuntimeDetailsText.Text = string.Format(
                    Loc.Get("Skinr.RuntimeDetailsFmt"),
                    _runtimeRelease.Version,
                    _runtimeRelease.SizeBytes / (1024.0 * 1024.0),
                    new Uri(_runtimeRelease.Url!).Host,
                    _runtimeRelease.ZipSha256);
                RuntimeDetailsText.IsVisible = true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: release details fetch failed: {ex.Message}");
            }
        }

        /// <summary>Puts the Rendering Runtime offer on the placeholder — the ONE
        /// screen for "no runtime", used at open and again whenever an action needed
        /// the renderer. Never the raw discovery report: that names environment
        /// variables and local paths, and it goes to the trace instead.</summary>
        /// <summary>Law 10 wrapper for sync call sites (design-click error path).</summary>
        private async void ShowRuntimeOffer(bool update = false, string? installedVersion = null)
        {
            try
            {
                await ShowRuntimeOfferAsync(update, installedVersion);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: runtime offer failed: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task ShowRuntimeOfferAsync(
            bool update = false, string? installedVersion = null)
        {
            // Server-driven availability, enforced HERE so every caller inherits
            // it: on a platform whose runtime is not yet published (announcement
            // 404s), the honest state is "coming" — never an Install button whose
            // click can only report "service unreachable". This bit a Mac user on
            // the design-click path, which bypassed the same check in OnOpened.
            if (!update && !OperatingSystem.IsWindows())
            {
                _runtimeRelease ??= await SkinrRuntimeInstaller.GetLatestAsync();
                if (_runtimeRelease == null)
                {
                    RenderImage.IsVisible = false;
                    bool stateOwns = ScopeMissingPanel.IsVisible ||
                                     LoadingText.IsVisible || ErrorText.IsVisible;
                    if (!stateOwns)
                        RenderPlaceholder.IsVisible = true;
                    WelcomeGuide.IsVisible = true;
                    RenderTitle.Text = Loc.Get("Skinr.RenderMacTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderMacDesc");
                    RuntimeInstallPanel.IsVisible = false;
                    return;
                }
            }

            RenderImage.IsVisible = false;
            // The view-state overlays (scope gate, loading, inventory error) own
            // the stage while active — the offer ARMS the placeholder without
            // stealing the screen (measured: the scope card and the welcome text
            // rendered on top of each other).
            bool stateOwnsStage = ScopeMissingPanel.IsVisible ||
                                  LoadingText.IsVisible || ErrorText.IsVisible;
            if (!stateOwnsStage)
                RenderPlaceholder.IsVisible = true;
            WelcomeGuide.IsVisible = true;
            if (update)
            {
                RenderTitle.Text = Loc.Get("Skinr.RuntimeUpdateTitle");
                RenderDesc.Text = string.Format(
                    Loc.Get("Skinr.RuntimeUpdateDescFmt"),
                    installedVersion, _runtimeRelease?.Version);
            }
            else
            {
                RenderTitle.Text = Loc.Get("Skinr.RuntimeTitle");
                RenderDesc.Text = Loc.Get("Skinr.RuntimeDesc");
            }
            RuntimeInstallPanel.IsVisible = true;
            if (_runtimeRelease == null)
                _ = ShowRuntimeReleaseDetailsAsync();
        }

        /// <summary>"Not now" — an explicit decline that leaves the welcome screen
        /// clean. The offer returns next time the window opens, or the next time an
        /// action actually needs the renderer.</summary>
        private void OnRuntimeNotNow(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            RuntimeInstallPanel.IsVisible = false;
        }

        /// <summary>The expander a technically concerned user expects: verification
        /// chain, communication boundaries, license, and the exact hash.</summary>
        private void OnRuntimeWhy(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            RuntimeWhyPanel.IsVisible = !RuntimeWhyPanel.IsVisible;
        }

        /// <summary>Runs the exact re-auth flow in place — a recoverable state must
        /// never send the user to a menu with directions.</summary>
        private async void OnScopeUpdate(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddCharacterWindow();
                await dialog.ShowDialog(this);
                if (!dialog.CharacterImported)
                    return;

                // The freshly granted characters' counts must not wait for the
                // scheduler's next SKINR pass (up to an hour): fetch them now, into
                // the monitored collection — the landing's live-update subscription
                // flips their cards from "checking" to a real count as each lands.
                foreach (string name in dialog.ImportedCharacterNames)
                {
                    var imported = AppServices.Characters.OfType<CCPCharacter>()
                        .FirstOrDefault(c => string.Equals(c.Name, name,
                            StringComparison.OrdinalIgnoreCase));
                    if (imported != null)
                        _ = SkinrViewerViewModel.RefreshMonitoredAsync(imported);
                }

                if (LandingPane.IsVisible)
                {
                    // Granting FROM the chooser keeps the chooser: the card moves up
                    // into the have-access group and the user still picks whose
                    // collection to open. Navigating away mid-choice read as the
                    // page vanishing (#139 follow-up).
                    string? current = CharacterCombo.SelectedIndex >= 0 &&
                        CharacterCombo.Tag is List<Character> chars &&
                        CharacterCombo.SelectedIndex < chars.Count
                            ? chars[CharacterCombo.SelectedIndex].Name : null;
                    RebuildCharacterPicker(current, allowNone: current == null);
                    BuildLandingGrid();
                    return;
                }

                // Scopes changed: rebuild the picker so the access markers update, and
                // land on the character who just granted access — re-authing character X
                // while the window silently stayed on character A read as "not working"
                // (Issue #139). Replacing ItemsSource resets the selection, so setting
                // the index fires OnCharacterChanged, which runs the scope check.
                RebuildCharacterPicker(dialog.ImportedCharacterNames.LastOrDefault());
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: scope re-auth failed: {ex.Message}");
            }
        }

        /// <summary>"Continue without SKINR" — the Hub half needs no character scope.</summary>
        private void OnScopeContinue(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowMarket(true);
        }

        /// <summary>
        /// The add-on consent flow: fetch the release, download with progress,
        /// verify (hash → pinned-key signature → per-file hashes), install, then
        /// boot the renderer that was missing a minute ago.
        /// </summary>
        private async void OnRuntimeInstall(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                RuntimeInstallButton.IsEnabled = false;
                RuntimeInstallDesc.IsVisible = true;
                RuntimeInstallDesc.Text = Loc.Get("Skinr.RuntimeChecking");

                bool ok = await RunRuntimeInstallAsync(s => RuntimeInstallDesc.Text = s);
                if (ok)
                {
                    RuntimeInstallPanel.IsVisible = false;
                    RenderTitle.Text = Loc.Get("Skinr.RenderPlaceholderTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderPlaceholderDesc");
                    // A design may already be selected — put it on the stage.
                    if (_hub.Data.SelectedRecipe != null)
                        await _render.LoadRecipeAsync(_hub.Data.SelectedRecipe);
                }
                else
                    RuntimeInstallButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: runtime install failed: {ex.Message}");
                RuntimeInstallDesc.Text = ex.Message;
                RuntimeInstallButton.IsEnabled = true;
            }
        }

        /// <summary>The install pipeline both offer surfaces (stage panel and
        /// first-run wizard) share: fetch the release, download with progress,
        /// verify (hash → pinned-key signature → per-file hashes), install, and
        /// reboot the renderer. Phase text goes through <paramref name="report"/>;
        /// returns whether the renderer came up.</summary>
        private async System.Threading.Tasks.Task<bool> RunRuntimeInstallAsync(Action<string> report)
        {
            var release = _runtimeRelease ?? await SkinrRuntimeInstaller.GetLatestAsync();
            if (release == null)
            {
                report(Loc.Get("Skinr.RuntimeUnreachable"));
                return false;
            }
            _runtimeRelease = release;

            report(string.Format(
                Loc.Get("Skinr.RuntimeDownloadingFmt"), release.Version,
                release.SizeBytes / (1024.0 * 1024.0)));
            var progress = new Progress<double>(fraction => ReportDownload(
                Loc.Get("Skinr.RuntimeTitle"), fraction));
            await SkinrRuntimeInstaller.InstallAsync(release, progress);

            report(Loc.Get("Skinr.RuntimeStarting"));
            // Reinitialize, not Initialize: on the UPDATE path a healthy host
            // (and the converter it discovered) is already running and must be
            // replaced, not kept.
            await _render.ReinitializeAsync();
            if (!_render.IsAvailable)
            {
                // The friendly sentence for the user; the discovery report is
                // developer material and goes to the trace.
                report(Loc.Get("Skinr.RuntimeFailed"));
                AppServices.TraceService?.Trace(
                    "SkinrViewer: runtime installed but unavailable: " +
                    _render.UnavailableReason);
            }
            return _render.IsAvailable;
        }

        // --- Paragon Hub pane ----------------------------------------------------

        // THE NAVIGATION LAW OF THIS WINDOW (user-mandated, twice): Collection is ONLY
        // the user's own designs — nothing else, ever. Hub is where market listings
        // live, and a market design opened from a card stays inside Hub context (rail
        // on Hub, Back pill to the grid, collection carousel hidden). The two must
        // never read as one surface.
        private bool _marketDetail;
        private int _marketPresetTypeId;
        private string? _lastCollectionSkinrId;

        private void OnRailCollection(object? sender, PointerPressedEventArgs e)
        {
            // The rail unlocks when the first-run wizard finishes (also covered by
            // IsHitTestVisible, but the state machine should not rely on hit-testing)
            if (_wizardStep != WizardStep.None)
                return;

            // Already on Collection: the second click is the way BACK to the landing —
            // the character chooser stays reachable without a dedicated button.
            if (!MarketPane.IsVisible && !LandingPane.IsVisible)
            {
                ShowLanding();
                return;
            }
            HideLanding();

            bool wasDetail = _marketDetail;
            _marketDetail = false;
            ShowMarket(false);
            // Collection must show THEIR ship, not the market design left on the
            // stage. Restore the last owned design they had open, if any.
            if (wasDetail && _lastCollectionSkinrId != null &&
                _lastCollectionSkinrId != _selectedSkinrId)
                OnDesignTilePressed(_lastCollectionSkinrId);
        }

        private void OnRailHub(object? sender, PointerPressedEventArgs e)
        {
            if (_wizardStep != WizardStep.None)
                return;
            _marketDetail = false;
            ShowMarket(true);
        }

        private void OnMarketBack(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            _marketDetail = false;
            // No ship preset on the way back: "Back to results" means THE results, not
            // a fresh filter to whatever hull was just on the stage.
            ShowMarket(true, applyShipPreset: false);
        }

        private void ShowMarket(bool market, bool applyShipPreset = true)
        {
            MarketPane.IsVisible = market;
            MarketBackButton.IsVisible = !market && _marketDetail;
            // The collection carousel exists only in Collection context.
            CarouselScroller.IsVisible = !market && !_marketDetail;
            bool hubContext = market || _marketDetail;
            // The Hub is the public marketplace — character-agnostic by definition.
            // The picker belongs to Collection, where "whose ships" is the question.
            CharacterCombo.IsVisible = !hubContext;
            RailCollection.Background = hubContext
                ? Brushes.Transparent : (IBrush?)Resources["SkinrPillBrush"];
            RailHub.Background = hubContext
                ? (IBrush?)Resources["SkinrPillBrush"] : Brushes.Transparent;
            RailCollectionGlyph.Foreground = (IBrush?)Resources[hubContext
                ? "SkinrTextDimBrush" : "SkinrAccentBrush"];
            RailHubGlyph.Foreground = (IBrush?)Resources[hubContext
                ? "SkinrAccentBrush" : "SkinrTextDimBrush"];
            // The loading overlay narrates the STAGE (ship parts, geometry, frames).
            // With the market grid on top it narrated something the user can't see —
            // a cold start showed "Preparing 63 ship parts…" painted over the Hub.
            // Hide it with the market; restore it when the stage comes back mid-load.
            if (market)
                LoadingOverlay.IsVisible = false;
            else if (_stageWaiting)
                LoadingOverlay.IsVisible = true;
            if (!market)
                return;

            // Ship-first: arriving from a design means "find a design for THIS hull".
            // Only when the hull changed since the last visit — a user who switched the
            // dropdown to All ships and comes back keeps their choice.
            int hull = applyShipPreset ? _hub.Data.SelectedRecipe?.ShipTypeId ?? 0 : 0;
            if (hull > 0 && hull != _marketPresetTypeId)
            {
                _marketPresetTypeId = hull;
                _market.ShipTypeFilter = hull;
            }

            if (!_market.HasLoaded && !_market.IsLoading)
                _ = _market.LoadAsync();
            CdnBanner.IsVisible = _hubPrefs.CommunityPreviews == null;
            EnsurePrerenderer();
            RefreshMarketHeader();
            RebuildMarketGrid();
        }

        // --- community previews consent ------------------------------------------

        private void OnCdnEnable(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e) => SetCommunityPreviews(true);

        private void OnCdnDecline(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e) => SetCommunityPreviews(false);

        private void OnCdnToggled(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_suppressCdnToggle)
                SetCommunityPreviews(CdnToggle.IsChecked == true);
        }

        private void SetCommunityPreviews(bool enabled)
        {
            _hubPrefs.CommunityPreviews = enabled;
            _hubPrefs.Save();
            _suppressCdnToggle = true;
            CdnToggle.IsChecked = enabled;
            _suppressCdnToggle = false;
            CdnBanner.IsVisible = false;
            if (enabled)
            {
                // The shelf may now answer cards this session already marked missed.
                lock (_shelfMissed)
                {
                    _shelfMissed.Clear();
                }
                _ = _market.ApplyCatalogAsync();
            }
        }

        /// <summary>
        /// The Hub's second engine: a Preview-tier sidecar that quietly renders REAL
        /// skinned thumbnails for market designs, always working on whatever the grid
        /// currently shows first. Started on the first Hub visit, never before — the
        /// Collection experience must not pay for a marketplace it hasn't opened.
        /// </summary>
        private void EnsurePrerenderer()
        {
            if (_prerenderer != null || SkinrRenderPlatform.Current != SkinrRenderSupport.Supported)
                return;
            _prerenderer = new SkinrThumbnailPrerenderer(_hub.Thumbnails,
                () => _market.Entries
                    .Where(e => e.Recipe != null)
                    .Select(e => e.Recipe!)
                    .ToList(),
                () => _hubPrefs.CommunityPreviews == true,
                // Platform capability is not engine presence: on a Mac before the
                // Metal runtime ships (or after a declined install), only the CDN
                // shelf can fill cards — the sidecar must not be attempted.
                canRenderLocally: () => SkinrRuntimeInstaller.InstalledRoot() != null &&
                                        _render.IsAvailable);
            _prerenderer.ThumbnailCaptured += (id, path) =>
                Dispatcher.UIThread.Post(() =>
                {
                    _cardBitmaps.Invalidate(path);
                    TrySwapCardThumb(id, path);
                    if (MarketPane.IsVisible)
                        RefreshMarketHeader();
                });
            _prerenderer.StateChanged += () =>
                Dispatcher.UIThread.Post(() =>
                {
                    if (MarketPane.IsVisible)
                        RefreshMarketHeader();
                });
            _prerenderer.Start();
        }

        private void QueueMarketRefresh()
        {
            if (_marketRefreshPending || !MarketPane.IsVisible)
                return;
            _marketRefreshPending = true;
            DispatcherTimer.RunOnce(() =>
            {
                _marketRefreshPending = false;
                if (!MarketPane.IsVisible)
                    return;
                RefreshMarketHeader();
                MaybeRebuildMarketGrid();
            }, MarketRefreshDebounce);
        }

        private void OnMarketSearchChanged(object? sender, TextChangedEventArgs e) =>
            _market.SearchText = MarketSearchBox.Text ?? string.Empty;

        /// <summary>One tree node's scope: class, class+faction, or an exact hull.</summary>
        private sealed record MarketNode(string Key, string Class, string Faction, int TypeId);

        /// <summary>A section label row in the virtualized market list.</summary>
        private sealed record MarketHeaderRow(string Label);

        /// <summary>One visual row of market cards (chunked to the pane's width).</summary>
        private sealed record MarketCardRow(IReadOnlyList<SkinrMarketEntry> Entries);

        // The grid rebuilds only when its structure changed: immediately for a scope
        // or search change (the user is waiting), coalesced while the identify walk
        // streams recipes in (one ESI answer per 150 ms must not mean five full
        // rebuilds per second — that walk is what made the Hub unusable, #139).
        private string _marketScopeSig = string.Empty;
        private string _marketDataSig = string.Empty;
        private DateTime _marketGridBuiltUtc = DateTime.MinValue;
        private bool _marketGridChurnQueued;
        private static readonly TimeSpan MarketGridChurnInterval = TimeSpan.FromSeconds(2);

        // Live thumb hosts by design id, so a captured render swaps into the one card
        // it belongs to instead of rebuilding the grid. Entries go stale when rows
        // virtualize away — the Tag check at swap time makes stale hosts harmless.
        private readonly Dictionary<string, Border> _cardThumbHosts = new(StringComparer.OrdinalIgnoreCase);

        // Card geometry: Width 208 + right margin 12. Recomputed from the list's
        // actual bounds so the chunking follows the window.
        private const double MarketCardSlotWidth = 220.0;
        private int _marketCardsPerRow = 4;

        private int MarketCardsPerRowNow()
        {
            double width = MarketList.Bounds.Width;
            if (width < 1)
                width = MarketPane.Bounds.Width - 300;   // pre-layout estimate
            return Math.Max(1, (int)(width / MarketCardSlotWidth));
        }

        private void InitMarketList()
        {
            MarketList.ItemTemplate =
                new global::Avalonia.Controls.Templates.FuncDataTemplate<object>(
                    (item, _) => item switch
                    {
                        MarketHeaderRow header => new TextBlock
                        {
                            Text = header.Label,
                            FontWeight = FontWeight.SemiBold,
                            FontSize = FontScaleService.Small,
                            Foreground = (IBrush?)Resources["SkinrAccentBrush"],
                            Margin = new Thickness(0, 8, 0, 4)
                        },
                        MarketCardRow row => BuildMarketCardRow(row),
                        _ => new Panel()
                    },
                    supportsRecycling: false);

            MarketList.PropertyChanged += (_, e) =>
            {
                if (e.Property != BoundsProperty || !MarketPane.IsVisible)
                    return;
                int perRow = MarketCardsPerRowNow();
                if (perRow != _marketCardsPerRow)
                {
                    _marketCardsPerRow = perRow;
                    RebuildMarketGrid();
                }
            };
        }

        private Control BuildMarketCardRow(MarketCardRow row)
        {
            var panel = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal };
            foreach (SkinrMarketEntry entry in row.Entries)
                panel.Children.Add(BuildMarketCard(entry));
            return panel;
        }

        private string _marketTreeSig = string.Empty;
        private bool _marketTreeRebuilding;

        private void OnMarketTreeSelection(object? sender, SelectionChangedEventArgs e)
        {
            if (_marketTreeRebuilding)
                return;
            if (MarketTree.SelectedItem is TreeViewItem { Tag: MarketNode node })
                _market.SetScope(node.Class, node.Faction, node.TypeId);
        }

        /// <summary>
        /// Rebuilds the market tree only when its CONTENT changed (the identify walk
        /// grows it steadily), preserving which branches the user has open — a rebuild
        /// that collapsed the tree every few seconds would be unusable during the walk.
        /// </summary>
        private void RefreshMarketTree()
        {
            var tree = _market.Tree();
            var sig = new System.Text.StringBuilder();
            int total = 0;
            foreach (var cls in tree)
            {
                sig.Append(cls.Name).Append(':');
                foreach (var fac in cls.Factions)
                {
                    sig.Append(fac.Name).Append('=');
                    foreach (var hull in fac.Hulls)
                    {
                        sig.Append(hull.TypeId).Append(',').Append(hull.Designs).Append(';');
                        total += hull.Designs;
                    }
                }
            }
            string signature = sig.ToString();
            if (signature == _marketTreeSig)
                return;
            _marketTreeSig = signature;

            var expanded = new HashSet<string>();
            void Collect(ItemsControl parent)
            {
                foreach (object? child in parent.Items)
                {
                    if (child is not TreeViewItem tvi)
                        continue;
                    if (tvi.IsExpanded && tvi.Tag is MarketNode node)
                        expanded.Add(node.Key);
                    Collect(tvi);
                }
            }
            Collect(MarketTree);

            _marketTreeRebuilding = true;
            try
            {
                MarketTree.Items.Clear();
                MarketTree.Items.Add(new TreeViewItem
                {
                    Header = $"{Loc.Get("Skinr.HubAllShips")} ({total})",
                    Tag = new MarketNode("all", string.Empty, string.Empty, 0)
                });
                foreach (var cls in tree)
                {
                    string clsLabel = cls.Name.Length == 0
                        ? Loc.Get("Skinr.HubOther") : cls.Name;
                    int clsCount = cls.Factions.Sum(f => f.Hulls.Sum(h => h.Designs));
                    var clsNode = new MarketNode("c:" + cls.Name, cls.Name, string.Empty, 0);
                    var clsItem = new TreeViewItem
                    {
                        Header = $"{clsLabel} ({clsCount})",
                        Tag = clsNode,
                        IsExpanded = expanded.Contains(clsNode.Key)
                    };
                    foreach (var fac in cls.Factions)
                    {
                        string facLabel = fac.Name.Length == 0
                            ? Loc.Get("Skinr.HubOther") : fac.Name;
                        var facNode = new MarketNode(
                            "f:" + cls.Name + "|" + fac.Name, cls.Name, fac.Name, 0);
                        var facItem = new TreeViewItem
                        {
                            Header = $"{facLabel} ({fac.Hulls.Sum(h => h.Designs)})",
                            Tag = facNode,
                            IsExpanded = expanded.Contains(facNode.Key)
                        };
                        foreach (var hull in fac.Hulls)
                        {
                            facItem.Items.Add(new TreeViewItem
                            {
                                Header = $"{hull.Name} ({hull.Designs})",
                                Tag = new MarketNode("h:" + hull.TypeId,
                                    cls.Name, fac.Name, hull.TypeId)
                            });
                        }
                        clsItem.Items.Add(facItem);
                    }
                    MarketTree.Items.Add(clsItem);
                }
            }
            finally
            {
                _marketTreeRebuilding = false;
            }
        }

        /// <summary>
        /// Loads CCP's official render of the plain hull into a market card's
        /// placeholder. Best-effort and cached by <see cref="ImageService"/> (memory +
        /// disk, keyed by URL), so the same hull across fifty cards downloads once. A
        /// card rebuilt before the image lands just orphans a bitmap for the GC.
        /// </summary>
        private async void LoadHullRender(Image image, int typeId)
        {
            try
            {
                // Once per hull, ever — five designs on the same hull used to run
                // five SKBitmap→PNG→Bitmap conversions per grid rebuild, on the UI
                // thread. The cache's single-flight makes them share one.
                Bitmap? bitmap = await _cardBitmaps.GetHullRenderAsync(typeId);
                if (bitmap != null)
                    image.Source = bitmap;
            }
            catch (Exception)
            {
                // Best-effort art; the hull-name text stays underneath.
            }
        }

        /// <summary>The display name for a hull type id, empty when unknown.</summary>
        private static string HullNameFor(int typeId)
        {
            if (typeId <= 0)
                return string.Empty;
            var item = EveLens.Common.Data.StaticItems.GetItemByID(typeId);
            return item == EveLens.Common.Data.Item.UnknownItem
                ? string.Empty : item.LocalizedName;
        }

        /// <summary>
        /// The cheap half of a market refresh: title, stats line, status text. Safe
        /// to run on every data whisper — the expensive grid rebuild is gated
        /// separately, because "the render label changed" must never cost a
        /// thousand-card rebuild (#139: the Hub froze the whole app).
        /// </summary>
        private void RefreshMarketHeader()
        {
            var entries = _market.Entries;

            string hullName = HullNameFor(_market.ShipTypeFilter);
            if (!string.IsNullOrEmpty(hullName))
                MarketTitle.Text = string.Format(
                    Loc.Get("Skinr.HubForShip"), hullName.ToUpperInvariant());
            else if (_market.GroupFilter.Length > 0)
                MarketTitle.Text = _market.FactionFilter.Length > 0
                    ? $"{_market.GroupFilter.ToUpperInvariant()} · {_market.FactionFilter}"
                    : _market.GroupFilter.ToUpperInvariant();
            else
                MarketTitle.Text = Loc.Get("Skinr.HubTitle");

            string stats = string.Format(Loc.Get("Skinr.HubStats"),
                entries.Count, _market.TotalListings);
            // "identifying" counts entries with no identity from ANY source; with
            // the hub catalog on this is zero and the recipe walk stays invisible.
            int unidentified = _market.UnidentifiedCount;
            if (unidentified > 0)
                stats += " · " + string.Format(Loc.Get("Skinr.HubResolvingFmt"), unidentified);
            if (_prerenderer?.CurrentLabel is { } rendering)
                stats += " · " + string.Format(Loc.Get("Skinr.HubRenderingFmt"), rendering);
            MarketStats.Text = stats;

            if (_market.IsLoading && entries.Count == 0)
            {
                MarketStatus.Text = Loc.Get("Skinr.HubLoading");
                MarketStatus.IsVisible = true;
            }
            else if (!string.IsNullOrEmpty(_market.Error) && entries.Count == 0)
            {
                MarketStatus.Text = _market.Error;
                MarketStatus.IsVisible = true;
            }
            else if (entries.Count == 0 && _market.HasLoaded)
            {
                MarketStatus.Text = Loc.Get("Skinr.HubEmpty");
                MarketStatus.IsVisible = true;
            }
            else
            {
                MarketStatus.IsVisible = false;
            }
        }

        private string MarketScopeSignature() =>
            $"{_market.SearchText}{_market.GroupFilter}" +
            $"{_market.FactionFilter}{_market.ShipTypeFilter}";

        private string MarketDataSignature(IReadOnlyList<SkinrMarketEntry> entries)
        {
            int identified = 0;
            foreach (SkinrMarketEntry entry in entries)
            {
                if (entry.IsIdentified)
                    identified++;
            }
            return $"{entries.Count}{identified}" +
                   $"{_market.TotalListings}{_market.HasLoaded}";
        }

        /// <summary>
        /// Rebuilds the grid only when its structure changed: immediately for a scope
        /// or search change (the user is waiting on it), coalesced to
        /// <see cref="MarketGridChurnInterval"/> while background walks stream data in.
        /// </summary>
        private void MaybeRebuildMarketGrid()
        {
            string scopeSig = MarketScopeSignature();
            if (scopeSig != _marketScopeSig)
            {
                RebuildMarketGrid();
                return;
            }
            string dataSig = MarketDataSignature(_market.Entries);
            if (dataSig == _marketDataSig)
                return;
            TimeSpan since = DateTime.UtcNow - _marketGridBuiltUtc;
            if (since >= MarketGridChurnInterval)
            {
                RebuildMarketGrid();
                return;
            }
            if (_marketGridChurnQueued)
                return;
            _marketGridChurnQueued = true;
            DispatcherTimer.RunOnce(() =>
            {
                _marketGridChurnQueued = false;
                if (MarketPane.IsVisible)
                    MaybeRebuildMarketGrid();
            }, MarketGridChurnInterval - since);
        }

        /// <summary>
        /// Recomputes the row list and hands it to the virtualized market list. Rows
        /// are data records — cards materialize only when scrolled into view, so this
        /// is milliseconds even for the full unfiltered catalog.
        /// </summary>
        private void RebuildMarketGrid()
        {
            _marketGridBuiltUtc = DateTime.UtcNow;
            _marketScopeSig = MarketScopeSignature();
            var entries = _market.Entries;
            _marketDataSig = MarketDataSignature(entries);
            _cardThumbHosts.Clear();

            RefreshMarketTree();
            _marketCardsPerRow = MarketCardsPerRowNow();

            // Ship class → faction → hull → name, the way a capsuleer thinks about
            // hulls ("Shuttles · Amarr"). "…" is the designs still identifying.
            var rows = new List<object>();
            foreach ((string group, string faction, IReadOnlyList<SkinrMarketEntry> designs)
                in _market.Sections())
            {
                string label;
                if (group == "…")
                    label = Loc.Get("Skinr.HubIdentifying");
                else if (group.Length == 0)
                    label = string.IsNullOrEmpty(faction)
                        ? Loc.Get("Skinr.HubOther") : faction;
                else
                    label = string.IsNullOrEmpty(faction)
                        ? group.ToUpperInvariant()
                        : $"{group.ToUpperInvariant()} · {faction}";
                rows.Add(new MarketHeaderRow(label));
                for (int start = 0; start < designs.Count; start += _marketCardsPerRow)
                {
                    int count = Math.Min(_marketCardsPerRow, designs.Count - start);
                    var chunk = new SkinrMarketEntry[count];
                    for (int k = 0; k < count; k++)
                        chunk[k] = designs[start + k];
                    rows.Add(new MarketCardRow(chunk));
                }
            }

            // An ItemsSource swap resets scroll; put the reader back where they were
            // (approximately — churn adds rows above, and exact is not worth chasing).
            var offset = MarketList.Scroll?.Offset;
            MarketList.ItemsSource = rows;
            if (offset is { Y: > 0 } o)
                Dispatcher.UIThread.Post(() =>
                {
                    if (MarketList.Scroll is { } scroll)
                        scroll.Offset = o;
                }, DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Swaps a freshly cached thumbnail into the one card it belongs to, in
        /// place — the alternative was rebuilding the grid per arrival, which at
        /// shelf download speed meant rebuilding it continuously. The Tag check makes
        /// hosts that virtualized away (or got reused) harmless.
        /// </summary>
        private void TrySwapCardThumb(string skinrId, string path)
        {
            if (!_cardThumbHosts.TryGetValue(skinrId, out Border? host))
                return;
            if (!string.Equals(host.Tag as string, skinrId, StringComparison.OrdinalIgnoreCase))
            {
                _cardThumbHosts.Remove(skinrId);
                return;
            }
            Bitmap? decoded = _cardBitmaps.GetFile(path);
            if (decoded != null)
                host.Child = new Image { Source = decoded, Stretch = Stretch.UniformToFill };
        }

        // The community shelf, pulled the way a website pulls images: each visible
        // card with no local thumbnail asks for its PNG, a handful in parallel,
        // completely outside the GPU prerenderer's one-at-a-time pacing. Misses are
        // remembered per session so re-realized cards don't re-ask the server.
        private static readonly SemaphoreSlim s_shelfGate = new(6);
        private readonly HashSet<string> _shelfMissed = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _shelfInFlight = new(StringComparer.OrdinalIgnoreCase);

        private async Task FillCardFromShelfAsync(string skinrId)
        {
            try
            {
                lock (_shelfMissed)
                {
                    if (_shelfMissed.Contains(skinrId) || !_shelfInFlight.Add(skinrId))
                        return;
                }
                string? path;
                await s_shelfGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    path = _hub.Thumbnails.TryGetPath(skinrId)
                        ?? await SkinrThumbnailCdn.TryFetchAsync(skinrId, _hub.Thumbnails)
                            .ConfigureAwait(false);
                }
                finally
                {
                    s_shelfGate.Release();
                }
                lock (_shelfMissed)
                {
                    _shelfInFlight.Remove(skinrId);
                    if (path == null)
                        _shelfMissed.Add(skinrId);
                }
                if (path == null)
                    return;
                Dispatcher.UIThread.Post(() =>
                {
                    _cardBitmaps.Invalidate(path);
                    TrySwapCardThumb(skinrId, path);
                });
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr shelf: {skinrId} fetch failed: {ex.Message}");
            }
        }


        /// <summary>One discovery card: render thumbnail (when the cache has one), the
        /// design name, creator, and the ask. Ridiculously visual, zero lore.</summary>
        private Control BuildMarketCard(SkinrMarketEntry entry)
        {
            var card = new Border
            {
                Width = 208,
                Margin = new Thickness(0, 0, 12, 12),
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                BorderBrush = (IBrush?)Resources["SkinrBorderBrush"],
                Background = (IBrush?)Resources["SkinrPanelBrush"],
                Cursor = new Cursor(StandardCursorType.Hand),
                ClipToBounds = true
            };

            var layout = new StackPanel();

            var thumbHost = new Border
            {
                Height = 120,
                Background = (IBrush?)Resources["SkinrPillBrush"]
            };
            thumbHost.Tag = entry.SkinrId;
            _cardThumbHosts[entry.SkinrId] = thumbHost;

            string? thumb = _hub.Thumbnails.TryGetPath(entry.SkinrId);
            if (thumb != null)
            {
                // Decoded once per file via the cache — a grid rebuild must reuse
                // pixels, not re-pay SkiaSharp for them (the macOS 4 GB hang).
                Bitmap? decoded = _cardBitmaps.GetFile(thumb);
                thumbHost.Child = decoded != null
                    ? new Image { Source = decoded, Stretch = Stretch.UniformToFill }
                    : PlaceholderGlyph();
            }
            else
            {
                // No captured render yet — CCP's official hull render stands in (the
                // REGULAR hull, no skin), so every card shows a ship immediately;
                // opening the design captures the real skinned thumbnail for everyone
                // after (capture-on-view). The hull name underlays while it downloads.
                var placeholder = new Panel();
                placeholder.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrEmpty(entry.HullName) ? "◈" : entry.HullName,
                    FontSize = FontScaleService.Body,
                    Foreground = (IBrush?)Resources["SkinrTextDimBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
                if (entry.ShipTypeId > 0)
                {
                    // Ghosted deliberately: a full-strength stock photo made five
                    // designs on the same hull read as five identical cards. The dim
                    // art + tag says "this is the ship, not the skin"; the real skinned
                    // thumbnail arrives at full strength once the design is viewed.
                    var hullImage = new Image { Stretch = Stretch.Uniform, Opacity = 0.4 };
                    placeholder.Children.Add(hullImage);
                    placeholder.Children.Add(new TextBlock
                    {
                        Text = Loc.Get("Skinr.HubBaseHull"),
                        FontSize = FontScaleService.Tiny,
                        Foreground = (IBrush?)Resources["SkinrTextDimBrush"],
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Margin = new Thickness(0, 0, 6, 4)
                    });
                    LoadHullRender(hullImage, entry.ShipTypeId);
                }
                thumbHost.Child = placeholder;
                // The community shelf fills this card the way a website would -- a
                // parallel image GET, not a turn in the GPU queue.
                if (_hubPrefs.CommunityPreviews == true)
                    _ = FillCardFromShelfAsync(entry.SkinrId);
            }
            layout.Children.Add(thumbHost);

            var body = new StackPanel { Margin = new Thickness(10, 8, 10, 10), Spacing = 2 };
            body.Children.Add(new TextBlock
            {
                Text = entry.DisplayName,
                FontWeight = FontWeight.SemiBold,
                FontSize = FontScaleService.Small,
                Foreground = (IBrush?)Resources["SkinrTextBrush"],
                TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis
            });

            // Ship always visible on the card (sections sort by it); creator appended.
            string subtitle = entry.HullName;
            if (!string.IsNullOrEmpty(entry.CreatorName))
            {
                string by = string.Format(Loc.Get("Skinr.HubBy"), entry.CreatorName);
                subtitle = string.IsNullOrEmpty(subtitle) ? by : subtitle + " · " + by;
            }
            body.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = FontScaleService.Caption,
                Foreground = (IBrush?)Resources["SkinrTextSecBrush"],
                TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis
            });

            var priceRow = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
            priceRow.Children.Add(new TextBlock
            {
                Text = entry.ActiveListings > 0
                    ? string.Format(Loc.Get("Skinr.HubListingsFmt"), entry.ActiveListings)
                    : Loc.Get("Skinr.HubNoListings"),
                FontSize = FontScaleService.Caption,
                Foreground = (IBrush?)Resources["SkinrTextDimBrush"],
                VerticalAlignment = VerticalAlignment.Center,
                [DockPanel.DockProperty] = Dock.Right
            });
            priceRow.Children.Add(new TextBlock
            {
                Text = entry.MinPlex > 0
                    ? string.Format(Loc.Get("Skinr.HubPlexFmt"), entry.MinPlex)
                    : string.Empty,
                FontWeight = FontWeight.SemiBold,
                FontSize = FontScaleService.Small,
                Foreground = (IBrush?)Resources["SkinrAccentBrush"],
                VerticalAlignment = VerticalAlignment.Center
            });
            body.Children.Add(priceRow);
            layout.Children.Add(body);

            card.Child = layout;
            string tooltip = entry.DisplayName;
            if (!string.IsNullOrEmpty(entry.HullName))
                tooltip += " — " + entry.HullName;
            if (entry.TierLevel > 0)
                tooltip += " · " + string.Format(Loc.Get("Skinr.TierBadge"), entry.TierLevel);
            ToolTip.SetTip(card, tooltip);

            // A card opens the design in the viewer we already have — the recipe route
            // is public, so unowned designs render exactly like owned ones. This is a
            // DETAIL view inside the Hub: the rail stays on Hub and the Back pill
            // returns to the grid; the Collection rail remains the way to your own designs.
            card.PointerPressed += (_, _) =>
            {
                _marketDetail = true;
                ShowMarket(false);
                OnDesignTilePressed(entry.SkinrId);
            };
            return card;
        }

        protected override void OnClosed(EventArgs e)
        {
            _prerenderer?.Dispose();
            _market.Dispose();
            _idNamesSub?.Dispose();
            _skinrDataSub?.Dispose();
            // The render VM first: it owns the sidecar process, and disposing it is what kills a
            // 200 MB engine that would otherwise outlive the window it was opened for.
            _render.Dispose();
            _surface?.Dispose();
            _cardBitmaps.Dispose();
            _hub.Dispose();
            base.OnClosed(e);
        }

        // --- data wiring -------------------------------------------------------

        private async void OnCharacterChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CharacterCombo.Tag is not System.Collections.Generic.List<Character> characters)
                    return;
                int index = CharacterCombo.SelectedIndex;
                var character = index >= 0 && index < characters.Count ? characters[index] : null;

                if (character != null)
                {
                    // Every path into a character leaves the landing — the top-right
                    // picker used to load the collection BEHIND the still-opaque
                    // landing pane, which read as "nothing happened" (#139)
                    if (LandingPane.IsVisible)
                        HideLanding();

                    // Every explicit choice is remembered — next open skips the landing
                    Settings.UI.SkinrLastCharacterId = character.CharacterID;
                    Settings.Save();

                    // A collection load is ESI + resolver + thumbnails: front and center,
                    // not a bottom-left whisper (Issue #139)
                    LoadingOverlayText.Text = string.Format(
                        Loc.Get("Skinr.LoadingDesignsFmt"), character.Name);
                    LoadingOverlayBar.IsIndeterminate = true;
                    _overlayFromDownload = false;
                    LoadingOverlay.Background = (IBrush?)Resources["SkinrBgBrush"];
                    LoadingOverlay.IsVisible = true;
                }

                try
                {
                    await _hub.Data.SelectCharacterAsync(character);
                    _hub.RefreshDesigns();
                }
                finally
                {
                    LoadingOverlay.IsVisible = false;
                }

                // The stage shows a ship, never a text wall: opening a collection
                // puts its first design up immediately ("nope, not this page").
                // With no renderer installed this is also what surfaces the
                // runtime offer without waiting for a click. Skipped when the
                // current selection already belongs to this collection, so
                // re-picking the same character never re-renders.
                if (character != null && _hub.Designs.Count > 0 &&
                    !_hub.Designs.Any(d => d.SkinrId == _selectedSkinrId))
                {
                    _lastCollectionSkinrId = _hub.Designs[0].SkinrId;
                    OnDesignTilePressed(_hub.Designs[0].SkinrId);
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.IsVisible = false;
                AppServices.TraceService?.Trace($"SkinrViewer: character select failed: {ex.Message}");
            }
        }

        private async void OnDesignTilePressed(string skinrId)
        {
            try
            {
                _selectedSkinrId = skinrId;
                _thumbSavedFor = null;
                _thumbArmed = false;   // no captures until THIS design's build returns
                RefreshCarousel();

                // Every design choice engages the centered narration until its first
                // frame. Empty stage: opaque (nothing to see behind). Ship on stage:
                // transparent scrim, just the floating card over the dimmed ship —
                // the swap is visible progress, so it must not be hidden.
                _stageWaiting = true;
                _overlayFromDownload = false;
                LoadingOverlay.Background = RenderImage.IsVisible
                    ? Brushes.Transparent
                    : (IBrush?)Resources["SkinrBgBrush"];
                LoadingOverlayBar.IsIndeterminate = true;
                LoadingOverlayText.Text = Loc.Get("Skinr.StatusRendering");
                LoadingOverlay.IsVisible = !MarketPane.IsVisible;

                await _hub.Data.SelectDesignAsync(skinrId);
                RefreshDesignCard();

                // Dim the previous design while the new one builds; the first frame of the
                // new build fades back up (ShowFrame sets Opacity 1 through the transition).
                RenderImage.Opacity = 0.25;

                // The recipe drives the render. Awaited so a second click while the first design
                // is still building cannot interleave two builds in the engine.
                await _render.LoadRecipeAsync(_hub.Data.SelectedRecipe);
                _thumbArmed = _selectedSkinrId == skinrId;

                // No recipe (fetch failed) or no renderer (nothing will ever draw
                // it): end the wait so the placeholder — including the runtime
                // offer — owns the stage instead of an eternal spinner.
                if ((_hub.Data.SelectedRecipe == null || !_render.IsAvailable) &&
                    _selectedSkinrId == skinrId)
                    EndStageWait();

                // The welcome sweep — CCP's studio rotates a freshly landed ship into place,
                // and so do we. Fire-and-forget: any user gesture cancels it mid-flight.
                _ = _render.PlayIntroAsync();
            }
            catch (Exception ex)
            {
                EndStageWait();
                AppServices.TraceService?.Trace($"SkinrViewer: design select failed: {ex.Message}");
            }
        }

        private void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            _hub.SearchText = SearchBox.Text ?? string.Empty;
        }

        // --- carousel ----------------------------------------------------------

        /// <summary>
        /// Rebuilds the design strip. Tiles are plain controls rather than a templated
        /// ItemsControl because the strip is small (a character's licenses), rebuilds are
        /// rare, and per-tile thumbnails load from disk paths that change at runtime.
        /// </summary>
        private void RefreshCarousel()
        {
            DesignStrip.Children.Clear();
            var designs = _hub.Designs;

            foreach (SkinrHubDesignEntry entry in designs)
            {
                bool selected = entry.SkinrId == _selectedSkinrId;
                var tile = new Border
                {
                    Width = 148,
                    Height = 112,
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(selected ? 2 : 1),
                    BorderBrush = (IBrush?)Resources[selected
                        ? "SkinrAccentBrush" : "SkinrBorderBrush"],
                    Background = (IBrush?)Resources["SkinrPanelBrush"],
                    Tag = entry.SkinrId,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };

                var layout = new DockPanel();
                var label = new TextBlock
                {
                    Text = entry.DisplayLabel,
                    FontSize = FontScaleService.Caption,
                    Foreground = (IBrush?)Resources[selected
                        ? "SkinrTextBrush" : "SkinrTextSecBrush"],
                    Margin = new Thickness(8, 4, 8, 6),
                    TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis
                };
                DockPanel.SetDock(label, Dock.Bottom);
                layout.Children.Add(label);

                if (entry.ThumbnailPath != null)
                {
                    Bitmap? decoded = _cardBitmaps.GetFile(entry.ThumbnailPath);
                    layout.Children.Add(decoded != null
                        ? new Image { Source = decoded, Stretch = Stretch.UniformToFill }
                        : PlaceholderGlyph());
                }
                else
                {
                    layout.Children.Add(PlaceholderGlyph());
                }

                tile.Child = layout;
                ToolTip.SetTip(tile, string.IsNullOrEmpty(entry.HullName)
                    ? entry.SkinrId
                    : $"{entry.DisplayLabel} — {entry.HullName}");
                tile.PointerPressed += (_, _) =>
                {
                    // Remembered so leaving Hub detail can put THEIR ship back on stage.
                    _lastCollectionSkinrId = entry.SkinrId;
                    OnDesignTilePressed(entry.SkinrId);
                };
                DesignStrip.Children.Add(tile);
            }

            if (designs.Count == 0 &&
                _hub.Data.State == SkinrViewerViewModel.ViewState.Loaded)
            {
                // Two different empties: a search that excluded everything says so;
                // a character who owns nothing gets told THAT, by name, with the Hub
                // as the way forward. "No designs match" right after granting access
                // read as a bug — the user never searched anything (#139 follow-up).
                bool ownsNone = _hub.Data.Licenses.Count == 0;
                DesignStrip.Children.Add(new TextBlock
                {
                    Text = ownsNone
                        ? string.Format(Loc.Get("Skinr.CollectionEmptyFmt"),
                            _hub.Data.SelectedCharacter?.Name)
                        : Loc.Get("Skinr.DesignsEmpty"),
                    FontSize = FontScaleService.Small,
                    Foreground = (IBrush?)Resources["SkinrTextDimBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8)
                });
            }
        }

        private TextBlock PlaceholderGlyph()
        {
            var glyph = new TextBlock
            {
                Text = "⬡",
                FontSize = FontScaleService.Title * 1.6,
                Foreground = (IBrush?)Resources["SkinrTextDimBrush"],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            glyph.Classes.Add("shimmer");
            return glyph;
        }

        // --- environment switcher -----------------------------------------------

        private void BuildEnvironmentSwitcher()
        {
            foreach (SkinrEnvironmentPreset preset in SkinrEnvironmentPresets.All)
            {
                var button = new Button
                {
                    Content = Loc.Get(SkinrEnvironmentPresets.NameKey(preset)),
                    Padding = new Thickness(14, 5),
                    CornerRadius = new CornerRadius(14),
                    FontSize = FontScaleService.Small,
                    Background = Brushes.Transparent,
                    Foreground = (IBrush?)Resources["SkinrTextSecBrush"],
                    Tag = preset
                };
                button.Click += async (_, _) => await OnEnvironmentPicked(preset);
                if (preset == SkinrEnvironmentPreset.Space)
                    ToolTip.SetTip(button, Loc.Get("Skinr.SpaceCycleTip"));
                EnvSwitcher.Children.Add(button);
            }
            HighlightEnvironment();
        }

        private async System.Threading.Tasks.Task OnEnvironmentPicked(
            SkinrEnvironmentPreset preset)
        {
            try
            {
                _hub.Environment = preset;
                HighlightEnvironment();

                // Every switch narrates itself (#139) — env swaps have no design id
                // for the frame gate to key on, so this overlay is closed by the
                // awaited call itself, not by ShowFrame.
                LoadingOverlayText.Text = Loc.Get("Skinr.StatusRendering");
                LoadingOverlayBar.IsIndeterminate = true;
                _overlayFromDownload = false;
                LoadingOverlay.Background = RenderImage.IsVisible
                    ? Brushes.Transparent
                    : (IBrush?)Resources["SkinrBgBrush"];
                LoadingOverlay.IsVisible = true;
                try
                {
                    await _render.SetEnvironmentAsync(preset);
                }
                finally
                {
                    // A design swap in flight keeps its own overlay; only end ours
                    if (!_stageWaiting)
                        LoadingOverlay.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: environment change failed: {ex.Message}");
            }
        }

        private void HighlightEnvironment()
        {
            foreach (Control child in EnvSwitcher.Children)
            {
                if (child is not Button button ||
                    button.Tag is not SkinrEnvironmentPreset preset)
                    continue;
                bool active = preset == _hub.Environment;
                button.Background = active
                    ? (IBrush?)Resources["SkinrAccentDimBrush"] : Brushes.Transparent;
                button.Foreground = (IBrush?)Resources[active
                    ? "SkinrTextBrush" : "SkinrTextSecBrush"];

                // Space is a deck of nebulas, not one backdrop: clicking it again deals
                // the next sky. Nobody discovered that (Issue #139), so the active pill
                // says so — the cycle glyph is the invitation to click once more.
                if (preset == SkinrEnvironmentPreset.Space)
                {
                    string name = Loc.Get(SkinrEnvironmentPresets.NameKey(preset));
                    button.Content = active ? name + "  ↻" : name;
                }
            }
        }

        // --- overlays ------------------------------------------------------------

        /// <summary>
        /// Shows/hides the Hangar pill for the staged hull. Supercapitals have no
        /// hangar look at all — the pill disappears rather than sit disabled — and
        /// if one arrives while the hangar is showing, the view retreats to Studio,
        /// never a titan clipping through the bay's architecture.
        /// </summary>
        private void ApplyHangarGate()
        {
            bool fits = _hub.HullFitsHangar;
            foreach (Control child in EnvSwitcher.Children)
            {
                if (child is not Button button ||
                    button.Tag is not SkinrEnvironmentPreset preset ||
                    preset != SkinrEnvironmentPreset.Hangar)
                    continue;
                button.IsVisible = fits;
            }
            if (!fits && _hub.Environment == SkinrEnvironmentPreset.Hangar)
                _ = OnEnvironmentPicked(SkinrEnvironmentPreset.Studio);
        }

        private void RefreshDesignCard()
        {
            ApplyHangarGate();
            var recipe = _hub.Data.SelectedRecipe;
            DesignCard.IsVisible = recipe != null;
            // Photo Op assembles the user's OWN ships around their own primary — the
            // Collection context only, never a market design someone else listed.
            PhotoOpButton.IsVisible = recipe != null && !_marketDetail &&
                                      _render.IsAvailable;
            HullNameText.IsVisible = _hub.Hull != null && !_photoMode;
            if (recipe == null)
                return;

            // The stage carries names only, by request — the design's centered on the
            // band, the hull's centered up top. Tier, composition, ship type and the
            // lore paragraph all live in the details panel.
            DesignNameText.Text = string.IsNullOrEmpty(recipe.Name)
                ? recipe.Id : recipe.Name;
            CollectionCountText.Text = string.Format(
                Loc.Get("Skinr.CollectionCountFmt"), _hub.Data.Licenses.Count);
            RefreshDetailsPanel();

            var hull = _hub.Hull;
            if (hull != null)
                HullNameText.Text = hull.LocalizedName.ToUpperInvariant();
        }

        // --- Photo Op ------------------------------------------------------------

        /// <summary>Formation size cap: each wingman is a full engine build; ten
        /// escorts plus the primary is a small fleet and a full frame.</summary>
        private const int PhotoOpMaxWingmen = 10;

        private bool _photoOpBusy;
        private bool _photoOpComboReady;

        /// <summary>
        /// (Re)builds the flyout's checkbox list from the user's collection. Only
        /// designs whose recipes have arrived qualify — a wingman is built from its
        /// recipe DNA, so a still-loading tile has nothing to fly yet.
        /// </summary>
        private void OnPhotoOpOpening(object? sender, EventArgs e)
        {
            try
            {
                if (!_photoOpComboReady)
                {
                    PhotoOpFormationCombo.ItemsSource = SkinrFleetFormations.All
                        .Select(f => Loc.Get(SkinrFleetFormations.NameKey(f)))
                        .ToList();
                    PhotoOpFormationCombo.SelectedIndex = 0;
                    _photoOpComboReady = true;
                }
                PhotoOpList.Children.Clear();
                foreach (SkinrHubDesignEntry entry in _hub.Designs)
                {
                    if (entry.Recipe == null || entry.SkinrId == _selectedSkinrId)
                        continue;
                    var box = new CheckBox
                    {
                        Content = string.IsNullOrEmpty(entry.HullName)
                            ? entry.DisplayLabel
                            : $"{entry.DisplayLabel} — {entry.HullName}",
                        FontSize = FontScaleService.Small,
                        Tag = entry.Recipe,
                        // The list reflects the LIVE fleet: reopening the flyout shows
                        // what's already flying instead of forgetting the selection.
                        IsChecked = _render.Fleet.AssembledDesignIds.Contains(entry.Recipe.Id ?? "")
                    };
                    box.IsCheckedChanged += OnPhotoOpChecked;
                    PhotoOpList.Children.Add(box);
                }
                if (PhotoOpList.Children.Count == 0)
                {
                    PhotoOpList.Children.Add(new TextBlock
                    {
                        Text = Loc.Get("Skinr.PhotoOpEmpty"),
                        TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                        FontSize = FontScaleService.Caption,
                        Foreground = (IBrush?)Resources["SkinrTextDimBrush"]
                    });
                }
                PhotoOpAssembleButton.IsEnabled = !_photoOpBusy;
                PhotoOpDisbandButton.IsEnabled = !_photoOpBusy && _render.Fleet.WingmenCount > 0;
                PhotoOpStatus.IsVisible = false;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: photo op list failed: {ex.Message}");
            }
        }

        /// <summary>Enforces the wingman cap by refusing the check that exceeds it.</summary>
        private void OnPhotoOpChecked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is not CheckBox box || box.IsChecked != true)
                return;
            int chosen = PhotoOpList.Children.OfType<CheckBox>()
                .Count(c => c.IsChecked == true);
            if (chosen > PhotoOpMaxWingmen)
            {
                box.IsChecked = false;
                PhotoOpStatus.Text = string.Format(
                    Loc.Get("Skinr.PhotoOpLimit"), PhotoOpMaxWingmen);
                PhotoOpStatus.IsVisible = true;
            }
        }

        private async void OnPhotoOpAssemble(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (_photoOpBusy || !_render.HasDesign)
                    return;
                var recipes = PhotoOpList.Children.OfType<CheckBox>()
                    .Where(c => c.IsChecked == true)
                    .Select(c => c.Tag)
                    .OfType<EsiSkinrRecipe>()
                    .Take(PhotoOpMaxWingmen)
                    .ToList();
                if (recipes.Count == 0)
                {
                    PhotoOpStatus.Text = Loc.Get("Skinr.PhotoOpPickFirst");
                    PhotoOpStatus.IsVisible = true;
                    return;
                }

                _photoOpBusy = true;
                PhotoOpAssembleButton.IsEnabled = false;
                PhotoOpDisbandButton.IsEnabled = false;
                PhotoOpStatus.Text = Loc.Get("Skinr.PhotoOpAssembling");
                PhotoOpStatus.IsVisible = true;

                // A photo op happens in open space; the bay can't hold a fleet.
                if (_render.EnvironmentPreset != SkinrEnvironmentPreset.Space)
                {
                    await _render.SetEnvironmentAsync(SkinrEnvironmentPreset.Space);
                    HighlightEnvironment();
                }
                int placed = await _render.Fleet.AssembleAsync(recipes, SelectedFormation());
                PhotoOpStatus.Text = placed > 0
                    ? string.Format(Loc.Get("Skinr.PhotoOpAssembled"), placed)
                    : Loc.Get("Skinr.PhotoOpFailed");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: photo op assemble failed: {ex.Message}");
                PhotoOpStatus.Text = Loc.Get("Skinr.PhotoOpFailed");
                PhotoOpStatus.IsVisible = true;
            }
            finally
            {
                _photoOpBusy = false;
                PhotoOpAssembleButton.IsEnabled = true;
                PhotoOpDisbandButton.IsEnabled = _render.Fleet.WingmenCount > 0;
            }
        }

        private SkinrFleetFormation SelectedFormation()
        {
            int index = PhotoOpFormationCombo.SelectedIndex;
            return index >= 0 && index < SkinrFleetFormations.All.Count
                ? SkinrFleetFormations.All[index]
                : SkinrFleetFormation.Vic;
        }

        /// <summary>Re-forms a live fleet instantly when the shape changes — the ships
        /// move; nothing rebuilds.</summary>
        private async void OnPhotoOpFormationChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (!_photoOpComboReady || _photoOpBusy || _render.Fleet.WingmenCount == 0)
                    return;
                await _render.Fleet.ApplyFormationAsync(SelectedFormation());
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: formation change failed: {ex.Message}");
            }
        }

        private async void OnPhotoOpDisband(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (_photoOpBusy)
                    return;
                _photoOpBusy = true;
                PhotoOpAssembleButton.IsEnabled = false;
                PhotoOpDisbandButton.IsEnabled = false;
                await _render.Fleet.DisbandAsync();
                PhotoOpStatus.Text = Loc.Get("Skinr.PhotoOpDisbanded");
                PhotoOpStatus.IsVisible = true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: photo op disband failed: {ex.Message}");
            }
            finally
            {
                _photoOpBusy = false;
                PhotoOpAssembleButton.IsEnabled = true;
                PhotoOpDisbandButton.IsEnabled = _render.Fleet.WingmenCount > 0;
            }
        }

        /// <summary>
        /// Honest platform messaging in the render pane: DirectX renderer on Windows
        /// x64, Metal planned on Apple Silicon, explicit "not available" for Linux
        /// and Intel Macs. The data half of the window works on every platform.
        /// </summary>
        private void ApplyPlatformSupport()
        {
            if (SkinrRenderPlatform.Current != SkinrRenderSupport.Supported)
                WelcomeGuide.IsVisible = false;   // platform message, not a welcome
            switch (SkinrRenderPlatform.Current)
            {
                case SkinrRenderSupport.Supported:
                    break;
                case SkinrRenderSupport.MacArmPlanned:
                    RenderTitle.Text = Loc.Get("Skinr.RenderMacTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderMacDesc");
                    break;
                case SkinrRenderSupport.UnsupportedMacIntel:
                    RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                    RenderDesc.Text = Loc.Get("Skinr.RenderMacIntelDesc");
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
                string status = active
                    ? string.Format(Loc.Get("Skinr.StatusDownloading"), what, (int)(fraction * 100))
                    : Loc.Get("Skinr.StatusReady");
                RenderStatusText.Text = status;

                // Front and center while there is nothing on the stage to cover: an
                // empty stage with a bottom-corner whisper read as frozen (Issue #139).
                // Once a ship is up, downloads stay in the pill — a modal bar over a
                // render you're actively orbiting would be worse.
                bool center = active && !RenderImage.IsVisible && !LandingPane.IsVisible;
                if (center)
                {
                    LoadingOverlayBar.IsIndeterminate = false;
                    LoadingOverlayBar.Value = fraction * 100;
                    LoadingOverlayText.Text = status;
                    LoadingOverlay.Background = (IBrush?)Resources["SkinrBgBrush"];
                    LoadingOverlay.IsVisible = true;
                    _overlayFromDownload = true;
                }
                else if (_overlayFromDownload)
                {
                    LoadingOverlay.IsVisible = false;
                    LoadingOverlayBar.IsIndeterminate = true;
                    _overlayFromDownload = false;
                }
            });
        }

        /// <summary>True while the center overlay is showing DOWNLOAD progress — so a
        /// finished download hides it without stomping a character-load overlay.</summary>
        private bool _overlayFromDownload;

        /// <summary>True from picking a design on an empty stage until its first frame
        /// (or a reported failure): the center overlay narrates everything in between.</summary>
        private bool _stageWaiting;

        private void EndStageWait()
        {
            _stageWaiting = false;
            _overlayFromDownload = false;
            LoadingOverlay.IsVisible = false;
            LoadingOverlayBar.IsIndeterminate = true;
        }

        // --- render surface ---------------------------------------------------

        /// <summary>
        /// Blits a finished frame into the stage, and — for the first settled frame of each
        /// design — captures the carousel thumbnail as a free side effect.
        /// </summary>
        private void ShowFrame(SkinrFrame frame)
        {
            // The first frame OF THE REQUESTED DESIGN ends the wait. Settled frames of
            // the PREVIOUS design keep arriving right after a swap click (the same race
            // CaptureThumbnailIfDue guards) — ending the wait on any frame made the swap
            // loader flash off instantly, so it only ever survived on a cold stage where
            // no frames were streaming yet (#139). And only the design-swap wait is ours
            // to end: character-load and download overlays are closed by their owners.
            if (_stageWaiting && _render.LoadedSkinrId == _selectedSkinrId)
                EndStageWait();

            var size = new PixelSize(frame.Width, frame.Height);
            if (_surface == null || _surface.PixelSize != size)
            {
                _surface?.Dispose();
                _surface = new WriteableBitmap(size, new Vector(96, 96),
                    PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            }

            using (ILockedFramebuffer buffer = _surface.Lock())
            {
                for (int y = 0; y < frame.Height; y++)
                {
                    Marshal.Copy(frame.Pixels, y * frame.Stride,
                        buffer.Address + y * buffer.RowBytes,
                        Math.Min(frame.Stride, buffer.RowBytes));
                }
            }

            RenderImage.Source = null;   // force Avalonia to re-read the same bitmap instance
            RenderImage.Source = _surface;
            if (!_stageWaiting)
                RenderImage.Opacity = 1.0;   // fades up through the opacity transition;
                                             // stale frames mid-swap keep the dim
            RenderImage.IsVisible = true;
            RenderPlaceholder.IsVisible = false;
            HintStrip.IsVisible = true;

            CaptureThumbnailIfDue(frame);
        }

        /// <summary>
        /// Saves one thumbnail per design, from the first settled frame after it builds.
        /// Capture-on-view: no extra engine work, and the carousel fills as designs are browsed.
        /// </summary>
        private void CaptureThumbnailIfDue(SkinrFrame frame)
        {
            string? skinrId = _selectedSkinrId;
            if (skinrId == null || !frame.Settled || _thumbSavedFor == skinrId)
                return;
            // THE RACE THIS GUARDS (it shipped a Raven thumbnail under an Oracle's id):
            // settled frames of the PREVIOUS design keep arriving after a click. The
            // arm flag only sets after the new build returns — Dispatcher posts are
            // FIFO, so every frame processed before that point predates the build —
            // and the engine's own loaded-id must agree. The luma floor drops black
            // warm-up frames.
            if (!_thumbArmed || _render.LoadedSkinrId != skinrId || frame.MeanLuma <= 4.0)
                return;
            _thumbSavedFor = skinrId;

            string? path = _hub.Thumbnails.Save(skinrId, frame);
            if (path != null)
            {
                // The file just changed under any cached decode of it.
                _cardBitmaps.Invalidate(path);
                _hub.OnThumbnailCaptured(skinrId, path);
            }
        }

        /// <summary>
        /// Shows a render error in the placeholder rather than leaving a blank stage, and folds
        /// any warnings into the details flyout so a partially-correct render never claims to be
        /// the finished article.
        /// </summary>
        private void RefreshRenderDiagnostics()
        {
            // While the Rendering Runtime offer is on screen, IT owns the placeholder.
            // The raw discovery report (env vars, local build paths) is developer
            // diagnostics — it belongs in the trace, not painted over the consent
            // panel. Measured: the offer rendered with the search dump stamped on
            // top of it, twice.
            if (RuntimeInstallPanel.IsVisible)
                return;

            if (_render.Error != null)
            {
                // "No runtime" is not an error, it is the offer — clicking a design
                // after "Not now" must re-ask, not print a discovery report.
                if (SkinrRenderPlatform.Current == SkinrRenderSupport.Supported &&
                    SkinrRuntimeInstaller.InstalledRoot() == null)
                {
                    AppServices.TraceService?.Trace(
                        "SkinrViewer: render blocked, runtime not installed: " + _render.Error);
                    HintStrip.IsVisible = false;
                    ShowRuntimeOffer();
                    return;
                }
                RenderImage.IsVisible = false;
                HintStrip.IsVisible = false;
                RenderPlaceholder.IsVisible = true;
                WelcomeGuide.IsVisible = false;   // an error report is not a welcome
                RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                // Design errors are short human sentences and stay verbatim; the
                // discovery report (recognisable by its search log) names local
                // paths and environment variables — that goes to the trace and the
                // user gets a sentence.
                bool discoveryDump = _render.Error.Contains("Searched:");
                RenderDesc.Text = discoveryDump
                    ? Loc.Get("Skinr.RenderFailedShort") : _render.Error;
                if (discoveryDump)
                    AppServices.TraceService?.Trace(
                        "SkinrViewer: render error: " + _render.Error);
                return;
            }

            if (_render.Warnings.Count > 0 && _hub.Data.SelectedRecipe != null)
            {
                PanelNotes.Text = string.Join("\n", _render.Warnings);
                PanelNotes.IsVisible = true;
            }
        }

        /// <summary>
        /// Fills the quality picker, showing what each tier actually does beside its name.
        /// </summary>
        private void PopulateQualities()
        {
            _suppressQuality = true;
            QualityCombo.ItemsSource = s_qualities
                .Select(q => $"{Loc.Get(SkinrRenderQualityPresets.NameKey(q))}  " +
                             $"({SkinrRenderQualityPresets.Describe(q)})")
                .ToList();
            QualityCombo.SelectedIndex = Array.IndexOf(s_qualities, _render.Quality);
            _suppressQuality = false;
        }

        /// <summary>
        /// Fills the resolution picker, appending the pixel count to the fixed sizes so the two
        /// measured options are not the only entries that say anything concrete.
        /// </summary>
        private void PopulateResolutions()
        {
            _suppressResolution = true;
            ResolutionCombo.ItemsSource = s_resolutions
                .Select(r =>
                {
                    string name = Loc.Get(SkinrRenderResolutionPresets.NameKey(r));
                    int w = SkinrRenderResolutionPresets.Width(r);
                    int h = SkinrRenderResolutionPresets.Height(r);
                    return w > 0 ? $"{name}  ({w}×{h})" : name;
                })
                .ToList();
            ResolutionCombo.SelectedIndex = Array.IndexOf(s_resolutions, _render.Resolution);
            _suppressResolution = false;
        }

        private async void OnQualityChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_suppressQuality) return;
                int index = QualityCombo.SelectedIndex;
                if (index < 0 || index >= s_qualities.Length) return;
                await _render.SetQualityAsync(s_qualities[index]);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: quality change failed: {ex.Message}");
            }
        }

        private async void OnResolutionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_suppressResolution) return;
                int index = ResolutionCombo.SelectedIndex;
                if (index < 0 || index >= s_resolutions.Length) return;

                // MatchDisplay needs a display measurement, and the window may have been dragged
                // to a different monitor since it opened.
                ReportDisplaySize();
                await _render.SetResolutionAsync(s_resolutions[index]);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: resolution change failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Feeds the stage's real pixel size to the view model, which debounces it into a
        /// single <c>resize</c> once a window drag stops.
        /// </summary>
        private void OnRenderSurfaceSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            try
            {
                double scale = RenderScaling;
                _render.SetViewportSize(
                    (int)(e.NewSize.Width * scale),
                    (int)(e.NewSize.Height * scale));
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: viewport size failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Tells the view model how large the display this window is on actually is, in physical
        /// pixels — the number behind "match my desktop".
        /// </summary>
        private void ReportDisplaySize()
        {
            global::Avalonia.Platform.Screen? screen =
                Screens.ScreenFromVisual(this) ?? Screens.Primary;
            if (screen == null) return;
            _render.SetDisplaySize(screen.Bounds.Width, screen.Bounds.Height);
        }

        private void OnResetView(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                _render.ResetCamera();
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: reset view failed: {ex.Message}");
            }
        }

        private void OnRenderDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_render.HasDesign)
                _render.ResetCamera();
        }

        // --- photo mode ---------------------------------------------------------

        private bool _photoMode;

        /// <summary>
        /// Hides every scrap of chrome so a screenshot is just the ship: rail, top bar,
        /// bottom band, overlays, status — gone. The camera button stays, ghosted, as the
        /// one way back besides Esc. Collapsing the bottom band grows the render viewport,
        /// which re-renders at the larger size for free through the resize path.
        /// </summary>
        private void OnPhotoModeToggle(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            SetPhotoMode(!_photoMode);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _photoMode)
            {
                SetPhotoMode(false);
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private void SetPhotoMode(bool on)
        {
            _photoMode = on;
            LeftRail.IsVisible = !on;
            TopBar.IsVisible = !on;
            BottomBand.IsVisible = !on;
            HullNameText.IsVisible = !on && _hub.Hull != null;
            MarketBackButton.IsVisible = !on && _marketDetail;
            StatusPill.IsVisible = !on;
            SettingsButton.IsVisible = !on;
            if (on)
                DetailsPanel.IsVisible = false;
            PhotoButton.Opacity = on ? 0.35 : 1.0;
            ToolTip.SetTip(PhotoButton,
                Loc.Get(on ? "Skinr.PhotoModeExit" : "Skinr.PhotoMode"));
        }

        private void OnRenderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!_render.HasDesign)
                return;
            Point pos = e.GetPosition(RenderSurface);

            // Ctrl+drag = photo-op free move: grab the ship nearest the cursor and
            // slide it in the camera plane. A plain drag stays the orbit it always was.
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && _render.Fleet.WingmenCount > 0 &&
                _render.Fleet.BeginDrag(pos.X, pos.Y,
                    RenderSurface.Bounds.Width, RenderSurface.Bounds.Height))
            {
                _dragOrigin = pos;
                e.Pointer.Capture(RenderSurface);
                return;
            }

            _dragOrigin = pos;
            _render.SetInteracting(true);
            e.Pointer.Capture(RenderSurface);
        }

        /// <summary>
        /// Turns pointer movement into orbit deltas, tracked against the previous position so
        /// rotation follows the hand continuously.
        /// </summary>
        private void OnRenderPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_dragOrigin is not { } origin)
                return;

            Point current = e.GetPosition(RenderSurface);
            if (_render.Fleet.IsDraggingWingman)
                _render.Fleet.DragBy(current.X - origin.X, current.Y - origin.Y);
            else
                _render.Orbit(current.X - origin.X, current.Y - origin.Y);
            _dragOrigin = current;
        }

        private void OnRenderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_dragOrigin == null)
                return;
            _dragOrigin = null;
            if (_render.Fleet.IsDraggingWingman)
                _render.Fleet.EndDrag();
            else
                _render.SetInteracting(false);
            e.Pointer.Capture(null);
        }

        private async void OnRenderPointerWheel(object? sender, PointerWheelEventArgs e)
        {
            try
            {
                if (!_render.HasDesign)
                    return;
                e.Handled = true;

                // Ctrl+scroll = the depth half of photo-op free movement: push the
                // ship under the cursor along the view axis instead of zooming.
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && _render.Fleet.WingmenCount > 0)
                {
                    Point pos = e.GetPosition(RenderSurface);
                    await _render.Fleet.PushDepthAsync(pos.X, pos.Y, e.Delta.Y,
                        RenderSurface.Bounds.Width, RenderSurface.Bounds.Height);
                    return;
                }
                _render.Zoom(e.Delta.Y);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: wheel failed: {ex.Message}");
            }
        }

        // --- details panel -------------------------------------------------------

        private void OnViewDetails(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            DetailsPanel.IsVisible = !DetailsPanel.IsVisible;
            if (DetailsPanel.IsVisible)
                RefreshDetailsPanel();
        }

        private void OnDetailsClose(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            DetailsPanel.IsVisible = false;
        }

        /// <summary>
        /// Copies a text summary of the current design to the clipboard — the "share"
        /// that needs no server and leaks nothing.
        /// </summary>
        private async void OnShare(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                string text = _hub.Data.DescribeSelectedRecipe();
                if (string.IsNullOrEmpty(text))
                    return;
                await AppServices.ClipboardService.SetTextAsync(text);
                RenderStatusText.Text = Loc.Get("Skinr.Copied");
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: share failed: {ex.Message}");
            }
        }

        private void OnFavorite(object? sender,
            global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                if (_selectedSkinrId == null)
                    return;
                bool now = _hub.ToggleFavorite(_selectedSkinrId);
                ApplyFavoriteGlyph(now);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: favorite failed: {ex.Message}");
            }
        }

        private void ApplyFavoriteGlyph(bool favorite)
        {
            FavoriteButton.Content = favorite ? "♥" : "♡";
            FavoriteButton.Foreground = (IBrush?)Resources[favorite
                ? "SkinrAccentBrush" : "SkinrTextSecBrush"];
        }

        /// <summary>
        /// Rebuilds the panel's ledger rows from the hull and the selected recipe. Cheap:
        /// a handful of TextBlocks, rebuilt only on design change or panel open.
        /// </summary>
        private void RefreshDetailsPanel()
        {
            var hull = _hub.Hull;
            var recipe = _hub.Data.SelectedRecipe;
            PanelHullName.Text = hull?.LocalizedName ?? string.Empty;
            PanelHullSub.Text = _hub.HullSubtitle;
            PanelHullDesc.Text = hull?.Description ?? string.Empty;
            PanelHullDesc.IsVisible = !string.IsNullOrEmpty(PanelHullDesc.Text);
            PanelNotes.IsVisible = false;

            PanelHullStats.Children.Clear();
            foreach ((string labelKey, string value) in _hub.HullStats(_render.HullRadius))
                PanelHullStats.Children.Add(LedgerRow(Loc.Get(labelKey), value));

            PanelDesignStats.Children.Clear();
            PanelCompositionStats.Children.Clear();
            if (recipe != null)
            {
                var culture = System.Globalization.CultureInfo.CurrentCulture;
                PanelDesignStats.Children.Add(LedgerRow(
                    Loc.Get("Skinr.StatDesign"),
                    string.IsNullOrEmpty(recipe.Name) ? recipe.Id : recipe.Name));
                // Who built it — resolves through the shared ID→name pipeline; the row
                // appears once the lookup lands (EveIDToNameUpdatedEvent re-runs this).
                string? designer = _hub.DesignerName;
                if (!string.IsNullOrEmpty(designer))
                    PanelDesignStats.Children.Add(LedgerRow(
                        Loc.Get("Skinr.StatDesigner"), designer));
                if (!string.IsNullOrEmpty(_hub.SelectedLine))
                    PanelDesignStats.Children.Add(LedgerRow(
                        Loc.Get("Skinr.StatCollection"), _hub.SelectedLine));
                if (_hub.SelectedTier > 0)
                    PanelDesignStats.Children.Add(LedgerRow(
                        Loc.Get("Skinr.StatTier"),
                        string.Format(Loc.Get("Skinr.TierBadge"), _hub.SelectedTier)));

                (int coatings, int patterns) = SkinrHubViewModel.Composition(recipe);
                PanelCompositionStats.Children.Add(LedgerRow(
                    Loc.Get("Skinr.StatCoatings"), coatings.ToString(culture)));
                PanelCompositionStats.Children.Add(LedgerRow(
                    Loc.Get("Skinr.StatPatterns"), patterns.ToString(culture)));
                SkinrLicenseEntry? license = _hub.SelectedLicense;
                if (license != null)
                {
                    string value = license.Activated
                        ? Loc.Get("Skinr.LicActive") : Loc.Get("Skinr.LicInactive");
                    if (license.Unactivated > 0)
                        value += " · " + string.Format(
                            Loc.Get("Skinr.LicSpareFmt"), license.Unactivated);
                    PanelCompositionStats.Children.Add(LedgerRow(
                        Loc.Get("Skinr.StatLicenses"), value));
                }
            }

            ApplyFavoriteGlyph(_selectedSkinrId != null && _hub.IsFavorite(_selectedSkinrId));
        }

        /// <summary>One ledger row: dim label left, light value right-aligned.</summary>
        private Control LedgerRow(string label, string value)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = FontScaleService.Small,
                Foreground = (IBrush?)Resources["SkinrTextDimBrush"]
            };
            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = FontScaleService.Small,
                Foreground = (IBrush?)Resources["SkinrTextBrush"],
                HorizontalAlignment = HorizontalAlignment.Right,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(12, 0, 0, 0)
            };
            Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(valueBlock);
            return grid;
        }

        private void RefreshFromViewModel()
        {
            var state = _hub.Data.State;
            // The cosmetics scope gates the character's OWN collection and nothing
            // else — market designs are public routes. With a design open (a Hub
            // card, typically) the notice would block a render it has no claim
            // over; it shows only while the stage is otherwise empty.
            ScopeMissingPanel.IsVisible =
                state == SkinrViewerViewModel.ViewState.ScopeMissing &&
                _selectedSkinrId == null;
            LoadingText.IsVisible = state == SkinrViewerViewModel.ViewState.LoadingInventory;
            ErrorText.IsVisible = state == SkinrViewerViewModel.ViewState.Error;

            bool showPlaceholder = state != SkinrViewerViewModel.ViewState.ScopeMissing &&
                                   state != SkinrViewerViewModel.ViewState.LoadingInventory &&
                                   state != SkinrViewerViewModel.ViewState.Error &&
                                   !RenderImage.IsVisible;
            RenderPlaceholder.IsVisible = showPlaceholder;

            if (state == SkinrViewerViewModel.ViewState.Error)
                ErrorText.Text = _hub.Data.ErrorMessage;

            RefreshDesignCard();
        }
    }
}
