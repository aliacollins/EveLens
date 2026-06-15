// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EveLens.Common.Models;
using EveLens.Common.Service;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression guard for the hard crash on Linux/macOS where fallback/default images were
    /// read from <c>Properties.Resources.*</c> (typed <see cref="System.Drawing.Bitmap"/>).
    /// On .NET 6+ <c>System.Drawing.Common</c> throws <see cref="PlatformNotSupportedException"/>
    /// on any non-Windows platform via the GDI+ type initializer, terminating the process when a
    /// user opened the Standings / Contacts / Employment / Loyalty tabs.
    /// </summary>
    /// <remarks>
    /// The fix routes all default images through <see cref="DefaultImages"/>, which decodes
    /// embedded PNGs as cross-platform SkiaSharp <see cref="SKBitmap"/>. These tests run on every
    /// platform; on the CI Windows box they prove correctness, and on Linux/macOS they would have
    /// crashed before the fix.
    /// </remarks>
    public class NonWindowsImageCrashTests
    {
        [Fact]
        public void DefaultImages_Character_ReturnsCrossPlatformBitmap()
        {
            SKBitmap? bmp = DefaultImages.Character;

            bmp.Should().NotBeNull("the default character placeholder must decode on every platform");
            bmp!.Width.Should().BeGreaterThan(0);
            bmp.Height.Should().BeGreaterThan(0);
        }

        [Fact]
        public void DefaultImages_Corporation_ReturnsCrossPlatformBitmap()
        {
            DefaultImages.Corporation.Should().NotBeNull();
        }

        [Fact]
        public void DefaultImages_Alliance_ReturnsCrossPlatformBitmap()
        {
            DefaultImages.Alliance.Should().NotBeNull();
        }

        [Theory]
        [InlineData("standingIconTerrible.png")]
        [InlineData("standingIconBad.png")]
        [InlineData("standingIconNeutral.png")]
        [InlineData("standingIconGood.png")]
        [InlineData("standingIconExcelent.png")]
        public void DefaultImages_StandingIcons_DecodeWithoutThrowing(string fileName)
        {
            Func<SKBitmap?> act = () => DefaultImages.Load(fileName);

            act.Should().NotThrow("standing icons must decode via SkiaSharp on every platform");
            act().Should().NotBeNull($"{fileName} is an embedded resource and must decode");
        }

        [Fact]
        public void DefaultImages_MissingResource_DegradesToNull_NeverThrows()
        {
            Func<SKBitmap?> act = () => DefaultImages.Load("this-resource-does-not-exist.png");

            act.Should().NotThrow("a missing placeholder must degrade gracefully, not crash");
            act().Should().BeNull();
        }

        [Fact]
        public void DefaultImages_NullOrEmptyName_ReturnsNull()
        {
            DefaultImages.Load(null!).Should().BeNull();
            DefaultImages.Load(string.Empty).Should().BeNull();
        }

        [Fact]
        public void DefaultImages_AreCached_SameInstanceReturned()
        {
            SKBitmap? first = DefaultImages.Character;
            SKBitmap? second = DefaultImages.Character;

            ReferenceEquals(first, second).Should().BeTrue(
                "decoded placeholders are cached and shared; callers must not dispose them");
        }

        /// <summary>
        /// The crux of the regression: <see cref="Standing.GetStandingImage"/> reads default
        /// standing icons. Before the fix this returned a GDI+ <see cref="System.Drawing.Bitmap"/>
        /// and threw on non-Windows. It must now return a cross-platform bitmap without throwing.
        /// </summary>
        [Theory]
        [InlineData(-10)]
        [InlineData(-3)]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(10)]
        public void Standing_GetStandingImage_DoesNotThrowOnAnyPlatform(int standing)
        {
            Func<object?> act = () => Standing.GetStandingImage(standing);

            act.Should().NotThrow(
                "GetStandingImage must not touch System.Drawing.Common (crashes on Linux/macOS)");
            act().Should().BeOfType<SKBitmap>("the icon must be a cross-platform SkiaSharp bitmap");
        }

        /// <summary>
        /// Guards that no live model image path can produce a <see cref="System.Drawing.Bitmap"/>.
        /// Any such instance would crash the GDI+ initializer on non-Windows.
        /// </summary>
        [Fact]
        public void DefaultImages_NeverReturnSystemDrawingTypes()
        {
            object?[] images =
            {
                DefaultImages.Character,
                DefaultImages.Corporation,
                DefaultImages.Alliance,
                Standing.GetStandingImage(5),
            };

            foreach (object? image in images.Where(i => i != null))
            {
                image!.GetType().FullName.Should().NotStartWith("System.Drawing.",
                    "model images must be SkiaSharp/cross-platform, never GDI+");
            }
        }
    }
}
