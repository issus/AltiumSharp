namespace LibraryViewer
{
    partial class PropertyViewer
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
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.comboBoxObjects = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // propertyGrid
            // 
            this.propertyGrid.DisabledItemForeColor = System.Drawing.SystemColors.ControlText;
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.HelpVisible = false;
            this.propertyGrid.Location = new System.Drawing.Point(0, 21);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propertyGrid.SelectedObject = this;
            this.propertyGrid.Size = new System.Drawing.Size(284, 430);
            this.propertyGrid.TabIndex = 0;
            this.propertyGrid.ToolbarVisible = false;
            this.propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid_PropertyValueChanged);
            // 
            // comboBoxObjects
            // 
            this.comboBoxObjects.Dock = System.Windows.Forms.DockStyle.Top;
            this.comboBoxObjects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxObjects.FormattingEnabled = true;
            this.comboBoxObjects.Location = new System.Drawing.Point(0, 0);
            this.comboBoxObjects.Name = "comboBoxObjects";
            this.comboBoxObjects.Size = new System.Drawing.Size(284, 21);
            this.comboBoxObjects.TabIndex = 1;
            this.comboBoxObjects.SelectedIndexChanged += new System.EventHandler(this.ComboBoxObjects_SelectedIndexChanged);
            // 
            // PropertyViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 451);
            this.Controls.Add(this.propertyGrid);
            this.Controls.Add(this.comboBoxObjects);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PropertyViewer";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Property Viewer";
            this.Load += new System.EventHandler(this.PropertyViewer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.ComboBox comboBoxObjects;
    }
}