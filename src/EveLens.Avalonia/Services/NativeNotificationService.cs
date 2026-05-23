// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DesktopNotifications;
using DesktopNotifications.Windows;
using EveLens.Common.Services;
using Microsoft.Extensions.Logging;

namespace EveLens.Avalonia.Services
{
    internal static class NativeNotificationService
    {
        private static ILogger? s_logger;
        private static INotificationManager? s_manager;

        private static ILogger Logger =>
            s_logger ??= AppServices.LoggerFactory.CreateLogger("NativeNotification");

        public static void Initialize()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    s_manager = new WindowsNotificationManager();
                else
                    return;

                s_manager.Initialize().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize notification manager");
            }
        }

        public static void Show(string title, string message)
        {
            try
            {
                if (s_manager == null)
                    return;

                _ = ShowAsync(title, message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to show native notification");
            }
        }

        private static async Task ShowAsync(string title, string message)
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Body = message
                };

                await s_manager!.ShowNotification(notification);
                Logger.LogInformation("Native notification shown: {Title}", title);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to show native notification async");
            }
        }
    }
}
