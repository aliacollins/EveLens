// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;

namespace EVEMon.Common.Controls.MultiPanel.Design
{
    public static class MultiPanelDesignerHelper
    {
        /// <summary>
        /// Gets the collection of verbs displayed in the top right menu of the designer.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="panel"></param>
        /// <returns></returns>
        public static DesignerVerbCollection GetDesignerVerbs(IDesignerHost host, MultiPanel panel)
        {
            DesignerVerbCollection verbs = new DesignerVerbCollection
                                               {
                                                   new DesignerVerb("Add Page", (sender, args) => AddPage(host, panel)),
                                                   new DesignerVerb("Remove Page", (sender, args) => RemovePage(host, panel))
                                               };

            foreach (MultiPanelPage page in panel.Controls)
            {
                MultiPanelPage pageCopy = page;
                verbs.Add(new DesignerVerb("Select \"" + page.Text + "\"", (sender, args) => SelectPage(host, panel, pageCopy)));
            }

            return verbs;
        }

        /// <summary>
        /// Event handler for the "Add Page" verb.
        /// </summary>
        /// <param name="dh"></param>
        /// <param name="panel"></param>
        private static void AddPage(IDesignerHost dh, MultiPanel panel)
        {
            DesignerTransaction dt = dh.CreateTransaction("Added new page");

            // Gets a free name
            int i = 1;
            while (panel.Controls.Cast<Control>().Any(x => x.Name == "Page" + i))
            {
                i++;
            }
            string name = "Page" + i;

            // Creates the page
            MultiPanelPage newPage = dh.CreateComponent(typeof(MultiPanelPage), name) as MultiPanelPage;
            if (newPage != null)
            {
                newPage.Text = name;
                panel.Controls.Add(newPage);

                // Update selection
                panel.SelectedPage = newPage;
            }

            dt.Commit();
        }

        /// <summary>
        /// Event handler for the "Remove Tab" verb.
        /// </summary>
        /// <param name="dh"></param>
        /// <param name="panel"></param>
        private static void RemovePage(IDesignerHost dh, MultiPanel panel)
        {
            MultiPanelPage page = panel.SelectedPage;
            if (page == null)
                return;

            DesignerTransaction dt = dh.CreateTransaction("Removed page");

            panel.Controls.Remove(page);
            dh.DestroyComponent(page);

            panel.SelectedPage = panel.Controls.Count > 0 ? (MultiPanelPage)panel.Controls[0] : null;

            dt.Commit();
        }

        /// <summary>
        /// Event handler for the "Select X page" handler.
        /// </summary>
        /// <param name="dh"></param>
        /// <param name="panel"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        private static void SelectPage(IDesignerHost dh, MultiPanel panel, MultiPanelPage page)
        {
            DesignerTransaction dt = dh.CreateTransaction("Selected page");

            panel.SelectedPage = page;
            dt.Commit();
        }
    }
}