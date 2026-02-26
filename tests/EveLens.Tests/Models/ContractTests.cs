// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Xml.Serialization;
using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Settings;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// Tests for contract serialization and business rules.
    /// Full Contract construction requires CCPCharacter + EsiContractListItem from API.
    /// These tests cover the serializable layer and constants.
    /// </summary>
    public class ContractTests
    {
        #region SerializableContract basics

        [Fact]
        public void SerializableContract_DefaultConstructor_HasDefaults()
        {
            var contract = new SerializableContract();
            contract.ContractID.Should().Be(0);
            contract.ContractState.Should().Be(ContractState.Assigned);
            contract.ContractType.Should().Be(ContractType.None);
            contract.IssuedFor.Should().Be(IssuedFor.None);
        }

        [Fact]
        public void SerializableContract_SetProperties_Preserves()
        {
            var contract = new SerializableContract
            {
                ContractID = 123456789,
                ContractState = ContractState.Created,
                ContractType = ContractType.ItemExchange,
                IssuedFor = IssuedFor.Character
            };

            contract.ContractID.Should().Be(123456789);
            contract.ContractState.Should().Be(ContractState.Created);
            contract.ContractType.Should().Be(ContractType.ItemExchange);
            contract.IssuedFor.Should().Be(IssuedFor.Character);
        }

        #endregion

        #region XML round-trip

        [Fact]
        public void SerializableContract_XmlRoundTrip_PreservesAllFields()
        {
            var contract = new SerializableContract
            {
                ContractID = 987654321,
                ContractState = ContractState.Finished,
                ContractType = ContractType.Courier,
                IssuedFor = IssuedFor.Corporation
            };

            var result = XmlRoundTrip(contract);
            result.ContractID.Should().Be(987654321);
            result.ContractState.Should().Be(ContractState.Finished);
            result.ContractType.Should().Be(ContractType.Courier);
            result.IssuedFor.Should().Be(IssuedFor.Corporation);
        }

        #endregion

        #region Contract state values

        [Theory]
        [InlineData(ContractState.Assigned)]
        [InlineData(ContractState.Created)]
        [InlineData(ContractState.Canceled)]
        [InlineData(ContractState.Deleted)]
        [InlineData(ContractState.Expired)]
        [InlineData(ContractState.Rejected)]
        [InlineData(ContractState.Finished)]
        [InlineData(ContractState.Failed)]
        public void SerializableContract_State_RoundTrips(ContractState state)
        {
            var contract = new SerializableContract
            {
                ContractID = 1,
                ContractState = state
            };

            var result = XmlRoundTrip(contract);
            result.ContractState.Should().Be(state);
        }

        #endregion

        #region Contract type values

        [Theory]
        [InlineData(ContractType.None)]
        [InlineData(ContractType.ItemExchange)]
        [InlineData(ContractType.Courier)]
        [InlineData(ContractType.Auction)]
        public void SerializableContract_Type_RoundTrips(ContractType contractType)
        {
            var contract = new SerializableContract
            {
                ContractID = 1,
                ContractType = contractType
            };

            var result = XmlRoundTrip(contract);
            result.ContractType.Should().Be(contractType);
        }

        #endregion

        #region IssuedFor values

        [Theory]
        [InlineData(IssuedFor.Character)]
        [InlineData(IssuedFor.Corporation)]
        [InlineData(IssuedFor.None)]
        public void SerializableContract_IssuedFor_RoundTrips(IssuedFor issuedFor)
        {
            var contract = new SerializableContract
            {
                ContractID = 1,
                IssuedFor = issuedFor
            };

            var result = XmlRoundTrip(contract);
            result.IssuedFor.Should().Be(issuedFor);
        }

        #endregion

        #region CCPCharacter contracts collection

        [Fact]
        public void SerializableCCPCharacter_Contracts_Empty_RoundTrips()
        {
            var character = new SerializableCCPCharacter();
            character.Contracts.Should().BeEmpty();

            var result = XmlRoundTrip(character);
            result.Contracts.Should().BeEmpty();
        }

        [Fact]
        public void SerializableCCPCharacter_Contracts_Multiple_Preserves()
        {
            var character = new SerializableCCPCharacter();
            character.Contracts.Add(new SerializableContract
            {
                ContractID = 1,
                ContractState = ContractState.Created,
                ContractType = ContractType.ItemExchange,
                IssuedFor = IssuedFor.Character
            });
            character.Contracts.Add(new SerializableContract
            {
                ContractID = 2,
                ContractState = ContractState.Finished,
                ContractType = ContractType.Courier,
                IssuedFor = IssuedFor.Corporation
            });
            character.Contracts.Add(new SerializableContract
            {
                ContractID = 3,
                ContractState = ContractState.Expired,
                ContractType = ContractType.Auction,
                IssuedFor = IssuedFor.Character
            });

            var result = XmlRoundTrip(character);
            result.Contracts.Should().HaveCount(3);
            result.Contracts[0].ContractID.Should().Be(1);
            result.Contracts[0].ContractState.Should().Be(ContractState.Created);
            result.Contracts[1].ContractID.Should().Be(2);
            result.Contracts[1].ContractType.Should().Be(ContractType.Courier);
            result.Contracts[2].ContractID.Should().Be(3);
            result.Contracts[2].ContractState.Should().Be(ContractState.Expired);
        }

        #endregion

        #region Contract MaxEndedDays constant

        [Fact]
        public void Contract_MaxEndedDays_IsSeven()
        {
            Common.Models.Contract.MaxEndedDays.Should().Be(7);
        }

        #endregion

        #region Large contract IDs

        [Fact]
        public void SerializableContract_LargeContractID_Preserves()
        {
            var contract = new SerializableContract
            {
                ContractID = long.MaxValue
            };

            var result = XmlRoundTrip(contract);
            result.ContractID.Should().Be(long.MaxValue);
        }

        #endregion

        #region Contract state transitions (documentation)

        [Fact]
        public void ContractState_ActiveStates_AreCreatedAndAssigned()
        {
            // Document the business rule: Created and Assigned are active states
            // Contract.IsAvailable checks for these states + not expired
            var created = ContractState.Created;
            var assigned = ContractState.Assigned;

            // Active states should have lower enum values than terminal states
            ((int)created).Should().BeLessThan((int)ContractState.Expired);
            ((int)assigned).Should().BeLessThan((int)ContractState.Expired);
        }

        [Fact]
        public void ContractState_TerminalStates_AreHigherValues()
        {
            // Terminal states: Expired, Rejected, Finished, Failed
            ((int)ContractState.Expired).Should().BeGreaterThanOrEqualTo(4);
            ((int)ContractState.Rejected).Should().BeGreaterThanOrEqualTo(4);
            ((int)ContractState.Finished).Should().BeGreaterThanOrEqualTo(4);
            ((int)ContractState.Failed).Should().BeGreaterThanOrEqualTo(4);
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
