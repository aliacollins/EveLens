// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Architecture
{
    /// <summary>
    /// Guards the attach/detach lifecycle of Avalonia views.
    /// </summary>
    /// <remarks>
    /// MainWindow caches one CharacterMonitorView per character and re-shows it with the
    /// same DataContext, so OnDataContextChanged does not fire when a cached view comes back.
    /// A view that tears down its subscriptions in OnDetachedFromVisualTree must therefore
    /// restore them in OnAttachedToVisualTree, or it silently stops updating after the first
    /// time the user navigates away (Issue #164: header Located In / Docked At stuck at the
    /// location from app launch).
    /// </remarks>
    public class ViewLifecycleArchitectureTests
    {
        private static readonly string ProjectRoot = FindProjectRoot();
        private static readonly string AvaloniaDir = Path.Combine(ProjectRoot, "src", "EveLens.Avalonia");

        [Fact]
        public void ViewsOverridingDetach_MustAlsoOverrideAttach()
        {
            var offenders = Directory.GetFiles(AvaloniaDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                            && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Where(f =>
                {
                    var src = File.ReadAllText(f);
                    return src.Contains("override void OnDetachedFromVisualTree")
                           && !src.Contains("override void OnAttachedToVisualTree");
                })
                .Select(f => Path.GetRelativePath(ProjectRoot, f))
                .ToList();

            offenders.Should().BeEmpty(
                "a view that unsubscribes on detach must resubscribe on attach; cached views are " +
                "re-shown without a DataContext change, so OnDataContextChanged alone never restores " +
                "the subscriptions (Issue #164)");
        }

        [Fact]
        public void CharacterMonitorHeader_ResubscribesOnAttach()
        {
            var file = Path.Combine(AvaloniaDir, "Views", "CharacterMonitor", "CharacterMonitorHeader.axaml.cs");
            var src = File.ReadAllText(file);

            src.Should().Contain("override void OnAttachedToVisualTree",
                "the header must resume listening when its cached view is re-shown (Issue #164)");
        }

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
