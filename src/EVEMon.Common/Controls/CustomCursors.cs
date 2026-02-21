// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Windows.Forms;
using EVEMon.Common.Helpers;

namespace EVEMon.Common.Controls
{
    public static class CustomCursors
    {
        private static Cursor s_contextMenuCursor;

        /// <summary>
        /// Gets the context menu cursor.
        /// </summary>
        /// <value>
        /// The context menu.
        /// </value>
        public static Cursor ContextMenu
            => s_contextMenuCursor ??
               (s_contextMenuCursor = GetCursorFromResource(Properties.Resources.ContextMenuPointer));

        /// <summary>
        /// Gets the cursor from resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns></returns>
        private static Cursor GetCursorFromResource(byte[] resource)
        {
            try
            {
                string tempFile = Path.GetTempFileName();

                using (MemoryStream cursorStream = new MemoryStream(resource))
                using (FileStream fileStream = Util.GetFileStream(tempFile))
                    cursorStream.WriteTo(fileStream);

                Cursor cursor = new Cursor(NativeMethods.LoadCursorFromFile(tempFile));
                FileHelper.DeleteFile(tempFile);

                return cursor;
            }
            catch (Exception exc)
            {
                ExceptionHandler.LogException(exc, true);
                return Cursor.Current;
            }
        }
    }
}
