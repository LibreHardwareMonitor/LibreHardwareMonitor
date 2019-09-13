﻿// Mozilla Public License 2.0
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors
// All Rights Reserved

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
            public IParameter Parameter;
            private float _value;
            public bool IsDefault;
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(String propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public ParameterRow(IParameter parameter)
            {
                Parameter = parameter;
                _value = parameter.Value;
                IsDefault = parameter.IsDefault;
            }

            public string Name
            {
                get { return Parameter.Name; }
            }

            public float Value
            {
                get { return _value; }
                set
                {
                    IsDefault = false;
                    _value = value;
                    NotifyPropertyChanged("Default");
                    NotifyPropertyChanged("Value");
                }
            }

            public bool Default
            {
                get { return IsDefault; }
                set
                {
                    IsDefault = value;
                    if (value)
                        _value = Parameter.DefaultValue;
                    NotifyPropertyChanged("Default");
                    NotifyPropertyChanged("Value");
                }
            }
        }

        private void DataGridView_RowEnter(object sender,
          DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _parameters.Count)
                descriptionLabel.Text = _parameters[e.RowIndex].Description;
            else
                descriptionLabel.Text = "";
        }

        private void DataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            float value;
            if (e.ColumnIndex == 2 &&
              !float.TryParse(e.FormattedValue.ToString(), out value))
            {
                dataGridView.Rows[e.RowIndex].Cells[0].ErrorText =
                  "Invalid value";
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
                {
                    row.Parameter.IsDefault = true;
                }
                else
                {
                    row.Parameter.Value = row.Value;
                }
            }
        }

        private void DataGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView.CurrentCell is DataGridViewCheckBoxCell || dataGridView.CurrentCell is DataGridViewComboBoxCell)
            {
                dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
