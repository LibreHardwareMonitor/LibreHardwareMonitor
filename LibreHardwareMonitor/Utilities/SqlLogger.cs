using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Wmi;

namespace LibreHardwareMonitor.Utilities
{
    public class SqlLogger : ILogger
    {
        public IComputer Computer { get; private set; }

        private string _connectionsString;
        private string[] _identifiers;
        private ISensor[] _sensors;
        public DateTime LastLoggedTime { get; private set; }

        public static string ColumnNameFromIdentifier(string identifier)
        {
            string value = "s";
            foreach (char c in identifier)
                if (char.IsLetterOrDigit(c))
                    value += c;
                else
                    value += "_";
            return value;
        }

        public SqlLogger(IComputer computer, string connectionString)
        {
            Computer = computer;
            _connectionsString = connectionString;

            //Computer.HardwareAdded += HardwareAdded;
            //Computer.HardwareRemoved += HardwareRemoved;
        }

        public TimeSpan LoggingInterval { get; set; }

        public void Log()
        {
            Log(false, null);
        }

        public void Log(bool selectiveLogging = false, List<string> Identifiers = null)
        {
            DateTime now = DateTime.Now;

            if (LastLoggedTime + LoggingInterval - new TimeSpan(5000000) > now)
                return;

            IList<ISensor> list = new List<ISensor>();
            SensorVisitor visitor = new SensorVisitor(sensor =>
            {
                list.Add(sensor);
            });
            visitor.VisitComputer(Computer);
            _sensors = list.ToArray();
            _identifiers = _sensors.Select(s => s.Identifier.ToString()).ToArray();

            System.Data.Odbc.OdbcConnection conn = new System.Data.Odbc.OdbcConnection(_connectionsString);
            try
            {
                OdbcCommand cmd = conn.CreateCommand();
                cmd.Connection = conn;
                cmd.CommandText = "INSERT INTO sensor_data SET eventDateTime='" + now.ToString("yyyy-MM-dd HH:mm:ss.ffffff") + "',";

                for (int i = 0; i < _identifiers.Length; i++)
                {
                    string id = _identifiers[i];
                    if (!selectiveLogging || Identifiers.Contains(id))
                    {
                        float? value = _sensors[i].Value;
                        if (value.HasValue)
                            cmd.CommandText += SqlLogger.ColumnNameFromIdentifier(id) + "=" + value.Value.ToString("R", CultureInfo.InvariantCulture) + ","; 
                    }
                }

                cmd.CommandText = cmd.CommandText.Substring(0, cmd.CommandText.Length - 1);

                conn.Open();
                int queryResult = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Logging to database failed: \n\n" + ex.Message, "Log to database", MessageBoxButtons.OK);
            }
            finally
            {
                conn.Close();
            }

            LastLoggedTime = now;
        }

        public void UpdateStructure(bool selectiveLogging = false, List<string> Identifiers = null)
        {
            
        }
    }
}
