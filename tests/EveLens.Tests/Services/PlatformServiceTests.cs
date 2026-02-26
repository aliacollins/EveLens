// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for the platform-agnostic service interfaces and their AppServices registration.
    /// Phase 0 of the Avalonia migration: verifies WinForms decoupling infrastructure.
    /// </summary>
    [Collection("AppServices")]
    public class PlatformServiceTests
    {
        public PlatformServiceTests()
        {
            AppServices.Reset();
        }

        #region AppServices Registration

        [Fact]
        public void DialogService_ReturnsNonNull()
        {
            AppServices.DialogService.Should().NotBeNull();
            AppServices.DialogService.Should().BeAssignableTo<IDialogService>();
        }

        [Fact]
        public void ClipboardService_ReturnsNonNull()
        {
            AppServices.ClipboardService.Should().NotBeNull();
            AppServices.ClipboardService.Should().BeAssignableTo<IClipboardService>();
        }

        [Fact]
        public void ApplicationLifecycle_ReturnsNonNull()
        {
            AppServices.ApplicationLifecycle.Should().NotBeNull();
            AppServices.ApplicationLifecycle.Should().BeAssignableTo<IApplicationLifecycle>();
        }

        [Fact]
        public void ScreenInfo_ReturnsNonNull()
        {
            AppServices.ScreenInfo.Should().NotBeNull();
            AppServices.ScreenInfo.Should().BeAssignableTo<IScreenInfo>();
        }

        #endregion

        #region Set Override

        [Fact]
        public void SetDialogService_OverridesDefault()
        {
            var mock = Substitute.For<IDialogService>();
            AppServices.SetDialogService(mock);

            AppServices.DialogService.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetClipboardService_OverridesDefault()
        {
            var mock = Substitute.For<IClipboardService>();
            AppServices.SetClipboardService(mock);

            AppServices.ClipboardService.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetApplicationLifecycle_OverridesDefault()
        {
            var mock = Substitute.For<IApplicationLifecycle>();
            AppServices.SetApplicationLifecycle(mock);

            AppServices.ApplicationLifecycle.Should().BeSameAs(mock);
        }

        [Fact]
        public void SetScreenInfo_OverridesDefault()
        {
            var mock = Substitute.For<IScreenInfo>();
            AppServices.SetScreenInfo(mock);

            AppServices.ScreenInfo.Should().BeSameAs(mock);
        }

        #endregion

        #region Reset Restores Defaults

        [Fact]
        public void Reset_RestoresDefaultDialogService()
        {
            var mock = Substitute.For<IDialogService>();
            AppServices.SetDialogService(mock);

            AppServices.Reset();

            AppServices.DialogService.Should().NotBeSameAs(mock);
            AppServices.DialogService.Should().NotBeNull();
        }

        [Fact]
        public void Reset_RestoresDefaultClipboardService()
        {
            var mock = Substitute.For<IClipboardService>();
            AppServices.SetClipboardService(mock);

            AppServices.Reset();

            AppServices.ClipboardService.Should().NotBeSameAs(mock);
            AppServices.ClipboardService.Should().NotBeNull();
        }

        #endregion

        #region Dialog Enum Coverage

        [Fact]
        public void DialogChoice_AllValuesHandled()
        {
            var values = System.Enum.GetValues<DialogChoice>();
            values.Should().HaveCountGreaterOrEqualTo(7,
                "DialogChoice should map to OK, Cancel, Abort, Retry, Ignore, Yes, No");
        }

        [Fact]
        public void DialogButtons_AllValuesHandled()
        {
            var values = System.Enum.GetValues<DialogButtons>();
            values.Should().HaveCountGreaterOrEqualTo(6,
                "DialogButtons should map to OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel");
        }

        [Fact]
        public void DialogIcon_AllValuesHandled()
        {
            var values = System.Enum.GetValues<DialogIcon>();
            values.Should().HaveCountGreaterOrEqualTo(5,
                "DialogIcon should map to None, Error, Warning, Information, Question");
        }

        #endregion
    }
}
