// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;

namespace EVEMon.Common.IgbService
{
    /// <summary>
    /// Manages a individual client connection
    /// </summary>
    public class IgbTcpClient
    {
        public event EventHandler<EventArgs> Closed;
        public event EventHandler<IgbClientDataReadEventArgs> DataRead;

        private readonly Object m_syncLock = new Object();
        private readonly TcpClient m_client;

        private const int BufferSize = 4096;

        private byte[] m_buffer;
        private bool m_running;
        private NetworkStream m_stream;


        #region Constructor and Close

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">Client information</param>
        public IgbTcpClient(TcpClient client)
        {
            m_client = client;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            if (!m_running)
                return;

            m_running = false;
            try
            {
                m_client.Close();
            }
            catch (SocketException e)
            {
                ExceptionHandler.LogException(e, false);
            }
            OnClosed();
        }

        /// <summary>
        /// Called when [closed].
        /// </summary>
        private void OnClosed()
        {
            Closed?.ThreadSafeInvoke(this, new EventArgs());
        }

        #endregion


        #region Reading

        /// <summary>
        /// Start reading from the client.
        /// </summary>
        public void Start()
        {
            lock (m_syncLock)
            {
                m_running = true;
                m_stream = m_client.GetStream();
                m_buffer = new byte[BufferSize];
                BeginRead(false);
            }
        }

        /// <summary>
        /// Begin reading from the client.
        /// </summary>
        /// <param name="acquireLock">lock the object</param>
        private void BeginRead(bool acquireLock)
        {
            if (acquireLock)
                Monitor.Enter(m_syncLock);

            try
            {
                IAsyncResult ar;
                do
                {
                    ar = null;
                    if (m_running)
                        ar = m_stream.BeginRead(m_buffer, 0, m_buffer.Length, EndRead, null);
                } while (ar != null && ar.CompletedSynchronously);
            }
            finally
            {
                if (acquireLock)
                    Monitor.Exit(m_syncLock);
            }
        }

        /// <summary>
        /// Async called when reading has finished.
        /// </summary>
        /// <param name="ar">result</param>
        private void EndRead(IAsyncResult ar)
        {
            try
            {
                int bytesRead = m_stream.EndRead(ar);
                if (bytesRead <= 0)
                    Close();
                else
                {
                    OnDataRead(m_buffer, bytesRead);
                    if (!ar.CompletedSynchronously)
                        BeginRead(true);
                }
            }
            catch (IOException ex)
            {
                Close();
                ExceptionHandler.LogException(ex, true);
            }
            catch (Exception ex)
            {
                Close();
                ExceptionHandler.LogRethrowException(ex);
                throw;
            }
        }

        /// <summary>
        /// Event triggered on data read.
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="count">bytes read</param>
        private void OnDataRead(IEnumerable<byte> buffer, int count)
        {
            DataRead?.ThreadSafeInvoke(this, new IgbClientDataReadEventArgs(buffer, count));
        }

        #endregion


        #region Writing

        /// <summary>
        /// Writes the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        public void Write(string str)
        {
            byte[] outbuf = Encoding.UTF8.GetBytes(str);
            m_stream.Write(outbuf, 0, outbuf.Length);
        }

        #endregion
    }
}