// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Controls
{
    /// <summary>
    /// Contains helper methods to change extended styles on ListView, including enabling double buffering.
    /// Based on Giovanni Montrone's article on <see cref="http://www.codeproject.com/KB/list/listviewxp.aspx"/>
    /// </summary>
    public static class ListViewHelper
    {
        /// <summary>
        /// Sets the extended style.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="exStyle">The ex style.</param>
        /// <exception cref="System.ArgumentNullException">control</exception>
        public static void SetExtendedStyle(Control control, ListViewExtendedStyles exStyle)
        {
            control.ThrowIfNull(nameof(control));

            ListViewExtendedStyles styles =
                (ListViewExtendedStyles)NativeMethods.SendMessage(control.Handle, (int)ListViewMessages.GetExtendedStyle,
                                                                  IntPtr.Zero, IntPtr.Zero);
            styles |= exStyle;
            NativeMethods.SendMessage(control.Handle, (int)ListViewMessages.SetExtendedStyle, IntPtr.Zero, (IntPtr)styles);
        }

        /// <summary>
        /// Enables the double buffer.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <exception cref="System.ArgumentNullException">control</exception>
        public static void EnableDoubleBuffer(Control control)
        {
            control.ThrowIfNull(nameof(control));

            // read current style
            ListViewExtendedStyles styles =
                (ListViewExtendedStyles)NativeMethods.SendMessage(control.Handle, (int)ListViewMessages.GetExtendedStyle,
                                                                  IntPtr.Zero, IntPtr.Zero);

            // enable double buffer and border select
            styles |= ListViewExtendedStyles.DoubleBuffer | ListViewExtendedStyles.BorderSelect;
            // write new style
            NativeMethods.SendMessage(control.Handle, (int)ListViewMessages.SetExtendedStyle, IntPtr.Zero, (IntPtr)styles);
        }

        /// <summary>
        /// Disables the double buffer.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <exception cref="System.ArgumentNullException">control</exception>
        public static void DisableDoubleBuffer(Control control)
        {
            control.ThrowIfNull(nameof(control));

            // read current style
            ListViewExtendedStyles styles =
                (ListViewExtendedStyles)NativeMethods.SendMessage(control.Handle, (int)ListViewMessages.GetExtendedStyle,
                                                                  IntPtr.Zero, IntPtr.Zero);
            // disable double buffer and border select
            styles -= styles & ListViewExtendedStyles.DoubleBuffer;
            styles -= styles & ListViewExtendedStyles.BorderSelect;
            // write new style
            NativeMethods.SendMessage(control.Handle, (int)ListViewMessages.SetExtendedStyle, IntPtr.Zero, (IntPtr)styles);
        }

        private enum ListViewMessages
        {
            First = 0x1000,
            SetExtendedStyle = First + 54,
            GetExtendedStyle = First + 55,
        }
    }

    [Flags]
    public enum ListViewExtendedStyles
    {
        /// <summary>
        /// LVS_EX_GRIDLINES
        /// </summary>
        GridLines = 0x00000001,

        /// <summary>
        /// LVS_EX_SUBITEMIMAGES
        /// </summary>
        SubItemImages = 0x00000002,

        /// <summary>
        /// LVS_EX_CHECKBOXES
        /// </summary>
        CheckBoxes = 0x00000004,

        /// <summary>
        /// LVS_EX_TRACKSELECT
        /// </summary>
        TrackSelect = 0x00000008,

        /// <summary>
        /// LVS_EX_HEADERDRAGDROP
        /// </summary>
        HeaderDragDrop = 0x00000010,

        /// <summary>
        /// LVS_EX_FULLROWSELECT
        /// </summary>
        FullRowSelect = 0x00000020,

        /// <summary>
        /// LVS_EX_ONECLICKACTIVATE
        /// </summary>
        OneClickActivate = 0x00000040,

        /// <summary>
        /// LVS_EX_TWOCLICKACTIVATE
        /// </summary>
        TwoClickActivate = 0x00000080,

        /// <summary>
        /// LVS_EX_FLATSB
        /// </summary>
        FlatsB = 0x00000100,

        /// <summary>
        /// LVS_EX_REGIONAL
        /// </summary>
        Regional = 0x00000200,

        /// <summary>
        /// LVS_EX_INFOTIP
        /// </summary>
        InfoTip = 0x00000400,

        /// <summary>
        /// LVS_EX_UNDERLINEHOT
        /// </summary>
        UnderlineHot = 0x00000800,

        /// <summary>
        /// LVS_EX_UNDERLINECOLD
        /// </summary>
        UnderlineCold = 0x00001000,

        /// <summary>
        /// LVS_EX_MULTIWORKAREAS
        /// </summary>
        MultilWorkAreas = 0x00002000,

        /// <summary>
        /// LVS_EX_LABELTIP
        /// </summary>
        LabelTip = 0x00004000,

        /// <summary>
        /// LVS_EX_BORDERSELECT
        /// </summary>
        BorderSelect = 0x00008000,

        /// <summary>
        /// LVS_EX_DOUBLEBUFFER
        /// </summary>
        DoubleBuffer = 0x00010000,

        /// <summary>
        /// LVS_EX_HIDELABELS
        /// </summary>
        HideLabels = 0x00020000,

        /// <summary>
        /// LVS_EX_SINGLEROW
        /// </summary>
        SingleRow = 0x00040000,

        /// <summary>
        /// LVS_EX_SNAPTOGRID
        /// </summary>
        SnapToGrid = 0x00080000,

        /// <summary>
        /// LVS_EX_SIMPLESELECT
        /// </summary>
        SimpleSelect = 0x00100000
    }
}