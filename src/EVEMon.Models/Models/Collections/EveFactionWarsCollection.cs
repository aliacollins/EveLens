using System.Collections.Generic;
using EVEMon.Common.Collections;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Models.Collections
{
    public class EveFactionWarsCollection : ReadonlyCollection<EveFactionWar>
    {
        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The source.</param>
        internal void Import(IEnumerable<SerializableEveFactionWarsListItem> src)
        {
            Items.Clear();

            foreach (SerializableEveFactionWarsListItem item in src)
            {
                Items.Add(new EveFactionWar(item));
            }
        }
    }
}