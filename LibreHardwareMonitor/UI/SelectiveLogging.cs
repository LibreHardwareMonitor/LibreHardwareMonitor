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
using Aga.Controls.Tree.NodeControls;

namespace LibreHardwareMonitor.UI
{
    public partial class SelectiveLogging : Form
    {
        public SelectiveLogging(ITreeModel treeModel)
        {
            InitializeComponent();

            nodeCheckBox.IsVisibleValueNeeded += NodeCheckBox_IsVisibleValueNeeded;
            treeView.Model = treeModel;
            treeView.FullUpdate();
        }

        private void NodeCheckBox_IsVisibleValueNeeded(object sender, NodeControlValueEventArgs e)
        {
            e.Value = e.Node.Tag is SensorNode;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
