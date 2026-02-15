using System;
using EVEMon.Common.Controls;
using EVEMon.Common.Extensions;
using EVEMon.Common.Factories;

namespace EVEMon.SkillPlanner
{
    public partial class AttributesOptimizerOptionsWindow : EVEMonForm
    {
        private readonly PlanEditorControl m_planEditorControl = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributesOptimizerOptionsWindow"/> class.
        /// Default constructor for designer.
        /// </summary>
        private AttributesOptimizerOptionsWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributesOptimizerOptionsWindow"/> class.
        /// Constructor used in WindowsFactory.
        /// </summary>
        /// <param name="planEditorControl">The plan editor control.</param>
        /// <exception cref="System.ArgumentNullException">planEditorControl</exception>
        public AttributesOptimizerOptionsWindow(PlanEditorControl planEditorControl)
            : this()
        {
            planEditorControl.ThrowIfNull(nameof(planEditorControl));

            buttonWholePlan.Font = FontFactory.GetFont("Microsoft Sans Serif", 10F);
            buttonCharacter.Font = FontFactory.GetFont("Microsoft Sans Serif", 10F);
            buttonRemappingPoints.Font = FontFactory.GetFont("Microsoft Sans Serif", 10F);

            m_planEditorControl = planEditorControl;
        }

        /// <summary>
        /// Handles the Click event of the buttonRemappingPoints control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonRemappingPoints_Click(object? sender, EventArgs e)
        {
            WindowsFactory.ShowByTag<AttributesOptimizerWindow, PlanEditorControl>(m_planEditorControl.ParentForm!,
                m_planEditorControl, AttributeOptimizationStrategy.RemappingPoints);

            Close();
        }

        /// <summary>
        /// Handles the Click event of the buttonWholePlan control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonWholePlan_Click(object? sender, EventArgs e)
        {
            WindowsFactory.ShowByTag<AttributesOptimizerWindow, PlanEditorControl>(m_planEditorControl.ParentForm!,
                m_planEditorControl, AttributeOptimizationStrategy.OneYearPlan);

            Close();
        }

        /// <summary>
        /// Handles the Click event of the buttonCharacter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonCharacter_Click(object? sender, EventArgs e)
        {
            WindowsFactory.ShowByTag<AttributesOptimizerWindow, PlanEditorControl>(m_planEditorControl.ParentForm!,
                m_planEditorControl, AttributeOptimizationStrategy.Character);

            Close();
        }
    }
}