// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Everything needed to launch the render sidecar: where its interpreter and engine
    /// binaries are, where it may cache and read game resources, how big a frame to render,
    /// and the limits its jail applies.
    /// </summary>
    /// <remarks>
    /// This is a value object on purpose. The sidecar's configuration is entirely
    /// command-line — it reads no settings file and no environment beyond encoding — so the
    /// whole contract fits here and <see cref="BuildArguments"/> is the single place that knows
    /// the flag spellings. When the Python side gains a flag, exactly one C# file changes.
    ///
    /// <para><b>Why discovery is explicit rather than clever.</b> The engine is not ours to
    /// bundle casually: Trinity is a large native build with a Python 3.12 ABI dependency
    /// (<c>blue.pyd</c> links <c>python312.dll</c>, so <em>any</em> other interpreter, including
    /// a newer one, fails at import with a module-version conflict). So the runtime is located,
    /// never guessed: an explicit root, then the environment, then the layout we ship. A missing
    /// runtime is a first-class answer — <see cref="Validate"/> returns what is absent so the
    /// viewer can say "3D preview unavailable: render runtime not installed" instead of showing
    /// a stack trace or, worse, a blank pane.</para>
    ///
    /// <para><b>The cache directories are not interchangeable.</b>
    /// <see cref="ResourceCacheDirectory"/> is Blue's own CDN cache, written by the engine and
    /// keyed by CCP's hash paths. <see cref="ResourceOverrideDirectory"/> is ours: it holds the
    /// <c>.cmf</c> geometry we convert from CCP's <c>.gr2</c>, and it is registered as the
    /// <em>local</em> search path so it wins over the CDN. Pointing either at the other's tree
    /// produces a renderer that works until the first converted hull and then silently serves
    /// the wrong geometry.</para>
    /// </remarks>
    public sealed class SkinrSidecarOptions
    {
        /// <summary>Environment variable pointing at a complete render runtime tree.</summary>
        public const string RuntimeRootVariable = "EVELENS_SKINR_RUNTIME";

        /// <summary>
        /// Environment variable pointing at a Trinity <em>source</em> build — the developer
        /// path. Shares its name with the lab tooling so one variable configures both.
        /// </summary>
        public const string TrinityRootVariable = "EVELENS_TRINITY_ROOT";

        // --- what to launch ---------------------------------------------------

        /// <summary>Python 3.12 interpreter that can import <c>blue</c>. Mandatory.</summary>
        public string PythonPath { get; set; } = string.Empty;

        /// <summary>Full path to <c>skinr_sidecar.py</c>. Mandatory.</summary>
        public string ScriptPath { get; set; } = string.Empty;

        /// <summary>
        /// Working directory for the child. Defaults to the script's own directory, which is
        /// what makes the sidecar's relative <c>bin</c> search path resolve.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>Extra environment for the child, on top of encoding settings.</summary>
        public IDictionary<string, string> Environment { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Where <see cref="Discover"/> looked for a render runtime, in the order it looked, and
        /// what it found at each place. Empty when the options were built by hand.
        /// </summary>
        /// <remarks>
        /// <para><b>Why the search is part of the result.</b> Discovery tries several roots and each
        /// attempt overwrites the paths from the last one, so a failed search leaves the options
        /// holding whichever layout was tried <em>last</em>. Reporting only that produced a message
        /// naming four missing paths under the installed-build folder and never mentioning that
        /// <see cref="TrinityRootVariable"/> exists, was consulted, or was empty — which reads as
        /// "EveLens wants a folder next to the exe" when the real answer is "set one variable".
        /// The remedy is not a better-worded final state: it is keeping the search itself.</para>
        /// <para>Consumed by <see cref="Validate"/> when nothing at all was found, and worth tracing
        /// on success too — knowing <em>which</em> runtime won matters the moment a machine has
        /// both an installed copy and a build tree.</para>
        /// </remarks>
        public IList<string> DiscoverySteps { get; } = new List<string>();

        // --- where the engine lives -------------------------------------------

        /// <summary>Directory holding the Trinity art DLLs and <c>blue.pyd</c>.</summary>
        public string ArtDirectory { get; set; } = string.Empty;

        /// <summary>Directory holding the engine's vcpkg runtime DLLs.</summary>
        public string BinDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Root Blue searches for its <c>bin</c> assets. Defaults to
        /// <see cref="BinDirectory"/> when unset, which is correct for a shipped tree.
        /// </summary>
        public string? BinRoot { get; set; }

        // --- where resources live ---------------------------------------------

        /// <summary>Blue's CDN cache directory. Created if absent.</summary>
        public string ResourceCacheDirectory { get; set; } = string.Empty;

        /// <summary>Our converted-geometry tree, searched before the CDN. Created if absent.</summary>
        public string ResourceOverrideDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Resource index files handed to Blue. Without at least one, no <c>res:/</c> path
        /// resolves and every build fails — see <see cref="EveResourceService.GetIndexFileAsync"/>,
        /// which produces the file this list normally holds.
        /// </summary>
        public IList<string> ResourceIndexPaths { get; } = new List<string>();

        /// <summary>
        /// Full path to the PEM bundle Blue's libcurl must use, whose file name has to be
        /// <c>cacert.pem</c>. Not optional in practice — see the remarks.
        /// </summary>
        /// <remarks>
        /// <para>Blue resolves <c>bin://cacert.pem</c> and passes it to <c>CURLOPT_CAINFO</c> on
        /// every connection it creates, unconditionally and without checking that the file is
        /// there. Because <c>CURLOPT_CAINFO</c> overrides the platform trust store, a missing
        /// bundle means <em>no</em> HTTPS request the engine makes can ever succeed, on a machine
        /// whose certificate store is entirely healthy. It surfaces as
        /// <c>RuntimeError: Couldn't download file</c>, which is a description of the symptom
        /// three layers above the cause.</para>
        ///
        /// <para>The sidecar therefore points Blue's <c>bin</c> search path at this file's
        /// directory. That is safe rather than lucky: the <c>bin://</c> scheme has exactly one
        /// consumer in the engine, the certificate line itself. See
        /// <see cref="SkinrCertificateBundle"/> for where the file comes from.</para>
        /// </remarks>
        public string? CertificatePath { get; set; }

        /// <summary>Resource CDN origin. Overridable for a mirror or a test double.</summary>
        public string ResourceServer { get; set; } = "https://resources.eveonline.com";

        // --- render settings --------------------------------------------------

        /// <summary>Render target width in pixels.</summary>
        public int Width { get; set; } = 1024;

        /// <summary>Render target height in pixels.</summary>
        public int Height { get; set; } = 768;

        /// <summary>
        /// Device preference: <c>AUTO</c>, <c>HARDWARE</c>, <c>SOFTWARE</c>. AUTO tries the GPU
        /// first and falls back; the sidecar reports which it got, and the host logs it, because
        /// a SOFTWARE fallback is roughly 20× slower and explains an otherwise baffling wait.
        /// </summary>
        public string Device { get; set; } = "AUTO";

        /// <summary>
        /// Frames rendered before the first real one is trusted. Not padding: the driver's
        /// screen-space globals are not resolved on frame zero, and rendering into them
        /// produced the black-hull bug that cost a day to find.
        /// </summary>
        public int WarmupFrames { get; set; } = 8;

        /// <summary>Supersample factor, 1-4. Costs the square; 2 is the useful setting.</summary>
        public int Supersample { get; set; } = 1;

        /// <summary>
        /// Which backdrop the render sits against: <c>dome</c>, <c>room</c>, <c>studio</c>,
        /// <c>nebula</c> or <c>transparent</c>.
        /// </summary>
        /// <remarks>
        /// <para><c>room</c> is the default, and it is CCP's own scene background rather than an
        /// imitation of it: <c>skinrenv_holographic_01a</c>, whose <c>cylinder_01a_ds</c>
        /// BackgroundGradient at scale 3e7 is the grey gradient behind every ship in the game's
        /// SKINR studio. Measured against CCP's viewport at mean luma 22.02, it reads 22.55 with a
        /// +17.52 top-to-bottom ramp — 1.02×, with their dust specks and their arc.</para>
        ///
        /// <para><c>dome</c> — a grey gradient we generate as a cubemap — was the default for a
        /// while, and it existed only because the room appeared not to work. It did work; its four
        /// primitives are Granny <c>.gr2</c> files, and until the host converted them they reported
        /// themselves healthy and drew nothing, so the empty viewport read mean luma 1.08 with a
        /// ramp of −0.15. The dome stays as the fallback for a host with no geometry converter,
        /// which is the one case where the room genuinely cannot be made to draw.</para>
        ///
        /// <para>Two host-side alternatives were measured and closed rather than argued away.
        /// Compositing on alpha fails: every frame comes back 100% opaque with zero partial
        /// pixels, because the post-process chain writes 1.0 to alpha. Luma keying fails too:
        /// a pure black clear reads back as flat grey around luma 30 after the chain, which sits
        /// on top of the hull's own dark plating, so no threshold separates them. That leaves a
        /// real 3D backdrop as the root-cause fix, and it is the better one anyway — it renders
        /// before post-process, so it blooms and antialiases against the hull for free, and a
        /// SKINR nanocoating is a conductor with zero diffuse whose entire appearance is
        /// reflection. Until now it was reflecting a black cubemap.</para>
        ///
        /// <para>Named here rather than left to the sidecar's default so the contract is the
        /// host's. The sidecar defaults to the same value, but a renderer setting that only one
        /// side knows about is a setting nobody can find when it is wrong.</para>
        /// </remarks>
        public string Backdrop { get; set; } = "room";

        /// <summary>
        /// Whether CCP's star and nebula billboards draw behind the ship. Off by default.
        /// </summary>
        /// <remarks>
        /// <para>CCP's room resource brings a grey gradient cylinder, a reflection sphere, and
        /// per-faction nebula plates. Only the first is a studio backdrop; the plates carry the
        /// star field, and CCP's own SKINR Studio has no stars behind the ship. We had been
        /// scoring an in-space backdrop against a studio reference and reading a 1.02x luma match
        /// as parity.</para>
        ///
        /// <para>They also cost quality, which is why this is a default and not a preference. A
        /// star is a sub-pixel high-contrast point on a near-black field — the pathological input
        /// for TAA, which has no neighbourhood to resolve it against, so it crawls between frames
        /// and reprojection smears it. On a view whose entire subject is a metal coating judged by
        /// eye, that noise lands on top of the thing being judged.</para>
        /// </remarks>
        public bool Stars { get; set; }

        /// <summary>
        /// Where the backdrop cylinder sits, in multiples of the camera's orbit distance. Zero
        /// leaves CCP's authored scale alone.
        /// </summary>
        /// <remarks>
        /// <para>Fixes the backdrop vanishing on small hulls, and on any hull when zoomed in.
        /// CCP's room is authored at a fixed 300,000 units while the near plane scales with the
        /// orbit (<c>max(0.05, distance * 0.005)</c>), so the depth range the backdrop must
        /// survive depends on how close the camera is while its position does not: near/room is
        /// 1.5e-6 on an Astero against 5.0e-5 on a Charon, a 33x swing, and the far end quantises
        /// into the clear value.</para>
        ///
        /// <para>80 rather than a round number because the capital case is measured good (22.55
        /// mean against the game's 22.02) and a fix that moves a working case is a trade nobody
        /// agreed to. The Charon's 300,000/3770 is 79.6, so this reproduces today's geometry at
        /// capital scale to within half a percent and only moves the hulls that are broken. It
        /// also keeps the room's apparent size constant, since it already rides the camera.</para>
        /// </remarks>
        public double RoomAnchor { get; set; } = 80.0;

        /// <summary>
        /// Post-process chain resource, used only as a fallback. Trinity's anti-aliasing is TAA
        /// and TAA only runs inside a post-process chain, so clearing this trades every smooth
        /// edge for a small amount of time. Null means "no chain", which is a diagnostic setting,
        /// not a fast one.
        /// </summary>
        /// <remarks>
        /// A fallback because CCP's SKINR scene resource carries its own <c>Tr2PostProcess2</c>,
        /// and the sidecar prefers the scene's chain whenever one is present — so on the shipping
        /// configuration this path is not taken. It matters for the nebula scene and for any
        /// scene authored without one.
        /// </remarks>
        public string? PostProcess { get; set; } = "res:/dx9/default/postprocess.black";

        /// <summary>
        /// Chain stages to disable. <c>filmGrain</c> injects per-frame noise, which both dirties
        /// a still and stops the settle loop converging. <c>dynamicExposure</c> is deliberately
        /// NOT disabled any more: it is the engine's own per-frame auto-exposure (histogram to
        /// middle grey), and it is what keeps the game's white hulls gradient-rich instead of
        /// bleached and its dark hulls readable - a global normalisation, identical for every
        /// ship. The settle loop converges because adaptation is deterministic for a static
        /// frame; the sidecar constructs the effect with CCP's authored parameters.
        /// <c>bloom</c> is disabled to match the reference: the user's own client runs
        /// postProcessingQuality 1 (read from their core_public__.yaml), and the engine
        /// gates bloom at MEDIUM (Tr2PostProcess2.cpp:92) — every in-game reference
        /// screenshot is bloomless, and our bloom was the "greyish tint" they reported.
        /// </summary>
        public IList<string> DisabledPostProcessStages { get; } =
            new List<string> { "filmGrain", "bloom" };

        // --- containment ------------------------------------------------------

        /// <summary>
        /// Per-process memory ceiling. Trinity plus a 4K target plus a hull's textures sits well
        /// under this; the limit is there to stop unbounded growth from a malformed resource,
        /// not to run close to the edge.
        /// </summary>
        public long MemoryLimitBytes { get; set; } = 3L * 1024 * 1024 * 1024;

        /// <summary>
        /// CPU share for the jail, or 0 for unlimited. The software rasteriser will saturate
        /// every core, which a laptop user experiences as fan noise and a stalled UI rather than
        /// as a faster render.
        /// </summary>
        public int CpuPercent { get; set; } = 60;

        /// <summary>
        /// How long boot may take. Generous by measurement, not by guess: a cold start
        /// bootstraps Blue, creates a device, compiles shaders and renders warm-up frames, and
        /// on a SOFTWARE device that has been observed at 46-49 seconds.
        /// </summary>
        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(180);

        /// <summary>Grace period for a clean shutdown before the jail closes.</summary>
        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Sets <see cref="Width"/>, <see cref="Height"/> and <see cref="Supersample"/> to the
        /// size the given tier and resolution choice resolve to.
        /// </summary>
        /// <remarks>
        /// <para>These are the <em>boot</em> size only. They used to be the final word, because the
        /// render target was believed to be fixed at device creation; it is not, and the sidecar's
        /// <c>resize</c> op changes it in place — see <see cref="SkinrRenderQuality"/> for what the
        /// engine actually does. So a caller that knows its pane's size should still boot small and
        /// resize once laid out: a smaller first target is a measurably faster cold start, and the
        /// pane's real size is not knowable at launch anyway.</para>
        /// </remarks>
        public void ApplyQuality(SkinrRenderQuality quality,
            SkinrRenderResolution resolution = SkinrRenderResolution.MatchViewport)
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(resolution, quality);
            Width = size.Width;
            Height = size.Height;
            Supersample = size.Supersample;
        }

        /// <summary>
        /// Turns this configuration into the sidecar's argv tail, in the flag spellings the
        /// Python side parses. The script path itself is added by the caller.
        /// </summary>
        public IEnumerable<string> BuildArguments()
        {
            var args = new List<string>
            {
                "--art", ArtDirectory,
                "--vbin", BinDirectory,
                "--bin-root", TrailingSlash(BinRoot ?? BinDirectory),
                "--res-cache", ResourceCacheDirectory,
                "--res-override", ResourceOverrideDirectory,
                "--width", Width.ToString(),
                "--height", Height.ToString(),
                "--device", Device,
                "--warmup-frames", WarmupFrames.ToString(),
                "--supersample", Math.Clamp(Supersample, 1, 4).ToString(),
                "--server", ResourceServer,
                "--backdrop", Backdrop,
                "--stars", Stars ? "on" : "off",
                "--room-anchor", RoomAnchor.ToString(CultureInfo.InvariantCulture)
            };

            foreach (string index in ResourceIndexPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
                args.AddRange(new[] { "--index", index });

            if (!string.IsNullOrWhiteSpace(CertificatePath))
                args.AddRange(new[] { "--cert", CertificatePath! });

            if (PostProcess == null)
                args.Add("--no-postprocess");
            else
                args.AddRange(new[] { "--postprocess", PostProcess });

            // "none" is the sidecar's explicit empty-list token; an empty string would be
            // parsed as a stage named "".
            args.AddRange(new[]
            {
                "--pp-disable",
                DisabledPostProcessStages.Count == 0
                    ? "none"
                    : string.Join(",", DisabledPostProcessStages)
            });

            return args;
        }

        /// <summary>
        /// What is missing or wrong, in words a user can act on. Empty means launchable.
        /// </summary>
        /// <remarks>
        /// Checked before spawning rather than after failing, because the failures this catches
        /// are indistinguishable from each other once Python is in the picture: a missing engine
        /// directory and a missing index both surface as an import error or an empty render.
        /// </remarks>
        public IReadOnlyList<string> Validate()
        {
            var problems = new List<string>();

            // Nothing of the runtime present is one problem, not four. Listing four missing paths
            // under the last-tried layout invites someone to create those exact folders, which is
            // the wrong action; the right one is naming a root. When some of it IS present the
            // per-piece messages are the useful ones — that is a broken install, not a missing one.
            bool anyRuntimePiece =
                File.Exists(PythonPath) || File.Exists(ScriptPath) ||
                Directory.Exists(ArtDirectory) || Directory.Exists(BinDirectory);

            if (!anyRuntimePiece && DiscoverySteps.Count > 0)
            {
                problems.Add(
                    "no render runtime found. Searched: " + string.Join("; ", DiscoverySteps) +
                    $". Point {TrinityRootVariable} at a Trinity build tree, or " +
                    $"{RuntimeRootVariable} at an installed render runtime.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(PythonPath) || !File.Exists(PythonPath))
                    problems.Add($"render interpreter not found ({Describe(PythonPath)})");
                if (string.IsNullOrWhiteSpace(ScriptPath) || !File.Exists(ScriptPath))
                    problems.Add($"render script not found ({Describe(ScriptPath)})");
                if (string.IsNullOrWhiteSpace(ArtDirectory) || !Directory.Exists(ArtDirectory))
                    problems.Add($"engine art directory not found ({Describe(ArtDirectory)})");
                if (string.IsNullOrWhiteSpace(BinDirectory) || !Directory.Exists(BinDirectory))
                    problems.Add($"engine runtime directory not found ({Describe(BinDirectory)})");
            }
            if (string.IsNullOrWhiteSpace(ResourceCacheDirectory))
                problems.Add("no resource cache directory configured");
            if (string.IsNullOrWhiteSpace(ResourceOverrideDirectory))
                problems.Add("no geometry override directory configured");
            if (ResourceIndexPaths.Count == 0 || !ResourceIndexPaths.Any(File.Exists))
                problems.Add("no resource index available — EveLens could not reach CCP's CDN " +
                             "and has no cached copy");
            else if (!ResourceIndexPaths.Any(
                         p => p.EndsWith("-shaders.txt", StringComparison.OrdinalIgnoreCase)))
            {
                // Reported as a launch problem rather than a warning, because the render it
                // produces is not degraded — it is entirely black, and indistinguishable from a
                // failure with a real cause. Better to say why up front than to let someone spend
                // an afternoon on a picture that was never going to have anything in it.
                problems.Add("the compiled-shader index is missing, so every render would be " +
                             "black — EveLens needs CCP's resfileindex_Windows.txt as well as " +
                             "the main index");
            }

            // Checked here, in words, because the engine will not tell us: it reports a missing
            // CA bundle as a failed download, and every hour spent on that is an hour spent
            // looking at the network instead of at a file. See CertificatePath's remarks.
            if (string.IsNullOrWhiteSpace(CertificatePath) || !File.Exists(CertificatePath))
            {
                problems.Add("no certificate bundle available, so the renderer cannot fetch game " +
                             $"resources ({Describe(CertificatePath ?? string.Empty)})");
            }
            else if (!string.Equals(Path.GetFileName(CertificatePath),
                         SkinrCertificateBundle.FileName, StringComparison.OrdinalIgnoreCase))
            {
                // The name is compiled into the engine. A correctly-generated bundle under the
                // wrong name is invisible to it, and would fail exactly like having none.
                problems.Add($"the certificate bundle must be named " +
                             $"{SkinrCertificateBundle.FileName} for the render engine to find " +
                             $"it (got {Path.GetFileName(CertificatePath)})");
            }
            if (Width < 16 || Height < 16)
                problems.Add($"render size {Width}x{Height} is below the minimum 16x16");

            return problems;
        }

        /// <summary>
        /// Creates the cache directories this configuration names. Called by the host before
        /// launch: Blue does not create its own cache folder and fails obscurely without it.
        /// </summary>
        /// <remarks>
        /// <para>Including the two-hex-character fan-out, and that part is not optional. CCP's
        /// resource index names every file by a sharded cache name — <c>a9/a9d1721dd5cc6d54_…</c>
        /// — and <c>RemoteFileCache::CacheContentsOfRemoteStream</c> writes straight to
        /// <c>&lt;cache&gt;/a9/a9d17….tmp</c> through <c>BlueFileStream::Create</c>, which does
        /// not create intermediate directories. In the real client the launcher lays this tree
        /// down before the game ever runs; nothing in the engine does it.</para>
        /// <para>Without the fan-out the failure is invisible rather than loud: the download
        /// succeeds, the checksum verifies, the stream is handed to the caller and the render is
        /// correct — only the write to disk fails, and it fails into a log channel. Every
        /// subsequent run then re-downloads the same 217 MB, because the read side asks
        /// <c>FileExistsLocally</c> about a path that was never written. A cache that silently
        /// never persists looks exactly like a cache that works.</para>
        /// </remarks>
        public void EnsureDirectories()
        {
            foreach (string dir in new[] { ResourceCacheDirectory, ResourceOverrideDirectory })
            {
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);
            }

            if (!string.IsNullOrWhiteSpace(ResourceCacheDirectory))
                EnsureCacheShards(ResourceCacheDirectory);
        }

        /// <summary>
        /// Lays down the 256 lowercase-hex shard directories Blue's cache names require.
        /// </summary>
        /// <remarks>
        /// Cheap enough to do unconditionally on every launch — 256 <c>CreateDirectory</c> calls
        /// against an existing tree are no-ops — and doing it unconditionally means a cache
        /// half-created by an interrupted first run repairs itself rather than staying broken for
        /// the shards it never reached.
        /// </remarks>
        internal static void EnsureCacheShards(string cacheDirectory)
        {
            const string Hex = "0123456789abcdef";
            foreach (char high in Hex)
            {
                foreach (char low in Hex)
                    Directory.CreateDirectory(Path.Combine(cacheDirectory, $"{high}{low}"));
            }
        }

        /// <summary>
        /// Locates a render runtime and fills in everything but the caches, which the caller
        /// supplies because they belong to EveLens's own cache tree.
        /// </summary>
        /// <param name="cacheDirectory">Blue's CDN cache directory.</param>
        /// <param name="overrideDirectory">Our converted-geometry tree.</param>
        /// <param name="indexFiles">
        /// The resfileindex files, normally from
        /// <see cref="EveResourceService.GetIndexFilesAsync"/>. Plural because CCP keeps the
        /// compiled shaders in a second index, and a renderer holding only the first one produces
        /// black frames without complaining. An empty list is accepted so the caller can report
        /// the specific problem via <see cref="Validate"/> rather than having discovery throw.
        /// </param>
        /// <returns>
        /// Options that may still be invalid — always <see cref="Validate"/> the result. There
        /// is no null return and no exception, because "we could not find the runtime" needs to
        /// reach the UI as a sentence, and the search order is worth reporting either way.
        /// </returns>
        public static SkinrSidecarOptions Discover(
            string cacheDirectory, string overrideDirectory, IEnumerable<string>? indexFiles) =>
            Discover(cacheDirectory, overrideDirectory, indexFiles, ResolveVariable);

        /// <summary>
        /// <see cref="Discover(string, string, IEnumerable{string})"/> with variable lookup injected.
        /// </summary>
        /// <param name="resolve">
        /// Supplies a configuration variable and the scope that answered.
        /// </param>
        /// <remarks>
        /// This seam exists because discovery consults the Windows registry, and a test cannot fake
        /// that by setting a process variable. Without it, a test asking "what happens when nothing
        /// is set?" would silently pick up the developer's own persisted
        /// <c>EVELENS_TRINITY_ROOT</c> and assert against whatever machine it ran on.
        /// </remarks>
        internal static SkinrSidecarOptions Discover(
            string cacheDirectory, string overrideDirectory, IEnumerable<string>? indexFiles,
            Func<string, (string? Value, string Scope)> resolve)
        {
            var options = new SkinrSidecarOptions
            {
                ResourceCacheDirectory = cacheDirectory,
                ResourceOverrideDirectory = overrideDirectory
            };
            foreach (string index in (indexFiles ?? Array.Empty<string>())
                     .Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                options.ResourceIndexPaths.Add(index);
            }

            (string? root, string rootScope) = resolve(RuntimeRootVariable);
            if (string.IsNullOrWhiteSpace(root))
                options.DiscoverySteps.Add($"{RuntimeRootVariable} not set");
            else if (options.Record($"{RuntimeRootVariable}={root} ({rootScope})",
                         ApplyShippedLayout(options, root!)))
                return options;

            (string? trinity, string trinityScope) = resolve(TrinityRootVariable);
            if (string.IsNullOrWhiteSpace(trinity))
                options.DiscoverySteps.Add($"{TrinityRootVariable} not set");
            else if (options.Record($"{TrinityRootVariable}={trinity} ({trinityScope})",
                         ApplySourceBuildLayout(options, trinity!)))
                return options;

            // Shipped default: a "skinr" folder beside the executable. Last rather than first
            // so a developer's environment variable always wins over a stale installed copy.
            string beside = Path.Combine(AppContext.BaseDirectory, "skinr");
            options.Record($"beside the executable ({beside})",
                ApplyShippedLayout(options, beside));
            return options;
        }

        /// <summary>
        /// Reads a configuration variable from the process block, then — on Windows — from the
        /// persisted user and machine scopes, and reports which scope answered.
        /// </summary>
        /// <returns>The value and the scope that supplied it; a null value when no scope has one.</returns>
        /// <remarks>
        /// <para><b>Why the registry scopes are consulted at all.</b> A process inherits its
        /// environment from whatever launched it, frozen at launch. <c>setx</c> and the System
        /// Properties dialog write the registry, and only <em>later</em> shells inherit that — so a
        /// variable can be correctly and permanently set while every already-running shell, and
        /// every app launched from one, still reports it missing. That produced a viewer saying
        /// "EVELENS_TRINITY_ROOT not set" on a machine where it demonstrably was, and the honest
        /// reading of "I set this permanently" is that it applies now, not after a reboot.</para>
        ///
        /// <para><b>Why Windows only.</b> <see cref="EnvironmentVariableTarget.User"/> and
        /// <see cref="EnvironmentVariableTarget.Machine"/> are registry concepts. On Unix there is
        /// no equivalent store to consult — the shell's own profile <em>is</em> the persistence
        /// mechanism, and it is already in the process block — so the extra reads would be noise
        /// at best. Guarding on the platform keeps the reported scope truthful rather than
        /// implying a lookup that did not happen.</para>
        ///
        /// <para>Process scope wins, so a developer can still override the persisted value for one
        /// launch by exporting it in the shell — which is the whole reason that ordering exists.</para>
        /// </remarks>
        private static (string? Value, string Scope) ResolveVariable(string name) =>
            ResolveVariable(name, System.Environment.GetEnvironmentVariable,
                OperatingSystem.IsWindows() ? ReadPersisted : null);

        /// <summary>
        /// <see cref="ResolveVariable(string)"/> with both stores injected, so the scope ordering
        /// can be tested without writing to a real registry — which a unit test has no business
        /// doing, and which would make the outcome depend on the machine running it.
        /// </summary>
        /// <param name="process">Reads the process block.</param>
        /// <param name="persisted">
        /// Reads a registry scope, or null on a platform that has no such store.
        /// </param>
        internal static (string? Value, string Scope) ResolveVariable(
            string name,
            Func<string, string?> process,
            Func<string, EnvironmentVariableTarget, string?>? persisted)
        {
            string? value = process(name);
            if (!string.IsNullOrWhiteSpace(value))
                return (value, "process");

            if (persisted == null)
                return (null, "process");

            foreach (EnvironmentVariableTarget target in
                     new[] { EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine })
            {
                value = persisted(name, target);
                if (!string.IsNullOrWhiteSpace(value))
                    return (value, target == EnvironmentVariableTarget.User ? "user" : "machine");
            }

            return (null, "process");
        }

        /// <summary>
        /// Reads one registry-backed scope, treating an unreadable one as absent.
        /// </summary>
        /// <remarks>
        /// A policy-blocked read is not the same answer as "not set", but at this point it has the
        /// same consequence — there is no value to use — so it degrades to the next scope rather
        /// than taking the viewer down with an exception from a diagnostic path.
        /// </remarks>
        private static string? ReadPersisted(string name, EnvironmentVariableTarget target)
        {
            try
            {
                return System.Environment.GetEnvironmentVariable(name, target);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Adds one line to <see cref="DiscoverySteps"/> and reports whether that attempt won.
        /// </summary>
        /// <param name="where">The root being tried, already described.</param>
        /// <param name="reason">Why it failed, or null if it succeeded.</param>
        private bool Record(string where, string? reason)
        {
            DiscoverySteps.Add(reason == null ? $"{where} — found" : $"{where} — {reason}");
            return reason == null;
        }

        /// <summary>
        /// The layout EveLens installs:
        /// <c>&lt;root&gt;/python/python.exe</c>, <c>&lt;root&gt;/engine</c>,
        /// <c>&lt;root&gt;/bin</c>, <c>&lt;root&gt;/renderer/skinr_sidecar.py</c>,
        /// <c>&lt;root&gt;/cacert.pem</c>.
        /// </summary>
        /// <returns>Null when this root is usable, otherwise why it is not.</returns>
        private static string? ApplyShippedLayout(SkinrSidecarOptions options, string root)
        {
            string python = Path.Combine(root, "python", "python.exe");
            string script = Path.Combine(root, "renderer", "skinr_sidecar.py");
            options.PythonPath = python;
            options.ScriptPath = script;
            options.ArtDirectory = Path.Combine(root, "engine");
            options.BinDirectory = Path.Combine(root, "bin");
            options.WorkingDirectory = Path.Combine(root, "renderer");

            // A bundle shipped with the runtime is honoured, but it is only a fallback: the
            // generated one is preferred because a pinned trust list expires and a generated one
            // inherits whatever the OS currently trusts, including a corporate inspection root.
            string cert = Path.Combine(root, SkinrCertificateBundle.FileName);
            if (File.Exists(cert))
                options.CertificatePath = cert;

            return WhatIsMissing(python, script);
        }

        /// <summary>
        /// The developer layout: a CMake build tree of Trinity plus the sidecar script from this
        /// repository. Mirrors the lab's own path derivation exactly, so what we test against is
        /// what this reproduces.
        /// </summary>
        /// <returns>Null when this root is usable, otherwise why it is not.</returns>
        private static string? ApplySourceBuildLayout(
            SkinrSidecarOptions options, string trinityRoot)
        {
            string build = Path.Combine(trinityRoot, ".cmake-build-x64-windows-release");
            string vcpkg = Path.Combine(build, "vcpkg_installed", "x64-windows-release");

            options.ArtDirectory = Path.Combine(build, "trinity", "Windows", "x64", "v141");
            options.BinDirectory = Path.Combine(vcpkg, "bin");
            options.PythonPath = Path.Combine(vcpkg, "tools", "python3", "python.exe");

            // The script is not in the engine tree; it is ours. Look beside the executable
            // first, then walk up to a repository checkout.
            string? script = FindRepositoryScript();
            options.ScriptPath = script ?? options.ScriptPath;
            options.WorkingDirectory = string.IsNullOrEmpty(options.ScriptPath)
                ? null
                : Path.GetDirectoryName(options.ScriptPath);

            // Reported separately from a missing file, because the path it would otherwise name is
            // not a path anyone searched — it is whatever the previously-tried layout left behind.
            if (script == null)
            {
                return "no tools/skinr-pipeline/renderer/skinr_sidecar.py above " +
                       AppContext.BaseDirectory;
            }

            return WhatIsMissing(options.PythonPath, script);
        }

        /// <summary>
        /// Names whichever of the two mandatory files is absent, or null when both are present.
        /// Both are named when both are missing: a half-answer sends someone to fix one path and
        /// come back for the other.
        /// </summary>
        private static string? WhatIsMissing(string python, string script)
        {
            var missing = new List<string>(2);
            if (!File.Exists(python))
                missing.Add($"no interpreter ({python})");
            if (!File.Exists(script))
                missing.Add($"no renderer script ({script})");
            return missing.Count == 0 ? null : string.Join(", ", missing);
        }

        /// <summary>
        /// Walks up from the running assembly looking for <c>tools/skinr-pipeline/renderer</c>.
        /// Only useful in a source checkout; returns null in an installed build, where the
        /// shipped layout applies instead.
        /// </summary>
        private static string? FindRepositoryScript()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int depth = 0; depth < 8 && dir != null; depth++, dir = dir.Parent)
            {
                string candidate = Path.Combine(
                    dir.FullName, "tools", "skinr-pipeline", "renderer", "skinr_sidecar.py");
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        private static string TrailingSlash(string path)
        {
            string forward = (path ?? string.Empty).Replace('\\', '/');
            return forward.EndsWith('/') ? forward : forward + "/";
        }

        private static string Describe(string path) =>
            string.IsNullOrWhiteSpace(path) ? "not configured" : path;
    }
}
