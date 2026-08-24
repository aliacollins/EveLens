// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Skinr;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The SKINR renderer as the rest of EveLens sees it: hand it a resolved design, get frames
    /// back. Owns the sidecar's lifetime, the geometry conversion step, and the translation from
    /// domain objects into the render protocol.
    /// </summary>
    /// <remarks>
    /// <para><b>What this class is for.</b> <see cref="SkinrSidecarProcess"/> knows how to talk
    /// to the sidecar and nothing about SKINR; <see cref="SkinrResolvedDesign"/> knows what a
    /// design is and nothing about rendering. This is the only place that knows both, which is
    /// why the mask contract is written out here in one method rather than spread across the
    /// call sites that want a picture.</para>
    ///
    /// <para><b>Start is lazy and restart is automatic.</b> Booting costs 45-90 seconds on a
    /// software device, so nothing starts until a design is actually loaded — opening the SKINR
    /// window and browsing an inventory must stay instant. When the process faults (a timeout, a
    /// crash, a driver wedge) the next call gets a fresh one, because a faulted engine's state is
    /// unknown and retrying against it produces a render nobody can explain. The cost of a
    /// restart is honest and visible: it is reported through <see cref="Progress"/>.</para>
    ///
    /// <para><b>Two guards a render must not skip.</b> A hull whose shaders never declared the
    /// mask samplers cannot show a pattern — the render succeeds and quietly shows the wrong
    /// design, which is worse than an error, so <see cref="SkinrLoadResult.SupportsPatterns"/>
    /// says so from the measured <c>rebound</c> count. And a black frame passes every structural
    /// check there is, so the sidecar measures luma and refuses one; <see cref="LoadAsync"/>
    /// surfaces that as a load failure rather than a blank pane.</para>
    ///
    /// <para><b>Threading.</b> All ops are serialized by the transport, so this class is safe to
    /// call from anywhere, but calls will queue behind each other. Nothing here touches the UI
    /// thread; frames come back as raw BGRA bytes for the caller to blit.</para>
    /// </remarks>
    public sealed class SkinrSidecarHost : IDisposable
    {
        private readonly string _frameScratch = $"frame-{Guid.NewGuid():N}.bgra";

        // These are INACTIVITY budgets, not durations: the transport re-arms each one on every
        // line the sidecar sends, and the sidecar heartbeats every ~2s while it is working. So
        // these numbers answer "how long may the engine be silent before we assume it is wedged",
        // which is a question with a defensible answer — unlike "how long may a cold build take",
        // which depends on how much of a hull's texture set the CDN still has to send us. A
        // generous 240s total deadline killed a healthy first build; 30s of silence would not.
        //
        // Resolve is library lookup with no device work, so it should never pause at all.
        // Camera is arithmetic. Build and render pump the engine and therefore heartbeat.
        private static readonly TimeSpan s_resolveTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan s_buildTimeout = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan s_cameraTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_renderTimeout = TimeSpan.FromSeconds(60);

        private readonly SkinrSidecarOptions _options;
        private readonly SkinrGeometryConverter? _converter;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private SkinrSidecarProcess? _process;
        private string? _loadedSkinrId;
        private bool _disposed;

        // Shared-memory frame handoff. The base name is unique per host so two viewer
        // windows can never read each other's frames; the sidecar appends the dimensions
        // and the mapping is reopened only when that full name changes (i.e. on resize).
        private readonly string _shmBase =
            "EveLensSkinr_" + Guid.NewGuid().ToString("N")[..8];
        private System.IO.MemoryMappedFiles.MemoryMappedFile? _shmFile;
        private string? _shmOpenName;

        public SkinrSidecarHost(SkinrSidecarOptions options,
            SkinrGeometryConverter? converter = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _converter = converter;
        }

        /// <summary>
        /// Builds a host against EveLens's own cache layout, discovering the render runtime and
        /// the geometry converter and making sure a resource index exists on disk.
        /// </summary>
        /// <remarks>
        /// <para>The three directories are separate on purpose, and the separation is not
        /// cosmetic:</para>
        /// <list type="bullet">
        /// <item><c>skinr/rescache</c> is Blue's own CDN cache, written by the engine in its own
        /// hash layout. It is deliberately <em>not</em>
        /// <see cref="EveResourceService.CacheDirectory"/>: that tree is LRU-pruned by EveLens,
        /// and pruning files the engine believes it owns produces missing-texture renders that
        /// look like art bugs.</item>
        /// <item><c>skinr/geometry</c> is the override tree, holding our converted <c>.cmf</c>
        /// hulls at the res-paths Blue asks for.</item>
        /// <item><c>skinr/cmfcache</c> is the converter's content-addressed cache, keyed by input
        /// digest rather than by name, which is what makes a re-download of unchanged geometry
        /// free.</item>
        /// </list>
        /// <para>Fetching the index here rather than at first render is deliberate too: it is a
        /// network call, and doing it during construction means <see cref="Validate"/> can tell
        /// the truth about whether a render is possible <em>before</em> the UI offers the tab.
        /// </para>
        /// </remarks>
        public static Task<SkinrSidecarHost> CreateAsync(CancellationToken ct = default)
            => CreateAsync(SkinrRenderQuality.Preview, ct);

        /// <summary>
        /// As <see cref="CreateAsync(CancellationToken)"/>, at a chosen render quality.
        /// </summary>
        /// <remarks>
        /// The tier decides the <em>boot</em> size only. It is no longer a life sentence: see
        /// <see cref="ResizeAsync"/>, which changes size in place on a running sidecar.
        /// </remarks>
        public static async Task<SkinrSidecarHost> CreateAsync(
            SkinrRenderQuality quality, CancellationToken ct = default)
        {
            string root = Path.Combine(
                AppServices.ApplicationPaths.DataDirectory, "cache", "skinr");
            string overrideDirectory = Path.Combine(root, "geometry");

            // Both of them. CCP splits the index in two and the smaller half is the one holding
            // every compiled shader, so a renderer given only the base index draws a black frame
            // and reports nothing wrong. See EveResourceService's chain description.
            IReadOnlyList<string> indexes = Array.Empty<string>();
            try
            {
                indexes = await EveResourceService.GetIndexFilesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Not fatal here. Validate() will report "no resource index available" in words
                // the user can act on, which beats an exception escaping a window's constructor.
                AppServices.TraceService?.Trace(
                    $"Skinr: could not obtain a resource index: {ex.Message}");
            }

            var options = SkinrSidecarOptions.Discover(
                Path.Combine(root, "rescache"), overrideDirectory, indexes);
            options.ApplyQuality(quality);

            // Traced on success as well as failure. A machine can hold both an installed runtime and
            // a build tree, and "which one is running" is the first question when the two disagree —
            // asking it after the fact means guessing from a path in an error message.
            AppServices.TraceService?.Trace(
                "Skinr: render runtime search — " + string.Join("; ", options.DiscoverySteps));

            // Before the engine can fetch anything at all. Blue hands libcurl a CA bundle path
            // whether or not one exists, and CURLOPT_CAINFO overrides the platform trust store —
            // so without this the renderer cannot download a single resource on a machine whose
            // certificates are perfectly fine. A generated bundle beats the runtime's shipped one
            // when both are present; see SkinrCertificateBundle.
            string? certificates =
                SkinrCertificateBundle.Ensure(AppServices.ApplicationPaths.DataDirectory);
            if (certificates != null)
            {
                options.CertificatePath =
                    Path.Combine(certificates, SkinrCertificateBundle.FileName);
            }

            var converter = SkinrGeometryConverter.Discover(
                Path.Combine(root, "cmfcache"), overrideDirectory);

            return new SkinrSidecarHost(options, converter);
        }

        /// <summary>
        /// Human-readable progress for the render pane's status strip: "Converting geometry…",
        /// "Starting renderer (45s on first use)…". Raised from background threads.
        /// </summary>
        public event Action<string>? Progress;

        /// <summary>Download progress for a hull's geometry, 0.0-1.0, or null when idle.</summary>
        public event Action<double>? DownloadProgress;

        /// <summary>Which device the running sidecar got, or empty when not started.</summary>
        public string Device => _process?.Device ?? string.Empty;

        /// <summary>True when a sidecar is up and has not faulted.</summary>
        public bool IsRunning => _process?.IsRunning == true;

        /// <summary>The design currently built in the engine, or null.</summary>
        public string? LoadedSkinrId => _loadedSkinrId;

        /// <summary>
        /// The size the sidecar is configured to render at — the boot size until
        /// <see cref="ResizeAsync"/> changes it, and thereafter whatever the renderer reported it
        /// actually applied.
        /// </summary>
        /// <remarks>
        /// Exposed so a caller can tell what the engine is doing without keeping a parallel copy of
        /// the number. A caller that assumed zero until its first frame would queue a redundant
        /// resize on boot and, worse, report zero in a status strip that is meant to be the honest
        /// answer to "what am I looking at".
        /// </remarks>
        public SkinrRenderSize RenderSize =>
            new(_options.Width, _options.Height, _options.Supersample);

        /// <summary>
        /// Everything wrong with this host's configuration, in words a user can act on. Empty
        /// means a render can be attempted. Checked by the UI before it offers a 3D tab at all.
        /// </summary>
        public IReadOnlyList<string> Validate() => _options.Validate();

        /// <summary>
        /// Builds a design in the engine: converts its hull geometry if needed, validates the
        /// DNA against the live SOF library, and applies the two pattern masks.
        /// </summary>
        /// <remarks>
        /// The order is not arbitrary. <c>resolve</c> comes first because it is the only thing
        /// that knows which <c>.gr2</c> the hull uses, and it costs no device work; the geometry
        /// fetch and conversion follow; <c>build</c> comes last because it is the expensive step
        /// and there is no point paying for it against a DNA the library has already rejected.
        /// </remarks>
        public async Task<SkinrLoadResult> LoadAsync(SkinrResolvedDesign design,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(design);
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!design.IsRenderable)
                return SkinrLoadResult.Failed(
                    "This design cannot be rendered — its hull has no 3D identity in the " +
                    "static data.");

            var problems = Validate();
            if (problems.Count > 0)
                return SkinrLoadResult.Failed("3D preview unavailable: " +
                                              string.Join("; ", problems));

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                SkinrSidecarProcess sidecar = await EnsureStartedAsync(ct).ConfigureAwait(false);

                Report("Reading hull data…");
                SkinrSidecarResponse resolved = await sidecar.CallAsync(
                    BuildResolveRequest(design), s_resolveTimeout, ct).ConfigureAwait(false);

                var warnings = new List<string>(design.Warnings);
                string? refusal = DescribeResolveRefusal(design, resolved, warnings);
                if (refusal != null)
                    return SkinrLoadResult.Failed(refusal, warnings);

                string? geometry = await EnsureGeometryAsync(resolved, ct).ConfigureAwait(false);
                if (geometry == null)
                    return SkinrLoadResult.Failed(
                        "Could not prepare this hull's geometry. EveLens converts CCP's model " +
                        "files locally the first time a hull is shown; see the diagnostic log " +
                        "for what failed.", warnings);

                Report("Building design…");
                SkinrSidecarResponse built = await sidecar.CallAsync(
                    BuildBuildRequest(design, geometry), s_buildTimeout, ct).ConfigureAwait(false);

                built = await CompleteShipGeometryAsync(sidecar, design, geometry, built, ct)
                    .ConfigureAwait(false);

                _loadedSkinrId = design.SkinrId;
                return Interpret(design, resolved, built, warnings);
            }
            catch (SkinrSidecarException ex)
            {
                _loadedSkinrId = null;
                AppServices.TraceService?.Trace($"Skinr: load failed: {ex.Message}");
                return SkinrLoadResult.Failed(ex.Message);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Builds an ADDITIONAL ship into the scene at a formation offset — the Photo
        /// Op wingman. Converts whatever of the wingman's geometry is missing (same
        /// contract as the primary build) so it draws completely. Returns the built
        /// hull's radius, or null on failure.
        /// </summary>
        public async Task<double?> AddWingmanAsync(SkinrResolvedDesign design,
            IReadOnlyList<double> offset, CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var sidecar = _process;
            if (sidecar?.IsRunning != true || string.IsNullOrEmpty(design.Dna))
                return null;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                SkinrSidecarResponse built = await sidecar.CallAsync(new SkinrSidecarRequest
                {
                    Op = "wingman",
                    Dna = design.Dna,
                    Offset = offset
                }, s_buildTimeout, ct).ConfigureAwait(false);
                if (built.Ok != true)
                    return null;

                // The wingman's unconverted meshes, fixed the same way the primary's
                // are: convert, then geometry-map (which re-repoints all wingmen).
                var missing = Dark(built.WingmanGeometry);
                if (missing.Count > 0 && _converter != null)
                {
                    Report($"Preparing {missing.Count} wingman parts…");
                    var entries = new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);
                    foreach (string path in missing)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            string? cmf = await _converter
                                .ConvertAsync(GrannySourceOf(path), null, ct)
                                .ConfigureAwait(false);
                            if (!string.IsNullOrWhiteSpace(cmf))
                                entries[path] = cmf;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            AppServices.TraceService?.Trace(
                                $"Skinr: wingman convert failed {path}: {ex.Message}");
                        }
                    }
                    if (entries.Count > 0)
                    {
                        await sidecar.CallAsync(new SkinrSidecarRequest
                        {
                            Op = "geometry-map",
                            GeometryEntries = entries
                        }, s_resolveTimeout, ct).ConfigureAwait(false);
                    }
                }
                return built.Radius;
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: wingman failed: {ex.Message}");
                return null;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Repositions a built wingman (its radius is only known after the
        /// build, so slots are computed and applied afterwards).</summary>
        public async Task<bool> MoveWingmanAsync(int index, IReadOnlyList<double> offset,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var sidecar = _process;
            if (sidecar?.IsRunning != true)
                return false;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                SkinrSidecarResponse r = await sidecar.CallAsync(new SkinrSidecarRequest
                {
                    Op = "wingman-move",
                    Index = index,
                    Offset = offset
                }, s_cameraTimeout, ct).ConfigureAwait(false);
                return r.Ok == true;
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: wingman move failed: {ex.Message}");
                return false;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Disbands the formation: removes every wingman from the scene.</summary>
        public async Task<bool> ClearWingmenAsync(CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var sidecar = _process;
            if (sidecar?.IsRunning != true)
                return false;
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                SkinrSidecarResponse r = await sidecar.CallAsync(
                    new SkinrSidecarRequest { Op = "wingman-clear" },
                    s_cameraTimeout, ct).ConfigureAwait(false);
                return r.Ok == true;
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: disband failed: {ex.Message}");
                return false;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Applies an environment preset: a backdrop mode plus optional sun overrides, in a
        /// single <c>scene</c> round trip. Returns false when the sidecar is not running or
        /// declined the change — the caller keeps its previous switcher state either way.
        /// </summary>
        public async Task<bool> SetSceneAsync(string backdrop,
            IReadOnlyList<double>? sunColor, IReadOnlyList<double>? sunDirection,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process?.IsRunning != true)
                return false;

            var request = new SkinrSidecarRequest
            {
                Op = "scene",
                Backdrop = backdrop,
                SunColor = sunColor,
                SunDirection = sunDirection
            };

            try
            {
                SkinrSidecarResponse response = await _process
                    .CallAsync(request, s_resolveTimeout, ct).ConfigureAwait(false);
                return response.Ok != false;
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: scene change failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Points the camera. Orbit angles are degrees; distance is in the hull's own units, and
        /// null means "keep the framing <c>build</c> chose", which is the auto-framed default.
        /// </summary>
        public async Task<SkinrSidecarCamera?> SetCameraAsync(double yaw, double pitch,
            double? distance = null, double? fov = null,
            IReadOnlyList<double>? eye = null, IReadOnlyList<double>? at = null,
            IReadOnlyList<double>? shipOffset = null,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process?.IsRunning != true)
                return null;

            var request = new SkinrSidecarRequest
            {
                Op = "camera",
                Yaw = yaw,
                Pitch = pitch,
                Distance = distance,
                Fov = fov,
                Eye = eye,
                At = at,
                ShipOffset = shipOffset
            };

            try
            {
                SkinrSidecarResponse response = await _process
                    .CallAsync(request, s_cameraTimeout, ct).ConfigureAwait(false);
                return response.Camera;
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: camera move failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Changes the render size in place, without restarting the renderer. Returns the size the
        /// sidecar settled on, or null when there is no running sidecar or the request failed.
        /// </summary>
        /// <remarks>
        /// <para>This exists because the size is <em>not</em> welded to the device, contrary to what
        /// this class asserted for most of the feature's life. Trinity's driver reads its destination
        /// target's dimensions on every frame and allocates its internal buffers from a size-keyed
        /// pool, so a new target is all a new size needs — see <see cref="SkinrRenderQuality"/> for
        /// the specifics and <see cref="SkinrRenderResolution"/> for what the user is choosing
        /// between.</para>
        ///
        /// <para>The returned size may be smaller than the one requested, and the caller must
        /// display what came back rather than what it asked for: the sidecar applies a hardware
        /// pixel ceiling, and a software device applies a much lower one. It surrenders supersampling
        /// before resolution, so what typically comes back is the requested size with fewer samples.
        /// </para>
        ///
        /// <para>Timed against the build budget rather than the camera one. A resize renders the
        /// warm-up frames — TAA's history is at the old size, so the first frames after it are
        /// converging rather than final — and on a software device those are seconds, not
        /// milliseconds.</para>
        /// </remarks>
        public async Task<SkinrRenderSize?> ResizeAsync(SkinrRenderSize size,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process?.IsRunning != true)
                return null;

            var request = new SkinrSidecarRequest
            {
                Op = "resize",
                Width = size.Width,
                Height = size.Height,
                Supersample = size.Supersample
            };

            try
            {
                SkinrSidecarResponse response = await _process
                    .CallAsync(request, s_buildTimeout, ct).ConfigureAwait(false);

                var applied = new SkinrRenderSize(
                    response.OutputWidth ?? size.Width,
                    response.OutputHeight ?? size.Height,
                    response.Supersample ?? size.Supersample);

                // The options carry the boot size, and RenderAsync falls back to them when a frame
                // arrives without dimensions. Leaving them stale would make that fallback describe
                // a target that no longer exists — a wrong stride on a real buffer, which is a torn
                // blit rather than a wrong number.
                _options.Width = applied.Width;
                _options.Height = applied.Height;
                _options.Supersample = applied.Supersample;

                if (applied != size)
                {
                    AppServices.TraceService?.Trace(
                        $"Skinr: renderer clamped {size} to {applied}");
                }

                return applied;
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: resize failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Renders the current design and returns the frame as raw BGRA bytes, ready to blit
        /// into a bitmap. Null when there is nothing loaded or the render failed.
        /// </summary>
        /// <remarks>
        /// The frame travels through a scratch file rather than the JSON pipe. A 1024×768 frame
        /// is 3 MB, and base64 over stdout would cost a copy, a 33% inflation and a parse before
        /// anything could be drawn. The sidecar writes to a <c>.part</c> and renames, so the host
        /// can never read a half-written frame.
        /// </remarks>
        public async Task<SkinrFrame?> RenderAsync(bool settle = true,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process?.IsRunning != true || _loadedSkinrId == null)
                return null;

            // Per-instance scratch: the viewer and the thumbnail pre-renderer are two
            // hosts sharing one cache directory, and a single "frame.bgra" would have
            // them overwriting each other's frames mid-read.
            string scratch = Path.Combine(_options.ResourceCacheDirectory, _frameScratch);
            var request = new SkinrSidecarRequest
            {
                Op = "render",
                // Shared memory first, scratch file as the sidecar's own fallback. The file
                // round-trip was ~10 MB of filesystem traffic per frame at 60 Hz — the
                // choppiness itself.
                Shm = _shmBase,
                Raw = scratch.Replace('\\', '/'),
                Settle = settle,
                // Animation frames are never dedup'd, so they don't pay for a 5 MB SHA-256.
                Digest = settle ? null : false,
                // One pass, stated explicitly. The sidecar's own default for an unsettled
                // render is two, which is right for a scripted capture and exactly wrong for
                // a drag: it doubled the cost of every frame the user's hand was waiting on,
                // for a second sample nothing was going to accumulate anyway with TAA off.
                Frames = settle ? null : 1
            };

            try
            {
                SkinrSidecarResponse response = await _process
                    .CallAsync(request, s_renderTimeout, ct).ConfigureAwait(false);

                int width = response.Width ?? _options.Width;
                int height = response.Height ?? _options.Height;
                int stride = response.Stride ?? width * 4;

                byte[]? pixels = null;
                if (response.Shm != null && response.RawBytes is > 0)
                    pixels = ReadSharedFrame(response.Shm, response.RawBytes.Value);

                if (pixels == null)
                {
                    if (response.Raw == null || !File.Exists(scratch))
                    {
                        AppServices.TraceService?.Trace(
                            "Skinr: render reported success but produced no frame");
                        return null;
                    }
                    pixels = await File.ReadAllBytesAsync(scratch, ct).ConfigureAwait(false);
                }

                // The one check worth making on a buffer we are about to hand a bitmap: a short
                // read here is a torn frame, and blitting it is an access violation rather than
                // a wrong picture.
                if (pixels.Length < (long)stride * height)
                {
                    AppServices.TraceService?.Trace(
                        $"Skinr: frame is {pixels.Length:N0} bytes, expected " +
                        $"{(long)stride * height:N0} for {width}x{height}");
                    return null;
                }

                // A frame that never converged is still a frame, and returning it is right — the
                // user gets a slightly soft picture instead of nothing. But it is worth saying so
                // once, with the numbers, because an unconverged capture is indistinguishable
                // from a converged one by inspection and the delta tail is the only thing that
                // says which knob was wrong.
                if (response.Settle is { Converged: false } report)
                    AppServices.TraceService?.Trace(
                        $"Skinr: frame never settled — {report.Frames} frames in " +
                        $"{report.ElapsedMilliseconds:n0}ms, last delta " +
                        $"{report.LastDelta ?? double.NaN:0.####} against epsilon " +
                        $"{report.Epsilon:0.####}" +
                        (report.DeltaTail is { Count: > 0 } tail
                            ? "; tail " + string.Join(", ", tail)
                            : string.Empty));

                return new SkinrFrame(width, height, stride, pixels,
                    response.MeanLuma ?? 0, response.Settled ?? false,
                    response.AntiAliased ?? false);
            }
            catch (SkinrSidecarException ex)
            {
                AppServices.TraceService?.Trace($"Skinr: render failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Copies a frame out of the sidecar's named shared-memory mapping. Returns null on
        /// any failure so the caller falls back to the scratch file — a slow frame beats a
        /// dropped one.
        /// </summary>
        /// <remarks>
        /// The mapping is created by the sidecar (pagefile-backed, name carries the
        /// dimensions) and stays alive as long as the sidecar holds it; we reopen only when
        /// the name changes, which happens exactly on resize. Ordering is safe without any
        /// lock: the sidecar finishes writing before it answers the render request, and
        /// only one render is ever in flight per host.
        /// </remarks>
        private byte[]? ReadSharedFrame(string name, int byteCount)
        {
            try
            {
                if (_shmOpenName != name)
                {
                    _shmFile?.Dispose();
                    _shmFile = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(
                        name, System.IO.MemoryMappedFiles.MemoryMappedFileRights.Read);
                    _shmOpenName = name;
                }

                byte[] pixels = new byte[byteCount];
                using var accessor = _shmFile!.CreateViewAccessor(
                    0, byteCount, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read);
                accessor.ReadArray(0, pixels, 0, byteCount);
                return pixels;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: shared-memory frame read failed ({ex.Message}); using file");
                _shmFile?.Dispose();
                _shmFile = null;
                _shmOpenName = null;
                return null;
            }
        }

        // --- request composition ---------------------------------------------

        /// <summary>
        /// The library lookup: what the hull's geometry is, and whether the SOF library agrees
        /// with the DNA our resolver composed from the SDE.
        /// </summary>
        /// <remarks>
        /// Both token lists are sent so the sidecar can name what is missing. The SDE and the
        /// client's <c>data.black</c> are two different release trains and can disagree, and
        /// "material cosm_x is not in the library" is a fixable report where "build failed" is
        /// not.
        /// </remarks>
        private static SkinrSidecarRequest BuildResolveRequest(SkinrResolvedDesign design) =>
            new()
            {
                Op = "resolve",
                Hull = design.Hull?.SofHullName,
                Faction = design.Hull?.SofFactionName,
                Race = design.Hull?.SofRaceName,
                Dna = design.Dna,
                Materials = design.Nanocoatings.Select(m => m.DnaToken)
                    .Concat(design.Patterns.Select(p => p.MaterialDnaToken))
                    .Where(t => t != SkinrDna.EmptyMaterialToken)
                    .Distinct()
                    .ToList(),
                Patterns = new[] { SkinrResolvedDesign.CarrierPatternName }
            };

        /// <summary>
        /// The build request, including THE MASK LAW: we do not append masks, we redirect the two
        /// the carrier pattern already made.
        /// </summary>
        /// <remarks>
        /// Every field on a mask spec is load-bearing and every one of them has a wrong default
        /// that looks plausible:
        ///
        /// <list type="bullet">
        /// <item><c>materialIndex</c> 4 and 5 are <c>SOURCE_PATTERN1</c> and
        /// <c>SOURCE_PATTERN2</c>. <c>EveCustomMask</c>'s constructor leaves it at 0, which is
        /// the hull's primary colour — a design that paints its own base colour through the mask
        /// looks intentional and is completely wrong.</item>
        /// <item><c>targetMaterials</c> is what admits the paint. Measured: a bound mask
        /// targeting nothing differs from a black mask by 0.19% of pixels, inside the renderer's
        /// own 0.74% noise floor; targeting everything differs by 2.5%. An empty vector is a
        /// silent no-op.</item>
        /// <item><c>clampU</c>/<c>clampV</c> are true only for <c>clamp-to-edge</c>. CCP's test is
        /// <c>== TA_CLAMP</c>, so <c>clamp-to-border</c> maps to false exactly like
        /// <c>repeat</c> — the intuitive reading puts a hard border on patterns that should
        /// tile.</item>
        /// </list>
        ///
        /// A layer whose pattern component is unknown to the catalog is dropped rather than sent
        /// with an empty texture path: binding "" to a sampler is how a hull ends up rendering
        /// with no pattern and no error. The drop is <see cref="SkinrResolvedDesign"/>'s call,
        /// not ours — <see cref="Interpret"/> has to be able to count what went missing, and it
        /// cannot do that if this method decides drawability with a local predicate.
        /// </remarks>
        internal static SkinrSidecarRequest BuildBuildRequest(SkinrResolvedDesign design,
            string geometryResPath)
        {
            var drawable = design.DrawablePatterns.ToList();

            return new SkinrSidecarRequest
            {
                Op = "build",
                Dna = design.Dna,
                // The design id keys the sidecar's built-ship LRU: revisiting a
                // recently viewed design restores the parked ship instead of paying
                // the full build again.
                CacheKey = string.IsNullOrEmpty(design.SkinrId) ? null : design.SkinrId,
                GeometryResPath = geometryResPath,
                AutoFrame = true,
                // The blend mode travels beside the masks, not inside them: it is one fact per
                // design and the renderer expresses it as a shader permutation on the hull
                // effects, while the masks stay SOURCE_PATTERN1/2 exactly as the engine seeds
                // them. See SkinrResolvedPattern.MaterialIndex for the measurements.
                PatternBlendMode = design.PatternBlendMode,
                PatternTextures = drawable
                    .Select(p => new SkinrSidecarTexture
                    {
                        Name = p.TextureName,
                        Path = p.TextureResourcePath
                    })
                    .ToList(),
                Masks = drawable
                    .Select(p => new SkinrSidecarMask
                    {
                        Layer = p.LayerIndex,
                        MaterialIndex = p.MaterialIndex,
                        Position = p.Position,
                        Scaling = p.Scaling,
                        Rotation = p.Rotation,
                        TargetMaterials = p.TargetMaterials,
                        IsMirrored = p.IsMirrored,
                        ClampU = p.ClampU,
                        ClampV = p.ClampV,
                        SamplerU = p.SamplerAddressU,
                        SamplerV = p.SamplerAddressV
                    })
                    .ToList(),
                Darkhull = BuildDarkhullSpec(design)
            };
        }

        /// <summary>
        /// The Darkhull painting order — the tech slot's coating, aimed at the areas ordinary
        /// material application skips by design. Sent for every design that has a tech slot
        /// resolved; the sidecar decides whether the hull actually has Darkhull areas, so no
        /// hull list exists on either side.
        /// </summary>
        private static SkinrSidecarDarkhull? BuildDarkhullSpec(SkinrResolvedDesign design)
        {
            SkinrResolvedMaterial? tech = design.Nanocoatings
                .FirstOrDefault(m => m.SlotId == SkinrSlot.TechArea);
            if (tech == null)
                return null;

            // cosmeticsConst.DARKHULL_TEXTURE_MAP, verbatim: the constant-index texture that
            // makes every darkhull pixel read the material slot the tech coating landed in.
            string materialMap = tech.MaterialPosition switch
            {
                2 => "res:/texture/Global/darkGray.dds",
                3 => "res:/texture/Global/lightGray.dds",
                4 => "res:/texture/Global/white.dds",
                _ => "res:/texture/Global/black.dds"
            };

            string? material = tech.Component?.ResourceFile;
            return new SkinrSidecarDarkhull
            {
                MaterialMap = materialMap,
                Material = string.IsNullOrEmpty(material) ? null : material
            };
        }

        // --- interpretation ---------------------------------------------------

        /// <summary>
        /// Turns a <c>resolve</c> response into a refusal, or null to proceed. Additive warnings
        /// go into <paramref name="warnings"/> — they degrade the render, they do not stop it.
        /// </summary>
        private static string? DescribeResolveRefusal(SkinrResolvedDesign design,
            SkinrSidecarResponse resolved, List<string> warnings)
        {
            if (resolved.HullKnown == false)
                return $"The renderer's ship library has no hull called " +
                       $"'{design.Hull?.SofHullName}'. This usually means EveLens's static data " +
                       $"is newer than the graphics data it downloaded.";

            if (resolved.DnaValid == false)
                return "The renderer rejected this design's recipe" +
                       (string.IsNullOrEmpty(resolved.DnaError)
                           ? "." : $": {resolved.DnaError}");

            if (resolved.Missing is { Count: > 0 })
            {
                // Not fatal: a DNA that validates will build, and a missing token falls through
                // to the hull's stock colour. Worth saying, though — the alternative is a user
                // wondering why one panel is the wrong shade.
                warnings.Add("The graphics data is missing " +
                             string.Join(", ", resolved.Missing) +
                             " — those parts will show the hull's default colours.");
            }

            if (resolved.CarrierSupportsHull == false)
                warnings.Add("This hull cannot carry pattern layers in the 3D preview, so only " +
                             "its four base colours are shown.");

            return null;
        }

        /// <remarks>
        /// Internal rather than private so it can be unit tested without a live engine. That is
        /// not test-driven convenience: this method's entire job is deciding what to tell the
        /// user, and its previous version told them nothing at all when every pattern layer
        /// dropped. Reporting logic that cannot be exercised without booting Trinity is
        /// reporting logic that never gets a regression test.
        /// </remarks>
        internal static SkinrLoadResult Interpret(SkinrResolvedDesign design,
            SkinrSidecarResponse resolved, SkinrSidecarResponse built, List<string> warnings)
        {
            SkinrSidecarMaskReport? masks = built.Masks;
            SkinrSidecarTextureBinding? binding = built.TextureBinding;

            // Three different counts, and conflating any two of them is how a patternless render
            // passed for a correct one. `requested` is the recipe ESI sent. `sent` is what we
            // could compose a mask spec for. `rebound` is what the shaders actually took.
            int requested = design.Patterns.Count;
            List<SkinrResolvedPattern> dropped = design.UndrawablePatterns.ToList();
            int sent = requested - dropped.Count;

            // Reported first, because it is the only failure here that happens before the
            // renderer is involved — and the one that used to be unreportable. Naming the layer
            // and its component ID matters: this is a stale-catalog symptom, and the ID is what
            // makes it fixable rather than mysterious.
            if (dropped.Count > 0)
            {
                warnings.Add(dropped.Count == requested
                    ? $"None of this design's {requested} pattern layer(s) could be drawn — " +
                      $"the graphics data has no mask texture for pattern component(s) " +
                      $"{string.Join(", ", dropped.Select(p => p.PatternComponentId))}. The " +
                      $"preview shows the nanocoatings only."
                    : $"{dropped.Count} of this design's {requested} pattern layer(s) could not " +
                      $"be drawn — no mask texture for pattern component(s) " +
                      $"{string.Join(", ", dropped.Select(p => p.PatternComponentId))}.");
            }

            // rebound > 0 is the verified signal that this hull's shaders declare the mask
            // samplers at all. Measured on a Slasher: 14 mesh-area effects, 28 rebinds — both
            // layers on every area. `added` is the failure mode wearing a success's clothes.
            //
            // Gated on `sent`, not on `requested`: if every layer dropped we never asked the
            // shaders for anything, so their support is untested and claiming they lack it would
            // point the user at the wrong culprit. The drop is already reported above.
            bool supportsPatterns = sent == 0 || (binding?.Rebound ?? 0) > 0;

            if (sent > 0 && !supportsPatterns)
            {
                warnings.Add("This hull's shaders do not support SKINR pattern layers, so the " +
                             "preview shows its base colours only.");
            }
            else if (binding is { Added: > 0 })
            {
                warnings.Add("Some of this hull's surfaces do not accept pattern layers; the " +
                             "pattern may be missing from part of the ship.");
            }

            if (masks != null && masks.Failed > 0)
                warnings.Add($"{masks.Failed} of this design's pattern layers could not be " +
                             $"applied.");

            // An engine mask we did not claim is still bound to res:/texture/global/black.dds and
            // paints nothing. That is only a defect when there was a layer to put in it.
            //
            // `Unclaimed` counts ENGINE MASK SLOTS, not design layers, and the engine seeds a fixed
            // two on every SKINR-capable hull — materialIndex 4 and 5, SOURCE_PATTERN1 and
            // SOURCE_PATTERN2, capped by EVE_SPACEOBJECT_CUSTOWMASK_MAX. A one-pattern design
            // therefore leaves slot 5 idle *by construction*, which is the correct and universal
            // outcome, not a loss. Reading the slot count as a layer count told every owner of a
            // single-pattern design that their paint might be incomplete while it was rendering
            // exactly right — measured on a Charon: patterns 1/1 sent, masks 1 overridden,
            // unclaimed [5].
            //
            // So compare against how many slots we expected to leave alone. Anything beyond that is
            // a real undriven layer and still worth saying out loud. Always traced either way,
            // because the raw indices stay useful even when they are benign.
            IReadOnlyList<int>? unclaimed = masks?.Unclaimed;
            int expectedIdle = Math.Max(0, (masks?.Preexisting ?? 0) - sent);
            if (unclaimed is { Count: > 0 } && unclaimed.Count > expectedIdle
                && dropped.Count == 0 && sent > 0)
            {
                warnings.Add($"The renderer left {unclaimed.Count - expectedIdle} pattern " +
                             $"layer(s) inert — the design's paint may be incomplete.");
            }

            AppServices.TraceService?.Trace(
                $"Skinr: built {design.SkinrId} '{design.Name}' — " +
                $"patterns {sent}/{requested} sent" +
                (dropped.Count > 0
                    ? $" ({dropped.Count} dropped: " +
                      string.Join("; ", dropped.Select(
                          p => $"layer {p.LayerIndex} component {p.PatternComponentId}")) + ")"
                    : string.Empty) + ", " +
                $"masks {masks?.Overridden ?? 0} overridden / {masks?.Appended ?? 0} appended, " +
                (unclaimed is { Count: > 0 }
                    ? $"unclaimed [{string.Join(", ", unclaimed)}], " : string.Empty) +
                $"binding {binding?.Rebound ?? 0} rebound / {binding?.Added ?? 0} added across " +
                $"{binding?.Effects ?? 0} effects" +
                (binding?.EffectFiles is { Count: > 0 } files
                    ? $" [{string.Join(", ", files)}]" : string.Empty));

            return new SkinrLoadResult(true, null, warnings, built.Radius ?? 0,
                supportsPatterns, masks, binding, resolved.Category ?? string.Empty,
                resolved.IsSkinned ?? false);
        }

        // --- lifecycle --------------------------------------------------------

        /// <summary>
        /// Returns a live sidecar, starting or restarting as needed. Caller holds the gate.
        /// </summary>
        private async Task<SkinrSidecarProcess> EnsureStartedAsync(CancellationToken ct)
        {
            if (_process is { IsRunning: true })
                return _process;

            if (_process != null)
            {
                Report("Restarting the renderer…");
                AppServices.TraceService?.Trace(
                    "Skinr: discarding a faulted sidecar and starting a fresh one");
                _process.Dispose();
                _process = null;
                _loadedSkinrId = null;
            }

            _options.EnsureDirectories();
            Report("Starting the renderer — this takes up to a minute the first time…");

            var sidecar = new SkinrSidecarProcess(_options,
                message => AppServices.TraceService?.Trace(message));
            sidecar.ProgressReported += OnSidecarEvent;

            try
            {
                await sidecar.StartAsync(ct).ConfigureAwait(false);
            }
            catch (Exception)
            {
                sidecar.ProgressReported -= OnSidecarEvent;
                sidecar.Dispose();
                throw;
            }

            _process = sidecar;
            AppServices.TraceService?.Trace(
                $"Skinr: renderer ready — {sidecar.SidecarVersion} on {sidecar.Device}, " +
                $"jail: {sidecar.JailLimits}");
            return sidecar;
        }

        /// <summary>
        /// Turns the sidecar's unsolicited events into progress text.
        /// </summary>
        /// <remarks>
        /// Two events are deliberately not reported. <c>ready</c> is bookkeeping the caller
        /// already knows about, and <c>fatal</c> is raised as an exception by the transport with
        /// the reason attached — reporting it here as well would put the bare word "fatal" on
        /// screen a moment before the real message, which reads like two separate failures.
        /// Anything else is relayed under its own name: an unknown event from a newer sidecar is
        /// better shown than hidden.
        ///
        /// <para><c>working</c> is the one event with a body worth reading. It arrives every couple
        /// of seconds while the engine is pumping, and on a cold cache the honest thing to show is
        /// what it is actually doing — downloading — with the amount so far. A user waiting four
        /// minutes on a first render deserves to know the wait is a download and that it is
        /// progressing, not a spinner that could equally mean a hang.</para>
        /// </remarks>
        private void OnSidecarEvent(SkinrSidecarResponse response)
        {
            if (response.Event is not { Length: > 0 } name)
                return;
            if (name is "ready" or "fatal")
                return;

            if (name == "working")
            {
                Report(DescribeWork(response));
                return;
            }

            Report(name);
        }

        /// <summary>
        /// One line of progress from a heartbeat: what stage, and how much has come down.
        /// </summary>
        /// <remarks>
        /// <para>The byte count is deliberately omitted below a megabyte. Early in a build it
        /// flickers through small values and a number that jitters reads as noise; once it is
        /// worth mentioning it is worth a whole megabyte.</para>
        ///
        /// <para>The remaining count is preferred over bytes when the engine has a resource queue,
        /// because it is the number that actually moves. A cold hull fetch is hundreds of small
        /// textures: the megabyte figure can sit still for a quarter of a minute while forty files
        /// go past, and a progress line that appears frozen is worse than none. When a queue has
        /// stopped draining for more than a few seconds that gets said out loud too — the honest
        /// version of a spinner, and the difference between "this is slow" and "this is stuck".</para>
        /// </remarks>
        private static string DescribeWork(SkinrSidecarResponse response)
        {
            string stage = Capitalize(string.IsNullOrWhiteSpace(response.Op)
                ? "Working" : response.Op!);
            long bytes = response.BytesDownloaded ?? 0;
            long files = response.FilesDownloaded ?? 0;
            int pending = response.PendingResources ?? 0;
            long stalled = response.StalledMilliseconds ?? 0;

            if (pending > 0)
            {
                string queue = string.Format(CultureInfo.CurrentCulture,
                    "{0}… {1:n0} resource{2} remaining", stage, pending, pending == 1 ? "" : "s");

                if (stalled >= 5000)
                    queue += string.Format(CultureInfo.CurrentCulture,
                        " (waiting {0:n0}s)", stalled / 1000);

                return queue;
            }

            if (bytes < 1024 * 1024)
                return stage + "…";

            return string.Format(CultureInfo.CurrentCulture,
                "{0}… {1:n0} files, {2:n0} MB downloaded",
                stage, files, bytes / (1024 * 1024));
        }

        private static string Capitalize(string value) =>
            value.Length == 0 ? value : char.ToUpper(value[0], CultureInfo.CurrentCulture) + value[1..];

        /// <summary>
        /// Ensures the hull's geometry exists as a <c>.cmf</c> in the override tree and returns
        /// its <c>res:/</c> path.
        /// </summary>
        /// <remarks>
        /// The sidecar reports <c>geometryResFilePath</c> as a <c>.gr2</c> because that is what
        /// CCP's own data says. Trinity cannot load one, so this is where the conversion happens
        /// — and where a hull that is already a <c>.cmf</c> (should CCP ever publish them) passes
        /// straight through, which is exactly the behaviour we want when that day comes.
        /// </remarks>
        /// <summary>
        /// Converts everything the first build found it could not draw — the ship's non-hull
        /// meshes and the studio room behind it — and rebuilds.
        /// </summary>
        /// <remarks>
        /// <para><b>Why this is a second pass and not part of the first.</b> Neither list exists
        /// before a build. A hull's other meshes — exhaust plumes, reactor glow, tube glow,
        /// additive glow billboards — appear nowhere in CCP's data that the host can read; they
        /// exist only on the ship the renderer assembles from DNA. The room's primitives are only
        /// discoverable once the scene has booted and walked them. There is no ordering in which
        /// one pass suffices; the honest flow is build, read what was missing, convert exactly
        /// that, rebuild.</para>
        ///
        /// <para><b>What it fixes on the ship.</b> Before this, the host converted one file — the
        /// hull — and passed no geometry map at all. A Rifter has nineteen meshes. Eighteen of them
        /// stayed pointed at unreadable <c>.gr2</c> files, reporting a resolved shader and
        /// <c>display=True</c>, and drew nothing. Every render we compared against CCP's Studio was
        /// a bare shell missing its entire self-illuminated layer.</para>
        ///
        /// <para><b>What it fixes behind the ship.</b> The same defect, one scope wider, and it is
        /// the reason the viewport was black. CCP's SKINR backdrop is not a shader, a cubemap or a
        /// clear colour — it is a 30-million-unit <c>cylinder_01a_ds</c> BackgroundGradient inside
        /// <c>skinrenv_holographic_01a</c>, and it is Granny geometry like everything else.
        /// Unconverted, an empty viewport measures mean luma 1.08 with a top-to-bottom ramp of
        /// −0.15. Converted, 22.55 with a +17.52 ramp, against CCP's own measured 22.02 — a 1.02×
        /// match. The room is re-repointed by the <c>geometry-map</c> op itself rather than by the
        /// rebuild, because it attaches once at boot and a rebuild never touches it.</para>
        ///
        /// <para><b>Two lists per scope, not one.</b> A missing <c>.gr2</c> is only half the defect;
        /// see <see cref="Dark"/> for the half that an unmapped list cannot express, and which left
        /// an Astero rendering nothing but its spotlights while every count read healthy.</para>
        ///
        /// <para><b>Failure is partial, never fatal.</b> A file whose conversion fails is left
        /// unmapped and the render still happens — one glow billboard short, or a dark backdrop, is
        /// worth showing — and the original build result is returned untouched if nothing could be
        /// converted, so this pass can only ever add. Everything is skipped entirely when both
        /// lists come back empty, which is the warm-cache case on a second viewing.</para>
        /// </remarks>
        private async Task<SkinrSidecarResponse> CompleteShipGeometryAsync(
            SkinrSidecarProcess sidecar, SkinrResolvedDesign design, string geometry,
            SkinrSidecarResponse built, CancellationToken ct)
        {
            if (_converter == null)
                return built;

            List<string> ship = Dark(built.ShipGeometry);
            List<string> room = Dark(built.LightEnv?.Geometry);
            List<string> hangar = Dark(built.HangarGeometry);
            var missing = ship.Concat(room).Concat(hangar)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (missing.Count == 0)
                return built;

            Report($"Preparing {missing.Count} more ship parts…");

            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in missing)
            {
                ct.ThrowIfCancellationRequested();

                // Ask the converter for the Granny original in every case. A mesh already pointing
                // at a .cmf that has no geometry is a file the resource tree does not have — the
                // cache pruned it, or a write failed — and converting its .gr2 source is what puts
                // it back. `GrannySourceOf` is that inverse; for the ordinary case it is identity.
                string source = GrannySourceOf(path);
                try
                {
                    string? cmf = await _converter.ConvertAsync(source, null, ct)
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(cmf))
                        continue;

                    // Keyed on the path the MESH HOLDS, not on what we converted. For a .gr2 the
                    // two differ and this is the repoint. For a recovered .cmf they are the same
                    // string, and the entry is still needed: the sidecar's repoint writes "" before
                    // the path, which drops the failed resource and re-requests it. Without an
                    // entry, a mesh that failed its first load never asks again.
                    entries[path] = cmf;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One unconvertible glow billboard must not cost the whole ship.
                    AppServices.TraceService?.Trace(
                        $"Skinr: could not convert {source}: {ex.Message}");
                }
            }

            if (entries.Count == 0)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: {missing.Count} meshes have no geometry we can convert; " +
                    "the render is missing its additive layer or its backdrop");
                return built;
            }

            // This call also re-repoints the already-attached studio room, so the backdrop is
            // fixed by the op regardless of whether a rebuild follows.
            SkinrSidecarResponse mapped = await sidecar.CallAsync(new SkinrSidecarRequest
            {
                Op = "geometry-map",
                GeometryEntries = entries
            }, s_resolveTimeout, ct).ConfigureAwait(false);

            if (room.Count > 0)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: studio backdrop — {mapped.LightEnvGeometry?.Repointed ?? 0} of " +
                    $"{room.Count} primitives now drawable, " +
                    $"{mapped.LightEnvGeometry?.Unmapped?.Count ?? 0} still unmapped");
            }

            // Only the ship needs a rebuild. If nothing on the ship was missing, the room has
            // already been fixed above and rebuilding would cost a full reassembly for nothing.
            if (!ship.Any(entries.ContainsKey))
                return built;

            Report("Rebuilding with the complete ship…");
            SkinrSidecarResponse complete = await sidecar.CallAsync(
                BuildBuildRequest(design, geometry), s_buildTimeout, ct).ConfigureAwait(false);

            int still = complete.ShipGeometry?.Unmapped?.Count ?? 0;
            AppServices.TraceService?.Trace(
                $"Skinr: converted {entries.Count} meshes, " +
                $"{complete.ShipGeometry?.Repointed ?? 0} repointed, {still} still unmapped");

            // Only accept the rebuild if it actually succeeded. A failed second build must not
            // replace a first one that produced a viewable, if incomplete, ship.
            return complete.Ok == false ? built : complete;
        }

        /// <summary>
        /// Every mesh path in a geometry report that is on the ship and not on the screen.
        /// </summary>
        /// <remarks>
        /// <para>Two lists, because one of them cannot see half the problem.
        /// <see cref="SkinrSidecarGeometryReport.Unmapped"/> names <c>.gr2</c> files we have no
        /// conversion for. <see cref="SkinrSidecarGeometryReport.NotLoaded"/> names meshes that hold
        /// a resolved path and no geometry at all — which includes the case the first list is
        /// structurally blind to: a mesh already pointing at a <c>.cmf</c> that is not in the
        /// resource tree. That one counts as <c>native</c>, appears in no unmapped list, and draws
        /// nothing.</para>
        ///
        /// <para>An Astero is what found it — two native meshes, an empty unmapped list, and a
        /// viewport containing nothing but its spotlights. The resource cache prunes itself at 2 GB,
        /// so a converted file going missing under a warm design is a case that will happen in the
        /// field rather than a hypothetical.</para>
        /// </remarks>
        internal static List<string> Dark(SkinrSidecarGeometryReport? report)
        {
            if (report == null)
                return new List<string>();

            return (report.Unmapped ?? new List<string>())
                .Concat(report.NotLoaded ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// The Granny original a mesh path came from: <c>.cmf</c> back to <c>.gr2</c>, everything
        /// else unchanged.
        /// </summary>
        /// <remarks>
        /// The converter's input is always a <c>res:/</c> <c>.gr2</c>, because that is the only form
        /// CCP publish. A <c>.cmf</c> in a not-loaded list is therefore something we wrote once and
        /// no longer have, and this is how it gets asked for again. Deliberately not a general path
        /// rewrite: the converter chooses its own output location — one CCP tree flattens a
        /// directory level — so the only safe direction to invert is the extension.
        /// </remarks>
        internal static string GrannySourceOf(string path) =>
            path.EndsWith(".cmf", StringComparison.OrdinalIgnoreCase)
                ? string.Concat(path.AsSpan(0, path.Length - 4), ".gr2")
                : path;

        private async Task<string?> EnsureGeometryAsync(SkinrSidecarResponse resolved,
            CancellationToken ct)
        {
            string? path = resolved.GeometryResFilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                AppServices.TraceService?.Trace(
                    "Skinr: the hull record names no geometry file");
                return null;
            }

            if (path.EndsWith(".cmf", StringComparison.OrdinalIgnoreCase))
                return path;

            if (_converter == null)
            {
                AppServices.TraceService?.Trace(
                    "Skinr: no geometry converter configured — cannot load a .gr2 hull");
                return null;
            }

            Report("Preparing hull geometry…");
            var progress = new Progress<double>(fraction => DownloadProgress?.Invoke(fraction));
            return await _converter.ConvertAsync(path, progress, ct).ConfigureAwait(false);
        }

        private void Report(string message)
        {
            Progress?.Invoke(message);
            AppServices.TraceService?.Trace("Skinr: " + message);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_process != null)
                _process.ProgressReported -= OnSidecarEvent;
            _process?.Dispose();
            _shmFile?.Dispose();
            _gate.Dispose();
        }
    }

    /// <summary>
    /// What happened when a design was built: whether it can be shown, what had to be worked
    /// around, and the measured facts the diagnostics care about.
    /// </summary>
    /// <remarks>
    /// <see cref="Warnings"/> and <see cref="Error"/> are deliberately different things. An
    /// error means there is nothing to draw. A warning means the picture is real but incomplete
    /// — a missing material, a hull that cannot wear patterns — and the UI shows the render
    /// <em>and</em> says so, because a subtly wrong render that claims to be correct is the one
    /// failure mode a user cannot detect.
    /// </remarks>
    public sealed class SkinrLoadResult
    {
        internal SkinrLoadResult(bool ok, string? error, IReadOnlyList<string> warnings,
            double radius, bool supportsPatterns, SkinrSidecarMaskReport? masks,
            SkinrSidecarTextureBinding? binding, string hullCategory, bool isSkinned)
        {
            Ok = ok;
            Error = error;
            Warnings = warnings;
            Radius = radius;
            SupportsPatterns = supportsPatterns;
            Masks = masks;
            TextureBinding = binding;
            HullCategory = hullCategory;
            IsSkinned = isSkinned;
        }

        internal static SkinrLoadResult Failed(string error,
            IReadOnlyList<string>? warnings = null) =>
            new(false, error, warnings ?? Array.Empty<string>(), 0, false, null, null,
                string.Empty, false);

        public bool Ok { get; }

        /// <summary>Why nothing can be drawn, in words fit for the UI. Null when <see cref="Ok"/>.</summary>
        public string? Error { get; }

        /// <summary>Things that degraded the render without preventing it.</summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>The hull's bounding radius — the orbit control's distance scale.</summary>
        public double Radius { get; }

        /// <summary>
        /// Whether this hull's shaders actually declared the pattern-mask samplers. False means
        /// the base colours are real but the pattern layers are not being drawn.
        /// </summary>
        public bool SupportsPatterns { get; }

        public SkinrSidecarMaskReport? Masks { get; }

        public SkinrSidecarTextureBinding? TextureBinding { get; }

        /// <summary>SOF hull category: <c>frigate</c>, <c>battleship</c>, and so on.</summary>
        public string HullCategory { get; }

        /// <summary>
        /// Whether CCP's hull record marks this geometry as skinned. Our converter is built
        /// without the Granny runtime, so a skinned hull's shaders expect a bone palette that
        /// does not exist — worth carrying so the fallback path is visible rather than inferred.
        /// </summary>
        public bool IsSkinned { get; }
    }

    /// <summary>One rendered frame: raw BGRA pixels plus what the renderer measured about them.</summary>
    /// <remarks>
    /// <see cref="MeanLuma"/> travels with the pixels on purpose. A black frame is structurally
    /// perfect — right size, right stride, stable digest — and brightness is the only thing that
    /// tells it apart from a correct render of a dark hull.
    /// </remarks>
    public sealed class SkinrFrame
    {
        internal SkinrFrame(int width, int height, int stride, byte[] pixels, double meanLuma,
            bool settled, bool antiAliased)
        {
            Width = width;
            Height = height;
            Stride = stride;
            Pixels = pixels;
            MeanLuma = meanLuma;
            Settled = settled;
            AntiAliased = antiAliased;
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>Bytes per row. Always <c>Width * 4</c> today, but read, not assumed.</summary>
        public int Stride { get; }

        /// <summary>B8G8R8A8, top-down. Ready for a <c>WriteableBitmap</c> without conversion.</summary>
        public byte[] Pixels { get; }

        /// <summary>Mean luma 0-255. Below about 1 means the render failed, not that it is dark.</summary>
        public double MeanLuma { get; }

        /// <summary>Whether TAA converged before the frame was captured.</summary>
        public bool Settled { get; }

        /// <summary>Whether a post-process chain was attached — Trinity's only path to AA.</summary>
        public bool AntiAliased { get; }
    }
}
