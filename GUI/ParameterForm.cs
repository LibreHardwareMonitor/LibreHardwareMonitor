// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.GUI
{
    public partial class ParameterForm : Form
    {
        private IReadOnlyList<IParameter> _parameters;
        private BindingList<ParameterRow> _parameterRows;

        public ParameterForm()
        {
            InitializeComponent();
        }

        public IReadOnlyList<IParameter> Parameters
        {
            get
            {
                return _parameters;
            }
            set
            {
                _parameters = value;
                _parameterRows = new BindingList<ParameterRow>();

                foreach (IParameter parameter in _parameters)
                    _parameterRows.Add(new ParameterRow(parameter));

                bindingSource.DataSource = _parameterRows;
            }
        }

        private class ParameterRow : INotifyPropertyChanged
        {
            public readonly IParameter Parameter;

            public event PropertyChangedEventHandler PropertyChanged;

            public ParameterRow(IParameter parameter)
            {
                Parameter = parameter;
                Value = parameter.Value;
                Default = parameter.IsDefault;
            }

            public float Value { get; }

            public bool Default { get; }
        }

        private void DataGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _parameters.Count)
                descriptionLabel.Text = _parameters[e.RowIndex].Description;
            else
                descriptionLabel.Text = "";
        }

        private void DataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 2 && !float.TryParse(e.FormattedValue.ToString(), out float _))
            {
                dataGridView.Rows[e.RowIndex].Cells[0].ErrorText = "Invalid value";
                e.Cancel = true;
            }
        }

        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView.Rows[e.RowIndex].Cells[0].ErrorText = "";
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            foreach (ParameterRow row in _parameterRows)
            {
                if (row.Default)
                    row.Parameter.IsDefault = true;
                else
                    row.Parameter.Value = row.Value;
            }
        }

        private void DataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell is DataGridViewCheckBoxCell || dataGridView.CurrentCell is DataGridViewComboBoxCell)
                dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }
}
