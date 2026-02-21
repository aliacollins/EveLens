// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.InteropServices;
using System.Security;

namespace EVEMon.WindowsApi
{
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/dd378422%28VS.85%29.aspx
        /// </summary>
        /// <param name="appID">appID string</param>
        [DllImport("shell32.dll")]
        internal static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appID);
    }
}