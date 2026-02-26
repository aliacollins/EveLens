// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Constants;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Extensions;
using EveLens.Common.Net;
using EveLens.Common.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Ensures synchronization of local time to a known time source.
    /// </summary>
    public static class TimeCheck
    {
        /// <summary>
        /// Occurs when time check completed.
        /// </summary>
        public static event EventHandler<TimeCheckSyncEventArgs> TimeCheckCompleted;

        /// <summary>
        /// Check for time synchronization,
        /// or reschedule it for later if no connection is available.
        /// </summary>
        public static void ScheduleCheck(TimeSpan time)
        {
            AppServices.Dispatcher?.Schedule(time, () => BeginCheckAsync().ConfigureAwait(false));
            AppServices.TraceService?.Trace($"in {time}");
        }

        /// <summary>
        /// Method to determine if the user's clock is syncrhonised to NTP time pool.
        /// Updated to move to NTP (global NTP pool) rather than NIST port 13 time check, which is being deprecated
        /// </summary>
        private static async Task BeginCheckAsync()
        {
            if (!NetworkMonitor.IsNetworkAvailable)
            {
                ScheduleCheck(TimeSpan.FromMinutes(1));
                return;
            }

            AppServices.TraceService?.Trace((string)null);

            string ntpServer = NetworkConstants.GlobalNTPPool;

            try
            {
                IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(ntpServer).ConfigureAwait(false);

                if (!ipAddresses.Any())
                    return;

                DateTime localTime = DateTime.Now;

                byte[] ntpData = await Task.Run(() =>
                {
                    var data = new byte[48];
                    data[0] = 0x1B;

                    var ipEndPoint = new IPEndPoint(ipAddresses.First(), 123);
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        socket.ReceiveTimeout = 5000;
                        socket.SendTimeout = 5000;
                        socket.Connect(ipEndPoint);
                        socket.Send(data);
                        socket.Receive(data);
                        socket.Close();
                    }
                    return data;
                }).ConfigureAwait(false);

                ulong intPart = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
                ulong fractPart = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                var networkDateTime = (new DateTime(1900, 1, 1)).AddMilliseconds((long)milliseconds);

                DateTime serverTimeToLocalTime = networkDateTime.ToLocalTime();
                TimeSpan timediff = TimeSpan.FromSeconds(Math.Abs(serverTimeToLocalTime.Subtract(localTime).TotalSeconds));
                bool isSynchronised = timediff < TimeSpan.FromSeconds(60);

                OnCheckCompleted(isSynchronised, serverTimeToLocalTime, localTime);
            }
            catch (Exception exc)
            {
                CheckFailure(exc);
            }
        }

        /// <summary>
        /// Called when the check fails.
        /// </summary>
        /// <param name="exc">The exc.</param>
        private static void CheckFailure(Exception exc)
        {
            AppServices.TraceService?.Trace(exc.Message);
            ScheduleCheck(TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Called when time check completed.
        /// </summary>
        /// <param name="isSynchronised">if set to <c>true</c> [is synchronised].</param>
        /// <param name="serverTimeToLocalTime">The server time to local time.</param>
        /// <param name="localTime">The local time.</param>
        private static void OnCheckCompleted(bool isSynchronised, DateTime serverTimeToLocalTime, DateTime localTime)
        {
            AppServices.TraceService?.Trace(Settings.Updates.CheckTimeOnStartup ?  "Synchronised" : "Disabled");

            TimeCheckCompleted?.ThreadSafeInvoke(null, new TimeCheckSyncEventArgs(isSynchronised, serverTimeToLocalTime, localTime));

            // Reschedule
            ScheduleCheck(TimeSpan.FromDays(1));
        }
    }
}
