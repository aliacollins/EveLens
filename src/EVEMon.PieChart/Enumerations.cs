// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.PieChart
{
    /// <summary>
    ///   Enumeration for different shadow styles
    /// </summary>
    public enum ShadowStyle
    {
        /// <summary>
        ///   No shadow. Sides are drawn in the same color as the top od the 
        ///   pie.
        /// </summary>
        NoShadow,

        /// <summary>
        ///   Uniform shadow. Sides are drawn somewhat darker.
        /// </summary>
        UniformShadow,

        /// <summary>
        ///   Gradual shadow is used to fully simulate 3-D shadow.
        /// </summary>
        GradualShadow
    }

    /// <summary>
    ///   Enumeration for edge color types.
    /// </summary>
    public enum EdgeColorType
    {
        /// <summary>
        ///   Edges are not drawn at all.
        /// </summary>
        NoEdge,

        /// <summary>
        ///   System (window text) color is used to draw edges.
        /// </summary>
        SystemColor,

        /// <summary>
        ///   Surface color is used for edges.
        /// </summary>
        SurfaceColor,

        /// <summary>
        ///   A color that is little darker than surface color is used for
        ///   edges.
        /// </summary>
        DarkerThanSurface,

        /// <summary>
        ///   A color that is significantly darker than surface color is used 
        ///   for edges.
        /// </summary>
        DarkerDarkerThanSurface,

        /// <summary>
        ///   A color that is little lighter than surface color is used for
        ///   edges.
        /// </summary>
        LighterThanSurface,

        /// <summary>
        ///   A color that is significantly lighter than surface color is used 
        ///   for edges.
        /// </summary>
        LighterLighterThanSurface,

        /// <summary>
        ///   Contrast color is used for edges.
        /// </summary>
        Contrast,

        /// <summary>
        ///   Enhanced contrast color is used for edges.
        /// </summary>
        EnhancedContrast,

        /// <summary>
        ///   Black color is used for light surfaces and white for dark 
        ///   surfaces.
        /// </summary>
        FullContrast
    }
}