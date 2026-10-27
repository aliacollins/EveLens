// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia.Controls;
using Avalonia.Input;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    /// <summary>
    /// Asks for a new group's name. Closes with the trimmed name, or null when cancelled;
    /// the caller creates the group only for a name. The rules come from the caller
    /// (<c>CharacterGroupMutator.GroupNameProblem</c>).
    /// </summary>
    public partial class GroupNameDialog : Window
    {
        private readonly Func<string, string?> problemFor;

        public GroupNameDialog() : this(_ => null) { }

        public GroupNameDialog(Func<string, string?> problemFor)
        {
            this.problemFor = problemFor;
            InitializeComponent();
            NameBox.TextChanged += (_, _) => Validate();
            CreateButton.Click += (_, _) => Accept();
            CancelButton.Click += (_, _) => Close(null);
            NameBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter) { e.Handled = true; Accept(); }
            };
            KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape) { e.Handled = true; Close(null); }
            };
            Opened += (_, _) => NameBox.Focus();
        }

        private string EnteredName => NameBox.Text?.Trim() ?? string.Empty;

        /// <summary>Create is only available for a usable name; an empty box needs no message.</summary>
        private string? Validate()
        {
            string? problem = problemFor(EnteredName);
            CreateButton.IsEnabled = problem == null;
            ProblemText.IsVisible = problem != null && EnteredName.Length > 0;
            ProblemText.Text = problem == null ? string.Empty : Loc.Get(problem);
            return problem;
        }

        private void Accept()
        {
            if (Validate() == null) Close(EnteredName);
        }
    }
}
