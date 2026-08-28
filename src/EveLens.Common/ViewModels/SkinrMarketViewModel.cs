// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Constants;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Hub;
using EveLens.Common.Service;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// The Paragon Hub discovery pane's data: the public listing feed grouped into one
    /// entry per design, recipes resolved lazily behind the grid, and a ship-first
    /// filter over the result. Phase 1 of the Hub marketplace — everything here rides
    /// public, auth-free ESI routes; price history and recommendations arrive with the
    /// evelens.dev collector in a later phase.
    /// </summary>
    /// <remarks>
    /// Rate-limit posture: one page fetch per <see cref="PageDelayMs"/> during the
    /// initial load (capped at <see cref="MaxPages"/> pages), then one public recipe
    /// fetch per <see cref="RecipeDelayMs"/> for entries the user can currently see.
    /// The feed is browsed, not mirrored — mirroring is the collector's job.
    /// </remarks>
    public sealed class SkinrMarketViewModel : ViewModelBase
    {
        // Sized to the REAL market with headroom, not a guess: the live feed
        // measured 3,729 listings across 39 pages (2026-08-25). Eight pages
        // showed 800 and read as "the whole market" — a silent 79% truncation.
        private const int MaxPages = 60;
        private const int PageDelayMs = 250;
        private const int RecipeDelayMs = 150;

        private readonly List<SkinrMarketEntry> _entries = new();
        private CancellationTokenSource? _recipeWalk;
        private string _searchText = string.Empty;
        private int _shipTypeFilter;

        /// <summary>Fires on the loading thread whenever entries or their recipes change.</summary>
        public event Action? MarketChanged;

        /// <summary>
        /// Consent gate for the hub catalog — the same remembered community-previews
        /// choice that gates the thumbnail shelf (one yes covers "talk to the EveLens
        /// hub"). Null or false means the catalog is never fetched and identification
        /// falls back to the client-side ESI recipe walk.
        /// </summary>
        public Func<bool>? CommunityCatalog { get; set; }

        /// <summary>
        /// Fetches the hub catalog and stamps identity onto current entries — called
        /// from LoadAsync, and again if the user grants consent with the pane open.
        /// Safe to call repeatedly; a null catalog is a no-op.
        /// </summary>
        public async Task ApplyCatalogAsync()
        {
            if (CommunityCatalog?.Invoke() != true)
                return;
            var catalog = await SkinrHubCatalog.TryGetAsync().ConfigureAwait(false);
            if (catalog == null)
                return;
            lock (_entries)
            {
                ApplyCatalog(_entries, catalog);
            }
            MarketChanged?.Invoke();
        }

        /// <summary>The stamping half, pure and testable: entries the catalog knows
        /// gain identity; entries already stamped are left alone. Hull names resolve
        /// to type ids here — once per unique hull, on the loading thread — because
        /// GetItemByName is a linear SDE scan and the UI thread must never pay it.</summary>
        internal static void ApplyCatalog(
            IEnumerable<SkinrMarketEntry> entries,
            IReadOnlyDictionary<string, HubDesignInfo> catalog)
        {
            var hullIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (SkinrMarketEntry entry in entries)
            {
                if (entry.Catalog != null ||
                    !catalog.TryGetValue(entry.SkinrId, out HubDesignInfo? info))
                    continue;
                string hull = info!.Hull ?? string.Empty;
                if (!hullIds.TryGetValue(hull, out int typeId))
                {
                    Item? item = hull.Length == 0 ? null : StaticItems.GetItemByName(hull);
                    typeId = item == null || item == Item.UnknownItem ? 0 : item.ID;
                    hullIds[hull] = typeId;
                }
                entry.SetCatalog(info, typeId);
            }
        }

        /// <summary>True while the listing feed itself is loading (not the recipe walk).</summary>
        public bool IsLoading { get; private set; }

        /// <summary>Set when the feed could not be read at all.</summary>
        public string? Error { get; private set; }

        /// <summary>True once a load has completed (even an empty one).</summary>
        public bool HasLoaded { get; private set; }

        /// <summary>Total listings behind the current entries (all states).</summary>
        public int TotalListings { get; private set; }

        /// <summary>Case-insensitive match over design name, hull name and creator.</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value ?? string.Empty;
                MarketChanged?.Invoke();
            }
        }

        /// <summary>Ship type to show designs for; 0 means all ships. Setting this
        /// directly (the ship-first preset) clears any tree scope.</summary>
        public int ShipTypeFilter
        {
            get => _shipTypeFilter;
            set => SetScope(string.Empty, string.Empty, value);
        }

        /// <summary>Ship-class scope from the tree ("Assault Frigate"); empty = all.</summary>
        public string GroupFilter { get; private set; } = string.Empty;

        /// <summary>Faction scope from the tree ("GALLENTE"); empty = all.</summary>
        public string FactionFilter { get; private set; } = string.Empty;

        /// <summary>
        /// The browse scope, set atomically (one change event): a tree node maps to
        /// class / class+faction / exact hull; the root clears everything.
        /// </summary>
        public void SetScope(string groupName, string factionName, int shipTypeId)
        {
            GroupFilter = groupName ?? string.Empty;
            FactionFilter = factionName ?? string.Empty;
            _shipTypeFilter = shipTypeId;
            MarketChanged?.Invoke();
        }

        /// <summary>The entries the filters allow, in feed order (newest first).</summary>
        public IReadOnlyList<SkinrMarketEntry> Entries
        {
            get
            {
                lock (_entries)
                {
                    return _entries.Where(MatchesFilters).ToList();
                }
            }
        }

        /// <summary>Every hull that resolved so far, for the ship filter dropdown.</summary>
        public IReadOnlyList<(int TypeId, string Name)> KnownHulls
        {
            get
            {
                lock (_entries)
                {
                    return _entries
                        .Where(e => e.ShipTypeId > 0)
                        .Select(e => (e.ShipTypeId, e.HullName))
                        .Distinct()
                        .OrderBy(h => h.Item2, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// Loads the public feed: pages until the cursor runs dry or the cap, groups
        /// listings into design entries, then starts the lazy recipe walk.
        /// </summary>
        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (IsLoading)
                return;
            IsLoading = true;
            Error = null;
            MarketChanged?.Invoke();
            try
            {
                // The feed is newest-first: page 1 is the newest slice, and history is
                // walked with the BEFORE cursor (after = "newer than newest" = one page
                // forever — the "10 listings total" bug, measured live). Pages overlap
                // at boundaries occasionally, so listings dedupe by their id.
                var listings = new List<EsiSkinrListing>();
                var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string? before = null;
                for (int page = 0; page < MaxPages && !ct.IsCancellationRequested; page++)
                {
                    var result = await EsiSkinrService.GetHubListingsAsync(before, limit: 100)
                        .ConfigureAwait(false);
                    if (result.HasError || result.Result == null)
                    {
                        // A first-page failure is an error; a later one is a shorter feed.
                        if (page == 0)
                            Error = string.IsNullOrEmpty(result.ErrorMessage)
                                ? "Paragon Hub feed unavailable" : result.ErrorMessage;
                        break;
                    }
                    var pageListings = result.Result.Listings ?? new List<EsiSkinrListing>();
                    int fresh = 0;
                    foreach (EsiSkinrListing listing in pageListings)
                    {
                        if (string.IsNullOrEmpty(listing?.Id) || seenIds.Add(listing.Id))
                        {
                            listings.Add(listing!);
                            fresh++;
                        }
                    }
                    before = result.Result.Cursor?.Before;
                    if (string.IsNullOrEmpty(before) || pageListings.Count == 0 || fresh == 0)
                        break;
                    await Task.Delay(PageDelayMs, ct).ConfigureAwait(false);
                }

                List<SkinrMarketEntry> grouped = GroupListings(listings);
                // Recipes are immutable once published, so yesterday's walk answers
                // today's "which hull is this design for" instantly — the ship-first
                // filter only works cold-start because of this cache.
                _recipeCache ??= LoadRecipeCache();
                ApplyRecipeCache(grouped, _recipeCache);
                lock (_entries)
                {
                    _entries.Clear();
                    _entries.AddRange(grouped);
                }
                TotalListings = listings.Count;
                HasLoaded = true;

                // One GET identifies (nearly) everything before the first paint; the
                // recipe walk below then only warms full recipes for rendering
                // instead of being the thing the whole grid waits on (#139).
                await ApplyCatalogAsync().ConfigureAwait(false);

                _recipeWalk?.Cancel();
                _recipeWalk = new CancellationTokenSource();
                _ = WalkRecipesAsync(_recipeWalk.Token);
            }
            catch (OperationCanceledException)
            {
                // Pane closed mid-load; whatever arrived stays usable.
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsLoading = false;
                MarketChanged?.Invoke();
            }
        }

        /// <summary>
        /// One entry per design: how many listings are buyable now and the cheapest ask.
        /// Feed order (newest listing first) is preserved. Listing states outside the
        /// known vocabulary count as buyable rather than vanishing — CCP's state strings
        /// are not documented, and an unknown value hiding the whole marketplace would
        /// be the worse failure.
        /// </summary>
        internal static List<SkinrMarketEntry> GroupListings(
            IEnumerable<EsiSkinrListing> listings)
        {
            var byDesign = new Dictionary<string, SkinrMarketEntry>(
                StringComparer.OrdinalIgnoreCase);
            var ordered = new List<SkinrMarketEntry>();
            foreach (EsiSkinrListing listing in listings ?? Enumerable.Empty<EsiSkinrListing>())
            {
                if (string.IsNullOrEmpty(listing?.SkinrId))
                    continue;
                if (!byDesign.TryGetValue(listing.SkinrId, out SkinrMarketEntry? entry))
                {
                    entry = new SkinrMarketEntry(listing.SkinrId);
                    byDesign[listing.SkinrId] = entry;
                    ordered.Add(entry);
                }
                bool gone = string.Equals(listing.State, "sold", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listing.State, "expired", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listing.State, "cancelled", StringComparison.OrdinalIgnoreCase);
                if (gone)
                    continue;
                entry.ActiveListings++;
                long plex = listing.Price?.Plex ?? 0;
                if (plex > 0 && (entry.MinPlex == 0 || plex < entry.MinPlex))
                    entry.MinPlex = plex;
            }
            return ordered;
        }

        /// <summary>
        /// The filtered entries organised the way a capsuleer thinks about hulls:
        /// ship class, then faction, then hull, then design name ("Shuttles · Amarr").
        /// Entries whose recipe hasn't resolved yet land in a trailing section keyed
        /// "…" — the window labels it "identifying".
        /// </summary>
        public IReadOnlyList<(string Group, string Faction, IReadOnlyList<SkinrMarketEntry> Designs)>
            Sections()
        {
            return BuildSections(Entries,
                e => e.ClassName,
                e => e.FactionName,
                e => e.HullName);
        }

        /// <summary>The grouping contract, selectors injected so it tests without SDE data.</summary>
        internal static IReadOnlyList<(string Group, string Faction, IReadOnlyList<SkinrMarketEntry> Designs)>
            BuildSections(
                IEnumerable<SkinrMarketEntry> entries,
                Func<SkinrMarketEntry, string> shipClass,
                Func<SkinrMarketEntry, string> faction,
                Func<SkinrMarketEntry, string> hull)
        {
            var sections =
                new List<(string, string, IReadOnlyList<SkinrMarketEntry>)>();
            var resolved = new List<SkinrMarketEntry>();
            var pending = new List<SkinrMarketEntry>();
            foreach (SkinrMarketEntry entry in entries)
            {
                if (entry.IsIdentified)
                    resolved.Add(entry);
                else
                    pending.Add(entry);
            }
            // Classes alphabetically with the classless bucket last, factions the same
            // within a class; the still-identifying bucket ("…") is always the tail.
            foreach (var group in resolved
                .GroupBy(e => (Class: shipClass(e) ?? string.Empty,
                               Faction: faction(e) ?? string.Empty))
                .OrderBy(g => g.Key.Class.Length == 0 ? 1 : 0)
                .ThenBy(g => g.Key.Class, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Key.Faction.Length == 0 ? 1 : 0)
                .ThenBy(g => g.Key.Faction, StringComparer.OrdinalIgnoreCase))
            {
                var designs = group
                    .OrderBy(e => hull(e), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                sections.Add((group.Key.Class, group.Key.Faction, designs));
            }
            if (pending.Count > 0)
                sections.Add(("…", string.Empty, pending));
            return sections;
        }

        /// <summary>Entries with no identity from ANY source (recipe or catalog) —
        /// the count the pane's "identifying N designs…" label reports. With the hub
        /// catalog on, this is zero from the first paint and the label never shows.</summary>
        public int UnidentifiedCount
        {
            get
            {
                lock (_entries)
                {
                    return _entries.Count(e => !e.IsIdentified);
                }
            }
        }

        /// <summary>Entries still waiting on their recipe — surfaced so the pane can say
        /// "identifying N designs…" instead of looking mysteriously sparse.</summary>
        public int UnresolvedCount
        {
            get
            {
                lock (_entries)
                {
                    return _entries.Count(e => e.Recipe == null && !e.RecipeFailed);
                }
            }
        }

        /// <summary>
        /// Resolves recipes for entries that lack one, filtered-first so the designs the
        /// user is looking at get names before the ones scrolled away. Sequential with a
        /// polite gap — this is a browse, not a crawl.
        /// </summary>
        private async Task WalkRecipesAsync(CancellationToken ct)
        {
            int sinceSave = 0;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    SkinrMarketEntry? next;
                    lock (_entries)
                    {
                        next = _entries.FirstOrDefault(e => e.Recipe == null
                                && !e.RecipeFailed && MatchesFilters(e))
                            ?? _entries.FirstOrDefault(e => e.Recipe == null && !e.RecipeFailed);
                    }
                    if (next == null)
                        return;

                    var result = await EsiSkinrService.GetDesignAsync(next.SkinrId)
                        .ConfigureAwait(false);
                    if (result.HasError || result.Result == null)
                    {
                        next.RecipeFailed = true;
                    }
                    else
                    {
                        next.Recipe = result.Result;
                        var cache = _recipeCache;
                        if (cache != null)
                        {
                            lock (cache)
                            {
                                cache[next.SkinrId] = result.Result;
                            }
                            if (++sinceSave >= 25)
                            {
                                sinceSave = 0;
                                SaveRecipeCache();
                            }
                        }
                    }
                    MarketChanged?.Invoke();
                    await Task.Delay(RecipeDelayMs, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Pane closed; the walk resumes where the cache says it left off.
            }
            finally
            {
                if (sinceSave > 0)
                    SaveRecipeCache();
            }
        }

        // --- recipe disk cache ----------------------------------------------------

        private Dictionary<string, EsiSkinrRecipe>? _recipeCache;

        private string RecipeCacheFile => Path.Combine(
            AppServices.ApplicationPaths.DataDirectory, "cache", "skinr",
            "market-recipes.json");

        /// <summary>Marries cached recipes to fresh entries — the pure half, testable.</summary>
        internal static void ApplyRecipeCache(
            IEnumerable<SkinrMarketEntry> entries,
            IReadOnlyDictionary<string, EsiSkinrRecipe> cache)
        {
            foreach (SkinrMarketEntry entry in entries)
            {
                if (entry.Recipe == null &&
                    cache.TryGetValue(entry.SkinrId, out EsiSkinrRecipe? recipe))
                    entry.Recipe = recipe;
            }
        }

        private Dictionary<string, EsiSkinrRecipe> LoadRecipeCache()
        {
            try
            {
                if (File.Exists(RecipeCacheFile))
                {
                    var loaded = JsonSerializer
                        .Deserialize<Dictionary<string, EsiSkinrRecipe>>(
                            File.ReadAllText(RecipeCacheFile));
                    if (loaded != null)
                        return new Dictionary<string, EsiSkinrRecipe>(
                            loaded, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception)
            {
                // A corrupt cache re-resolves from ESI; never worth failing the pane.
            }
            return new Dictionary<string, EsiSkinrRecipe>(StringComparer.OrdinalIgnoreCase);
        }

        private void SaveRecipeCache()
        {
            var cache = _recipeCache;
            if (cache == null)
                return;
            try
            {
                string json;
                lock (cache)
                {
                    json = JsonSerializer.Serialize(cache);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(RecipeCacheFile)!);
                File.WriteAllText(RecipeCacheFile, json);
            }
            catch (Exception)
            {
                // Cache misses cost a re-walk, not correctness.
            }
        }

        private bool MatchesFilters(SkinrMarketEntry entry)
        {
            if (_shipTypeFilter > 0 && entry.ShipTypeId != _shipTypeFilter)
                return false;
            if (GroupFilter.Length > 0 && !string.Equals(
                    entry.ClassName, GroupFilter, StringComparison.OrdinalIgnoreCase))
                return false;
            if (FactionFilter.Length > 0 && !string.Equals(
                    entry.FactionName, FactionFilter, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;
            return Matches(entry, _searchText);
        }

        /// <summary>
        /// The navigation tree over everything the feed carries (ignoring the current
        /// scope — the tree IS the scope picker): class → faction → hull with design
        /// counts. Only resolved entries appear; the identifying tail lives in the grid.
        /// </summary>
        public IReadOnlyList<MarketTreeClass> Tree()
        {
            List<SkinrMarketEntry> resolved;
            lock (_entries)
            {
                resolved = _entries.Where(e => e.IsIdentified).ToList();
            }
            return resolved
                .GroupBy(e => e.ClassName)
                .OrderBy(g => g.Key.Length == 0 ? 1 : 0)
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(cls => new MarketTreeClass(
                    cls.Key,
                    cls.GroupBy(e => e.FactionName)
                        .OrderBy(g => g.Key.Length == 0 ? 1 : 0)
                        .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(fac => new MarketTreeFaction(
                            fac.Key,
                            fac.GroupBy(e => (e.ShipTypeId, e.HullName))
                                .OrderBy(g => g.Key.HullName, StringComparer.OrdinalIgnoreCase)
                                .Select(h => new MarketTreeHull(
                                    h.Key.ShipTypeId, h.Key.HullName, h.Count()))
                                .ToList()))
                        .ToList()))
                .ToList();
        }

        /// <summary>The search contract: name, hull or creator, case-insensitive.</summary>
        internal static bool Matches(SkinrMarketEntry entry, string text)
        {
            return entry.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.HullName.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.CreatorName.Contains(text, StringComparison.OrdinalIgnoreCase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _recipeWalk?.Cancel();
            base.Dispose(disposing);
        }
    }

    /// <summary>A hull leaf in the market tree: the ship and its design count.</summary>
    public sealed record MarketTreeHull(int TypeId, string Name, int Designs);

    /// <summary>A faction branch in the market tree.</summary>
    public sealed record MarketTreeFaction(string Name, IReadOnlyList<MarketTreeHull> Hulls);

    /// <summary>A ship-class branch in the market tree (the top level).</summary>
    public sealed record MarketTreeClass(string Name, IReadOnlyList<MarketTreeFaction> Factions);

    /// <summary>One design on the Paragon Hub: its buyable listings collapsed into a
    /// card's worth of facts, with the recipe arriving lazily behind it.</summary>
    public sealed class SkinrMarketEntry
    {
        public SkinrMarketEntry(string skinrId)
        {
            SkinrId = skinrId;
        }

        public string SkinrId { get; }

        /// <summary>Listings buyable right now (sold/expired/cancelled excluded).</summary>
        public int ActiveListings { get; internal set; }

        /// <summary>Cheapest current ask in PLEX; 0 when no priced listing exists.</summary>
        public long MinPlex { get; internal set; }

        /// <summary>The public recipe, once the walk reaches this entry.</summary>
        public EsiSkinrRecipe? Recipe { get; internal set; }

        /// <summary>Set when the recipe route errored — the walk moves on.</summary>
        public bool RecipeFailed { get; internal set; }

        /// <summary>The hub catalog's pre-resolved identity for this design, when the
        /// community catalog is enabled and knows it. The recipe, once fetched, always
        /// wins — the catalog is a head start, not an authority.</summary>
        public HubDesignInfo? Catalog { get; private set; }

        // The catalog names the hull ("Cerberus"); ApplyCatalog resolves that back
        // to a type id (once per unique hull, off the UI thread) so ship filters,
        // tree hull nodes and stock hull art all work before any recipe arrives.
        private int _catalogTypeId;

        internal void SetCatalog(HubDesignInfo info, int shipTypeId)
        {
            Catalog = info;
            _catalogTypeId = shipTypeId;
        }

        /// <summary>True once anything (recipe or catalog) can say what this design
        /// IS — the section test. Unidentified entries wait in the "…" tail.</summary>
        public bool IsIdentified => Recipe != null || Catalog != null;

        public string DisplayName => !string.IsNullOrEmpty(Recipe?.Name)
            ? Recipe!.Name
            : !string.IsNullOrEmpty(Catalog?.Name)
                ? Catalog!.Name
                : SkinrId.Length > 12 ? SkinrId[..12] + "…" : SkinrId;

        public int ShipTypeId => Recipe?.ShipTypeId > 0
            ? Recipe!.ShipTypeId
            : _catalogTypeId;

        public int TierLevel => Recipe?.Tier?.Level ?? Catalog?.Tier ?? 0;

        public string HullName
        {
            get
            {
                if (ShipTypeId <= 0)
                    return string.Empty;
                Item item = StaticItems.GetItemByID(ShipTypeId);
                return item == Item.UnknownItem ? string.Empty : item.LocalizedName;
            }
        }

        /// <summary>The hull's ship class ("Shuttle", "Combat Recon Ship"…) — the top
        /// grouping level of the browse; empty until the recipe resolves.</summary>
        public string ClassName
        {
            get
            {
                if (ShipTypeId <= 0)
                    return string.Empty;
                Item item = StaticItems.GetItemByID(ShipTypeId);
                return item == Item.UnknownItem
                    ? string.Empty : item.GroupName ?? string.Empty;
            }
        }

        /// <summary>The hull's faction for section grouping — empty for combined-flag
        /// races (Race is a flags enum; a Triglavian hull reports four empire bits and
        /// printing that would be nonsense, same rule as the hull subtitle).</summary>
        public string FactionName
        {
            get
            {
                if (ShipTypeId <= 0)
                    return string.Empty;
                Item item = StaticItems.GetItemByID(ShipTypeId);
                if (item == Item.UnknownItem)
                    return string.Empty;
                return item.Race != Race.None && item.Race != Race.All &&
                       Enum.IsDefined(typeof(Race), item.Race)
                    ? item.Race.ToString().ToUpperInvariant()
                    : string.Empty;
            }
        }

        /// <summary>The creator's name via the shared ID→name pipeline; empty until it
        /// resolves (EveIDToNameUpdatedEvent announces arrivals).</summary>
        public string CreatorName
        {
            get
            {
                long id = Recipe?.CreatorId ?? 0;
                if (id > 0)
                {
                    string name = EveIDToName.GetIDToName(id);
                    if (name != EveLensConstants.UnknownText)
                        return name;
                }
                return Catalog?.Creator ?? string.Empty;
            }
        }
    }
}
