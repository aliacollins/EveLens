// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EVEMon.Common
{
    public sealed class AsyncPump
    {
        /// <summary>
        /// Runs the specified function.
        /// </summary>
        /// <param name="func">The function.</param>
        public static void Run(Func<Task> func)
        {
            SynchronizationContext prevCtx = SynchronizationContext.Current;
            try
            {
                var syncCtx = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(syncCtx);

                Task task = func();
                task.ContinueWith(_ => syncCtx.Complete(), TaskScheduler.Default);

                syncCtx.RunOnCurrentThread();

                task.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevCtx);
            }
        }

        private sealed class SingleThreadSynchronizationContext : SynchronizationContext
        {
            private readonly
                BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
                    new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

            /// <summary>
            /// Posts the specified callback.
            /// </summary>
            /// <param name="d">The callback.</param>
            /// <param name="state">The state.</param>
            public override void Post(SendOrPostCallback d, object state)
            {
                m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
            }

            /// <summary>
            /// Runs the on current thread.
            /// </summary>
            public void RunOnCurrentThread()
            {
                KeyValuePair<SendOrPostCallback, object> workItem;
                while (m_queue.TryTake(out workItem, Timeout.Infinite))
                    workItem.Key(workItem.Value);
            }

            /// <summary>
            /// Completes this instance.
            /// </summary>
            public void Complete()
            {
                m_queue.CompleteAdding();
            }
        }
    }
}