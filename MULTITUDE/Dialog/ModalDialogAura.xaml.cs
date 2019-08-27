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

namespace MULTITUDE.Dialog
{
    /// <summary>
    /// Interaction logic for ModalDialogAura.xaml
    /// </summary>
    public partial class ModalDialogAura : Window
    {
        public ModalDialogAura(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }
    }
}
