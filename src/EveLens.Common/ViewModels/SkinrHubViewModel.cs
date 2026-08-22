// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// The SKINR Hub window's view model: the design carousel with real names and
    /// thumbnails, the hull identity overlay, search, and the environment switcher —
    /// composed over <see cref="SkinrViewerViewModel"/>, which keeps owning the ESI data
    /// (scope gate, inventory, recipes).
    /// </summary>
    /// <remarks>
    /// <para><b>Names arrive lazily.</b> The inventory route returns license ids only; a
    /// design's name, tier and hull live in its public recipe. Fetching every recipe up
    /// front would turn "open the window" into N ESI calls on the UI's critical path, so
    /// entries appear immediately with their short ids and a background walk fills names
    /// in, raising <see cref="CarouselChanged"/> as batches land. The walk is sequential
    /// on purpose — the public recipe route is cheap and ETag-cached, and one request in
    /// flight is a polite client.</para>
    /// </remarks>
    public sealed class SkinrHubViewModel : ViewModelBase
    {
        private readonly List<SkinrHubDesignEntry> _entries = new();
        private CancellationTokenSource? _nameWalk;
        private string _searchText = string.Empty;

        /// <summary>The data half: character, scope gate, licenses, selected recipe.</summary>
        public SkinrViewerViewModel Data { get; } = new();

        /// <summary>Thumbnail store the carousel reads and the render path writes.</summary>
        public SkinrThumbnailCache Thumbnails { get; } = new();

        /// <summary>Raised when carousel entries appear, change labels, or gain thumbnails.</summary>
        public event Action? CarouselChanged;

        /// <summary>The switcher's current preset. Applied by the view via the render VM.</summary>
        public SkinrEnvironmentPreset Environment { get; set; } = SkinrEnvironmentPreset.Studio;

        /// <summary>Live search over design names and ids; empty shows everything.</summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value ?? string.Empty;
                CarouselChanged?.Invoke();
            }
        }

        /// <summary>Carousel entries after the search filter, in inventory order.</summary>
        public IReadOnlyList<SkinrHubDesignEntry> Designs
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_searchText))
                    return _entries;
                string needle = _searchText.Trim();
                return _entries
                    .Where(d => d.Label.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                                d.SkinrId.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                                d.HullName.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// Rebuilds carousel entries from the current inventory and starts the background
        /// name walk. Call after <see cref="SkinrViewerViewModel.SelectCharacterAsync"/>.
        /// </summary>
        public void RefreshDesigns()
        {
            _nameWalk?.Cancel();
            _entries.Clear();
            foreach (SkinrLicenseEntry license in Data.Licenses)
            {
                _entries.Add(new SkinrHubDesignEntry(license)
                {
                    ThumbnailPath = Thumbnails.TryGetPath(license.SkinrId)
                });
            }
            CarouselChanged?.Invoke();

            _nameWalk = new CancellationTokenSource();
            _ = WalkNamesAsync(_nameWalk.Token);
        }

        /// <summary>
        /// Records a freshly captured thumbnail for a design and refreshes its tile.
        /// </summary>
        public void OnThumbnailCaptured(string skinrId, string path)
        {
            SkinrHubDesignEntry? entry = _entries.FirstOrDefault(d => d.SkinrId == skinrId);
            if (entry == null)
                return;
            entry.ThumbnailPath = path;
            CarouselChanged?.Invoke();
        }

        // --- hull identity overlay --------------------------------------------

        /// <summary>The selected design's hull, or null before a recipe is loaded.</summary>
        public Item? Hull
        {
            get
            {
                int typeId = Data.SelectedRecipe?.ShipTypeId ?? 0;
                if (typeId <= 0)
                    return null;
                Item item = StaticItems.GetItemByID(typeId);
                return item == Item.UnknownItem ? null : item;
            }
        }

        /// <summary>"MINMATAR • FRIGATE"-style secondary line for the hull overlay.</summary>
        public string HullSubtitle
        {
            get
            {
                Item? hull = Hull;
                if (hull == null)
                    return string.Empty;
                var parts = new List<string>();
                if (hull.Race != Race.None)
                    parts.Add(hull.Race.ToString().ToUpperInvariant());
                if (!string.IsNullOrEmpty(hull.GroupName))
                    parts.Add(hull.GroupName!.ToUpperInvariant());
                return string.Join("  •  ", parts);
            }
        }

        /// <summary>Design title card: creator line, e.g. "Tier 3 design".</summary>
        public string DesignSubtitle
        {
            get
            {
                EsiSkinrRecipe? recipe = Data.SelectedRecipe;
                if (recipe == null)
                    return string.Empty;
                int coatings = recipe.Layout?.Slots?
                    .Count(s => s.Configuration?.Nanocoating != null) ?? 0;
                int patterns = recipe.Layout?.Slots?
                    .Count(s => s.Configuration?.Pattern != null) ?? 0;
                return $"{coatings} nanocoatings · {patterns} patterns";
            }
        }

        /// <summary>Tier level of the selected design, or 0 when none/unknown.</summary>
        public int SelectedTier => Data.SelectedRecipe?.Tier?.Level ?? 0;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nameWalk?.Cancel();
                _nameWalk?.Dispose();
                Data.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Fills entry names/tiers/hulls from public recipes, one request at a time.
        /// </summary>
        private async Task WalkNamesAsync(CancellationToken ct)
        {
            try
            {
                foreach (SkinrHubDesignEntry entry in _entries.ToList())
                {
                    if (ct.IsCancellationRequested)
                        return;
                    if (entry.HasRecipe)
                        continue;

                    var result = await EsiSkinrService.GetDesignAsync(entry.SkinrId)
                        .ConfigureAwait(false);
                    if (ct.IsCancellationRequested)
                        return;
                    if (result.HasError || result.Result == null)
                        continue;

                    entry.ApplyRecipe(result.Result);
                    CarouselChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrHub: design name walk stopped: {ex.Message}");
            }
        }
    }

    /// <summary>One carousel tile: a license, progressively enriched by its public recipe.</summary>
    public sealed class SkinrHubDesignEntry
    {
        public SkinrHubDesignEntry(SkinrLicenseEntry license)
        {
            License = license;
        }

        public SkinrLicenseEntry License { get; }

        public string SkinrId => License.SkinrId;

        /// <summary>Design name once the recipe has arrived; short id until then.</summary>
        public string Label { get; private set; } = string.Empty;

        /// <summary>Hull name once known, for search and the tile tooltip.</summary>
        public string HullName { get; private set; } = string.Empty;

        /// <summary>Tier level, 0 until the recipe arrives.</summary>
        public int TierLevel { get; private set; }

        public bool HasRecipe { get; private set; }

        /// <summary>Captured thumbnail path, or null while the tile shows its placeholder.</summary>
        public string? ThumbnailPath { get; set; }

        /// <summary>Display label falling back to the license's short id.</summary>
        public string DisplayLabel =>
            string.IsNullOrEmpty(Label) ? License.ShortId : Label;

        public void ApplyRecipe(EsiSkinrRecipe recipe)
        {
            Label = recipe.Name ?? string.Empty;
            TierLevel = recipe.Tier?.Level ?? 0;
            HullName = StaticItems.GetItemName(recipe.ShipTypeId);
            HasRecipe = true;
        }
    }
}
