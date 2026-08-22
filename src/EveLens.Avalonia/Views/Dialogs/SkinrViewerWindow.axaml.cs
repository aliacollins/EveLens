// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using EveLens.Avalonia.Services;
using EveLens.Common.Models;
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

        private bool _suppressQuality;
        private bool _suppressResolution;
        private WriteableBitmap? _surface;
        private Point? _dragOrigin;
        private string? _thumbSavedFor;
        private string? _selectedSkinrId;

        public SkinrViewerWindow()
        {
            InitializeComponent();
            ApplyDisplayTypography();
            ApplyPlatformSupport();
            PopulateQualities();
            PopulateResolutions();
            BuildEnvironmentSwitcher();

            _hub.Data.StateChanged += () => Dispatcher.UIThread.Post(RefreshFromViewModel);
            _hub.CarouselChanged += () => Dispatcher.UIThread.Post(RefreshCarousel);
            _render.FrameReady += frame => Dispatcher.UIThread.Post(() => ShowFrame(frame));
            _render.StatusChanged += text => Dispatcher.UIThread.Post(() =>
                RenderStatusText.Text = text);
            _render.DownloadProgress += fraction => ReportDownload(
                Loc.Get("Skinr.RenderPlaceholderTitle"), fraction);
            _render.DiagnosticsChanged += () => Dispatcher.UIThread.Post(RefreshRenderDiagnostics);

            var characters = AppServices.Characters.Where(c => c.Monitored)
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            CharacterCombo.ItemsSource = characters.Select(c => c.Name).ToList();
            CharacterCombo.Tag = characters;
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

                if (SkinrRenderPlatform.Current == SkinrRenderSupport.Supported &&
                    !_render.IsAvailable)
                {
                    RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                    RenderDesc.Text = _render.UnavailableReason;
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrViewer: renderer discovery failed: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // The render VM first: it owns the sidecar process, and disposing it is what kills a
            // 200 MB engine that would otherwise outlive the window it was opened for.
            _render.Dispose();
            _surface?.Dispose();
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
                await _hub.Data.SelectCharacterAsync(character);
                _hub.RefreshDesigns();
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"SkinrViewer: character select failed: {ex.Message}");
            }
        }

        private async void OnDesignTilePressed(string skinrId)
        {
            try
            {
                _selectedSkinrId = skinrId;
                _thumbSavedFor = null;
                RefreshCarousel();

                await _hub.Data.SelectDesignAsync(skinrId);
                RefreshDesignCard();

                // Dim the previous design while the new one builds; the first frame of the
                // new build fades back up (ShowFrame sets Opacity 1 through the transition).
                RenderImage.Opacity = 0.25;

                // The recipe drives the render. Awaited so a second click while the first design
                // is still building cannot interleave two builds in the engine.
                await _render.LoadRecipeAsync(_hub.Data.SelectedRecipe);

                // The welcome sweep — CCP's studio rotates a freshly landed ship into place,
                // and so do we. Fire-and-forget: any user gesture cancels it mid-flight.
                _ = _render.PlayIntroAsync();
            }
            catch (Exception ex)
            {
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
                    try
                    {
                        layout.Children.Add(new Image
                        {
                            Source = new Bitmap(entry.ThumbnailPath),
                            Stretch = Stretch.UniformToFill
                        });
                    }
                    catch (Exception)
                    {
                        // A truncated cache file must not break the strip; the glyph stands in.
                        layout.Children.Add(PlaceholderGlyph());
                    }
                }
                else
                {
                    layout.Children.Add(PlaceholderGlyph());
                }

                tile.Child = layout;
                ToolTip.SetTip(tile, string.IsNullOrEmpty(entry.HullName)
                    ? entry.SkinrId
                    : $"{entry.DisplayLabel} — {entry.HullName}");
                tile.PointerPressed += (_, _) => OnDesignTilePressed(entry.SkinrId);
                DesignStrip.Children.Add(tile);
            }

            if (designs.Count == 0 &&
                _hub.Data.State == SkinrViewerViewModel.ViewState.Loaded)
            {
                DesignStrip.Children.Add(new TextBlock
                {
                    Text = Loc.Get("Skinr.DesignsEmpty"),
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
                await _render.SetEnvironmentAsync(preset);
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
            }
        }

        // --- overlays ------------------------------------------------------------

        private void RefreshDesignCard()
        {
            var recipe = _hub.Data.SelectedRecipe;
            DesignCard.IsVisible = recipe != null;
            HullOverlay.IsVisible = _hub.Hull != null && !_photoMode;
            if (recipe == null)
                return;

            DesignNameText.Text = string.IsNullOrEmpty(recipe.Name)
                ? recipe.Id : recipe.Name;
            DesignSubtitleText.Text = _hub.DesignSubtitle;
            int tier = _hub.SelectedTier;
            TierBadge.IsVisible = tier > 0;
            TierBadgeText.Text = string.Format(Loc.Get("Skinr.TierBadge"), tier);
            DetailsFlyoutText.Text = _hub.Data.DescribeSelectedRecipe();

            var hull = _hub.Hull;
            if (hull != null)
            {
                HullNameText.Text = hull.LocalizedName.ToUpperInvariant();
                HullSubtitleText.Text = _hub.HullSubtitle;
                HullDescText.Text = hull.Description;
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
                RenderStatusText.Text = active
                    ? string.Format(Loc.Get("Skinr.StatusDownloading"), what, (int)(fraction * 100))
                    : Loc.Get("Skinr.StatusReady");
            });
        }

        // --- render surface ---------------------------------------------------

        /// <summary>
        /// Blits a finished frame into the stage, and — for the first settled frame of each
        /// design — captures the carousel thumbnail as a free side effect.
        /// </summary>
        private void ShowFrame(SkinrFrame frame)
        {
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
            RenderImage.Opacity = 1.0;   // fades up through the opacity transition
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
            _thumbSavedFor = skinrId;

            string? path = _hub.Thumbnails.Save(skinrId, frame);
            if (path != null)
                _hub.OnThumbnailCaptured(skinrId, path);
        }

        /// <summary>
        /// Shows a render error in the placeholder rather than leaving a blank stage, and folds
        /// any warnings into the details flyout so a partially-correct render never claims to be
        /// the finished article.
        /// </summary>
        private void RefreshRenderDiagnostics()
        {
            if (_render.Error != null)
            {
                RenderImage.IsVisible = false;
                HintStrip.IsVisible = false;
                RenderPlaceholder.IsVisible = true;
                RenderTitle.Text = Loc.Get("Skinr.RenderUnsupportedTitle");
                RenderDesc.Text = _render.Error;
                return;
            }

            if (_render.Warnings.Count > 0 && _hub.Data.SelectedRecipe != null)
            {
                DetailsFlyoutText.Text = _hub.Data.DescribeSelectedRecipe() + "\n" +
                                         string.Join("\n", _render.Warnings);
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
            HullOverlay.IsVisible = !on && _hub.Hull != null;
            StatusPill.IsVisible = !on;
            SettingsButton.IsVisible = !on;
            PhotoButton.Opacity = on ? 0.35 : 1.0;
            ToolTip.SetTip(PhotoButton,
                Loc.Get(on ? "Skinr.PhotoModeExit" : "Skinr.PhotoMode"));
        }

        private void OnRenderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!_render.HasDesign)
                return;
            _dragOrigin = e.GetPosition(RenderSurface);
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
            _render.Orbit(current.X - origin.X, current.Y - origin.Y);
            _dragOrigin = current;
        }

        private void OnRenderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_dragOrigin == null)
                return;
            _dragOrigin = null;
            _render.SetInteracting(false);
            e.Pointer.Capture(null);
        }

        private void OnRenderPointerWheel(object? sender, PointerWheelEventArgs e)
        {
            if (!_render.HasDesign)
                return;
            _render.Zoom(e.Delta.Y);
            e.Handled = true;
        }

        private void RefreshFromViewModel()
        {
            var state = _hub.Data.State;
            ScopeMissingPanel.IsVisible = state == SkinrViewerViewModel.ViewState.ScopeMissing;
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
