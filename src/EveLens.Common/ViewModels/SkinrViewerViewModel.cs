// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EveLens.Common.Data;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// State machine for the SKINR Viewer window (experimental): checks whether the
    /// selected character's ESI key carries <c>esi.cosmetic.char:read</c>, loads their
    /// SKINR license inventory, and resolves individual design recipes. The 3D render
    /// pane consumes <see cref="SelectedRecipe"/> — this VM knows nothing about Trinity.
    /// </summary>
    public sealed class SkinrViewerViewModel : IDisposable
    {
        /// <summary>The scope gating the character-inventory routes.</summary>
        public const string SkinrScope = "esi.cosmetic.char:read";

        public enum ViewState
        {
            NoCharacter,
            ScopeMissing,
            LoadingInventory,
            Loaded,
            Error
        }

        public ViewState State { get; private set; } = ViewState.NoCharacter;

        public string ErrorMessage { get; private set; } = string.Empty;

        public Character SelectedCharacter { get; private set; }

        /// <summary>The character's SKINR licenses after a successful load.</summary>
        public IReadOnlyList<SkinrLicenseEntry> Licenses { get; private set; } =
            new List<SkinrLicenseEntry>();

        /// <summary>The recipe of the design currently selected, if fetched.</summary>
        public EsiSkinrRecipe SelectedRecipe { get; private set; }

        /// <summary>Fires whenever State/Licenses/SelectedRecipe change.</summary>
        public event Action StateChanged;

        /// <summary>
        /// True when the character has at least one ESI key granting the SKINR scope.
        /// </summary>
        public static bool HasSkinrScope(Character character) =>
            character?.Identity?.ESIKeys?.Any(k => k.HasScope(SkinrScope)) == true;

        private static ESIKey FindSkinrKey(Character character) =>
            character?.Identity?.ESIKeys?.FirstOrDefault(k => k.HasScope(SkinrScope));

        /// <summary>
        /// Selects a character: gates on the scope, then loads their inventory.
        /// </summary>
        public async Task SelectCharacterAsync(Character character)
        {
            SelectedCharacter = character;
            SelectedRecipe = null;
            Licenses = new List<SkinrLicenseEntry>();

            if (character == null)
            {
                SetState(ViewState.NoCharacter);
                return;
            }

            var key = FindSkinrKey(character);
            if (key == null)
            {
                SetState(ViewState.ScopeMissing);
                return;
            }

            SetState(ViewState.LoadingInventory);
            try
            {
                var result = await EsiSkinrService.GetCharacterSkinrsAsync(
                    character.CharacterID, key.AccessToken).ConfigureAwait(false);

                if (result.HasError || result.Result == null)
                {
                    ErrorMessage = result.Exception?.Message ?? "ESI request failed";
                    SetState(ViewState.Error);
                    return;
                }

                Licenses = result.Result.Licenses
                    .Select(l => new SkinrLicenseEntry(l))
                    .OrderByDescending(l => l.Activated)
                    .ToList();
                SetState(ViewState.Loaded);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                SetState(ViewState.Error);
            }
        }

        /// <summary>
        /// Fetches the full public recipe for a design; null clears the selection.
        /// </summary>
        public async Task SelectDesignAsync(string skinrId)
        {
            if (string.IsNullOrEmpty(skinrId))
            {
                SelectedRecipe = null;
                StateChanged?.Invoke();
                return;
            }

            var result = await EsiSkinrService.GetDesignAsync(skinrId).ConfigureAwait(false);
            SelectedRecipe = result.HasError ? null : result.Result;
            StateChanged?.Invoke();
        }

        /// <summary>Human summary of the selected recipe for the details panel.</summary>
        public string DescribeSelectedRecipe()
        {
            var r = SelectedRecipe;
            if (r == null)
                return string.Empty;

            int coatings = r.Layout?.Slots?.Count(s => s.Configuration?.Nanocoating != null) ?? 0;
            int patterns = r.Layout?.Slots?.Count(s => s.Configuration?.Pattern != null) ?? 0;
            string hull = StaticItems.GetItemName(r.ShipTypeId);
            return $"{r.Name} — {r.Line}\n{hull} · Tier {r.Tier?.Level} · " +
                   $"{coatings} nanocoatings · {patterns} patterns";
        }

        private void SetState(ViewState state)
        {
            State = state;
            StateChanged?.Invoke();
        }

        public void Dispose() { }
    }

    /// <summary>One owned SKINR license, display-shaped.</summary>
    public sealed class SkinrLicenseEntry
    {
        public string SkinrId { get; }
        public bool Activated { get; }
        public long Unactivated { get; }

        public string ShortId => SkinrId.Length > 12 ? SkinrId[..12] + "…" : SkinrId;
        public string StatusText => Activated
            ? (Unactivated > 0 ? $"Active · {Unactivated} spare" : "Active")
            : $"{Unactivated} unactivated";

        public SkinrLicenseEntry(EsiSkinrLicense license)
        {
            SkinrId = license.SkinrId ?? string.Empty;
            Activated = license.Activated;
            Unactivated = license.Unactivated;
        }
    }
}
