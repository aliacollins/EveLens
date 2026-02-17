using EVEMon.Common.Services;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Services
{
    /// <summary>
    /// Verifies that adapter classes correctly implement their interfaces
    /// and can be instantiated without throwing.
    /// </summary>
    public class AdapterTests
    {
        [Fact]
        public void NameResolverAdapter_ImplementsINameResolver()
        {
            var adapter = new NameResolverAdapter();

            adapter.Should().BeAssignableTo<INameResolver>();
        }

        [Fact]
        public void NameResolverAdapter_GetName_ReturnsString()
        {
            var adapter = new NameResolverAdapter();

            // ID 0 should return a valid string (typically "Unknown" or empty)
            string result = adapter.GetName(0);

            result.Should().NotBeNull();
        }

        [Fact]
        public void NameResolverAdapter_GetRefTypeName_ReturnsString()
        {
            var adapter = new NameResolverAdapter();

            // Unknown ref type should still return a non-null string
            string result = adapter.GetRefTypeName(0);

            result.Should().NotBeNull();
        }

        [Fact]
        public void FlagResolverAdapter_ImplementsIFlagResolver()
        {
            var adapter = new FlagResolverAdapter();

            adapter.Should().BeAssignableTo<IFlagResolver>();
        }

        // FlagResolverAdapter.GetFlagText delegates to EveFlag which requires
        // LocalXmlCache and EveMonClient cache dir initialization. Tested via
        // integration tests only.

        [Fact]
        public void TraceServiceAdapter_ImplementsITraceService()
        {
            var adapter = new TraceServiceAdapter();

            adapter.Should().BeAssignableTo<ITraceService>();
        }

        [Fact]
        public void TraceServiceAdapter_Trace_DoesNotThrow()
        {
            var adapter = new TraceServiceAdapter();

            // Should not throw even without full EveMonClient initialization
            var act = () => adapter.Trace("test message", false);
            act.Should().NotThrow();
        }

        [Fact]
        public void TraceServiceAdapter_TraceFormat_DoesNotThrow()
        {
            var adapter = new TraceServiceAdapter();

            var act = () => adapter.Trace("test {0}", "arg");
            act.Should().NotThrow();
        }

        [Fact]
        public void TraceServiceAdapter_MinimumLevel_DefaultsToDebug()
        {
            var adapter = new TraceServiceAdapter();

            adapter.MinimumLevel.Should().Be(TraceLevel.Debug);
        }

        [Fact]
        public void TraceServiceAdapter_TraceLevelOverload_DoesNotThrow()
        {
            var adapter = new TraceServiceAdapter();

            var act = () => adapter.Trace(TraceLevel.Warning, "warning message", false);
            act.Should().NotThrow();
        }

        [Fact]
        public void TraceServiceAdapter_TraceLevelFormatOverload_DoesNotThrow()
        {
            var adapter = new TraceServiceAdapter();

            var act = () => adapter.Trace(TraceLevel.Error, "error {0}", "details");
            act.Should().NotThrow();
        }

        [Fact]
        public void TraceServiceAdapter_StopLogging_WithoutStart_DoesNotThrow()
        {
            var adapter = new TraceServiceAdapter();

            var act = () => adapter.StopLogging();
            act.Should().NotThrow();
        }

        [Fact]
        public void StationResolverAdapter_ImplementsIStationResolver()
        {
            var adapter = new StationResolverAdapter();

            adapter.Should().BeAssignableTo<IStationResolver>();
        }

        [Fact]
        public void ImageServiceAdapter_ImplementsIImageService()
        {
            var adapter = new ImageServiceAdapter();

            adapter.Should().BeAssignableTo<Core.Interfaces.IImageService>();
        }

        [Fact]
        public void NotificationTypeResolverAdapter_ImplementsINotificationTypeResolver()
        {
            var adapter = new NotificationTypeResolverAdapter();

            adapter.Should().BeAssignableTo<INotificationTypeResolver>();
        }

        [Fact]
        public void ApplicationPathsAdapter_ImplementsIApplicationPaths()
        {
            var adapter = new ApplicationPathsAdapter();

            adapter.Should().BeAssignableTo<IApplicationPaths>();
        }

        [Fact]
        public void NotificationTypeResolverAdapter_GetName_ReturnsString()
        {
            var adapter = new NotificationTypeResolverAdapter();

            string result = adapter.GetName(0);

            result.Should().NotBeNull();
        }

        [Fact]
        public void NotificationTypeResolverAdapter_GetSubjectLayout_ReturnsString()
        {
            var adapter = new NotificationTypeResolverAdapter();

            string result = adapter.GetSubjectLayout(0);

            result.Should().NotBeNull();
        }

        // NotificationTypeResolverAdapter.GetTextLayout delegates to EveNotificationType
        // which calls EnsureImportation() in non-debug builds, requiring cache dir init.
    }
}
