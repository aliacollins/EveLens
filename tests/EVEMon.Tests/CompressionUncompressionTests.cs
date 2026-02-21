// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Linq;
using System.Text;
using EVEMon.Common;
using Xunit;

namespace EVEMon.Tests
{
    public static class CompressionUncompressionTests
    {
        private static readonly byte[] s_data = Encoding.UTF8.GetBytes("Data to compress and uncompress.");


        #region Tests

        /// <summary>
        /// Compress and uncompress using zlib gzip.
        /// </summary>
        [Fact]
        public static void GzipCompressAndUncompress()
        {
            var compressed = Util.GZipCompress(s_data);
            var uncompressed = Util.GZipUncompress(compressed.ToArray());

            Assert.Equal(uncompressed, s_data);
        }

        /// <summary>
        /// Compress and uncompress using zlib deflate-inflate.
        /// </summary>
        [Fact]
        public static void DeflateCompressAndInflateUncompress()
        {
            var compressed = Util.DeflateCompress(s_data);
            var uncompressed = Util.InflateUncompress(compressed.ToArray());

            Assert.Equal(uncompressed, s_data);
        }

        /// <summary>
        /// Compress using zlib gzip and uncompress using zlib uncompress automation.
        /// </summary>
        [Fact]
        public static void GzipCompressAndZlibUncompress()
        {
            var compressed = Util.GZipCompress(s_data);
            var uncompressed = Util.ZlibUncompress(compressed.ToArray());

            Assert.Equal(uncompressed, s_data);
        }

        /// <summary>
        /// Compress using zlib deflate and uncompress using zlib uncompress automation.
        /// </summary>
        [Fact]
        public static void DeflateCompressAndZlibUncompress()
        {
            var compressed = Util.DeflateCompress(s_data);
            var uncompressed = Util.ZlibUncompress(compressed.ToArray());

            Assert.Equal(uncompressed, s_data);
        }

        /// <summary>
        /// Compress using zlib gzip and uncompress using zlib uncompress automation with stream.
        /// </summary>
        [Fact]
        public static void GzipCompressAndZlibUncompressUsingStream()
        {
            var compressed = (byte[])Util.GZipCompress(s_data);
            var uncompressed = (MemoryStream)Util.ZlibUncompress(new MemoryStream(compressed));

            Assert.Equal(uncompressed.ToArray(), s_data);
        }

        /// <summary>
        /// Compress using zlib deflate and uncompress using zlib uncompress automation with stream.
        /// </summary>
        [Fact]
        public static void DeflateCompressAndZlibUncompressUsingStream()
        {
            var compressed = (byte[])Util.DeflateCompress(s_data);
            var uncompressed = (MemoryStream)Util.ZlibUncompress(new MemoryStream(compressed));

            Assert.Equal(uncompressed.ToArray(), s_data);
        }

        #endregion
    }
}
