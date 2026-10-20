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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManagerForm));
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
            this.tabControl.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1259, 548);
            this.tabControl.TabIndex = 0;
            // 
            // tabInstalled
            // 
            this.tabInstalled.Controls.Add(this.listInstalled);
            this.tabInstalled.Controls.Add(this.txtInstalledDetails);
            this.tabInstalled.Controls.Add(this.panelInstalledButtons);
            this.tabInstalled.Location = new System.Drawing.Point(4, 25);
            this.tabInstalled.Margin = new System.Windows.Forms.Padding(4);
            this.tabInstalled.Name = "tabInstalled";
            this.tabInstalled.Padding = new System.Windows.Forms.Padding(4);
            this.tabInstalled.Size = new System.Drawing.Size(1251, 519);
            this.tabInstalled.TabIndex = 0;
            this.tabInstalled.Text = "Installed";
            this.tabInstalled.UseVisualStyleBackColor = true;
            // 
            // listInstalled
            // 
            this.listInstalled.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colInstalledName, this.colInstalledVersion, this.colInstalledStatus, this.colInstalledType, this.colInstalledFolder });
            this.listInstalled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listInstalled.FullRowSelect = true;
            this.listInstalled.HideSelection = false;
            this.listInstalled.Location = new System.Drawing.Point(4, 4);
            this.listInstalled.Margin = new System.Windows.Forms.Padding(4);
            this.listInstalled.MultiSelect = false;
            this.listInstalled.Name = "listInstalled";
            this.listInstalled.Size = new System.Drawing.Size(1243, 388);
            this.listInstalled.TabIndex = 0;
            this.listInstalled.UseCompatibleStateImageBehavior = false;
            this.listInstalled.View = System.Windows.Forms.View.Details;
            this.listInstalled.SelectedIndexChanged += new System.EventHandler(this.listInstalled_SelectedIndexChanged);
            // 
            // colInstalledName
            // 
            this.colInstalledName.Text = "Name";
            this.colInstalledName.Width = 220;
            // 
            // colInstalledVersion
            // 
            this.colInstalledVersion.Text = "Version";
            this.colInstalledVersion.Width = 90;
            // 
            // colInstalledStatus
            // 
            this.colInstalledStatus.Text = "Status";
            this.colInstalledStatus.Width = 120;
            // 
            // colInstalledType
            // 
            this.colInstalledType.Text = "DLL";
            this.colInstalledType.Width = 70;
            // 
            // colInstalledFolder
            // 
            this.colInstalledFolder.Text = "Folder";
            this.colInstalledFolder.Width = 420;
            // 
            // txtInstalledDetails
            // 
            this.txtInstalledDetails.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtInstalledDetails.Location = new System.Drawing.Point(4, 392);
            this.txtInstalledDetails.Margin = new System.Windows.Forms.Padding(4);
            this.txtInstalledDetails.Multiline = true;
            this.txtInstalledDetails.Name = "txtInstalledDetails";
            this.txtInstalledDetails.ReadOnly = true;
            this.txtInstalledDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtInstalledDetails.Size = new System.Drawing.Size(1243, 83);
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
            this.panelInstalledButtons.Location = new System.Drawing.Point(4, 475);
            this.panelInstalledButtons.Margin = new System.Windows.Forms.Padding(4);
            this.panelInstalledButtons.Name = "panelInstalledButtons";
            this.panelInstalledButtons.Size = new System.Drawing.Size(1243, 40);
            this.panelInstalledButtons.TabIndex = 2;
            // 
            // btnRefresh
            // 
            this.btnRefresh.AutoSize = true;
            this.btnRefresh.Location = new System.Drawing.Point(4, 4);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(4);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(100, 32);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "&Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnInstallFromFile
            // 
            this.btnInstallFromFile.AutoSize = true;
            this.btnInstallFromFile.Location = new System.Drawing.Point(112, 4);
            this.btnInstallFromFile.Margin = new System.Windows.Forms.Padding(4);
            this.btnInstallFromFile.Name = "btnInstallFromFile";
            this.btnInstallFromFile.Size = new System.Drawing.Size(145, 32);
            this.btnInstallFromFile.TabIndex = 1;
            this.btnInstallFromFile.Text = "Install from &file...";
            this.btnInstallFromFile.UseVisualStyleBackColor = true;
            this.btnInstallFromFile.Click += new System.EventHandler(this.btnInstallFromFile_Click);
            // 
            // btnDisable
            // 
            this.btnDisable.AutoSize = true;
            this.btnDisable.Location = new System.Drawing.Point(265, 4);
            this.btnDisable.Margin = new System.Windows.Forms.Padding(4);
            this.btnDisable.Name = "btnDisable";
            this.btnDisable.Size = new System.Drawing.Size(100, 32);
            this.btnDisable.TabIndex = 2;
            this.btnDisable.Text = "&Disable";
            this.btnDisable.UseVisualStyleBackColor = true;
            this.btnDisable.Click += new System.EventHandler(this.btnDisable_Click);
            // 
            // btnEnable
            // 
            this.btnEnable.AutoSize = true;
            this.btnEnable.Location = new System.Drawing.Point(373, 4);
            this.btnEnable.Margin = new System.Windows.Forms.Padding(4);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(100, 32);
            this.btnEnable.TabIndex = 3;
            this.btnEnable.Text = "&Enable";
            this.btnEnable.UseVisualStyleBackColor = true;
            this.btnEnable.Click += new System.EventHandler(this.btnEnable_Click);
            // 
            // btnUninstall
            // 
            this.btnUninstall.AutoSize = true;
            this.btnUninstall.Location = new System.Drawing.Point(481, 4);
            this.btnUninstall.Margin = new System.Windows.Forms.Padding(4);
            this.btnUninstall.Name = "btnUninstall";
            this.btnUninstall.Size = new System.Drawing.Size(100, 32);
            this.btnUninstall.TabIndex = 4;
            this.btnUninstall.Text = "&Uninstall";
            this.btnUninstall.UseVisualStyleBackColor = true;
            this.btnUninstall.Click += new System.EventHandler(this.btnUninstall_Click);
            // 
            // btnRemoveRegistration
            // 
            this.btnRemoveRegistration.AutoSize = true;
            this.btnRemoveRegistration.Location = new System.Drawing.Point(589, 4);
            this.btnRemoveRegistration.Margin = new System.Windows.Forms.Padding(4);
            this.btnRemoveRegistration.Name = "btnRemoveRegistration";
            this.btnRemoveRegistration.Size = new System.Drawing.Size(244, 32);
            this.btnRemoveRegistration.TabIndex = 5;
            this.btnRemoveRegistration.Text = "Remove &broken registration";
            this.btnRemoveRegistration.UseVisualStyleBackColor = true;
            this.btnRemoveRegistration.Click += new System.EventHandler(this.btnRemoveRegistration_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.AutoSize = true;
            this.btnOpenFolder.Location = new System.Drawing.Point(841, 4);
            this.btnOpenFolder.Margin = new System.Windows.Forms.Padding(4);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(116, 32);
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
            this.tabAvailable.Location = new System.Drawing.Point(4, 25);
            this.tabAvailable.Margin = new System.Windows.Forms.Padding(4);
            this.tabAvailable.Name = "tabAvailable";
            this.tabAvailable.Padding = new System.Windows.Forms.Padding(4);
            this.tabAvailable.Size = new System.Drawing.Size(1251, 519);
            this.tabAvailable.TabIndex = 1;
            this.tabAvailable.Text = "Available";
            this.tabAvailable.UseVisualStyleBackColor = true;
            // 
            // splitAvailable
            // 
            this.splitAvailable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitAvailable.Location = new System.Drawing.Point(4, 44);
            this.splitAvailable.Margin = new System.Windows.Forms.Padding(4);
            this.splitAvailable.Name = "splitAvailable";
            this.splitAvailable.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitAvailable.Panel1
            // 
            this.splitAvailable.Panel1.Controls.Add(this.listAvailable);
            // 
            // splitAvailable.Panel2
            // 
            this.splitAvailable.Panel2.Controls.Add(this.txtAvailableDetails);
            this.splitAvailable.Size = new System.Drawing.Size(1243, 431);
            this.splitAvailable.SplitterDistance = 251;
            this.splitAvailable.SplitterWidth = 5;
            this.splitAvailable.TabIndex = 1;
            // 
            // listAvailable
            // 
            this.listAvailable.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { this.colAvailableName, this.colAvailablePublisher, this.colAvailableLatest, this.colAvailableInstalled, this.colAvailableSummary });
            this.listAvailable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listAvailable.FullRowSelect = true;
            this.listAvailable.HideSelection = false;
            this.listAvailable.Location = new System.Drawing.Point(0, 0);
            this.listAvailable.Margin = new System.Windows.Forms.Padding(4);
            this.listAvailable.MultiSelect = false;
            this.listAvailable.Name = "listAvailable";
            this.listAvailable.Size = new System.Drawing.Size(1243, 251);
            this.listAvailable.TabIndex = 0;
            this.listAvailable.UseCompatibleStateImageBehavior = false;
            this.listAvailable.View = System.Windows.Forms.View.Details;
            this.listAvailable.SelectedIndexChanged += new System.EventHandler(this.listAvailable_SelectedIndexChanged);
            // 
            // colAvailableName
            // 
            this.colAvailableName.Text = "Name";
            this.colAvailableName.Width = 220;
            // 
            // colAvailablePublisher
            // 
            this.colAvailablePublisher.Text = "Publisher";
            this.colAvailablePublisher.Width = 130;
            // 
            // colAvailableLatest
            // 
            this.colAvailableLatest.Text = "Latest";
            this.colAvailableLatest.Width = 80;
            // 
            // colAvailableInstalled
            // 
            this.colAvailableInstalled.Text = "Installed";
            this.colAvailableInstalled.Width = 80;
            // 
            // colAvailableSummary
            // 
            this.colAvailableSummary.Text = "Summary";
            this.colAvailableSummary.Width = 400;
            // 
            // txtAvailableDetails
            // 
            this.txtAvailableDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAvailableDetails.Location = new System.Drawing.Point(0, 0);
            this.txtAvailableDetails.Margin = new System.Windows.Forms.Padding(4);
            this.txtAvailableDetails.Multiline = true;
            this.txtAvailableDetails.Name = "txtAvailableDetails";
            this.txtAvailableDetails.ReadOnly = true;
            this.txtAvailableDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtAvailableDetails.Size = new System.Drawing.Size(1243, 175);
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
            this.panelCatalog.Location = new System.Drawing.Point(4, 4);
            this.panelCatalog.Margin = new System.Windows.Forms.Padding(4);
            this.panelCatalog.Name = "panelCatalog";
            this.panelCatalog.RowCount = 1;
            this.panelCatalog.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.panelCatalog.Size = new System.Drawing.Size(1243, 40);
            this.panelCatalog.TabIndex = 0;
            // 
            // lblCatalogUrl
            // 
            this.lblCatalogUrl.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCatalogUrl.AutoSize = true;
            this.lblCatalogUrl.Location = new System.Drawing.Point(4, 12);
            this.lblCatalogUrl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCatalogUrl.Name = "lblCatalogUrl";
            this.lblCatalogUrl.Size = new System.Drawing.Size(57, 16);
            this.lblCatalogUrl.TabIndex = 0;
            this.lblCatalogUrl.Text = "Catalog:";
            // 
            // txtCatalogUrl
            // 
            this.txtCatalogUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCatalogUrl.Location = new System.Drawing.Point(69, 9);
            this.txtCatalogUrl.Margin = new System.Windows.Forms.Padding(4);
            this.txtCatalogUrl.Name = "txtCatalogUrl";
            this.txtCatalogUrl.Size = new System.Drawing.Size(1062, 22);
            this.txtCatalogUrl.TabIndex = 0;
            // 
            // btnLoadCatalog
            // 
            this.btnLoadCatalog.AutoSize = true;
            this.btnLoadCatalog.Location = new System.Drawing.Point(1139, 4);
            this.btnLoadCatalog.Margin = new System.Windows.Forms.Padding(4);
            this.btnLoadCatalog.Name = "btnLoadCatalog";
            this.btnLoadCatalog.Size = new System.Drawing.Size(100, 32);
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
            this.panelAvailableButtons.Location = new System.Drawing.Point(4, 475);
            this.panelAvailableButtons.Margin = new System.Windows.Forms.Padding(4);
            this.panelAvailableButtons.Name = "panelAvailableButtons";
            this.panelAvailableButtons.Size = new System.Drawing.Size(1243, 40);
            this.panelAvailableButtons.TabIndex = 2;
            // 
            // btnInstallSelected
            // 
            this.btnInstallSelected.AutoSize = true;
            this.btnInstallSelected.Location = new System.Drawing.Point(4, 4);
            this.btnInstallSelected.Margin = new System.Windows.Forms.Padding(4);
            this.btnInstallSelected.Name = "btnInstallSelected";
            this.btnInstallSelected.Size = new System.Drawing.Size(141, 32);
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
            this.groupPending.Location = new System.Drawing.Point(0, 548);
            this.groupPending.Margin = new System.Windows.Forms.Padding(4);
            this.groupPending.Name = "groupPending";
            this.groupPending.Padding = new System.Windows.Forms.Padding(4);
            this.groupPending.Size = new System.Drawing.Size(1259, 160);
            this.groupPending.TabIndex = 1;
            this.groupPending.TabStop = false;
            this.groupPending.Text = "Pending changes";
            // 
            // listPending
            // 
            this.listPending.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listPending.IntegralHeight = false;
            this.listPending.ItemHeight = 16;
            this.listPending.Location = new System.Drawing.Point(4, 19);
            this.listPending.Margin = new System.Windows.Forms.Padding(4);
            this.listPending.Name = "listPending";
            this.listPending.Size = new System.Drawing.Size(1070, 137);
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
            this.panelPendingButtons.Location = new System.Drawing.Point(1074, 19);
            this.panelPendingButtons.Margin = new System.Windows.Forms.Padding(4);
            this.panelPendingButtons.Name = "panelPendingButtons";
            this.panelPendingButtons.Size = new System.Drawing.Size(181, 137);
            this.panelPendingButtons.TabIndex = 1;
            // 
            // btnApply
            // 
            this.btnApply.AutoSize = true;
            this.btnApply.Location = new System.Drawing.Point(4, 4);
            this.btnApply.Margin = new System.Windows.Forms.Padding(4);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(173, 32);
            this.btnApply.TabIndex = 0;
            this.btnApply.Text = "&Apply changes...";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnRemovePending
            // 
            this.btnRemovePending.AutoSize = true;
            this.btnRemovePending.Location = new System.Drawing.Point(4, 44);
            this.btnRemovePending.Margin = new System.Windows.Forms.Padding(4);
            this.btnRemovePending.Name = "btnRemovePending";
            this.btnRemovePending.Size = new System.Drawing.Size(173, 32);
            this.btnRemovePending.TabIndex = 1;
            this.btnRemovePending.Text = "Remove selected";
            this.btnRemovePending.UseVisualStyleBackColor = true;
            this.btnRemovePending.Click += new System.EventHandler(this.btnRemovePending_Click);
            // 
            // btnClearPending
            // 
            this.btnClearPending.AutoSize = true;
            this.btnClearPending.Location = new System.Drawing.Point(4, 84);
            this.btnClearPending.Margin = new System.Windows.Forms.Padding(4);
            this.btnClearPending.Name = "btnClearPending";
            this.btnClearPending.Size = new System.Drawing.Size(173, 32);
            this.btnClearPending.TabIndex = 2;
            this.btnClearPending.Text = "Clear all";
            this.btnClearPending.UseVisualStyleBackColor = true;
            this.btnClearPending.Click += new System.EventHandler(this.btnClearPending_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.statusLabel });
            this.statusStrip.Location = new System.Drawing.Point(0, 708);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip.Size = new System.Drawing.Size(1259, 22);
            this.statusStrip.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(1239, 17);
            this.statusLabel.Spring = true;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // openPackageDialog
            // 
            this.openPackageDialog.Filter = "Alibre add-on package (*.alibreaddon)|*.alibreaddon|Zip file (*.zip)|*.zip|All fi" + "les (*.*)|*.*";
            this.openPackageDialog.Title = "Install add-on package";
            // 
            // ManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1259, 730);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.groupPending);
            this.Controls.Add(this.statusStrip);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(927, 543);
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
