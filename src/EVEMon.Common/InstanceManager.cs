// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;

namespace EVEMon.Common
{
    public sealed class InstanceManager
    {
        public event EventHandler<EventArgs> Signaled;

        private static InstanceManager s_instanceManager;

        private readonly Semaphore m_semaphore;
        private readonly bool m_createdNew;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceManager"/> class.
        /// </summary>
        private InstanceManager()
        {
            using (Semaphore semaphore = new Semaphore(0, 1, "EVEMonInstance", out m_createdNew))
            {
                ThreadPool.RegisterWaitForSingleObject(semaphore, SemaphoreReleased, null, -1, false);
                m_semaphore = semaphore;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a new instance has been created.
        /// </summary>
        /// <value><c>true</c> if  a new instance has been created; otherwise, <c>false</c>.</value>
        public bool CreatedNew => m_createdNew;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <returns></returns>
        public static InstanceManager Instance => s_instanceManager ?? (s_instanceManager = new InstanceManager());

        /// <summary>
        /// Fires the event.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <param name="b">if set to <c>true</c> [b].</param>
        private void SemaphoreReleased(object o, bool b)
        {
            Signaled?.ThreadSafeInvoke(this, new EventArgs());
        }

        /// <summary>
        /// Releases the semaphore.
        /// </summary>
        public void Signal()
        {
            try
            {
                m_semaphore.Release();
            }
            catch (SemaphoreFullException e)
            {
                ExceptionHandler.LogException(e, false);
            }
        }
    }
}