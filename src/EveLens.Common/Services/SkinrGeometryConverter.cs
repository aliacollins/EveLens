// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Serialization.Skinr;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Turns a hull's CDN <c>.gr2</c> geometry into a <c>.cmf</c> the renderer can load, and
    /// places it in the override tree where Blue will find it under its original
    /// <c>res:/</c> path.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this step exists at all.</b> CCP publishes ship geometry as Granny
    /// <c>.gr2</c>. Trinity cannot load it without the Granny runtime, which is proprietary and
    /// not shipped — but Trinity loads its own <c>.cmf</c> happily, and the open-source Carbon
    /// resource library can read one format and write the other. So the CDN's geometry becomes
    /// loadable geometry here, once per hull, cached forever after.</para>
    ///
    /// <para><b>Why out of process.</b> This is the only place in EveLens that parses an
    /// untrusted binary format, and a <c>.gr2</c> header is nothing but counts and offsets that
    /// a parser is invited to trust. So the converter runs as a separate process inside a
    /// <see cref="SkinrProcessJail"/> with a memory ceiling: a claimed 2-billion-vertex mesh
    /// fails an allocation inside a disposable process instead of taking EveLens with it. The
    /// converter also preflights the input before parsing, which catches the cheap cases without
    /// paying for a process at all.</para>
    ///
    /// <para><b>Why the result is placed by res-path rather than by digest.</b> Blue resolves
    /// <c>res:/dx9/model/ship/...</c> against a search path, so the file has to live at that
    /// relative path to be findable. The content-addressed cache still exists — the converter
    /// owns it — and the override tree holds a copy at the path the engine will ask for. Two
    /// copies of a few megabytes is the price of not teaching Blue a new resolution rule.</para>
    ///
    /// <para><b>Single-flight.</b> Two views asking for the same hull at once must not race to
    /// write the same file. The converter writes atomically so a race would not corrupt
    /// anything, but it would burn a second conversion, so identical requests share one task —
    /// the same pattern <see cref="EveResourceService"/> already uses for downloads.</para>
    /// </remarks>
    public sealed class SkinrGeometryConverter
    {
        /// <summary>Overrides Node discovery. Set when Node is not on PATH.</summary>
        public const string NodePathVariable = "EVELENS_SKINR_NODE";

        private static readonly JsonSerializerOptions s_json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly ConcurrentDictionary<string, Task<string?>> _inFlight = new(
            StringComparer.OrdinalIgnoreCase);

        private readonly string _scriptPath;
        private readonly string _cacheDirectory;
        private readonly string _overrideDirectory;
        private readonly string? _nodePath;

        /// <param name="scriptPath">Full path to <c>convert.mjs</c>.</param>
        /// <param name="cacheDirectory">The converter's content-addressed cache.</param>
        /// <param name="overrideDirectory">
        /// Root of the renderer's <c>res:</c> override tree. Converted geometry lands at
        /// <c>&lt;overrideDirectory&gt;/dx9/model/ship/...</c>.
        /// </param>
        /// <param name="nodePath">
        /// Node executable, or null to discover one. Node 24 LTS is what the converter is
        /// verified against.
        /// </param>
        public SkinrGeometryConverter(string scriptPath, string cacheDirectory,
            string overrideDirectory, string? nodePath = null)
        {
            _scriptPath = scriptPath ?? throw new ArgumentNullException(nameof(scriptPath));
            _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
            _overrideDirectory = overrideDirectory
                                 ?? throw new ArgumentNullException(nameof(overrideDirectory));
            _nodePath = nodePath ?? DiscoverNode();
        }

        /// <summary>Overrides converter-script discovery.</summary>
        public const string ScriptPathVariable = "EVELENS_SKINR_CONVERTER";

        /// <summary>
        /// Locates <c>convert.mjs</c> and builds a converter around the caches it should use.
        /// Never returns null: an unfound script surfaces as <see cref="IsAvailable"/> being
        /// false, so the reason reaches the UI as a sentence instead of a null check.
        /// </summary>
        /// <remarks>
        /// Search order mirrors <see cref="SkinrSidecarOptions.Discover"/> deliberately — an
        /// explicit variable, then the shipped layout, then a repository checkout — because a
        /// developer running from source and a user running an installed build must both work
        /// without either configuring anything, and the two halves of the pipeline must never
        /// disagree about which tree they came from.
        /// </remarks>
        public static SkinrGeometryConverter Discover(string cacheDirectory,
            string overrideDirectory)
        {
            string? explicitPath = Environment.GetEnvironmentVariable(ScriptPathVariable);
            string script = !string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath)
                ? explicitPath!
                : FindScript() ?? string.Empty;

            return new SkinrGeometryConverter(script, cacheDirectory, overrideDirectory);
        }

        private static string? FindScript()
        {
            string beside = Path.Combine(
                AppContext.BaseDirectory, "skinr", "gr2-convert", "convert.mjs");
            if (File.Exists(beside))
                return beside;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int depth = 0; depth < 8 && dir != null; depth++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "tools", "skinr-pipeline",
                    "gr2-convert", "convert.mjs");
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        /// <summary>
        /// Memory ceiling for the converter process. Sized for the heaviest legitimate
        /// input: station hangar interiors (the Jita bay is a 15.8 MB gr2 with 2.4M
        /// vertices, and Node's V8 heap peaks well past 1.5 GB compressing its 54 MB
        /// output — the old cap aborted it with exit 134). The jail exists to stop
        /// runaways, not CCP's own MD5-verified content.
        /// </summary>
        public long MemoryLimitBytes { get; set; } = 4096L * 1024 * 1024;

        /// <summary>CPU share for the converter process, or 0 for unlimited.</summary>
        public int CpuPercent { get; set; } = 50;

        /// <summary>
        /// How long one conversion may take. A real hull converts in under a second; this is
        /// the ceiling for a pathological input that slipped past preflight.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(90);

        /// <summary>The Node executable in use, or null when none was found.</summary>
        public string? NodePath => _nodePath;

        /// <summary>Whether conversion is possible at all on this machine.</summary>
        public bool IsAvailable => _nodePath != null && File.Exists(_scriptPath);

        /// <summary>
        /// Fetches a hull's <c>.gr2</c> and converts it, returning the <c>res:/</c> path of the
        /// resulting <c>.cmf</c> — which is what <c>build</c> takes as
        /// <c>geometryResPath</c>. Returns null when the geometry could not be produced; the
        /// reason is traced, and the caller shows "geometry unavailable" rather than an empty
        /// render pane.
        /// </summary>
        /// <param name="gr2ResPath">
        /// The <c>res:/</c> path the sidecar's <c>resolve</c> op reported as
        /// <c>geometryResFilePath</c>.
        /// </param>
        /// <param name="progress">Download progress for the <c>.gr2</c> fetch, 0.0-1.0.</param>
        public Task<string?> ConvertAsync(string gr2ResPath, IProgress<double>? progress = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(gr2ResPath))
                return Task.FromResult<string?>(null);

            string key = gr2ResPath.Trim().ToLowerInvariant();

            // GetOrAdd's factory can run more than once under contention, so the task it
            // creates must be cheap to make and idempotent to run — it is, since the converter
            // is content-addressed and writes atomically.
            Task<string?> task = _inFlight.GetOrAdd(key, k => ConvertCoreAsync(k, progress, ct));
            _ = task.ContinueWith(_ => _inFlight.TryRemove(key, out _), TaskScheduler.Default);
            return task;
        }

        private async Task<string?> ConvertCoreAsync(string gr2ResPath,
            IProgress<double>? progress, CancellationToken ct)
        {
            try
            {
                if (!IsAvailable)
                {
                    AppServices.TraceService?.Trace(_nodePath == null
                        ? "Skinr: no Node runtime found — geometry conversion unavailable"
                        : $"Skinr: converter script missing at {_scriptPath}");
                    return null;
                }

                string cmfResPath = Path.ChangeExtension(gr2ResPath, ".cmf")!;
                string target = ResPathToLocal(cmfResPath);

                string? gr2Local = await EveResourceService
                    .GetResourceAsync(gr2ResPath, progress, ct).ConfigureAwait(false);
                if (gr2Local == null)
                {
                    AppServices.TraceService?.Trace(
                        $"Skinr: {gr2ResPath} is not in CCP's resource index");
                    return null;
                }

                SkinrConverterResult? result = await RunConverterAsync(
                    gr2Local, target, gr2ResPath, ct).ConfigureAwait(false);

                if (result?.Ok != true)
                {
                    AppServices.TraceService?.Trace(
                        $"Skinr: geometry conversion failed for {gr2ResPath}: " +
                        (result?.Error ?? "converter produced no result"));
                    return null;
                }

                AppServices.TraceService?.Trace(
                    $"Skinr: geometry {(result.CacheHit ? "cached" : "converted")} " +
                    $"{gr2ResPath} -> {cmfResPath} ({result.Bytes:N0} bytes, " +
                    $"{result.DurationMs:0}ms, {result.Stats?.Meshes ?? 0} meshes)");

                // Trust the converter's own report over File.Exists: it writes atomically and
                // verifies the digest, so if it says ok the bytes are there and correct.
                return cmfResPath;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: geometry conversion threw for {gr2ResPath}: {ex.Message}");
                return null;
            }
        }

        private async Task<SkinrConverterResult?> RunConverterAsync(string inputPath,
            string outputPath, string resPath, CancellationToken ct)
        {
            Directory.CreateDirectory(_cacheDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var psi = new ProcessStartInfo
            {
                FileName = _nodePath!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
                WorkingDirectory = Path.GetDirectoryName(_scriptPath) ?? "."
            };
            // V8 aborts (exit 134) when its own heap default is tighter than the work,
            // regardless of the job-object ceiling; state the budget explicitly.
            psi.Environment["NODE_OPTIONS"] = "--max-old-space-size=3072";
            foreach (string arg in new[]
                     {
                         _scriptPath,
                         "--in", inputPath,
                         "--out", outputPath,
                         "--cache-dir", _cacheDirectory,
                         "--res-path", resPath
                     })
            {
                psi.ArgumentList.Add(arg);
            }

            using SkinrProcessJail? jail = SkinrProcessJail.TryCreate(MemoryLimitBytes, CpuPercent);
            using var process = Process.Start(psi);
            if (process == null)
            {
                AppServices.TraceService?.Trace("Skinr: could not start the geometry converter");
                return null;
            }
            jail?.TryAssign(process);

            // Both pipes must be drained concurrently with the wait. The converter's stderr is
            // JSON-lines diagnostics and a full pipe would deadlock a process that is otherwise
            // working perfectly.
            Task<string> stdout = process.StandardOutput.ReadToEndAsync(ct);
            Task<string> stderr = process.StandardError.ReadToEndAsync(ct);

            using var timer = new CancellationTokenSource(Timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timer.Token);
            try
            {
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timer.IsCancellationRequested)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: geometry conversion of {resPath} exceeded " +
                    $"{Timeout.TotalSeconds:0}s — killed");
                TryKill(process);
                return null;
            }

            string diagnostics = await stderr.ConfigureAwait(false);
            ForwardDiagnostics(diagnostics);

            string output = await stdout.ConfigureAwait(false);
            string? line = LastJsonLine(output);
            if (line == null)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: converter exited {process.ExitCode} with no result line");
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SkinrConverterResult>(line, s_json);
            }
            catch (JsonException ex)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: converter result was not valid JSON: {ex.Message}");
                return null;
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception) { /* already gone, or gone by the time we asked */ }
        }

        /// <summary>
        /// Mirrors the converter's stderr into the EveLens diagnostic stream. Its records
        /// already carry the same <c>lvl</c>/<c>msg</c> shape, so they are passed through nearly
        /// verbatim rather than re-parsed into a DTO nothing else would use.
        /// </summary>
        private static void ForwardDiagnostics(string stderr)
        {
            if (string.IsNullOrWhiteSpace(stderr))
                return;
            foreach (string line in stderr.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0)
                    AppServices.TraceService?.Trace("Skinr: gr2 " + trimmed);
            }
        }

        /// <summary>
        /// The converter contracts to write exactly one JSON object to stdout, but taking the
        /// last non-empty line rather than the whole buffer means a stray <c>console.log</c>
        /// from a dependency degrades to a warning instead of a parse failure.
        /// </summary>
        private static string? LastJsonLine(string stdout)
        {
            if (string.IsNullOrWhiteSpace(stdout))
                return null;
            string[] lines = stdout.Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (line.StartsWith('{') && line.EndsWith('}'))
                    return line;
            }
            return null;
        }

        /// <summary>
        /// Maps <c>res:/a/b/c.cmf</c> to its place in the override tree. Rejects anything that
        /// climbs out of it.
        /// </summary>
        /// <remarks>
        /// The res-path comes from a resource index we download, so it is not attacker-supplied
        /// in any ordinary sense — but it is the string that decides where we write a file, and
        /// a path-traversal check on a value that "cannot" contain <c>..</c> costs one
        /// comparison. Root cause, not perimeter: this is the only function that turns a res-path
        /// into a filesystem path, so the check belongs here and nowhere else.
        /// </remarks>
        internal string ResPathToLocal(string resPath)
        {
            string relative = resPath.StartsWith("res:/", StringComparison.OrdinalIgnoreCase)
                ? resPath.Substring("res:/".Length)
                : resPath;
            relative = relative.Replace('/', Path.DirectorySeparatorChar).TrimStart(
                Path.DirectorySeparatorChar);

            string root = Path.GetFullPath(_overrideDirectory);
            string full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"resource path escapes the geometry cache: {resPath}");
            return full;
        }

        /// <summary>
        /// Finds a Node runtime: the override variable, then a bundled copy beside the render
        /// runtime, then PATH.
        /// </summary>
        /// <remarks>
        /// PATH is checked last on purpose. A developer machine has whatever Node happened to be
        /// installed, and the converter's dependency set is pinned and verified against Node 24
        /// — so a bundled runtime, when we ship one, must win over a system one that might be
        /// Node 18.
        /// </remarks>
        private static string? DiscoverNode()
        {
            string? explicitPath = Environment.GetEnvironmentVariable(NodePathVariable);
            if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
                return explicitPath;

            string exe = OperatingSystem.IsWindows() ? "node.exe" : "node";

            string bundled = Path.Combine(AppContext.BaseDirectory, "skinr", "node", exe);
            if (File.Exists(bundled))
                return bundled;

            string? pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar))
                return null;

            foreach (string dir in pathVar.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;
                try
                {
                    string candidate = Path.Combine(dir.Trim(), exe);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                    // A malformed PATH entry is not our problem to fix, only to survive.
                }
            }
            return null;
        }
    }
}
