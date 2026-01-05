using System.Windows;

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
