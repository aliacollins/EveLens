// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Answers "is that process still doing something" from the outside, using the operating
    /// system's own accounting rather than anything the process has to volunteer.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists.</b> The render sidecar cannot reliably report its own liveness,
    /// and the reason is structural rather than fixable. Blue's Python bindings hold the
    /// interpreter lock for the entire duration of a native call: while the engine is inside
    /// <c>blue.os.Pump()</c> feeding a 216 MB download through libcurl, or inside
    /// <c>sof.BuildFromDNA</c> compiling shader permutations, no Python code runs anywhere in the
    /// process — not on the main thread, and not on a heartbeat thread either, because there is
    /// no GIL to be had. Measured: a heartbeat thread asking to run every two seconds got a slot
    /// roughly every ten during a large download, and not once during a 47-second build.</para>
    ///
    /// <para>So a heartbeat is a nice progress indicator and a bad health check. Its absence
    /// means "the engine is busy" at least as often as it means "the engine is wedged", and those
    /// are the two cases the host most needs to tell apart. Every attempt to fix that from inside
    /// the sidecar ends up measuring the cooperation of the thing whose failure to cooperate is
    /// the thing being detected.</para>
    ///
    /// <para><b>What is measured instead.</b> Two counters the kernel maintains whether or not
    /// the process is willing to talk: cumulative CPU time across all its threads, and cumulative
    /// I/O bytes transferred. A process compiling shaders burns CPU; one downloading textures
    /// moves bytes and burns CPU decompressing and hashing them; one deadlocked on a lock or
    /// waiting on a socket that will never answer moves neither. That is exactly the distinction
    /// wanted, and it costs two syscalls.</para>
    ///
    /// <para><b>What it does not catch.</b> A spin-deadlock — a process burning CPU in a loop
    /// that will never finish — looks identical to honest work. It is not worth defending against
    /// here: the absolute per-operation ceiling in <see cref="SkinrSidecarProcess"/> and the
    /// caller's own cancellation both still apply, so the worst case is a slow failure rather
    /// than a hang, and no cheap external measurement can distinguish a hot loop from a hot
    /// compiler.</para>
    ///
    /// <para><b>Platform.</b> CPU time is portable. I/O counters are a Windows call, and the
    /// SKINR renderer is Windows-only for now (see the platform gate in the viewer). Where the
    /// call is unavailable the I/O term simply reads zero and the CPU term carries the decision,
    /// which degrades to a slightly less sensitive check rather than a broken one.</para>
    /// </remarks>
    internal sealed class SkinrProcessActivity
    {
        /// <summary>
        /// CPU time below which a sample counts as idle. Deliberately small: the question is
        /// "did this process run at all", not "did it run hard". Anything above scheduler noise
        /// on an otherwise-blocked process is evidence of work.
        /// </summary>
        private static readonly TimeSpan CpuFloor = TimeSpan.FromMilliseconds(40);

        /// <summary>
        /// I/O below which a sample counts as idle. A stalled transfer still ticks a few bytes
        /// of TCP bookkeeping; 64 KB is above that and far below any real resource fetch.
        /// </summary>
        private const ulong IoFloor = 64 * 1024;

        private readonly Process _process;
        private TimeSpan _cpu;
        private ulong _io;
        private bool _sampled;

        public SkinrProcessActivity(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            Sample(out _, out _);
        }

        /// <summary>
        /// Whether the process has done measurable work since the previous call, and how much.
        /// </summary>
        /// <param name="description">
        /// Human-readable delta, for the trace and for the eventual failure message. Written in
        /// units a person can reason about, because the one place this text is read is while
        /// someone is deciding whether a render is stuck.
        /// </param>
        /// <returns>
        /// <c>true</c> when either counter moved past its floor. Also <c>true</c> for the first
        /// call after construction failed to read the counters at all — an unreadable counter is
        /// not evidence of death, and treating it as such would fault healthy renders on any
        /// machine where the query is denied.
        /// </returns>
        public bool MadeProgress(out string description)
        {
            TimeSpan previousCpu = _cpu;
            ulong previousIo = _io;
            bool had = _sampled;

            if (!Sample(out TimeSpan cpu, out ulong io))
            {
                description = "process counters unavailable";
                return true;
            }

            if (!had)
            {
                description = "first sample";
                return true;
            }

            TimeSpan cpuDelta = cpu - previousCpu;
            ulong ioDelta = io >= previousIo ? io - previousIo : 0;

            description = string.Format(CultureInfo.InvariantCulture,
                "cpu +{0:0}ms, io +{1:n0} KB", cpuDelta.TotalMilliseconds, ioDelta / 1024);

            return cpuDelta >= CpuFloor || ioDelta >= IoFloor;
        }

        private bool Sample(out TimeSpan cpu, out ulong io)
        {
            cpu = _cpu;
            io = _io;
            try
            {
                if (_process.HasExited) return false;
                cpu = _process.TotalProcessorTime;
            }
            catch (Exception ex) when (ex is InvalidOperationException or
                                           System.ComponentModel.Win32Exception or
                                           NotSupportedException or PlatformNotSupportedException)
            {
                // An exited or inaccessible process. Either way the caller's other checks — a
                // closed stdout, a non-null exit code — are the right ones, not this.
                return false;
            }

            io = ReadIoBytes(_process);

            _cpu = cpu;
            _io = io;
            _sampled = true;
            return true;
        }

        private static ulong ReadIoBytes(Process process)
        {
            if (!OperatingSystem.IsWindows()) return 0;
            try
            {
                if (GetProcessIoCounters(process.Handle, out IoCounters counters))
                    return counters.ReadTransferCount + counters.WriteTransferCount +
                           counters.OtherTransferCount;
            }
            catch (Exception ex) when (ex is InvalidOperationException or
                                           System.ComponentModel.Win32Exception or
                                           NotSupportedException)
            {
                // Handle already closed. Zero, and let the CPU term decide.
            }
            return 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessIoCounters(IntPtr process, out IoCounters counters);
    }
}
