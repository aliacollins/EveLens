// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// The render-runtime add-on's verification chain: the node↔.NET signature
    /// interop (the exact bytes sign.js produces must verify here), and the signed
    /// manifest's accept/reject decisions over a synthetic install tree.
    /// </summary>
    public sealed class SkinrRuntimeInstallerTests : IDisposable
    {
        // Produced ONCE by node:crypto with ieee-p1363 encoding over the payload
        // below — the actual cross-boundary contract, not a .NET-signed stand-in.
        // If .NET ever changes its default ECDSA signature format, this fails
        // loudly instead of every real package failing quietly in the field.
        private const string NodePublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE1E3PIOnQDKEAPzZB9Ye5iZzXR1iG
OY9lf+6Ycy246s0Zsp6FW8aS0qYeNam6rJpprTmqacZ4NhILv8Sj2ARukQ==
-----END PUBLIC KEY-----";
        private const string NodePayload = "evelens-render-runtime interop test vector v1";
        private const string NodeSignatureBase64 =
            "oPXyT98zy8Ubg+ZOGigN/YfShrab2Rdpcjh/9N2i6LQzb4ktSwkrs87qAed9F7+s22OUE+0i+GuQnCe/BHvlWQ==";

        private readonly string _root = Path.Combine(Path.GetTempPath(),
            "evelens-test-runtime-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); }
            catch (Exception) { /* temp cleanup is best effort */ }
        }

        [Fact]
        public void NodeSignature_VerifiesInDotNet()
        {
            bool ok = SkinrRuntimeInstaller.VerifySignature(
                Encoding.UTF8.GetBytes(NodePayload),
                Convert.FromBase64String(NodeSignatureBase64),
                NodePublicKeyPem);
            ok.Should().BeTrue("sign.js's ieee-p1363 output is the wire format");
        }

        [Fact]
        public void NodeSignature_RejectsTamperedPayload()
        {
            bool ok = SkinrRuntimeInstaller.VerifySignature(
                Encoding.UTF8.GetBytes(NodePayload + "!"),
                Convert.FromBase64String(NodeSignatureBase64),
                NodePublicKeyPem);
            ok.Should().BeFalse();
        }

        [Fact]
        public void StagedTree_Valid_Passes_AndTamperedFile_Fails()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            string pubPem = key.ExportSubjectPublicKeyInfoPem();
            WriteTree(key, ("renderer/core.bin", "the goods"), ("EULA.md", "terms"));

            // The honest tree verifies.
            var verify = () => SkinrRuntimeInstaller.VerifyStagedTree(_root, pubPem);
            verify.Should().NotThrow();

            // One flipped byte in a listed file must be fatal.
            File.WriteAllText(Path.Combine(_root, "renderer", "core.bin"), "the Goods");
            verify.Should().Throw<InvalidOperationException>()
                .WithMessage("*was altered*");
        }

        [Fact]
        public void StagedTree_BadSignature_Fails()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            WriteTree(key, ("a.txt", "hello"));
            // Verify against a DIFFERENT key: the signature must not transfer.
            using var otherKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var verify = () => SkinrRuntimeInstaller.VerifyStagedTree(
                _root, otherKey.ExportSubjectPublicKeyInfoPem());
            verify.Should().Throw<InvalidOperationException>()
                .WithMessage("*signature*");
        }

        [Fact]
        public void StagedTree_PathEscape_Fails()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            // A hostile manifest naming a path outside the tree must be refused
            // outright — never hashed, never trusted.
            WriteTree(key, ("../escape.txt", "outside"));
            var verify = () => SkinrRuntimeInstaller.VerifyStagedTree(
                _root, key.ExportSubjectPublicKeyInfoPem());
            verify.Should().Throw<InvalidOperationException>()
                .WithMessage("*outside its tree*");
        }

        [Fact]
        public void StagedTree_NewerProtocol_Fails()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            WriteTree(key, SkinrRuntimeInstaller.SupportedProtocolVersion + 1,
                ("a.txt", "hello"));
            var verify = () => SkinrRuntimeInstaller.VerifyStagedTree(
                _root, key.ExportSubjectPublicKeyInfoPem());
            verify.Should().Throw<InvalidOperationException>()
                .WithMessage("*update EveLens*");
        }

        /// <summary>Builds a synthetic install tree with a correctly signed manifest.</summary>
        private void WriteTree(ECDsa key, params (string Rel, string Content)[] files) =>
            WriteTree(key, SkinrRuntimeInstaller.SupportedProtocolVersion, files);

        private void WriteTree(ECDsa key, int protocolVersion,
            params (string Rel, string Content)[] files)
        {
            Directory.CreateDirectory(_root);
            var hashes = new System.Collections.Generic.SortedDictionary<string, string>();
            foreach ((string rel, string content) in files)
            {
                // Escaping entries go into the MANIFEST only — the verifier must
                // refuse them before ever touching the path, and the test must not
                // actually scatter files outside its temp root.
                if (!rel.StartsWith("..", StringComparison.Ordinal))
                {
                    string full = Path.GetFullPath(Path.Combine(_root, rel));
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                    File.WriteAllText(full, content);
                }
                hashes[rel.Replace('\\', '/')] = Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
            }

            byte[] manifest = JsonSerializer.SerializeToUtf8Bytes(new
            {
                name = "evelens-render-runtime",
                version = "0.0.0-test",
                protocolVersion,
                files = hashes
            });
            File.WriteAllBytes(Path.Combine(_root, "manifest.json"), manifest);
            byte[] sig = key.SignData(manifest, HashAlgorithmName.SHA256);
            File.WriteAllText(Path.Combine(_root, "manifest.json.sig"),
                Convert.ToBase64String(sig));
        }
    }
}
