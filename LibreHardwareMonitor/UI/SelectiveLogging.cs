using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aga.Controls.Tree;

namespace LibreHardwareMonitor.UI
{
    public partial class SelectiveLogging : Form
    {
        public SelectiveLogging(ITreeModel treeModel)
        {
            InitializeComponent();

            treeView.Model = treeModel;
        }
    }
}
