// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The Hub's design thumbnails: a settled render frame, downscaled and kept on disk per
    /// design id, so the carousel shows real ships wearing real designs instead of grey tiles.
    /// </summary>
    /// <remarks>
    /// <para><b>Capture-on-view, not render-on-demand.</b> A thumbnail is saved as a side
    /// effect of the frame the user is already looking at — zero extra engine work, no queue
    /// to manage, and the cache fills organically as designs are browsed. A design never
    /// viewed simply keeps its placeholder, which is honest.</para>
    ///
    /// <para><b>File-per-design, hashed name.</b> SKINR ids are opaque strings from ESI; a
    /// SHA-1 of the id keeps the filename filesystem-safe without caring what CCP puts in
    /// the id. One design's thumbnail is overwritten whenever a newer settled frame arrives,
    /// so the cache self-heals after render improvements.</para>
    /// </remarks>
    public sealed class SkinrThumbnailCache
    {
        /// <summary>Longest edge of a stored thumbnail, sized for the carousel at 2x DPI.</summary>
        private const int MaxEdge = 320;

        private readonly string _directory;

        public SkinrThumbnailCache(string? directory = null)
        {
            _directory = directory ?? Path.Combine(
                AppServices.ApplicationPaths.DataDirectory, "cache", "skinr", "thumbs");
        }

        /// <summary>The filename a design's thumbnail uses — shared verbatim with the
        /// evelens.dev preview CDN, so "download it" and "render it" produce the same
        /// cache entry.</summary>
        public static string FileNameFor(string skinrId)
        {
            byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(skinrId ?? string.Empty));
            return Convert.ToHexString(hash) + ".png";
        }

        /// <summary>The on-disk path a design's thumbnail lives at, whether or not it exists yet.</summary>
        public string PathFor(string skinrId) =>
            Path.Combine(_directory, FileNameFor(skinrId));

        /// <summary>The thumbnail path when one has been captured, else null.</summary>
        public string? TryGetPath(string skinrId)
        {
            string path = PathFor(skinrId);
            return File.Exists(path) ? path : null;
        }

        /// <summary>
        /// Downscales a settled frame and stores it as the design's thumbnail. Returns the
        /// stored path, or null when encoding failed — a broken thumbnail must never break
        /// the render path that produced the frame.
        /// </summary>
        public string? Save(string skinrId, SkinrFrame frame)
        {
            if (string.IsNullOrEmpty(skinrId) || frame == null)
                return null;

            try
            {
                var info = new SKImageInfo(frame.Width, frame.Height,
                    SKColorType.Bgra8888, SKAlphaType.Unpremul);
                using var full = new SKBitmap(info);
                Marshal.Copy(frame.Pixels, 0,
                    full.GetPixels(), Math.Min(frame.Pixels.Length, info.BytesSize));

                double scale = Math.Min(1.0,
                    (double)MaxEdge / Math.Max(frame.Width, frame.Height));
                var thumbInfo = new SKImageInfo(
                    Math.Max(1, (int)(frame.Width * scale)),
                    Math.Max(1, (int)(frame.Height * scale)),
                    SKColorType.Bgra8888, SKAlphaType.Unpremul);
                using SKBitmap thumb = full.Resize(thumbInfo,
                    new SKSamplingOptions(SKCubicResampler.Mitchell)) ?? full.Copy();

                Directory.CreateDirectory(_directory);
                string path = PathFor(skinrId);
                string tmp = path + ".tmp";
                using (SKImage image = SKImage.FromBitmap(thumb))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 90))
                using (FileStream stream = File.Create(tmp))
                {
                    data.SaveTo(stream);
                }
                File.Move(tmp, path, overwrite: true);
                return path;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: thumbnail save failed for {skinrId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Stores an already-encoded image fetched from the community preview CDN.
        /// Network content is validated, not trusted: the bytes must decode as a real
        /// image of sane dimensions or nothing is written. Returns the stored path.
        /// </summary>
        public string? SaveBytes(string skinrId, byte[] encoded)
        {
            if (string.IsNullOrEmpty(skinrId) || encoded == null || encoded.Length == 0)
                return null;
            try
            {
                using SKBitmap? decoded = SKBitmap.Decode(encoded);
                if (decoded == null || decoded.Width < 8 || decoded.Height < 8)
                    return null;
                Directory.CreateDirectory(_directory);
                string path = PathFor(skinrId);
                string tmp = path + ".tmp";
                File.WriteAllBytes(tmp, encoded);
                File.Move(tmp, path, overwrite: true);
                return path;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"Skinr: CDN thumbnail save failed for {skinrId}: {ex.Message}");
                return null;
            }
        }
    }
}
