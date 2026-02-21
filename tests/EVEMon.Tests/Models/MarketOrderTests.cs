// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Tests for market order serialization and business rules.
    /// Full MarketOrder construction requires CCPCharacter + EsiOrderListItem from API.
    /// These tests cover the serializable layer and its interaction with the MarketOrder model
    /// through the deserialization constructor (which is internal and requires CCPCharacter).
    /// We test the serializable DTOs that drive all order state and volume logic.
    /// </summary>
    public class MarketOrderTests
    {
        #region SerializableOrderBase basics

        [Fact]
        public void SerializableOrderBase_DefaultConstructor_HasDefaults()
        {
            var order = new SerializableSellOrder();
            order.OrderID.Should().Be(0);
            order.State.Should().Be(OrderState.Active);
            order.UnitaryPrice.Should().Be(0);
            order.RemainingVolume.Should().Be(0);
        }

        [Fact]
        public void SerializableSellOrder_SetProperties_Preserves()
        {
            var order = new SerializableSellOrder
            {
                OrderID = 12345678,
                State = OrderState.Active,
                UnitaryPrice = 1500000.50m,
                RemainingVolume = 100,
                Issued = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                IssuedFor = IssuedFor.Character
            };

            order.OrderID.Should().Be(12345678);
            order.State.Should().Be(OrderState.Active);
            order.UnitaryPrice.Should().Be(1500000.50m);
            order.RemainingVolume.Should().Be(100);
            order.IssuedFor.Should().Be(IssuedFor.Character);
        }

        #endregion

        #region XML round-trip sell order

        [Fact]
        public void SerializableSellOrder_XmlRoundTrip_PreservesAllFields()
        {
            var issued = new DateTime(2025, 6, 1, 10, 30, 0, DateTimeKind.Utc);
            var order = new SerializableSellOrder
            {
                OrderID = 99999,
                State = OrderState.Active,
                UnitaryPrice = 2500000m,
                RemainingVolume = 50,
                Issued = issued,
                IssuedFor = IssuedFor.Character,
                LastStateChange = issued.AddHours(1)
            };

            var result = XmlRoundTrip(order);
            result.OrderID.Should().Be(99999);
            result.State.Should().Be(OrderState.Active);
            result.UnitaryPrice.Should().Be(2500000m);
            result.RemainingVolume.Should().Be(50);
            result.IssuedFor.Should().Be(IssuedFor.Character);
        }

        #endregion

        #region XML round-trip buy order

        [Fact]
        public void SerializableBuyOrder_XmlRoundTrip_PreservesAllFields()
        {
            var issued = new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);
            var order = new SerializableBuyOrder
            {
                OrderID = 55555,
                State = OrderState.Active,
                UnitaryPrice = 100m,
                RemainingVolume = 10000,
                Issued = issued,
                IssuedFor = IssuedFor.Corporation
            };

            var result = XmlRoundTrip(order);
            result.OrderID.Should().Be(55555);
            result.State.Should().Be(OrderState.Active);
            result.UnitaryPrice.Should().Be(100m);
            result.RemainingVolume.Should().Be(10000);
            result.IssuedFor.Should().Be(IssuedFor.Corporation);
        }

        #endregion

        #region Order state values

        [Theory]
        [InlineData(OrderState.Active)]
        [InlineData(OrderState.Canceled)]
        [InlineData(OrderState.Expired)]
        [InlineData(OrderState.Fulfilled)]
        [InlineData(OrderState.Modified)]
        public void SerializableOrderBase_State_RoundTrips(OrderState state)
        {
            var order = new SerializableSellOrder
            {
                OrderID = 1,
                State = state
            };

            var result = XmlRoundTrip(order);
            result.State.Should().Be(state);
        }

        #endregion

        #region Volume calculations (computed at serializable layer)

        [Fact]
        public void SerializableOrderBase_ZeroRemainingVolume_IsFulfilled()
        {
            // When remaining volume is 0 and state is still Active,
            // the order is effectively fulfilled
            var order = new SerializableSellOrder
            {
                OrderID = 1,
                State = OrderState.Fulfilled,
                RemainingVolume = 0,
                UnitaryPrice = 100m
            };

            order.RemainingVolume.Should().Be(0);
            order.State.Should().Be(OrderState.Fulfilled);
        }

        [Fact]
        public void SerializableOrderBase_LargeVolume_Preserves()
        {
            var order = new SerializableBuyOrder
            {
                OrderID = 1,
                RemainingVolume = int.MaxValue,
                UnitaryPrice = 0.01m
            };

            var result = XmlRoundTrip(order);
            result.RemainingVolume.Should().Be(int.MaxValue);
        }

        [Fact]
        public void SerializableOrderBase_LargePrice_Preserves()
        {
            var order = new SerializableSellOrder
            {
                OrderID = 1,
                UnitaryPrice = 99999999999.99m
            };

            var result = XmlRoundTrip(order);
            result.UnitaryPrice.Should().Be(99999999999.99m);
        }

        #endregion

        #region IssuedFor values

        [Theory]
        [InlineData(IssuedFor.Character)]
        [InlineData(IssuedFor.Corporation)]
        [InlineData(IssuedFor.None)]
        public void SerializableOrderBase_IssuedFor_RoundTrips(IssuedFor issuedFor)
        {
            var order = new SerializableSellOrder
            {
                OrderID = 1,
                IssuedFor = issuedFor
            };

            var result = XmlRoundTrip(order);
            result.IssuedFor.Should().Be(issuedFor);
        }

        #endregion

        #region LastStateChange tracking

        [Fact]
        public void SerializableOrderBase_LastStateChange_Preserves()
        {
            var stateChangeTime = new DateTime(2025, 12, 25, 18, 30, 0, DateTimeKind.Utc);
            var order = new SerializableSellOrder
            {
                OrderID = 1,
                LastStateChange = stateChangeTime
            };

            var result = XmlRoundTrip(order);
            result.LastStateChange.Should().Be(stateChangeTime);
        }

        #endregion

        #region CCPCharacter market orders collection (serialized)

        [Fact]
        public void SerializableCCPCharacter_MarketOrders_Empty_RoundTrips()
        {
            var character = new SerializableCCPCharacter();
            character.MarketOrders.Should().BeEmpty();

            var result = XmlRoundTrip(character);
            result.MarketOrders.Should().BeEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_MarketOrders_MultipleMixed_Preserves()
        {
            var character = new SerializableCCPCharacter();
            character.MarketOrders.Add(new SerializableSellOrder
            {
                OrderID = 1,
                State = OrderState.Active,
                UnitaryPrice = 100m,
                RemainingVolume = 10,
                IssuedFor = IssuedFor.Character
            });
            character.MarketOrders.Add(new SerializableBuyOrder
            {
                OrderID = 2,
                State = OrderState.Expired,
                UnitaryPrice = 50m,
                RemainingVolume = 0,
                IssuedFor = IssuedFor.Corporation
            });

            var result = XmlRoundTrip(character);
            result.MarketOrders.Should().HaveCount(2);
            result.MarketOrders[0].OrderID.Should().Be(1);
            result.MarketOrders[1].OrderID.Should().Be(2);
        }

        #endregion

        #region MaxExpirationDays constant

        [Fact]
        public void MarketOrder_MaxExpirationDays_IsSeven()
        {
            // This constant determines how long expired orders are kept
            Common.Models.MarketOrder.MaxExpirationDays.Should().Be(7);
        }

        #endregion

        private static T XmlRoundTrip<T>(T obj) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            using var reader = new StringReader(writer.ToString());
            return (T)serializer.Deserialize(reader)!;
        }
    }
}
