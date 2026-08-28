// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Detects Gatekeeper App Translocation and repairs the install so in-place
    /// updates can work. See <see cref="IMacInstallService"/> for why this exists.
    /// </summary>
    public sealed class MacInstallService : IMacInstallService
    {
        /// <summary>The path segment macOS uses for its read-only mirror mounts.</summary>
        private const string TranslocationMarker = "/AppTranslocation/";

        private readonly string? _processPath;
        private readonly string _applicationsDir;

        /// <summary>Production constructor — reads the current process path.</summary>
        public MacInstallService()
            : this(Environment.ProcessPath, "/Applications")
        {
        }

        /// <summary>Testing constructor — detection runs on any supplied path.</summary>
        internal MacInstallService(string? processPath, string applicationsDir = "/Applications")
        {
            _processPath = processPath;
            _applicationsDir = applicationsDir;
        }

        /// <inheritdoc/>
        public bool IsTranslocated =>
            _processPath?.Contains(TranslocationMarker, StringComparison.Ordinal) == true;

        /// <summary>
        /// The root of the .app bundle the current process is running from
        /// (e.g. ".../EveLens.app" for ".../EveLens.app/Contents/MacOS/EveLens"),
        /// or null when the process is not inside a bundle.
        /// </summary>
        internal string? RunningBundlePath
        {
            get
            {
                // Walk up with plain string ops: Path.GetDirectoryName would rewrite
                // these macOS paths with '\' when the tests run on Windows.
                string? dir = _processPath;
                while (!string.IsNullOrEmpty(dir))
                {
                    if (dir.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                        return dir;
                    int cut = dir.LastIndexOfAny(s_separators);
                    dir = cut <= 0 ? null : dir.Substring(0, cut);
                }
                return null;
            }
        }

        private static readonly char[] s_separators = { '/', '\\' };

        /// <inheritdoc/>
        public string? RealBundlePath
        {
            get
            {
                string? running = RunningBundlePath;
                if (running == null)
                    return null;

                if (!IsTranslocated)
                    return running;

                // The translocated mount hides the original location, so the best
                // candidate is the canonical install dir — either an existing bundle
                // there, or the place the repair will copy one to. Joined with '/'
                // because these are always macOS paths, whatever OS the tests run on.
                return $"{_applicationsDir}/{Path.GetFileName(running)}";
            }
        }

        /// <inheritdoc/>
        public bool HealAndRelaunch()
        {
            if (!IsTranslocated)
                return false;

            string? running = RunningBundlePath;
            string? target = RealBundlePath;
            if (running == null || target == null)
                return false;

            try
            {
                // The translocated mirror is readable, so if nothing is installed at
                // the real location yet (launched straight from Downloads), put a copy
                // there. /bin/cp -Rp preserves the symlinks and Mach-O permission bits
                // a managed recursive copy would destroy.
                if (!Directory.Exists(target))
                {
                    if (Run("/bin/cp", $"-Rp \"{running}\" \"{target}\"") != 0)
                        return false;
                }

                // Clearing the quarantine attribute is what stops macOS from
                // translocating the bundle. The user has already approved this app
                // through Gatekeeper (it is running), and they just clicked the repair
                // button — this is user-consented self-repair, not evasion.
                if (Run("/usr/bin/xattr", $"-dr com.apple.quarantine \"{target}\"") != 0)
                    return false;

                // --restart-delay gives this instance time to exit and release the
                // single-instance signal before the new one checks it.
                Run("/usr/bin/open", $"-n \"{target}\" --args --restart-delay",
                    waitForExit: false);
                return true;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"MacInstall: heal failed — {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private static int Run(string fileName, string arguments, bool waitForExit = true)
        {
            AppServices.TraceService?.Trace($"MacInstall: {fileName} {arguments}");
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
            });
            if (process == null)
                return -1;
            if (!waitForExit)
                return 0;
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
