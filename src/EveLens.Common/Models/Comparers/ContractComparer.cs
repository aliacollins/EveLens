// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.SettingsObjects;
using EveLens.Common.Data;

namespace EveLens.Common.Models.Comparers
{
    /// <summary>
    /// Performs a comparison between two <see cref="Contract"/> types.
    /// </summary>
    public sealed class ContractComparer : Comparer<Contract>
    {
        private readonly ContractColumn m_column;
        private readonly bool m_isAscending;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractComparer"/> class.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="isAscending">Is ascending flag.</param>
        public ContractComparer(ContractColumn column, bool isAscending)
        {
            m_column = column;
            m_isAscending = isAscending;
        }

        /// <summary>
        /// Performs a comparison of two objects of the <see cref="Contract" /> type and returns a value
        /// indicating whether one object is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        /// Less than zero
        /// <paramref name="x"/> is less than <paramref name="y"/>.
        /// Zero
        /// <paramref name="x"/> equals <paramref name="y"/>.
        /// Greater than zero
        /// <paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        public override int Compare(Contract? x, Contract? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            if (m_isAscending)
                return CompareCore(x, y);

            return -CompareCore(x, y);
        }

        /// <summary>
        /// Performs a comparison of two objects of the <see cref="Contract" /> type and returns a value
        /// indicating whether one object is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        /// Less than zero
        /// <paramref name="x"/> is less than <paramref name="y"/>.
        /// Zero
        /// <paramref name="x"/> equals <paramref name="y"/>.
        /// Greater than zero
        /// <paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        private int CompareCore(Contract x, Contract y)
        {
            Station? xStart = x.StartStation, yStart = y.StartStation, xEnd = x.EndStation,
                yEnd = y.EndStation;
            switch (m_column)
            {
            case ContractColumn.Status:
                return x.Status.CompareTo(y.Status);
            case ContractColumn.ContractText:
                return string.Compare(x.ContractText, y.ContractText, StringComparison.CurrentCulture);
            case ContractColumn.ContractType:
                return x.ContractType.CompareTo(y.ContractType);
            case ContractColumn.Issuer:
                return string.Compare(x.Issuer, y.Issuer, StringComparison.CurrentCulture);
            case ContractColumn.Assignee:
                return string.Compare(x.Assignee, y.Assignee, StringComparison.CurrentCulture);
            case ContractColumn.Issued:
                return x.Issued.CompareTo(y.Issued);
            case ContractColumn.Expiration:
                return x.Expiration.CompareTo(y.Expiration);
            case ContractColumn.Title:
                return string.Compare(x.Description, y.Description, StringComparison.CurrentCulture);
            case ContractColumn.Acceptor:
                return string.Compare(x.Acceptor, y.Acceptor, StringComparison.CurrentCulture);
            case ContractColumn.Availability:
                return x.Availability.CompareTo(y.Availability);
            case ContractColumn.Price:
                return x.Price.CompareTo(y.Price);
            case ContractColumn.Buyout:
                return x.Buyout.CompareTo(y.Buyout);
            case ContractColumn.Reward:
                return x.Reward.CompareTo(y.Reward);
            case ContractColumn.Collateral:
                return x.Collateral.CompareTo(y.Collateral);
            case ContractColumn.Volume:
                return x.Volume.CompareTo(y.Volume);
            case ContractColumn.StartLocation:
                if (xStart == null && yStart == null) return 0;
                if (xStart == null) return -1;
                return xStart.CompareTo(yStart);
            case ContractColumn.StartRegion:
                return (xStart?.SolarSystemChecked?.Constellation?.Region)?.CompareTo(
                    yStart?.SolarSystemChecked?.Constellation?.Region) ?? 0;
            case ContractColumn.StartSolarSystem:
                return xStart?.SolarSystemChecked?.CompareTo(yStart?.SolarSystemChecked) ?? 0;
            case ContractColumn.StartStation:
                if (xStart == null && yStart == null) return 0;
                if (xStart == null) return -1;
                return xStart.CompareTo(yStart);
            case ContractColumn.EndLocation:
                if (xEnd == null && yEnd == null) return 0;
                if (xEnd == null) return -1;
                return xEnd.CompareTo(yEnd);
            case ContractColumn.EndRegion:
                return (xEnd?.SolarSystemChecked?.Constellation?.Region)?.CompareTo(
                    yEnd?.SolarSystemChecked?.Constellation?.Region) ?? 0;
            case ContractColumn.EndSolarSystem:
                return xEnd?.SolarSystemChecked?.CompareTo(yEnd?.SolarSystemChecked) ?? 0;
            case ContractColumn.EndStation:
                if (xEnd == null && yEnd == null) return 0;
                if (xEnd == null) return -1;
                return xEnd.CompareTo(yEnd);
            case ContractColumn.Accepted:
                return x.Accepted.CompareTo(y.Accepted);
            case ContractColumn.Completed:
                return x.Completed.CompareTo(y.Completed);
            case ContractColumn.Duration:
                return x.Duration.CompareTo(y.Duration);
            case ContractColumn.DaysToComplete:
                return x.DaysToComplete.CompareTo(y.DaysToComplete);
            case ContractColumn.IssuedFor:
                return x.IssuedFor.CompareTo(y.IssuedFor);
            default:
                return 0;
            }
        }
    }
}
