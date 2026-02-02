namespace EVEMon.SkillPlanner
{
    partial class BoosterInjectionWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblBoosterBonus = new System.Windows.Forms.Label();
            this.cbBoosterBonus = new System.Windows.Forms.ComboBox();
            this.lblDuration = new System.Windows.Forms.Label();
            this.nudDuration = new System.Windows.Forms.NumericUpDown();
            this.lblHours = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).BeginInit();
            this.SuspendLayout();
            //
            // lblBoosterBonus
            //
            this.lblBoosterBonus.AutoSize = true;
            this.lblBoosterBonus.Location = new System.Drawing.Point(15, 50);
            this.lblBoosterBonus.Name = "lblBoosterBonus";
            this.lblBoosterBonus.Size = new System.Drawing.Size(79, 13);
            this.lblBoosterBonus.TabIndex = 0;
            this.lblBoosterBonus.Text = "Booster Bonus:";
            //
            // cbBoosterBonus
            //
            this.cbBoosterBonus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBoosterBonus.FormattingEnabled = true;
            this.cbBoosterBonus.Location = new System.Drawing.Point(100, 47);
            this.cbBoosterBonus.Name = "cbBoosterBonus";
            this.cbBoosterBonus.Size = new System.Drawing.Size(170, 21);
            this.cbBoosterBonus.TabIndex = 1;
            this.cbBoosterBonus.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.cbBoosterBonus_Format);
            //
            // lblDuration
            //
            this.lblDuration.AutoSize = true;
            this.lblDuration.Location = new System.Drawing.Point(15, 83);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(50, 13);
            this.lblDuration.TabIndex = 2;
            this.lblDuration.Text = "Duration:";
            //
            // nudDuration
            //
            this.nudDuration.Location = new System.Drawing.Point(100, 80);
            this.nudDuration.Maximum = new decimal(new int[] {
            720,
            0,
            0,
            0});
            this.nudDuration.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudDuration.Name = "nudDuration";
            this.nudDuration.Size = new System.Drawing.Size(80, 21);
            this.nudDuration.TabIndex = 3;
            this.nudDuration.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            //
            // lblHours
            //
            this.lblHours.AutoSize = true;
            this.lblHours.Location = new System.Drawing.Point(186, 83);
            this.lblHours.Name = "lblHours";
            this.lblHours.Size = new System.Drawing.Size(33, 13);
            this.lblHours.TabIndex = 4;
            this.lblHours.Text = "hours";
            //
            // btnOk
            //
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(114, 120);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(195, 120);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // lblInfo
            //
            this.lblInfo.AutoSize = true;
            this.lblInfo.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblInfo.Location = new System.Drawing.Point(15, 15);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(255, 13);
            this.lblInfo.TabIndex = 7;
            this.lblInfo.Text = "Plan to inject a cerebral accelerator at this skill.";
            //
            // BoosterInjectionWindow
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(284, 155);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lblHours);
            this.Controls.Add(this.nudDuration);
            this.Controls.Add(this.lblDuration);
            this.Controls.Add(this.cbBoosterBonus);
            this.Controls.Add(this.lblBoosterBonus);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BoosterInjectionWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Inject Booster";
            this.Shown += new System.EventHandler(this.BoosterInjectionWindow_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblBoosterBonus;
        private System.Windows.Forms.ComboBox cbBoosterBonus;
        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.NumericUpDown nudDuration;
        private System.Windows.Forms.Label lblHours;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblInfo;
    }
}
