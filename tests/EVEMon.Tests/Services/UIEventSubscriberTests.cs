using System;
using System.Windows.Forms;
using EVEMon.Common.Events;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    /// <summary>
    /// Tests for UIEventSubscriber extension methods.
    /// Tests argument validation, subscription lifecycle, and dispose behavior.
    /// Note: Full UI-thread dispatch testing requires a SynchronizationContext;
    /// these tests focus on the non-UI-thread-dependent logic paths.
    /// </summary>
    public class UIEventSubscriberTests : IDisposable
    {
        private readonly IEventAggregator _mockAggregator;

        public UIEventSubscriberTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();

            // Make Subscribe return a disposable token
            _mockAggregator.Subscribe(Arg.Any<Action<SettingsChangedEvent>>())
                .Returns(Substitute.For<IDisposable>());
            _mockAggregator.Subscribe(Arg.Any<Action<CharacterUpdatedEvent>>())
                .Returns(Substitute.For<IDisposable>());
        }

        public void Dispose()
        {
            // No-op
        }

        #region SubscribeOnUI - Argument Validation

        [Fact]
        public void SubscribeOnUI_NullAggregator_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUI<SettingsChangedEvent>(
                null!, control, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("aggregator");
        }

        [Fact]
        public void SubscribeOnUI_NullControl_ThrowsArgumentNullException()
        {
            Action act = () => UIEventSubscriber.SubscribeOnUI<SettingsChangedEvent>(
                _mockAggregator, null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("control");
        }

        [Fact]
        public void SubscribeOnUI_NullHandler_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUI<SettingsChangedEvent>(
                _mockAggregator, control, null!);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("handler");
        }

        #endregion

        #region SubscribeOnUI - Normal Operation

        [Fact]
        public void SubscribeOnUI_ReturnsDisposable()
        {
            using var control = new Panel();

            var subscription = _mockAggregator.SubscribeOnUI<SettingsChangedEvent>(
                control, _ => { });

            subscription.Should().NotBeNull();
            subscription.Should().BeAssignableTo<IDisposable>();
            subscription.Dispose(); // cleanup
        }

        [Fact]
        public void SubscribeOnUI_CallsSubscribeOnAggregator()
        {
            using var control = new Panel();

            var subscription = _mockAggregator.SubscribeOnUI<SettingsChangedEvent>(
                control, _ => { });

            // Verify that Subscribe was called on the aggregator with a wrapped handler
            _mockAggregator.Received(1).Subscribe(Arg.Any<Action<SettingsChangedEvent>>());

            subscription.Dispose();
        }

        #endregion

        #region SubscribeOnUI - Dispose Unsubscribes

        [Fact]
        public void SubscribeOnUI_DisposingSubscription_CallsUnsubscribe()
        {
            using var control = new Panel();

            var subscription = _mockAggregator.SubscribeOnUI<SettingsChangedEvent>(
                control, _ => { });

            subscription.Dispose();

            // After dispose, unsubscribe should have been called
            _mockAggregator.Received(1).Unsubscribe(Arg.Any<Action<SettingsChangedEvent>>());
        }

        #endregion

        #region SubscribeOnUIForCharacter - Argument Validation

        [Fact]
        public void SubscribeOnUIForCharacter_NullAggregator_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacter<CharacterUpdatedEvent>(
                null!, control, () => null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("aggregator");
        }

        [Fact]
        public void SubscribeOnUIForCharacter_NullControl_ThrowsArgumentNullException()
        {
            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacter<CharacterUpdatedEvent>(
                _mockAggregator, null!, () => null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("control");
        }

        [Fact]
        public void SubscribeOnUIForCharacter_NullCharacterAccessor_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacter<CharacterUpdatedEvent>(
                _mockAggregator, control, null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("characterAccessor");
        }

        [Fact]
        public void SubscribeOnUIForCharacter_NullHandler_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacter<CharacterUpdatedEvent>(
                _mockAggregator, control, () => null!, null!);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("handler");
        }

        #endregion

        #region SubscribeOnUIForCharacter - Normal Operation

        [Fact]
        public void SubscribeOnUIForCharacter_ReturnsDisposable()
        {
            using var control = new Panel();

            var subscription = _mockAggregator.SubscribeOnUIForCharacter<CharacterUpdatedEvent>(
                control, () => null!, _ => { });

            subscription.Should().NotBeNull();
            subscription.Should().BeAssignableTo<IDisposable>();
            subscription.Dispose();
        }

        [Fact]
        public void SubscribeOnUIForCharacter_CallsSubscribeOnAggregator()
        {
            using var control = new Panel();

            var subscription = _mockAggregator.SubscribeOnUIForCharacter<CharacterUpdatedEvent>(
                control, () => null!, _ => { });

            _mockAggregator.Received(1).Subscribe(Arg.Any<Action<CharacterUpdatedEvent>>());

            subscription.Dispose();
        }

        #endregion

        #region SubscribeOnUIForCharacterBatch - Argument Validation

        [Fact]
        public void SubscribeOnUIForCharacterBatch_NullAggregator_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacterBatch<CharactersBatchUpdatedEvent>(
                null!, control, () => null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("aggregator");
        }

        [Fact]
        public void SubscribeOnUIForCharacterBatch_NullControl_ThrowsArgumentNullException()
        {
            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacterBatch<CharactersBatchUpdatedEvent>(
                _mockAggregator, null!, () => null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("control");
        }

        [Fact]
        public void SubscribeOnUIForCharacterBatch_NullCharacterAccessor_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacterBatch<CharactersBatchUpdatedEvent>(
                _mockAggregator, control, null!, _ => { });

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("characterAccessor");
        }

        [Fact]
        public void SubscribeOnUIForCharacterBatch_NullHandler_ThrowsArgumentNullException()
        {
            using var control = new Panel();

            Action act = () => UIEventSubscriber.SubscribeOnUIForCharacterBatch<CharactersBatchUpdatedEvent>(
                _mockAggregator, control, () => null!, null!);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("handler");
        }

        #endregion
    }
}
