// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    public static class ControlExtensions
    {
        /// <summary>
        /// Recursively checks among the container anecestry line whether one of the container is in design mode.
        /// This is done because <see cref="Control.DesignMode"/> is false when it is not the control directly edited in the designer, 
        /// like when it is a component of a form or another control.
        /// </summary>
        /// <param name="control"></param>
        /// <returns></returns>
        public static bool IsDesignModeHosted(this Control control)
        {
            while (control != null)
            {
                if (control.Site != null && control.Site.DesignMode)
                    return true;
                control = control.Parent;
            }
            return false;
        }
        
        /// <summary>
        /// Suspends the drawing.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public static void SuspendDrawing(this Control parent)
        {
            NativeMethods.SendMessage(parent.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Resumes the drawing.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public static void ResumeDrawing(this Control parent)
        {
            NativeMethods.SendMessage(parent.Handle, NativeMethods.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
            parent.Refresh();
        }
    }
}