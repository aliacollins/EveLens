using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for the macOS 1.5.0-beta.13 launch failure.
    ///
    /// beta.13 was our first single-file publish (Apple's bundle rules want only Mach-O
    /// binaries in Contents/MacOS, and rcodesign silently skips loose .NET dlls from the
    /// resource seal, so notarization needs a single file). In a single-file app
    /// <c>Assembly.Location</c> is an EMPTY STRING. The app version came from
    /// <c>FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location)</c>,
    /// which throws on an empty path — and the version is read on the startup path
    /// (User-Agent, window title, What's New check), so the app died before its first
    /// window. Windows and Linux were unaffected because they are not published
    /// single-file, which is exactly why nothing caught it before release.
    /// </summary>
    public class SingleFilePublishTests
    {
        [Fact]
        public void AppVersion_ReportsVersion_WithoutTouchingTheFilesystem()
        {
            IAppVersionInfo version = new AssemblyAppVersionInfo();

            version.ProductName.Should().NotBeNullOrWhiteSpace();
            version.ProductVersion.Should().NotBeNullOrWhiteSpace();
            version.FileVersion.Should().NotBeNullOrWhiteSpace();
            version.Company.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void AppVersion_SurvivesAnAssemblyWithNoLocation()
        {
            // The single-file condition, reproduced: an assembly whose Location is
            // empty. Attribute reads must still answer; anything path-based throws.
            Assembly inMemory = Assembly.Load(
                File.ReadAllBytes(typeof(AssemblyAppVersionInfo).Assembly.Location));
            inMemory.Location.Should().BeEmpty("this is what a single-file app reports");

            IAppVersionInfo version = new AssemblyAppVersionInfo(inMemory);

            version.ProductVersion.Should().NotBeNullOrWhiteSpace();
            version.FileVersion.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void AppVersion_StripsBuildMetadataFromInformationalVersion()
        {
            // The SDK appends "+<commit sha>" to AssemblyInformationalVersion. It is
            // noise in a title bar and it breaks the substring comparisons the update
            // flow makes against release tags.
            new AssemblyAppVersionInfo().ProductVersion.Should().NotContain("+");
        }

        [Fact]
        public void VersionSources_DoNotAskTheFilesystemForTheirOwnVersion()
        {
            // Architecture guard: the whole class of bug, not just the one call site.
            // Any FileVersionInfo.GetVersionInfo over an assembly Location is a crash
            // waiting for the next single-file platform.
            string src = FindSourceRoot();
            var offenders = Directory
                .EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
                .SelectMany(path => File.ReadLines(path)
                    .Select((line, i) => (path, line: line.Trim(), number: i + 1)))
                .Where(l => l.line.Contains("FileVersionInfo.GetVersionInfo") &&
                            !l.line.StartsWith("//") && !l.line.StartsWith("*"))
                .Select(l => $"{Path.GetRelativePath(src, l.path)}:{l.number}")
                .ToList();

            offenders.Should().BeEmpty(
                "app version must come from assembly attributes (IAppVersionInfo), " +
                "which a single-file publish still carries");
        }

        private static string FindSourceRoot()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
                dir = dir.Parent;

            dir.Should().NotBeNull("test must run inside the repository");
            return Path.Combine(dir!.FullName, "src");
        }
    }
}
