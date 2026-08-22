// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
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
    /// Owns one render-sidecar process and the JSON-lines conversation with it. Transport only:
    /// launch, one request at a time, per-op timeouts, teardown. What to ask for and what the
    /// answers mean belongs to <see cref="SkinrSidecarHost"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately not a general-purpose RPC client. Three properties of the other end shape
    /// this:
    ///
    /// <list type="number">
    /// <item>The sidecar is single-threaded and holds a GPU device, so ops are strictly
    /// serialized. There is no id-keyed pending-request table because there is never more than
    /// one request in flight — a table would imply concurrency the engine does not have.</item>
    /// <item>stderr must be drained continuously. It carries structured diagnostics, and a
    /// full pipe buffer blocks the writer — the sidecar would wedge mid-render for reasons
    /// that look nothing like a logging problem. So stderr gets its own task for the process
    /// lifetime while stdout is read inline.</item>
    /// <item>A timeout means the engine is in an unknown state, not that this one call was
    /// slow. Trinity can wedge inside a driver call; there is no "cancel the current op"
    /// message that would be answered. So a timeout faults the whole process and the host
    /// restarts it — see <see cref="IsFaulted"/>.</item>
    /// </list>
    ///
    /// Unsolicited lines (the <c>ready</c> event, build progress) are skipped rather than
    /// matched, exactly as the protocol specifies: they have no id and must never satisfy a
    /// pending request.
    /// </remarks>
    public sealed class SkinrSidecarProcess : IDisposable
    {
        /// <summary>The wire version this build speaks. Refuses anything else.</summary>
        public const int ExpectedProtocol = 5;

        private static readonly JsonSerializerOptions s_json = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition
                .WhenWritingNull
        };

        /// <summary>
        /// How many recent stderr lines to keep for inclusion in a failure message.
        /// </summary>
        /// <remarks>
        /// Small on purpose. This is not a log — the trace sink already has every line. It is
        /// the tail that gets attached to an exception, and an exception message long enough to
        /// scroll is one nobody reads. Eight lines is comfortably enough for a Python traceback's
        /// informative end, which is where the cause always is.
        /// </remarks>
        private const int DiagnosticTailLines = 8;

        /// <summary>
        /// Ceiling on a quiet period the sidecar declares for itself. See
        /// <see cref="ReadUntilAsync"/>.
        /// </summary>
        /// <remarks>
        /// The declaration exists because only the sidecar knows whether it is compiling shaders
        /// on a GPU or on WARP; the ceiling exists because the sidecar is also the process whose
        /// health is in question, and a hung one asking for an hour of silence must not get it.
        /// </remarks>
        private static readonly TimeSpan MaxDeclaredQuiet = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Absolute ceiling on one operation, however busy the process looks.
        /// </summary>
        /// <remarks>
        /// The backstop behind <see cref="SkinrProcessActivity"/>, which cannot distinguish a
        /// shader compiler from an infinite loop — both burn CPU. Fifteen minutes is far past any
        /// measured operation (the worst was a four-minute cold build downloading a quarter of a
        /// gigabyte) and far short of a user's patience, so it only ever fires on something that
        /// was never going to finish.
        /// </remarks>
        private static readonly TimeSpan MaxOperation = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Consecutive idle windows tolerated before the process is declared dead.
        /// </summary>
        /// <remarks>
        /// One idle window is not a wedge. Blue's resource fetcher backs off between retries when
        /// the CDN refuses or drops a connection, and a sleeping process consumes neither CPU nor
        /// I/O — indistinguishable, in a single sample, from one that will never wake. Requiring
        /// three consecutive windows costs a genuinely dead renderer a couple of extra minutes
        /// before restart and saves a healthy one from being killed mid-retry, which is the
        /// trade-off worth making: the first is an unusual failure, the second would be a
        /// routine one.
        /// </remarks>
        private const int IdleStrikes = 3;

        private readonly SkinrSidecarOptions _options;
        private readonly Action<string> _trace;
        private readonly SemaphoreSlim _turn = new(1, 1);
        private readonly Queue<string> _recentDiagnostics = new(DiagnosticTailLines);

        private Process? _process;
        private SkinrProcessJail? _jail;
        private StreamWriter? _stdin;
        private StreamReader? _stdout;
        private CancellationTokenSource? _lifetime;
        private long _nextId;
        private bool _disposed;

        public SkinrSidecarProcess(SkinrSidecarOptions options, Action<string>? trace = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _trace = trace ?? (_ => { });
        }

        /// <summary>Sidecar version string from the <c>ready</c> event, once started.</summary>
        public string SidecarVersion { get; private set; } = string.Empty;

        /// <summary>Which Trinity device the sidecar actually got — HARDWARE, SOFTWARE, WARP.</summary>
        public string Device { get; private set; } = string.Empty;

        /// <summary>Whether the jail applied, and with which limits. Empty when unjailed.</summary>
        public string JailLimits { get; private set; } = string.Empty;

        /// <summary>
        /// True once a timeout, a protocol violation or a process exit has made this instance
        /// unusable. The host discards a faulted process rather than retrying against it,
        /// because the engine's state is no longer known.
        /// </summary>
        public bool IsFaulted { get; private set; }

        public bool IsRunning => _process is { HasExited: false } && !IsFaulted;

        /// <summary>
        /// Launches the sidecar and waits for its <c>ready</c> event.
        /// </summary>
        /// <exception cref="SkinrSidecarException">
        /// The executable or script is missing, the process died during boot, boot exceeded
        /// <see cref="SkinrSidecarOptions.StartupTimeout"/>, or the protocol version does not
        /// match.
        /// </exception>
        public async Task StartAsync(CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_process != null)
                throw new InvalidOperationException("sidecar already started");

            if (!File.Exists(_options.PythonPath))
                throw new SkinrSidecarException(
                    $"render host not found at {_options.PythonPath}");
            if (!File.Exists(_options.ScriptPath))
                throw new SkinrSidecarException(
                    $"render script not found at {_options.ScriptPath}");

            var psi = new ProcessStartInfo
            {
                FileName = _options.PythonPath,
                WorkingDirectory = _options.WorkingDirectory ??
                                   Path.GetDirectoryName(_options.ScriptPath) ?? ".",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false)
            };
            psi.ArgumentList.Add(_options.ScriptPath);
            foreach (string arg in _options.BuildArguments())
                psi.ArgumentList.Add(arg);

            // The engine writes non-ASCII resource paths; without this Python picks the
            // console codepage and dies formatting its own diagnostics on cp1252.
            psi.Environment["PYTHONIOENCODING"] = "utf-8";
            psi.Environment["PYTHONUNBUFFERED"] = "1";
            foreach (KeyValuePair<string, string> kv in _options.Environment)
                psi.Environment[kv.Key] = kv.Value;

            // Jail before start would be ideal (a suspended-launch + assign + resume dance),
            // but the window between start and assign is microseconds of a process that has
            // not yet loaded the engine, and CREATE_SUSPENDED is not reachable through
            // ProcessStartInfo. Assigning immediately after start is the practical trade.
            _jail = SkinrProcessJail.TryCreate(_options.MemoryLimitBytes, _options.CpuPercent);

            _process = Process.Start(psi)
                       ?? throw new SkinrSidecarException("could not start the render host");
            _lifetime = new CancellationTokenSource();
            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;

            if (_jail != null)
            {
                JailLimits = _jail.TryAssign(_process)
                    ? _jail.AppliedLimits
                    : "assignment refused — running unconstrained";
            }
            else
            {
                JailLimits = OperatingSystem.IsWindows()
                    ? "unavailable — running unconstrained"
                    : "not applicable on this platform";
            }
            _trace($"Skinr: sidecar pid {_process.Id}, jail: {JailLimits}");

            _ = Task.Run(() => DrainStderrAsync(_lifetime.Token));

            SkinrSidecarResponse ready = await ReadUntilAsync(
                r => r.Event == "ready", _options.StartupTimeout, "startup", ct)
                .ConfigureAwait(false);

            if (ready.Protocol != ExpectedProtocol)
            {
                Fault();
                throw new SkinrSidecarException(
                    $"render host speaks protocol {ready.Protocol}, this build needs " +
                    $"{ExpectedProtocol}. The sidecar and EveLens are out of step.");
            }

            SidecarVersion = ready.Sidecar ?? "unknown";
            Device = ready.Device ?? "unknown";
            _trace($"Skinr: sidecar {SidecarVersion} ready, protocol {ready.Protocol}, " +
                   $"device {Device}");
        }

        /// <summary>
        /// Sends one request and returns its response. Serialized: concurrent callers queue.
        /// </summary>
        /// <param name="request">
        /// <see cref="SkinrSidecarRequest.Id"/> is assigned here and overwrites whatever the
        /// caller set, so correlation cannot be got wrong from outside.
        /// </param>
        /// <param name="timeout">
        /// Per-op, because the ops differ by two orders of magnitude: a camera move is
        /// milliseconds, a first build compiles shaders. Exceeding it faults the process.
        /// </param>
        /// <exception cref="SkinrSidecarException">
        /// The sidecar is not running, has faulted, timed out, or returned <c>ok: false</c>.
        /// </exception>
        public async Task<SkinrSidecarResponse> CallAsync(SkinrSidecarRequest request,
            TimeSpan timeout, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ObjectDisposedException.ThrowIf(_disposed, this);

            await _turn.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!IsRunning)
                    throw new SkinrSidecarException(IsFaulted
                        ? "render host has faulted and needs restarting"
                        : "render host is not running");

                request.Id = Interlocked.Increment(ref _nextId);
                string line = JsonSerializer.Serialize(request, s_json);

                try
                {
                    await _stdin!.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
                    await _stdin.FlushAsync(ct).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    Fault();
                    throw new SkinrSidecarException(
                        $"render host closed its input during {request.Op}", ex);
                }

                SkinrSidecarResponse response = await ReadUntilAsync(
                    r => r.Id == request.Id, timeout, request.Op, ct).ConfigureAwait(false);

                if (response.Ok == false)
                    throw new SkinrSidecarException(
                        $"{request.Op} failed: {response.Error ?? "no reason given"}");

                return response;
            }
            finally
            {
                _turn.Release();
            }
        }

        /// <summary>
        /// Reads response lines until <paramref name="match"/> accepts one, skipping
        /// unsolicited events. Faults on silence or a closed pipe.
        /// </summary>
        /// <remarks>
        /// <para><b><paramref name="timeout"/> is an inactivity budget, not a duration.</b> It
        /// restarts on every line the sidecar sends, and the sidecar heartbeats a
        /// <c>working</c> event roughly every two seconds while it is pumping the engine. The
        /// distinction matters because the expensive operations here are network-bound on a
        /// payload we do not control the size of: building a hull on a cold cache downloads every
        /// texture it wears straight from CCP's CDN, which on a first run is hundreds of
        /// megabytes and legitimately takes many minutes. Any fixed deadline is therefore either
        /// too short to survive a cold cache or too long to notice a genuinely wedged engine —
        /// a 240-second one killed a perfectly healthy first build, which is what prompted
        /// this.</para>
        ///
        /// <para>Timing out on silence asks the right question: not "is this taking a while", but
        /// "has the engine stopped talking". A sidecar that is downloading says so; one that has
        /// deadlocked inside a native call says nothing, and that is the case worth restarting
        /// for.</para>
        ///
        /// <para><b>The exception, declared rather than guessed at.</b> Some engine calls cannot
        /// heartbeat, because Blue's Python bindings hold the interpreter lock for the whole
        /// duration of the native call — during <c>BuildFromDNA</c>'s shader compilation no Python
        /// code runs anywhere in the process, on any thread, so no liveness signal is possible.
        /// Rather than slacken every deadline to cover the worst of those, the sidecar announces
        /// them: a <c>quietMs</c> on an event means "the next silence may legitimately last this
        /// long". It applies to exactly one read and then lapses, so a wedged engine still trips
        /// the normal budget the moment its declared quiet period is over — and the sidecar, which
        /// is the only party that knows whether it is on a GPU or on WARP, sets the number.</para>
        /// </remarks>
        private async Task<SkinrSidecarResponse> ReadUntilAsync(
            Func<SkinrSidecarResponse, bool> match, TimeSpan timeout, string what,
            CancellationToken ct)
        {
            var started = Stopwatch.StartNew();
            TimeSpan budget = timeout;
            SkinrProcessActivity? activity =
                _process is { HasExited: false } p ? new SkinrProcessActivity(p) : null;
            int idleStrikes = 0;

            while (true)
            {
                // Re-armed per line. Cheap: one timer object per message, against an operation
                // that is doing network and GPU work between messages.
                using var timer = new CancellationTokenSource(budget);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    ct, timer.Token, _lifetime?.Token ?? CancellationToken.None);

                string? line;
                try
                {
                    line = await _stdout!.ReadLineAsync(linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timer.IsCancellationRequested)
                {
                    // Silence is not evidence. Ask the operating system whether the process is
                    // working before declaring it dead — see SkinrProcessActivity for why the
                    // sidecar cannot answer that question about itself.
                    if (activity != null && started.Elapsed < MaxOperation)
                    {
                        bool working = activity.MadeProgress(out string delta);
                        idleStrikes = working ? 0 : idleStrikes + 1;

                        if (working || idleStrikes < IdleStrikes)
                        {
                            _trace($"Skinr: {what} quiet for {budget.TotalSeconds:0.#}s at " +
                                   $"{started.Elapsed.TotalSeconds:0}s — {delta}" +
                                   (working ? ", still working" :
                                       $", idle strike {idleStrikes}/{IdleStrikes}"));
                            continue;
                        }
                    }

                    Fault();
                    string why = activity == null
                        ? "it has been restarted"
                        : started.Elapsed >= MaxOperation
                            ? $"and {what} has now run for {started.Elapsed.TotalMinutes:0} " +
                              "minutes, past the point where waiting longer is useful; it has " +
                              "been restarted"
                            : "and the process is doing no work at all — no CPU, no I/O; it has " +
                              "been restarted";
                    throw new SkinrSidecarException(
                        $"render host went silent during {what} — nothing for " +
                        $"{budget.TotalSeconds:0.#}s after {started.Elapsed.TotalSeconds:0}s of " +
                        "work, " + why + DescribeRecentDiagnostics());
                }

                // Any line at all ends the declared quiet period, whether or not it is the one
                // being waited for. Extensions are single-shot by construction.
                budget = timeout;

                if (line == null)
                {
                    Fault();
                    throw new SkinrSidecarException(
                        $"render host exited during {what} (exit code " +
                        $"{TryExitCode()?.ToString() ?? "unknown"})" + DescribeRecentDiagnostics());
                }

                if (line.Length == 0)
                    continue;

                SkinrSidecarResponse? response;
                try
                {
                    response = JsonSerializer.Deserialize<SkinrSidecarResponse>(line, s_json);
                }
                catch (JsonException ex)
                {
                    // Two very different things arrive here and they were once logged the same
                    // way, at a cost of several wasted runs. One is a Python traceback that
                    // escaped the sidecar's own handler — genuinely not JSON, and safely skipped.
                    // The other is a perfectly well-formed response this build cannot map onto
                    // SkinrSidecarResponse: a field whose type drifted between the two sides. That
                    // second case is a protocol bug, and skipping it silently turns a completed
                    // operation into a hang, because the reply the caller is waiting for has
                    // already gone past. So the reason gets logged, loudly, and the line is not
                    // silently discarded when it looks like it was meant for us.
                    bool looksLikeAResponse = line.Contains("\"id\"", StringComparison.Ordinal);
                    _trace((looksLikeAResponse
                               ? "Skinr: PROTOCOL MISMATCH — the render host sent a response this " +
                                 "build cannot read: "
                               : "Skinr: unparseable line from render host: ") +
                           ex.Message + " || " + Truncate(line, 1200));

                    if (looksLikeAResponse)
                    {
                        Fault();
                        throw new SkinrSidecarException(
                            $"{what} completed but its reply could not be read — the render host " +
                            "and EveLens disagree about the shape of the response (" + ex.Message +
                            ")");
                    }
                    continue;
                }

                if (response == null)
                    continue;

                if (response.Event != null)
                {
                    ProgressReported?.Invoke(response);

                    // Clamped, because a declared budget is the sidecar's opinion and the sidecar
                    // is the process we are trying to keep honest. Ten minutes is longer than any
                    // legitimate quiet stretch measured on WARP and still short enough that a
                    // hung renderer is recovered inside one coffee.
                    long quiet = response.QuietBudgetMilliseconds ?? 0;
                    if (quiet > 0)
                    {
                        budget = TimeSpan.FromMilliseconds(
                            Math.Min(quiet, MaxDeclaredQuiet.TotalMilliseconds));
                        if (budget < timeout) budget = timeout;
                    }

                    // `fatal` is the one event that is not progress. The sidecar emits it and
                    // exits, so treating it as an unmatched line means the *reason* is dropped
                    // and the caller gets "exited during startup" — technically true and
                    // practically useless. Failing here is what turns "it didn't work" into
                    // "Trinity could not create a device", which is the difference between a
                    // bug report we can act on and one we cannot.
                    if (response.Event == "fatal")
                    {
                        Fault();
                        string phase = string.IsNullOrEmpty(response.Phase)
                            ? what : response.Phase!;
                        throw new SkinrSidecarException(
                            $"render host failed during {phase}: " +
                            (response.Error ?? "no reason given") +
                            DescribeRecentDiagnostics());
                    }

                    if (!match(response))
                        continue;
                }

                if (match(response))
                    return response;
            }
        }

        /// <summary>
        /// Raised for unsolicited event lines — <c>ready</c> at startup and build progress.
        /// Fires on the reader's thread, so handlers must marshal to the UI themselves.
        /// </summary>
        public event Action<SkinrSidecarResponse>? ProgressReported;

        private async Task DrainStderrAsync(CancellationToken ct)
        {
            try
            {
                StreamReader stderr = _process!.StandardError;
                while (!ct.IsCancellationRequested)
                {
                    string? line = await stderr.ReadLineAsync(ct).ConfigureAwait(false);
                    if (line == null)
                        break;
                    if (line.Length == 0)
                        continue;
                    Remember(line);
                    _trace("Skinr: " + Truncate(line, 1000));
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down.
            }
            catch (Exception ex)
            {
                // Losing the diagnostic stream must not take down the renderer, but silence
                // here would make a later wedge unexplainable.
                _trace("Skinr: diagnostic stream ended: " + ex.Message);
            }
        }

        /// <summary>
        /// Keeps the newest stderr line, dropping the oldest past
        /// <see cref="DiagnosticTailLines"/>. Locked because the drain task and whichever thread
        /// is building a failure message are different threads.
        /// </summary>
        private void Remember(string line)
        {
            lock (_recentDiagnostics)
            {
                _recentDiagnostics.Enqueue(Truncate(line, 300));
                while (_recentDiagnostics.Count > DiagnosticTailLines)
                    _recentDiagnostics.Dequeue();
            }
        }

        /// <summary>
        /// The sidecar's last words, formatted for appending to an exception message. Empty when
        /// it said nothing — in which case the bare exit code is genuinely all there is to say.
        /// </summary>
        /// <remarks>
        /// There is a small race worth naming: stderr is drained on its own task, so on a very
        /// fast death the last line or two may not have arrived by the time stdout closes. Not
        /// worth synchronising for — waiting on the drain task would trade a sometimes-incomplete
        /// message for a sometimes-hanging one, and the trace sink has the complete record either
        /// way. This is a best-effort courtesy attached to the error the user actually sees.
        /// </remarks>
        private string DescribeRecentDiagnostics()
        {
            string[] lines;
            lock (_recentDiagnostics)
            {
                lines = _recentDiagnostics.ToArray();
            }

            return lines.Length == 0
                ? string.Empty
                : Environment.NewLine + "Last diagnostics:" + Environment.NewLine +
                  string.Join(Environment.NewLine, lines);
        }

        private void Fault() => IsFaulted = true;

        private int? TryExitCode()
        {
            try
            {
                return _process is { HasExited: true } p ? p.ExitCode : null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max) + "…";

        /// <summary>
        /// Asks the sidecar to shut down cleanly, then tears it down regardless.
        /// </summary>
        /// <remarks>
        /// The graceful path matters: the sidecar releases its Trinity device and GPU
        /// allocations on <c>shutdown</c>, and a killed process leaves the driver to clean up,
        /// which on some drivers is where a device-removed error for the *next* launch comes
        /// from. But it is best-effort with a short leash — closing the jail handle is what
        /// actually guarantees the process is gone.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try
            {
                if (_process is { HasExited: false } && !IsFaulted && _stdin != null)
                {
                    _stdin.WriteLine("{\"id\":0,\"op\":\"shutdown\"}");
                    _stdin.Flush();
                    _process.WaitForExit((int)_options.ShutdownTimeout.TotalMilliseconds);
                }
            }
            catch (Exception)
            {
                // Nothing useful to do: the teardown below is unconditional.
            }

            try
            {
                _lifetime?.Cancel();
            }
            catch (Exception) { }

            try
            {
                if (_process is { HasExited: false })
                    _process.Kill(entireProcessTree: true);
            }
            catch (Exception) { }

            _lifetime?.Dispose();
            _jail?.Dispose();          // closing the job kills anything still inside it
            _process?.Dispose();
            _turn.Dispose();
        }
    }

    /// <summary>Anything that went wrong talking to the render sidecar.</summary>
    public sealed class SkinrSidecarException : Exception
    {
        public SkinrSidecarException(string message) : base(message) { }
        public SkinrSidecarException(string message, Exception inner) : base(message, inner) { }
    }
}
