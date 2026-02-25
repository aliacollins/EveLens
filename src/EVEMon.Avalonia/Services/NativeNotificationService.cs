// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using EVEMon.Common.Services;
using Microsoft.Extensions.Logging;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Cross-platform native OS notification service.
    ///   Windows — WinRT ToastNotification with registered AUMID (Start menu shortcut)
    ///   Linux   — notify-send (libnotify)
    ///   macOS   — osascript display notification
    /// </summary>
    internal static class NativeNotificationService
    {
        private const string AppId = "EVEMonNexT";
        private const string AppDisplayName = "EVEMon NexT";
        private static ILogger? s_logger;
        private static bool s_windowsInitialized;

        private static ILogger Logger =>
            s_logger ??= AppServices.LoggerFactory.CreateLogger("NativeNotification");

        public static void Show(string title, string message)
        {
            try
            {
                Logger.LogInformation("Show — platform={Platform}, title='{Title}'",
                    Environment.OSVersion.Platform, title);

                if (OperatingSystem.IsWindows())
                    ShowWindows(title, message);
                else if (OperatingSystem.IsLinux())
                    ShowLinux(title, message);
                else if (OperatingSystem.IsMacOS())
                    ShowMacOS(title, message);
                else
                    Logger.LogWarning("Unsupported platform for native notifications");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to show native notification");
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows10.0.19041.0")]
        private static void ShowWindows(string title, string message)
        {
            try
            {
                // Windows 11 requires apps to have a registered AUMID (Application User Model ID)
                // to show toast notifications. We register by creating a Start menu shortcut
                // with the AUMID property set — this is the same mechanism all desktop apps use.
                if (!s_windowsInitialized)
                {
                    EnsureWindowsShortcut();
                    s_windowsInitialized = true;
                }

                string xmlTitle = EscapeXml(title);
                string xmlMessage = EscapeXml(message);

                string toastXml =
                    "<toast>" +
                        "<visual>" +
                            "<binding template=\"ToastGeneric\">" +
                                $"<text>{xmlTitle}</text>" +
                                $"<text>{xmlMessage}</text>" +
                            "</binding>" +
                        "</visual>" +
                    "</toast>";

                // Use reflection to invoke WinRT toast APIs to avoid compile-time dependency
                // on net8.0-windows TFM (keeping cross-platform net8.0 target).
                var xmlDocType = Type.GetType("Windows.Data.Xml.Dom.XmlDocument, Microsoft.Windows.SDK.NET");
                if (xmlDocType == null)
                {
                    Logger.LogWarning("WinRT XmlDocument type not available — falling back to silent notification");
                    return;
                }
                var doc = Activator.CreateInstance(xmlDocType)!;
                xmlDocType.GetMethod("LoadXml")!.Invoke(doc, new object[] { toastXml });

                var toastNotificationType = Type.GetType("Windows.UI.Notifications.ToastNotification, Microsoft.Windows.SDK.NET")!;
                var toast = Activator.CreateInstance(toastNotificationType, doc)!;

                var managerType = Type.GetType("Windows.UI.Notifications.ToastNotificationManager, Microsoft.Windows.SDK.NET")!;
                var notifier = managerType.GetMethod("CreateToastNotifier", new[] { typeof(string) })!
                    .Invoke(null, new object[] { AppId })!;
                notifier.GetType().GetMethod("Show")!.Invoke(notifier, new[] { toast });

                Logger.LogInformation("Windows toast shown via WinRT API (AUMID={AppId})", AppId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "WinRT toast notification failed");
            }
        }

        /// <summary>
        /// Creates a Start menu shortcut with the AUMID property so Windows recognizes the app
        /// as a valid toast notification source. This is the standard mechanism for desktop
        /// (non-MSIX/non-Store) apps. The shortcut is created once and persists.
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static void EnsureWindowsShortcut()
        {
            try
            {
                string startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Windows", "Start Menu", "Programs");
                string shortcutPath = Path.Combine(startMenu, $"{AppDisplayName}.lnk");

                if (File.Exists(shortcutPath))
                {
                    Logger.LogDebug("Start menu shortcut already exists at {Path}", shortcutPath);
                    return;
                }

                // Create shortcut using COM IShellLink + IPropertyStore
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return;

                var shellLink = (IShellLinkW)new CShellLink();
                shellLink.SetPath(exePath);
                shellLink.SetDescription(AppDisplayName);

                // Set the AUMID on the shortcut via IPropertyStore
                var propertyStore = (IPropertyStore)shellLink;
                var aumidKey = new PROPERTYKEY
                {
                    fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),
                    pid = 5 // System.AppUserModel.ID
                };
                var propVariant = new PROPVARIANT { vt = 31, pwszVal = Marshal.StringToCoTaskMemUni(AppId) };
                propertyStore.SetValue(ref aumidKey, ref propVariant);
                propertyStore.Commit();

                var persistFile = (IPersistFile)shellLink;
                persistFile.Save(shortcutPath, true);

                Marshal.FreeCoTaskMem(propVariant.pwszVal);
                Logger.LogInformation("Created Start menu shortcut with AUMID at {Path}", shortcutPath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to create Start menu shortcut for toast registration");
            }
        }

        private static void ShowLinux(string title, string message)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "notify-send",
                ArgumentList = { "--app-name", AppDisplayName, title, message },
                CreateNoWindow = true,
                UseShellExecute = false
            });
            Logger.LogInformation("notify-send launched (PID {Pid})", process?.Id ?? -1);
        }

        private static void ShowMacOS(string title, string message)
        {
            string escapedTitle = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string escapedMessage = message.Replace("\\", "\\\\").Replace("\"", "\\\"");

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "osascript",
                ArgumentList =
                {
                    "-e",
                    $"display notification \"{escapedMessage}\" with title \"{escapedTitle}\""
                },
                CreateNoWindow = true,
                UseShellExecute = false
            });
            Logger.LogInformation("osascript launched (PID {Pid})", process?.Id ?? -1);
        }

        private static string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        #region Windows COM Interop for IShellLink + IPropertyStore

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class CShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath(IntPtr pszFile, int cch, IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription(IntPtr pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory(IntPtr pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments(IntPtr pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation(IntPtr pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PROPERTYKEY pkey);
            void GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
            void SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);
            void Commit();
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetCurFile(out IntPtr ppszFileName);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROPVARIANT
        {
            public ushort vt;
            public ushort wReserved1;
            public ushort wReserved2;
            public ushort wReserved3;
            public IntPtr pwszVal;
        }

        #endregion
    }
}
