// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.DetailsWindow
{
    partial class WalletJournalChartWindow
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.BalanceTabPage = new System.Windows.Forms.TabPage();
            this.BalanceChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.AmountTabPage = new System.Windows.Forms.TabPage();
            this.AmountChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabControl.SuspendLayout();
            this.BalanceTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BalanceChart)).BeginInit();
            this.AmountTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.AmountChart)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.BalanceTabPage);
            this.tabControl.Controls.Add(this.AmountTabPage);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(624, 442);
            this.tabControl.TabIndex = 0;
            // 
            // BalanceTabPage
            // 
            this.BalanceTabPage.Controls.Add(this.BalanceChart);
            this.BalanceTabPage.Location = new System.Drawing.Point(4, 22);
            this.BalanceTabPage.Name = "BalanceTabPage";
            this.BalanceTabPage.Size = new System.Drawing.Size(616, 416);
            this.BalanceTabPage.TabIndex = 0;
            this.BalanceTabPage.Text = "Balance";
            this.BalanceTabPage.UseVisualStyleBackColor = true;
            // 
            // BalanceChart
            // 
            chartArea1.Name = "ChartArea1";
            this.BalanceChart.ChartAreas.Add(chartArea1);
            this.BalanceChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BalanceChart.Location = new System.Drawing.Point(0, 0);
            this.BalanceChart.Name = "BalanceChart";
            series1.ChartArea = "ChartArea1";
            series1.Name = "Series";
            this.BalanceChart.Series.Add(series1);
            this.BalanceChart.Size = new System.Drawing.Size(616, 416);
            this.BalanceChart.TabIndex = 0;
            this.BalanceChart.Text = "chart1";
            // 
            // AmountTabPage
            // 
            this.AmountTabPage.Controls.Add(this.AmountChart);
            this.AmountTabPage.Location = new System.Drawing.Point(4, 22);
            this.AmountTabPage.Name = "AmountTabPage";
            this.AmountTabPage.Size = new System.Drawing.Size(616, 416);
            this.AmountTabPage.TabIndex = 1;
            this.AmountTabPage.Text = "Amount";
            this.AmountTabPage.UseVisualStyleBackColor = true;
            // 
            // AmountChart
            // 
            chartArea2.Name = "ChartArea1";
            chartArea3.Name = "ChartArea2";
            this.AmountChart.ChartAreas.Add(chartArea2);
            this.AmountChart.ChartAreas.Add(chartArea3);
            this.AmountChart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AmountChart.Location = new System.Drawing.Point(0, 0);
            this.AmountChart.Name = "AmountChart";
            series2.ChartArea = "ChartArea1";
            series2.Name = "Series1";
            series3.ChartArea = "ChartArea2";
            series3.Name = "Series2";
            this.AmountChart.Series.Add(series2);
            this.AmountChart.Series.Add(series3);
            this.AmountChart.Size = new System.Drawing.Size(616, 416);
            this.AmountChart.TabIndex = 0;
            this.AmountChart.Text = "chart1";
            // 
            // WalletJournalChartWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 442);
            this.Controls.Add(this.tabControl);
            this.Name = "WalletJournalChartWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Wallet Journal Charts";
            this.tabControl.ResumeLayout(false);
            this.BalanceTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BalanceChart)).EndInit();
            this.AmountTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.AmountChart)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage BalanceTabPage;
        private System.Windows.Forms.TabPage AmountTabPage;
        private System.Windows.Forms.DataVisualization.Charting.Chart AmountChart;
        private System.Windows.Forms.DataVisualization.Charting.Chart BalanceChart;
    }
}