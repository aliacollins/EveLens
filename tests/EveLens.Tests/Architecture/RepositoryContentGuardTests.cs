// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Architecture
{
    /// <summary>
    /// Guards the set of files tracked by git. Every rule reads the tracked file list, so build
    /// output and other untracked files are never inspected. Outside a git checkout the tracked
    /// list is empty and every rule passes.
    ///
    /// Scripts, data files, build files and packages are each pinned to an explicit list:
    /// adding one requires adding it to the list in this file.
    /// </summary>
    public class RepositoryContentGuardTests
    {
        private static readonly string ProjectRoot = FindProjectRoot();
        private static readonly Lazy<IReadOnlyList<string>> s_tracked = new(LoadTrackedFiles);

        private static readonly HashSet<string> s_forbiddenExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".so", ".dylib", ".msi", ".msix", ".appx", ".scr", ".com", ".sys", ".ocx",
            ".cpl", ".pyd", ".pyc", ".wasm", ".node", ".jar", ".class", ".lib", ".a", ".o", ".obj",
            ".bin", ".nupkg", ".snupkg", ".zip", ".7z", ".rar", ".tar", ".gz", ".tgz", ".bz2", ".xz",
            ".cab", ".iso", ".dmg", ".pkg", ".deb", ".rpm", ".appimage", ".chm", ".hta", ".lnk",
            ".bat", ".cmd", ".vbs", ".vbe", ".wsf", ".jse", ".psm1", ".psd1",
            ".pfx", ".p12", ".snk", ".pem", ".key", ".sqlite", ".db",
        };

        private static readonly HashSet<string> s_scriptExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ps1", ".sh", ".py", ".js", ".mjs", ".cjs", ".ts", ".rb", ".pl", ".lua",
        };

        private static readonly HashSet<string> s_allowedScripts = new(StringComparer.Ordinal)
        {
            "installer/linux/AppRun",
            "scripts/codegraph.ps1",
            "scripts/diag-stream.sh",
            "scripts/install-hooks.ps1",
            "scripts/make-appimage.sh",
            "scripts/make-dmg.sh",
            "scripts/make-macapp.sh",
            "scripts/promote-dashboard.ps1",
            "scripts/promote.ps1",
            "scripts/release.ps1",
            "scripts/update-sde.ps1",
            "scripts/validate-codegraph.ps1",
            "services/report-webhook/index.js",
            "tools/generate-translations.ps1",
            "tools/parse-sde.ps1",
            "tools/validate_evemon_export.py",
            "tools/validate_training_time.py",
        };

        private static readonly HashSet<string> s_allowedDataFiles = new(StringComparer.Ordinal)
        {
            "src/EveLens.Common/Resources/eve-blueprints-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-certificates-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-geography-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-items-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-masteries-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-properties-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-reprocessing-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-skills-en-US.xml.gzip",
            "src/EveLens.Common/Resources/eve-translations-ko.xml.gzip",
            "src/EveLens.Common/Resources/eve-translations-zh-CN.xml.gzip",
        };

        private static readonly HashSet<string> s_allowedBuildFiles = new(StringComparer.Ordinal)
        {
            "Directory.Build.props",
            "EveLens.sln",
            "global.json",
            "nuget.config",
            "services/report-webhook/package.json",
            "src/EveLens.Avalonia/EveLens.Avalonia.csproj",
            "src/EveLens.Common/EveLens.Common.csproj",
            "src/EveLens.Core/EveLens.Core.csproj",
            "src/EveLens.Data/EveLens.Data.csproj",
            "src/EveLens.Infrastructure/EveLens.Infrastructure.csproj",
            "src/EveLens.Models/EveLens.Models.csproj",
            "src/EveLens.Serialization/EveLens.Serialization.csproj",
            "tests/EveLens.Tests/EveLens.Tests.csproj",
            "tools/PatchXmlCreator/PatchXmlCreator.csproj",
            "tools/ResFileCreator/ResFileCreator.csproj",
            "tools/XmlGenerator/XmlGenerator.csproj",
            "tools/YamlToSqlite/YamlToSqlite.csproj",
        };

        private static readonly HashSet<string> s_allowedPackages = new(StringComparer.OrdinalIgnoreCase)
        {
            "Avalonia",
            "Avalonia.Controls.DataGrid",
            "Avalonia.Desktop",
            "Avalonia.Themes.Fluent",
            "CommunityToolkit.Mvvm",
            "DesktopNotifications",
            "DesktopNotifications.Windows",
            "FluentAssertions",
            "Google.Apis.Calendar.v3",
            "Google.Apis.Drive.v3",
            "MailKit",
            "Microsoft.Data.Sqlite",
            "Microsoft.EntityFrameworkCore.Sqlite",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Logging",
            "Microsoft.NET.Test.Sdk",
            "Newtonsoft.Json",
            "NSubstitute",
            "SkiaSharp",
            "System.Configuration.ConfigurationManager",
            "System.IdentityModel.Tokens.Jwt",
            "System.Resources.Extensions",
            "System.Security.Cryptography.Pkcs",
            "Velopack",
            "xunit",
            "xunit.runner.visualstudio",
            "YamlDotNet",
        };

        private static readonly HashSet<string> s_textExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".axaml", ".xaml", ".csproj", ".props", ".targets", ".sln", ".ps1", ".sh", ".py",
            ".js", ".json", ".xml", ".yml", ".yaml", ".md", ".config", ".txt", ".htm", ".html",
            ".plist", ".desktop", ".xslt", ".resx", ".settings", ".svg", ".example",
        };

        private static readonly HashSet<string> s_allowedSdks = new(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft.NET.Sdk",
        };

        private static readonly HashSet<string> s_buildFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".csproj", ".vbproj", ".fsproj", ".proj", ".projitems", ".shproj", ".props", ".targets",
            ".sln", ".slnx", ".slnf", ".rsp", ".user", ".pubxml",
        };

        private static readonly HashSet<string> s_buildFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "nuget.config", "global.json", "package.json", "package-lock.json", "packages.lock.json",
            "dotnet-tools.json",
        };

        private static readonly Regex s_storedCredential = new(
            "\"(?:RefreshToken|AccessToken|SSOClientSecret|ClientSecret|refresh_token|access_token|client_secret)\"" +
            "\\s*:\\s*\"(?!your-)[^\"]{8,}\"" +
            "|\\b(?:refreshToken|accessToken|ssoClientSecret|clientSecret)\\s*=\\s*\"(?!your-)[^\"]{8,}\"",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Fact]
        public void Tracked_file_list_is_read_inside_a_git_checkout()
        {
            if (!IsGitCheckout())
                return;

            s_tracked.Value.Should().Contain("EveLens.sln");
        }

        [Fact]
        public void No_executable_archive_or_key_file_is_tracked()
        {
            var offenders = s_tracked.Value
                .Where(path => s_forbiddenExtensions.Contains(Path.GetExtension(path)))
                .ToList();

            offenders.Should().BeEmpty(
                "compiled binaries, archives, key files and shell launchers are never tracked");
        }

        [Fact]
        public void No_tracked_file_starts_with_an_executable_header()
        {
            var offenders = s_tracked.Value
                .Where(path => HasExecutableHeader(Path.Combine(ProjectRoot, path)))
                .ToList();

            offenders.Should().BeEmpty(
                "a Windows, Linux or macOS executable is never tracked, whatever its file name");
        }

        [Fact]
        public void Every_tracked_script_is_on_the_script_list()
        {
            var offenders = s_tracked.Value
                .Where(IsScript)
                .Where(path => !s_allowedScripts.Contains(path))
                .ToList();

            offenders.Should().BeEmpty("a new script is added to the script list in this test");
        }

        [Fact]
        public void Every_tracked_data_file_is_on_the_data_file_list()
        {
            var offenders = s_tracked.Value
                .Where(path => path.EndsWith(".gzip", StringComparison.OrdinalIgnoreCase))
                .Where(path => !s_allowedDataFiles.Contains(path))
                .ToList();

            offenders.Should().BeEmpty("a new data file is added to the data file list in this test");
        }

        [Fact]
        public void Every_tracked_build_file_is_on_the_build_file_list()
        {
            var offenders = s_tracked.Value
                .Where(IsBuildFile)
                .Where(path => !s_allowedBuildFiles.Contains(path))
                .ToList();

            offenders.Should().BeEmpty("a new build file is added to the build file list in this test");
        }

        [Fact]
        public void Every_referenced_package_is_on_the_package_list()
        {
            var offenders = new List<string>();
            foreach (var path in s_tracked.Value.Where(IsMsBuildFile))
            {
                var fullPath = Path.Combine(ProjectRoot, path);
                if (!File.Exists(fullPath))
                    continue;

                foreach (var package in ReferencedPackages(File.ReadAllText(fullPath)))
                {
                    if (!s_allowedPackages.Contains(package))
                        offenders.Add($"{path}: {package}");
                }
            }

            offenders.Should().BeEmpty("a new package is added to the package list in this test");
        }

        [Fact]
        public void Every_referenced_sdk_is_on_the_sdk_list()
        {
            var offenders = new List<string>();
            foreach (var path in s_tracked.Value.Where(IsMsBuildFile))
            {
                var fullPath = Path.Combine(ProjectRoot, path);
                if (!File.Exists(fullPath))
                    continue;

                foreach (var sdk in ReferencedSdks(File.ReadAllText(fullPath)))
                {
                    if (!s_allowedSdks.Contains(sdk))
                        offenders.Add($"{path}: {sdk}");
                }
            }

            var globalJson = Path.Combine(ProjectRoot, "global.json");
            if (File.Exists(globalJson))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(globalJson));
                if (document.RootElement.TryGetProperty("msbuild-sdks", out _))
                    offenders.Add("global.json: msbuild-sdks");
            }

            offenders.Should().BeEmpty("a new build SDK is added to the SDK list in this test");
        }

        [Theory]
        [InlineData("<Project><ItemGroup><PackageReference Include=\"Evil\" Version=\"1\" /></ItemGroup></Project>")]
        [InlineData("<Project><ItemGroup><PackageReference Include='Evil' Version='1' /></ItemGroup></Project>")]
        [InlineData("<Project><ItemGroup><PackageReference Condition=\"1 &gt; 0\" Include=\"Evil\" /></ItemGroup></Project>")]
        [InlineData("<Project><ItemGroup><PackageReference Update=\"Evil\" Version=\"1\" /></ItemGroup></Project>")]
        [InlineData("<Project><ItemGroup><GlobalPackageReference Include=\"Evil\" Version=\"1\" /></ItemGroup></Project>")]
        [InlineData("<Project><ItemGroup><PackageDownload Include=\"Evil\" Version=\"[1]\" /></ItemGroup></Project>")]
        [InlineData("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><PackageReference Include=\"Evil\" /></ItemGroup></Project>")]
        public void Package_references_are_found_in_every_form(string project)
        {
            ReferencedPackages(project).Should().Equal("Evil");
        }

        [Theory]
        [InlineData("<Project Sdk=\"Evil.Sdk/1.0\" />", "Evil.Sdk")]
        [InlineData("<Project Sdk=\"Microsoft.NET.Sdk;Evil.Sdk\" />", "Microsoft.NET.Sdk,Evil.Sdk")]
        [InlineData("<Project><Sdk Name=\"Evil.Sdk\" Version=\"1.0\" /></Project>", "Evil.Sdk")]
        [InlineData("<Project><Import Project=\"x.props\" Sdk=\"Evil.Sdk\" /></Project>", "Evil.Sdk")]
        public void Sdk_references_are_found_in_every_form(string project, string expected)
        {
            ReferencedSdks(project).Should().Equal(expected.Split(','));
        }

        [Fact]
        public void No_tracked_text_file_contains_direction_override_characters()
        {
            var offenders = new List<string>();
            foreach (var path in s_tracked.Value.Where(p => s_textExtensions.Contains(Path.GetExtension(p))))
            {
                var fullPath = Path.Combine(ProjectRoot, path);
                if (File.Exists(fullPath) && File.ReadAllText(fullPath).Any(IsDirectionOverride))
                    offenders.Add(path);
            }

            offenders.Should().BeEmpty(
                "direction override characters make text display differently from what is compiled");
        }

        [Fact]
        public void No_tracked_data_or_config_file_contains_a_stored_credential()
        {
            var offenders = new List<string>();
            foreach (var path in s_tracked.Value.Where(IsDataOrConfig))
            {
                var fullPath = Path.Combine(ProjectRoot, path);
                if (File.Exists(fullPath) && s_storedCredential.IsMatch(File.ReadAllText(fullPath)))
                    offenders.Add(path);
            }

            offenders.Should().BeEmpty("settings files holding tokens or client secrets are never tracked");
        }

        [Theory]
        [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00 }, true)]
        [InlineData(new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, true)]
        [InlineData(new byte[] { 0xCF, 0xFA, 0xED, 0xFE }, true)]
        [InlineData(new byte[] { 0xFE, 0xED, 0xFA, 0xCF }, true)]
        [InlineData(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }, true)]
        [InlineData(new byte[] { 0x23, 0x21, 0x2F, 0x62 }, false)]
        [InlineData(new byte[] { 0x1F, 0x8B, 0x08, 0x00 }, false)]
        [InlineData(new byte[] { 0x4D }, false)]
        public void Executable_headers_are_recognised(byte[] header, bool expected)
        {
            IsExecutableHeader(header, header.Length).Should().Be(expected);
        }

        [Theory]
        [InlineData("{ \"RefreshToken\": \"3E/abcdefghijklmnop==\" }", true)]
        [InlineData("{ \"SSOClientSecret\": \"abcdefghijklmnop\" }", true)]
        [InlineData("{ \"ClientSecret\": \"your-client-secret-here\" }", false)]
        [InlineData("{ \"refreshToken\": \"3E/abcdefghijklmnop==\" }", true)]
        [InlineData("{ \"ssoClientSecret\": \"abcdefghijklmnop\" }", true)]
        [InlineData("<esiKey id=\"1\" refreshToken=\"3E/abcdefghijklmnop==\" />", true)]
        [InlineData("<settings clientSecret=\"abcdefghijklmnop\" />", true)]
        [InlineData("{ \"RefreshToken\": \"\" }", false)]
        [InlineData("{ \"Name\": \"A long character name\" }", false)]
        public void Stored_credentials_are_recognised(string text, bool expected)
        {
            s_storedCredential.IsMatch(text).Should().Be(expected);
        }

        private static bool IsScript(string path)
        {
            if (s_scriptExtensions.Contains(Path.GetExtension(path)))
                return true;

            return Path.GetExtension(path).Length == 0 && StartsWithShebang(Path.Combine(ProjectRoot, path));
        }

        private static bool IsBuildFile(string path) =>
            s_buildFileExtensions.Contains(Path.GetExtension(path))
            || s_buildFileNames.Contains(Path.GetFileName(path));

        private static bool IsMsBuildFile(string path)
        {
            var extension = Path.GetExtension(path);
            return s_buildFileExtensions.Contains(extension)
                && !extension.Equals(".rsp", StringComparison.OrdinalIgnoreCase)
                && !extension.StartsWith(".sln", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> ReferencedPackages(string projectXml) =>
            XDocument.Parse(projectXml).Descendants()
                .Where(element =>
                    element.Name.LocalName.EndsWith("PackageReference", StringComparison.OrdinalIgnoreCase)
                    || element.Name.LocalName.Equals("PackageVersion", StringComparison.OrdinalIgnoreCase)
                    || element.Name.LocalName.Equals("PackageDownload", StringComparison.OrdinalIgnoreCase))
                .SelectMany(element => element.Attributes()
                    .Where(attribute =>
                        attribute.Name.LocalName.Equals("Include", StringComparison.OrdinalIgnoreCase)
                        || attribute.Name.LocalName.Equals("Update", StringComparison.OrdinalIgnoreCase)))
                .SelectMany(attribute => attribute.Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .Select(name => name.Trim())
                .ToList();

        private static List<string> ReferencedSdks(string projectXml)
        {
            var document = XDocument.Parse(projectXml);
            var fromAttributes = document.Descendants()
                .Where(element => !element.Name.LocalName.Equals("Sdk", StringComparison.OrdinalIgnoreCase))
                .SelectMany(element => element.Attributes()
                    .Where(attribute => attribute.Name.LocalName.Equals("Sdk", StringComparison.OrdinalIgnoreCase)))
                .SelectMany(attribute => attribute.Value.Split(';', StringSplitOptions.RemoveEmptyEntries));
            var fromElements = document.Descendants()
                .Where(element => element.Name.LocalName.Equals("Sdk", StringComparison.OrdinalIgnoreCase))
                .Select(element => element.Attributes()
                    .FirstOrDefault(attribute => attribute.Name.LocalName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    ?.Value ?? string.Empty);

            return fromAttributes.Concat(fromElements)
                .Select(sdk => sdk.Split('/')[0].Trim())
                .ToList();
        }

        private static bool IsDataOrConfig(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".config", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".yml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".settings", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectionOverride(char c) =>
            (c >= '\u202A' && c <= '\u202E') || (c >= '\u2066' && c <= '\u2069');

        private static bool HasExecutableHeader(string fullPath)
        {
            var header = new byte[4];
            return IsExecutableHeader(header, ReadHeader(fullPath, header));
        }

        private static bool IsExecutableHeader(byte[] header, int length)
        {
            if (length >= 2 && header[0] == 0x4D && header[1] == 0x5A)
                return true;
            if (length < 4)
                return false;

            uint magic = ((uint)header[0] << 24) | ((uint)header[1] << 16) | ((uint)header[2] << 8) | header[3];
            return magic is 0x7F454C46 or 0xFEEDFACE or 0xFEEDFACF or 0xCEFAEDFE or 0xCFFAEDFE
                or 0xCAFEBABE or 0xBEBAFECA;
        }

        private static bool StartsWithShebang(string fullPath)
        {
            var header = new byte[2];
            return ReadHeader(fullPath, header) == 2 && header[0] == (byte)'#' && header[1] == (byte)'!';
        }

        private static int ReadHeader(string fullPath, byte[] buffer)
        {
            if (!File.Exists(fullPath))
                return 0;

            using var stream = File.OpenRead(fullPath);
            return stream.Read(buffer, 0, buffer.Length);
        }

        private static IReadOnlyList<string> LoadTrackedFiles()
        {
            if (!IsGitCheckout())
                return Array.Empty<string>();

            try
            {
                var startInfo = new ProcessStartInfo("git")
                {
                    WorkingDirectory = ProjectRoot,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add("core.quotepath=off");
                startInfo.ArgumentList.Add("ls-files");
                startInfo.ArgumentList.Add("-z");

                using var process = Process.Start(startInfo);
                if (process == null)
                    return Array.Empty<string>();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    return Array.Empty<string>();

                return output
                    .Split('\0', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                return Array.Empty<string>();
            }
        }

        private static bool IsGitCheckout() =>
            Directory.Exists(Path.Combine(ProjectRoot, ".git")) || File.Exists(Path.Combine(ProjectRoot, ".git"));

        private static string FindProjectRoot()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "EveLens.sln")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
