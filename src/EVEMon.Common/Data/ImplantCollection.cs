// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EVEMon.Common.Collections;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents a collection of all the implants bound to a given group.
    /// </summary>
    public sealed class ImplantCollection : ReadonlyCollection<Implant>
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="slot"></param>
        internal ImplantCollection(ImplantSlots slot)
        {
            Items.Add(new Implant(slot));
        }

        #endregion


        #region Indexers

        /// <summary>
        /// Gets an implant by its name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Implant this[string name] => Items.FirstOrDefault(implant => implant.Name == name);

        #endregion


        #region Helper Methods

        /// <summary>
        /// Add an implant to this slot.
        /// </summary>
        /// <param name="implant"></param>
        internal void Add(Implant implant)
        {
            Items.Add(implant);
        }

        #endregion
    }
}