namespace LibraryViewer
{
    partial class Main
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabComponents = new System.Windows.Forms.TabControl();
            this.tabPcbLib = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.gridPcbLibComponents = new System.Windows.Forms.DataGridView();
            this.gridPcbLibComponentsName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibComponentsPads = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibComponentsPrimitives = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibPrimitives = new System.Windows.Forms.DataGridView();
            this.gridPcbLibPrimitivesType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibPrimitivesName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibPrimitivesXSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibPrimitivesYSize = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridPcbLibPrimitivesLayer = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabSchLib = new System.Windows.Forms.TabPage();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.gridSchLibComponents = new System.Windows.Forms.DataGridView();
            this.gridSchLibComponentName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridSchLibComponentDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridSchLibPrimitives = new System.Windows.Forms.DataGridView();
            this.gridSchLibPrimitiveDescriptor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridSchLibPrimitiveName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gridSchLibPrimitiveType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelPart = new System.Windows.Forms.Panel();
            this.labelPartTotal = new System.Windows.Forms.Label();
            this.editPart = new System.Windows.Forms.NumericUpDown();
            this.labelPart = new System.Windows.Forms.Label();
            this.tabTree = new System.Windows.Forms.TabPage();
            this.treeViewStructure = new System.Windows.Forms.TreeView();
            this.pictureBox = new System.Windows.Forms.Panel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLocation = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.centerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.zoom50ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom100ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom200ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zoom400ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exportFootprintToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exportPrimitiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportImageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.redrawTimer = new System.Windows.Forms.Timer(this.components);
            this.testPcbLibCreationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabComponents.SuspendLayout();
            this.tabPcbLib.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridPcbLibComponents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPcbLibPrimitives)).BeginInit();
            this.tabSchLib.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridSchLibComponents)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridSchLibPrimitives)).BeginInit();
            this.panelPart.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editPart)).BeginInit();
            this.tabTree.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabComponents);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pictureBox);
            this.splitContainer1.Size = new System.Drawing.Size(990, 513);
            this.splitContainer1.SplitterDistance = 329;
            this.splitContainer1.TabIndex = 0;
            // 
            // tabComponents
            // 
            this.tabComponents.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabComponents.Controls.Add(this.tabPcbLib);
            this.tabComponents.Controls.Add(this.tabSchLib);
            this.tabComponents.Controls.Add(this.tabTree);
            this.tabComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabComponents.Location = new System.Drawing.Point(0, 0);
            this.tabComponents.Name = "tabComponents";
            this.tabComponents.SelectedIndex = 0;
            this.tabComponents.Size = new System.Drawing.Size(329, 513);
            this.tabComponents.TabIndex = 0;
            // 
            // tabPcbLib
            // 
            this.tabPcbLib.Controls.Add(this.splitContainer2);
            this.tabPcbLib.Location = new System.Drawing.Point(4, 4);
            this.tabPcbLib.Name = "tabPcbLib";
            this.tabPcbLib.Padding = new System.Windows.Forms.Padding(3);
            this.tabPcbLib.Size = new System.Drawing.Size(321, 487);
            this.tabPcbLib.TabIndex = 1;
            this.tabPcbLib.Text = "PCB Library";
            this.tabPcbLib.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.gridPcbLibComponents);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.gridPcbLibPrimitives);
            this.splitContainer2.Size = new System.Drawing.Size(315, 481);
            this.splitContainer2.SplitterDistance = 236;
            this.splitContainer2.TabIndex = 0;
            // 
            // gridPcbLibComponents
            // 
            this.gridPcbLibComponents.AllowUserToAddRows = false;
            this.gridPcbLibComponents.AllowUserToDeleteRows = false;
            this.gridPcbLibComponents.AllowUserToResizeRows = false;
            this.gridPcbLibComponents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPcbLibComponents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.gridPcbLibComponentsName,
            this.gridPcbLibComponentsPads,
            this.gridPcbLibComponentsPrimitives});
            this.gridPcbLibComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPcbLibComponents.Location = new System.Drawing.Point(0, 0);
            this.gridPcbLibComponents.MultiSelect = false;
            this.gridPcbLibComponents.Name = "gridPcbLibComponents";
            this.gridPcbLibComponents.ReadOnly = true;
            this.gridPcbLibComponents.RowHeadersVisible = false;
            this.gridPcbLibComponents.RowTemplate.ReadOnly = true;
            this.gridPcbLibComponents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gridPcbLibComponents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPcbLibComponents.Size = new System.Drawing.Size(315, 236);
            this.gridPcbLibComponents.TabIndex = 0;
            this.gridPcbLibComponents.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.GridPcbLibComponents_RowEnter);
            // 
            // gridPcbLibComponentsName
            // 
            this.gridPcbLibComponentsName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridPcbLibComponentsName.HeaderText = "Name";
            this.gridPcbLibComponentsName.MinimumWidth = 50;
            this.gridPcbLibComponentsName.Name = "gridPcbLibComponentsName";
            this.gridPcbLibComponentsName.ReadOnly = true;
            // 
            // gridPcbLibComponentsPads
            // 
            this.gridPcbLibComponentsPads.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridPcbLibComponentsPads.HeaderText = "Pads";
            this.gridPcbLibComponentsPads.MinimumWidth = 50;
            this.gridPcbLibComponentsPads.Name = "gridPcbLibComponentsPads";
            this.gridPcbLibComponentsPads.ReadOnly = true;
            this.gridPcbLibComponentsPads.Width = 65;
            // 
            // gridPcbLibComponentsPrimitives
            // 
            this.gridPcbLibComponentsPrimitives.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridPcbLibComponentsPrimitives.HeaderText = "Primitives";
            this.gridPcbLibComponentsPrimitives.MinimumWidth = 50;
            this.gridPcbLibComponentsPrimitives.Name = "gridPcbLibComponentsPrimitives";
            this.gridPcbLibComponentsPrimitives.ReadOnly = true;
            this.gridPcbLibComponentsPrimitives.Width = 65;
            // 
            // gridPcbLibPrimitives
            // 
            this.gridPcbLibPrimitives.AllowUserToAddRows = false;
            this.gridPcbLibPrimitives.AllowUserToDeleteRows = false;
            this.gridPcbLibPrimitives.AllowUserToResizeRows = false;
            this.gridPcbLibPrimitives.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridPcbLibPrimitives.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.gridPcbLibPrimitivesType,
            this.gridPcbLibPrimitivesName,
            this.gridPcbLibPrimitivesXSize,
            this.gridPcbLibPrimitivesYSize,
            this.gridPcbLibPrimitivesLayer});
            this.gridPcbLibPrimitives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridPcbLibPrimitives.Location = new System.Drawing.Point(0, 0);
            this.gridPcbLibPrimitives.Name = "gridPcbLibPrimitives";
            this.gridPcbLibPrimitives.ReadOnly = true;
            this.gridPcbLibPrimitives.RowHeadersVisible = false;
            this.gridPcbLibPrimitives.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridPcbLibPrimitives.Size = new System.Drawing.Size(315, 241);
            this.gridPcbLibPrimitives.TabIndex = 0;
            this.gridPcbLibPrimitives.CellPainting += new System.Windows.Forms.DataGridViewCellPaintingEventHandler(this.GridPcbLibPrimitives_CellPainting);
            this.gridPcbLibPrimitives.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.GridPcbLibPrimitives_RowEnter);
            this.gridPcbLibPrimitives.DoubleClick += new System.EventHandler(this.GridPrimitives_DoubleClick);
            // 
            // gridPcbLibPrimitivesType
            // 
            this.gridPcbLibPrimitivesType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.gridPcbLibPrimitivesType.HeaderText = "Type";
            this.gridPcbLibPrimitivesType.MinimumWidth = 40;
            this.gridPcbLibPrimitivesType.Name = "gridPcbLibPrimitivesType";
            this.gridPcbLibPrimitivesType.ReadOnly = true;
            this.gridPcbLibPrimitivesType.Width = 50;
            // 
            // gridPcbLibPrimitivesName
            // 
            this.gridPcbLibPrimitivesName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridPcbLibPrimitivesName.HeaderText = "Name";
            this.gridPcbLibPrimitivesName.MinimumWidth = 50;
            this.gridPcbLibPrimitivesName.Name = "gridPcbLibPrimitivesName";
            this.gridPcbLibPrimitivesName.ReadOnly = true;
            // 
            // gridPcbLibPrimitivesXSize
            // 
            this.gridPcbLibPrimitivesXSize.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle1.Format = "#####0.0#mm";
            dataGridViewCellStyle1.NullValue = null;
            this.gridPcbLibPrimitivesXSize.DefaultCellStyle = dataGridViewCellStyle1;
            this.gridPcbLibPrimitivesXSize.HeaderText = "X-Size";
            this.gridPcbLibPrimitivesXSize.MinimumWidth = 40;
            this.gridPcbLibPrimitivesXSize.Name = "gridPcbLibPrimitivesXSize";
            this.gridPcbLibPrimitivesXSize.ReadOnly = true;
            this.gridPcbLibPrimitivesXSize.Width = 60;
            // 
            // gridPcbLibPrimitivesYSize
            // 
            this.gridPcbLibPrimitivesYSize.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle2.Format = "#####0.0#mm";
            dataGridViewCellStyle2.NullValue = null;
            this.gridPcbLibPrimitivesYSize.DefaultCellStyle = dataGridViewCellStyle2;
            this.gridPcbLibPrimitivesYSize.HeaderText = "Y-Size";
            this.gridPcbLibPrimitivesYSize.MinimumWidth = 40;
            this.gridPcbLibPrimitivesYSize.Name = "gridPcbLibPrimitivesYSize";
            this.gridPcbLibPrimitivesYSize.ReadOnly = true;
            this.gridPcbLibPrimitivesYSize.Width = 60;
            // 
            // gridPcbLibPrimitivesLayer
            // 
            this.gridPcbLibPrimitivesLayer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridPcbLibPrimitivesLayer.HeaderText = "Layer";
            this.gridPcbLibPrimitivesLayer.MinimumWidth = 50;
            this.gridPcbLibPrimitivesLayer.Name = "gridPcbLibPrimitivesLayer";
            this.gridPcbLibPrimitivesLayer.ReadOnly = true;
            // 
            // tabSchLib
            // 
            this.tabSchLib.Controls.Add(this.splitContainer3);
            this.tabSchLib.Location = new System.Drawing.Point(4, 4);
            this.tabSchLib.Name = "tabSchLib";
            this.tabSchLib.Padding = new System.Windows.Forms.Padding(3);
            this.tabSchLib.Size = new System.Drawing.Size(321, 487);
            this.tabSchLib.TabIndex = 2;
            this.tabSchLib.Text = "SCH Library";
            this.tabSchLib.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(3, 3);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.gridSchLibComponents);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.gridSchLibPrimitives);
            this.splitContainer3.Panel2.Controls.Add(this.panelPart);
            this.splitContainer3.Size = new System.Drawing.Size(315, 481);
            this.splitContainer3.SplitterDistance = 236;
            this.splitContainer3.TabIndex = 1;
            // 
            // gridSchLibComponents
            // 
            this.gridSchLibComponents.AllowUserToAddRows = false;
            this.gridSchLibComponents.AllowUserToDeleteRows = false;
            this.gridSchLibComponents.AllowUserToResizeRows = false;
            this.gridSchLibComponents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSchLibComponents.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.gridSchLibComponentName,
            this.gridSchLibComponentDescription});
            this.gridSchLibComponents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSchLibComponents.Location = new System.Drawing.Point(0, 0);
            this.gridSchLibComponents.MultiSelect = false;
            this.gridSchLibComponents.Name = "gridSchLibComponents";
            this.gridSchLibComponents.ReadOnly = true;
            this.gridSchLibComponents.RowHeadersVisible = false;
            this.gridSchLibComponents.RowTemplate.ReadOnly = true;
            this.gridSchLibComponents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.gridSchLibComponents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSchLibComponents.Size = new System.Drawing.Size(315, 236);
            this.gridSchLibComponents.TabIndex = 0;
            this.gridSchLibComponents.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.GridSchLibComponents_RowEnter);
            // 
            // gridSchLibComponentName
            // 
            this.gridSchLibComponentName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridSchLibComponentName.HeaderText = "Components";
            this.gridSchLibComponentName.MinimumWidth = 50;
            this.gridSchLibComponentName.Name = "gridSchLibComponentName";
            this.gridSchLibComponentName.ReadOnly = true;
            // 
            // gridSchLibComponentDescription
            // 
            this.gridSchLibComponentDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridSchLibComponentDescription.HeaderText = "Description";
            this.gridSchLibComponentDescription.MinimumWidth = 50;
            this.gridSchLibComponentDescription.Name = "gridSchLibComponentDescription";
            this.gridSchLibComponentDescription.ReadOnly = true;
            // 
            // gridSchLibPrimitives
            // 
            this.gridSchLibPrimitives.AllowUserToAddRows = false;
            this.gridSchLibPrimitives.AllowUserToDeleteRows = false;
            this.gridSchLibPrimitives.AllowUserToResizeRows = false;
            this.gridSchLibPrimitives.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridSchLibPrimitives.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.gridSchLibPrimitiveDescriptor,
            this.gridSchLibPrimitiveName,
            this.gridSchLibPrimitiveType});
            this.gridSchLibPrimitives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridSchLibPrimitives.Location = new System.Drawing.Point(0, 27);
            this.gridSchLibPrimitives.Name = "gridSchLibPrimitives";
            this.gridSchLibPrimitives.ReadOnly = true;
            this.gridSchLibPrimitives.RowHeadersVisible = false;
            this.gridSchLibPrimitives.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.gridSchLibPrimitives.Size = new System.Drawing.Size(315, 214);
            this.gridSchLibPrimitives.TabIndex = 0;
            this.gridSchLibPrimitives.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.GridSchLibPrimitives_RowEnter);
            this.gridSchLibPrimitives.DoubleClick += new System.EventHandler(this.GridPrimitives_DoubleClick);
            // 
            // gridSchLibPrimitiveDescriptor
            // 
            this.gridSchLibPrimitiveDescriptor.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridSchLibPrimitiveDescriptor.HeaderText = "Pins";
            this.gridSchLibPrimitiveDescriptor.MinimumWidth = 40;
            this.gridSchLibPrimitiveDescriptor.Name = "gridSchLibPrimitiveDescriptor";
            this.gridSchLibPrimitiveDescriptor.ReadOnly = true;
            // 
            // gridSchLibPrimitiveName
            // 
            this.gridSchLibPrimitiveName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridSchLibPrimitiveName.HeaderText = "Name";
            this.gridSchLibPrimitiveName.MinimumWidth = 50;
            this.gridSchLibPrimitiveName.Name = "gridSchLibPrimitiveName";
            this.gridSchLibPrimitiveName.ReadOnly = true;
            // 
            // gridSchLibPrimitiveType
            // 
            this.gridSchLibPrimitiveType.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.gridSchLibPrimitiveType.HeaderText = "Type";
            this.gridSchLibPrimitiveType.MinimumWidth = 50;
            this.gridSchLibPrimitiveType.Name = "gridSchLibPrimitiveType";
            this.gridSchLibPrimitiveType.ReadOnly = true;
            // 
            // panelPart
            // 
            this.panelPart.Controls.Add(this.labelPartTotal);
            this.panelPart.Controls.Add(this.editPart);
            this.panelPart.Controls.Add(this.labelPart);
            this.panelPart.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelPart.Location = new System.Drawing.Point(0, 0);
            this.panelPart.Name = "panelPart";
            this.panelPart.Size = new System.Drawing.Size(315, 27);
            this.panelPart.TabIndex = 1;
            // 
            // labelPartTotal
            // 
            this.labelPartTotal.AutoSize = true;
            this.labelPartTotal.Location = new System.Drawing.Point(95, 5);
            this.labelPartTotal.Name = "labelPartTotal";
            this.labelPartTotal.Size = new System.Drawing.Size(31, 13);
            this.labelPartTotal.TabIndex = 2;
            this.labelPartTotal.Text = "of 10";
            // 
            // editPart
            // 
            this.editPart.Location = new System.Drawing.Point(37, 3);
            this.editPart.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.editPart.Name = "editPart";
            this.editPart.Size = new System.Drawing.Size(52, 20);
            this.editPart.TabIndex = 1;
            this.editPart.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.editPart.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.editPart.ValueChanged += new System.EventHandler(this.EditPart_ValueChanged);
            // 
            // labelPart
            // 
            this.labelPart.AutoSize = true;
            this.labelPart.Location = new System.Drawing.Point(5, 5);
            this.labelPart.Name = "labelPart";
            this.labelPart.Size = new System.Drawing.Size(26, 13);
            this.labelPart.TabIndex = 0;
            this.labelPart.Text = "Part";
            // 
            // tabTree
            // 
            this.tabTree.Controls.Add(this.treeViewStructure);
            this.tabTree.Location = new System.Drawing.Point(4, 4);
            this.tabTree.Name = "tabTree";
            this.tabTree.Padding = new System.Windows.Forms.Padding(3);
            this.tabTree.Size = new System.Drawing.Size(321, 487);
            this.tabTree.TabIndex = 3;
            this.tabTree.Text = "Tree";
            this.tabTree.UseVisualStyleBackColor = true;
            // 
            // treeViewStructure
            // 
            this.treeViewStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeViewStructure.Location = new System.Drawing.Point(3, 3);
            this.treeViewStructure.Name = "treeViewStructure";
            this.treeViewStructure.Size = new System.Drawing.Size(315, 481);
            this.treeViewStructure.TabIndex = 0;
            this.treeViewStructure.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewStructure_AfterSelect);
            this.treeViewStructure.DoubleClick += new System.EventHandler(this.GridPrimitives_DoubleClick);
            // 
            // pictureBox
            // 
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(657, 513);
            this.pictureBox.TabIndex = 0;
            this.pictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.PictureBox_Paint);
            this.pictureBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseClick);
            this.pictureBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseDoubleClick);
            this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseMove);
            this.pictureBox.Resize += new System.EventHandler(this.PictureBox_Resize);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLocation});
            this.statusStrip.Location = new System.Drawing.Point(0, 537);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(990, 22);
            this.statusStrip.TabIndex = 0;
            // 
            // statusLocation
            // 
            this.statusLocation.Name = "statusLocation";
            this.statusLocation.Size = new System.Drawing.Size(0, 17);
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.exportToolStripMenuItem1});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(990, 24);
            this.menuStrip.TabIndex = 1;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(120, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.centerToolStripMenuItem,
            this.toolStripMenuItem2,
            this.zoom50ToolStripMenuItem,
            this.zoom100ToolStripMenuItem,
            this.zoom200ToolStripMenuItem,
            this.zoom400ToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // centerToolStripMenuItem
            // 
            this.centerToolStripMenuItem.Name = "centerToolStripMenuItem";
            this.centerToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Next)));
            this.centerToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.centerToolStripMenuItem.Text = "Fit All Objects";
            this.centerToolStripMenuItem.Click += new System.EventHandler(this.CenterToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(207, 6);
            // 
            // zoom50ToolStripMenuItem
            // 
            this.zoom50ToolStripMenuItem.Name = "zoom50ToolStripMenuItem";
            this.zoom50ToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.zoom50ToolStripMenuItem.Tag = "50";
            this.zoom50ToolStripMenuItem.Text = "50%";
            this.zoom50ToolStripMenuItem.Click += new System.EventHandler(this.ZoomToolStripMenuItem_Click);
            // 
            // zoom100ToolStripMenuItem
            // 
            this.zoom100ToolStripMenuItem.Name = "zoom100ToolStripMenuItem";
            this.zoom100ToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.zoom100ToolStripMenuItem.Tag = "100";
            this.zoom100ToolStripMenuItem.Text = "100%";
            this.zoom100ToolStripMenuItem.Click += new System.EventHandler(this.ZoomToolStripMenuItem_Click);
            // 
            // zoom200ToolStripMenuItem
            // 
            this.zoom200ToolStripMenuItem.Name = "zoom200ToolStripMenuItem";
            this.zoom200ToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.zoom200ToolStripMenuItem.Tag = "200";
            this.zoom200ToolStripMenuItem.Text = "200%";
            this.zoom200ToolStripMenuItem.Click += new System.EventHandler(this.ZoomToolStripMenuItem_Click);
            // 
            // zoom400ToolStripMenuItem
            // 
            this.zoom400ToolStripMenuItem.Name = "zoom400ToolStripMenuItem";
            this.zoom400ToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.zoom400ToolStripMenuItem.Tag = "400";
            this.zoom400ToolStripMenuItem.Text = "400%";
            this.zoom400ToolStripMenuItem.Click += new System.EventHandler(this.ZoomToolStripMenuItem_Click);
            // 
            // exportToolStripMenuItem1
            // 
            this.exportToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportFootprintToolStripMenuItem1,
            this.exportPrimitiveToolStripMenuItem,
            this.exportImageToolStripMenuItem,
            this.testToolStripMenuItem,
            this.testPcbLibCreationToolStripMenuItem});
            this.exportToolStripMenuItem1.Name = "exportToolStripMenuItem1";
            this.exportToolStripMenuItem1.Size = new System.Drawing.Size(52, 20);
            this.exportToolStripMenuItem1.Text = "Export";
            // 
            // exportFootprintToolStripMenuItem1
            // 
            this.exportFootprintToolStripMenuItem1.Name = "exportFootprintToolStripMenuItem1";
            this.exportFootprintToolStripMenuItem1.Size = new System.Drawing.Size(181, 22);
            this.exportFootprintToolStripMenuItem1.Text = "Export footprint";
            this.exportFootprintToolStripMenuItem1.Click += new System.EventHandler(this.ExportFootprintToolStripMenuItem1_Click);
            // 
            // exportPrimitiveToolStripMenuItem
            // 
            this.exportPrimitiveToolStripMenuItem.Name = "exportPrimitiveToolStripMenuItem";
            this.exportPrimitiveToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.exportPrimitiveToolStripMenuItem.Text = "Export primitive";
            this.exportPrimitiveToolStripMenuItem.Click += new System.EventHandler(this.ExportPrimitiveToolStripMenuItem_Click);
            // 
            // exportImageToolStripMenuItem
            // 
            this.exportImageToolStripMenuItem.Name = "exportImageToolStripMenuItem";
            this.exportImageToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.exportImageToolStripMenuItem.Text = "Export image";
            this.exportImageToolStripMenuItem.Click += new System.EventHandler(this.ExportImageToolStripMenuItem_Click);
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.testToolStripMenuItem.Text = "Test SchLib creation";
            this.testToolStripMenuItem.Click += new System.EventHandler(this.testToolStripMenuItem_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "Supported files|*.pcblib;*.schlib;*.schdoc|PcbLib files|*.pcblib|SchLib files|*.s" +
    "chlib|SchDoc files|*.schdoc|All files|*.*";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "bin";
            this.saveFileDialog.Filter = "Supported files|*.pcblib;*.schlib;*.schdoc|PcbLib files|*.pcblib|SchLib files|*.s" +
    "chlib|SchDoc files|*.schdoc|All files|*.*";
            // 
            // redrawTimer
            // 
            this.redrawTimer.Interval = 500;
            this.redrawTimer.Tick += new System.EventHandler(this.RedrawTimer_Tick);
            // 
            // testPcbLibCreationToolStripMenuItem
            // 
            this.testPcbLibCreationToolStripMenuItem.Name = "testPcbLibCreationToolStripMenuItem";
            this.testPcbLibCreationToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.testPcbLibCreationToolStripMenuItem.Text = "Test PcbLib creation";
            this.testPcbLibCreationToolStripMenuItem.Click += new System.EventHandler(this.testPcbLibCreationToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(990, 559);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip);
            this.Controls.Add(this.statusStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Library Viewer";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabComponents.ResumeLayout(false);
            this.tabPcbLib.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridPcbLibComponents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridPcbLibPrimitives)).EndInit();
            this.tabSchLib.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridSchLibComponents)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridSchLibPrimitives)).EndInit();
            this.panelPart.ResumeLayout(false);
            this.panelPart.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editPart)).EndInit();
            this.tabTree.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.DataGridView gridPcbLibComponents;
        private System.Windows.Forms.DataGridView gridPcbLibPrimitives;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exportFootprintToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exportPrimitiveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportImageToolStripMenuItem;
        private System.Windows.Forms.Panel pictureBox;
        private System.Windows.Forms.ToolStripStatusLabel statusLocation;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem centerToolStripMenuItem;
        private System.Windows.Forms.Timer redrawTimer;
        private System.Windows.Forms.TabControl tabComponents;
        private System.Windows.Forms.TabPage tabPcbLib;
        private System.Windows.Forms.TabPage tabSchLib;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.DataGridView gridSchLibComponents;
        private System.Windows.Forms.DataGridView gridSchLibPrimitives;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibPrimitivesType;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibPrimitivesName;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibPrimitivesXSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibPrimitivesYSize;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibPrimitivesLayer;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibComponentsName;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibComponentsPads;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridPcbLibComponentsPrimitives;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridSchLibComponentName;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridSchLibComponentDescription;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridSchLibPrimitiveDescriptor;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridSchLibPrimitiveName;
        private System.Windows.Forms.DataGridViewTextBoxColumn gridSchLibPrimitiveType;
        private System.Windows.Forms.ToolStripMenuItem zoom100ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem zoom50ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoom200ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem zoom400ToolStripMenuItem;
        private System.Windows.Forms.Panel panelPart;
        private System.Windows.Forms.NumericUpDown editPart;
        private System.Windows.Forms.Label labelPart;
        private System.Windows.Forms.Label labelPartTotal;
        private System.Windows.Forms.TabPage tabTree;
        private System.Windows.Forms.TreeView treeViewStructure;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem testPcbLibCreationToolStripMenuItem;
    }
}

