// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Common.Services
{
    public static class Loc
    {
        private static string _language = "en";
        private static readonly Dictionary<string, Dictionary<string, string>> s_translations = new();

        static Loc()
        {
            RegisterEnglish();
            RegisterChineseSimplified();
        }

        public static string Language
        {
            get => _language;
            set => _language = value ?? "en";
        }

        public static string Get(string key)
        {
            if (s_translations.TryGetValue(_language, out var table) &&
                table.TryGetValue(key, out var value))
                return value;

            if (_language != "en" &&
                s_translations.TryGetValue("en", out var fallback) &&
                fallback.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key;
        }

        public static string[] AvailableLanguages => new[] { "en", "zh-CN" };

        public static string GetLanguageDisplayName(string code) => code switch
        {
            "en" => "English",
            "zh-CN" => "简体中文 (Simplified Chinese)",
            _ => code,
        };

        private static void Register(string lang, Dictionary<string, string> strings)
        {
            s_translations[lang] = strings;
        }

        private static void RegisterEnglish()
        {
            Register("en", new Dictionary<string, string>
            {
                // Main Window
                ["Menu.File"] = "File",
                ["Menu.Plans"] = "Plans",
                ["Menu.Tools"] = "Tools",
                ["Menu.Help"] = "Help",
                ["Menu.File.AddCharacter"] = "Add Character",
                ["Menu.File.ManageGroups"] = "Manage Groups",
                ["Menu.File.Settings"] = "Settings",
                ["Menu.File.Exit"] = "Exit",
                ["Menu.Plans.NewPlan"] = "New Plan",
                ["Menu.Plans.ManagePlans"] = "Manage Plans",
                ["Menu.Plans.ImportPlan"] = "Import Plan from File",
                ["Menu.Plans.CreateFromQueue"] = "Create Plan from Queue",
                ["Menu.Tools.CharComparison"] = "Character Comparison",
                ["Menu.Tools.SkillFarm"] = "Skill Farm Dashboard",
                ["Menu.Tools.DoctrineDesigner"] = "Doctrine Designer",
                ["Menu.Tools.SkillVisualization"] = "Skill Visualization",
                ["Menu.Tools.ClearCache"] = "Clear Cache",
                ["Menu.Help.About"] = "About EveLens",
                ["Menu.Help.KnownProblems"] = "Known Problems",
                ["Menu.Help.CheckUpdates"] = "Check for Updates",
                ["Menu.Help.UserGuide"] = "User Guide",
                ["Menu.Help.ReportIssue"] = "Report Issue",
                ["Menu.Help.Shortcuts"] = "Keyboard Shortcuts",
                ["Menu.File.CreateBlank"] = "Create Blank Character",
                ["Menu.File.ManageChars"] = "Manage Characters",
                ["Menu.File.RestoreSettings"] = "Restore Settings",
                ["Menu.File.SaveSettings"] = "Save Settings",
                ["Menu.File.ResetSettings"] = "Reset Settings",

                // Tabs
                ["Tab.Skills"] = "Skills",
                ["Tab.Queue"] = "Queue",
                ["Tab.Clones"] = "Clones",
                ["Tab.Employment"] = "Employment",
                ["Tab.Assets"] = "Assets",
                ["Tab.MarketOrders"] = "Market Orders",
                ["Tab.Contracts"] = "Contracts",
                ["Tab.IndustryJobs"] = "Industry Jobs",
                ["Tab.Wallet"] = "Wallet",
                ["Tab.Mail"] = "Mail",
                ["Tab.Notifications"] = "Notifications",
                ["Tab.KillLog"] = "Kill Log",
                ["Tab.Planetary"] = "Planetary",
                ["Tab.Research"] = "Research",

                // Common Actions
                ["Action.CollapseAll"] = "Collapse All",
                ["Action.ExpandAll"] = "Expand All",
                ["Action.TrainedOnly"] = "Trained Only",
                ["Action.ExportCsv"] = "Export CSV",
                ["Action.AllSkills"] = "All Skills",
                ["Action.Search"] = "Search...",
                ["Action.Save"] = "Save",
                ["Action.Cancel"] = "Cancel",
                ["Action.Close"] = "Close",
                ["Action.Apply"] = "Apply",
                ["Action.Create"] = "Create",
                ["Action.Delete"] = "Delete",
                ["Action.Refresh"] = "Refresh",
                ["Action.AddCharacter"] = "+ Add Character",
                ["Action.AddSkill"] = "+ Add Skill",

                // EVE Game Terms
                ["Eve.SkillPoints"] = "Skill Points",
                ["Eve.SP"] = "SP",
                ["Eve.SPPerHour"] = "SP/hr",
                ["Eve.ISK"] = "ISK",
                ["Eve.Omega"] = "Omega",
                ["Eve.Alpha"] = "Alpha",
                ["Eve.Intelligence"] = "Intelligence",
                ["Eve.Memory"] = "Memory",
                ["Eve.Perception"] = "Perception",
                ["Eve.Willpower"] = "Willpower",
                ["Eve.Charisma"] = "Charisma",
                ["Eve.Rank"] = "Rank",
                ["Eve.Level"] = "Level",
                ["Eve.Training"] = "Training",
                ["Eve.Queued"] = "Queued",
                ["Eve.Completed"] = "Completed",
                ["Eve.AlreadyTrained"] = "Already Trained",

                // Plan Editor
                ["Plan.Skill"] = "SKILL",
                ["Plan.Time"] = "TIME",
                ["Plan.Primary"] = "PRI",
                ["Plan.Secondary"] = "SEC",
                ["Plan.GroupByAttr"] = "Group by Attr",
                ["Plan.GroupedByAttr"] = "Grouped by Attr ✓",
                ["Plan.QueueReordered"] = "Queue reordered",
                ["Plan.DragToReorder"] = "Drag to reorder · Double-click for details",

                // Skill Farm
                ["SkillFarm.Title"] = "Skill Farm Dashboard",
                ["SkillFarm.AddAll"] = "+ Add All Eligible",
                ["SkillFarm.Character"] = "Character",
                ["SkillFarm.Injectors"] = "Injectors",
                ["SkillFarm.ExtractorCost"] = "Extractor Cost",
                ["SkillFarm.Revenue"] = "Revenue",
                ["SkillFarm.NetProfit"] = "Net Profit",
                ["SkillFarm.Status"] = "Status",

                // Doctrine Designer
                ["Doctrine.Title"] = "Doctrine Designer",
                ["Doctrine.Doctrines"] = "Doctrines",
                ["Doctrine.NewDoctrine"] = "+ New Doctrine",
                ["Doctrine.ImportFromPlan"] = "Import from Plan",
                ["Doctrine.CreatePlan"] = "Create Plan",
                ["Doctrine.PlanCreated"] = "Plan Created!",
                ["Doctrine.CreatePlansForAll"] = "Create Plans for All",
                ["Doctrine.NoDoctrines"] = "No doctrines yet.\nClick '+ New Doctrine' to create one,\nor 'Import from Plan' to use an existing plan.",
                ["Doctrine.SelectDoctrine"] = "Select a doctrine from the sidebar to view skill comparisons.",
                ["Doctrine.NoCharacters"] = "No characters assigned.\nClick '+ Add Character' to assign characters to this doctrine.",
                ["Doctrine.AllTrained"] = "All trained!",

                // Settings — Window
                ["Settings.Title"] = "Settings",
                ["Settings.WindowTitle"] = "EveLens Settings",
                ["Settings.SearchWatermark"] = "Search settings...",

                // Settings — Sidebar
                ["Settings.Appearance"] = "Appearance",
                ["Settings.AppearanceSub"] = "Theme, colors, compatibility",
                ["Settings.Window"] = "Window",
                ["Settings.WindowSub"] = "Tray icon, close behavior",
                ["Settings.Notifications"] = "Notifications",
                ["Settings.NotificationsSub"] = "Alerts, email, calendar",
                ["Settings.Data"] = "Data",
                ["Settings.DataSub"] = "Updates, market prices",
                ["Settings.Network"] = "Network",
                ["Settings.NetworkSub"] = "Proxy, SSO credentials",
                ["Settings.ESI"] = "ESI",
                ["Settings.ESISub"] = "API scopes, permissions",

                // Settings — Appearance section
                ["Settings.AppearanceDesc"] = "Customize the look and feel of EveLens.",
                ["Settings.Theme"] = "Theme",
                ["Settings.ThemeDesc"] = "Choose the visual theme for the application.",
                ["Settings.RestartRequired"] = "Restart required to apply",
                ["Settings.RestartNow"] = "Restart Now",
                ["Settings.FontSize"] = "Font Size",
                ["Settings.FontSizeDesc"] = "Scale all text in the application (80%-150%).",
                ["Settings.Language"] = "Language",
                ["Settings.LanguageDesc"] = "Game terms use CCP's official translations.",
                ["Settings.LanguageNote"] = "Game terms use CCP's official translations. Community translation — help us improve!",
                ["Settings.SafeForWork"] = "Safe for Work",
                ["Settings.SafeForWorkDesc"] = "Hide character portraits and identifying colors.",
                ["Settings.CustomBrowser"] = "Browser",
                ["Settings.CustomBrowserDesc"] = "Choose a browser for opening links. Default uses your OS setting.",
                ["Settings.DefaultBrowser"] = "System Default",
                ["Settings.OpenDataDir"] = "Open Data Directory",

                // Settings — Window section
                ["Settings.WindowBehavior"] = "Window Behavior",
                ["Settings.WindowBehaviorDesc"] = "Configure how the main window and system tray behave.",
                ["Settings.MinimizeToTray"] = "Minimize to Tray",
                ["Settings.MinimizeToTrayDesc"] = "When enabled, closing the window minimizes EveLens to the system tray instead of exiting. Click the tray icon to restore.",
                ["Settings.TrayTooltipDisplay"] = "Tray Tooltip Display",
                ["Settings.TrayTooltipDesc"] = "Choose what information shows when hovering the system tray icon.",
                ["Settings.TrayCountAndNext"] = "Training Count + Next Finisher",
                ["Settings.TrayCountOnly"] = "Training Count Only",
                ["Settings.TrayNextOnly"] = "Next Finisher Only",

                // Settings — Notifications section
                ["Settings.NotificationsDesc"] = "Configure alerts and notifications for skill training and events.",
                ["Settings.OsNotifications"] = "OS Notifications",
                ["Settings.OsNotificationsDesc"] = "Show Windows toast notifications for skill completions and alerts.",
                ["Settings.PlaySound"] = "Play Sound",
                ["Settings.PlaySoundDesc"] = "Play a sound when skill training completes.",
                ["Settings.EmailAlerts"] = "Email Alerts",
                ["Settings.EmailAlertsDesc"] = "Send an email when skill training completes.",
                ["Settings.Provider"] = "Provider",
                ["Settings.SmtpServer"] = "SMTP Server",
                ["Settings.Port"] = "Port",
                ["Settings.SslTls"] = "SSL/TLS",
                ["Settings.SslTlsDesc"] = "Require encrypted connection.",
                ["Settings.Authentication"] = "Authentication",
                ["Settings.AuthenticationDesc"] = "Server requires username and password.",
                ["Settings.Username"] = "Username",
                ["Settings.Password"] = "Password",
                ["Settings.From"] = "From",
                ["Settings.To"] = "To",
                ["Settings.ShortFormat"] = "Short Format",
                ["Settings.ShortFormatDesc"] = "Use a compact email format.",
                ["Settings.ExternalCalendar"] = "External Calendar",
                ["Settings.ExternalCalendarDesc"] = "Sync skill training schedules to an external calendar.",
                ["Settings.GoogleCalendar"] = "Google Calendar",
                ["Settings.MicrosoftOutlook"] = "Microsoft Outlook",
                ["Settings.CalendarName"] = "Calendar Name",
                ["Settings.Reminder"] = "Reminder",
                ["Settings.UseDefaultCalendar"] = "Use default calendar",
                ["Settings.CalendarPath"] = "Calendar Path",
                ["Settings.EnableReminders"] = "Enable reminders",
                ["Settings.IntervalMin"] = "Interval (min)",
                ["Settings.AltRemindingTimes"] = "Use alternate reminding times",
                ["Settings.Early"] = "Early",
                ["Settings.Late"] = "Late",
                ["Settings.LastQueuedOnly"] = "Export last queued skill only",

                // Settings — Data section
                ["Settings.DataUpdates"] = "Data & Updates",
                ["Settings.DataUpdatesDesc"] = "Manage automatic updates and market data providers.",
                ["Settings.CheckForUpdates"] = "Check for Updates",
                ["Settings.CheckForUpdatesDesc"] = "Automatically check for new EveLens versions on startup.",
                ["Settings.ClockSync"] = "Clock Sync",
                ["Settings.ClockSyncDesc"] = "Verify system clock is synchronized on startup.",
                ["Settings.MarketPriceProvider"] = "Market Price Provider",
                ["Settings.MarketPriceProviderDesc"] = "Source for market price lookups.",

                // Settings — Network section
                ["Settings.NetworkDesc"] = "Configure proxy and SSO credentials.",
                ["Settings.HttpProxy"] = "HTTP Proxy",
                ["Settings.HttpProxyDesc"] = "Route traffic through a custom proxy server.",
                ["Settings.Host"] = "Host",
                ["Settings.SsoCredentials"] = "SSO Credentials",
                ["Settings.SsoCredentialsDesc"] = "Override built-in EveLens SSO client credentials.",
                ["Settings.ClientId"] = "Client ID",
                ["Settings.ClientSecret"] = "Client Secret",
                ["Settings.UseDefault"] = "Use Default",

                // Settings — ESI section
                ["Settings.EsiScopes"] = "ESI Scopes",
                ["Settings.EsiScopesDesc"] = "Controls which API scopes EveLens requests when authenticating.",
                ["Settings.ScopePreset"] = "Scope Preset",
                ["Settings.ScopePresetDesc"] = "Choose a predefined set of API permissions.",
                ["Settings.CustomizeScopes"] = "Customize Scopes...",
                ["Settings.EsiChangesNote"] = "Changes take effect the next time you authenticate a character.",
                ["Settings.CharacterScopes"] = "Character Scopes",
                ["Settings.NoCharsAuthenticated"] = "No characters authenticated yet.",
                ["Settings.EsiReauthNote"] = "Characters not matching the selected preset can be re-authenticated to update their scopes.",
                ["Settings.Reauthenticate"] = "Re-authenticate",
                ["Settings.Restart"] = "Restart",
                ["Settings.RestartEveLens"] = "Restart EveLens",
                ["Settings.RestartMessage"] = "EveLens will restart to apply the new theme.\nPlease save any work in progress (e.g., skill plans being edited).",

                // Status / Empty States
                ["Status.Paused"] = "Paused",
                ["Status.Training"] = "Training",
                ["Status.Online"] = "Online",
                ["Status.SkillsInQueue"] = "Skills in queue",
                ["Status.Trained"] = "Trained",
                ["Status.Of"] = "of",
                ["Status.Skills"] = "skills",
                ["Status.TotalSP"] = "Total SP",
                ["Status.Remaining"] = "remaining",

                // Character Header
                ["Header.Corporation"] = "Corporation",
                ["Header.Alliance"] = "Alliance",
                ["Header.LocatedIn"] = "Located in",
                ["Header.DockedAt"] = "Docked at",
                ["Header.InSpace"] = "In space",
                ["Header.Skills"] = "Skills",
                ["Header.SP"] = "SP",
                ["Header.FreeSP"] = "Free SP",
                ["Header.TotalSP"] = "Total SP",
                ["Header.Remaps"] = "Remaps",
                ["Header.AutoDetect"] = "Auto-detect",
                ["Header.AlphaOverride"] = "Alpha override",
                ["Header.OmegaOverride"] = "Omega override",

                // ESI
                ["ESI.NextRefresh"] = "Next refresh in",
                ["ESI.Fetching"] = "fetching...",
                ["ESI.Idle"] = "ESI: idle",
                ["ESI.Refreshing"] = "ESI: refreshing...",
                ["ESI.LastRefresh"] = "Last refresh",

                // Splash
                ["Splash.Tagline"] = "Character Intelligence for EVE Online",
                ["Splash.LoadingGameData"] = "Loading game data...",

                // Skills level filter strip
                ["Skills.AllSkills"] = "All Skills",
                ["Skills.AllTrained"] = "All Trained",
                ["Skills.LevelV"] = "Level V",
                ["Skills.LevelIV"] = "Level IV",
                ["Skills.LevelIII"] = "Level III",
                ["Skills.LevelII"] = "Level II",
                ["Skills.LevelI"] = "Level I",
                ["Skills.Injected"] = "Injected",
                ["Skills.Untrained"] = "Untrained",

                // Character header labels
                ["Header.SecurityStatus"] = "Security Status",
                ["Header.ActiveShip"] = "Active Ship",
                ["Header.Unknown"] = "Unknown",
                ["Header.KnownSkills"] = "Known Skills",
                ["Header.BonusRemaps"] = "Bonus Remaps",

                // Missing character monitor tabs
                ["Tab.Standings"] = "Standings",
                ["Tab.Contacts"] = "Contacts",
                ["Tab.Medals"] = "Medals",
                ["Tab.LP"] = "LP",
                ["Tab.Transactions"] = "Transactions",

                // Plan Editor Window
                ["PlanEditor.Title"] = "Plan Editor",
                ["PlanEditor.PlanTab"] = "Plan",
                ["PlanEditor.SkillsTab"] = "Skills",
                ["PlanEditor.ShipsTab"] = "Ships",
                ["PlanEditor.ItemsTab"] = "Items",
                ["PlanEditor.BlueprintsTab"] = "Blueprints",
                ["PlanEditor.SearchSkills"] = "Search skills...",
                ["PlanEditor.ClearPlan"] = "Clear Plan",
                ["PlanEditor.ImportFit"] = "Import Fit",
                ["PlanEditor.Export"] = "Export",
                ["PlanEditor.AddSkills"] = "+ Add Skills",
                ["PlanEditor.CopyPlan"] = "Copy Skill Plan",
                ["PlanEditor.Done"] = "Done",
                ["PlanEditor.SkillsToTrain"] = "skills to train",
                ["PlanEditor.SkillsRemaining"] = "skills remaining",
                ["PlanEditor.OptimizePlan"] = "Optimize Plan",
                ["PlanEditor.Remap"] = "Remap",
                ["PlanEditor.AvailableNow"] = "Available now",
                ["PlanEditor.Bonus"] = "bonus",
                ["PlanEditor.TrainingTime"] = "Training time",
                ["PlanEditor.Unique"] = "unique",
                ["PlanEditor.Finishes"] = "Finishes",
                ["PlanEditor.Books"] = "books",
                ["PlanEditor.Skills"] = "skills",
                ["PlanEditor.Of"] = "of",
                ["PlanEditor.Remaining"] = "remaining",
                ["PlanEditor.NextIn"] = "Next in",
                ["PlanEditor.Injectors"] = "injectors",
                ["PlanEditor.CopyToClipboard"] = "Copy to Clipboard",
                ["PlanEditor.SaveToFile"] = "Save to File...",
                ["PlanEditor.FromClipboard"] = "From Clipboard (EFT/XML/DNA)",
                ["PlanEditor.FromFile"] = "From File...",
                ["PlanEditor.PlanTo"] = "Plan to",
                ["PlanEditor.Back"] = "Back",

                // Skill Browser
                ["PlanEditor.AllAttributes"] = "All Attributes",
                ["PlanEditor.AllSkills"] = "All Skills",
                ["PlanEditor.Trained"] = "Trained",
                ["PlanEditor.HavePrereqs"] = "Have Prerequisites",
                ["PlanEditor.Untrained"] = "Untrained",
                ["PlanEditor.Collapse"] = "Collapse",
                ["PlanEditor.Expand"] = "Expand",
                ["PlanEditor.NotTrained"] = "Not trained",
                ["PlanEditor.Rank"] = "Rank:",
                ["PlanEditor.Primary"] = "Primary:",
                ["PlanEditor.Secondary"] = "Secondary:",
                ["PlanEditor.TrainingTimeLabel"] = "Training Time:",
                ["PlanEditor.Prerequisites"] = "Prerequisites:",

                // Ship Browser
                ["ShipBrowser.SearchShips"] = "Search ships...",
                ["ShipBrowser.CanFlyOnly"] = "Can Fly Only",
                ["ShipBrowser.RequiredSkills"] = "Required Skills:",
                ["ShipBrowser.Properties"] = "Properties:",
                ["ShipBrowser.PlanToFly"] = "Plan to Fly",
                ["ShipBrowser.Race"] = "Race:",
                ["ShipBrowser.Amarr"] = "Amarr",
                ["ShipBrowser.Caldari"] = "Caldari",
                ["ShipBrowser.Gallente"] = "Gallente",
                ["ShipBrowser.Minmatar"] = "Minmatar",

                // Item Browser
                ["ItemBrowser.SearchItems"] = "Search items...",
                ["ItemBrowser.CanUseOnly"] = "Can Use Only",
                ["ItemBrowser.CollapseAll"] = "Collapse All",
                ["ItemBrowser.Slot"] = "Slot:",
                ["ItemBrowser.RequiredSkills"] = "Required Skills:",
                ["ItemBrowser.Properties"] = "Properties:",
                ["ItemBrowser.PlanToUse"] = "Plan to Use",

                // Blueprint Browser
                ["BlueprintBrowser.SearchBlueprints"] = "Search blueprints...",
                ["BlueprintBrowser.CanBuildOnly"] = "Can Build Only",
                ["BlueprintBrowser.Produces"] = "Produces:",
                ["BlueprintBrowser.ProductionTime"] = "Production Time:",
                ["BlueprintBrowser.Materials"] = "Materials:",
                ["BlueprintBrowser.RequiredSkills"] = "Required Skills:",
                ["BlueprintBrowser.PlanSkills"] = "Plan Skills",

                // Plan Queue Headers
                ["Plan.Rank"] = "R",
                ["Plan.SPPerHour"] = "SP/HR",
                ["Plan.Level"] = "LEVEL",

                // Queue view
                ["Queue.NoSkillsInTraining"] = "No skills in training",
                ["Queue.SkillsInQueue"] = "Skills in queue",
                ["Queue.EndsIn"] = "Ends in",
                ["Queue.EndsOn"] = "Ends on",
                ["Queue.ExportCsv"] = "Export CSV",

                // ESI Endpoints
                ["Endpoints.Title"] = "ESI Endpoints",
                ["Endpoints.Description"] = "Choose which data to fetch for this character.",
                ["Endpoints.EnableAll"] = "Enable All",
                ["Endpoints.DisableAll"] = "Disable All",

                // Sidebar
                ["Sidebar.Add"] = "Add",
                ["Sidebar.AddSkills"] = "Add Skills",
                ["Sidebar.Plan"] = "Plan",

                // List Views — Common toolbar/UI strings
                ["ListView.GroupBy"] = "Group by:",
                ["ListView.Filter"] = "Filter:",
                ["ListView.SearchAssets"] = "Search assets...",
                ["ListView.SearchContracts"] = "Search contracts...",
                ["ListView.SearchMarketOrders"] = "Search market orders...",
                ["ListView.SearchContacts"] = "Search contacts...",
                ["ListView.SearchMail"] = "Search mail...",
                ["ListView.SearchKills"] = "Search kills...",
                ["ListView.SearchStandings"] = "Search standings...",
                ["ListView.SearchWalletJournal"] = "Search journal...",
                ["ListView.SearchNotifications"] = "Search notifications...",

                // List Views — Enable prompts
                ["ListView.EnableToFetch"] = "Enable this to start fetching data from EVE Online.",
                ["ListView.ScopeNotAuthorized"] = "ESI scope not authorized",
                ["ListView.ScopeNotAuthorizedDesc"] = "This character was authenticated without the required scope.\nRe-authenticate from File → Manage Characters to enable this feature.",
                ["ListView.EnableAssets"] = "Asset Monitoring is not enabled for this character.",
                ["ListView.EnableAssetBtn"] = "Enable Asset Monitoring",
                ["ListView.EnableMarketOrders"] = "Market Order Tracking is not enabled for this character.",
                ["ListView.EnableMarketOrderBtn"] = "Enable Market Order Tracking",
                ["ListView.EnableContracts"] = "Contract Tracking is not enabled for this character.",
                ["ListView.EnableContractBtn"] = "Enable Contract Tracking",
                ["ListView.EnableIndustry"] = "Industry Job Tracking is not enabled for this character.",
                ["ListView.EnableIndustryBtn"] = "Enable Industry Job Tracking",
                ["ListView.EnableMail"] = "Mail Messages is not enabled for this character.",
                ["ListView.EnableMailBtn"] = "Enable Mail Messages",
                ["ListView.EnableNotifications"] = "Notifications is not enabled for this character.",
                ["ListView.EnableNotificationsBtn"] = "Enable Notifications",
                ["ListView.EnableKillLog"] = "Kill Log is not enabled for this character.",
                ["ListView.EnableKillLogBtn"] = "Enable Kill Log",
                ["ListView.EnablePlanetary"] = "Planetary Interaction is not enabled for this character.",
                ["ListView.EnablePlanetaryBtn"] = "Enable Planetary Interaction",
                ["ListView.EnableResearch"] = "Research Points is not enabled for this character.",
                ["ListView.EnableResearchBtn"] = "Enable Research Points",
                ["ListView.EnableContacts"] = "Contacts is not enabled for this character.",
                ["ListView.EnableContactsBtn"] = "Enable Contacts",
                ["ListView.EnableStandings"] = "Standings is not enabled for this character.",
                ["ListView.EnableStandingsBtn"] = "Enable Standings",
                ["ListView.EnableMedals"] = "Medals is not enabled for this character.",
                ["ListView.EnableMedalsBtn"] = "Enable Medals",
                ["ListView.EnableLP"] = "Loyalty Points is not enabled for this character.",
                ["ListView.EnableLPBtn"] = "Enable Loyalty Points",
                ["ListView.EnableEmployment"] = "Employment History is not enabled for this character.",
                ["ListView.EnableEmploymentBtn"] = "Enable Employment History",
                ["ListView.EnableFW"] = "Factional Warfare is not enabled for this character.",
                ["ListView.EnableFWBtn"] = "Enable Factional Warfare",
                ["ListView.EnableWalletJournal"] = "Wallet Journal is not enabled for this character.",
                ["ListView.EnableWalletJournalBtn"] = "Enable Wallet Journal",
                ["ListView.EnableWalletTx"] = "Wallet Transactions is not enabled for this character.",
                ["ListView.EnableWalletTxBtn"] = "Enable Wallet Transactions",

                // List Views — Empty states
                ["ListView.NoAssets"] = "No assets found",
                ["ListView.NoAssetsDesc"] = "Assets will appear here once data is fetched.",
                ["ListView.NoContracts"] = "No Contracts",
                ["ListView.NoContractsDesc"] = "No contracts match the current filters. Try adjusting your search or filter settings.",

                // List Views — Grouping options
                ["ListView.Location"] = "Location",
                ["ListView.Region"] = "Region",
                ["ListView.Category"] = "Category",
                ["ListView.Container"] = "Container",
                ["ListView.NoGrouping"] = "No Grouping",
                ["ListView.Status"] = "Status",
                ["ListView.ContractType"] = "Contract Type",
                ["ListView.IssueDate"] = "Issue Date",
                ["ListView.StartLocation"] = "Start Location",
                ["ListView.HideInactive"] = "Hide Inactive",
                ["ListView.All"] = "All",
                ["ListView.Character"] = "Character",
                ["ListView.Corporation"] = "Corporation",

                // DataGrid column headers — Market Orders
                ["Column.Item"] = "Item",
                ["Column.Station"] = "Station",
                ["Column.UnitPrice"] = "Unit Price",
                ["Column.TotalPrice"] = "Total Price",
                ["Column.InitialVol"] = "Initial Vol",
                ["Column.Remaining"] = "Remaining",
                ["Column.Duration"] = "Duration",
                ["Column.Issued"] = "Issued",
                ["Column.Expires"] = "Expires",

                // DataGrid column headers — Contracts
                ["Column.Contract"] = "Contract",
                ["Column.Type"] = "Type",
                ["Column.Status"] = "Status",
                ["Column.Issuer"] = "Issuer",
                ["Column.Assignee"] = "Assignee",
                ["Column.Price"] = "Price",

                // DataGrid column headers — Industry Jobs
                ["Column.Activity"] = "Activity",
                ["Column.State"] = "State",
                ["Column.Blueprint"] = "Blueprint",
                ["Column.Output"] = "Output",
                ["Column.Location"] = "Location",
                ["Column.Runs"] = "Runs",
                ["Column.Cost"] = "Cost",
                ["Column.InstallDate"] = "Install Date",
                ["Column.EndDate"] = "End Date",

                // DataGrid column headers — Research Points
                ["Column.Agent"] = "Agent",
                ["Column.Level"] = "Level",
                ["Column.Field"] = "Field",
                ["Column.CurrentRP"] = "Current RP",
                ["Column.PointsPerDay"] = "Points/Day",
                ["Column.StartDate"] = "Start Date",

                // DataGrid column headers — Planetary
                ["Column.Planet"] = "Planet",
                ["Column.PlanetType"] = "Planet Type",
                ["Column.PinType"] = "Pin Type",
                ["Column.Content"] = "Content",
                ["Column.Quantity"] = "Quantity",
                ["Column.Expiry"] = "Expiry",
                ["Column.SolarSystem"] = "Solar System",

                // DataGrid column headers — Wallet Transactions
                ["Column.Date"] = "Date",
                ["Column.Credit"] = "Credit",
                ["Column.Client"] = "Client",

                // Context menu items
                ["ContextMenu.CopyItemName"] = "Copy Item Name",
                ["ContextMenu.CopyStation"] = "Copy Station",
                ["ContextMenu.ExportCsv"] = "Export to CSV...",
                ["ContextMenu.CopyContractInfo"] = "Copy Contract Info",
                ["ContextMenu.CopyIssuer"] = "Copy Issuer",
                ["ContextMenu.CopyBlueprintName"] = "Copy Blueprint Name",
                ["ContextMenu.CopyOutput"] = "Copy Output",
                ["ContextMenu.CopyAgent"] = "Copy Agent",
                ["ContextMenu.CopyField"] = "Copy Field",
                ["ContextMenu.CopyPlanet"] = "Copy Planet",
                ["ContextMenu.CopySolarSystem"] = "Copy Solar System",
                ["ContextMenu.CopyClient"] = "Copy Client",

                // Plan Sidebar strings
                ["Plan.AlreadyOptimal"] = "Already optimal",
                ["Plan.Analyzing"] = "Analyzing…",
                ["Plan.Current"] = "Current:",
                ["Plan.RemapTo"] = "To save {0}, remap to:",
                ["Plan.ChangeAttrsTo"] = "Change attributes to:",
                ["Plan.RemapAvailable"] = "Remap available",
                ["Plan.RemapInDays"] = "Remap in {0} days",
            });
        }

        private static void RegisterChineseSimplified()
        {
            Register("zh-CN", new Dictionary<string, string>
            {
                // Main Window — 主窗口
                ["Menu.File"] = "文件",
                ["Menu.Plans"] = "训练计划",
                ["Menu.Tools"] = "工具",
                ["Menu.Help"] = "帮助",
                ["Menu.File.AddCharacter"] = "添加角色",
                ["Menu.File.ManageGroups"] = "管理分组",
                ["Menu.File.Settings"] = "设置",
                ["Menu.File.Exit"] = "退出",
                ["Menu.Plans.NewPlan"] = "新建计划",
                ["Menu.Plans.ManagePlans"] = "管理计划",
                ["Menu.Plans.ImportPlan"] = "从文件导入计划",
                ["Menu.Plans.CreateFromQueue"] = "从训练队列创建计划",
                ["Menu.Tools.CharComparison"] = "角色对比",
                ["Menu.Tools.SkillFarm"] = "技能农场面板",
                ["Menu.Tools.DoctrineDesigner"] = "教义设计器",
                ["Menu.Tools.SkillVisualization"] = "技能可视化",
                ["Menu.Tools.ClearCache"] = "清除缓存",
                ["Menu.Help.About"] = "关于EveLens",
                ["Menu.Help.KnownProblems"] = "已知问题",
                ["Menu.Help.CheckUpdates"] = "检查更新",
                ["Menu.Help.UserGuide"] = "用户指南",
                ["Menu.Help.ReportIssue"] = "报告问题",
                ["Menu.Help.Shortcuts"] = "键盘快捷键",
                ["Menu.File.CreateBlank"] = "创建空白角色",
                ["Menu.File.ManageChars"] = "管理角色",
                ["Menu.File.RestoreSettings"] = "恢复设置",
                ["Menu.File.SaveSettings"] = "保存设置",
                ["Menu.File.ResetSettings"] = "重置设置",

                // Tabs — 标签页
                ["Tab.Skills"] = "技能",
                ["Tab.Queue"] = "训练队列",
                ["Tab.Clones"] = "克隆体",
                ["Tab.Employment"] = "雇佣记录",
                ["Tab.Assets"] = "资产",
                ["Tab.MarketOrders"] = "市场订单",
                ["Tab.Contracts"] = "合同",
                ["Tab.IndustryJobs"] = "工业作业",
                ["Tab.Wallet"] = "钱包",
                ["Tab.Mail"] = "邮件",
                ["Tab.Notifications"] = "通知",
                ["Tab.KillLog"] = "击毁记录",
                ["Tab.Planetary"] = "行星开发",
                ["Tab.Research"] = "研究",

                // Common Actions — 常用操作
                ["Action.CollapseAll"] = "全部收起",
                ["Action.ExpandAll"] = "全部展开",
                ["Action.TrainedOnly"] = "仅已训练",
                ["Action.ExportCsv"] = "导出CSV",
                ["Action.AllSkills"] = "全部技能",
                ["Action.Search"] = "搜索…",
                ["Action.Save"] = "保存",
                ["Action.Cancel"] = "取消",
                ["Action.Close"] = "关闭",
                ["Action.Apply"] = "应用",
                ["Action.Create"] = "创建",
                ["Action.Delete"] = "删除",
                ["Action.Refresh"] = "刷新",
                ["Action.AddCharacter"] = "+ 添加角色",
                ["Action.AddSkill"] = "+ 添加技能",

                // EVE Game Terms — EVE游戏术语（CCP官方中文翻译）
                ["Eve.SkillPoints"] = "技能点",
                ["Eve.SP"] = "技能点",
                ["Eve.SPPerHour"] = "技能点/时",
                ["Eve.ISK"] = "星币",
                ["Eve.Omega"] = "欧米伽",
                ["Eve.Alpha"] = "阿尔法",
                ["Eve.Intelligence"] = "智力",
                ["Eve.Memory"] = "记忆",
                ["Eve.Perception"] = "感知",
                ["Eve.Willpower"] = "毅力",
                ["Eve.Charisma"] = "魅力",
                ["Eve.Rank"] = "等级",
                ["Eve.Level"] = "级",
                ["Eve.Training"] = "训练中",
                ["Eve.Queued"] = "排队中",
                ["Eve.Completed"] = "已完成",
                ["Eve.AlreadyTrained"] = "已训练",

                // Plan Editor — 计划编辑器
                ["Plan.Skill"] = "技能",
                ["Plan.Time"] = "时间",
                ["Plan.Primary"] = "主属性",
                ["Plan.Secondary"] = "副属性",
                ["Plan.GroupByAttr"] = "按属性分组",
                ["Plan.GroupedByAttr"] = "已按属性分组 ✓",
                ["Plan.QueueReordered"] = "队列已重新排序",
                ["Plan.DragToReorder"] = "拖动以重新排序 · 双击查看详情",

                // Skill Farm — 技能农场
                ["SkillFarm.Title"] = "技能农场面板",
                ["SkillFarm.AddAll"] = "+ 添加全部符合条件",
                ["SkillFarm.Character"] = "角色",
                ["SkillFarm.Injectors"] = "注入器",
                ["SkillFarm.ExtractorCost"] = "提取器成本",
                ["SkillFarm.Revenue"] = "收入",
                ["SkillFarm.NetProfit"] = "净利润",
                ["SkillFarm.Status"] = "状态",

                // Doctrine Designer — 教义设计器
                ["Doctrine.Title"] = "教义设计器",
                ["Doctrine.Doctrines"] = "教义",
                ["Doctrine.NewDoctrine"] = "+ 新建教义",
                ["Doctrine.ImportFromPlan"] = "从计划导入",
                ["Doctrine.CreatePlan"] = "生成计划",
                ["Doctrine.PlanCreated"] = "计划已创建！",
                ["Doctrine.CreatePlansForAll"] = "为所有角色生成计划",
                ["Doctrine.NoDoctrines"] = "暂无教义。\n点击「+ 新建教义」创建，\n或「从计划导入」使用现有计划。",
                ["Doctrine.SelectDoctrine"] = "从侧边栏选择一个教义以查看技能对比。",
                ["Doctrine.NoCharacters"] = "暂无分配角色。\n点击「+ 添加角色」将角色分配到此教义。",
                ["Doctrine.AllTrained"] = "全部已训练！",

                // Settings — 设置窗口
                ["Settings.Title"] = "设置",
                ["Settings.WindowTitle"] = "EveLens 设置",
                ["Settings.SearchWatermark"] = "搜索设置…",

                // Settings — 侧边栏
                ["Settings.Appearance"] = "外观",
                ["Settings.AppearanceSub"] = "主题、颜色、兼容性",
                ["Settings.Window"] = "窗口",
                ["Settings.WindowSub"] = "托盘图标、关闭行为",
                ["Settings.Notifications"] = "通知",
                ["Settings.NotificationsSub"] = "提醒、邮件、日历",
                ["Settings.Data"] = "数据",
                ["Settings.DataSub"] = "更新、市场价格",
                ["Settings.Network"] = "网络",
                ["Settings.NetworkSub"] = "代理、SSO凭据",
                ["Settings.ESI"] = "ESI",
                ["Settings.ESISub"] = "API权限范围",

                // Settings — 外观
                ["Settings.AppearanceDesc"] = "自定义EveLens的外观和风格。",
                ["Settings.Theme"] = "主题",
                ["Settings.ThemeDesc"] = "选择应用程序的视觉主题。",
                ["Settings.RestartRequired"] = "需要重启以应用",
                ["Settings.RestartNow"] = "立即重启",
                ["Settings.FontSize"] = "字体大小",
                ["Settings.FontSizeDesc"] = "缩放应用程序中的所有文本（80%-150%）。",
                ["Settings.Language"] = "语言",
                ["Settings.LanguageDesc"] = "游戏术语使用CCP官方中文翻译。",
                ["Settings.LanguageNote"] = "游戏术语使用CCP官方中文翻译。社区翻译——欢迎帮助改进！",
                ["Settings.SafeForWork"] = "安全工作模式",
                ["Settings.SafeForWorkDesc"] = "隐藏角色头像和识别颜色。",
                ["Settings.CustomBrowser"] = "浏览器",
                ["Settings.CustomBrowserDesc"] = "选择用于打开链接的浏览器。默认使用系统设置。",
                ["Settings.DefaultBrowser"] = "系统默认",
                ["Settings.OpenDataDir"] = "打开数据目录",

                // Settings — 窗口行为
                ["Settings.WindowBehavior"] = "窗口行为",
                ["Settings.WindowBehaviorDesc"] = "配置主窗口和系统托盘的行为。",
                ["Settings.MinimizeToTray"] = "最小化到托盘",
                ["Settings.MinimizeToTrayDesc"] = "启用后，关闭窗口将最小化到系统托盘而不是退出。点击托盘图标恢复。",
                ["Settings.TrayTooltipDisplay"] = "托盘提示显示",
                ["Settings.TrayTooltipDesc"] = "选择悬停在系统托盘图标上时显示的信息。",
                ["Settings.TrayCountAndNext"] = "训练数量 + 下一个完成",
                ["Settings.TrayCountOnly"] = "仅训练数量",
                ["Settings.TrayNextOnly"] = "仅下一个完成",

                // Settings — 通知
                ["Settings.NotificationsDesc"] = "配置技能训练和事件的提醒和通知。",
                ["Settings.OsNotifications"] = "系统通知",
                ["Settings.OsNotificationsDesc"] = "显示技能完成和提醒的系统通知。",
                ["Settings.PlaySound"] = "播放声音",
                ["Settings.PlaySoundDesc"] = "技能训练完成时播放声音。",
                ["Settings.EmailAlerts"] = "邮件提醒",
                ["Settings.EmailAlertsDesc"] = "技能训练完成时发送邮件。",
                ["Settings.Provider"] = "提供商",
                ["Settings.SmtpServer"] = "SMTP服务器",
                ["Settings.Port"] = "端口",
                ["Settings.SslTls"] = "SSL/TLS",
                ["Settings.SslTlsDesc"] = "要求加密连接。",
                ["Settings.Authentication"] = "认证",
                ["Settings.AuthenticationDesc"] = "服务器需要用户名和密码。",
                ["Settings.Username"] = "用户名",
                ["Settings.Password"] = "密码",
                ["Settings.From"] = "发件人",
                ["Settings.To"] = "收件人",
                ["Settings.ShortFormat"] = "简短格式",
                ["Settings.ShortFormatDesc"] = "使用紧凑的邮件格式。",
                ["Settings.ExternalCalendar"] = "外部日历",
                ["Settings.ExternalCalendarDesc"] = "将技能训练计划同步到外部日历。",
                ["Settings.GoogleCalendar"] = "Google日历",
                ["Settings.MicrosoftOutlook"] = "Microsoft Outlook",
                ["Settings.CalendarName"] = "日历名称",
                ["Settings.Reminder"] = "提醒",
                ["Settings.UseDefaultCalendar"] = "使用默认日历",
                ["Settings.CalendarPath"] = "日历路径",
                ["Settings.EnableReminders"] = "启用提醒",
                ["Settings.IntervalMin"] = "间隔（分钟）",
                ["Settings.AltRemindingTimes"] = "使用替代提醒时间",
                ["Settings.Early"] = "早",
                ["Settings.Late"] = "晚",
                ["Settings.LastQueuedOnly"] = "仅导出最后排队的技能",

                // Settings — 数据
                ["Settings.DataUpdates"] = "数据与更新",
                ["Settings.DataUpdatesDesc"] = "管理自动更新和市场数据来源。",
                ["Settings.CheckForUpdates"] = "检查更新",
                ["Settings.CheckForUpdatesDesc"] = "启动时自动检查新版本。",
                ["Settings.ClockSync"] = "时钟同步",
                ["Settings.ClockSyncDesc"] = "启动时验证系统时钟是否同步。",
                ["Settings.MarketPriceProvider"] = "市场价格来源",
                ["Settings.MarketPriceProviderDesc"] = "市场价格查询来源。",

                // Settings — 网络
                ["Settings.NetworkDesc"] = "配置代理和SSO凭据。",
                ["Settings.HttpProxy"] = "HTTP代理",
                ["Settings.HttpProxyDesc"] = "通过自定义代理服务器路由流量。",
                ["Settings.Host"] = "主机",
                ["Settings.SsoCredentials"] = "SSO凭据",
                ["Settings.SsoCredentialsDesc"] = "覆盖内置的EveLens SSO客户端凭据。",
                ["Settings.ClientId"] = "客户端ID",
                ["Settings.ClientSecret"] = "客户端密钥",
                ["Settings.UseDefault"] = "使用默认",

                // Settings — ESI
                ["Settings.EsiScopes"] = "ESI权限范围",
                ["Settings.EsiScopesDesc"] = "控制认证时EveLens请求的API权限范围。",
                ["Settings.ScopePreset"] = "权限预设",
                ["Settings.ScopePresetDesc"] = "选择预定义的API权限集。",
                ["Settings.CustomizeScopes"] = "自定义权限范围…",
                ["Settings.EsiChangesNote"] = "更改在下次认证角色时生效。",
                ["Settings.CharacterScopes"] = "角色权限范围",
                ["Settings.NoCharsAuthenticated"] = "尚未认证任何角色。",
                ["Settings.EsiReauthNote"] = "与所选预设不匹配的角色可以重新认证以更新其权限范围。",
                ["Settings.Reauthenticate"] = "重新认证",
                ["Settings.Restart"] = "重启",
                ["Settings.RestartEveLens"] = "重启EveLens",
                ["Settings.RestartMessage"] = "EveLens将重启以应用新主题。\n请保存正在进行的工作（如正在编辑的技能计划）。",

                // Status / Empty States — 状态
                ["Status.Paused"] = "已暂停",
                ["Status.Training"] = "训练中",
                ["Status.Online"] = "在线",
                ["Status.SkillsInQueue"] = "队列中的技能",
                ["Status.Trained"] = "已训练",
                ["Status.Of"] = "/",
                ["Status.Skills"] = "个技能",
                ["Status.TotalSP"] = "总技能点",
                ["Status.Remaining"] = "剩余",

                // Character Header — 角色头部
                ["Header.Corporation"] = "军团",
                ["Header.Alliance"] = "联盟",
                ["Header.LocatedIn"] = "位于",
                ["Header.DockedAt"] = "停靠在",
                ["Header.InSpace"] = "在太空中",
                ["Header.Skills"] = "技能",
                ["Header.SP"] = "技能点",
                ["Header.FreeSP"] = "未分配技能点",
                ["Header.TotalSP"] = "总技能点",
                ["Header.Remaps"] = "重映射",
                ["Header.AutoDetect"] = "自动检测",
                ["Header.AlphaOverride"] = "Alpha覆盖",
                ["Header.OmegaOverride"] = "Omega覆盖",

                // ESI
                ["ESI.NextRefresh"] = "下次刷新",
                ["ESI.Fetching"] = "获取中…",
                ["ESI.Idle"] = "ESI：空闲",
                ["ESI.Refreshing"] = "ESI：刷新中…",
                ["ESI.LastRefresh"] = "上次刷新",

                // Splash — 启动画面
                ["Splash.Tagline"] = "EVE Online 角色情报工具",
                ["Splash.LoadingGameData"] = "正在加载游戏数据…",

                // Skills level filter strip — 技能等级筛选
                ["Skills.AllSkills"] = "所有技能",
                ["Skills.AllTrained"] = "全部已训练",
                ["Skills.LevelV"] = "V级",
                ["Skills.LevelIV"] = "IV级",
                ["Skills.LevelIII"] = "III级",
                ["Skills.LevelII"] = "II级",
                ["Skills.LevelI"] = "I级",
                ["Skills.Injected"] = "已注入",
                ["Skills.Untrained"] = "未训练",

                // Character header labels — 角色头部标签
                ["Header.SecurityStatus"] = "安全等级",
                ["Header.ActiveShip"] = "当前舰船",
                ["Header.Unknown"] = "未知",
                ["Header.KnownSkills"] = "已知技能",
                ["Header.BonusRemaps"] = "奖励重置次数",

                // Missing character monitor tabs — 缺失的角色监控标签
                ["Tab.Standings"] = "声望",
                ["Tab.Contacts"] = "联系人",
                ["Tab.Medals"] = "勋章",
                ["Tab.LP"] = "忠诚点",
                ["Tab.Transactions"] = "交易记录",

                // Plan Editor Window — 计划编辑器窗口
                ["PlanEditor.Title"] = "计划编辑器",
                ["PlanEditor.PlanTab"] = "计划",
                ["PlanEditor.SkillsTab"] = "技能",
                ["PlanEditor.ShipsTab"] = "舰船",
                ["PlanEditor.ItemsTab"] = "物品",
                ["PlanEditor.BlueprintsTab"] = "蓝图",
                ["PlanEditor.SearchSkills"] = "搜索技能…",
                ["PlanEditor.ClearPlan"] = "清除计划",
                ["PlanEditor.ImportFit"] = "导入配装",
                ["PlanEditor.Export"] = "导出",
                ["PlanEditor.AddSkills"] = "+ 添加技能",
                ["PlanEditor.CopyPlan"] = "复制技能计划",
                ["PlanEditor.Done"] = "已完成",
                ["PlanEditor.SkillsToTrain"] = "个技能待训练",
                ["PlanEditor.SkillsRemaining"] = "个技能剩余",
                ["PlanEditor.OptimizePlan"] = "优化计划",
                ["PlanEditor.Remap"] = "重置属性",
                ["PlanEditor.AvailableNow"] = "现在可用",
                ["PlanEditor.Bonus"] = "奖励",
                ["PlanEditor.TrainingTime"] = "训练时间",
                ["PlanEditor.Unique"] = "唯一",
                ["PlanEditor.Finishes"] = "完成于",
                ["PlanEditor.Books"] = "技能书",
                ["PlanEditor.Skills"] = "个技能",
                ["PlanEditor.Of"] = "/",
                ["PlanEditor.Remaining"] = "剩余",
                ["PlanEditor.NextIn"] = "下次于",
                ["PlanEditor.Injectors"] = "注入器",
                ["PlanEditor.CopyToClipboard"] = "复制到剪贴板",
                ["PlanEditor.SaveToFile"] = "保存到文件…",
                ["PlanEditor.FromClipboard"] = "从剪贴板（EFT/XML/DNA）",
                ["PlanEditor.FromFile"] = "从文件…",
                ["PlanEditor.PlanTo"] = "训练到",
                ["PlanEditor.Back"] = "返回",

                // Skill Browser — 技能浏览器
                ["PlanEditor.AllAttributes"] = "所有属性",
                ["PlanEditor.AllSkills"] = "所有技能",
                ["PlanEditor.Trained"] = "已训练",
                ["PlanEditor.HavePrereqs"] = "有前置技能",
                ["PlanEditor.Untrained"] = "未训练",
                ["PlanEditor.Collapse"] = "收起",
                ["PlanEditor.Expand"] = "展开",
                ["PlanEditor.NotTrained"] = "未训练",
                ["PlanEditor.Rank"] = "等级：",
                ["PlanEditor.Primary"] = "主属性：",
                ["PlanEditor.Secondary"] = "副属性：",
                ["PlanEditor.TrainingTimeLabel"] = "训练时间：",
                ["PlanEditor.Prerequisites"] = "前置技能：",

                // Ship Browser — 舰船浏览器
                ["ShipBrowser.SearchShips"] = "搜索舰船…",
                ["ShipBrowser.CanFlyOnly"] = "仅可驾驶",
                ["ShipBrowser.RequiredSkills"] = "所需技能：",
                ["ShipBrowser.Properties"] = "属性：",
                ["ShipBrowser.PlanToFly"] = "添加到训练计划",
                ["ShipBrowser.Race"] = "种族：",
                ["ShipBrowser.Amarr"] = "艾玛",
                ["ShipBrowser.Caldari"] = "加达里",
                ["ShipBrowser.Gallente"] = "盖伦特",
                ["ShipBrowser.Minmatar"] = "米玛塔尔",

                // Item Browser — 物品浏览器
                ["ItemBrowser.SearchItems"] = "搜索物品…",
                ["ItemBrowser.CanUseOnly"] = "仅可使用",
                ["ItemBrowser.CollapseAll"] = "全部收起",
                ["ItemBrowser.Slot"] = "槽位：",
                ["ItemBrowser.RequiredSkills"] = "所需技能：",
                ["ItemBrowser.Properties"] = "属性：",
                ["ItemBrowser.PlanToUse"] = "添加到训练计划",

                // Blueprint Browser — 蓝图浏览器
                ["BlueprintBrowser.SearchBlueprints"] = "搜索蓝图…",
                ["BlueprintBrowser.CanBuildOnly"] = "仅可制造",
                ["BlueprintBrowser.Produces"] = "产出：",
                ["BlueprintBrowser.ProductionTime"] = "制造时间：",
                ["BlueprintBrowser.Materials"] = "材料：",
                ["BlueprintBrowser.RequiredSkills"] = "所需技能：",
                ["BlueprintBrowser.PlanSkills"] = "添加到训练计划",

                // Plan Queue Headers — 计划队列表头
                ["Plan.Rank"] = "等级",
                ["Plan.SPPerHour"] = "技能点/时",
                ["Plan.Level"] = "级别",

                // Queue view — 训练队列
                ["Queue.NoSkillsInTraining"] = "没有正在训练的技能",
                ["Queue.SkillsInQueue"] = "队列中的技能",
                ["Queue.EndsIn"] = "结束于",
                ["Queue.EndsOn"] = "结束日期",
                ["Queue.ExportCsv"] = "导出CSV",

                // ESI Endpoints — ESI端点
                ["Endpoints.Title"] = "ESI端点",
                ["Endpoints.Description"] = "选择要为此角色获取的数据。",
                ["Endpoints.EnableAll"] = "全部启用",
                ["Endpoints.DisableAll"] = "全部禁用",

                // Sidebar — 侧边栏
                ["Sidebar.Add"] = "添加",
                ["Sidebar.AddSkills"] = "添加技能",
                ["Sidebar.Plan"] = "计划",

                // List Views — 列表视图通用工具栏
                ["ListView.GroupBy"] = "分组方式：",
                ["ListView.Filter"] = "筛选：",
                ["ListView.SearchAssets"] = "搜索资产…",
                ["ListView.SearchContracts"] = "搜索合同…",
                ["ListView.SearchMarketOrders"] = "搜索市场订单…",
                ["ListView.SearchContacts"] = "搜索联系人…",
                ["ListView.SearchMail"] = "搜索邮件…",
                ["ListView.SearchKills"] = "搜索击毁记录…",
                ["ListView.SearchStandings"] = "搜索声望…",
                ["ListView.SearchWalletJournal"] = "搜索日志…",
                ["ListView.SearchNotifications"] = "搜索通知…",

                // List Views — 启用提示
                ["ListView.EnableToFetch"] = "启用后即可从EVE Online获取数据。",
                ["ListView.ScopeNotAuthorized"] = "ESI权限未授权",
                ["ListView.ScopeNotAuthorizedDesc"] = "此角色认证时未包含所需权限范围。\n从 文件 → 管理角色 重新认证以启用此功能。",
                ["ListView.EnableAssets"] = "此角色未启用资产监控。",
                ["ListView.EnableAssetBtn"] = "启用资产监控",
                ["ListView.EnableMarketOrders"] = "此角色未启用市场订单追踪。",
                ["ListView.EnableMarketOrderBtn"] = "启用市场订单追踪",
                ["ListView.EnableContracts"] = "此角色未启用合同追踪。",
                ["ListView.EnableContractBtn"] = "启用合同追踪",
                ["ListView.EnableIndustry"] = "此角色未启用工业作业追踪。",
                ["ListView.EnableIndustryBtn"] = "启用工业作业追踪",
                ["ListView.EnableMail"] = "此角色未启用邮件。",
                ["ListView.EnableMailBtn"] = "启用邮件",
                ["ListView.EnableNotifications"] = "此角色未启用通知。",
                ["ListView.EnableNotificationsBtn"] = "启用通知",
                ["ListView.EnableKillLog"] = "此角色未启用击毁记录。",
                ["ListView.EnableKillLogBtn"] = "启用击毁记录",
                ["ListView.EnablePlanetary"] = "此角色未启用行星开发。",
                ["ListView.EnablePlanetaryBtn"] = "启用行星开发",
                ["ListView.EnableResearch"] = "此角色未启用研究点数。",
                ["ListView.EnableResearchBtn"] = "启用研究点数",
                ["ListView.EnableContacts"] = "此角色未启用联系人。",
                ["ListView.EnableContactsBtn"] = "启用联系人",
                ["ListView.EnableStandings"] = "此角色未启用声望。",
                ["ListView.EnableStandingsBtn"] = "启用声望",
                ["ListView.EnableMedals"] = "此角色未启用勋章。",
                ["ListView.EnableMedalsBtn"] = "启用勋章",
                ["ListView.EnableLP"] = "此角色未启用忠诚点。",
                ["ListView.EnableLPBtn"] = "启用忠诚点",
                ["ListView.EnableEmployment"] = "此角色未启用雇佣记录。",
                ["ListView.EnableEmploymentBtn"] = "启用雇佣记录",
                ["ListView.EnableFW"] = "此角色未启用势力战争。",
                ["ListView.EnableFWBtn"] = "启用势力战争",
                ["ListView.EnableWalletJournal"] = "此角色未启用钱包日志。",
                ["ListView.EnableWalletJournalBtn"] = "启用钱包日志",
                ["ListView.EnableWalletTx"] = "此角色未启用钱包交易。",
                ["ListView.EnableWalletTxBtn"] = "启用钱包交易",

                // List Views — 空状态
                ["ListView.NoAssets"] = "未找到资产",
                ["ListView.NoAssetsDesc"] = "数据获取后资产将显示在此处。",
                ["ListView.NoContracts"] = "无合同",
                ["ListView.NoContractsDesc"] = "没有合同匹配当前筛选条件。请尝试调整搜索或筛选设置。",

                // List Views — 分组选项
                ["ListView.Location"] = "位置",
                ["ListView.Region"] = "星域",
                ["ListView.Category"] = "分类",
                ["ListView.Container"] = "容器",
                ["ListView.NoGrouping"] = "不分组",
                ["ListView.Status"] = "状态",
                ["ListView.ContractType"] = "合同类型",
                ["ListView.IssueDate"] = "签发日期",
                ["ListView.StartLocation"] = "起始位置",
                ["ListView.HideInactive"] = "隐藏非活跃",
                ["ListView.All"] = "全部",
                ["ListView.Character"] = "角色",
                ["ListView.Corporation"] = "军团",

                // DataGrid列标题 — 市场订单
                ["Column.Item"] = "物品",
                ["Column.Station"] = "空间站",
                ["Column.UnitPrice"] = "单价",
                ["Column.TotalPrice"] = "总价",
                ["Column.InitialVol"] = "初始数量",
                ["Column.Remaining"] = "剩余",
                ["Column.Duration"] = "持续时间",
                ["Column.Issued"] = "签发",
                ["Column.Expires"] = "到期",

                // DataGrid列标题 — 合同
                ["Column.Contract"] = "合同",
                ["Column.Type"] = "类型",
                ["Column.Status"] = "状态",
                ["Column.Issuer"] = "签发人",
                ["Column.Assignee"] = "受让人",
                ["Column.Price"] = "价格",

                // DataGrid列标题 — 工业作业
                ["Column.Activity"] = "活动",
                ["Column.State"] = "状态",
                ["Column.Blueprint"] = "蓝图",
                ["Column.Output"] = "产出",
                ["Column.Location"] = "位置",
                ["Column.Runs"] = "次数",
                ["Column.Cost"] = "费用",
                ["Column.InstallDate"] = "安装日期",
                ["Column.EndDate"] = "结束日期",

                // DataGrid列标题 — 研究点数
                ["Column.Agent"] = "代理人",
                ["Column.Level"] = "等级",
                ["Column.Field"] = "领域",
                ["Column.CurrentRP"] = "当前研究点",
                ["Column.PointsPerDay"] = "每日点数",
                ["Column.StartDate"] = "开始日期",

                // DataGrid列标题 — 行星开发
                ["Column.Planet"] = "行星",
                ["Column.PlanetType"] = "行星类型",
                ["Column.PinType"] = "设施类型",
                ["Column.Content"] = "内容",
                ["Column.Quantity"] = "数量",
                ["Column.Expiry"] = "到期",
                ["Column.SolarSystem"] = "星系",

                // DataGrid列标题 — 钱包交易
                ["Column.Date"] = "日期",
                ["Column.Credit"] = "金额",
                ["Column.Client"] = "客户",

                // 右键菜单
                ["ContextMenu.CopyItemName"] = "复制物品名称",
                ["ContextMenu.CopyStation"] = "复制空间站",
                ["ContextMenu.ExportCsv"] = "导出CSV…",
                ["ContextMenu.CopyContractInfo"] = "复制合同信息",
                ["ContextMenu.CopyIssuer"] = "复制签发人",
                ["ContextMenu.CopyBlueprintName"] = "复制蓝图名称",
                ["ContextMenu.CopyOutput"] = "复制产出",
                ["ContextMenu.CopyAgent"] = "复制代理人",
                ["ContextMenu.CopyField"] = "复制领域",
                ["ContextMenu.CopyPlanet"] = "复制行星",
                ["ContextMenu.CopySolarSystem"] = "复制星系",
                ["ContextMenu.CopyClient"] = "复制客户",

                // Plan Sidebar strings — 计划侧边栏
                ["Plan.AlreadyOptimal"] = "已经是最优",
                ["Plan.Analyzing"] = "分析中…",
                ["Plan.Current"] = "当前：",
                ["Plan.RemapTo"] = "重映射后可节省 {0}：",
                ["Plan.ChangeAttrsTo"] = "将属性更改为：",
                ["Plan.RemapAvailable"] = "可重映射",
                ["Plan.RemapInDays"] = "{0}天后可重映射",
            });
        }
    }
}
