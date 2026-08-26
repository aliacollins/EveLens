// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace EveLens.Common.Services
{
    /// <summary>
    /// A Windows Job Object that bounds the render sidecar: memory ceiling, CPU share, no
    /// child processes, and — the part that matters most — guaranteed teardown when EveLens
    /// exits for any reason.
    /// </summary>
    /// <remarks>
    /// The sidecar is a Python process hosting CCP's Trinity engine and a GPU device. Three
    /// things about that combination make a jail non-optional rather than defensive:
    ///
    /// <list type="bullet">
    /// <item>It allocates from resource files whose sizes we do not control. A malformed or
    /// hostile <c>.cmf</c> can ask the engine for an arbitrary buffer, and a renderer that
    /// takes the machine into swap is indistinguishable to the user from EveLens hanging.</item>
    /// <item>It can wedge inside native code. <see cref="Process.Kill()"/> handles a wedged
    /// managed process; it does not help when the wedge is a driver call, and a
    /// <c>TerminateProcess</c> that the kernel defers still leaves the memory charged. The
    /// job's memory limit fails the allocation instead, which the sidecar reports as an
    /// error we can show.</item>
    /// <item>EveLens can die without running finalizers — a crash, Task Manager, a debugger
    /// detach. Without <c>KILL_ON_JOB_CLOSE</c> that leaves a multi-hundred-megabyte process
    /// holding a GPU device with no parent and no console, which the user has no way to
    /// connect to what they were doing.</item>
    /// </list>
    ///
    /// <c>KILL_ON_JOB_CLOSE</c> is what makes the last one safe: the job handle is the only
    /// thing keeping the sidecar alive, and the OS closes handles even when nothing else runs.
    ///
    /// Windows-only by construction, and that is honest rather than a gap — Trinity ships
    /// DirectX and Metal backends, and <see cref="SkinrRenderPlatform"/> already gates the
    /// feature to Windows x64. <see cref="TryCreate"/> returns null everywhere else and on any
    /// failure at all; the host then runs the sidecar unjailed rather than refusing to render,
    /// because a working renderer with weaker containment beats a dead feature. The one thing
    /// it must never do is silently believe it applied limits it did not — hence
    /// <see cref="AppliedLimits"/>, which the host logs.
    /// </remarks>
    public sealed class SkinrProcessJail : IDisposable
    {
        private IntPtr _job;
        private bool _disposed;

        private SkinrProcessJail(IntPtr job, string appliedLimits)
        {
            _job = job;
            AppliedLimits = appliedLimits;
        }

        /// <summary>
        /// Human-readable summary of the limits that actually took effect, for the diagnostic
        /// log. Never assume; a limit that failed to apply is worth seeing.
        /// </summary>
        public string AppliedLimits { get; }

        /// <summary>
        /// Creates a job with the given ceilings, or returns null when jailing is unavailable
        /// (non-Windows, or the API refused). Never throws.
        /// </summary>
        /// <param name="memoryLimitBytes">
        /// Per-process commit ceiling. Trinity plus a 4K render target plus a hull's textures
        /// sits comfortably under 2 GB; the limit exists to stop unbounded growth, not to run
        /// close to the edge.
        /// </param>
        /// <param name="cpuPercent">
        /// Share of total CPU the job may use, 1-100, or 0 to leave CPU unlimited. Trinity's
        /// software rasteriser will happily saturate every core, which on a laptop is felt as
        /// fan noise and a stalled UI rather than as a faster render.
        /// </param>
        public static SkinrProcessJail? TryCreate(long memoryLimitBytes, int cpuPercent)
        {
            if (!OperatingSystem.IsWindows())
                return null;

            try
            {
                return CreateWindows(memoryLimitBytes, cpuPercent);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException
                                         or MarshalDirectiveException)
            {
                // A Windows build without the job APIs is not a scenario we can recover from
                // or usefully report; run unjailed.
                return null;
            }
        }

        /// <summary>
        /// Puts a running process under this job's limits. Returns false if the assignment
        /// failed, in which case the process is running unconstrained and the caller should
        /// say so rather than assume otherwise.
        /// </summary>
        public bool TryAssign(Process process)
        {
            if (_disposed || _job == IntPtr.Zero || process == null)
                return false;
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                return AssignProcessToJobObject(_job, process.Handle);
            }
            catch (InvalidOperationException)
            {
                // The process exited between launch and assignment.
                return false;
            }
        }

        /// <summary>
        /// Closes the job handle, which terminates every process still inside it.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_job != IntPtr.Zero && OperatingSystem.IsWindows())
            {
                CloseHandle(_job);
                _job = IntPtr.Zero;
            }
        }

        [SupportedOSPlatform("windows")]
        private static SkinrProcessJail? CreateWindows(long memoryLimitBytes, int cpuPercent)
        {
            IntPtr job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero)
                return null;

            var applied = new System.Text.StringBuilder();
            bool ok = false;

            var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    // KILL_ON_JOB_CLOSE is the whole point; ACTIVE_PROCESS caps the job at the
                    // one sidecar, so a compromised converter cannot fork workers to escape
                    // the memory ceiling by spreading across processes.
                    LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                                 | JOB_OBJECT_LIMIT_ACTIVE_PROCESS
                                 | JOB_OBJECT_LIMIT_PROCESS_MEMORY,
                    ActiveProcessLimit = 1
                },
                ProcessMemoryLimit = (UIntPtr)(ulong)Math.Max(memoryLimitBytes, 64L * 1024 * 1024)
            };

            int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(info, buffer, false);
                if (SetInformationJobObject(job, JobObjectExtendedLimitInformation, buffer,
                        (uint)size))
                {
                    ok = true;
                    applied.Append("kill-on-close, 1 process, ")
                           .Append(memoryLimitBytes / (1024 * 1024)).Append(" MB");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            if (!ok)
            {
                CloseHandle(job);
                return null;
            }

            if (cpuPercent is > 0 and < 100)
                applied.Append(ApplyCpuRate(job, cpuPercent) ? $", {cpuPercent}% CPU"
                                                             : ", CPU unlimited (refused)");
            else
                applied.Append(", CPU unlimited");

            return new SkinrProcessJail(job, applied.ToString());
        }

        [SupportedOSPlatform("windows")]
        private static bool ApplyCpuRate(IntPtr job, int cpuPercent)
        {
            // CPU rate control arrived in Windows 8. It is a nice-to-have, not a security
            // boundary, so a refusal downgrades rather than fails the whole jail.
            var rate = new JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
            {
                ControlFlags = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE
                               | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
                CpuRate = (uint)(cpuPercent * 100)   // hundredths of a percent
            };

            int size = Marshal.SizeOf<JOBOBJECT_CPU_RATE_CONTROL_INFORMATION>();
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(rate, buffer, false);
                return SetInformationJobObject(job, JobObjectCpuRateControlInformation, buffer,
                    (uint)size);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // --- Win32 -----------------------------------------------------------

        private const int JobObjectExtendedLimitInformation = 9;
        private const int JobObjectCpuRateControlInformation = 15;

        private const uint JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x00000008;
        private const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        private const uint JOB_OBJECT_CPU_RATE_CONTROL_ENABLE = 0x00000001;
        private const uint JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP = 0x00000004;

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            public uint ControlFlags;
            public uint CpuRate;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr attributes, string? name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr job, int infoClass,
            IntPtr info, uint length);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
