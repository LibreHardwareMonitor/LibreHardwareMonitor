using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aga.Controls.Tree;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Utilities;

namespace LibreHardwareMonitor.UI
{
    public partial class SqlConnectionForm : Form
    {
        private string _leaveBlank = "(leave blank to use file DSN)";
        private string _dateTimeField = "eventDateTime";
        private string _connString = "";
        private TreeViewAdv _treeView;
        private UserOption _selectiveLogging;
        public string ConnectionString { get { return _connString; } }
        public SqlConnectionForm(TreeViewAdv treeView, UserOption uoSelectinveLogging)
        {
            InitializeComponent();

            tbUsername.Text = _leaveBlank;
            tbUsername.ForeColor = SystemColors.InactiveCaption;

            tbPassword.Text = _leaveBlank;
            tbPassword.ForeColor = SystemColors.InactiveCaption;

            DisableControls();
            _treeView = treeView;
            _selectiveLogging = uoSelectinveLogging;

            //for testing only
            //tbDsnPath.Text = "C:\\Users\\romko\\Documents\\Coding\\VS 2022\\LibreHwMonitor\\koombaal.dsn";

        }

        private void DisableControls()
        {
            btnCheckDB.Enabled = false;
            btnEnableSqlLogging.Enabled = false;
            btnCreateTable.Enabled = false;
            btnAddMissingColumns.Enabled = false;
            btnDropObsoleteColumns.Enabled = false;
            btnExecute.Enabled = false;
        }

        private void EnableControls()
        {
            btnCheckDB.Enabled = true;
            btnEnableSqlLogging.Enabled = true;
            btnCreateTable.Enabled = true;
            btnAddMissingColumns.Enabled = true;
            btnDropObsoleteColumns.Enabled = true;
            btnExecute.Enabled = true;
        }

        private void btnTestConnection_Click(object sender, EventArgs e)
        {
            string credentials = "";

            if (tbUsername.Text.Trim() != "" && tbUsername.Text.Trim() != _leaveBlank)
                credentials = "Uid=" + tbUsername.Text + ";";

            if (tbPassword.Text.Trim() != "" && tbPassword.Text.Trim() != _leaveBlank)
                credentials += "Pwd=" + tbPassword.Text + ";";

            System.Data.Odbc.OdbcConnection conn = new System.Data.Odbc.OdbcConnection();
            conn.ConnectionString = "FILEDSN=" + tbDsnPath.Text + ";" + (credentials != "" ? credentials : "");

            lbDbColumns.Items.Clear();
            lbSensors.Items.Clear();

            try
            {
                conn.Open();
                conn.Close();

                _connString = conn.ConnectionString;
                MessageBox.Show("Connection successuful!", "Test database connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _connString = "";
                MessageBox.Show("Connection failed! \n\n" + ex.Message, "Test database connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {

            }

            if (_connString != "")
                EnableControls();
            else
                DisableControls();
        }

        private void btnBrowseDSN_Click(object sender, EventArgs e)
        {
            odBrowseDSN.InitialDirectory = Environment.CurrentDirectory;

            if (odBrowseDSN.ShowDialog() == DialogResult.OK)
            {
                tbDsnPath.Text = odBrowseDSN.FileName;
                DisableControls();
            }
        }

        private void tbUsername_Click(object sender, EventArgs e)
        {
            if (sender is TextBox tb && tb.Text == _leaveBlank)
            {
                tb.Text = "";
                tb.ForeColor = SystemColors.ControlText;
            }
        }

        private void btnCheckDB_Click(object sender, EventArgs e)
        {
            System.Data.Odbc.OdbcConnection conn = new System.Data.Odbc.OdbcConnection();
            conn.ConnectionString = _connString;

            lbDbColumns.Items.Clear();
            lbSensors.Items.Clear();

            DataTable dataTable = new DataTable();

            try
            {
                conn.Open();

                OdbcCommand cmd = conn.CreateCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT * FROM sensor_data LIMIT 1";

                OdbcDataAdapter adapter = new OdbcDataAdapter(cmd);
                adapter.Fill(dataTable);

                foreach (DataColumn column in dataTable.Columns)
                {
                    lbDbColumns.Items.Add(column.ColumnName);
                }

                adapter.Dispose();
                conn.Close();

                _connString = conn.ConnectionString;
                //MessageBox.Show("Connection successuful!", "Test database connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database operation failed! \n\n" + ex.Message, "Check database structure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {

            }

            foreach (TreeNodeAdv node in _treeView.AllNodes)
            {
                if (node.Tag is SensorNode sensorNode)
                    if (!_selectiveLogging.Value || sensorNode.LogOutput)
                    {
                        //lbSensors.Items.Add(ColumnNameFromIdentifier(sensorNode.Sensor.Identifier));
                        lbSensors.Items.Add(sensorNode.Sensor.Identifier.ToString());
                    }
            }
        }
  
        private void lbSensors_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            Graphics g = e.Graphics;

            Color backColor = SystemColors.Window;
            
            if (lbDbColumns.Items.Contains(SqlLogger.ColumnNameFromIdentifier((string)lbSensors.Items[e.Index])))
                backColor = Color.LimeGreen;
            else
                backColor = Color.Orange;

            g.FillRectangle(new SolidBrush(backColor), e.Bounds);

            g.DrawString(lbSensors.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), new PointF(e.Bounds.X, e.Bounds.Y));

            e.DrawFocusRectangle();
        }

        private void btnAddMissingColumns_Click(object sender, EventArgs e)
        {
            string sqlStatement = "ALTER TABLE sensor_data ";

            if (!lbDbColumns.Items.Contains("eventDateTime"))
                sqlStatement += " ADD eventDateTime datetime PRIMARY KEY,";

            foreach (string identifier in lbSensors.Items)
            {
                if (!lbDbColumns.Items.Contains(SqlLogger.ColumnNameFromIdentifier(identifier)))
                    sqlStatement += " ADD " + SqlLogger.ColumnNameFromIdentifier(identifier) + " float,";
            }

            sqlStatement = sqlStatement.Substring(0, sqlStatement.Length - 1);

            tbSqlStatement.Text = sqlStatement;
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Execute SQL statement?", "Modify database table", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            System.Data.Odbc.OdbcConnection conn = new System.Data.Odbc.OdbcConnection();
            conn.ConnectionString = _connString;

            try
            {
                conn.Open();

                OdbcCommand cmd = conn.CreateCommand();
                cmd.Connection = conn;
                cmd.CommandText = tbSqlStatement.Text;

                int queryResult;
                queryResult = cmd.ExecuteNonQuery();
 
                conn.Close();

                MessageBox.Show("Query executed successufully! \n\n Number of rows affected: " + queryResult, "SQL query", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Query execution failed! \n\n" + ex.Message, "SQL query", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCheckDB_Click(sender, e);
            }
        }

        private void lbDbColumns_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            Graphics g = e.Graphics;

            Color backColor = SystemColors.Window;

            List<string> tempSensors = new List<string>();
            foreach (string sensor in lbSensors.Items)
                tempSensors.Add(SqlLogger.ColumnNameFromIdentifier(sensor));

            if (tempSensors.Contains(lbDbColumns.Items[e.Index]) || lbDbColumns.Items[e.Index].ToString() == _dateTimeField)
                backColor = Color.LimeGreen;
            else
                backColor = Color.Orange;

            g.FillRectangle(new SolidBrush(backColor), e.Bounds);

            g.DrawString(lbDbColumns.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), new PointF(e.Bounds.X, e.Bounds.Y));

            e.DrawFocusRectangle();
        }

        private void btnDropObsoleteColumns_Click(object sender, EventArgs e)
        {
            string sqlStatement = "ALTER TABLE sensor_data ";

            List<string> tempSensors = new List<string>();
            foreach (string sensor in lbSensors.Items)
                tempSensors.Add(SqlLogger.ColumnNameFromIdentifier(sensor));

            foreach (string identifier in lbDbColumns.Items)
            {
                if (!tempSensors.Contains(identifier) && identifier != _dateTimeField)
                    sqlStatement += " DROP " + identifier + ",";
            }

            sqlStatement = sqlStatement.Substring(0, sqlStatement.Length - 1);

            tbSqlStatement.Text = sqlStatement;
        }

        private void btnCreateTable_Click(object sender, EventArgs e)
        {
            string sqlStatement = "CREATE OR REPLACE TABLE sensor_data (eventDateTime DATETIME PRIMARY KEY, ";

            btnCheckDB_Click(sender, e);

            foreach (string sensor in lbSensors.Items)
                sqlStatement += SqlLogger.ColumnNameFromIdentifier(sensor) + " float,";

            sqlStatement = sqlStatement.Substring(0, sqlStatement.Length - 1);
            sqlStatement += ")";

            tbSqlStatement.Text = sqlStatement;
        }

        private void tbUsername_TextChanged(object sender, EventArgs e)
        {
            DisableControls();
        }
    }
}
