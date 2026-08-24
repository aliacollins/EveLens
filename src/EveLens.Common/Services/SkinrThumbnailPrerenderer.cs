// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Data;
using EveLens.Common.Interfaces;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The Hub's quiet workhorse: a second, Preview-quality render sidecar that walks
    /// the marketplace and fills the thumbnail cache with REAL skinned renders — every
    /// design at the same camera and lighting, which is the entire point of the Hub
    /// ("judge the skins, not the screenshots"). Runs one design at a time on its own
    /// engine process so the interactive stage never queues behind it; a design that
    /// fails to build is remembered and skipped rather than retried forever.
    /// </summary>
    /// <remarks>
    /// Candidates come from a provider callback each cycle, so priority is always
    /// live: whatever the market grid currently shows renders first, and a scope
    /// change reprioritises the very next pick. Thumbnails persist on disk, so this
    /// is a one-time cost per design per machine.
    /// </remarks>
    public sealed class SkinrThumbnailPrerenderer : IDisposable
    {
        private readonly SkinrThumbnailCache _cache;
        private readonly ISkinrRecipeResolver _resolver;
        private readonly Func<IReadOnlyList<EsiSkinrRecipe>> _candidates;
        private readonly HashSet<string> _failed = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _cts;
        private Task? _loop;
        private SkinrSidecarHost? _host;

        // Long enough that the GPU and the CDN get real gaps between designs; a
        // background job that saturates either stops being background.
        private const int BetweenDesignsMs = 750;
        private const int IdlePollMs = 5000;

        private readonly Func<bool>? _communityPreviews;

        public SkinrThumbnailPrerenderer(
            SkinrThumbnailCache cache,
            Func<IReadOnlyList<EsiSkinrRecipe>> candidates,
            Func<bool>? communityPreviews = null,
            ISkinrRecipeResolver? resolver = null)
        {
            _cache = cache;
            _candidates = candidates;
            _communityPreviews = communityPreviews;
            _resolver = resolver ?? AppServices.SkinrRecipeResolver;
        }

        /// <summary>Raised (off the UI thread) when a design's thumbnail lands on disk.</summary>
        public event Action<string, string>? ThumbnailCaptured;

        /// <summary>Raised when <see cref="CurrentLabel"/> changes.</summary>
        public event Action? StateChanged;

        /// <summary>The design being rendered right now, null while idle.</summary>
        public string? CurrentLabel { get; private set; }

        /// <summary>Thumbnails produced this session.</summary>
        public int Rendered { get; private set; }

        /// <summary>Starts the background loop; safe to call more than once.</summary>
        public void Start()
        {
            if (_loop != null)
                return;
            _cts = new CancellationTokenSource();
            _loop = RunAsync(_cts.Token);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    EsiSkinrRecipe? next = PickNext();
                    if (next == null)
                    {
                        SetLabel(null);
                        await Task.Delay(IdlePollMs, ct).ConfigureAwait(false);
                        continue;
                    }

                    SetLabel(string.IsNullOrEmpty(next.Name) ? next.Id : next.Name);
                    bool rendered = false;
                    try
                    {
                        rendered = await ProduceOneAsync(next, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _failed.Add(next.Id);
                        AppServices.TraceService?.Trace(
                            $"Skinr prerender: {next.Id} failed: {ex.Message}");
                    }
                    // Only local renders earn the long breather; a CDN hit costs the
                    // GPU nothing and the shelf should empty at download speed.
                    await Task.Delay(rendered ? BetweenDesignsMs : 100, ct)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Window closed; the loop's work so far is safely on disk.
            }
            finally
            {
                SetLabel(null);
            }
        }

        private EsiSkinrRecipe? PickNext()
        {
            foreach (EsiSkinrRecipe recipe in _candidates())
            {
                if (string.IsNullOrEmpty(recipe?.Id) || _failed.Contains(recipe!.Id))
                    continue;
                if (_cache.TryGetPath(recipe.Id) != null)
                    continue;
                return recipe;
            }
            return null;
        }

        /// <summary>Produces one thumbnail; returns true when the GPU did the work
        /// (false for CDN hits and skips, which need no cool-down).</summary>
        private async Task<bool> ProduceOneAsync(EsiSkinrRecipe recipe, CancellationToken ct)
        {
            // Someone may already have paid for this render — take it off the shelf
            // rather than rendering and waiting. Opt-in, validated, read-only.
            if (_communityPreviews?.Invoke() == true)
            {
                string? shelf = await SkinrThumbnailCdn.TryFetchAsync(recipe.Id, _cache)
                    .ConfigureAwait(false);
                if (shelf != null)
                {
                    Rendered++;
                    ThumbnailCaptured?.Invoke(recipe.Id, shelf);
                    return false;
                }
            }

            if (!_resolver.IsAvailable)
            {
                // The catalog arrives with startup data; poll rather than fail designs.
                await Task.Delay(IdlePollMs, ct).ConfigureAwait(false);
                return false;
            }
            SkinrResolvedDesign design = _resolver.Resolve(recipe);
            if (!design.IsRenderable)
            {
                _failed.Add(recipe.Id);
                return false;
            }

            // Preview tier: small frame, fast settle — thumbnail physics, not portraits.
            _host ??= await SkinrSidecarHost.CreateAsync(ct).ConfigureAwait(false);

            SkinrLoadResult load = await _host.LoadAsync(design, ct).ConfigureAwait(false);
            if (!load.Ok)
            {
                _failed.Add(recipe.Id);
                return true;   // the engine worked even though it failed — breathe
            }
            SkinrFrame? frame = await _host.RenderAsync(settle: true, ct)
                .ConfigureAwait(false);
            // A dark frame is a failed render wearing a success return. 2.0 let
            // through near-black frames (ships as faint constellations of running
            // lights — user-reported); a lit hull on the studio backdrop measures
            // well above 8 even for all-black coatings.
            if (frame == null || frame.MeanLuma <= 8.0)
            {
                _failed.Add(recipe.Id);
                return true;
            }
            string? path = _cache.Save(recipe.Id, frame);
            if (path == null)
            {
                _failed.Add(recipe.Id);
                return true;
            }
            Rendered++;
            ThumbnailCaptured?.Invoke(recipe.Id, path);
            return true;
        }

        private void SetLabel(string? label)
        {
            if (CurrentLabel == label)
                return;
            CurrentLabel = label;
            StateChanged?.Invoke();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _host?.Dispose();
        }
    }
}
