namespace LibreHardwareMonitor.UI
{
    partial class SelectHardwareForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectHardwareForm));
            this.selectHardwareOkButton = new System.Windows.Forms.Button();
            this.selectHardwareCancelButton = new System.Windows.Forms.Button();
            this.hardwareListBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // selectHardwareOkButton
            // 
            this.selectHardwareOkButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.selectHardwareOkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.selectHardwareOkButton.Location = new System.Drawing.Point(94, 176);
            this.selectHardwareOkButton.Name = "selectHardwareOkButton";
            this.selectHardwareOkButton.Size = new System.Drawing.Size(75, 23);
            this.selectHardwareOkButton.TabIndex = 3;
            this.selectHardwareOkButton.Text = "OK";
            this.selectHardwareOkButton.UseVisualStyleBackColor = true;
            // 
            // selectHardwareCancelButton
            // 
            this.selectHardwareCancelButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.selectHardwareCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.selectHardwareCancelButton.Location = new System.Drawing.Point(13, 176);
            this.selectHardwareCancelButton.Name = "selectHardwareCancelButton";
            this.selectHardwareCancelButton.Size = new System.Drawing.Size(75, 23);
            this.selectHardwareCancelButton.TabIndex = 2;
            this.selectHardwareCancelButton.Text = "Cancel";
            this.selectHardwareCancelButton.UseVisualStyleBackColor = true;
            // 
            // hardwareListBox
            // 
            this.hardwareListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hardwareListBox.FormattingEnabled = true;
            this.hardwareListBox.Location = new System.Drawing.Point(12, 12);
            this.hardwareListBox.Name = "hardwareListBox";
            this.hardwareListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.hardwareListBox.Size = new System.Drawing.Size(160, 147);
            this.hardwareListBox.TabIndex = 1;
            // 
            // SelectHardwareForm
            // 
            this.AcceptButton = this.selectHardwareOkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.selectHardwareCancelButton;
            this.ClientSize = new System.Drawing.Size(184, 211);
            this.Controls.Add(this.hardwareListBox);
            this.Controls.Add(this.selectHardwareOkButton);
            this.Controls.Add(this.selectHardwareCancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectHardwareForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Hardware";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button selectHardwareOkButton;
        private System.Windows.Forms.Button selectHardwareCancelButton;
        private System.Windows.Forms.ListBox hardwareListBox;
    }
}
