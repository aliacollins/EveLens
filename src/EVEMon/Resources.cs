// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Drawing;
using System.IO;
using CommonResources = EVEMon.Common.Properties.Resources;

namespace EVEMon
{
    /// <summary>
    /// Facade class that provides access to common resources.
    /// This class delegates to EVEMon.Common.Properties.Resources.
    /// </summary>
    internal static class Resources
    {
        // Error/Warning/Info icons
        public static Bitmap Error16 => CommonResources.Error16;
        public static Bitmap Warning16 => CommonResources.Warning16;
        public static Bitmap Information16 => CommonResources.Information16;

        // Cross/Close icons
        public static Bitmap CrossBlack => CommonResources.CrossBlack;
        public static Bitmap CrossGray => CommonResources.CrossGray;

        // Magnifier
        public static Bitmap Magnifier => CommonResources.Magnifier;

        // Expand/Collapse
        public static Bitmap Expand => CommonResources.Expand;
        public static Bitmap Collapse => CommonResources.Collapse;

        // Copy
        public static Bitmap Copy => CommonResources.Copy;

        // Watch
        public static Bitmap Watch => CommonResources.Watch;

        // Bug
        public static Bitmap Bug => CommonResources.Bug;

        // Key icons
        public static Bitmap KeyWrong32 => CommonResources.KeyWrong32;
        public static Bitmap KeyGrey16 => CommonResources.KeyGrey16;
        public static Bitmap KeyGold16 => CommonResources.KeyGold16;
        public static Bitmap DefaultCharacterImage32 => CommonResources.DefaultCharacterImage32;
        public static Bitmap Medal32 => CommonResources.Medal32;

        // Skill trained sound
        public static UnmanagedMemoryStream SkillTrained => CommonResources.SkillTrained;
    }
}
