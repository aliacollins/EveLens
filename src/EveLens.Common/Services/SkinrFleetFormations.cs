// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;

namespace EveLens.Common.Services
{
    /// <summary>The photo-op formation shapes a fleet can assemble into.</summary>
    public enum SkinrFleetFormation
    {
        /// <summary>The wedge: primary at the tip, wingmen fanning back left and right.</summary>
        Vic,

        /// <summary>One lateral line, wings level with the primary.</summary>
        LineAbreast,

        /// <summary>A single diagonal — every ship offset right and back of the one ahead.</summary>
        Echelon,

        /// <summary>Line astern — the conga line, with a slight weave so no ship hides
        /// exactly behind another.</summary>
        Column,

        /// <summary>The wall: ships stacked two rows tall as well as wide.</summary>
        Wall
    }

    /// <summary>
    /// Slot math for photo-op formations. Pure functions of the measured hull radii:
    /// spacing derives from each ship's real bounding sphere, so a shuttle and a
    /// battleship pack correctly in the same shot. Offsets are world-axis metres
    /// relative to the primary: +x starboard, +y up, −z astern (all hulls face the
    /// same way on the SKINR stage).
    /// </summary>
    public static class SkinrFleetFormations
    {
        public static IReadOnlyList<SkinrFleetFormation> All { get; } = new[]
        {
            SkinrFleetFormation.Vic,
            SkinrFleetFormation.LineAbreast,
            SkinrFleetFormation.Echelon,
            SkinrFleetFormation.Column,
            SkinrFleetFormation.Wall
        };

        /// <summary>Localization key for a formation's display name.</summary>
        public static string NameKey(SkinrFleetFormation formation) =>
            "Skinr.Formation" + formation;

        /// <summary>
        /// Computes one slot per wingman. Slots on the same flank park beyond the
        /// running outer edge of everything already there, so mixed hull sizes can
        /// never overlap regardless of the order ships were picked in.
        /// </summary>
        public static IReadOnlyList<double[]> ComputeSlots(SkinrFleetFormation formation,
            double primaryRadius, IReadOnlyList<double> radii)
        {
            double r0 = Math.Max(1.0, primaryRadius);
            double clearance = Math.Max(40.0, r0 * 0.35);
            var slots = new List<double[]>(radii.Count);

            switch (formation)
            {
                case SkinrFleetFormation.LineAbreast:
                {
                    double left = r0, right = r0;
                    for (int i = 0; i < radii.Count; i++)
                    {
                        double r = radii[i];
                        bool port = i % 2 == 0;
                        double edge = port ? left : right;
                        double x = (edge + clearance + r) * (port ? -1.0 : 1.0);
                        if (port) left = Math.Abs(x) + r; else right = Math.Abs(x) + r;
                        slots.Add(new[] { x, 0.0, 0.0 });
                    }
                    break;
                }
                case SkinrFleetFormation.Vic:
                {
                    // Lateral like line-abreast, then swept back proportionally to the
                    // lateral offset — a true V whatever the mix of hull sizes.
                    double left = r0, right = r0;
                    for (int i = 0; i < radii.Count; i++)
                    {
                        double r = radii[i];
                        bool port = i % 2 == 0;
                        double edge = port ? left : right;
                        double x = (edge + clearance + r) * (port ? -1.0 : 1.0);
                        if (port) left = Math.Abs(x) + r; else right = Math.Abs(x) + r;
                        slots.Add(new[] { x, 0.0, -Math.Abs(x) * 0.8 });
                    }
                    break;
                }
                case SkinrFleetFormation.Echelon:
                {
                    double edge = r0;
                    for (int i = 0; i < radii.Count; i++)
                    {
                        double r = radii[i];
                        double x = edge + clearance + r;
                        edge = x + r;
                        slots.Add(new[] { x, 0.0, -x * 0.7 });
                    }
                    break;
                }
                case SkinrFleetFormation.Column:
                {
                    double edge = r0;
                    for (int i = 0; i < radii.Count; i++)
                    {
                        double r = radii[i];
                        double z = -(edge + clearance + r);
                        edge = Math.Abs(z) + r;
                        // The weave: enough lateral offset that rank four is a ship in
                        // frame instead of a silhouette eclipsed by rank one.
                        double x = (i % 2 == 0 ? -1.0 : 1.0) * r0 * 0.35;
                        slots.Add(new[] { x, 0.0, z });
                    }
                    break;
                }
                case SkinrFleetFormation.Wall:
                default:
                {
                    // Two ships share each lateral column, one high, one low; columns
                    // alternate flanks outward, all in the primary's plane (z = 0).
                    double left = r0, right = r0;
                    for (int i = 0; i < radii.Count; i += 2)
                    {
                        double rA = radii[i];
                        double rB = i + 1 < radii.Count ? radii[i + 1] : 0.0;
                        double rCol = Math.Max(rA, rB);
                        bool port = (i / 2) % 2 == 0;
                        double edge = port ? left : right;
                        double x = (edge + clearance + rCol) * (port ? -1.0 : 1.0);
                        if (port) left = Math.Abs(x) + rCol; else right = Math.Abs(x) + rCol;

                        if (i + 1 < radii.Count)
                        {
                            double sep = (rA + rB + clearance) * 0.5;
                            slots.Add(new[] { x, sep, 0.0 });
                            slots.Add(new[] { x, -sep, 0.0 });
                        }
                        else
                        {
                            slots.Add(new[] { x, 0.0, 0.0 });
                        }
                    }
                    break;
                }
            }
            return slots;
        }

        /// <summary>
        /// The formation's half-span from the primary: the farthest any ship's hull
        /// reaches on any axis. This is what the camera must pull back to cover.
        /// </summary>
        public static double Span(double primaryRadius,
            IReadOnlyList<double> radii, IReadOnlyList<double[]> slots)
        {
            double span = Math.Max(1.0, primaryRadius);
            for (int i = 0; i < slots.Count && i < radii.Count; i++)
            {
                double[] s = slots[i];
                double reach = Math.Sqrt(s[0] * s[0] + s[1] * s[1] + s[2] * s[2]) + radii[i];
                if (reach > span)
                    span = reach;
            }
            return span;
        }
    }
}
