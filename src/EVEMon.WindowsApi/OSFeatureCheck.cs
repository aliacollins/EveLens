// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.WindowsApi
{
    /// <summary>
    /// Allows for quick checks for support of various opperating system features
    /// </summary>
    public static class OSFeatureCheck
    {
        /// <summary>
        /// Checks to see if the current operating system is Windows
        /// </summary>
        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                                        Environment.OSVersion.Platform == PlatformID.Win32S ||
                                        IsWindowsNT;

        /// <summary>
        /// Checks to see if the current operating system is Windows NT
        /// </summary>
        public static bool IsWindowsNT => Environment.OSVersion.Platform == PlatformID.Win32NT;

        /// <summary>
        /// Checks to see if the Windows 7 Taskbar is supported in the
        /// current operating system.
        /// </summary>
        public static bool TaskbarSupported
        {
            get
            {
                if (!IsWindowsNT)
                    return false;

                Version winVer = Environment.OSVersion.Version;

                if (winVer.Major < 6)
                    return false;

                return winVer.Major != 6 || winVer.Minor >= 1;
            }
        }
    }
}