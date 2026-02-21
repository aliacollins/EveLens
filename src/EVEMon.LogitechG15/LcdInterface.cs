// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using LogitechLcd.NET;

namespace EVEMon.LogitechG15
{
    internal static class LcdInterface
    {
        private static bool s_isAvailable;
        private static bool s_lcdInterfaceInitialized;
        private static bool s_result;
        private static string s_appName = string.Empty;

        private static readonly ILogitechLcd s_lgLcd = LogitechLcd.NET.LogitechLcd.Instance;


        #region Internal Methods

        /// <summary>
        /// Opens the LCD with the specified application name.
        /// </summary>
        /// <param name="appName">Name of the app.</param>
        /// <returns></returns>
        internal static void Open(string appName)
        {
            s_appName = appName;
            Open();
        }

        /// <summary>
        /// Opens the LCD.
        /// </summary>
        private static void Open()
        {
            try
            {
                if (s_isAvailable)
                    return;

                // Initialize interface to LCD library if needed
                if (!s_lcdInterfaceInitialized)
                {
                    // Initialize the library
                    s_result = s_lgLcd.Init(s_appName, (int)LogitechLcdConstants.LogiLcdType.LogiLCDTypeMono);

                    // Is an LCD available?
                    if (!s_result)
                        return;

                    s_lcdInterfaceInitialized = true;
                }

                s_result = s_lgLcd.IsConnected((int)LogitechLcdConstants.LogiLcdType.LogiLCDTypeMono);

                if (s_result)
                    s_isAvailable = true;
                else
                    s_lgLcd.Shutdown();
            }
            catch (Exception ex)
            {
                // This might happen for a number of reasons .. most likely missing the lgLcd library (LogitechLcd.x##.dll)
                Console.Write(@"Open Caught Exception: ");
                Console.WriteLine(ex.Message);
                s_lgLcd.Shutdown();
            }
        }

        /// <summary>
        /// Closes the LCD.
        /// </summary>
        /// <returns></returns>
        internal static void Close()
        {
            try
            {
                s_isAvailable = false;
                s_lcdInterfaceInitialized = false;
                s_lgLcd.Shutdown();
            }
            catch (Exception ex)
            {
                // This might happen for a number of reasons .. most likely missing the lgLcd library (LogitechLcd.x##.dll)
                Console.Write(@"Close Caught Exception: ");
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Displays the bitmap on the LCD screen.
        /// </summary>
        /// <param name="sampleBitmap">The sampleBitmap.</param>
        /// <returns></returns>
        internal static void DisplayBitmap(byte[] sampleBitmap)
        {
            try
            {
                // Initialize if not done yet
                if (!s_lcdInterfaceInitialized || !s_isAvailable)
                    Open(s_appName);

                // Is an LCD available?
                if (!s_lcdInterfaceInitialized)
                    return;

                // Is an LCD available?
                if (!s_isAvailable)
                {
                    Close();
                    return;
                }

                // Display bitmap if LCD is found
                s_lgLcd.MonoSetBackground(sampleBitmap);
                s_lgLcd.Update();
            }
            catch (Exception ex)
            {
                // This might happen for a number of reasons .. most likely missing the lgLcd library (LogitechLcd.x##.dll)
                Console.Write(@"DisplayBitmap Caught Exception: ");
                Console.WriteLine(ex.Message);
                Close();
            }
        }

        /// <summary>
        /// Reads the soft button.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <returns></returns>
        internal static bool ReadSoftButton(int button)
        {
            try
            {
                // Has the LCD been disconnected?
                if (!s_isAvailable)
                    return false;

                s_result = s_lgLcd.IsButtonPressed(button);
            }
            catch (Exception ex)
            {
                // This might happen for a number of reasons .. most likely missing the lgLcd library (LogitechLcd.x##.dll)
                Console.Write(@"ReadSoftButton Caught Exception: ");
                Console.WriteLine(ex.Message);
                Close();
            }
            return s_result;
        }

        #endregion

    }
}