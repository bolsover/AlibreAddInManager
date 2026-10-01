namespace AlibreAddOnManager.Ui
{
    partial class ManagerForm
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
            this.components = new System.ComponentModel.Container();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabInstalled = new System.Windows.Forms.TabPage();
            this.listInstalled = new System.Windows.Forms.ListView();
            this.colInstalledName = new System.Windows.Forms.ColumnHeader();
            this.colInstalledVersion = new System.Windows.Forms.ColumnHeader();
            this.colInstalledStatus = new System.Windows.Forms.ColumnHeader();
            this.colInstalledType = new System.Windows.Forms.ColumnHeader();
            this.colInstalledFolder = new System.Windows.Forms.ColumnHeader();
            this.txtInstalledDetails = new System.Windows.Forms.TextBox();
            this.panelInstalledButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnInstallFromFile = new System.Windows.Forms.Button();
            this.btnDisable = new System.Windows.Forms.Button();
            this.btnEnable = new System.Windows.Forms.Button();
            this.btnUninstall = new System.Windows.Forms.Button();
            this.btnRemoveRegistration = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.tabAvailable = new System.Windows.Forms.TabPage();
            this.splitAvailable = new System.Windows.Forms.SplitContainer();
            this.listAvailable = new System.Windows.Forms.ListView();
            this.colAvailableName = new System.Windows.Forms.ColumnHeader();
            this.colAvailablePublisher = new System.Windows.Forms.ColumnHeader();
            this.colAvailableLatest = new System.Windows.Forms.ColumnHeader();
            this.colAvailableInstalled = new System.Windows.Forms.ColumnHeader();
            this.colAvailableSummary = new System.Windows.Forms.ColumnHeader();
            this.txtAvailableDetails = new System.Windows.Forms.TextBox();
            this.panelCatalog = new System.Windows.Forms.TableLayoutPanel();
            this.lblCatalogUrl = new System.Windows.Forms.Label();
            this.txtCatalogUrl = new System.Windows.Forms.TextBox();
            this.btnLoadCatalog = new System.Windows.Forms.Button();
            this.panelAvailableButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnInstallSelected = new System.Windows.Forms.Button();
            this.groupPending = new System.Windows.Forms.GroupBox();
            this.listPending = new System.Windows.Forms.ListBox();
            this.panelPendingButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnRemovePending = new System.Windows.Forms.Button();
            this.btnClearPending = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.openPackageDialog = new System.Windows.Forms.OpenFileDialog();
            this.tabControl.SuspendLayout();
            this.tabInstalled.SuspendLayout();
            this.panelInstalledButtons.SuspendLayout();
            this.tabAvailable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitAvailable)).BeginInit();
            this.splitAvailable.Panel1.SuspendLayout();
            this.splitAvailable.Panel2.SuspendLayout();
            this.splitAvailable.SuspendLayout();
            this.panelCatalog.SuspendLayout();
            this.panelAvailableButtons.SuspendLayout();
            this.groupPending.SuspendLayout();
            this.panelPendingButtons.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl
            //
            this.tabControl.Controls.Add(this.tabInstalled);
            this.tabControl.Controls.Add(this.tabAvailable);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(944, 441);
            this.tabControl.TabIndex = 0;
            //
            // tabInstalled
            //
            this.tabInstalled.Controls.Add(this.listInstalled);
            this.tabInstalled.Controls.Add(this.txtInstalledDetails);
            this.tabInstalled.Controls.Add(this.panelInstalledButtons);
            this.tabInstalled.Location = new System.Drawing.Point(4, 22);
            this.tabInstalled.Name = "tabInstalled";
            this.tabInstalled.Padding = new System.Windows.Forms.Padding(3);
            this.tabInstalled.Size = new System.Drawing.Size(936, 415);
            this.tabInstalled.TabIndex = 0;
            this.tabInstalled.Text = "Installed";
            this.tabInstalled.UseVisualStyleBackColor = true;
            //
            // listInstalled
            //
            this.listInstalled.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colInstalledName, this.colInstalledVersion, this.colInstalledStatus, this.colInstalledType, this.colInstalledFolder });
            this.listInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listInstalled.FullRowSelect = true;
            this.listInstalled.HideSelection = false;
            this.listInstalled.Location = new System.Drawing.Point(3, 3);
            this.listInstalled.MultiSelect = false;
            this.listInstalled.Name = "listInstalled";
            this.listInstalled.Size = new System.Drawing.Size(930, 305);
            this.listInstalled.TabIndex = 0;
            this.listInstalled.UseCompatibleStateImageBehavior = false;
            this.listInstalled.View = System.Windows.Forms.View.Details;
            this.listInstalled.SelectedIndexChanged += new System.EventHandler(this.listInstalled_SelectedIndexChanged);
            //
            // column headers (Installed)
            //
            this.colInstalledName.Text = "Name";
            this.colInstalledName.Width = 220;
            this.colInstalledVersion.Text = "Version";
            this.colInstalledVersion.Width = 90;
            this.colInstalledStatus.Text = "Status";
            this.colInstalledStatus.Width = 120;
            this.colInstalledType.Text = "DLL";
            this.colInstalledType.Width = 70;
            this.colInstalledFolder.Text = "Folder";
            this.colInstalledFolder.Width = 420;
            //
            // txtInstalledDetails
            //
            this.txtInstalledDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtInstalledDetails.Location = new System.Drawing.Point(3, 308);
            this.txtInstalledDetails.Multiline = true;
            this.txtInstalledDetails.Name = "txtInstalledDetails";
            this.txtInstalledDetails.ReadOnly = true;
            this.txtInstalledDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtInstalledDetails.Size = new System.Drawing.Size(930, 68);
            this.txtInstalledDetails.TabIndex = 1;
            //
            // panelInstalledButtons
            //
            this.panelInstalledButtons.AutoSize = true;
            this.panelInstalledButtons.Controls.Add(this.btnRefresh);
            this.panelInstalledButtons.Controls.Add(this.btnInstallFromFile);
            this.panelInstalledButtons.Controls.Add(this.btnDisable);
            this.panelInstalledButtons.Controls.Add(this.btnEnable);
            this.panelInstalledButtons.Controls.Add(this.btnUninstall);
            this.panelInstalledButtons.Controls.Add(this.btnRemoveRegistration);
            this.panelInstalledButtons.Controls.Add(this.btnOpenFolder);
            this.panelInstalledButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelInstalledButtons.Location = new System.Drawing.Point(3, 376);
            this.panelInstalledButtons.Name = "panelInstalledButtons";
            this.panelInstalledButtons.Size = new System.Drawing.Size(930, 36);
            this.panelInstalledButtons.TabIndex = 2;
            //
            // btnRefresh
            //
            this.btnRefresh.AutoSize = true;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "&Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // btnInstallFromFile
            //
            this.btnInstallFromFile.AutoSize = true;
            this.btnInstallFromFile.Name = "btnInstallFromFile";
            this.btnInstallFromFile.TabIndex = 1;
            this.btnInstallFromFile.Text = "Install from &file...";
            this.btnInstallFromFile.UseVisualStyleBackColor = true;
            this.btnInstallFromFile.Click += new System.EventHandler(this.btnInstallFromFile_Click);
            //
            // btnDisable
            //
            this.btnDisable.AutoSize = true;
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.TabIndex = 2;
            this.btnDisable.Text = "&Disable";
            this.btnDisable.UseVisualStyleBackColor = true;
            this.btnDisable.Click += new System.EventHandler(this.btnDisable_Click);
            //
            // btnEnable
            //
            this.btnEnable.AutoSize = true;
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.TabIndex = 3;
            this.btnEnable.Text = "&Enable";
            this.btnEnable.UseVisualStyleBackColor = true;
            this.btnEnable.Click += new System.EventHandler(this.btnEnable_Click);
            //
            // btnUninstall
            //
            this.btnUninstall.AutoSize = true;
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.TabIndex = 4;
            this.btnUninstall.Text = "&Uninstall";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            //
            // btnRemoveRegistration
            //
            this.btnRemoveRegistration.AutoSize = true;
            this.btnRemoveRegistration.Name = "btnRemoveRegistration";
            this.btnRemoveRegistration.TabIndex = 5;
            this.btnRemoveRegistration.Text = "Remove &broken registration";
            this.btnRemoveRegistration.UseVisualStyleBackColor = true;
            this.btnRemoveRegistration.Click += new System.EventHandler(this.btnRemoveRegistration_Click);
            //
            // btnOpenFolder
            //
            this.btnOpenFolder.AutoSize = true;
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.TabIndex = 6;
            this.btnOpenFolder.Text = "&Open folder";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            //
            // tabAvailable
            //
            this.tabAvailable.Controls.Add(this.splitAvailable);
            this.tabAvailable.Controls.Add(this.panelCatalog);
            this.tabAvailable.Controls.Add(this.panelAvailableButtons);
            this.tabAvailable.Location = new System.Drawing.Point(4, 22);
            this.tabAvailable.Name = "tabAvailable";
            this.tabAvailable.Padding = new System.Windows.Forms.Padding(3);
            this.tabAvailable.Size = new System.Drawing.Size(936, 415);
            this.tabAvailable.TabIndex = 1;
            this.tabAvailable.Text = "Available";
            this.tabAvailable.UseVisualStyleBackColor = true;
            //
            // splitAvailable
            //
            this.splitAvailable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitAvailable.Name = "splitAvailable";
            this.splitAvailable.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitAvailable.Panel1.Controls.Add(this.listAvailable);
            this.splitAvailable.Panel2.Controls.Add(this.txtAvailableDetails);
            this.splitAvailable.Size = new System.Drawing.Size(930, 341);
            this.splitAvailable.SplitterDistance = 200;
            this.splitAvailable.TabIndex = 1;
            //
            // listAvailable
            //
            this.listAvailable.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colAvailableName, this.colAvailablePublisher, this.colAvailableLatest, this.colAvailableInstalled, this.colAvailableSummary });
            this.listAvailable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listAvailable.FullRowSelect = true;
            this.listAvailable.HideSelection = false;
            this.listAvailable.MultiSelect = false;
            this.listAvailable.Name = "listAvailable";
            this.listAvailable.TabIndex = 0;
            this.listAvailable.UseCompatibleStateImageBehavior = false;
            this.listAvailable.View = System.Windows.Forms.View.Details;
            this.listAvailable.SelectedIndexChanged += new System.EventHandler(this.listAvailable_SelectedIndexChanged);
            //
            // column headers (Available)
            //
            this.colAvailableName.Text = "Name";
            this.colAvailableName.Width = 220;
            this.colAvailablePublisher.Text = "Publisher";
            this.colAvailablePublisher.Width = 130;
            this.colAvailableLatest.Text = "Latest";
            this.colAvailableLatest.Width = 80;
            this.colAvailableInstalled.Text = "Installed";
            this.colAvailableInstalled.Width = 80;
            this.colAvailableSummary.Text = "Summary";
            this.colAvailableSummary.Width = 400;
            //
            // txtAvailableDetails
            //
            this.txtAvailableDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAvailableDetails.Multiline = true;
            this.txtAvailableDetails.Name = "txtAvailableDetails";
            this.txtAvailableDetails.ReadOnly = true;
            this.txtAvailableDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtAvailableDetails.TabIndex = 0;
            //
            // panelCatalog
            //
            this.panelCatalog.AutoSize = true;
            this.panelCatalog.ColumnCount = 3;
            this.panelCatalog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelCatalog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelCatalog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.panelCatalog.Controls.Add(this.lblCatalogUrl, 0, 0);
            this.panelCatalog.Controls.Add(this.txtCatalogUrl, 1, 0);
            this.panelCatalog.Controls.Add(this.btnLoadCatalog, 2, 0);
            this.panelCatalog.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelCatalog.Name = "panelCatalog";
            this.panelCatalog.RowCount = 1;
            this.panelCatalog.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelCatalog.TabIndex = 0;
            //
            // lblCatalogUrl
            //
            this.lblCatalogUrl.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCatalogUrl.AutoSize = true;
            this.lblCatalogUrl.Name = "lblCatalogUrl";
            this.lblCatalogUrl.Text = "Catalog:";
            //
            // txtCatalogUrl
            //
            this.txtCatalogUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCatalogUrl.Name = "txtCatalogUrl";
            this.txtCatalogUrl.TabIndex = 0;
            //
            // btnLoadCatalog
            //
            this.btnLoadCatalog.AutoSize = true;
            this.btnLoadCatalog.Name = "btnLoadCatalog";
            this.btnLoadCatalog.TabIndex = 1;
            this.btnLoadCatalog.Text = "&Load";
            this.btnLoadCatalog.UseVisualStyleBackColor = true;
            this.btnLoadCatalog.Click += new System.EventHandler(this.btnLoadCatalog_Click);
            //
            // panelAvailableButtons
            //
            this.panelAvailableButtons.AutoSize = true;
            this.panelAvailableButtons.Controls.Add(this.btnInstallSelected);
            this.panelAvailableButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelAvailableButtons.Name = "panelAvailableButtons";
            this.panelAvailableButtons.TabIndex = 2;
            //
            // btnInstallSelected
            //
            this.btnInstallSelected.AutoSize = true;
            this.btnInstallSelected.Name = "btnInstallSelected";
            this.btnInstallSelected.TabIndex = 0;
            this.btnInstallSelected.Text = "&Install / Update";
            this.btnInstallSelected.UseVisualStyleBackColor = true;
            this.btnInstallSelected.Click += new System.EventHandler(this.btnInstallSelected_Click);
            //
            // groupPending
            //
            this.groupPending.Controls.Add(this.listPending);
            this.groupPending.Controls.Add(this.panelPendingButtons);
            this.groupPending.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupPending.Location = new System.Drawing.Point(0, 441);
            this.groupPending.Name = "groupPending";
            this.groupPending.Size = new System.Drawing.Size(944, 130);
            this.groupPending.TabIndex = 1;
            this.groupPending.TabStop = false;
            this.groupPending.Text = "Pending changes";
            //
            // listPending
            //
            this.listPending.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listPending.IntegralHeight = false;
            this.listPending.Name = "listPending";
            this.listPending.TabIndex = 0;
            //
            // panelPendingButtons
            //
            this.panelPendingButtons.AutoSize = true;
            this.panelPendingButtons.Controls.Add(this.btnApply);
            this.panelPendingButtons.Controls.Add(this.btnRemovePending);
            this.panelPendingButtons.Controls.Add(this.btnClearPending);
            this.panelPendingButtons.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelPendingButtons.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.panelPendingButtons.Name = "panelPendingButtons";
            this.panelPendingButtons.TabIndex = 1;
            //
            // btnApply
            //
            this.btnApply.AutoSize = true;
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(130, 25);
            this.btnApply.TabIndex = 0;
            this.btnApply.Text = "&Apply changes...";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            //
            // btnRemovePending
            //
            this.btnRemovePending.AutoSize = true;
            this.btnRemovePending.Name = "btnRemovePending";
            this.btnRemovePending.Size = new System.Drawing.Size(130, 25);
            this.btnRemovePending.TabIndex = 1;
            this.btnRemovePending.Text = "Remove selected";
            this.btnRemovePending.UseVisualStyleBackColor = true;
            this.btnRemovePending.Click += new System.EventHandler(this.btnRemovePending_Click);
            //
            // btnClearPending
            //
            this.btnClearPending.AutoSize = true;
            this.btnClearPending.Name = "btnClearPending";
            this.btnClearPending.Size = new System.Drawing.Size(130, 25);
            this.btnClearPending.TabIndex = 2;
            this.btnClearPending.Text = "Clear all";
            this.btnClearPending.UseVisualStyleBackColor = true;
            this.btnClearPending.Click += new System.EventHandler(this.btnClearPending_Click);
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabel });
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.TabIndex = 2;
            //
            // statusLabel
            //
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Spring = true;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // openPackageDialog
            //
            this.openPackageDialog.Filter = "Alibre add-on package (*.alibreaddon)|*.alibreaddon|Zip file (*.zip)|*.zip|All files (*.*)|*.*";
            this.openPackageDialog.Title = "Install add-on package";
            //
            // ManagerForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(944, 593);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.groupPending);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(700, 450);
            this.Name = "ManagerForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Alibre Add-On Manager";
            this.Load += new System.EventHandler(this.ManagerForm_Load);
            this.tabControl.ResumeLayout(false);
            this.tabInstalled.ResumeLayout(false);
            this.tabInstalled.PerformLayout();
            this.panelInstalledButtons.ResumeLayout(false);
            this.panelInstalledButtons.PerformLayout();
            this.tabAvailable.ResumeLayout(false);
            this.tabAvailable.PerformLayout();
            this.splitAvailable.Panel1.ResumeLayout(false);
            this.splitAvailable.Panel2.ResumeLayout(false);
            this.splitAvailable.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitAvailable)).EndInit();
            this.splitAvailable.ResumeLayout(false);
            this.panelCatalog.ResumeLayout(false);
            this.panelCatalog.PerformLayout();
            this.panelAvailableButtons.ResumeLayout(false);
            this.panelAvailableButtons.PerformLayout();
            this.groupPending.ResumeLayout(false);
            this.groupPending.PerformLayout();
            this.panelPendingButtons.ResumeLayout(false);
            this.panelPendingButtons.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabInstalled;
        private System.Windows.Forms.ListView listInstalled;
        private System.Windows.Forms.ColumnHeader colInstalledName;
        private System.Windows.Forms.ColumnHeader colInstalledVersion;
        private System.Windows.Forms.ColumnHeader colInstalledStatus;
        private System.Windows.Forms.ColumnHeader colInstalledType;
        private System.Windows.Forms.ColumnHeader colInstalledFolder;
        private System.Windows.Forms.TextBox txtInstalledDetails;
        private System.Windows.Forms.FlowLayoutPanel panelInstalledButtons;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnInstallFromFile;
        private System.Windows.Forms.Button btnDisable;
        private System.Windows.Forms.Button btnEnable;
        private System.Windows.Forms.Button btnUninstall;
        private System.Windows.Forms.Button btnRemoveRegistration;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.TabPage tabAvailable;
        private System.Windows.Forms.SplitContainer splitAvailable;
        private System.Windows.Forms.ListView listAvailable;
        private System.Windows.Forms.ColumnHeader colAvailableName;
        private System.Windows.Forms.ColumnHeader colAvailablePublisher;
        private System.Windows.Forms.ColumnHeader colAvailableLatest;
        private System.Windows.Forms.ColumnHeader colAvailableInstalled;
        private System.Windows.Forms.ColumnHeader colAvailableSummary;
        private System.Windows.Forms.TextBox txtAvailableDetails;
        private System.Windows.Forms.TableLayoutPanel panelCatalog;
        private System.Windows.Forms.Label lblCatalogUrl;
        private System.Windows.Forms.TextBox txtCatalogUrl;
        private System.Windows.Forms.Button btnLoadCatalog;
        private System.Windows.Forms.FlowLayoutPanel panelAvailableButtons;
        private System.Windows.Forms.Button btnInstallSelected;
        private System.Windows.Forms.GroupBox groupPending;
        private System.Windows.Forms.ListBox listPending;
        private System.Windows.Forms.FlowLayoutPanel panelPendingButtons;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnRemovePending;
        private System.Windows.Forms.Button btnClearPending;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.OpenFileDialog openPackageDialog;
    }
}
