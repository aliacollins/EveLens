// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Threading;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;

namespace EVEMon.Common
{
    public sealed class InstanceManager
    {
        public event EventHandler<EventArgs> Signaled;

        private static InstanceManager s_instanceManager;

        private readonly bool m_createdNew;
        private FileStream m_lockFile;
        private RegisteredWaitHandle m_waitHandle;
        private EventWaitHandle m_signal;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceManager"/> class.
        /// Uses a file-based lock for cross-platform single-instance detection,
        /// and a named EventWaitHandle for inter-process signaling.
        /// </summary>
        private InstanceManager()
        {
            string lockPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EVEMon", ".instance-lock");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
                m_lockFile = new FileStream(lockPath, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
                m_createdNew = true;
            }
            catch (IOException)
            {
                // File is locked by another instance
                m_createdNew = false;
            }

            // Set up cross-process signaling via named EventWaitHandle
            try
            {
                m_signal = new EventWaitHandle(false, EventResetMode.AutoReset, "EVEMonInstanceSignal",
                    out _);
                m_waitHandle = ThreadPool.RegisterWaitForSingleObject(
                    m_signal, SignalReceived, null, -1, false);
            }
            catch (Exception e)
            {
                // Named events may not be available on all platforms; degrade gracefully
                ExceptionHandler.LogException(e, false);
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
        /// Fires the event when signaled by another instance.
        /// </summary>
        private void SignalReceived(object o, bool b)
        {
            Signaled?.ThreadSafeInvoke(this, new EventArgs());
        }

        /// <summary>
        /// Signals the existing instance (e.g., to bring it to the foreground).
        /// </summary>
        public void Signal()
        {
            try
            {
                m_signal?.Set();
            }
            catch (Exception e)
            {
                ExceptionHandler.LogException(e, false);
            }
        }
    }
}
