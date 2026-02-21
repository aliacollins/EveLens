// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Windows.Forms;
using EVEMon.Common.Controls.MultiPanel.Design;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Controls.MultiPanel
{
    /// <summary>
    /// A page of the <see cref="MultiPanel"/> control.
    /// </summary>
    /// <remarks>
    /// Based on the work from Liron Levi on Code Project, under public domain. 
    /// See http://www.codeproject.com/KB/cs/multipanelcontrol.aspx
    /// </remarks>
    [Designer(typeof(MultiPanelPageDesigner))]
    public class MultiPanelPage : ContainerControl
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MultiPanelPage()
        {
            base.Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Gets <see cref="DockStyle.Fill"/>. Sets is available but always set <see cref="DockStyle.Fill"/>.
        /// </summary>
        public override DockStyle Dock
        {
            get { return base.Dock; }
            set { base.Dock = DockStyle.Fill; }
        }

        /// <summary>
        /// Only here so that it shows up in the property panel.
        /// </summary>
        [Category("Design")]
        [Description("The text identifying the page.")]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        /// <summary>
        /// Overriden. Creates the underlying controls collection.
        /// </summary>
        /// <returns>A <see cref="MultiPanelPage.ControlCollection"/>.</returns>
        protected override ControlCollection CreateControlsInstance() => new PageControlCollection(this);


        #region ControlCollection

        /// <summary>
        /// A control collection when ensures only <see cref="MultiPanelPage"/> are added.
        /// </summary>
        private sealed class PageControlCollection : ControlCollection
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="owner">A <see cref="T:System.Windows.Forms.Control" /> representing the control that owns the control collection.</param>
            /// <exception cref="System.ArgumentException">Tried to create a MultiPanelPage.ControlCollection with a non-MultiPanelPage owner.;owner</exception>
            /// <exception cref="System.ArgumentNullException">owner</exception>
            public PageControlCollection(Control owner)
                : base(owner)
            {
                // Should not happen
                owner.ThrowIfNull(nameof(owner), "Tried to create a MultiPanelPage.ControlCollection with a null owner.");

                // Should not happen
                MultiPanelPage c = owner as MultiPanelPage;
                if (c == null)
                {
                    throw new ArgumentException(
                        @"Tried to create a MultiPanelPage.ControlCollection with a non-MultiPanelPage owner.", nameof(owner));
                }
            }

            /// <summary>
            /// Adds an item to the control. Ensures it is a <see cref="MultiPanelPage" />.
            /// </summary>
            /// <param name="value">The <see cref="T:System.Windows.Forms.Control" /> to add to the control collection.</param>
            /// <exception cref="System.ArgumentException">Tried to add a MultiPanelPage control to the MultiPanelPage.ControlCollection.;value</exception>
            /// <exception cref="System.ArgumentNullException">value</exception>
            public override void Add(Control value)
            {
                value.ThrowIfNull(nameof(value), "Tried to add a null value to the MultiPanelPage.ControlCollection.");

                MultiPanelPage p = value as MultiPanelPage;
                if (p != null)
                {
                    throw new ArgumentException("Tried to add a MultiPanelPage control to the MultiPanelPage.ControlCollection.",
                        "value");
                }

                base.Add(value);
            }
        }

        #endregion
    }
}