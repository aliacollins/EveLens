// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// The SKINR stage's orbit camera: yaw/pitch/distance state, the gesture math
    /// (orbit, zoom, clamps), flick-inertia velocity tracking, and the ship-spin
    /// counter. Split from <see cref="SkinrRenderViewModel"/> along its natural seam
    /// (Law 2): the render VM owns the sidecar, the loop and the frames; this owns
    /// where the camera is and how gestures move it. Deliberately NOT a ViewModelBase —
    /// it holds no subscriptions and raises no property changes; it is camera state
    /// plus pure math, testable without any framework.
    /// </summary>
    /// <remarks>
    /// Threading contract mirrors the fields' old home in the VM: gesture methods run
    /// on the UI thread; the render loop and animation tasks read the doubles from
    /// workers, which is benign for the same reason it always was (torn reads of a
    /// camera angle produce a one-frame smear at worst, and doubles on x64 don't tear).
    /// </remarks>
    public sealed class SkinrStageCamera
    {
        // Orbit limits. Pitch stops short of the poles because a camera looking straight down its
        // own up-vector has no defined orientation and the view rolls unpredictably as it passes.
        // CCP's own studio camera allows 0.001 rad from either pole
        // (skinrShipSceneContainerCamera: kMinPitch 0.001, kMaxPitch pi-0.001, i.e.
        // ±89.94° about level). ±85 felt like hitting a wall a fifth of a turn early on
        // every vertical drag. 89.4 stays inside the sidecar's own ±89.5 degeneracy
        // guard on the up vector.
        private const double MinPitch = -89.4;
        private const double MaxPitch = 89.4;

        // The far end is CCP's own studio limit (shipSKINRSceneContainer ZOOM_RANGE
        // [1.6, 3.6]) and it is load-bearing, not taste: past ~4 radii the hull is so
        // small on screen that the glow strips' bloom envelope — a fixed fraction of
        // the frame — swallows the whole ship in a milky grey wash. The game never
        // shows that veil because the game never lets you get there. The near end
        // stays at 1.2 (CCP use 1.6): close inspection has no such artefact and the
        // detail is the point of a viewer.
        private const double MinDistanceFactor = 1.2;
        private const double MaxDistanceFactor = 3.6;

        // 8% per notch rather than 15%. A wheel notch is the coarsest input the pane takes, and
        // at 15% a three-notch flick more than halves the distance — which reads as a jump even
        // when every intermediate frame is drawn.
        private const double ZoomPerNotch = 0.92;

        // A drag across a 1024px pane is roughly a full turn, which is what makes the
        // gesture feel like the ship is under the cursor rather than geared to it.
        private const double DegreesPerPixel = 0.35;

        // The release velocity comes from the drag's own recent motion: samples older
        // than the window are a hold, not a flick, so a drag that stops before letting
        // go releases a stationary ship. The floor separates "flick" from the tremor of
        // an ordinary release; the cap keeps a wild swipe to a fast-but-followable spin.
        private const int FlickWindowMs = 120;
        private const double FlickMinSpeed = 30.0;    // degrees/second
        private const double FlickMaxSpeed = 480.0;   // degrees/second

        private double _yaw = 35.0;
        private double _pitch = 15.0;
        private double _distance;
        private double _radius = 1.0;
        private double _fleetSpan;
        private double _spinAccumDegrees;

        /// <summary>The sidecar's default projection FOV in degrees — the value the
        /// engine uses when the camera op sends none. Mirrored here because the
        /// screen↔world drag math must divide by the same tangent the projection
        /// multiplied by.</summary>
        public const double FovDegrees = 55.0;

        // UI-thread only (pointer events and release), like the gestures that feed it.
        private readonly List<OrbitSample> _orbitSamples = new();

        /// <summary>One drag movement: when it landed and the rotation it applied.</summary>
        internal readonly record struct OrbitSample(
            long TimeMs, double YawDelta, double PitchDelta);

        public double Yaw => _yaw;
        public double Pitch => _pitch;
        public double Distance => _distance;

        /// <summary>The built hull's bounding radius in world units (metres); 1.0
        /// before any build.</summary>
        public double Radius => _radius;

        /// <summary>The active environment — it decides the zoom ceiling and, in the
        /// hangar, the pad-plane pitch floor.</summary>
        public SkinrEnvironmentPreset EnvironmentPreset { get; set; } =
            SkinrEnvironmentPreset.Studio;

        /// <summary>Full 360° camera revolutions completed — the docked capsuleer's
        /// oldest pastime, honoured the way the game client honours it.</summary>
        public int SpinCount { get; private set; }

        /// <summary>Raised with the new total each time a revolution completes. May be
        /// raised off the UI thread (the inertia glide spins too).</summary>
        public event Action<int>? SpinCountChanged;

        /// <summary>
        /// The zoom-out ceiling depends on where the ship is: the studio's 3.6 radii is
        /// CCP's own limit and load-bearing there (the bloom veil), but inside the
        /// hangar bay or under a nebula the whole POINT of pulling back is seeing the
        /// ship in its place.
        /// </summary>
        private double CurrentMaxDistanceFactor =>
            EnvironmentPreset is SkinrEnvironmentPreset.Hangar or SkinrEnvironmentPreset.Space
                ? 16.0
                : MaxDistanceFactor;

        /// <summary>A freshly built hull: adopt its radius and frame it at three radii.</summary>
        public void SetHull(double radius)
        {
            _radius = radius > 0 ? radius : 1.0;
            _fleetSpan = 0.0;
            _distance = _radius * 3.0;
        }

        /// <summary>
        /// A photo-op formation is on stage: extend the zoom-OUT ceiling to cover its
        /// half-span and pull back to frame it. The hull radius — and with it the
        /// zoom-IN floor — is deliberately untouched: assembling a fleet must never
        /// cost the ability to fly the camera right up to one ship's plating.
        /// </summary>
        public void FrameFleet(double span)
        {
            _fleetSpan = Math.Max(0.0, span);
            if (_fleetSpan > 0.0)
                _distance = Math.Clamp(Math.Max(_distance, _fleetSpan * 1.8),
                    _radius * MinDistanceFactor, MaxDistance);
        }

        /// <summary>The formation is gone; the ceiling contracts back to the hull's.</summary>
        public void ClearFleetFraming()
        {
            _fleetSpan = 0.0;
            _distance = Math.Clamp(_distance,
                _radius * MinDistanceFactor, MaxDistance);
        }

        private double MaxDistance =>
            Math.Max(_radius * CurrentMaxDistanceFactor, _fleetSpan * 3.5);

        /// <summary>Returns to the default three-quarter view.</summary>
        public void Reset()
        {
            _yaw = 35.0;
            _pitch = 15.0;
            _distance = _radius * 3.0;
        }

        /// <summary>Writes an animated pose (the intro sweep). Pitch and distance are
        /// clamped to the live limits so a script can never park the camera somewhere
        /// a gesture couldn't reach.</summary>
        public void Set(double yaw, double pitch, double distance)
        {
            _yaw = Wrap(yaw);
            _pitch = Math.Clamp(pitch, EffectiveMinPitch(), MaxPitch);
            _distance = Math.Clamp(distance,
                _radius * MinDistanceFactor, MaxDistance);
        }

        /// <summary>Applies a drag as an orbit; deltas are pixels.</summary>
        public void Orbit(double deltaXPixels, double deltaYPixels)
        {
            double pitchBefore = _pitch;
            _yaw = Wrap(_yaw + deltaXPixels * DegreesPerPixel);
            _pitch = Math.Clamp(_pitch - deltaYPixels * DegreesPerPixel,
                EffectiveMinPitch(), MaxPitch);
            // The APPLIED pitch delta, so a drag pinned against the pole or the hangar's
            // pad floor releases with zero pitch velocity instead of a phantom flick into
            // the wall. Yaw has no clamp, only the wrap, so its raw delta is the truth.
            RecordOrbitSample(deltaXPixels * DegreesPerPixel, _pitch - pitchBefore);
            AccumulateSpin(deltaXPixels * DegreesPerPixel);
        }

        /// <summary>
        /// Applies a wheel notch as a zoom. Multiplicative, not additive: a fixed step
        /// feels glacial when far out and lurches when close in, because what the eye
        /// reads as "closer" is a ratio.
        /// </summary>
        public void Zoom(double notches)
        {
            double factor = Math.Pow(ZoomPerNotch, notches);
            _distance = Math.Clamp(_distance * factor,
                _radius * MinDistanceFactor, MaxDistance);
            // The pad-plane floor tightens as the camera pulls back (same angle, more
            // depth below the deck), so a zoom-out at a low angle rides the camera up
            // over the pad instead of sinking it through the plating.
            ClampPitchToFloor();
        }

        /// <summary>Re-asserts the live pitch floor — entering the hangar with the
        /// camera parked below the pad plane would show the deck's underside.</summary>
        public void ClampPitchToFloor() =>
            _pitch = Math.Clamp(_pitch, EffectiveMinPitch(), MaxPitch);

        /// <summary>
        /// One step of the post-flick glide: advances yaw (which spins the counter too)
        /// and pitch by the velocities over <paramref name="dtSeconds"/>. Hitting the
        /// pole or the hangar's pad plane absorbs the vertical component — the spin
        /// continues flat, which is how a globe feels when you flick it into its stand.
        /// </summary>
        public void GlideStep(double yawPerSec, ref double pitchPerSec, double dtSeconds)
        {
            _yaw = Wrap(_yaw + yawPerSec * dtSeconds);
            AccumulateSpin(yawPerSec * dtSeconds);
            double floor = EffectiveMinPitch();
            double next = _pitch + pitchPerSec * dtSeconds;
            if (next <= floor || next >= MaxPitch)
                pitchPerSec = 0.0;
            _pitch = Math.Clamp(next, floor, MaxPitch);
        }

        /// <summary>The flick velocity implied by the drag that just released, and the
        /// end of that drag's sample window. (0, 0) when the release is a stop.</summary>
        public (double YawPerSec, double PitchPerSec) ReleaseFlick()
        {
            (double, double) v = FlickVelocity(_orbitSamples, Environment.TickCount64);
            _orbitSamples.Clear();
            return v;
        }

        /// <summary>A fresh grab: whatever the previous drag left in the window is history.</summary>
        public void ClearFlickSamples() => _orbitSamples.Clear();

        private void RecordOrbitSample(double yawDelta, double pitchDelta)
        {
            long now = Environment.TickCount64;
            _orbitSamples.Add(new OrbitSample(now, yawDelta, pitchDelta));
            _orbitSamples.RemoveAll(s => now - s.TimeMs > FlickWindowMs);
        }

        /// <summary>
        /// Net signed yaw travel; a full turn in either direction counts one spin.
        /// Signed (not absolute) so wiggling in place never scores — only rotation
        /// that actually goes somewhere does, drag and inertia glide alike.
        /// </summary>
        private void AccumulateSpin(double yawDeltaDegrees)
        {
            _spinAccumDegrees += yawDeltaDegrees;
            while (Math.Abs(_spinAccumDegrees) >= 360.0)
            {
                _spinAccumDegrees -= 360.0 * Math.Sign(_spinAccumDegrees);
                SpinCount++;
                SpinCountChanged?.Invoke(SpinCount);
            }
        }

        private double EffectiveMinPitch() =>
            MinPitchForEnvironment(EnvironmentPreset, _radius, _distance);

        /// <summary>
        /// The lowest pitch the camera may take for a given preset, hull radius and orbit
        /// distance. Everywhere but the hangar this is the pole guard; inside the bay the
        /// floor is the landing pad. The sidecar sinks the bay by max(60, 1.25×radius)
        /// below the orbit centre (use_hangar_backdrop — the formula is mirrored there,
        /// change both), so a camera whose height drops below that plane is orbiting under
        /// the deck, looking up through the station's plumbing. The 0.9 margin keeps the
        /// lens skimming just above the plating rather than exactly in it. Close in
        /// (distance inside the drop) no angle can reach the plane, so the full range stays.
        /// </summary>
        internal static double MinPitchForEnvironment(
            SkinrEnvironmentPreset preset, double hullRadius, double distance)
        {
            if (preset != SkinrEnvironmentPreset.Hangar)
                return MinPitch;
            double floor = Math.Max(60.0, hullRadius * 1.25) * 0.9;
            if (distance <= floor)
                return MinPitch;
            return -Math.Asin(floor / distance) * 180.0 / Math.PI;
        }

        /// <summary>
        /// The release velocity in degrees/second implied by the drag's last
        /// <see cref="FlickWindowMs"/> of motion, or (0, 0) when the release is a stop —
        /// too few samples, everything stale, or motion under <see cref="FlickMinSpeed"/>.
        /// Static and pure so the flick contract is testable without pointer events.
        /// </summary>
        internal static (double YawPerSec, double PitchPerSec) FlickVelocity(
            IReadOnlyList<OrbitSample> samples, long nowMs)
        {
            double yaw = 0.0, pitch = 0.0;
            long oldest = nowMs;
            int counted = 0;
            foreach (OrbitSample s in samples)
            {
                if (nowMs - s.TimeMs > FlickWindowMs)
                    continue;
                yaw += s.YawDelta;
                pitch += s.PitchDelta;
                if (s.TimeMs < oldest)
                    oldest = s.TimeMs;
                counted++;
            }
            // One sample is a twitch, not a trajectory.
            if (counted < 2)
                return (0.0, 0.0);
            double seconds = Math.Max(16L, nowMs - oldest) / 1000.0;
            double yawPerSec = yaw / seconds, pitchPerSec = pitch / seconds;
            double speed = Math.Sqrt(yawPerSec * yawPerSec + pitchPerSec * pitchPerSec);
            if (speed < FlickMinSpeed)
                return (0.0, 0.0);
            if (speed > FlickMaxSpeed)
            {
                yawPerSec *= FlickMaxSpeed / speed;
                pitchPerSec *= FlickMaxSpeed / speed;
            }
            return (yawPerSec, pitchPerSec);
        }

        // --- fleet drag math ---------------------------------------------------
        //
        // The sidecar's view is Trinity's LookAt with up (0,1,0) and the eye at
        //   at + distance * (sin yaw · cos pitch, sin pitch, cos yaw · cos pitch).
        // From that orbit pose these derive the camera basis in world axes, so a
        // pointer drag can move a wingman in the exact plane the user is looking at.
        // All pure math on (yaw, pitch, distance) — testable without an engine.

        /// <summary>Camera basis vectors in world space for the current orbit pose:
        /// Forward (eye→target), Right (screen +x), Up (screen +y).</summary>
        internal (double[] Forward, double[] Right, double[] Up) Basis()
        {
            double yaw = _yaw * Math.PI / 180.0, pitch = _pitch * Math.PI / 180.0;
            double sy = Math.Sin(yaw), cy = Math.Cos(yaw);
            double sp = Math.Sin(pitch), cp = Math.Cos(pitch);
            // Right-handed, MEASURED (probe220): a wingman parked at +right*K renders
            // right of centre and +up*K renders above it with exactly these vectors.
            double[] forward = { -sy * cp, -sp, -cy * cp };
            double[] right = { cy, 0.0, -sy };
            double[] up = { -sp * sy, cp, -sp * cy };
            return (forward, right, up);
        }

        /// <summary>
        /// Where a world offset (relative to the orbit target) lands on screen, plus
        /// its depth along the view axis. Positions behind the camera report a
        /// non-positive depth — callers must treat those as unpickable.
        /// </summary>
        public (double X, double Y, double Depth) Project(IReadOnlyList<double> offset,
            double viewportWidth, double viewportHeight)
        {
            var (f, r, u) = Basis();
            // Eye sits at -forward * distance from the target.
            double vx = offset[0] + f[0] * _distance;
            double vy = offset[1] + f[1] * _distance;
            double vz = offset[2] + f[2] * _distance;
            double depth = vx * f[0] + vy * f[1] + vz * f[2];
            if (depth <= 0.0)
                return (double.NaN, double.NaN, depth);
            double tanV = Math.Tan(FovDegrees * Math.PI / 360.0);
            double tanH = tanV * (viewportWidth / Math.Max(1.0, viewportHeight));
            double sx = (vx * r[0] + vy * r[1] + vz * r[2]) / (depth * tanH);
            double sy = (vx * u[0] + vy * u[1] + vz * u[2]) / (depth * tanV);
            return (viewportWidth * 0.5 * (1.0 + sx),
                    viewportHeight * 0.5 * (1.0 - sy), depth);
        }

        /// <summary>
        /// The world-space move that keeps a ship at <paramref name="depth"/> under a
        /// pointer travelling (dx, dy) pixels — the drag stays glued to the cursor at
        /// any zoom because the pixel size is measured at that ship's own depth.
        /// </summary>
        public double[] ScreenDragToWorld(double dxPixels, double dyPixels,
            double depth, double viewportHeight)
        {
            var (_, r, u) = Basis();
            double perPixel = 2.0 * Math.Max(0.0, depth) *
                Math.Tan(FovDegrees * Math.PI / 360.0) / Math.Max(1.0, viewportHeight);
            double mx = dxPixels * perPixel, my = -dyPixels * perPixel;
            return new[]
            {
                r[0] * mx + u[0] * my,
                r[1] * mx + u[1] * my,
                r[2] * mx + u[2] * my
            };
        }

        /// <summary>Keeps yaw in [0, 360) so a long spinning session never walks the
        /// value toward float imprecision.</summary>
        private static double Wrap(double degrees)
        {
            degrees %= 360.0;
            return degrees < 0 ? degrees + 360.0 : degrees;
        }
    }
}
