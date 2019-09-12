// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace OpenHardwareMonitor.GUI
{
    public partial class CrashForm : Form
    {
        private Exception exception;

        public CrashForm()
        {
            InitializeComponent();
        }

        public Exception Exception
        {
            get { return exception; }
            set
            {
                exception = value;
                StringBuilder s = new StringBuilder();
                Version version = typeof(CrashForm).Assembly.GetName().Version;
                s.Append("Version: "); s.AppendLine(version.ToString());
                s.AppendLine();
                s.AppendLine(exception.ToString());
                s.AppendLine();
                if (exception.InnerException != null)
                {
                    s.AppendLine(exception.InnerException.ToString());
                    s.AppendLine();
                }
                s.Append("Common Language Runtime: ");
                s.AppendLine(Environment.Version.ToString());
                s.Append("Operating System: ");
                s.AppendLine(Environment.OSVersion.ToString());
                s.Append("Process Type: ");
                s.AppendLine(IntPtr.Size == 4 ? "32-Bit" : "64-Bit");
                reportTextBox.Text = s.ToString();
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            try
            {
                Version version = typeof(CrashForm).Assembly.GetName().Version;
                WebRequest request = WebRequest.Create("http://openhardwaremonitor.org/report.php");
                request.Method = "POST";
                request.Timeout = 5000;
                request.ContentType = "application/x-www-form-urlencoded";

                string report =
                  "type=crash&" +
                  "version=" + Uri.EscapeDataString(version.ToString()) + "&" +
                  "report=" + Uri.EscapeDataString(reportTextBox.Text) + "&" +
                  "comment=" + Uri.EscapeDataString(commentTextBox.Text) + "&" +
                  "email=" + Uri.EscapeDataString(emailTextBox.Text);
                byte[] byteArray = Encoding.UTF8.GetBytes(report);
                request.ContentLength = byteArray.Length;

                try
                {
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse response = request.GetResponse();
                    dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();
                    reader.Close();
                    dataStream.Close();
                    response.Close();

                    Close();
                }
                catch (WebException)
                {
                    MessageBox.Show("Sending the crash report failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
            }
        }
    }
}
