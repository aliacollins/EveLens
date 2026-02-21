using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EVEMon.Common.Models;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Architecture
{
    /// <summary>
    /// Architecture tests enforcing WinForms decoupling from EVEMon.Common service/model layer.
    /// Prevents regression — model and service files should use platform-agnostic interfaces.
    /// </summary>
    public class WinFormsCouplingTests
    {
        private static readonly Assembly CommonAssembly = typeof(EVEMon.Common.Services.AppServices).Assembly;

        /// <summary>
        /// Verifies that model image properties return object? not System.Drawing.Image.
        /// This ensures models are framework-agnostic.
        /// </summary>
        [Theory]
        [InlineData(typeof(Standing), "EntityImage")]
        [InlineData(typeof(Contact), "EntityImage")]
        [InlineData(typeof(KillLogItem), "ItemImage")]
        [InlineData(typeof(KillLog), "VictimShipImage")]
        [InlineData(typeof(Loyalty), "CorporationImage")]
        [InlineData(typeof(EmploymentRecord), "CorporationImage")]
        public void ImageProperties_UseObjectNotDrawingImage(Type modelType, string propertyName)
        {
            var property = modelType.GetProperty(propertyName);
            property.Should().NotBeNull($"{modelType.Name} should have {propertyName} property");

            property!.PropertyType.Should().Be(typeof(object),
                $"{modelType.Name}.{propertyName} should return object? (not System.Drawing.Image) for framework agnosticism");
        }

        /// <summary>
        /// Verifies that Standing.GetStandingImage returns object? not Image.
        /// </summary>
        [Fact]
        public void Standing_GetStandingImage_ReturnsObject()
        {
            var method = typeof(Standing).GetMethod("GetStandingImage", BindingFlags.Public | BindingFlags.Static);
            method.Should().NotBeNull();
            method!.ReturnType.Should().Be(typeof(object),
                "Standing.GetStandingImage should return object? for framework agnosticism");
        }

        /// <summary>
        /// Scans source files for MessageBox.Show usage outside allowed locations.
        /// Allowed: WinFormsDialogService, Controls/, ViewModels/Binding/.
        /// </summary>
        [Fact]
        public void ServiceFiles_DoNotCallMessageBoxDirectly()
        {
            string commonDir = FindProjectDirectory("src/EVEMon.Common");
            if (commonDir == null)
                return; // Skip if we can't find source (CI environment)

            var csFiles = Directory.GetFiles(commonDir, "*.cs", SearchOption.AllDirectories);

            var allowedPaths = new[]
            {
                Path.DirectorySeparatorChar + "Controls" + Path.DirectorySeparatorChar,
                Path.DirectorySeparatorChar + "Binding" + Path.DirectorySeparatorChar,
                "WinFormsDialogService.cs",
                "WinFormsClipboardService.cs",
                "WinFormsApplicationLifecycle.cs",
                "WinFormsScreenInfo.cs",
            };

            var violations = new List<string>();

            foreach (var file in csFiles)
            {
                // Skip allowed directories/files
                if (allowedPaths.Any(p => file.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Skip files with known remaining static coupling (migration targets)
                // PlanPrinter uses PrintPreviewDialog (GDI+ printing), ImageService uses PictureBox
                // GlobalSuppressions is auto-generated
                if (file.Contains("PlanPrinter.cs") || file.Contains("ImageService.cs") ||
                    file.Contains("GlobalSuppressions.cs"))
                    continue;

                string content = File.ReadAllText(file);
                if (content.Contains("MessageBox.Show("))
                {
                    string relativePath = file.Substring(file.IndexOf("EVEMon.Common"));
                    violations.Add(relativePath);
                }
            }

            violations.Should().BeEmpty(
                "Service/helper files should use AppServices.DialogService, not MessageBox.Show directly. " +
                $"Violations: {string.Join(", ", violations)}");
        }

        /// <summary>
        /// SSO services must use AppServices.Dispatcher (cross-platform) not
        /// EVEMon.Common.Threading.Dispatcher (WinForms-only).
        /// Prevents regression of the Part 1 migration.
        /// </summary>
        [Fact]
        public void SSOServices_UseAppServicesDispatcher_NotStaticDispatcher()
        {
            string? commonDir = FindProjectDirectory("src/EVEMon.Common");
            if (commonDir == null)
                return;

            var ssoFiles = new[]
            {
                Path.Combine(commonDir, "Service", "SSOAuthenticationService.cs"),
                Path.Combine(commonDir, "Service", "SSOWebServerHttpListener.cs"),
            };

            var violations = new List<string>();

            foreach (var file in ssoFiles)
            {
                if (!File.Exists(file))
                    continue;

                string content = File.ReadAllText(file);
                string fileName = Path.GetFileName(file);

                // Must not import the old static Dispatcher
                if (content.Contains("using EVEMon.Common.Threading;"))
                    violations.Add($"{fileName}: still imports EVEMon.Common.Threading");

                // Must not call Dispatcher.Invoke (without AppServices. prefix)
                // Match standalone Dispatcher.Invoke but not AppServices.Dispatcher?.Invoke
                var lines = content.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("//")) continue;
                    if (line.Contains("Dispatcher.Invoke") &&
                        !line.Contains("AppServices.Dispatcher"))
                    {
                        violations.Add($"{fileName}:{i + 1}: uses Dispatcher.Invoke instead of AppServices.Dispatcher?.Invoke");
                    }
                }
            }

            violations.Should().BeEmpty(
                "SSO services must use AppServices.Dispatcher?.Invoke (cross-platform), " +
                "not EVEMon.Common.Threading.Dispatcher.Invoke (WinForms-only). " +
                $"Violations: {string.Join("; ", violations)}");
        }

        private static string? FindProjectDirectory(string relativePath)
        {
            // Walk up from the test assembly location to find the repo root
            string? dir = Path.GetDirectoryName(CommonAssembly.Location);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relativePath);
                if (Directory.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
