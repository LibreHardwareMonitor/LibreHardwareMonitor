using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LibreHardwareMonitor.UI
{
    /// <summary>
    /// This form handles the selection and de-selection of hardware.
    /// It uses the same mechanisms as the menuItems.
    /// </summary>
    public partial class SelectHardwareForm : Form
    {
        /// <summary>
        /// Initializes the ListBox with the Texts of the
        /// menuItems included in the userOptions.
        /// </summary>
        /// <param name="userOptions">Relevant UserOptions of the MainForm.</param>
        public SelectHardwareForm(UserOption[] userOptions)
        {
            InitializeComponent();

            foreach (var userOption in userOptions)
            {
                hardwareListBox.Items.Add(userOption.getItemText());
                if (userOption.Value)
                    hardwareListBox.SelectedItems.Add(userOption.getItemText());
            }
        }

        /// <summary>
        /// Access the SelectedItems of the ListBox
        /// </summary>
        /// <returns>An IEnumerable with the Texts of the selected Items.</returns>
        public IEnumerable<string> GetSelectedOptions()
        {
            return hardwareListBox.SelectedItems.OfType<string>();
        }
    }
}
