using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI
{
    public partial class AuthForm : Form
    {
        private readonly MainForm _parent;
        public AuthForm(MainForm m)
        {
            InitializeComponent();
            _parent = m;
        }

        private void AuthForm_Load(object sender, EventArgs e)
        {
            httpAuthUsernameTextBox.Enabled = httpAuthPasswordTextBox.Enabled = enableHTTPAuthCheckBox.Checked = _parent.Server.AuthEnabled;
            httpAuthUsernameTextBox.Text = _parent.Server.Username;
        }

        private void httpAuthCancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void httpAuthOkButton_Click(object sender, EventArgs e)
        {
            _parent.Server.Username = httpAuthUsernameTextBox.Text;
            _parent.Server.Password = httpAuthPasswordTextBox.Text;
            _parent.Server.AuthEnabled = enableHTTPAuthCheckBox.Checked;
            _parent.AuthWebServerMenuItemChecked = _parent.Server.AuthEnabled;
            Close();
        }

        private void enableHTTPAuthCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            httpAuthUsernameTextBox.Enabled = httpAuthPasswordTextBox.Enabled = enableHTTPAuthCheckBox.Checked;
        }
    }
}
