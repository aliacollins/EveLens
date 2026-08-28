// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using EveLens.Common;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Byte-format compatibility pins for Util's compression helpers, written BEFORE the
    /// SharpZipLib → System.IO.Compression swap. Two formats are load-bearing:
    ///
    /// - GZip (RFC 1952): cloud settings backups (Dropbox/GoogleDrive/OneDrive) are
    ///   gzip-compressed before upload — a backup written by an old EveLens must stay
    ///   readable forever.
    /// - zlib (RFC 1950): HTTP "deflate" Content-Encoding is zlib-WRAPPED deflate, which
    ///   is what SharpZipLib's Deflater emitted. The .NET replacement must therefore be
    ///   ZLibStream — NOT DeflateStream, whose raw RFC 1951 output would silently break
    ///   both HTTP peers and any persisted data.
    ///
    /// The static fixtures below are standard-format streams (identical framing to the
    /// old SharpZipLib output); they pin historical readability no matter which library
    /// sits behind Util.
    /// </summary>
    public class CompressionCompatibilityTests
    {
        private const string FixtureText =
            "EveLens compression compatibility fixture: EVE Online, New Eden, 2026. o7";

        // Standard RFC 1952 / RFC 1950 streams of FixtureText, generated with the
        // pre-swap format (SharpZipLib emits these exact framings).
        private const string GzipFixture =
            "H4sIAGdMfGoC/3MtS/VJzStWSM7PLShKLS7OzM8DsxNLMpMyczJLKhXSMitKSotSrRRcw1wV/PNyMvNSdRT8UssVXFNS83QUjAyMzPQU8s0B1C916kkAAAA=";
        private const string ZlibFixture =
            "eJxzLUv1Sc0rVkjOzy0oSi0uzszPA7MTSzKTMnMySyoV0jIrSkqLUq0UXMNcFfzzcjLzUnUU/FLLFVxTUvN0FIwMjMz0FPLNAe+1GSE=";

        private static byte[] Payload => Encoding.UTF8.GetBytes(FixtureText);

        #region Historical data stays readable

        [Fact]
        public void GZipUncompress_ReadsHistoricalGzipStream()
        {
            var result = Util.GZipUncompress(Convert.FromBase64String(GzipFixture)).ToArray();
            Encoding.UTF8.GetString(result).Should().Be(FixtureText,
                "cloud settings backups written by older EveLens versions must stay readable");
        }

        [Fact]
        public void InflateUncompress_ReadsHistoricalZlibStream()
        {
            var result = Util.InflateUncompress(Convert.FromBase64String(ZlibFixture)).ToArray();
            Encoding.UTF8.GetString(result).Should().Be(FixtureText,
                "deflate data has always been zlib-wrapped (RFC 1950), never raw RFC 1951");
        }

        #endregion

        #region Output framing is what external peers expect

        [Fact]
        public void GZipCompress_OutputIsStandardGzip()
        {
            byte[] compressed = Util.GZipCompress(Payload).ToArray();

            // RFC 1952 magic bytes — what cloud providers and older clients expect
            compressed[0].Should().Be(0x1f);
            compressed[1].Should().Be(0x8b);

            // and independently decodable by the BCL's gzip reader
            using var output = new MemoryStream();
            using (var gz = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
                gz.CopyTo(output);
            output.ToArray().Should().Equal(Payload);
        }

        [Fact]
        public void DeflateCompress_OutputIsZlibWrapped()
        {
            byte[] compressed = Util.DeflateCompress(Payload).ToArray();

            // RFC 1950 header: 0x78 CMF — the marker HTTP "deflate" peers require.
            // A raw-deflate (DeflateStream) regression changes this first byte.
            compressed[0].Should().Be(0x78,
                "HTTP deflate Content-Encoding means zlib-wrapped deflate, not raw deflate");

            using var output = new MemoryStream();
            using (var z = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress))
                z.CopyTo(output);
            output.ToArray().Should().Equal(Payload);
        }

        #endregion

        #region Round-trips through Util

        [Fact]
        public void GZip_RoundTrip()
        {
            var data = Util.GZipUncompress(Util.GZipCompress(Payload).ToArray()).ToArray();
            data.Should().Equal(Payload);
        }

        [Fact]
        public void Deflate_RoundTrip()
        {
            var data = Util.InflateUncompress(Util.DeflateCompress(Payload).ToArray()).ToArray();
            data.Should().Equal(Payload);
        }

        [Fact]
        public void GZip_RoundTrip_LargePayload()
        {
            // Datafile-scale payload (the xml.gzip files are multi-MB)
            byte[] large = new byte[4 * 1024 * 1024];
            new Random(42).NextBytes(large);

            var data = Util.GZipUncompress(Util.GZipCompress(large).ToArray()).ToArray();
            data.Should().Equal(large);
        }

        #endregion
    }
}
