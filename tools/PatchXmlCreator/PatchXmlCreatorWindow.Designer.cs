namespace EveLens.PatchXmlCreator
{
    partial class PatchXmlCreatorWindow
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnCreate = new System.Windows.Forms.Button();
            this.lblEveLensReleaseDate = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.dtpRelease = new System.Windows.Forms.DateTimePicker();
            this.lblEveLensVersion = new System.Windows.Forms.Label();
            this.gbRelease = new System.Windows.Forms.GroupBox();
            this.btnInstallerClear = new System.Windows.Forms.Button();
            this.btnLoadReleaseInfo = new System.Windows.Forms.Button();
            this.lblMessage = new System.Windows.Forms.Label();
            this.rtbReleaseMessage = new System.Windows.Forms.RichTextBox();
            this.rtbReleaseUrl = new System.Windows.Forms.RichTextBox();
            this.lblInstallerUrl = new System.Windows.Forms.Label();
            this.rtbTopicUrl = new System.Windows.Forms.RichTextBox();
            this.lblForumUrl = new System.Windows.Forms.Label();
            this.lblMD5Sum = new System.Windows.Forms.Label();
            this.btPaste = new System.Windows.Forms.Button();
            this.gbDatafiles = new System.Windows.Forms.GroupBox();
            this.lblExpVersion = new System.Windows.Forms.Label();
            this.tbExpVersion = new System.Windows.Forms.TextBox();
            this.btnDatafilesClear = new System.Windows.Forms.Button();
            this.datafileControl = new EveLens.PatchXmlCreator.DatafileControl();
            this.btnLoadDatafileInfo = new System.Windows.Forms.Button();
            this.lblRevision = new System.Windows.Forms.Label();
            this.tbExpRevision = new System.Windows.Forms.TextBox();
            this.lblExpansion = new System.Windows.Forms.Label();
            this.rtbDatafileUrl = new System.Windows.Forms.RichTextBox();
            this.tbExpansion = new System.Windows.Forms.TextBox();
            this.lblUrl = new System.Windows.Forms.Label();
            this.pnlButtons = new System.Windows.Forms.Panel();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.gbRelease.SuspendLayout();
            this.gbDatafiles.SuspendLayout();
            this.pnlButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.AutoSize = true;
            this.btnCancel.Location = new System.Drawing.Point(783, 18);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnCreate
            // 
            this.btnCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreate.AutoSize = true;
            this.btnCreate.Enabled = false;
            this.btnCreate.Location = new System.Drawing.Point(702, 18);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(75, 23);
            this.btnCreate.TabIndex = 1;
            this.btnCreate.Text = "Create";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // lblEveLensReleaseDate
            // 
            this.lblEveLensReleaseDate.AutoSize = true;
            this.lblEveLensReleaseDate.Location = new System.Drawing.Point(5, 20);
            this.lblEveLensReleaseDate.Name = "lblEveLensReleaseDate";
            this.lblEveLensReleaseDate.Size = new System.Drawing.Size(78, 13);
            this.lblEveLensReleaseDate.TabIndex = 2;
            this.lblEveLensReleaseDate.Text = "Release Date :";
            this.lblEveLensReleaseDate.Click += new System.EventHandler(this.OnClick);
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(258, 20);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(48, 13);
            this.lblVersion.TabIndex = 3;
            this.lblVersion.Text = "Version :";
            this.lblVersion.Click += new System.EventHandler(this.OnClick);
            // 
            // dtpRelease
            // 
            this.dtpRelease.CustomFormat = " dd MMMM yyyy";
            this.dtpRelease.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpRelease.Location = new System.Drawing.Point(86, 16);
            this.dtpRelease.Name = "dtpRelease";
            this.dtpRelease.Size = new System.Drawing.Size(166, 20);
            this.dtpRelease.TabIndex = 4;
            this.dtpRelease.Value = new System.DateTime(2010, 1, 1, 0, 0, 0, 0);
            this.dtpRelease.MouseCaptureChanged += new System.EventHandler(this.OnClick);
            // 
            // lblEveLensVersion
            // 
            this.lblEveLensVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblEveLensVersion.BackColor = System.Drawing.SystemColors.ControlLight;
            this.lblEveLensVersion.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblEveLensVersion.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblEveLensVersion.Location = new System.Drawing.Point(313, 16);
            this.lblEveLensVersion.Name = "lblEveLensVersion";
            this.lblEveLensVersion.Size = new System.Drawing.Size(76, 21);
            this.lblEveLensVersion.TabIndex = 5;
            this.lblEveLensVersion.Text = "1.0.0.0";
            this.lblEveLensVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gbRelease
            // 
            this.gbRelease.Controls.Add(this.btnInstallerClear);
            this.gbRelease.Controls.Add(this.btnLoadReleaseInfo);
            this.gbRelease.Controls.Add(this.lblMessage);
            this.gbRelease.Controls.Add(this.rtbReleaseMessage);
            this.gbRelease.Controls.Add(this.rtbReleaseUrl);
            this.gbRelease.Controls.Add(this.lblInstallerUrl);
            this.gbRelease.Controls.Add(this.rtbTopicUrl);
            this.gbRelease.Controls.Add(this.lblForumUrl);
            this.gbRelease.Controls.Add(this.lblEveLensReleaseDate);
            this.gbRelease.Controls.Add(this.lblEveLensVersion);
            this.gbRelease.Controls.Add(this.lblVersion);
            this.gbRelease.Controls.Add(this.dtpRelease);
            this.gbRelease.Controls.Add(this.lblMD5Sum);
            this.gbRelease.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbRelease.Location = new System.Drawing.Point(10, 10);
            this.gbRelease.Name = "gbRelease";
            this.gbRelease.Size = new System.Drawing.Size(396, 211);
            this.gbRelease.TabIndex = 6;
            this.gbRelease.TabStop = false;
            this.gbRelease.Text = "Release Info";
            this.gbRelease.MouseCaptureChanged += new System.EventHandler(this.OnClick);
            // 
            // btnInstallerClear
            // 
            this.btnInstallerClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnInstallerClear.Location = new System.Drawing.Point(7, 182);
            this.btnInstallerClear.Name = "btnInstallerClear";
            this.btnInstallerClear.Size = new System.Drawing.Size(75, 23);
            this.btnInstallerClear.TabIndex = 2;
            this.btnInstallerClear.Text = "Clear All";
            this.btnInstallerClear.UseVisualStyleBackColor = true;
            this.btnInstallerClear.Click += new System.EventHandler(this.btnReleaseClear_Click);
            // 
            // btnLoadReleaseInfo
            // 
            this.btnLoadReleaseInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoadReleaseInfo.AutoSize = true;
            this.btnLoadReleaseInfo.Location = new System.Drawing.Point(232, 183);
            this.btnLoadReleaseInfo.Name = "btnLoadReleaseInfo";
            this.btnLoadReleaseInfo.Size = new System.Drawing.Size(158, 23);
            this.btnLoadReleaseInfo.TabIndex = 3;
            this.btnLoadReleaseInfo.Text = "Load info from existing patch";
            this.btnLoadReleaseInfo.UseVisualStyleBackColor = true;
            this.btnLoadReleaseInfo.Click += new System.EventHandler(this.btnLoadReleaseInfo_Click);
            // 
            // lblMessage
            // 
            this.lblMessage.AutoSize = true;
            this.lblMessage.Location = new System.Drawing.Point(5, 100);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(56, 13);
            this.lblMessage.TabIndex = 12;
            this.lblMessage.Text = "Message :";
            this.lblMessage.Click += new System.EventHandler(this.OnClick);
            // 
            // rtbReleaseMessage
            // 
            this.rtbReleaseMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbReleaseMessage.DetectUrls = false;
            this.rtbReleaseMessage.Location = new System.Drawing.Point(7, 116);
            this.rtbReleaseMessage.Name = "rtbReleaseMessage";
            this.rtbReleaseMessage.Size = new System.Drawing.Size(383, 47);
            this.rtbReleaseMessage.TabIndex = 11;
            this.rtbReleaseMessage.Text = "Type your message here.";
            this.rtbReleaseMessage.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.rtbReleaseMessage.Enter += new System.EventHandler(this.Control_Enter);
            this.rtbReleaseMessage.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // rtbReleaseUrl
            // 
            this.rtbReleaseUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbReleaseUrl.DetectUrls = false;
            this.rtbReleaseUrl.Location = new System.Drawing.Point(37, 63);
            this.rtbReleaseUrl.Multiline = false;
            this.rtbReleaseUrl.Name = "rtbReleaseUrl";
            this.rtbReleaseUrl.Size = new System.Drawing.Size(352, 21);
            this.rtbReleaseUrl.TabIndex = 10;
            this.rtbReleaseUrl.Text = "";
            this.rtbReleaseUrl.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.rtbReleaseUrl.Enter += new System.EventHandler(this.Control_Enter);
            this.rtbReleaseUrl.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // lblInstallerUrl
            // 
            this.lblInstallerUrl.AutoSize = true;
            this.lblInstallerUrl.Location = new System.Drawing.Point(5, 66);
            this.lblInstallerUrl.Name = "lblInstallerUrl";
            this.lblInstallerUrl.Size = new System.Drawing.Size(26, 13);
            this.lblInstallerUrl.TabIndex = 9;
            this.lblInstallerUrl.Text = "Url :";
            this.lblInstallerUrl.Click += new System.EventHandler(this.OnClick);
            // 
            // rtbTopicUrl
            // 
            this.rtbTopicUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbTopicUrl.DetectUrls = false;
            this.rtbTopicUrl.Location = new System.Drawing.Point(65, 40);
            this.rtbTopicUrl.Multiline = false;
            this.rtbTopicUrl.Name = "rtbTopicUrl";
            this.rtbTopicUrl.Size = new System.Drawing.Size(324, 21);
            this.rtbTopicUrl.TabIndex = 7;
            this.rtbTopicUrl.Text = "";
            this.rtbTopicUrl.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.rtbTopicUrl.Enter += new System.EventHandler(this.Control_Enter);
            this.rtbTopicUrl.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // lblForumUrl
            // 
            this.lblForumUrl.AutoSize = true;
            this.lblForumUrl.Location = new System.Drawing.Point(5, 44);
            this.lblForumUrl.Name = "lblForumUrl";
            this.lblForumUrl.Size = new System.Drawing.Size(56, 13);
            this.lblForumUrl.TabIndex = 6;
            this.lblForumUrl.Text = "Topic Url :";
            this.lblForumUrl.Click += new System.EventHandler(this.OnClick);
            // 
            // lblMD5Sum
            // 
            this.lblMD5Sum.AutoSize = true;
            this.lblMD5Sum.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblMD5Sum.Location = new System.Drawing.Point(339, 16);
            this.lblMD5Sum.Name = "lblMD5Sum";
            this.lblMD5Sum.Padding = new System.Windows.Forms.Padding(0, 70, 0, 0);
            this.lblMD5Sum.Size = new System.Drawing.Size(54, 83);
            this.lblMD5Sum.TabIndex = 13;
            this.lblMD5Sum.Text = "MD5 Sum";
            this.lblMD5Sum.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // btPaste
            // 
            this.btPaste.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btPaste.AutoSize = true;
            this.btPaste.Location = new System.Drawing.Point(12, 18);
            this.btPaste.Name = "btPaste";
            this.btPaste.Size = new System.Drawing.Size(117, 23);
            this.btPaste.TabIndex = 2;
            this.btPaste.Text = "Paste from Clipboard";
            this.btPaste.UseVisualStyleBackColor = true;
            this.btPaste.Click += new System.EventHandler(this.btPaste_Click);
            // 
            // gbDatafiles
            // 
            this.gbDatafiles.Controls.Add(this.lblExpVersion);
            this.gbDatafiles.Controls.Add(this.tbExpVersion);
            this.gbDatafiles.Controls.Add(this.btnDatafilesClear);
            this.gbDatafiles.Controls.Add(this.datafileControl);
            this.gbDatafiles.Controls.Add(this.btnLoadDatafileInfo);
            this.gbDatafiles.Controls.Add(this.lblRevision);
            this.gbDatafiles.Controls.Add(this.tbExpRevision);
            this.gbDatafiles.Controls.Add(this.lblExpansion);
            this.gbDatafiles.Controls.Add(this.rtbDatafileUrl);
            this.gbDatafiles.Controls.Add(this.tbExpansion);
            this.gbDatafiles.Controls.Add(this.lblUrl);
            this.gbDatafiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbDatafiles.Location = new System.Drawing.Point(5, 10);
            this.gbDatafiles.Name = "gbDatafiles";
            this.gbDatafiles.Size = new System.Drawing.Size(440, 211);
            this.gbDatafiles.TabIndex = 7;
            this.gbDatafiles.TabStop = false;
            this.gbDatafiles.Text = "Data Files Info";
            this.gbDatafiles.GotFocus += new System.EventHandler(this.OnClick);
            this.gbDatafiles.MouseCaptureChanged += new System.EventHandler(this.OnClick);
            // 
            // lblExpVersion
            // 
            this.lblExpVersion.AutoSize = true;
            this.lblExpVersion.Location = new System.Drawing.Point(180, 43);
            this.lblExpVersion.Name = "lblExpVersion";
            this.lblExpVersion.Size = new System.Drawing.Size(48, 13);
            this.lblExpVersion.TabIndex = 22;
            this.lblExpVersion.Text = "Version :";
            // 
            // tbExpVersion
            // 
            this.tbExpVersion.Location = new System.Drawing.Point(229, 40);
            this.tbExpVersion.Name = "tbExpVersion";
            this.tbExpVersion.Size = new System.Drawing.Size(49, 20);
            this.tbExpVersion.TabIndex = 19;
            this.tbExpVersion.Text = "Version";
            this.tbExpVersion.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbExpVersion.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.tbExpVersion.Enter += new System.EventHandler(this.Control_Enter);
            this.tbExpVersion.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // btnDatafilesClear
            // 
            this.btnDatafilesClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDatafilesClear.Location = new System.Drawing.Point(9, 182);
            this.btnDatafilesClear.Name = "btnDatafilesClear";
            this.btnDatafilesClear.Size = new System.Drawing.Size(75, 23);
            this.btnDatafilesClear.TabIndex = 1;
            this.btnDatafilesClear.Text = "Clear All";
            this.btnDatafilesClear.UseVisualStyleBackColor = true;
            this.btnDatafilesClear.Click += new System.EventHandler(this.btnDatafilesClear_Click);
            // 
            // datafileControl
            // 
            this.datafileControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.datafileControl.AutoSize = true;
            this.datafileControl.Location = new System.Drawing.Point(9, 65);
            this.datafileControl.Name = "datafileControl";
            this.datafileControl.Size = new System.Drawing.Size(427, 91);
            this.datafileControl.TabIndex = 21;
            // 
            // btnLoadDatafileInfo
            // 
            this.btnLoadDatafileInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLoadDatafileInfo.AutoSize = true;
            this.btnLoadDatafileInfo.Location = new System.Drawing.Point(278, 183);
            this.btnLoadDatafileInfo.Name = "btnLoadDatafileInfo";
            this.btnLoadDatafileInfo.Size = new System.Drawing.Size(158, 23);
            this.btnLoadDatafileInfo.TabIndex = 2;
            this.btnLoadDatafileInfo.Text = "Load info from existing patch";
            this.btnLoadDatafileInfo.UseVisualStyleBackColor = true;
            this.btnLoadDatafileInfo.Click += new System.EventHandler(this.btnLoadDatafileInfo_Click);
            // 
            // lblRevision
            // 
            this.lblRevision.AutoSize = true;
            this.lblRevision.Location = new System.Drawing.Point(292, 43);
            this.lblRevision.Name = "lblRevision";
            this.lblRevision.Size = new System.Drawing.Size(54, 13);
            this.lblRevision.TabIndex = 19;
            this.lblRevision.Text = "Revision :";
            this.lblRevision.Click += new System.EventHandler(this.OnClick);
            // 
            // tbExpRevision
            // 
            this.tbExpRevision.Location = new System.Drawing.Point(346, 40);
            this.tbExpRevision.Name = "tbExpRevision";
            this.tbExpRevision.Size = new System.Drawing.Size(78, 20);
            this.tbExpRevision.TabIndex = 20;
            this.tbExpRevision.Text = "Exp. Revision";
            this.tbExpRevision.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbExpRevision.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.tbExpRevision.Enter += new System.EventHandler(this.Control_Enter);
            this.tbExpRevision.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // lblExpansion
            // 
            this.lblExpansion.AutoSize = true;
            this.lblExpansion.Location = new System.Drawing.Point(12, 43);
            this.lblExpansion.Name = "lblExpansion";
            this.lblExpansion.Size = new System.Drawing.Size(62, 13);
            this.lblExpansion.TabIndex = 17;
            this.lblExpansion.Text = "Expansion :";
            this.lblExpansion.Click += new System.EventHandler(this.OnClick);
            // 
            // rtbDatafileUrl
            // 
            this.rtbDatafileUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbDatafileUrl.DetectUrls = false;
            this.rtbDatafileUrl.Location = new System.Drawing.Point(46, 16);
            this.rtbDatafileUrl.Multiline = false;
            this.rtbDatafileUrl.Name = "rtbDatafileUrl";
            this.rtbDatafileUrl.Size = new System.Drawing.Size(390, 21);
            this.rtbDatafileUrl.TabIndex = 15;
            this.rtbDatafileUrl.Text = "";
            this.rtbDatafileUrl.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.rtbDatafileUrl.Enter += new System.EventHandler(this.Control_Enter);
            this.rtbDatafileUrl.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // tbExpansion
            // 
            this.tbExpansion.Location = new System.Drawing.Point(75, 40);
            this.tbExpansion.Name = "tbExpansion";
            this.tbExpansion.Size = new System.Drawing.Size(91, 20);
            this.tbExpansion.TabIndex = 18;
            this.tbExpansion.Text = "Expansion Name";
            this.tbExpansion.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.tbExpansion.DoubleClick += new System.EventHandler(this.Control_DoubleClick);
            this.tbExpansion.Enter += new System.EventHandler(this.Control_Enter);
            this.tbExpansion.Leave += new System.EventHandler(this.Control_Leave);
            // 
            // lblUrl
            // 
            this.lblUrl.AutoSize = true;
            this.lblUrl.Location = new System.Drawing.Point(13, 19);
            this.lblUrl.Name = "lblUrl";
            this.lblUrl.Size = new System.Drawing.Size(26, 13);
            this.lblUrl.TabIndex = 14;
            this.lblUrl.Text = "Url :";
            this.lblUrl.Click += new System.EventHandler(this.OnClick);
            // 
            // pnlButtons
            // 
            this.pnlButtons.BackColor = System.Drawing.SystemColors.ControlLight;
            this.pnlButtons.Controls.Add(this.btPaste);
            this.pnlButtons.Controls.Add(this.btnCancel);
            this.pnlButtons.Controls.Add(this.btnCreate);
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlButtons.Location = new System.Drawing.Point(0, 231);
            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Size = new System.Drawing.Size(870, 59);
            this.pnlButtons.TabIndex = 9;
            this.pnlButtons.Click += new System.EventHandler(this.OnClick);
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.gbRelease);
            this.splitContainer.Panel1.Padding = new System.Windows.Forms.Padding(10, 10, 5, 10);
            this.splitContainer.Panel1.Click += new System.EventHandler(this.OnClick);
            this.splitContainer.Panel1MinSize = 411;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.gbDatafiles);
            this.splitContainer.Panel2.Padding = new System.Windows.Forms.Padding(5, 10, 10, 10);
            this.splitContainer.Panel2.Click += new System.EventHandler(this.OnClick);
            this.splitContainer.Panel2MinSize = 455;
            this.splitContainer.Size = new System.Drawing.Size(870, 231);
            this.splitContainer.SplitterDistance = 411;
            this.splitContainer.TabIndex = 10;
            this.splitContainer.Click += new System.EventHandler(this.OnClick);
            // 
            // PatchXmlCreatorWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(870, 290);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.pnlButtons);
            this.Name = "PatchXmlCreatorWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Patch Xml File Creator";
            this.gbRelease.ResumeLayout(false);
            this.gbRelease.PerformLayout();
            this.gbDatafiles.ResumeLayout(false);
            this.gbDatafiles.PerformLayout();
            this.pnlButtons.ResumeLayout(false);
            this.pnlButtons.PerformLayout();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlButtons;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btPaste;
        private System.Windows.Forms.Button btnLoadReleaseInfo;
        private System.Windows.Forms.Button btnLoadDatafileInfo;
        private System.Windows.Forms.Label lblEveLensReleaseDate;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblEveLensVersion;
        private System.Windows.Forms.Label lblForumUrl;
        private System.Windows.Forms.Label lblInstallerUrl;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Label lblUrl;
        private System.Windows.Forms.Label lblExpansion;
        private System.Windows.Forms.Label lblRevision;
        private System.Windows.Forms.DateTimePicker dtpRelease;
        private System.Windows.Forms.GroupBox gbRelease;
        private System.Windows.Forms.GroupBox gbDatafiles;
        private System.Windows.Forms.RichTextBox rtbDatafileUrl;
        private System.Windows.Forms.TextBox tbExpansion;
        private System.Windows.Forms.TextBox tbExpRevision;
        private System.Windows.Forms.RichTextBox rtbTopicUrl;
        private System.Windows.Forms.RichTextBox rtbReleaseUrl;
        private System.Windows.Forms.RichTextBox rtbReleaseMessage;
        private DatafileControl datafileControl;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Button btnInstallerClear;
        private System.Windows.Forms.Button btnDatafilesClear;
        private System.Windows.Forms.Label lblExpVersion;
        private System.Windows.Forms.TextBox tbExpVersion;
        private System.Windows.Forms.Label lblMD5Sum;
    }
}
