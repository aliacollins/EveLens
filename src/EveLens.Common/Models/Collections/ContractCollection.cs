// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Settings;

namespace EveLens.Common.Models.Collections
{
    public class ContractCollection : ReadonlyCollection<Contract>
    {
        private readonly CCPCharacter m_character;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        internal ContractCollection(CCPCharacter character)
        {
            m_character = character;
        }


        #region Importation/Exportation Methods

        /// <summary>
        /// Imports an enumeration of serialization objects.
        /// </summary>
        /// <param name="src"></param>
        internal void Import(IEnumerable<SerializableContract> src)
        {
            Items.Clear();
            foreach (SerializableContract srcContract in src)
            {
                Items.Add(new Contract(m_character, srcContract));
            }
        }

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable contracts from the API.</param>
        /// <param name="endedContracts">The ended contracts.</param>
        internal void Import(IEnumerable<EsiContractListItem> src,
            ICollection<Contract> endedContracts)
        {
            // Mark all contracts for deletion
            // If they are found again on the API feed, they won't be deleted
            // and those set as ignored will be left as ignored
            foreach (Contract contract in Items)
                contract.MarkedForDeletion = true;
            // Import the contracts from the API, excluding the expired assigned ones
            var newContracts = new LinkedList<Contract>();
            DateTime now = DateTime.UtcNow;
            foreach (var contract in src)
            {
                var status = contract.Status;
                // For contracts issued to/by us, or finished, or outstanding and unexpired
                if (contract.IssuerID == m_character.CharacterID || status ==
                    CCPContractStatus.Completed.ToString() || status == CCPContractStatus.
                    CompletedByContractor.ToString() || status == CCPContractStatus.
                    CompletedByIssuer.ToString() || (status == CCPContractStatus.Outstanding.
                    ToString() && contract.DateExpired >= now) || contract.AcceptorID ==
                    m_character.CharacterID)
                {
                    // Exclude contracts which expired or were completed too long ago
                    var limit = contract.DateExpired.SafeAddDays(Contract.MaxEndedDays);
                    if ((limit >= now || status == CCPContractStatus.Outstanding.ToString()) &&
                            !Items.Any(x => x.TryImport(contract, endedContracts)))
                        // Exclude contracts which matched an existing contract
                        newContracts.AddLast(new Contract(m_character, contract));
                }
            }
            // Add the new contracts that need attention to be notified to the user
            endedContracts.AddRange(newContracts.Where(newContract => newContract.NeedsAttention));
            // Add the items that are no longer marked for deletion
            newContracts.AddRange(Items.Where(x => !x.MarkedForDeletion));
            Items.Clear();
            Items.AddRange(newContracts);
        }

        /// <summary>
        /// Exports only the character issued contracts to a serialization object for the settings file.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Used to export only the corporation contracts issued by a character.</remarks>
        internal IEnumerable<SerializableContract> ExportOnlyIssuedByCharacter()
            => Items.Where(contract => contract.IssuerID == m_character.CharacterID).Select(contract => contract.Export());

        /// <summary>
        /// Exports the contracts to a serialization object for the settings file.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SerializableContract> Export() => Items.Select(contract => contract.Export());

        #endregion
    }
}
