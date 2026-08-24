// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Data;
using EveLens.Common.Interfaces;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// The 3D pane's state: takes an ESI recipe, gets a rendered ship back, and turns mouse
    /// gestures into camera moves without ever making the UI wait for the renderer.
    /// </summary>
    /// <remarks>
    /// <para><b>The problem this class exists to solve.</b> A render is not a frame — it is
    /// somewhere between a fraction of a second and several seconds, because Trinity is running
    /// on a software rasteriser and anti-aliasing means letting temporal AA converge over
    /// multiple passes. A naive "render on every mouse-move" would queue hundreds of renders and
    /// the ship would lag minutes behind the cursor. So this VM runs a <b>coalescing render
    /// loop</b>: one render in flight at a time, and while it runs, incoming camera changes
    /// overwrite each other rather than queue. When the render finishes, the loop picks up
    /// whatever the newest camera state is and renders that. Drag as fast as you like — the
    /// renderer never falls behind by more than one frame, and it never renders a camera position
    /// the user has already left.</para>
    ///
    /// <para><b>Two-quality rendering.</b> While the pointer is down we ask for an unsettled
    /// frame: no TAA convergence, noisy edges, but fast enough to feel like control. On release
    /// we render once more with settling on, and the image sharpens. That is the same trick
    /// every DCC viewport uses, and it is the difference between a preview that feels alive and
    /// one that feels broken.</para>
    ///
    /// <para><b>Where the boundary is.</b> This VM knows about designs, cameras and frames. It
    /// does not know Trinity, res-paths, masks or DNA — <see cref="SkinrSidecarHost"/> owns all of
    /// that — and it does not know bitmaps, which is why <see cref="FrameReady"/> hands out raw
    /// BGRA and lets the Avalonia layer do the blit (Law 16: this must stay testable without a
    /// UI framework).</para>
    /// </remarks>
    public sealed class SkinrRenderViewModel : ViewModelBase
    {
        // How long after the last gesture we render the sharp frame. Long enough that a
        // continuous scroll or a drag with a pause in it counts as one gesture, short enough that
        // letting go feels like it sharpened immediately rather than after a beat.
        private const int SettleDelayMs = 220;

        /// <summary>
        /// The idle heartbeat between animation frames — a 60 fps cue. Ships are not
        /// statues: the Triglavian orb pulses, circleflow strips scroll, reactor glows
        /// breathe — all driven by engine-side curves on the real-time clock. A viewer
        /// that only renders on input freezes them mid-frame the moment the mouse stops,
        /// so the loop keeps drawing at this cadence whenever a design is loaded. There
        /// is deliberately no toggle: animation is what the content does, not a feature
        /// to opt into. The loop is self-clocked — the next cue only arms after the
        /// previous frame returns — so a heavy window degrades to whatever the pipeline
        /// sustains instead of queueing. TAA keeps converging across these frames
        /// because the camera holds still, so idle frames are the sharp ones.
        /// </summary>
        private const int AnimationFrameMs = 16;

        // A window drag emits a size on every frame of the drag, and a resize costs the warm-up
        // frames (TAA's history is at the old size), so this is longer than the camera debounce on
        // purpose: the goal is one resize when the user stops dragging, not fifty during it.
        private const int ResizeDelayMs = 320;

        private readonly ISkinrRecipeResolver _resolver;
        private readonly SemaphoreSlim _wake = new(0, 1);
        private readonly CancellationTokenSource _shutdown = new();

        private SkinrSidecarHost? _host;
        private Task? _loop;
        private SkinrResolvedDesign? _design;

        // Kept so a quality change can rebuild what is on screen. Without it, switching tiers
        // would leave the user staring at the pane they were already looking at, empty, with no
        // way back but re-picking the design from the list.
        private EsiSkinrRecipe? _recipe;


        private volatile bool _cameraDirty;
        private volatile bool _interacting;
        private volatile bool _needsSettledFrame;

        // Size state. `_pendingSize` is the resize the loop owes the renderer; it is applied inside
        // the render pass rather than at the call site so a window drag cannot start a resize while
        // a frame is in flight — the sidecar serialises ops anyway, but a resize queued behind a
        // settling render would block the drag for as long as the convergence takes.
        private SkinrRenderResolution _resolution = SkinrRenderResolution.MatchViewport;
        private int _viewportWidth;
        private int _viewportHeight;
        private int _displayWidth;
        private int _displayHeight;
        private SkinrRenderSize? _pendingSize;
        private SkinrRenderSize _appliedSize;

        // When the sharp frame is due, as an Environment.TickCount64; 0 means none pending.
        // Read and written with Volatile because gestures arrive on the UI thread and the loop
        // reads it on a worker.
        private long _settleDueAt;

        public SkinrRenderViewModel(ISkinrRecipeResolver? resolver = null)
        {
            // The shared instance, not a fresh one: the resolver holds the SDE SKINR catalog, and
            // constructing a second copy would decompress and index thousands of components a
            // second time for no benefit.
            _resolver = resolver ?? AppServices.SkinrRecipeResolver;
        }

        // --- observable state -------------------------------------------------

        /// <summary>A finished frame, raw BGRA. Raised off the UI thread.</summary>
        public event Action<SkinrFrame>? FrameReady;

        /// <summary>Status text for the pane's strip. Raised off the UI thread.</summary>
        public event Action<string>? StatusChanged;

        /// <summary>Download progress for hull geometry, 0.0-1.0. Negative means idle.</summary>
        public event Action<double>? DownloadProgress;

        /// <summary>Raised when <see cref="Warnings"/> or <see cref="Error"/> change.</summary>
        public event Action? DiagnosticsChanged;

        /// <summary>What went wrong badly enough that there is nothing to draw.</summary>
        public string? Error { get; private set; }

        /// <summary>Things that made the render incomplete without preventing it.</summary>
        public IReadOnlyList<string> Warnings { get; private set; } = Array.Empty<string>();

        /// <summary>True once a design is built and frames can be produced.</summary>
        public bool HasDesign => _design != null && Error == null;

        /// <summary>Whether a render is currently in flight.</summary>
        public bool IsRendering { get; private set; }

        /// <summary>The graphics device in use, for the diagnostics line.</summary>
        public string Device => _host?.Device ?? string.Empty;

        /// <summary>The design the engine has actually BUILT, or null. This is the
        /// truth the thumbnail capture must check: the selected id changes at click
        /// time, but frames keep arriving from the previous scene until the new build
        /// completes — saving one of those under the new id is how a Raven ends up
        /// wearing an Oracle's name in the cache.</summary>
        public string? LoadedSkinrId => _host?.LoadedSkinrId;

        /// <summary>
        /// How much the renderer is allowed to spend on a frame. Defaults to
        /// <see cref="SkinrRenderQuality.Balanced"/> rather than the cheapest tier because the
        /// point of the pane is to look at a design. Change it with <see cref="SetQualityAsync"/>.
        /// </summary>
        public SkinrRenderQuality Quality { get; private set; } = SkinrRenderQuality.Balanced;

        /// <summary>The orbit camera: gesture math, clamps, flick inertia and the
        /// ship-spin counter live here (split out of this VM along Law 2's seam).</summary>
        public SkinrStageCamera Camera { get; } = new();

        /// <summary>The built hull's bounding radius in world units (metres); 1.0 before
        /// any build. The details panel derives the ship's real length from it.</summary>
        public double HullRadius => Camera.Radius;

        /// <summary>
        /// How large a frame to produce. Defaults to <see cref="SkinrRenderResolution.MatchViewport"/>
        /// — one render pixel per pane pixel, at the pane's own aspect — because that is the only
        /// choice that neither blurs the image by upscaling nor wastes pixels in a pillarbox.
        /// </summary>
        public SkinrRenderResolution Resolution => _resolution;

        /// <summary>
        /// The size the renderer is actually producing, which is not always the size that was asked
        /// for: a hardware pixel ceiling applies, and a software device applies a much lower one.
        /// Zero until <see cref="InitializeAsync"/> has adopted the sidecar's boot size; from then on
        /// it is whatever the renderer last reported it applied, never what was requested.
        /// </summary>
        public SkinrRenderSize RenderSize => _appliedSize;


        /// <summary>
        /// Why the 3D preview cannot run here, or null when it can. Checked before the UI offers
        /// a 3D tab, so an unsupported platform gets an explanation instead of a dead pane.
        /// </summary>
        public string? UnavailableReason { get; private set; }

        /// <summary>
        /// Whether this machine can render at all. Platform first, then configuration: a Linux
        /// user is told there is no renderer to install, which is true, while a Windows user
        /// missing the runtime is told what is missing, which is actionable.
        /// </summary>
        public bool IsAvailable => UnavailableReason == null;

        // --- lifecycle --------------------------------------------------------

        /// <summary>
        /// Prepares the renderer without starting it: classifies the platform, discovers the
        /// runtime, and settles <see cref="IsAvailable"/>. Cheap — no process is spawned until a
        /// design is actually loaded.
        /// </summary>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (_host != null)
                return;

            string? platform = DescribePlatform(SkinrRenderPlatform.Current);
            if (platform != null)
            {
                UnavailableReason = platform;
                return;
            }

            _host = await SkinrSidecarHost.CreateAsync(Quality, ct).ConfigureAwait(false);
            _host.Progress += OnHostProgress;
            _host.DownloadProgress += fraction => DownloadProgress?.Invoke(fraction);

            // Adopt the host's boot size rather than leaving _appliedSize at zero. Zero is not
            // merely untidy: it never equals anything Fit produces, so the first ApplySizeChange
            // would queue a resize even when the pane happens to be exactly the boot size, and
            // RenderSize would report a frame size the renderer is not using.
            _appliedSize = _host.RenderSize;

            IReadOnlyList<string> problems = _host.Validate();
            UnavailableReason = problems.Count == 0
                ? null
                : "3D preview unavailable — " + string.Join("; ", problems);
        }

        private static string? DescribePlatform(SkinrRenderSupport support) => support switch
        {
            SkinrRenderSupport.Supported => null,
            SkinrRenderSupport.MacArmPlanned =>
                "3D preview is Windows-only for now; the Apple Silicon renderer is planned.",
            SkinrRenderSupport.UnsupportedMacIntel =>
                "3D preview needs Apple Silicon — EveLens does not ship an Intel macOS renderer.",
            SkinrRenderSupport.UnsupportedLinux =>
                "3D preview is not available on Linux: EVE's graphics engine has no Linux backend.",
            _ => "3D preview is not available on this platform."
        };

        /// <summary>
        /// Changes how many samples the renderer spends per pixel.
        /// </summary>
        /// <remarks>
        /// <para>This used to dispose the sidecar and rebuild the design from scratch — twenty to
        /// fifty seconds — on the belief that the render target could not be resized without a new
        /// device. That belief was wrong: Trinity's driver reads its destination target's size every
        /// frame and allocates from a size-keyed pool, so the sidecar can resize in place. See
        /// <see cref="SkinrRenderQuality"/> for the specifics. The tier is now a live setting and
        /// the design on screen survives it.</para>
        /// </remarks>
        public Task SetQualityAsync(SkinrRenderQuality quality, CancellationToken ct = default)
        {
            if (quality == Quality)
                return Task.CompletedTask;

            Quality = quality;
            Status(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "Switching to {0} ({1})…", quality,
                SkinrRenderQualityPresets.Describe(quality)));
            ApplySizeChange(immediate: true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Switches the environment preset — backdrop plus lighting — live, without touching
        /// the built design. No-ops quietly when no sidecar is running (the switcher stays
        /// usable before a design is picked; the choice applies when the engine exists).
        /// </summary>
        public async Task SetEnvironmentAsync(SkinrEnvironmentPreset preset,
            CancellationToken ct = default)
        {
            // Space is deliberately re-clickable: every application advances one sky
            // through CCP's authored nebula list. The rest no-op when already active.
            if (preset == EnvironmentPreset && preset != SkinrEnvironmentPreset.Space)
                return;
            EnvironmentPreset = preset;
            // Entering the bay with the camera already parked below the pad plane
            // (studio allows it) would show the deck's underside; lift it to the floor.
            Camera.ClampPitchToFloor();

            var host = _host;
            if (host == null)
                return;
            bool ok = await host.SetSceneAsync(
                SkinrEnvironmentPresets.Backdrop(preset),
                SkinrEnvironmentPresets.SunColor(preset),
                SkinrEnvironmentPresets.SunDirection(preset), ct).ConfigureAwait(false);
            if (ok && HasDesign)
                RequestRender(settled: true);
        }

        /// <summary>The environment preset currently applied (or queued for first boot).</summary>
        public SkinrEnvironmentPreset EnvironmentPreset
        {
            get => Camera.EnvironmentPreset;
            private set => Camera.EnvironmentPreset = value;
        }

        /// <summary>
        /// Changes the frame size the renderer produces. Live, like <see cref="SetQualityAsync"/>.
        /// </summary>
        public Task SetResolutionAsync(SkinrRenderResolution resolution,
            CancellationToken ct = default)
        {
            if (resolution == _resolution)
                return Task.CompletedTask;

            _resolution = resolution;
            ApplySizeChange(immediate: true);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Tells the view model how large the render pane is, in <em>device</em> pixels.
        /// </summary>
        /// <param name="widthPixels">Pane width times the render scaling.</param>
        /// <param name="heightPixels">Pane height times the render scaling.</param>
        /// <remarks>
        /// <para>Device pixels, not layout units, and the distinction is the whole point: on a 150%
        /// display a 1280-unit pane is 1920 real pixels, and rendering 1280 into it is a visibly
        /// soft image that no amount of supersampling fixes. The caller multiplies by
        /// <c>RenderScaling</c> because only the UI layer knows it (Law 16 keeps this class free of
        /// Avalonia).</para>
        ///
        /// <para>Safe to call on every layout pass. Nothing happens unless the resolved size
        /// actually differs, and when it does the resize is debounced by
        /// <see cref="ResizeDelayMs"/> so dragging a window edge produces one resize rather than
        /// one per frame of the drag.</para>
        /// </remarks>
        public void SetViewportSize(int widthPixels, int heightPixels)
        {
            if (widthPixels == _viewportWidth && heightPixels == _viewportHeight)
                return;

            _viewportWidth = widthPixels;
            _viewportHeight = heightPixels;

            // Only the viewport-following mode cares. Someone who explicitly picked 1080p asked for
            // 1080p regardless of how big the window is, and re-resolving here would be a no-op that
            // still had to be computed on every layout pass.
            if (_resolution == SkinrRenderResolution.MatchViewport)
                ApplySizeChange(immediate: false);
        }

        /// <summary>
        /// Tells the view model how large the user's primary display is, in pixels. Backs
        /// <see cref="SkinrRenderResolution.MatchDisplay"/>.
        /// </summary>
        /// <remarks>
        /// Passed in rather than queried because only the UI layer can see a screen, and this class
        /// has to stay testable without a UI framework (Law 16). Zero means "unknown", which
        /// <see cref="SkinrRenderResolutionPresets.Fit"/> handles by falling back to 1280×720 rather
        /// than by refusing.
        /// </remarks>
        public void SetDisplaySize(int widthPixels, int heightPixels)
        {
            if (widthPixels == _displayWidth && heightPixels == _displayHeight)
                return;

            _displayWidth = widthPixels;
            _displayHeight = heightPixels;

            if (_resolution == SkinrRenderResolution.MatchDisplay)
                ApplySizeChange(immediate: false);
        }

        /// <summary>
        /// Works out the size the current settings imply and, if it is not what the renderer is
        /// already producing, queues it for the render loop to apply.
        /// </summary>
        /// <remarks>
        /// The resize is queued rather than performed here because this is called from the UI thread
        /// — a quality picker, a layout pass — and a resize renders the warm-up frames. Handing it to
        /// the loop also means it lands between frames instead of behind one, and that it is
        /// naturally coalesced by the same debounce the camera uses.
        /// </remarks>
        private void ApplySizeChange(bool immediate)
        {
            SkinrRenderSize wanted = SkinrRenderResolutionPresets.Fit(
                _resolution, Quality, _viewportWidth, _viewportHeight,
                _displayWidth, _displayHeight);

            if (wanted == _appliedSize)
            {
                _pendingSize = null;
                return;
            }

            _pendingSize = wanted;

            if (immediate)
            {
                RequestRender(settled: true);
                return;
            }

            // Arm the debounce and wake the loop so it can compute its wait. The loop's settle
            // branch is what eventually fires: it treats "the timer came due with no camera change"
            // as a settled frame, which is exactly the pass a resize wants to be applied in.
            Volatile.Write(ref _settleDueAt, Environment.TickCount64 + ResizeDelayMs);
            try { _wake.Release(); }
            catch (SemaphoreFullException) { /* already awake */ }
        }

        // --- loading ----------------------------------------------------------

        /// <summary>
        /// Resolves an ESI recipe into a renderable design, builds it in the engine, and renders
        /// the first frame.
        /// </summary>
        /// <remarks>
        /// The resolve step is local and instant — it is the SDE catalog turning component IDs
        /// into the DNA tokens and texture paths the engine understands — so a recipe that cannot
        /// be resolved is rejected before anything expensive happens. That matters more than it
        /// sounds: booting the renderer to discover the catalog is out of date would cost the user
        /// a minute to be told something we already knew.
        /// </remarks>
        public async Task LoadRecipeAsync(EsiSkinrRecipe? recipe, CancellationToken ct = default)
        {
            _design = null;
            _recipe = recipe;
            Error = null;
            Warnings = Array.Empty<string>();
            DiagnosticsChanged?.Invoke();

            if (recipe == null)
                return;

            await InitializeAsync(ct).ConfigureAwait(false);
            if (_host == null)
            {
                SetError(UnavailableReason ?? "3D preview unavailable.");
                return;
            }

            if (!_resolver.IsAvailable)
            {
                SetError("EveLens's SKINR component catalog is missing, so designs cannot be " +
                         "resolved into a renderable form.");
                return;
            }

            SkinrResolvedDesign design = _resolver.Resolve(recipe);
            if (!design.IsRenderable)
            {
                SetError("This design cannot be rendered — " + (design.Warnings.Count > 0
                    ? string.Join("; ", design.Warnings)
                    : "its hull has no 3D identity in the static data."));
                return;
            }

            SkinrLoadResult result = await _host.LoadAsync(design, ct).ConfigureAwait(false);
            Warnings = result.Warnings;

            if (!result.Ok)
            {
                SetError(result.Error ?? "The renderer could not build this design.");
                return;
            }

            _design = design;

            // Frame the hull by its measured radius rather than a fixed distance: a Rifter and a
            // Leviathan differ by three orders of magnitude, and a constant would put one of them
            // inside the camera and the other in a corner of the frame.
            Camera.SetHull(result.Radius);

            DiagnosticsChanged?.Invoke();
            EnsureLoop();
            RequestRender(settled: true);
        }

        // --- camera gestures --------------------------------------------------

        /// <summary>
        /// Applies a drag as an orbit. Deltas are pixels; the scale is chosen so a drag across a
        /// 1024px pane is roughly a full turn, which is what makes the gesture feel like the
        /// ship is under the cursor rather than geared to it.
        /// </summary>
        public void Orbit(double deltaXPixels, double deltaYPixels)
        {
            // The balcony is a fixed stance — drags must not mutate the orbit state
            // behind it (or tick the spin counter for a camera that isn't moving).
            if (_balcony)
                return;
            // The hand always wins: a drag mid-intro (or mid-glide) takes the camera over
            // cleanly rather than fighting a script for it.
            _cameraAnimCts?.Cancel();
            Camera.Orbit(deltaXPixels, deltaYPixels);
            Gesture();
        }

        /// <summary>
        /// Applies a wheel notch as a zoom. Multiplicative, not additive: a fixed step feels
        /// glacial when far out and lurches when close in, because what the eye reads as "closer"
        /// is a ratio.
        /// </summary>
        public void Zoom(double notches)
        {
            if (_balcony)
                return;
            _cameraAnimCts?.Cancel();
            Camera.Zoom(notches);
            Gesture();
        }

        // Exponential decay: a full-speed flick glides for roughly two seconds, losing
        // most of its speed in the first half — reads as mass, not as a conveyor belt.
        private const double InertiaTimeConstant = 0.55;   // seconds
        private const double InertiaStopSpeed = 4.0;       // degrees/second

        /// <summary>
        /// The glide after a flick: the release velocity carries the orbit on, decaying
        /// exponentially until it drops under <see cref="InertiaStopSpeed"/>, then the
        /// settled frame lands. Any gesture cancels it mid-glide — same contract as the
        /// intro sweep, same token. Pitch obeys the live clamps every step; hitting the
        /// pole or the hangar's pad plane absorbs the vertical component and the spin
        /// continues flat, which is how a globe feels when you flick it into its stand.
        /// </summary>
        private async Task RunInertiaAsync(double yawPerSec, double pitchPerSec)
        {
            _cameraAnimCts?.Cancel();
            var cts = new CancellationTokenSource();
            _cameraAnimCts = cts;
            try
            {
                long last = Environment.TickCount64;
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(AnimationFrameMs, cts.Token).ConfigureAwait(false);
                    long now = Environment.TickCount64;
                    // Capped so a stall (breakpoint, starved thread pool) resumes with a
                    // step, not a leap.
                    double dt = Math.Min(0.1, (now - last) / 1000.0);
                    last = now;

                    Camera.GlideStep(yawPerSec, ref pitchPerSec, dt);

                    double decay = Math.Exp(-dt / InertiaTimeConstant);
                    yawPerSec *= decay;
                    pitchPerSec *= decay;
                    if (Math.Sqrt(yawPerSec * yawPerSec + pitchPerSec * pitchPerSec)
                        < InertiaStopSpeed)
                        break;
                    Gesture();
                }
                if (!cts.IsCancellationRequested)
                    RequestRender(settled: true);
            }
            catch (OperationCanceledException)
            {
                // A gesture grabbed the ship mid-glide; it stays where the hand put it.
            }
        }

        /// <summary>
        /// The rotate-in: sweeps the camera from an offset vantage to the rest framing over
        /// about a second when a design lands — the same welcome CCP's studio performs
        /// (<c>rotate_ship_in</c>, decompiled). Any user gesture cancels it instantly and
        /// keeps the camera wherever the hand put it.
        /// </summary>
        public async Task PlayIntroAsync()
        {
            if (!HasDesign)
                return;
            _cameraAnimCts?.Cancel();
            var cts = new CancellationTokenSource();
            _cameraAnimCts = cts;

            const int steps = 30;
            const double durationMs = 1100;
            double restYaw = Camera.Yaw, restPitch = Camera.Pitch,
                restDistance = Camera.Distance;
            // The from-pose offsets; Camera.Set clamps them to the live limits.
            double fromYaw = restYaw - 26.0;
            double fromPitch = restPitch + 8.0;
            double fromDistance = restDistance * 1.22;

            try
            {
                SetInteracting(true);   // fast-tier frames for the sweep
                for (int i = 0; i <= steps; i++)
                {
                    if (cts.IsCancellationRequested)
                        return;
                    double t = (double)i / steps;
                    double eased = t * t * (3.0 - 2.0 * t);   // smoothstep: no snap at either end
                    Camera.Set(
                        fromYaw + (restYaw - fromYaw) * eased,
                        fromPitch + (restPitch - fromPitch) * eased,
                        fromDistance + (restDistance - fromDistance) * eased);
                    RequestRender(settled: false);
                    await Task.Delay((int)(durationMs / steps), cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // A gesture took over; the camera stays where the hand put it.
            }
            finally
            {
                if (!cts.IsCancellationRequested)
                {
                    SetInteracting(false);   // release re-arms the settled frame
                    RequestRender(settled: true);
                }
            }
        }

        private CancellationTokenSource? _cameraAnimCts;

        /// <summary>
        /// Marks the start and end of a drag. Pointer-down holds off the settled frame for as
        /// long as the button is held, so a drag with a pause in it does not stall mid-gesture.
        /// </summary>
        public void SetInteracting(bool interacting)
        {
            bool wasInteracting = _interacting;
            _interacting = interacting;
            if (interacting)
            {
                // A fresh grab: whatever the previous drag left in the window is history.
                Camera.ClearFlickSamples();
                return;
            }
            if (!wasInteracting)
                return;
            Gesture();          // release re-arms the settle timer; it does not force a render
            // A flick at release keeps the orbit going. The intro sweep also passes
            // through here with no samples recorded, so it releases a stationary camera.
            (double yawPerSec, double pitchPerSec) = Camera.ReleaseFlick();
            if (yawPerSec != 0.0 || pitchPerSec != 0.0)
                _ = RunInertiaAsync(yawPerSec, pitchPerSec);
        }

        /// <summary>
        /// What every camera gesture does: ask for a fast frame now, and arm a sharp one for
        /// shortly after the user stops moving.
        /// </summary>
        /// <remarks>
        /// <para>This replaces a per-gesture <c>settled: !_interacting</c> decision that was
        /// correct for dragging and badly wrong for the wheel. A drag sets <see cref="_interacting"/>
        /// on pointer-down, so drag frames were cheap; the wheel has no pointer-down, so
        /// <em>every notch</em> asked for a settled frame — and a settled frame is a TAA
        /// convergence loop of up to 90 passes. Scrolling three notches queued three convergences,
        /// which is why zoom moved in lurches while rotation felt continuous.</para>
        /// <para>Debouncing is the fix rather than "make the wheel unsettled too", because the
        /// user still has to end up looking at a sharp image, and the only moment we can know the
        /// gesture is over is a short silence after it. <see cref="SettleDelayMs"/> is deliberately
        /// just above the interval between wheel notches from a continuous scroll, so a flick of
        /// the wheel is one gesture and not eight.</para>
        /// </remarks>
        private void Gesture()
        {
            Volatile.Write(ref _settleDueAt, Environment.TickCount64 + SettleDelayMs);
            RequestRender(settled: false);
        }

        // --- the Garage balcony -------------------------------------------------

        private volatile bool _balcony;

        /// <summary>True while the camera stands on the deck instead of orbiting.</summary>
        public bool BalconyActive => _balcony;

        /// <summary>
        /// The showcase: PARKS the hull on the pad (ship offset sinks it out of hover
        /// height to a size-scaled ground clearance) and stands a human 1.7m eye beside
        /// it, looking at its side profile — a showroom stance, not a ceiling view.
        /// Small ships sit close and personal; capitals become a wall of metal, which
        /// is the honest experience of standing next to one. Orbit input is suspended
        /// while active. Call after the design has built (needs the radius).
        /// </summary>
        public async Task EnterBalconyAsync(CancellationToken ct = default)
        {
            var host = _host;
            if (host == null || !HasDesign)
                return;
            _cameraAnimCts?.Cancel();
            double r = Camera.Radius;
            double drop = Math.Max(60.0, r * 1.25);

            // Park it: hover clearance is (drop - r); keep a quarter of it.
            double offsetY = -(drop - r) * 0.75;
            var shipOffset = new[] { 0.0, offsetY, 0.0 };
            double hullBottom = offsetY - r;

            // The observation gallery (probe218 B_gallery, matched to the user's
            // concept): eye a bit ABOVE the hull's spine, close in, gazing gently
            // down across it — a berth seen from the balcony, not a ceiling seen
            // from the floor. Standoff capped so capitals don't put the eye
            // through the bay wall.
            _ = hullBottom; // geometry documented above; the gallery stands off the spine
            double standoff = Math.Min(2400.0, Math.Max(60.0, 1.7 * r));
            var eye = new[] { standoff * 0.85, offsetY + 0.65 * r + 1.7, standoff * 0.55 };
            var at = new[] { 0.0, offsetY, 0.0 };
            double dx = eye[0] - at[0], dy = eye[1] - at[1], dz = eye[2] - at[2];
            double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            _balcony = true;
            await host.SetCameraAsync(0.0, 0.0, dist, 70.0, eye, at, shipOffset, ct)
                .ConfigureAwait(false);
            RequestRender(settled: true);
        }

        /// <summary>Clears the POV eye and the pad parking, returning to the orbit
        /// camera (fov back to the sidecar's 55° default, hull back to hover).</summary>
        public async Task ExitBalconyAsync(CancellationToken ct = default)
        {
            if (!_balcony)
                return;
            _balcony = false;
            var host = _host;
            if (host != null)
                await host.SetCameraAsync(Camera.Yaw, Camera.Pitch, Camera.Distance,
                    55.0, Array.Empty<double>(), null, new[] { 0.0, 0.0, 0.0 }, ct)
                    .ConfigureAwait(false);
            _sentCamera = (double.NaN, double.NaN, double.NaN);
            RequestRender(settled: true);
        }

        /// <summary>Returns the camera to the default three-quarter view.</summary>
        public void ResetCamera()
        {
            _cameraAnimCts?.Cancel();
            Camera.Reset();
            RequestRender(settled: true);
        }

        /// <summary>Renders again at the current camera — used after a quality change.</summary>
        public void Invalidate() => RequestRender(settled: true);

        // --- the coalescing render loop ---------------------------------------

        /// <summary>
        /// Marks the camera dirty and nudges the loop. Never blocks and never queues: this is
        /// called from pointer events, so it has to be as cheap as setting two flags.
        /// </summary>
        private void RequestRender(bool settled)
        {
            if (_design == null)
                return;

            _cameraDirty = true;
            if (settled)
            {
                // An explicit settled request — a load, a quality change, Reset view — supersedes
                // any armed timer. Leaving it armed would fire a second, identical convergence a
                // fifth of a second after this one for no reason.
                _needsSettledFrame = true;
                Volatile.Write(ref _settleDueAt, 0);
            }

            // A semaphore with a capacity of one is the whole coalescing mechanism: extra
            // releases are dropped, so a thousand mouse-moves wake the loop once.
            try { _wake.Release(); }
            catch (SemaphoreFullException) { /* already awake — exactly what we want */ }
        }

        private void EnsureLoop()
        {
            _loop ??= Task.Run(() => RenderLoopAsync(_shutdown.Token));
        }

        private async Task RenderLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Wait for work — but only until the armed settle falls due, if one is. The
                // timeout is the whole debounce: every gesture pushes the deadline out, so this
                // wakes on silence rather than on a count of events.
                try
                {
                    long due = Volatile.Read(ref _settleDueAt);
                    // The wait is bounded by the animation heartbeat whenever a design is
                    // loaded — a timeout IS the next animation frame's cue — and by the
                    // settle deadline when one is armed, whichever comes first. An armed
                    // settle must NOT suspend the heartbeat: a drag keeps a settle armed
                    // the whole time it lasts, and waiting on the deadline alone froze
                    // every pulse and glow the moment the mouse paused mid-rotation.
                    // Without a design there is nothing to animate; sleep until woken.
                    int waitMs;
                    if (_design == null)
                        waitMs = due == 0
                            ? Timeout.Infinite
                            : (int)Math.Clamp(due - Environment.TickCount64, 0, 1000);
                    else if (due == 0)
                        waitMs = AnimationFrameMs;
                    else
                        waitMs = (int)Math.Clamp(
                            Math.Min(due - Environment.TickCount64, AnimationFrameMs),
                            0, 1000);
                    await _wake.WaitAsync(waitMs, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // Claim the current state, then clear the flags. Anything that arrives from here
                // on sets them again and wakes us for the next pass — which is precisely the
                // behaviour we want, and why the flags are cleared before the render rather than
                // after: clearing after would swallow a camera change made mid-render.
                bool settled;
                bool animationFrame = false;
                if (_cameraDirty)
                {
                    _cameraDirty = false;
                    settled = _needsSettledFrame;
                    _needsSettledFrame = false;
                }
                else
                {
                    // Nothing new from the user. Either a settle has come due — the pointer
                    // being down vetoes it, so a drag that pauses mid-gesture does not stall
                    // on a convergence the user is about to invalidate — or it is time for an
                    // animation frame. An armed-but-not-due settle must FALL THROUGH to the
                    // animation frame, not skip the pass: a drag keeps a settle armed for its
                    // whole lifetime, and `continue` here froze every pulse the moment the
                    // hand paused — most visibly on vertical drags, where the pitch clamp
                    // stops the camera long before the hand stops moving.
                    long due = Volatile.Read(ref _settleDueAt);
                    bool settleDue = due != 0 && Environment.TickCount64 >= due;
                    if (settleDue && _interacting)
                    {
                        Volatile.Write(ref _settleDueAt, Environment.TickCount64 + SettleDelayMs);
                        settleDue = false;
                    }
                    if (settleDue)
                    {
                        Volatile.Write(ref _settleDueAt, 0);
                        settled = true;
                    }
                    else
                    {
                        if (_design == null)
                            continue;
                        settled = false;
                        animationFrame = true;
                    }
                }

                double yaw = Camera.Yaw, pitch = Camera.Pitch, distance = Camera.Distance;

                try
                {
                    // Animation frames leave IsRendering alone: a spinner flickering
                    // fifteen times a second would turn "alive" into "busy".
                    if (!animationFrame)
                        IsRendering = true;
                    await RenderOnceAsync(yaw, pitch, distance, settled, ct,
                        quiet: animationFrame).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // The loop must outlive a bad frame. A render that throws is logged and the
                    // loop keeps serving; killing it would leave a live window that silently
                    // stops responding to the mouse forever.
                    AppServices.TraceService?.Trace($"Skinr: render loop error: {ex.Message}");
                    Status("Render failed — see the diagnostic log.");
                }
                finally
                {
                    IsRendering = false;
                }
            }
        }

        /// <summary>The camera as last sent to the renderer, so animation frames can skip
        /// re-sending an unchanged one. NaN until the first send, which never matches.</summary>
        private (double Yaw, double Pitch, double Distance) _sentCamera =
            (double.NaN, double.NaN, double.NaN);

        private async Task RenderOnceAsync(double yaw, double pitch, double distance,
            bool settled, CancellationToken ct, bool quiet = false)
        {
            if (_host == null)
                return;

            // Size before camera, because a resize re-applies the camera itself (the projection's
            // aspect is recomputed from the new target) and doing it the other way round would send
            // a camera the resize then immediately recomputes.
            await ApplyPendingResizeAsync(ct).ConfigureAwait(false);

            // Animation frames hold the camera still by definition, and the camera op is a
            // whole sidecar round trip — at a 60 fps cue it was half the per-frame cost for
            // a value that had not changed. Interaction and settled frames always send: the
            // resize path re-derives the projection, and a stale skip after one of those is
            // a frame rendered through last gesture's camera.
            // While the balcony is active the camera is a fixed POV eye the sidecar
            // holds; orbit values must not be sent (eye wins sidecar-side, but the
            // send would still churn near/far derivation for nothing).
            if (!_balcony && (!quiet || _sentCamera != (yaw, pitch, distance)))
            {
                await _host.SetCameraAsync(yaw, pitch, distance, ct: ct).ConfigureAwait(false);
                _sentCamera = (yaw, pitch, distance);
            }

            SkinrFrame? frame = await _host.RenderAsync(settled, ct).ConfigureAwait(false);
            if (frame == null)
            {
                if (!quiet)
                    Status("Render failed.");
                return;
            }

            FrameReady?.Invoke(frame);
            // Animation frames keep the status strip silent: fifteen updates a second of
            // "Adjusting…" is churn, and the last settled frame's numbers are still true.
            if (quiet)
                return;
            Status(settled
                ? $"{frame.Width}×{frame.Height}" +
                  (_appliedSize.Supersample > 1 ? $" ×{_appliedSize.Supersample}" : string.Empty) +
                  (frame.AntiAliased ? " · anti-aliased" : string.Empty) +
                  (frame.Settled ? string.Empty : " · still converging")
                : "Adjusting…");
        }

        /// <summary>
        /// Applies a queued resize, if there is one, and records what the renderer settled on.
        /// </summary>
        /// <remarks>
        /// <para>The recorded size is the one that came <em>back</em>, never the one that went out.
        /// The sidecar clamps to a pixel ceiling — much lower on a software device — and it gives up
        /// supersampling before it gives up resolution. A status strip quoting the request rather
        /// than the result is the same category of quiet lie this feature has already been bitten by
        /// several times: it looks like a working setting and it is a wrong number.</para>
        ///
        /// <para>A failed resize clears the queue rather than retrying. The old target is still
        /// valid, so the user gets the previous size and a trace line, and the next layout pass or
        /// picker change will ask again — retrying here would spin against a dead sidecar.</para>
        /// </remarks>
        private async Task ApplyPendingResizeAsync(CancellationToken ct)
        {
            SkinrRenderSize? wanted = _pendingSize;
            if (wanted == null || _host == null)
                return;

            _pendingSize = null;

            SkinrRenderSize? applied = await _host
                .ResizeAsync(wanted.Value, ct).ConfigureAwait(false);
            if (applied == null)
                return;

            _appliedSize = applied.Value;
            DiagnosticsChanged?.Invoke();
        }

        // --- plumbing ---------------------------------------------------------

        private void OnHostProgress(string message) => Status(message);

        private void Status(string message) => StatusChanged?.Invoke(message);

        private void SetError(string message)
        {
            Error = message;
            DiagnosticsChanged?.Invoke();
            Status(message);
        }

        protected override void Dispose(bool disposing)
        {
            _cameraAnimCts?.Cancel();
            if (disposing)
            {
                _shutdown.Cancel();

                // The sidecar is shut down but the loop is not awaited. Dispose runs on the UI
                // thread when a window closes, and a render already in flight can take seconds;
                // blocking here would freeze the close. Cancellation plus disposing the host is
                // enough — the host kills the process, the in-flight call fails, and the loop
                // exits on its own.
                if (_host != null)
                {
                    _host.Progress -= OnHostProgress;
                    _host.Dispose();
                    _host = null;
                }

                _wake.Dispose();
                _shutdown.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
