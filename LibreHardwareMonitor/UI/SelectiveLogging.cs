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
using LibreHardwareMonitor.Hardware;

namespace LibreHardwareMonitor.UI
{
    public partial class SelectiveLoggingForm : Form
    {
        public bool StructureChanged { get; set; }
        public bool SelectiveLogging { get { return btnSelectiveLogging.Text == _textDisable; } }
        private readonly UserOption _selectiveLogging;
        private bool _structureChangeConfirmed = false;
        private string _textDisable = "Disable selective logging";
        private string _textEnable = "Enable selective logging";

        public SelectiveLoggingForm(ITreeModel treeModel, UserOption uoSelectinveLogging)
        {
            InitializeComponent();

            _selectiveLogging = uoSelectinveLogging;
            if (_selectiveLogging.Value)
                btnSelectiveLogging.Text = _textDisable;
            else
                btnSelectiveLogging.Text = _textEnable;
            btnChangeStructure.Enabled = false;
            nodeCheckBox.IsVisibleValueNeeded += NodeCheckBox_IsVisibleValueNeeded;
            nodeCheckBox.CheckStateChanged += NodeCheckBox_CheckStateChanged;
            treeView.Model = treeModel;
            treeView.FullUpdate();
        }

        private void NodeCheckBox_IsVisibleValueNeeded(object sender, NodeControlValueEventArgs e)
        {
            e.Value = e.Node.Tag is SensorNode;
        }

        private void btnChangeStructure_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void treeView_Click(object sender, EventArgs e)
        {
            
        }

        private void NodeCheckBox_CheckStateChanged(object sender, TreePathEventArgs e)
        {
            if (btnSelectiveLogging.Text == _textEnable)
            {
                StructureChanged = true;
                return;
            }

            if (!_structureChangeConfirmed)
                _structureChangeConfirmed = ConfirmStructureChange();
            if (!_structureChangeConfirmed)
            {
                if (e.Path.LastNode is SensorNode node)
                    node.LogOutputTemp = node.LogOutput;
                return;
            }

            StructureChanged = true;
            btnChangeStructure.Enabled = true;
        }

        private void btnSelectiveLogging_Click(object sender, EventArgs e)
        {
            if (_structureChangeConfirmed || ConfirmStructureChange())
            {
                _structureChangeConfirmed = true;
                StructureChanged = true;
                btnChangeStructure.Enabled = true;

                if (btnSelectiveLogging.Text == _textEnable)
                {
                    btnSelectiveLogging.Text = _textDisable;
                }
                else
                {
                    btnSelectiveLogging.Text = _textEnable;
                }
            }
        }

        private bool ConfirmStructureChange()
        {
            if (MessageBox.Show("This operation will result in log structure change. Continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                return true;
            }

            return false;
        }

        private void SelectiveLoggingForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}
