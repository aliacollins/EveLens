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
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// The photo-op fleet: wingman lifecycle (assemble/re-form/disband), the live
    /// layout (measured radii, current offsets, design ids), and free 3D placement
    /// (camera-plane drag + view-axis depth push). Split from
    /// <see cref="SkinrRenderViewModel"/> along the same seam as
    /// <see cref="SkinrStageCamera"/> (Law 2): the render VM owns the sidecar, the
    /// loop and the frames; this owns who is flying and where.
    /// </summary>
    /// <remarks>
    /// The host is resolved through a delegate because the VM replaces it across
    /// re-initialisations; the controller must always talk to the CURRENT engine.
    /// </remarks>
    public sealed class SkinrFleetController
    {
        private readonly Func<SkinrSidecarHost?> _host;
        private readonly ISkinrRecipeResolver _resolver;
        private readonly SkinrStageCamera _camera;
        private readonly Action<bool> _requestRender;   // settled?
        private readonly Action<string> _status;

        // The fleet's live layout: one measured radius, one current offset and one
        // design id per wingman, in build order — the same order the sidecar
        // indexes moves by.
        private readonly List<double> _wingRadii = new();
        private readonly List<double[]> _wingOffsets = new();
        private readonly List<string> _wingDesignIds = new();

        // Free-move drag state: which ship the pointer holds, the pixel delta not
        // yet sent, and whether a move op is in flight (one at a time; deltas
        // coalesce). Viewport dims come in with each gesture, in the same layout
        // units as the pointer positions the window passes.
        private int _dragWingman = -1;
        private double _dragPendingX, _dragPendingY, _dragDepth, _dragViewH = 1.0;
        private bool _movePumpBusy;

        public SkinrFleetController(Func<SkinrSidecarHost?> host,
            ISkinrRecipeResolver resolver, SkinrStageCamera camera,
            Action<bool> requestRender, Action<string> status)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _requestRender = requestRender ?? throw new ArgumentNullException(nameof(requestRender));
            _status = status ?? (_ => { });
        }

        /// <summary>Ships currently flying formation with the primary (0 = solo).</summary>
        public int WingmenCount => _wingRadii.Count;

        /// <summary>The formation the fleet last assembled into.</summary>
        public SkinrFleetFormation Formation { get; private set; } = SkinrFleetFormation.Vic;

        /// <summary>The design ids currently flying as wingmen — what the Photo Op
        /// flyout re-checks on reopen so the list reflects the live fleet.</summary>
        public IReadOnlyList<string> AssembledDesignIds => _wingDesignIds;

        /// <summary>True while a free-move drag holds a ship (the window routes
        /// pointer motion here instead of to the orbit).</summary>
        public bool IsDraggingWingman => _dragWingman >= 0;

        /// <summary>Forgets the fleet's client-side state after the engine cleared it.
        /// The VM calls this on new-design loads and non-Space environment switches.</summary>
        public void Forget()
        {
            _wingRadii.Clear();
            _wingOffsets.Clear();
            _wingDesignIds.Clear();
            _dragWingman = -1;
            _camera.ClearFleetFraming();
        }

        /// <summary>
        /// Assembles a formation: each recipe becomes an additional built ship. A
        /// wingman builds at a far parking offset first so its REAL radius can be
        /// read back — the formation's slot math runs on measured hulls, then every
        /// ship moves into place at once and the camera pulls back to frame it.
        /// </summary>
        public async Task<int> AssembleAsync(IReadOnlyList<EsiSkinrRecipe> recipes,
            SkinrFleetFormation formation, CancellationToken ct = default)
        {
            var host = _host();
            if (host == null || !_resolver.IsAvailable)
                return 0;
            await host.ClearWingmenAsync(ct).ConfigureAwait(false);
            _wingRadii.Clear();
            _wingOffsets.Clear();
            _wingDesignIds.Clear();
            _dragWingman = -1;
            Formation = formation;

            foreach (EsiSkinrRecipe recipe in recipes)
            {
                ct.ThrowIfCancellationRequested();
                SkinrResolvedDesign design = _resolver.Resolve(recipe);
                if (!design.IsRenderable)
                    continue;
                _status(string.Format("Wingman arriving: {0}…", design.Name));
                double? radius = await host.AddWingmanAsync(design,
                    new[] { 0.0, 0.0, -200000.0 }, ct).ConfigureAwait(false);
                if (radius is > 0)
                {
                    // Ids stay index-aligned with radii/offsets: only a ship that
                    // actually built counts as flying.
                    _wingRadii.Add(radius.Value);
                    _wingDesignIds.Add(recipe.Id ?? string.Empty);
                }
            }

            await ApplyFormationAsync(formation, ct).ConfigureAwait(false);
            return _wingRadii.Count;
        }

        /// <summary>
        /// Re-forms the assembled fleet into a new shape by moving the existing
        /// ships — no rebuilds, so switching formations is instant. Also reframes
        /// the camera.
        /// </summary>
        public async Task ApplyFormationAsync(SkinrFleetFormation formation,
            CancellationToken ct = default)
        {
            Formation = formation;
            var host = _host();
            if (host == null || _wingRadii.Count == 0)
                return;

            IReadOnlyList<double[]> slots = SkinrFleetFormations.ComputeSlots(
                formation, _camera.Radius, _wingRadii);
            _wingOffsets.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                await host.MoveWingmanAsync(i, slots[i], ct).ConfigureAwait(false);
                _wingOffsets.Add((double[])slots[i].Clone());
                _requestRender(false);
            }

            _camera.FrameFleet(
                SkinrFleetFormations.Span(_camera.Radius, _wingRadii, slots) * 1.1);
            _requestRender(true);
        }

        /// <summary>Disbands the formation and reframes the primary alone.</summary>
        public async Task DisbandAsync(CancellationToken ct = default)
        {
            var host = _host();
            if (host == null)
                return;
            await host.ClearWingmenAsync(ct).ConfigureAwait(false);
            Forget();
            _requestRender(true);
        }

        // --- free movement -----------------------------------------------------
        //
        // Full 3D placement with two gestures: Ctrl+drag slides a ship in the plane
        // the camera is looking at (measured at that ship's own depth, so it stays
        // glued to the cursor at any zoom), and Ctrl+scroll pushes it along the view
        // axis. Orbit the camera and the same two gestures reach any point in space.

        /// <summary>The wingman visually nearest a screen point, or −1 with no fleet.
        /// Distance is measured on screen, where the user is aiming. Coordinates and
        /// viewport are the SAME frame (the render pane's layout units).</summary>
        public int PickWingman(double x, double y, double viewW, double viewH)
        {
            int best = -1;
            double bestSq = double.MaxValue;
            for (int i = 0; i < _wingOffsets.Count; i++)
            {
                (double px, double py, double depth) =
                    _camera.Project(_wingOffsets[i], viewW, viewH);
                if (depth <= 0.0)
                    continue;
                double dx = px - x, dy = py - y;
                double d2 = dx * dx + dy * dy;
                if (d2 < bestSq)
                {
                    bestSq = d2;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Grabs the wingman nearest the pointer for a free-move drag.
        /// Returns false when there is nothing to grab.</summary>
        public bool BeginDrag(double x, double y, double viewW, double viewH)
        {
            _dragWingman = PickWingman(x, y, viewW, viewH);
            if (_dragWingman < 0)
                return false;
            (_, _, _dragDepth) = _camera.Project(_wingOffsets[_dragWingman], viewW, viewH);
            _dragViewH = Math.Max(1.0, viewH);
            _dragPendingX = _dragPendingY = 0.0;
            return true;
        }

        /// <summary>Feeds pointer travel into the held ship. Cheap: accumulates the
        /// delta and nudges the single-in-flight move pump.</summary>
        public void DragBy(double dxPixels, double dyPixels)
        {
            if (_dragWingman < 0)
                return;
            _dragPendingX += dxPixels;
            _dragPendingY += dyPixels;
            PumpMove();
        }

        /// <summary>Releases the held ship and settles the frame.</summary>
        public void EndDrag()
        {
            _dragWingman = -1;
            _requestRender(true);
        }

        /// <summary>
        /// Ctrl+scroll: pushes the wingman nearest the pointer along the camera's
        /// view axis — the depth half of free 3D placement. Step scales with the
        /// ship so a notch moves a frigate a nudge and a battleship a stride.
        /// </summary>
        public async Task PushDepthAsync(double x, double y, double notches,
            double viewW, double viewH, CancellationToken ct = default)
        {
            var host = _host();
            int index = _dragWingman >= 0
                ? _dragWingman : PickWingman(x, y, viewW, viewH);
            if (host == null || index < 0 || index >= _wingOffsets.Count)
                return;
            var (forward, _, _) = _camera.Basis();
            double step = Math.Max(_wingRadii[index], _camera.Radius) * 0.2 * notches;
            double[] offset = _wingOffsets[index];
            offset[0] += forward[0] * step;
            offset[1] += forward[1] * step;
            offset[2] += forward[2] * step;
            await host.MoveWingmanAsync(index, offset, ct).ConfigureAwait(false);
            _requestRender(true);
        }

        /// <summary>
        /// The single-in-flight move pump: sends the accumulated drag as one engine
        /// move, and loops while more travel arrived during the send. Pointer events
        /// therefore never queue behind the sidecar — worst case a fast drag lands
        /// as fewer, larger steps.
        /// </summary>
        private async void PumpMove()
        {
            if (_movePumpBusy)
                return;
            _movePumpBusy = true;
            try
            {
                var host = _host();
                while (host != null && _dragWingman >= 0 &&
                       (Math.Abs(_dragPendingX) > 0.01 || Math.Abs(_dragPendingY) > 0.01))
                {
                    int index = _dragWingman;
                    double dx = _dragPendingX, dy = _dragPendingY;
                    _dragPendingX = _dragPendingY = 0.0;

                    double[] move = _camera.ScreenDragToWorld(dx, dy, _dragDepth, _dragViewH);
                    double[] offset = _wingOffsets[index];
                    offset[0] += move[0];
                    offset[1] += move[1];
                    offset[2] += move[2];
                    await host.MoveWingmanAsync(index, offset).ConfigureAwait(false);
                    _requestRender(false);
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrFleet: wingman drag failed: {ex.Message}");
            }
            finally
            {
                _movePumpBusy = false;
            }
        }
    }
}
