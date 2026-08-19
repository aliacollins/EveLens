// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.InteropServices;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Which platforms the Trinity-based 3D preview can support. Honest by design:
    /// Trinity ships DirectX (Windows) and Metal (macOS) backends only — there is no
    /// Linux renderer to offer — and our macOS builds target Apple Silicon only.
    /// The rest of the SKINR window (inventory, recipes, market data) works everywhere.
    /// </summary>
    public enum SkinrRenderSupport
    {
        /// <summary>Windows x64 — DirectX 11 backend.</summary>
        Supported,

        /// <summary>Apple Silicon macOS — Metal backend, planned after Windows.</summary>
        MacArmPlanned,

        /// <summary>Intel macOS — we only build osx-arm64 artifacts.</summary>
        UnsupportedMacIntel,

        /// <summary>Linux — Trinity has no Vulkan/GL backend to build on.</summary>
        UnsupportedLinux,

        /// <summary>Anything else (e.g. Windows ARM) — no renderer target.</summary>
        Unsupported
    }

    public static class SkinrRenderPlatform
    {
        /// <summary>Support level for the machine EveLens is running on.</summary>
        public static SkinrRenderSupport Current => Classify(
            OperatingSystemKind(), RuntimeInformation.OSArchitecture);

        /// <summary>Pure classification, testable without platform shims.</summary>
        public static SkinrRenderSupport Classify(string os, Architecture arch) => os switch
        {
            "windows" when arch == Architecture.X64 => SkinrRenderSupport.Supported,
            "macos" when arch == Architecture.Arm64 => SkinrRenderSupport.MacArmPlanned,
            "macos" => SkinrRenderSupport.UnsupportedMacIntel,
            "linux" => SkinrRenderSupport.UnsupportedLinux,
            _ => SkinrRenderSupport.Unsupported
        };

        private static string OperatingSystemKind()
        {
            if (System.OperatingSystem.IsWindows()) return "windows";
            if (System.OperatingSystem.IsMacOS()) return "macos";
            if (System.OperatingSystem.IsLinux()) return "linux";
            return "other";
        }
    }
}
