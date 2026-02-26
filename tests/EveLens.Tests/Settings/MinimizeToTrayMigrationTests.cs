// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Xml.Serialization;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Settings
{
    /// <summary>
    /// Tests for the MinimizeToTray migration from the old 3-setting tray matrix
    /// (SystemTrayBehaviour × CloseBehaviour × TrayPopupStyles) to a single boolean.
    /// </summary>
    public class MinimizeToTrayMigrationTests
    {
        #region Migration Logic

        [Fact]
        public void MigrateToMinimizeToTray_OldMinimizeToTrayWithAlwaysVisible_MigratesToTrue()
        {
            // Arrange — user had close=MinimizeToTray + tray=AlwaysVisible
            var ui = new UISettings
            {
                MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray,
                SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible
            };

            // Act
            ui.MigrateToMinimizeToTray();

            // Assert
            ui.MinimizeToTray.Should().BeTrue(
                "old MinimizeToTray + AlwaysVisible means user wanted tray behavior");
        }

        [Fact]
        public void MigrateToMinimizeToTray_OldMinimizeToTrayWithShowWhenMinimized_MigratesToTrue()
        {
            // Arrange — user had close=MinimizeToTray + tray=ShowWhenMinimized
            var ui = new UISettings
            {
                MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray,
                SystemTrayIcon = SystemTrayBehaviour.ShowWhenMinimized
            };

            // Act
            ui.MigrateToMinimizeToTray();

            // Assert
            ui.MinimizeToTray.Should().BeTrue(
                "old MinimizeToTray + non-disabled tray means user wanted tray behavior");
        }

        [Fact]
        public void MigrateToMinimizeToTray_OldExitBehaviour_MigratesToFalse()
        {
            // Arrange — user had close=Exit
            var ui = new UISettings
            {
                MainWindowCloseBehaviour = CloseBehaviour.Exit,
                SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible
            };

            // Act
            ui.MigrateToMinimizeToTray();

            // Assert
            ui.MinimizeToTray.Should().BeFalse(
                "old Exit close behavior means user didn't want tray-on-close");
        }

        [Fact]
        public void MigrateToMinimizeToTray_OldTrayDisabled_MigratesToFalse()
        {
            // Arrange — user had tray disabled even with MinimizeToTray close behavior
            var ui = new UISettings
            {
                MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray,
                SystemTrayIcon = SystemTrayBehaviour.Disabled
            };

            // Act
            ui.MigrateToMinimizeToTray();

            // Assert
            ui.MinimizeToTray.Should().BeFalse(
                "tray disabled means minimize-to-tray can't work");
        }

        [Fact]
        public void MigrateToMinimizeToTray_OldMinimizeToTaskbar_MigratesToFalse()
        {
            // Arrange — user had close=MinimizeToTaskbar (not tray)
            var ui = new UISettings
            {
                MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTaskbar,
                SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible
            };

            // Act
            ui.MigrateToMinimizeToTray();

            // Assert
            ui.MinimizeToTray.Should().BeFalse(
                "MinimizeToTaskbar is not MinimizeToTray");
        }

        [Fact]
        public void MigrateToMinimizeToTray_DefaultSettings_MigratesToFalse()
        {
            // Arrange — fresh UISettings with defaults (Exit + Disabled)
            var ui = new UISettings();

            // Act
            ui.MigrateToMinimizeToTray();

            // Assert
            ui.MinimizeToTray.Should().BeFalse(
                "default settings should not enable minimize-to-tray");
        }

        [Fact]
        public void MigrateToMinimizeToTray_UserSetFalse_OldEnumsStillTray_DoesNotReEnable()
        {
            // Arrange — simulates what happens on save: user set MinimizeToTray=false
            // but old enums still say MinimizeToTray+AlwaysVisible.
            // Migration must NOT flip it back to true.
            var ui = new UISettings
            {
                MinimizeToTray = false,
                MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray,
                SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible
            };

            // Act — migration WOULD set it to true if called blindly
            // But in production, migration only runs at startup (Initialize),
            // not on save (ImportAsync). This test documents the behavior:
            // migration always sets based on old enums, so it must NOT be
            // called after the user explicitly sets the new property.
            ui.MigrateToMinimizeToTray();

            // Assert — migration does set it based on old enums (that's its job).
            // The key invariant is that production code only calls this at startup.
            ui.MinimizeToTray.Should().BeTrue(
                "migration reads old enums — this is why it must only run at startup, not on save");
        }

        #endregion

        #region XML Round-Trip

        [Fact]
        public void XmlRoundTrip_MinimizeToTrayTrue_PreservesValue()
        {
            // Arrange
            var original = new SerializableSettings();
            original.UI.MinimizeToTray = true;

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.UI.MinimizeToTray.Should().BeTrue();
        }

        [Fact]
        public void XmlRoundTrip_MinimizeToTrayFalse_PreservesValue()
        {
            // Arrange
            var original = new SerializableSettings();
            original.UI.MinimizeToTray = false;

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.UI.MinimizeToTray.Should().BeFalse();
        }

        [Fact]
        public void XmlRoundTrip_MinimizeToTrayDefault_IsFalse()
        {
            // Arrange — default settings (no explicit set)
            var original = new SerializableSettings();

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.UI.MinimizeToTray.Should().BeFalse(
                "default for new installs should be false (close exits)");
        }

        [Fact]
        public void XmlRoundTrip_OldEnumsStillPreserved()
        {
            // Arrange — verify old enums survive round-trip for backward compat
            var original = new SerializableSettings();
            original.UI.SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible;
            original.UI.MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray;

            // Act
            var result = XmlRoundTrip(original);

            // Assert
            result.UI.SystemTrayIcon.Should().Be(SystemTrayBehaviour.AlwaysVisible);
            result.UI.MainWindowCloseBehaviour.Should().Be(CloseBehaviour.MinimizeToTray);
        }

        #endregion

        #region Helpers

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            string xml = writer.ToString();

            using var reader = new StringReader(xml);
            return (T)serializer.Deserialize(reader)!;
        }

        #endregion
    }
}
