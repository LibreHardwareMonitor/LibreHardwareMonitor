namespace LibreHardwareMonitor.UI
{
    partial class SqlConnectionForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlConnectionForm));
            this.odBrowseDSN = new System.Windows.Forms.OpenFileDialog();
            this.tbSqlStatement = new System.Windows.Forms.TextBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.btnEnableSqlLogging = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbUsername = new System.Windows.Forms.TextBox();
            this.btnBrowseDSN = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tbDsnPath = new System.Windows.Forms.TextBox();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lbDbColumns = new System.Windows.Forms.ListBox();
            this.lbSensors = new System.Windows.Forms.ListBox();
            this.btnCreateTable = new System.Windows.Forms.Button();
            this.btnDropObsoleteColumns = new System.Windows.Forms.Button();
            this.btnAddMissingColumns = new System.Windows.Forms.Button();
            this.btnCheckDB = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // odBrowseDSN
            // 
            this.odBrowseDSN.DefaultExt = "*.dsn";
            this.odBrowseDSN.FileName = "*.dsn";
            this.odBrowseDSN.Filter = "DSN files|*.dsn";
            this.odBrowseDSN.Title = "Choose existing DSN file";
            // 
            // tbSqlStatement
            // 
            this.tbSqlStatement.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSqlStatement.Location = new System.Drawing.Point(477, 31);
            this.tbSqlStatement.Multiline = true;
            this.tbSqlStatement.Name = "tbSqlStatement";
            this.tbSqlStatement.Size = new System.Drawing.Size(451, 340);
            this.tbSqlStatement.TabIndex = 14;
            // 
            // btnExecute
            // 
            this.btnExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExecute.Location = new System.Drawing.Point(778, 378);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(150, 23);
            this.btnExecute.TabIndex = 17;
            this.btnExecute.Text = "Execute SQL statement";
            this.btnExecute.UseVisualStyleBackColor = true;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // btnEnableSqlLogging
            // 
            this.btnEnableSqlLogging.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnEnableSqlLogging.Location = new System.Drawing.Point(233, 378);
            this.btnEnableSqlLogging.Name = "btnEnableSqlLogging";
            this.btnEnableSqlLogging.Size = new System.Drawing.Size(150, 23);
            this.btnEnableSqlLogging.TabIndex = 18;
            this.btnEnableSqlLogging.Text = "Enable database logging";
            this.btnEnableSqlLogging.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(389, 378);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 19;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.tbPassword);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.tbUsername);
            this.groupBox1.Controls.Add(this.btnBrowseDSN);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.tbDsnPath);
            this.groupBox1.Controls.Add(this.btnTestConnection);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(452, 109);
            this.groupBox1.TabIndex = 20;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Database connection properties";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(228, 53);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Password";
            // 
            // tbPassword
            // 
            this.tbPassword.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbPassword.Location = new System.Drawing.Point(296, 50);
            this.tbPassword.Name = "tbPassword";
            this.tbPassword.Size = new System.Drawing.Size(147, 20);
            this.tbPassword.TabIndex = 15;
            this.tbPassword.Text = "(leave blank to use file DSN)";
            this.tbPassword.Click += new System.EventHandler(this.tbUsername_Click);
            this.tbPassword.TextChanged += new System.EventHandler(this.tbUsername_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 14;
            this.label2.Text = "Username";
            // 
            // tbUsername
            // 
            this.tbUsername.ForeColor = System.Drawing.SystemColors.InactiveCaption;
            this.tbUsername.Location = new System.Drawing.Point(64, 50);
            this.tbUsername.Name = "tbUsername";
            this.tbUsername.Size = new System.Drawing.Size(147, 20);
            this.tbUsername.TabIndex = 13;
            this.tbUsername.Text = "(leave blank to use file DSN)";
            this.tbUsername.Click += new System.EventHandler(this.tbUsername_Click);
            this.tbUsername.TextChanged += new System.EventHandler(this.tbUsername_TextChanged);
            // 
            // btnBrowseDSN
            // 
            this.btnBrowseDSN.Location = new System.Drawing.Point(368, 19);
            this.btnBrowseDSN.Name = "btnBrowseDSN";
            this.btnBrowseDSN.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseDSN.TabIndex = 12;
            this.btnBrowseDSN.Text = "Browse...";
            this.btnBrowseDSN.UseVisualStyleBackColor = true;
            this.btnBrowseDSN.Click += new System.EventHandler(this.btnBrowseDSN_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "File DSN";
            // 
            // tbDsnPath
            // 
            this.tbDsnPath.Location = new System.Drawing.Point(64, 21);
            this.tbDsnPath.Name = "tbDsnPath";
            this.tbDsnPath.Size = new System.Drawing.Size(298, 20);
            this.tbDsnPath.TabIndex = 10;
            this.tbDsnPath.TextChanged += new System.EventHandler(this.tbUsername_TextChanged);
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(368, 76);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(75, 23);
            this.btnTestConnection.TabIndex = 9;
            this.btnTestConnection.Text = "Test";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.lbDbColumns);
            this.groupBox2.Controls.Add(this.lbSensors);
            this.groupBox2.Controls.Add(this.btnCreateTable);
            this.groupBox2.Controls.Add(this.btnDropObsoleteColumns);
            this.groupBox2.Controls.Add(this.btnAddMissingColumns);
            this.groupBox2.Controls.Add(this.btnCheckDB);
            this.groupBox2.Location = new System.Drawing.Point(12, 127);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(452, 245);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Log table";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(221, 57);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 24;
            this.label5.Text = "Database columns:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 13);
            this.label4.TabIndex = 23;
            this.label4.Text = "Sensors to log:";
            // 
            // lbDbColumns
            // 
            this.lbDbColumns.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbDbColumns.FormattingEnabled = true;
            this.lbDbColumns.Location = new System.Drawing.Point(224, 73);
            this.lbDbColumns.Name = "lbDbColumns";
            this.lbDbColumns.Size = new System.Drawing.Size(219, 160);
            this.lbDbColumns.Sorted = true;
            this.lbDbColumns.TabIndex = 22;
            this.lbDbColumns.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbDbColumns_DrawItem);
            // 
            // lbSensors
            // 
            this.lbSensors.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbSensors.FormattingEnabled = true;
            this.lbSensors.Location = new System.Drawing.Point(9, 73);
            this.lbSensors.Name = "lbSensors";
            this.lbSensors.Size = new System.Drawing.Size(209, 160);
            this.lbSensors.Sorted = true;
            this.lbSensors.TabIndex = 21;
            this.lbSensors.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbSensors_DrawItem);
            // 
            // btnCreateTable
            // 
            this.btnCreateTable.Location = new System.Drawing.Point(6, 19);
            this.btnCreateTable.Name = "btnCreateTable";
            this.btnCreateTable.Size = new System.Drawing.Size(77, 23);
            this.btnCreateTable.TabIndex = 20;
            this.btnCreateTable.Text = "Create";
            this.btnCreateTable.UseVisualStyleBackColor = true;
            this.btnCreateTable.Click += new System.EventHandler(this.btnCreateTable_Click);
            // 
            // btnDropObsoleteColumns
            // 
            this.btnDropObsoleteColumns.Location = new System.Drawing.Point(317, 19);
            this.btnDropObsoleteColumns.Name = "btnDropObsoleteColumns";
            this.btnDropObsoleteColumns.Size = new System.Drawing.Size(126, 23);
            this.btnDropObsoleteColumns.TabIndex = 19;
            this.btnDropObsoleteColumns.Text = "Drop obsolete columns";
            this.btnDropObsoleteColumns.UseVisualStyleBackColor = true;
            this.btnDropObsoleteColumns.Click += new System.EventHandler(this.btnDropObsoleteColumns_Click);
            // 
            // btnAddMissingColumns
            // 
            this.btnAddMissingColumns.Location = new System.Drawing.Point(196, 19);
            this.btnAddMissingColumns.Name = "btnAddMissingColumns";
            this.btnAddMissingColumns.Size = new System.Drawing.Size(115, 23);
            this.btnAddMissingColumns.TabIndex = 18;
            this.btnAddMissingColumns.Text = "Add missing columns";
            this.btnAddMissingColumns.UseVisualStyleBackColor = true;
            this.btnAddMissingColumns.Click += new System.EventHandler(this.btnAddMissingColumns_Click);
            // 
            // btnCheckDB
            // 
            this.btnCheckDB.Location = new System.Drawing.Point(89, 19);
            this.btnCheckDB.Name = "btnCheckDB";
            this.btnCheckDB.Size = new System.Drawing.Size(101, 23);
            this.btnCheckDB.TabIndex = 17;
            this.btnCheckDB.Text = "Check structure";
            this.btnCheckDB.UseVisualStyleBackColor = true;
            this.btnCheckDB.Click += new System.EventHandler(this.btnCheckDB_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(474, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(77, 13);
            this.label6.TabIndex = 22;
            this.label6.Text = "SQL statement";
            // 
            // SqlConnectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(941, 410);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnEnableSqlLogging);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.tbSqlStatement);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(957, 449);
            this.Name = "SqlConnectionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Connect to database...";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog odBrowseDSN;
        private System.Windows.Forms.TextBox tbSqlStatement;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.Button btnEnableSqlLogging;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbPassword;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbUsername;
        private System.Windows.Forms.Button btnBrowseDSN;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbDsnPath;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnCreateTable;
        private System.Windows.Forms.Button btnDropObsoleteColumns;
        private System.Windows.Forms.Button btnAddMissingColumns;
        private System.Windows.Forms.Button btnCheckDB;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox lbDbColumns;
        private System.Windows.Forms.ListBox lbSensors;
        private System.Windows.Forms.Label label6;
    }
}
