// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace LibreHardwareMonitor.UI
{
    partial class AccessControlForm
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
      this.accessControlInput = new System.Windows.Forms.TextBox();
      this.AccessControlCancelButton = new System.Windows.Forms.Button();
      this.AccessControlOKButton = new System.Windows.Forms.Button();
      this.label1 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.linkLabel1 = new System.Windows.Forms.LinkLabel();
      this.SuspendLayout();
      // 
      // accessControlInput
      // 
      this.accessControlInput.Location = new System.Drawing.Point(219, 9);
      this.accessControlInput.Name = "accessControlInput";
      this.accessControlInput.Size = new System.Drawing.Size(336, 20);
      this.accessControlInput.TabIndex = 0;
      this.accessControlInput.Text = "*";
      // 
      // AccessControlCancelButton
      // 
      this.AccessControlCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.AccessControlCancelButton.Location = new System.Drawing.Point(202, 62);
      this.AccessControlCancelButton.Name = "AccessControlCancelButton";
      this.AccessControlCancelButton.Size = new System.Drawing.Size(75, 23);
      this.AccessControlCancelButton.TabIndex = 3;
      this.AccessControlCancelButton.Text = "Cancel";
      this.AccessControlCancelButton.UseVisualStyleBackColor = true;
      this.AccessControlCancelButton.Click += new System.EventHandler(this.AccessControlCancelButton_Click);
      // 
      // AccessControlOKButton
      // 
      this.AccessControlOKButton.Location = new System.Drawing.Point(283, 62);
      this.AccessControlOKButton.Name = "AccessControlOKButton";
      this.AccessControlOKButton.Size = new System.Drawing.Size(75, 23);
      this.AccessControlOKButton.TabIndex = 2;
      this.AccessControlOKButton.Text = "OK";
      this.AccessControlOKButton.UseVisualStyleBackColor = true;
      this.AccessControlOKButton.Click += new System.EventHandler(this.AccessControlOKButton_Click);
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 12);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(201, 13);
      this.label1.TabIndex = 4;
      this.label1.Text = "Set header \"Access-Control-Allow-Origin\"";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(12, 36);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(91, 13);
      this.label2.TabIndex = 5;
      this.label2.Text = "More information: ";
      // 
      // linkLabel1
      // 
      this.linkLabel1.AutoSize = true;
      this.linkLabel1.Location = new System.Drawing.Point(109, 36);
      this.linkLabel1.Name = "linkLabel1";
      this.linkLabel1.Size = new System.Drawing.Size(446, 13);
      this.linkLabel1.TabIndex = 6;
      this.linkLabel1.TabStop = true;
      this.linkLabel1.Text = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Access-Control-Allow-Or" +
    "igin";
      this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
      // 
      // AccessControlForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(572, 89);
      this.Controls.Add(this.linkLabel1);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.AccessControlCancelButton);
      this.Controls.Add(this.AccessControlOKButton);
      this.Controls.Add(this.accessControlInput);
      this.Name = "AccessControlForm";
      this.Text = "Set Access Control Allow Origin";
      this.Load += new System.EventHandler(this.AccessControl_Load);
      this.ResumeLayout(false);
      this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox accessControlInput;
        private System.Windows.Forms.Button AccessControlCancelButton;
        private System.Windows.Forms.Button AccessControlOKButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel linkLabel1;
    }
}
