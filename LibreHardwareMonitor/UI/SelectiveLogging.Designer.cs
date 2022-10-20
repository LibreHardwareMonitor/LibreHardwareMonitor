namespace LibreHardwareMonitor.UI
{
    partial class SelectiveLoggingForm
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
            this.treeView = new Aga.Controls.Tree.TreeViewAdv();
            this.sensor = new Aga.Controls.Tree.TreeColumn();
            this.logOutput = new Aga.Controls.Tree.TreeColumn();
            this.nodeImage = new Aga.Controls.Tree.NodeControls.NodeIcon();
            this.nodeTextBoxText = new Aga.Controls.Tree.NodeControls.NodeTextBox();
            this.nodeCheckBox = new Aga.Controls.Tree.NodeControls.NodeCheckBox();
            this.btnChangeStructure = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSelectiveLogging = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeView
            // 
            this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView.BackColor = System.Drawing.SystemColors.Window;
            this.treeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeView.Columns.Add(this.sensor);
            this.treeView.Columns.Add(this.logOutput);
            this.treeView.DefaultToolTipProvider = null;
            this.treeView.DragDropMarkColor = System.Drawing.Color.Black;
            this.treeView.FullRowSelect = true;
            this.treeView.GridLineStyle = Aga.Controls.Tree.GridLineStyle.Horizontal;
            this.treeView.LineColor = System.Drawing.SystemColors.ControlDark;
            this.treeView.Location = new System.Drawing.Point(10, 10);
            this.treeView.Margin = new System.Windows.Forms.Padding(0);
            this.treeView.Model = null;
            this.treeView.Name = "treeView";
            this.treeView.NodeControls.Add(this.nodeImage);
            this.treeView.NodeControls.Add(this.nodeTextBoxText);
            this.treeView.NodeControls.Add(this.nodeCheckBox);
            this.treeView.SelectedNode = null;
            this.treeView.ShowLines = false;
            this.treeView.Size = new System.Drawing.Size(710, 466);
            this.treeView.TabIndex = 0;
            this.treeView.Text = "treeView";
            this.treeView.UseColumns = true;
            this.treeView.Click += new System.EventHandler(this.treeView_Click);
            // 
            // sensor
            // 
            this.sensor.Header = "Sensor";
            this.sensor.SortOrder = System.Windows.Forms.SortOrder.None;
            this.sensor.TooltipText = null;
            this.sensor.Width = 250;
            // 
            // logOutput
            // 
            this.logOutput.Header = "Log values";
            this.logOutput.SortOrder = System.Windows.Forms.SortOrder.None;
            this.logOutput.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.logOutput.TooltipText = null;
            this.logOutput.Width = 100;
            // 
            // nodeImage
            // 
            this.nodeImage.DataPropertyName = "Image";
            this.nodeImage.LeftMargin = 1;
            this.nodeImage.ParentColumn = this.sensor;
            this.nodeImage.ScaleMode = Aga.Controls.Tree.ImageScaleMode.Fit;
            // 
            // nodeTextBoxText
            // 
            this.nodeTextBoxText.DataPropertyName = "Text";
            this.nodeTextBoxText.EditEnabled = true;
            this.nodeTextBoxText.IncrementalSearchEnabled = true;
            this.nodeTextBoxText.LeftMargin = 3;
            this.nodeTextBoxText.ParentColumn = this.sensor;
            this.nodeTextBoxText.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
            this.nodeTextBoxText.UseCompatibleTextRendering = true;
            // 
            // nodeCheckBox
            // 
            this.nodeCheckBox.DataPropertyName = "LogOutputTemp";
            this.nodeCheckBox.EditEnabled = true;
            this.nodeCheckBox.LeftMargin = 3;
            this.nodeCheckBox.ParentColumn = this.logOutput;
            // 
            // btnChangeStructure
            // 
            this.btnChangeStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnChangeStructure.ForeColor = System.Drawing.Color.Red;
            this.btnChangeStructure.Location = new System.Drawing.Point(10, 506);
            this.btnChangeStructure.Name = "btnChangeStructure";
            this.btnChangeStructure.Size = new System.Drawing.Size(121, 23);
            this.btnChangeStructure.TabIndex = 1;
            this.btnChangeStructure.Text = "Change log structure";
            this.btnChangeStructure.UseVisualStyleBackColor = true;
            this.btnChangeStructure.Click += new System.EventHandler(this.btnChangeStructure_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(645, 506);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnSelectiveLogging
            // 
            this.btnSelectiveLogging.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectiveLogging.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnSelectiveLogging.Location = new System.Drawing.Point(137, 506);
            this.btnSelectiveLogging.Name = "btnSelectiveLogging";
            this.btnSelectiveLogging.Size = new System.Drawing.Size(145, 23);
            this.btnSelectiveLogging.TabIndex = 4;
            this.btnSelectiveLogging.Text = "Enable selective logging";
            this.btnSelectiveLogging.UseVisualStyleBackColor = true;
            this.btnSelectiveLogging.Click += new System.EventHandler(this.btnSelectiveLogging_Click);
            // 
            // SelectiveLoggingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(729, 542);
            this.Controls.Add(this.btnSelectiveLogging);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnChangeStructure);
            this.Controls.Add(this.treeView);
            this.MinimumSize = new System.Drawing.Size(16, 39);
            this.Name = "SelectiveLoggingForm";
            this.Padding = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select sensors for logging";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SelectiveLoggingForm_FormClosed);
            this.ResumeLayout(false);

        }

        #endregion

        private Aga.Controls.Tree.TreeViewAdv treeView;
        private Aga.Controls.Tree.TreeColumn sensor;
        private Aga.Controls.Tree.TreeColumn logOutput;
        /*private Aga.Controls.Tree.TreeColumn min;
        private Aga.Controls.Tree.TreeColumn max;*/
        private Aga.Controls.Tree.NodeControls.NodeIcon nodeImage;
        private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxText;
        /*private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxValue;
        private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxMin;
        private Aga.Controls.Tree.NodeControls.NodeTextBox nodeTextBoxMax;
        */
        private Aga.Controls.Tree.NodeControls.NodeCheckBox nodeCheckBox;
        private System.Windows.Forms.Button btnChangeStructure;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnSelectiveLogging;
    }
}
