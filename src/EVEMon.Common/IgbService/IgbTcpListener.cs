using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EVEMon.Common.Helpers;
using EVEMon.Common.Services;

namespace EVEMon.Common.IgbService
{
    /// <summary>
    /// Listens for IGB client connections
    /// </summary>
    public class IgbTcpListener
    {
        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        private readonly Object m_syncLock = new Object();
        private readonly IPEndPoint m_listenEndpoint;

        private bool m_running;
        private TcpListener m_listener;


        #region Constructor, Start and Stop

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ep"></param>
        public IgbTcpListener(IPEndPoint ep)
        {
            m_listenEndpoint = ep;
        }

        /// <summary>
        /// Start listening.
        /// </summary>
        public void Start()
        {
            lock (m_syncLock)
            {
                if (m_running)
                    return;

                m_running = true;
                m_listener = new TcpListener(m_listenEndpoint);
                try
                {
                    m_listener.Start();
                    BeginAcceptTcpClient(false);
                }
                catch (SocketException ex)
                {
                    // Null out the listener then notify the trace file and the user
                    m_listener = null;
                    AppServices.TraceService?.Trace($"{ex.SocketErrorCode} - {ex.Message} - {ex.ErrorCode}", printMethod: false);
                    AppServices.Notifications.NotifyIgbServiceException(m_listenEndpoint.Port);
                    return;
                }

                AppServices.Notifications.InvalidateIgbServiceException();
            }
        }

        /// <summary>
        /// Stop listening.
        /// </summary>
        /// <remarks>
        /// My wife thinks I use this function too often.
        /// </remarks>
        public void Stop()
        {
            lock (m_syncLock)
            {
                if (!m_running)
                    return;

                m_running = false;
                if (m_listener == null)
                    return;

                m_listener.Stop();
                m_listener = null;
            }
        }

        #endregion


        #region Client Connected

        /// <summary>
        /// Begin accepting a client connection
        /// </summary>
        /// <param name="acquireLock">require lock on object</param>
        private void BeginAcceptTcpClient(bool acquireLock)
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
                        ar = m_listener.BeginAcceptTcpClient(EndAcceptTcpClient, null);
                } while (ar != null && ar.CompletedSynchronously);
            }
            finally
            {
                if (acquireLock)
                    Monitor.Exit(m_syncLock);
            }
        }

        /// <summary>
        /// TCP client connection acceptance has completed
        /// </summary>
        /// <param name="ar">results</param>
        private void EndAcceptTcpClient(IAsyncResult ar)
        {
            lock (m_syncLock)
            {
                // Has the client been stopped ?
                if (m_listener == null)
                    return;

                try
                {
                    bool inLock = ar.CompletedSynchronously;

                    TcpClient newClient = m_listener.EndAcceptTcpClient(ar);
                    OnClientConnected(newClient, !inLock);
                    if (!ar.CompletedSynchronously)
                        BeginAcceptTcpClient(true);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.LogRethrowException(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Triggered on client connection
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="acquireLock">Require lock on object</param>
        private void OnClientConnected(TcpClient client, bool acquireLock)
        {
            if (acquireLock)
                Monitor.Enter(m_syncLock);

            try
            {
                if (m_running && ClientConnected != null)
                    ClientConnected(this, new ClientConnectedEventArgs(client));
                else
                {
                    try
                    {
                        client.Close();
                    }
                    catch (SocketException e)
                    {
                        ExceptionHandler.LogException(e, false);
                    }
                }
            }
            finally
            {
                if (acquireLock)
                    Monitor.Exit(m_syncLock);
            }
        }

        #endregion
    }
}