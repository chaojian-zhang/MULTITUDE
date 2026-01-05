using MULTITUDE.Canvas;
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

namespace MULTITUDE.CustomControl.CanvasSpaceWindow
{
    /// <summary>
    /// Interaction logic for ClueBrowserWindow.xaml
    /// </summary>
    public partial class ClueBrowserWindow : Window
    {
        public ClueBrowserWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

        internal void Update(MULTITUDE.Class.Home home)
        {
            ClueBrowserControl.Update(home);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }
    }
}
