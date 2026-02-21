// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

// *****************************************************************************
// 
//  Copyright 2004, Coder's Lab
//  All rights reserved. The software and associated documentation 
//  supplied hereunder are the proprietary information of Coder's Lab
//  and are supplied subject to licence terms.
//  
//  If you modify this code, add your code comments and sent the modified code
//  to coder's lab.
//
//  You can use this control freely in your projects, but let me know if you
//  are using it so I can add you to a list of references. 
//
//  Email: ludwig.stuyck@coders-lab.be
//  Home page: http://www.coders-lab.be
//
//  History
//		18/07/2004	
//			- Control creation	
//		24/07/2004	
//			- Implemented rubberband selection; also combination keys work: 
//			  ctrl, shift, ctrl+shift	
//		25/08/2004	
//			- Rubberband selection temporary removed due to scrolling problems. 
//			- Renamed TreeViewSelectionMode property to SelectionMode.
//			- Renamed SelectionModes enumeration to TreeViewSelectionMode.
//			- Added MultiSelectSameParent selection mode.
//			- Added keyboard functionality.
//			- Enhanced selection drawing.
//			- Added SelectionBackColor property.	
//		02/09/2004	
//			- When shift/ctrl was pressed, treeview scrolled to last selected 
//			  node. Fixed.
//			- Moved TreeViewSelectionMode outside the TreeView class.
//			- BeforeSelect was fired multiple times, AfterSelect was never 
//			  fired. Fixed.
//			- Collapsing/Expanding node changed selection. This does not happen 
//			  anymore, except if a node that has selected descendants is 
//			  collapsed; then all descendants are unselected and the collapsed 
//			  node becomes selected.
//			- If in the BeforeSelect event, e.Cancel is set to true, then node 
//			  will not be selected
//			- SHIFT selection sometimes didn�t behave correctly. Fixed.
//		04/09/2004	
//			- SelectedNodes is no longer an array of tree nodes, but a 
//			  SelectedNodesCollection
//			- In the AfterSelect event, the SelectedNodes contained two tree 
//			  nodes; the old one and the new one. Fixed.
//		05/09/2004	
//			- Added Home, End, PgUp and PgDwn keys functionality	
//		08/10/2004
//			- SelectedNodeCollection renamed to NodeCollection
//			- Fixes by GKM
//
//		18/8/2005
//			- Added events BeforeDeselect and AfterDeselect
//		09/5/2007
//			- Added an InvokeRequired check to Flashnode()
//		16/5/2007
//			- Gave the document a consistant format
//			- Created a new event 'SelectionsChanged'
// 
// *****************************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Controls
{


    #region TreeViewSelectionMode enumeration

    /// <summary>
    /// Selection mode for the treeview.	
    /// </summary>
    /// <remarks>
    /// The Selection mode determines how treeview nodes can be selected.
    /// </remarks>
    public enum TreeViewSelectionMode
    {
        /// <summary>
        /// Only one node can be selected at a time.
        /// </summary>
        SingleSelect,

        /// <summary>
        /// Multiple nodes can be selected at the same time without restriction.
        /// </summary>
        MultiSelect,

        /// <summary>
        /// Multiple nodes that belong to the same root branch can be selected at the same time.
        /// </summary>
        MultiSelectSameRootBranch,

        /// <summary>
        /// Multiple nodes that belong to the same level can be selected at the same time.
        /// </summary>
        MultiSelectSameLevel,

        /// <summary>
        /// Multiple nodes that belong to the same level and same root branch can be selected at the same time.
        /// </summary>
        MultiSelectSameLevelAndRootBranch,

        /// <summary>
        /// Only nodes that belong to the same direct parent can be selected at the same time.
        /// </summary>
        MultiSelectSameParent
    }

    #endregion


    #region Delegates

    /// <summary>
    /// Delegate used for tree node events.
    /// </summary>
    public delegate void TreeNodeEventHandler(TreeNode tn);

    #endregion


    /// <summary>
    /// The TreeView control is a regular treeview with multi-selection capability.
    /// </summary>
    [ToolboxItem(true)]
    public class TreeView : System.Windows.Forms.TreeView
    {
        public event TreeViewEventHandler? AfterDeselect;
        public event TreeViewEventHandler? BeforeDeselect;
        public event EventHandler? SelectionsChanged;

        protected void OnAfterDeselect(TreeNode tn)
        {
            AfterDeselect?.Invoke(this, new TreeViewEventArgs(tn));
        }

        protected void OnBeforeDeselect(TreeNode tn)
        {
            BeforeDeselect?.Invoke(this, new TreeViewEventArgs(tn));
        }

        protected void OnSelectionsChanged()
        {
            if (!m_blnSelectionChanged)
                return;

            SelectionsChanged?.ThreadSafeInvoke(this, new EventArgs());
        }


        #region Private variables

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private Container? m_components;

        /// <summary>
        /// Used to make sure that SelectedNode can only be used from within this class.
        /// </summary>
        private bool m_blnInternalCall;

        /// <summary>
        /// List that contains all selected nodes.
        /// </summary>
        private readonly List<object> m_listSelectedNodes = new List<object>();

        /// <summary>
        /// Track whether the total SelectedNodes changed across multiple operations
        /// for SelectionsChanged event
        /// </summary>
        private bool m_blnSelectionChanged;

        /// <summary>
        /// Hashtable to preserve Node's original colors (colors can be set on the TreeView, or individual nodes)
        /// (GKM)
        /// </summary>
        private readonly Hashtable m_htblSelectedNodesOrigColors = new Hashtable();

        /// <summary>
        /// Keeps track of node that has to be pu in edit mode.
        /// </summary>
        private TreeNode? m_tnNodeToStartEditOn;

        /// <summary>
        /// Remembers whether mouse click on a node was single or double click.
        /// </summary>
        private bool m_blnWasDoubleClick;

        /// <summary>
        /// Keeps track of most recent selected node.
        /// </summary>
        private TreeNode? m_tnMostRecentSelectedNode;

        /// <summary>
        /// Keeps track of the selection mirror point; this is the last selected node without SHIFT key pressed.
        /// It is used as the mirror node during SHIFT selection.
        /// </summary>
        private TreeNode? m_tnSelectionMirrorPoint;

        /// <summary>
        /// Keeps track of the number of mouse clicks.
        /// </summary>
        private int m_intMouseClicks;

        /// <summary>
        /// Selection mode.
        /// </summary>
        private TreeViewSelectionMode m_selectionMode = TreeViewSelectionMode.SingleSelect;

        /// <summary>
        /// Backcolor for selected nodes.
        /// </summary>
        private Color m_selectionBackColor = SystemColors.Highlight;

        /// <summary>
        /// Keeps track whether a node click has been handled by the mouse down event. This is almost always the
        /// case, except when a selected node has been clicked again. Then, it will not be handled in the mouse
        /// down event because we might want to drag the node and if that's the case, node should not go in edit 
        /// mode.
        /// </summary>
        private bool m_blnNodeProcessedOnMouseDown;

        /// <summary>
        /// Holds node that needs to be flashed.
        /// </summary>
        private TreeNode? m_tnToFlash;

        /// <summary>
        /// Keeps track of the first selected node when selection has begun with the keyboard.
        /// </summary>
        private TreeNode? m_tnKeysStartNode;

        #endregion


        #region SelectedNode, SelectionMode, SelectionBackColor, SelectedNodes + events

        /// <summary>
        /// This property is for internal use only. Use SelectedNodes instead.
        /// </summary>
        public new TreeNode? SelectedNode
        {
            get
            {
                // Instead of not working, return the most recent selected node
                //throw new NotSupportedException("Use SelectedNodes instead of SelectedNode.");
                return !m_blnInternalCall ? m_tnMostRecentSelectedNode : base.SelectedNode;
            }
            set
            {
                if (!m_blnInternalCall)
                    base.SelectedNode = value;
            }
        }

        /// <summary>
        /// Gets/sets selection mode.
        /// </summary>
        public TreeViewSelectionMode SelectionMode
        {
            get { return m_selectionMode; }
            set { m_selectionMode = value; }
        }

        /// <summary>
        /// Gets/sets backcolor for selected nodes.
        /// </summary>
        public Color SelectionBackColor
        {
            get { return m_selectionBackColor; }
            set { m_selectionBackColor = value; }
        }

        /// <summary>
        /// Gets selected nodes.
        /// </summary>
        public NodesCollection SelectedNodes
        {
            get
            {
                // Create a SelectedNodesCollection to return, and add event handlers to catch actions on it
                NodesCollection selectedNodesCollection = new NodesCollection();
                foreach (TreeNode tn in m_listSelectedNodes)
                {
                    selectedNodesCollection.Add(tn);
                }

                selectedNodesCollection.TreeNodeAdded += SelectedNodes_TreeNodeAdded;
                selectedNodesCollection.TreeNodeInserted += SelectedNodes_TreeNodeInserted;
                selectedNodesCollection.TreeNodeRemoved += SelectedNodes_TreeNodeRemoved;
                selectedNodesCollection.SelectedNodesCleared += SelectedNodes_SelectedNodesCleared;

                return selectedNodesCollection;
            }
        }

        /// <summary>
        /// Gets the last selected node.
        /// </summary>
        public TreeNode? LastSelectedNode => m_tnMostRecentSelectedNode;

        /// <summary>
        /// Occurs when a tree node is added to the SelectedNodes collection.
        /// </summary>
        /// <param name="tn">Tree node that was added.</param>
        private void SelectedNodes_TreeNodeAdded(TreeNode tn)
        {
            m_blnSelectionChanged = false;

            SelectNode(tn, true, TreeViewAction.Unknown);

            OnSelectionsChanged();
        }

        /// <summary>
        /// Occurs when a tree node is inserted to the SelectedNodes collection.
        /// </summary>
        /// <param name="tn">tree node that was inserted.</param>
        private void SelectedNodes_TreeNodeInserted(TreeNode tn)
        {
            m_blnSelectionChanged = false;

            SelectNode(tn, true, TreeViewAction.Unknown);

            OnSelectionsChanged();
        }

        /// <summary>
        /// Occurs when a tree node is removed from the SelectedNodes collection.
        /// </summary>
        /// <param name="tn">Tree node that was removed.</param>
        private void SelectedNodes_TreeNodeRemoved(TreeNode tn)
        {
            m_blnSelectionChanged = false;

            SelectNode(tn, false, TreeViewAction.Unknown);

            OnSelectionsChanged();
        }

        /// <summary>
        /// Occurs when the SelectedNodes collection was cleared.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedNodes_SelectedNodesCleared(object? sender, EventArgs e)
        {
            m_blnSelectionChanged = false;

            UnselectAllNodes(TreeViewAction.Unknown);

            OnSelectionsChanged();
        }

        #endregion


        #region Node selection methods

        /// <summary>
        /// Unselects all selected nodes.
        /// </summary>
        internal void UnselectAllNodes()
        {
            UnselectAllNodesExceptNode(null, TreeViewAction.Unknown);
        }

        /// <summary>
        /// Unselects all selected nodes.
        /// </summary>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void UnselectAllNodes(TreeViewAction tva)
        {
            UnselectAllNodesExceptNode(null, tva);
        }

        /// <summary>
        /// Unselects all selected nodes that don't belong to the specified level.
        /// </summary>
        /// <param name="level">Node level.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void UnselectAllNodesNotBelongingToLevel(int level, TreeViewAction tva)
        {
            // First, build list of nodes that need to be unselected
            ArrayList arrNodesToDeselect = new ArrayList();
            foreach (TreeNode selectedTreeNode in m_listSelectedNodes.Cast<TreeNode>().Where(
                selectedTreeNode => GetNodeLevel(selectedTreeNode) != level))
            {
                arrNodesToDeselect.Add(selectedTreeNode);
            }

            // Do the actual unselect
            foreach (TreeNode tnToDeselect in arrNodesToDeselect)
            {
                SelectNode(tnToDeselect, false, tva);
            }
        }

        /// <summary>
        /// Unselects all selected nodes that don't belong directly to the specified parent.
        /// </summary>
        /// <param name="parent">Parent node.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void UnselectAllNodesNotBelongingDirectlyToParent(TreeNode parent, TreeViewAction tva)
        {
            // First, build list of nodes that need to be unselected
            ArrayList arrNodesToDeselect = new ArrayList();
            foreach (TreeNode selectedTreeNode in m_listSelectedNodes.Cast<TreeNode>().Where(
                selectedTreeNode => selectedTreeNode.Parent != parent))
            {
                arrNodesToDeselect.Add(selectedTreeNode);
            }

            // Do the actual unselect
            foreach (TreeNode tnToDeselect in arrNodesToDeselect)
            {
                SelectNode(tnToDeselect, false, tva);
            }
        }

        /// <summary>
        /// Unselects all selected nodes that don't belong directly or indirectly to the specified parent.
        /// </summary>
        /// <param name="parent">Parent node.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void UnselectAllNodesNotBelongingToParent(TreeNode parent, TreeViewAction tva)
        {
            // First, build list of nodes that need to be unselected
            ArrayList arrNodesToDeselect = new ArrayList();
            foreach (TreeNode selectedTreeNode in m_listSelectedNodes.Cast<TreeNode>().Where(
                selectedTreeNode => !IsChildOf(selectedTreeNode, parent)))
            {
                arrNodesToDeselect.Add(selectedTreeNode);
            }

            // Do the actual unselect
            foreach (TreeNode tnToDeselect in arrNodesToDeselect)
            {
                SelectNode(tnToDeselect, false, tva);
            }
        }

        /// <summary>
        /// Unselects all selected nodes, except for the specified node which should not be touched.
        /// </summary>
        /// <param name="nodeKeepSelected">Node not to touch.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        internal void UnselectAllNodesExceptNode(TreeNode? nodeKeepSelected, TreeViewAction tva)
        {
            // First, build list of nodes that need to be unselected
            ArrayList arrNodesToDeselect = new ArrayList();
            foreach (TreeNode selectedTreeNode in m_listSelectedNodes)
            {
                if (nodeKeepSelected == null)
                    arrNodesToDeselect.Add(selectedTreeNode);
                else if (selectedTreeNode != nodeKeepSelected)
                    arrNodesToDeselect.Add(selectedTreeNode);
            }

            // Do the actual unselect
            foreach (TreeNode tnToDeselect in arrNodesToDeselect)
            {
                SelectNode(tnToDeselect, false, tva);
            }
        }

        /// <summary>
        /// occurs when a node is about to be selected.
        /// </summary>
        /// <param name="e">TreeViewCancelEventArgs.</param>
        protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
        {
            // We don't want the base TreeView to handle the selection, because it can only handle single selection. 
            // Instead, we'll handle the selection ourselves by keeping track of the selected nodes and drawing the 
            // selection ourselves.
            e.Cancel = true;
        }

        /// <summary>
        /// Determines whether the specified node is selected or not.
        /// </summary>
        /// <param name="tn">Node to check.</param>
        /// <returns>True if specified node is selected, false if not.</returns>
        private bool IsNodeSelected(TreeNode tn) => tn != null && m_listSelectedNodes.Contains(tn);

        /// <summary>
        /// Preserves the nodes color.
        /// </summary>
        /// <param name="tn">Node to check.</param>
        private void PreserveNodeColors(TreeNode tn)
        {
            if (tn == null)
                return;

            if (!m_htblSelectedNodesOrigColors.ContainsKey(tn.GetHashCode()))
                m_htblSelectedNodesOrigColors.Add(tn.GetHashCode(), new[] { tn.BackColor, tn.ForeColor });
        }

        /// <summary>
        /// (Un)selects the specified node.
        /// </summary>
        /// <param name="tn">Node to (un)select.</param>
        /// <param name="select">True to select node, false to unselect node.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        /// <returns>True if node was selected, false if not.</returns>
        internal void SelectNode(TreeNode tn, bool select, TreeViewAction tva)
        {
            if (tn == null)
                return;

            if (select)
            {
                // Only try to select node if it was not already selected																		
                if (!IsNodeSelected(tn))
                {
                    // Check if node selection is cancelled
                    TreeViewCancelEventArgs tvcea = new TreeViewCancelEventArgs(tn, false, tva);
                    base.OnBeforeSelect(tvcea);

                    // This node selection was cancelled!						
                    if (tvcea.Cancel)
                        return;

                    PreserveNodeColors(tn);

                    tn.BackColor = SelectionBackColor; // GKM moved from above
                    tn.ForeColor = BackColor; // GKM moved from above									

                    m_listSelectedNodes.Add(tn);
                    m_blnSelectionChanged = true;

                    base.OnAfterSelect(new TreeViewEventArgs(tn, tva));
                }

                m_tnMostRecentSelectedNode = tn;
            }
            else
            {
                // Only unselect node if it was selected
                if (!IsNodeSelected(tn))
                    return;

                OnBeforeDeselect(tn);

                Color[]? originalColors = m_htblSelectedNodesOrigColors[tn.GetHashCode()] as Color[];
                if (originalColors != null)
                {
                    m_listSelectedNodes.Remove(tn);
                    m_blnSelectionChanged = true;
                    m_htblSelectedNodesOrigColors.Remove(tn.GetHashCode());

                    // GKM - Restore original node colors
                    tn.BackColor = originalColors[0]; // GKM - was BackColor;
                    tn.ForeColor = originalColors[1]; // GKM - was ForeColor;
                }

                OnAfterDeselect(tn);
            }
        }

        /// <summary>
        /// Selects nodes within the specified range.
        /// </summary>
        /// <param name="startNode">Start node.</param>
        /// <param name="endNode">End Node.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void SelectNodesInsideRange(TreeNode startNode, TreeNode endNode, TreeViewAction tva)
        {
            if (startNode == null || endNode == null)
                return;

            // Calculate start node and end node
            TreeNode firstNode;
            TreeNode lastNode;
            if (startNode.Bounds.Y < endNode.Bounds.Y)
            {
                firstNode = startNode;
                lastNode = endNode;
            }
            else
            {
                firstNode = endNode;
                lastNode = startNode;
            }

            // Select each node in range
            SelectNode(firstNode, true, tva);
            TreeNode tnTemp = firstNode;
            while (tnTemp != lastNode && tnTemp != null)
            {
                tnTemp = tnTemp.NextVisibleNode;
                if (tnTemp != null)
                    SelectNode(tnTemp, true, tva);
            }
            SelectNode(lastNode, true, tva);
        }

        /// <summary>
        /// Unselects nodes outside the specified range.
        /// </summary>
        /// <param name="startNode">Start node.</param>
        /// <param name="endNode">End node.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void UnselectNodesOutsideRange(TreeNode startNode, TreeNode endNode, TreeViewAction tva)
        {
            if (startNode == null || endNode == null)
                return;

            // Calculate start node and end node
            TreeNode firstNode;
            TreeNode lastNode;
            if (startNode.Bounds.Y < endNode.Bounds.Y)
            {
                firstNode = startNode;
                lastNode = endNode;
            }
            else
            {
                firstNode = endNode;
                lastNode = startNode;
            }

            // Unselect each node outside range
            TreeNode tnTemp = firstNode;
            while (tnTemp != null)
            {
                tnTemp = tnTemp.PrevVisibleNode;
                if (tnTemp != null)
                    SelectNode(tnTemp, false, tva);
            }

            tnTemp = lastNode;
            while (tnTemp != null)
            {
                tnTemp = tnTemp.NextVisibleNode;
                if (tnTemp != null)
                    SelectNode(tnTemp, false, tva);
            }
        }

        /// <summary>
        /// Recursively unselect node.
        /// </summary>
        /// <param name="tn">Node to recursively unselect.</param>
        /// <param name="tva">Specifies the action that caused the selection change.</param>
        private void UnselectNodesRecursively(TreeNode tn, TreeViewAction tva)
        {
            SelectNode(tn, false, tva);
            foreach (TreeNode child in tn.Nodes)
            {
                UnselectNodesRecursively(child, tva);
            }
        }

        #endregion


        #region Helper methods

        /// <summary>
        /// Determines whether a mouse click was inside the node bounds or outside the node bounds..
        /// </summary>
        /// <param name="tn">TreeNode to check.</param>
        /// <param name="e">MouseEventArgs.</param>
        /// <returns>True is mouse was clicked inside the node bounds, false if it was clicked ouside the node bounds.</returns>
        private static bool IsClickOnNode(TreeNode tn, MouseEventArgs e)
        {
            if (tn == null)
                return false;

            // GKM
            // Determine the rightmost position we'll process clicks (so that the click has to be on the node's bounds, 
            // like the .NET treeview
            int rightMostX = tn.Bounds.X + tn.Bounds.Width;
            return e.X < rightMostX; // GKM
        }

        /// <summary>
        /// Gets level of specified node.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <returns>Level of node.</returns>
        private static int GetNodeLevel(TreeNode node)
        {
            int level = 0;
            while ((node = node.Parent) != null)
            {
                level++;
            }
            return level;
        }

        /// <summary>
        /// Determines whether the specified node is a child (indirect or direct) of the specified parent.
        /// </summary>
        /// <param name="child">Node to check.</param>
        /// <param name="parent">Parent node.</param>
        /// <returns>True if specified node is a direct or indirect child of parent node, false if not.</returns>
        private static bool IsChildOf(TreeNode child, TreeNode parent)
        {
            bool blnChild = false;

            TreeNode tnTemp = child;
            while (tnTemp != null)
            {
                if (tnTemp == parent)
                {
                    blnChild = true;
                    break;
                }
                tnTemp = tnTemp.Parent;
            }

            return blnChild;
        }

        /// <summary>
        /// Gets root parent of specified node.
        /// </summary>
        /// <param name="child">Node.</param>
        /// <returns>Root parent of specified node.</returns>
        private static TreeNode GetRootParent(TreeNode child)
        {
            TreeNode tnParent = child;

            while (tnParent.Parent != null)
            {
                tnParent = tnParent.Parent;
            }

            return tnParent;
        }

        /// <summary>
        /// Gets number of visible nodes.
        /// </summary>
        /// <returns>Number of visible nodes.</returns>
        private int GetNumberOfVisibleNodes()
        {
            int intCounter = 0;

            TreeNode tnTemp = Nodes[0];

            while (tnTemp != null)
            {
                if (tnTemp.IsVisible)
                    intCounter++;

                tnTemp = tnTemp.NextVisibleNode;
            }

            return intCounter;
        }

        /// <summary>
        /// Gets last visible node.
        /// </summary>
        /// <returns>Last visible node.</returns>
        private TreeNode GetLastVisibleNode()
        {
            TreeNode tnTemp = Nodes[0];

            while (tnTemp.NextVisibleNode != null)
            {
                tnTemp = tnTemp.NextVisibleNode;
            }

            return tnTemp;
        }

        /// <summary>
        /// Gets next tree node(s), starting from the specified node and direction.
        /// </summary>
        /// <param name="start">Node to start from.</param>
        /// <param name="down">True to go down, false to go up.</param>
        /// <param name="intNumber">Number of nodes to go down or up.</param>
        /// <returns>Next node.</returns>
        private static TreeNode GetNextTreeNode(TreeNode start, bool down, int intNumber)
        {
            int intCounter = 0;
            TreeNode tnTemp = start;
            while (intCounter < intNumber)
            {
                if (down)
                {
                    if (tnTemp.NextVisibleNode != null)
                        tnTemp = tnTemp.NextVisibleNode;
                    else
                        break;
                }
                else
                {
                    if (tnTemp.PrevVisibleNode != null)
                        tnTemp = tnTemp.PrevVisibleNode;
                    else
                        break;
                }

                intCounter++;
            }

            return tnTemp;
        }

        /// <summary>
        /// makes focus rectangle visible or hides it.
        /// </summary>
        /// <param name="tn">Node to make focus rectangle (in)visible for.</param>
        /// <param name="visible">True to make focus rectangle visible, false to hide it.</param>
        private void SetFocusToNode(TreeNode tn, bool visible)
        {
            Graphics g = CreateGraphics();
            Rectangle rect = new Rectangle(tn.Bounds.X, tn.Bounds.Y, tn.Bounds.Width, tn.Bounds.Height);
            if (visible)
            {
                Invalidate(rect, false);
                Update();
                if (tn.BackColor == SelectionBackColor)
                    return;

                using (SolidBrush brush = new SolidBrush(SelectionBackColor))
                using (Pen pen = new Pen(brush, 1))
                {
                    g.DrawRectangle(pen, rect);
                }
            }
            else
            {
                if (tn.BackColor != SelectionBackColor && m_tnMostRecentSelectedNode != null)
                {
                    using (SolidBrush brush = new SolidBrush(BackColor))
                    using (Pen pen = new Pen(brush, 1))
                    {
                        g.DrawRectangle(pen, m_tnMostRecentSelectedNode.Bounds.X, m_tnMostRecentSelectedNode.Bounds.Y,
                            m_tnMostRecentSelectedNode.Bounds.Width, m_tnMostRecentSelectedNode.Bounds.Height);
                    }
                }

                Invalidate(rect, false);
                Update();
            }
        }

        #endregion


        #region Dispose

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion


        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            m_components = new Container();
        }

        #endregion


        #region OnMouseUp, OnMouseDown

        /// <summary>
        /// Occurs when mouse button is up after a click.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
#if DEBUG
            try
            {
#endif
            if (!m_blnNodeProcessedOnMouseDown)
            {
                TreeNode tn = GetNodeAt(e.X, e.Y);

                // Mouse click has not been handled by the mouse down event, so do it here. This is the case when
                // a selected node was clicked again; in that case we handle that click here because in case the
                // user is dragging the node, we should not put it in edit mode.					

                if (IsClickOnNode(tn, e))
                {
                    ProcessNodeRange(m_tnMostRecentSelectedNode, tn, e, ModifierKeys, TreeViewAction.ByMouse,
                        true);
                }
            }

            m_blnNodeProcessedOnMouseDown = false;

            base.OnMouseUp(e);
#if DEBUG
            }
            catch (Exception ex)
            {
                // GKM - Untrapped exceptions were killing me for debugging purposes.
                // It probably shouldn't be here permanently, but it was causing real trouble for me.
                MessageBox.Show(this, ex.ToString());
                throw;
            }
#endif
        }

        /// <summary>
        /// Checks if we have clicked on the plus/minus icon.
        /// </summary>
        /// <param name="tn">Node to check.</param>
        /// <param name="e"></param>
        /// <returns>True if we click on the plus/minus icon</returns>
        private static bool IsPlusMinusClicked(TreeNode? tn, MouseEventArgs e) => tn != null && e.X < tn.Bounds.X;

        /// <summary>
        /// Occurs when mouse is down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            m_tnKeysStartNode = null;

            // Store number of mouse clicks in OnMouseDown event, because here we also get e.Clicks = 2 when an item was doubleclicked
            // in OnMouseUp we seem to get always e.Clicks = 1, also when item is doubleclicked
            m_intMouseClicks = e.Clicks;

            TreeNode tn = GetNodeAt(e.X, e.Y);

            if (tn == null)
                return;

            // Preserve colors here, because if you do it later then it will already have selected colors 
            // Don't know why...!
            PreserveNodeColors(tn);

            // If +/- was clicked, we should not process the node.
            if (!IsPlusMinusClicked(tn, e))
            {
                // If mouse down on a node that is already selected, then we should process this node in the mouse up event, because we
                // might want to drag it and it should not be put in edit mode.
                // Also, only process node if click was in node's bounds.
                if (IsClickOnNode(tn, e) && !IsNodeSelected(tn))
                {
                    // Flash node. In case the node selection is cancelled by the user, this gives the effect that it
                    // was selected and unselected again.
                    m_tnToFlash = tn;
                    Thread t = new Thread(FlashNode);
                    t.Start();

                    m_blnNodeProcessedOnMouseDown = true;
                    ProcessNodeRange(m_tnMostRecentSelectedNode, tn, e, ModifierKeys, TreeViewAction.ByMouse, true);
                }
            }

            base.OnMouseDown(e);
        }

        #endregion


        #region FlashNode, StartEdit

        /// <summary>
        /// Flashes node.
        /// </summary>
        private void FlashNode()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(FlashNode));
                return;
            }

            TreeNode? tn = m_tnToFlash;
            if (tn == null)
                return;

            // Only flash node is it's not yet selected
            if (!IsNodeSelected(tn))
            {
                tn.BackColor = SelectionBackColor;
                tn.ForeColor = BackColor;
                Invalidate();
                Refresh();
                Application.DoEvents();
                Thread.Sleep(200);
            }

            // If node is not selected yet, restore default colors to end flashing
            if (IsNodeSelected(tn))
                return;

            tn.BackColor = BackColor;
            tn.ForeColor = ForeColor;
        }

        /// <summary>
        /// Starts edit on a node.
        /// </summary>
        private void StartEdit()
        {
            Thread.Sleep(200);
            if (!m_blnWasDoubleClick && m_tnNodeToStartEditOn != null)
            {
                m_blnInternalCall = true;
                SelectedNode = m_tnNodeToStartEditOn;
                m_blnInternalCall = false;
                m_tnNodeToStartEditOn.BeginEdit();
            }
            else
                m_blnWasDoubleClick = false;
        }

        #endregion


        #region ProcessNodeRange

        /// <summary>
        /// Processes a node range.
        /// </summary>
        /// <param name="startNode">Start node of range.</param>
        /// <param name="endNode">End node of range.</param>
        /// <param name="e">MouseEventArgs.</param>
        /// <param name="keys">Keys.</param>
        /// <param name="tva">TreeViewAction.</param>
        /// <param name="allowStartEdit">True if node can go to edit mode, false if not.</param>
        public void ProcessNodeRange(TreeNode? startNode, TreeNode endNode, MouseEventArgs e, Keys keys, TreeViewAction tva,
            bool allowStartEdit)
        {
            m_blnSelectionChanged = false; // prepare for OnSelectionsChanged

            switch (e.Button)
            {
                case MouseButtons.Left:
                {
                    m_blnWasDoubleClick = m_intMouseClicks == 2;

                    if (((keys & Keys.Control) == 0) && ((keys & Keys.Shift) == 0))
                    {
                        // CTRL and SHIFT not held down							
                        m_tnSelectionMirrorPoint = endNode;
                        int intNumberOfSelectedNodes = SelectedNodes.Count;

                        // If it was a double click, select node and suspend further processing					
                        if (m_blnWasDoubleClick)
                        {
                            base.OnMouseDown(e);
                            return;
                        }

                        if (!IsPlusMinusClicked(endNode, e))
                        {
                            bool blnNodeWasSelected = IsNodeSelected(endNode);

                            UnselectAllNodesExceptNode(endNode, tva);
                            SelectNode(endNode, true, tva);


                            if (blnNodeWasSelected && LabelEdit && allowStartEdit && !m_blnWasDoubleClick &&
                                (intNumberOfSelectedNodes <= 1))
                            {
                                // Node should be put in edit mode					
                                m_tnNodeToStartEditOn = endNode;
                                Thread t = new Thread(StartEdit);
                                t.Start();
                            }
                        }
                    }
                    else if (((keys & Keys.Control) != 0) && ((keys & Keys.Shift) == 0))
                        HandleControlHeldDown(endNode, tva);
                    else if (((keys & Keys.Control) == 0) && ((keys & Keys.Shift) != 0))
                        HandleShiftPressed(startNode, endNode, tva);
                    else if (((keys & Keys.Control) != 0) && ((keys & Keys.Shift) != 0))
                        HandleShiftAndControlPressed(startNode, endNode, tva);
                }
                    break;
                case MouseButtons.Right:
                    if (!IsNodeSelected(endNode))
                    {
                        UnselectAllNodes(tva);
                        SelectNode(endNode, true, tva);
                    }
                    break;
            }
            OnSelectionsChanged();
        }

        /// <summary>
        /// Handles the control key held down.
        /// </summary>
        /// <param name="endNode">The end node.</param>
        /// <param name="tva">The tva.</param>
        private void HandleControlHeldDown(TreeNode endNode, TreeViewAction tva)
        {
            // CTRL held down
            m_tnSelectionMirrorPoint = null;

            if (!IsNodeSelected(endNode))
            {
                switch (m_selectionMode)
                {
                    case TreeViewSelectionMode.SingleSelect:
                        UnselectAllNodesExceptNode(endNode, tva);
                        break;

                    case TreeViewSelectionMode.MultiSelectSameRootBranch:
                        TreeNode tnAbsoluteParent2 = GetRootParent(endNode);
                        UnselectAllNodesNotBelongingToParent(tnAbsoluteParent2, tva);
                        break;

                    case TreeViewSelectionMode.MultiSelectSameLevel:
                        UnselectAllNodesNotBelongingToLevel(GetNodeLevel(endNode), tva);
                        break;

                    case TreeViewSelectionMode.MultiSelectSameLevelAndRootBranch:
                        TreeNode tnAbsoluteParent = GetRootParent(endNode);
                        UnselectAllNodesNotBelongingToParent(tnAbsoluteParent, tva);
                        UnselectAllNodesNotBelongingToLevel(GetNodeLevel(endNode), tva);
                        break;

                    case TreeViewSelectionMode.MultiSelectSameParent:
                        TreeNode tnParent = endNode.Parent;
                        UnselectAllNodesNotBelongingDirectlyToParent(tnParent, tva);
                        break;
                }

                SelectNode(endNode, true, tva);
            }
            else
                SelectNode(endNode, false, tva);
        }

        /// <summary>
        /// Handles the shift key pressed.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <param name="endNode">The end node.</param>
        /// <param name="tva">The tva.</param>
        private void HandleShiftPressed(TreeNode? startNode, TreeNode endNode, TreeViewAction tva)
        {
            if (startNode == null)
                return;

            TreeNode tnTemp;
            int intNodeLevelStart;

            // SHIFT pressed
            if (m_tnSelectionMirrorPoint == null)
                m_tnSelectionMirrorPoint = startNode;

            switch (m_selectionMode)
            {
                case TreeViewSelectionMode.SingleSelect:
                    UnselectAllNodesExceptNode(endNode, tva);
                    SelectNode(endNode, true, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameRootBranch:
                    TreeNode tnAbsoluteParentStartNode = GetRootParent(startNode);
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        TreeNode tnAbsoluteParent = GetRootParent(tnTemp);
                        if (tnAbsoluteParent == tnAbsoluteParentStartNode)
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingToParent(tnAbsoluteParentStartNode, tva);
                    UnselectNodesOutsideRange(m_tnSelectionMirrorPoint, endNode, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameLevel:
                    intNodeLevelStart = GetNodeLevel(startNode);
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        int intNodeLevel = GetNodeLevel(tnTemp);
                        if (intNodeLevel == intNodeLevelStart)
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingToLevel(intNodeLevelStart, tva);
                    UnselectNodesOutsideRange(m_tnSelectionMirrorPoint, endNode, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameLevelAndRootBranch:
                    TreeNode tnAbsoluteParentStart = GetRootParent(startNode);
                    intNodeLevelStart = GetNodeLevel(startNode);
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        int intNodeLevel = GetNodeLevel(tnTemp);
                        TreeNode tnAbsoluteParent = GetRootParent(tnTemp);
                        if ((intNodeLevel == intNodeLevelStart) && (tnAbsoluteParent == tnAbsoluteParentStart))
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingToParent(tnAbsoluteParentStart, tva);
                    UnselectAllNodesNotBelongingToLevel(intNodeLevelStart, tva);
                    UnselectNodesOutsideRange(m_tnSelectionMirrorPoint, endNode, tva);
                    break;

                case TreeViewSelectionMode.MultiSelect:
                    SelectNodesInsideRange(m_tnSelectionMirrorPoint, endNode, tva);
                    UnselectNodesOutsideRange(m_tnSelectionMirrorPoint, endNode, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameParent:
                    TreeNode tnParentStartNode = startNode.Parent;
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        TreeNode tnParent = tnTemp.Parent;
                        if (tnParent == tnParentStartNode)
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingDirectlyToParent(tnParentStartNode, tva);
                    UnselectNodesOutsideRange(m_tnSelectionMirrorPoint, endNode, tva);
                    break;
            }
        }

        /// <summary>
        /// Handles the shift and control keys pressed.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <param name="endNode">The end node.</param>
        /// <param name="tva">The tva.</param>
        private void HandleShiftAndControlPressed(TreeNode? startNode, TreeNode endNode, TreeViewAction tva)
        {
            if (startNode == null)
                return;

            TreeNode tnTemp;
            int intNodeLevelStart;

            // SHIFT AND CTRL pressed
            switch (m_selectionMode)
            {
                case TreeViewSelectionMode.SingleSelect:
                    UnselectAllNodesExceptNode(endNode, tva);
                    SelectNode(endNode, true, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameRootBranch:
                    TreeNode tnAbsoluteParentStartNode = GetRootParent(startNode);
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        TreeNode tnAbsoluteParent = GetRootParent(tnTemp);
                        if (tnAbsoluteParent == tnAbsoluteParentStartNode)
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingToParent(tnAbsoluteParentStartNode, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameLevel:
                    intNodeLevelStart = GetNodeLevel(startNode);
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        int intNodeLevel = GetNodeLevel(tnTemp);
                        if (intNodeLevel == intNodeLevelStart)
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingToLevel(intNodeLevelStart, tva);
                    break;

                case TreeViewSelectionMode.MultiSelectSameLevelAndRootBranch:
                    TreeNode tnAbsoluteParentStart = GetRootParent(startNode);
                    intNodeLevelStart = GetNodeLevel(startNode);
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        int intNodeLevel = GetNodeLevel(tnTemp);
                        TreeNode tnAbsoluteParent = GetRootParent(tnTemp);
                        if ((intNodeLevel == intNodeLevelStart) && (tnAbsoluteParent == tnAbsoluteParentStart))
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingToParent(tnAbsoluteParentStart, tva);
                    UnselectAllNodesNotBelongingToLevel(intNodeLevelStart, tva);
                    break;

                case TreeViewSelectionMode.MultiSelect:
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp != null)
                            SelectNode(tnTemp, true, tva);
                    }
                    break;

                case TreeViewSelectionMode.MultiSelectSameParent:
                    TreeNode tnParentStartNode = startNode.Parent;
                    tnTemp = startNode;
                    // Check each visible node from startNode to endNode and select it if needed
                    while ((tnTemp != null) && (tnTemp != endNode))
                    {
                        tnTemp = startNode.Bounds.Y > endNode.Bounds.Y
                            ? tnTemp.PrevVisibleNode
                            : tnTemp.NextVisibleNode;
                        if (tnTemp == null)
                            continue;

                        TreeNode tnParent = tnTemp.Parent;
                        if (tnParent == tnParentStartNode)
                            SelectNode(tnTemp, true, tva);
                    }
                    UnselectAllNodesNotBelongingDirectlyToParent(tnParentStartNode, tva);
                    break;
            }
        }

        #endregion


        #region OnBeforeLabelEdit

        /// <summary>
        /// Occurs before node goes into edit mode.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnBeforeLabelEdit(NodeLabelEditEventArgs e)
        {
            m_blnSelectionChanged = false; // prepare for OnSelectionsChanged

            if (e.Node == null)
                return;

            // Make sure that it's the only selected node
            SelectNode(e.Node, true, TreeViewAction.ByMouse);
            UnselectAllNodesExceptNode(e.Node, TreeViewAction.ByMouse);

            OnSelectionsChanged();

            base.OnBeforeLabelEdit(e);
        }

        #endregion


        #region OnKeyDown

        /// <summary>
        /// occurs when a key is down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            Keys kMod = Keys.None;
            switch (e.Modifiers)
            {
                case Keys.Shift:
                case Keys.Control:
                case Keys.Control | Keys.Shift:
                    kMod = Keys.Shift;
                    if (m_tnKeysStartNode == null)
                        m_tnKeysStartNode = m_tnMostRecentSelectedNode;
                    break;
                default:
                    m_tnKeysStartNode = null;
                    break;
            }

            int intNumber = 0;

            TreeNode? tnNewlySelectedNodeWithKeys = null;
            if (m_tnMostRecentSelectedNode != null)
            {
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        tnNewlySelectedNodeWithKeys = m_tnMostRecentSelectedNode.NextVisibleNode;
                        break;

                    case Keys.Up:
                        tnNewlySelectedNodeWithKeys = m_tnMostRecentSelectedNode.PrevVisibleNode;
                        break;

                    case Keys.Left:
                        if (m_tnMostRecentSelectedNode.IsExpanded)
                            m_tnMostRecentSelectedNode.Collapse();
                        else
                            tnNewlySelectedNodeWithKeys = m_tnMostRecentSelectedNode.Parent;
                        break;

                    case Keys.Right:
                        if (!m_tnMostRecentSelectedNode.IsExpanded)
                            m_tnMostRecentSelectedNode.Expand();
                        else
                            tnNewlySelectedNodeWithKeys = m_tnMostRecentSelectedNode.Nodes[0];
                        break;

                    case Keys.Home:
                        tnNewlySelectedNodeWithKeys = Nodes[0];
                        break;

                    case Keys.End:
                        tnNewlySelectedNodeWithKeys = GetLastVisibleNode();
                        break;

                    case Keys.PageDown:

                        intNumber = GetNumberOfVisibleNodes();
                        tnNewlySelectedNodeWithKeys = GetNextTreeNode(m_tnMostRecentSelectedNode, true, intNumber);
                        break;

                    case Keys.PageUp:

                        intNumber = GetNumberOfVisibleNodes();
                        tnNewlySelectedNodeWithKeys = GetNextTreeNode(m_tnMostRecentSelectedNode, false, intNumber);
                        break;

                    default:
                        base.OnKeyDown(e); // GKM
                        return;
                }
            }
            if (tnNewlySelectedNodeWithKeys != null && m_tnMostRecentSelectedNode != null)
            {
                SetFocusToNode(m_tnMostRecentSelectedNode, false);
                ProcessNodeRange(m_tnKeysStartNode, tnNewlySelectedNodeWithKeys,
                    new MouseEventArgs(MouseButtons.Left, 1, Cursor.Position.X, Cursor.Position.Y, 0), kMod,
                    TreeViewAction.ByKeyboard, false);
                m_tnMostRecentSelectedNode = tnNewlySelectedNodeWithKeys;
                SetFocusToNode(m_tnMostRecentSelectedNode, true);
            }

            // Ensure visibility
            if (m_tnMostRecentSelectedNode != null)
            {
                TreeNode? tnToMakeVisible = null;
                switch (e.KeyCode)
                {
                    case Keys.Down:
                    case Keys.Right:
                        tnToMakeVisible = GetNextTreeNode(m_tnMostRecentSelectedNode, true, 5);
                        break;

                    case Keys.Up:
                    case Keys.Left:
                        tnToMakeVisible = GetNextTreeNode(m_tnMostRecentSelectedNode, false, 5);
                        break;

                    case Keys.Home:
                    case Keys.End:
                        tnToMakeVisible = m_tnMostRecentSelectedNode;
                        break;

                    case Keys.PageDown:
                        tnToMakeVisible = GetNextTreeNode(m_tnMostRecentSelectedNode, true, intNumber - 2);
                        break;

                    case Keys.PageUp:
                        tnToMakeVisible = GetNextTreeNode(m_tnMostRecentSelectedNode, false, intNumber - 2);
                        break;
                }

                tnToMakeVisible?.EnsureVisible();
            }

            base.OnKeyDown(e);
        }

        #endregion


        #region OnAfterCollapse

        /// <summary>
        /// Occurs after a node is collapsed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            m_blnSelectionChanged = false;

            if (e.Node == null)
            {
                base.OnAfterCollapse(e);
                return;
            }

            // All child nodes should be deselected
            bool blnChildSelected = false;
            foreach (TreeNode tn in e.Node.Nodes)
            {
                if (IsNodeSelected(tn))
                    blnChildSelected = true;
                UnselectNodesRecursively(tn, TreeViewAction.Collapse);
            }

            if (blnChildSelected)
                SelectNode(e.Node, true, TreeViewAction.Collapse);

            OnSelectionsChanged();

            base.OnAfterCollapse(e);
        }

        #endregion


        #region OnItemDrag

        /// <summary>
        /// Occurs when an item is being dragged.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnItemDrag(ItemDragEventArgs e)
        {
            e = new ItemDragEventArgs(MouseButtons.Left, SelectedNodes);
            base.OnItemDrag(e);
        }

        #endregion
    }


    #region SelectedNodesCollection

    /// <summary>
    /// Collection of selected nodes.
    /// </summary>
    public class NodesCollection : CollectionBase, IList<TreeNode>
    {
        #region Events

        /// <summary>
        /// Event fired when a tree node has been added to the collection.
        /// </summary>
        internal event TreeNodeEventHandler? TreeNodeAdded;

        /// <summary>
        /// Event fired when a tree node has been removed to the collection.
        /// </summary>
        internal event TreeNodeEventHandler? TreeNodeRemoved;

        /// <summary>
        /// Event fired when a tree node has been inserted to the collection.
        /// </summary>
        internal event TreeNodeEventHandler? TreeNodeInserted;

        /// <summary>
        /// Event fired the collection has been cleared.
        /// </summary>
        internal event EventHandler? SelectedNodesCleared;

        #endregion


        #region CollectionBase members

        /// <summary>
        /// Gets tree node at specified index.
        /// </summary>
        public TreeNode this[int index] => (TreeNode)List[index]!;

        /// <summary>
        /// Adds a tree node to the collection.
        /// </summary>
        /// <param name="treeNode">Tree node to add.</param>
        /// <returns>The position into which the new element was inserted.</returns>
        public void Add(TreeNode treeNode)
        {
            TreeNodeAdded?.Invoke(treeNode);

            List.Add(treeNode);
        }

        /// <summary>
        /// Inserts a tree node at specified index.
        /// </summary>
        /// <param name="index">The position into which the new element has to be inserted.</param>
        /// <param name="treeNode">Tree node to insert.</param>
        public void InsertAt(int index, TreeNode treeNode)
        {
            TreeNodeInserted?.Invoke(treeNode);

            List.Insert(index, treeNode);
        }

        /// <summary>
        /// Removed a tree node from the collection.
        /// </summary>
        /// <param name="treeNode">Tree node to remove.</param>
        public void Remove(TreeNode treeNode)
        {
            TreeNodeRemoved?.Invoke(treeNode);

            List.Remove(treeNode);
        }

        /// <summary>
        /// Determines whether treenode belongs to the collection.
        /// </summary>
        /// <param name="treeNode">Tree node to check.</param>
        /// <returns>True if tree node belongs to the collection, false if not.</returns>
        public bool Contains(TreeNode treeNode) => List.Contains(treeNode);

        /// <summary>
        /// Gets index of tree node in the collection.
        /// </summary>
        /// <param name="treeNode">Tree node to get index of.</param>
        /// <returns>Index of tree node in the collection.</returns>
        public int IndexOf(TreeNode treeNode) => List.IndexOf(treeNode);

        /// <summary>
        /// Copies the tree node array to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(TreeNode[] array, int arrayIndex)
        {
            List.CopyTo(array, arrayIndex);
        }

        #endregion


        #region IList<T> members

        void ICollection<TreeNode>.Add(TreeNode treeNode)
        {
        }

        bool ICollection<TreeNode>.Contains(TreeNode treeNode) => true;

        void ICollection<TreeNode>.CopyTo(TreeNode[] array, int arrayIndex)
        {
        }

        bool ICollection<TreeNode>.IsReadOnly => false;

        bool ICollection<TreeNode>.Remove(TreeNode treeNode)
        {
            if (!List.Contains(treeNode))
                return false;

            List.Remove(treeNode);
            return true;
        }

        int IList<TreeNode>.IndexOf(TreeNode treeNode) => List.IndexOf(treeNode);

        void IList<TreeNode>.Insert(int index, TreeNode treeNode)
        {
        }

        TreeNode IList<TreeNode>.this[int index]
        {
            get { return (TreeNode)List[index]!; }
            set { }
        }

        IEnumerator<TreeNode> IEnumerable<TreeNode>.GetEnumerator() => new NodesCollectionEnumerator(InnerList.GetEnumerator());

        #endregion


        #region OnClear

        /// <summary>
        /// Occurs when collection is being cleared.
        /// </summary>
        protected override void OnClear()
        {
            SelectedNodesCleared?.ThreadSafeInvoke(this, EventArgs.Empty);

            base.OnClear();
        }

        #endregion


        #region IEnumerator Implementation

        private class NodesCollectionEnumerator : IEnumerator<TreeNode>
        {
            private readonly IEnumerator m_enumerator;

            public NodesCollectionEnumerator(IEnumerator enumerator)
            {
                m_enumerator = enumerator;
            }

            public TreeNode Current => (TreeNode)m_enumerator.Current;

            object IEnumerator.Current => m_enumerator.Current;

            public bool MoveNext() => m_enumerator.MoveNext();

            public void Reset()
            {
                m_enumerator.Reset();
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }

    #endregion
}