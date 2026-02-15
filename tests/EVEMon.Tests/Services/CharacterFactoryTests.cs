using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    public class CharacterFactoryTests
    {
        private readonly ICharacterRepository _repo;
        private readonly IEventAggregator _events;
        private readonly CharacterFactory _factory;

        public CharacterFactoryTests()
        {
            _repo = Substitute.For<ICharacterRepository>();
            _events = Substitute.For<IEventAggregator>();
            _factory = new CharacterFactory(_repo, _events);
        }

        private static ICharacterIdentity CreateMockIdentity(long characterId = 12345, string name = "Test Char")
        {
            var identity = Substitute.For<ICharacterIdentity>();
            identity.CharacterID.Returns(characterId);
            identity.Name.Returns(name);
            identity.Guid.Returns(Guid.NewGuid());
            return identity;
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            Action act = () => new CharacterFactory(null!, _events);
            act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
        }

        [Fact]
        public void Constructor_NullEventAggregator_ThrowsArgumentNullException()
        {
            Action act = () => new CharacterFactory(_repo, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("eventAggregator");
        }

        #endregion

        #region CreateFromSerialized Tests

        [Fact]
        public void CreateFromSerialized_ValidInputs_ReturnsNonNullIdentity()
        {
            // Arrange
            var identity = CreateMockIdentity();
            var serializedData = new object();

            // Act
            var result = _factory.CreateFromSerialized(identity, serializedData);

            // Assert
            result.Should().NotBeNull();
            result.CharacterID.Should().Be(12345);
        }

        [Fact]
        public void CreateFromSerialized_NullIdentity_ThrowsArgumentNullException()
        {
            Action act = () => _factory.CreateFromSerialized(null!, new object());
            act.Should().Throw<ArgumentNullException>().WithParameterName("identity");
        }

        [Fact]
        public void CreateFromSerialized_NullData_ThrowsArgumentNullException()
        {
            var identity = CreateMockIdentity();
            Action act = () => _factory.CreateFromSerialized(identity, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("serializedData");
        }

        [Fact]
        public void CreateFromSerialized_IncreasesManagedCount()
        {
            // Arrange
            var identity = CreateMockIdentity();
            _factory.ManagedCount.Should().Be(0);

            // Act
            _factory.CreateFromSerialized(identity, new object());

            // Assert
            _factory.ManagedCount.Should().Be(1);
        }

        [Fact]
        public void CreateFromSerialized_PublishesCharacterCreatedEvent()
        {
            // Arrange
            var identity = CreateMockIdentity();

            // Act
            _factory.CreateFromSerialized(identity, new object());

            // Assert
            _events.Received(1).Publish(Arg.Is<CharacterCreatedEvent>(
                e => e.Identity == identity && e.FromSerialized == true));
        }

        #endregion

        #region CreateNew Tests

        [Fact]
        public void CreateNew_ReturnsIdentityWithCorrectCharacterID()
        {
            // Arrange
            var identity = CreateMockIdentity(99999, "New Char");

            // Act
            var result = _factory.CreateNew(identity);

            // Assert
            result.Should().NotBeNull();
            result.CharacterID.Should().Be(99999);
            result.Name.Should().Be("New Char");
        }

        [Fact]
        public void CreateNew_NullIdentity_ThrowsArgumentNullException()
        {
            Action act = () => _factory.CreateNew(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("identity");
        }

        [Fact]
        public void CreateNew_IncreasesManagedCount()
        {
            // Arrange
            var identity = CreateMockIdentity();
            _factory.ManagedCount.Should().Be(0);

            // Act
            _factory.CreateNew(identity);

            // Assert
            _factory.ManagedCount.Should().Be(1);
        }

        [Fact]
        public void CreateNew_PublishesCharacterCreatedEventWithFromSerializedFalse()
        {
            // Arrange
            var identity = CreateMockIdentity();

            // Act
            _factory.CreateNew(identity);

            // Assert
            _events.Received(1).Publish(Arg.Is<CharacterCreatedEvent>(
                e => e.Identity == identity && e.FromSerialized == false));
        }

        #endregion

        #region DisposeCharacter Tests

        [Fact]
        public void DisposeCharacter_DecreasesManagedCount()
        {
            // Arrange
            var identity = CreateMockIdentity();
            _factory.CreateNew(identity);
            _factory.ManagedCount.Should().Be(1);

            // Act
            _factory.DisposeCharacter(identity);

            // Assert
            _factory.ManagedCount.Should().Be(0);
        }

        [Fact]
        public void DisposeCharacter_UnknownIdentity_DoesNotThrow()
        {
            // Arrange
            var identity = CreateMockIdentity(77777);

            // Act & Assert
            Action act = () => _factory.DisposeCharacter(identity);
            act.Should().NotThrow();
        }

        [Fact]
        public void DisposeCharacter_PublishesCharacterDisposedEvent()
        {
            // Arrange
            var identity = CreateMockIdentity();
            _factory.CreateNew(identity);

            // Act
            _factory.DisposeCharacter(identity);

            // Assert
            _events.Received(1).Publish(Arg.Is<CharacterDisposedEvent>(
                e => e.Identity == identity));
        }

        [Fact]
        public void DisposeCharacter_UnknownIdentity_DoesNotPublishEvent()
        {
            // Arrange
            var identity = CreateMockIdentity(77777);

            // Act
            _factory.DisposeCharacter(identity);

            // Assert
            _events.DidNotReceive().Publish(Arg.Any<CharacterDisposedEvent>());
        }

        #endregion

        #region Bulk and Thread Safety Tests

        [Fact]
        public void Create100Characters_ManagedCountIs100()
        {
            // Act
            for (int i = 0; i < 100; i++)
            {
                var identity = CreateMockIdentity(characterId: 10000 + i, name: $"Char {i}");
                _factory.CreateNew(identity);
            }

            // Assert
            _factory.ManagedCount.Should().Be(100);
        }

        [Fact]
        public void ConcurrentCreateNew_MaintainsCorrectCount()
        {
            // Arrange
            const int threadCount = 50;
            var barrier = new Barrier(threadCount);
            var tasks = new List<Task>();

            // Act - create characters concurrently from multiple threads
            for (int i = 0; i < threadCount; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var identity = CreateMockIdentity(characterId: 20000 + index, name: $"Concurrent {index}");
                    barrier.SignalAndWait();
                    _factory.CreateNew(identity);
                }));
            }
            Task.WaitAll(tasks.ToArray());

            // Assert
            _factory.ManagedCount.Should().Be(threadCount);
        }

        [Fact]
        public void ConcurrentCreateAndDispose_MaintainsConsistency()
        {
            // Arrange - pre-create some characters
            var identities = new ICharacterIdentity[20];
            for (int i = 0; i < 20; i++)
            {
                identities[i] = CreateMockIdentity(characterId: 30000 + i, name: $"Stress {i}");
                _factory.CreateNew(identities[i]);
            }
            _factory.ManagedCount.Should().Be(20);

            // Act - concurrently dispose the first 10 and create 10 new ones
            var barrier = new Barrier(20);
            var tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    _factory.DisposeCharacter(identities[index]);
                }));
            }

            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() =>
                {
                    var identity = CreateMockIdentity(characterId: 40000 + index, name: $"New Stress {index}");
                    barrier.SignalAndWait();
                    _factory.CreateNew(identity);
                }));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert - 20 original - 10 disposed + 10 new = 20
            _factory.ManagedCount.Should().Be(20);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void CreateFromSerialized_SameCharacterTwice_OverwritesEntry()
        {
            // Arrange
            var identity = CreateMockIdentity(55555);

            // Act
            _factory.CreateFromSerialized(identity, new object());
            _factory.CreateFromSerialized(identity, new object());

            // Assert - should not double-count, ConcurrentDictionary overwrites
            _factory.ManagedCount.Should().Be(1);
        }

        [Fact]
        public void ManagedCount_InitiallyZero()
        {
            _factory.ManagedCount.Should().Be(0);
        }

        #endregion
    }
}
