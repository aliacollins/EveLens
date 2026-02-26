// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [CollectionDataContract]
    public sealed class EsiAPISkillQueue : List<EsiSkillQueueListItem>, ISynchronizableWithLocalClock
    {
        public ICollection<SerializableQueuedSkill> CreateSkillQueue()
        {
            var queue = new List<SerializableQueuedSkill>(Count);
            foreach (var queueItem in this)
                queue.Add(queueItem.ToXMLItem());
            return queue;
        }

        #region ISynchronizableWithLocalClock Members

        /// <summary>
        /// Synchronizes the stored times with local clock
        /// </summary>
        /// <param name="drift"></param>
        void ISynchronizableWithLocalClock.SynchronizeWithLocalClock(TimeSpan drift)
        {
            foreach (ISynchronizableWithLocalClock queueItem in this)
                queueItem.SynchronizeWithLocalClock(drift);
        }

        #endregion

    }
}
