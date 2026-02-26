// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for BrowserTreeNode with IBrush properties.
    /// Contains zero business logic per Law 16.
    /// </summary>
    internal sealed class BrowserTreeNodeDisplay : INotifyPropertyChanged
    {
        private static readonly IBrush CanUseBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush CategoryBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush CanUseStatusBrush = new SolidColorBrush(Color.Parse("#FF81C784"));

        public BrowserTreeNode Node { get; }

        public string Name => Node.Name;
        public int Depth => Node.Depth;
        public bool IsLeaf => Node.IsLeaf;
        public bool IsCategory => !Node.IsLeaf;
        public Thickness IndentMargin => new Thickness(Node.Depth * 20, 0, 0, 0);

        public bool IsExpanded
        {
            get => Node.IsExpanded;
            set
            {
                if (Node.IsExpanded != value)
                {
                    Node.IsExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
                }
            }
        }

        public string Chevron => Node.IsLeaf ? "" : (Node.IsExpanded ? "\u25BE" : "\u25B8");

        public IBrush NameBrush => Node.IsLeaf
            ? (Node.CanUse ? CanUseBrush : DefaultBrush)
            : CategoryBrush;

        public string CountText => Node.IsLeaf ? "" : $"({Node.VisibleChildren.Count})";
        public string StatusText => Node.IsLeaf ? (Node.CanUse ? "Can Use" : "") : "";

        public IBrush StatusBrush => Node.CanUse ? CanUseStatusBrush : DefaultBrush;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BrowserTreeNodeDisplay(BrowserTreeNode node) => Node = node;
    }
}
