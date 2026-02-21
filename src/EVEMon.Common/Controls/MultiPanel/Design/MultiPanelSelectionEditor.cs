// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Controls.MultiPanel.Design
{
    /// <summary>
    /// The editor for selecting the selected page property of the <see cref="MultiPanel"/>.
    /// </summary>
    internal class MultiPanelSelectionEditor : ObjectSelectorEditor
    {
        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <exception cref="System.ArgumentNullException">selector or context or provider</exception>
        protected override void FillTreeWithData(Selector selector, ITypeDescriptorContext context, IServiceProvider provider)
        {
            selector.ThrowIfNull(nameof(selector));

            context.ThrowIfNull(nameof(context));

            provider.ThrowIfNull(nameof(provider));

            // Base method, clear the selector
            base.FillTreeWithData(selector, context, provider);

            // Scroll through the pages
            MultiPanel panel = (MultiPanel)context.Instance;
            foreach (MultiPanelPage page in panel.Controls)
            {
                SelectorNode node = new SelectorNode(page.Name, page);
                selector.Nodes.Add(node);

                if (page != panel.SelectedPage)
                    continue;

                selector.SelectedNode = node;
                return;
            }
        }
    }
}