using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LibreHardwareMonitor.UI
{
    public class ChartJSChart
    {
        public List<KeyValuePair<string, Color>> Colors { set; get; }

        public class _DataSets
        {
            public string Name { get; set; }
            public List<Color> BackgroundColor { get; set; }
            public List<Color> BorderColor { get; set; }
        }

        public List<_DataSets> DataSets { get; set; }
        public JObject Options { get; set; }
    }
    public class Dashboard
    {
        private bool _isready;
        public List<ChartJSChart> Charts { get; set; }
    }
}
