// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.Xsl;
using EveLens.Common.Attributes;
using EveLens.Common.CloudStorageServices;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Helpers;
using EveLens.Common.Scheduling;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Core.Events;
using CommonEvents = EveLens.Common.Events;

namespace EveLens.Common
{
    /// <summary>
    /// Stores EveLens's current settings and writes them to the settings file when necessary.
    /// </summary>
    [EnforceUIThreadAffinity]
    public static partial class Settings
    {
        private static volatile bool s_savePending;
        private static DateTime s_nextSaveTime;
        private static XslCompiledTransform? s_settingsTransform;
        private static volatile SerializableSettings? s_settings;
        private static volatile SmartSettingsManager? s_smartSettingsManager;
        private static IDisposable? s_thirtySecondTickSubscription;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Settings()
        {
            // ESI credentials loaded from esi-credentials.json (gitignored)
            // Create your own at https://developers.eveonline.com/
            LoadESICredentials();
            UI = new UISettings();
            G15 = new G15Settings();
            Proxy = new ProxySettings();
            Updates = new UpdateSettings();
            Calendar = new CalendarSettings();
            Exportation = new ExportationSettings();
            MarketPricer = new MarketPricerSettings();
            Notifications = new NotificationSettings();
            LoadoutsProvider = new LoadoutsProviderSettings();
            PortableEveInstallations = new PortableEveInstallationsSettings();
            CloudStorageServiceProvider = new CloudStorageServiceProviderSettings();

            // Use ThirtySecondTick instead of TimerTick for save checks
            // Saves are already delayed by 10 seconds, so checking every 30s is sufficient
            s_thirtySecondTickSubscription = AppServices.EventAggregator?.Subscribe<ThirtySecondTickEvent>(
                e => EveLensClient_TimerTick(null, EventArgs.Empty));
        }

        // Default ESI credentials for EveLens - registered by Alia Collins
        // Users can override via esi-credentials.json in app directory
        private const string DefaultClientID = "e87550c5642e4de0bac3b124d110ca7a";
        private const string DefaultClientSecret = "eat_qpDb4LCQRKRcGWKNfoLhcrRlqQo75Aes_3fgYhF";

        /// <summary>
        /// Handles the TimerTick event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static async void EveLensClient_TimerTick(object? sender, EventArgs e)
        {
            try
            {
                await UpdateOnOneSecondTickAsync();
            }
            catch (Exception ex)
            {
                ExceptionHandler.LogException(ex, false);
            }
        }

        /// <summary>
        /// Gets true if we're currently restoring the settings.
        /// </summary>
        public static bool IsRestoring { get; private set; }


        #region The very settings

        /// <summary>
        /// Gets or sets the SSO client ID.
        /// </summary>
        public static string SSOClientID { get; private set; } = null!;

        /// <summary>
        /// Gets or sets the SSO secret key.
        /// </summary>
        public static string SSOClientSecret { get; private set; } = null!;

        /// <summary>
        /// Gets or sets the compatibility mode.
        /// </summary>
        public static CompatibilityMode Compatibility { get; private set; }

        /// <summary>
        /// Gets the settings for updates.
        /// </summary>
        public static UpdateSettings Updates { get; private set; }

        /// <summary>
        /// Gets the settings for UI (look'n feel)
        /// </summary>
        public static UISettings UI { get; private set; }

        /// <summary>
        /// Gets the settings for the G15 keyboard.
        /// </summary>
        public static G15Settings G15 { get; private set; }

        /// <summary>
        /// Gets the settings for the notifications (alerts).
        /// </summary>
        public static NotificationSettings Notifications { get; private set; }

        /// <summary>
        /// Gets the settings for the portable EVE installations.
        /// </summary>
        public static PortableEveInstallationsSettings PortableEveInstallations { get; private set; }

        /// <summary>
        /// Gets the calendar settings.
        /// </summary>
        public static CalendarSettings Calendar { get; private set; }

        /// <summary>
        /// Gets or sets the exportation settings.
        /// </summary>
        public static ExportationSettings Exportation { get; private set; }

        /// <summary>
        /// Gets or sets the custom proxy settings.
        /// </summary>
        public static ProxySettings Proxy { get; private set; }

        /// <summary>
        /// Gets the market pricer settings.
        /// </summary>
        public static MarketPricerSettings MarketPricer { get; private set; }

        /// <summary>
        /// Gets the loadouts provider settings.
        /// </summary>
        public static LoadoutsProviderSettings LoadoutsProvider { get; private set; }

        /// <summary>
        /// Gets the cloud storage service provider settings.
        /// </summary>
        public static CloudStorageServiceProviderSettings CloudStorageServiceProvider { get; private set; }

        /// <summary>
        /// Gets or sets the ESI scope preset name.
        /// </summary>
        public static string EsiScopePreset { get; set; } = "FullMonitoring";

        /// <summary>
        /// Gets the custom ESI scopes when preset is "Custom".
        /// </summary>
        public static IList<string> EsiCustomScopes { get; private set; } = new List<string>();

        /// <summary>
        /// Gets the character group settings for organizing the overview.
        /// </summary>
        public static IList<CharacterGroupSettings> CharacterGroups { get; private set; } = new List<CharacterGroupSettings>();

        #endregion


        #region Import

        /// <summary>
        /// Imports the provided serialization object.
        /// </summary>
        private static void Import()
        {
            AppServices.TraceService?.Trace("begin");

            // When null, we just reset
            if (s_settings == null)
                s_settings = new SerializableSettings();

            try
            {
                // API settings: empty string means "use defaults" (user clicked Use Default).
                // Non-empty, non-default values are custom credentials the user entered.
                if (string.IsNullOrEmpty(s_settings.SSOClientID))
                    SSOClientID = DefaultClientID;
                else
                    SSOClientID = s_settings.SSOClientID;

                if (string.IsNullOrEmpty(s_settings.SSOClientSecret))
                    SSOClientSecret = DefaultClientSecret;
                else
                    SSOClientSecret = s_settings.SSOClientSecret;

                // User settings
                UI = s_settings.UI;
                G15 = s_settings.G15;
                Proxy = s_settings.Proxy;
                Updates = s_settings.Updates;
                Calendar = s_settings.Calendar;
                Exportation = s_settings.Exportation;
                MarketPricer = s_settings.MarketPricer;
                Notifications = s_settings.Notifications;
                Compatibility = s_settings.Compatibility;
                LoadoutsProvider = s_settings.LoadoutsProvider;
                PortableEveInstallations = s_settings.PortableEveInstallations;
                CloudStorageServiceProvider = s_settings.CloudStorageServiceProvider;

                // ESI scope settings
                EsiScopePreset = s_settings.EsiScopePreset ?? "FullMonitoring";
                EsiCustomScopes = new List<string>(s_settings.EsiCustomScopes);

                // Character groups
                CharacterGroups = new List<CharacterGroupSettings>(s_settings.CharacterGroups);

                // Scheduler
                Scheduler.Import(s_settings.Scheduler);
            }
            finally
            {
                AppServices.TraceService?.Trace("done");

                // Notify the subscribers
                AppServices.TraceService?.Trace("SettingsChanged");
                AppServices.EventAggregator?.Publish(SettingsChangedEvent.Instance);
                AppServices.EventAggregator?.Publish(CommonEvents.SettingsChangedEvent.Instance);
            }
        }

        #endregion


        #region Save

        /// <summary>
        /// Every timer tick, checks whether we should save the settings every 10s.
        /// </summary>
        private static async Task UpdateOnOneSecondTickAsync()
        {
            // Is a save requested and is the last save older than 10s ?
            if (s_savePending && DateTime.UtcNow > s_nextSaveTime)
                await SaveImmediateAsync();
        }

        /// <summary>
        /// Saves settings to disk.
        /// </summary>
        /// <remarks>
        /// Saves will be cached for 10 seconds to avoid thrashing the disk when this method is called very rapidly
        /// such as at startup. If a save is currently pending, no action is needed. 
        /// </remarks>
        public static void Save()
        {
            if (IsRestoring)
                return;

            if (s_smartSettingsManager != null)
            {
                // SmartSettingsManager owns the full pipeline: Export→Serialize→Write
                s_smartSettingsManager.Save();
                return;
            }

            s_savePending = true;
        }

        /// <summary>
        /// Saves settings immediately. JSON-only — no XML writes.
        /// </summary>
        public static async Task SaveImmediateAsync()
        {
            // Prevents the saving if we are restoring the settings at that time
            if (IsRestoring)
                return;

            // If SmartSettingsManager is active, it owns the full pipeline exclusively
            if (s_smartSettingsManager != null)
            {
                await s_smartSettingsManager.SaveImmediateAsync();
                return;
            }

            // Reset flags
            s_savePending = false;
            s_nextSaveTime = DateTime.UtcNow.AddSeconds(10);

            AppServices.TraceService?.Trace("begin");

            try
            {
                // Export settings on UI thread (required for collection access)
                SerializableSettings settings = Export();
                AppServices.TraceService?.Trace("Export done");

                // JSON-only save — SerializableSettings serialized directly
                await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
                AppServices.TraceService?.Trace("JSON save complete");
            }
            catch (Exception exception)
            {
                AppServices.TraceService?.Trace($"Error: {exception.Message}");
                ExceptionHandler.LogException(exception, true);
            }
        }

        /// <summary>
        /// Shuts down settings services, disposing the SmartSettingsManager if active.
        /// Called during application shutdown.
        /// </summary>
        public static void Shutdown()
        {
            s_thirtySecondTickSubscription?.Dispose();
            s_thirtySecondTickSubscription = null;
            s_smartSettingsManager?.Dispose();
            s_smartSettingsManager = null;
        }

        #endregion
    }
}
