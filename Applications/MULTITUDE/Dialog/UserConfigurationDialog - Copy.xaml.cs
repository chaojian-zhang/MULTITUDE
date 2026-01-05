using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE
{
    /// <summary>
    /// A dialog window that enables user to create select a list of locations as user home locations for management;
    /// And configure login information for notes
    /// </summary>
    public partial class UserConfigurationDialog : Window
    {
        public UserConfigurationDialog()
        {
            InitializeComponent();
        }

        // textChanged: // Validate inputs: any one of the user home locations should be empty or non-existing folders; Otherwise check whether other MULTITUDE profiles exist there and ask User whether we should manage them as well
    }
}
