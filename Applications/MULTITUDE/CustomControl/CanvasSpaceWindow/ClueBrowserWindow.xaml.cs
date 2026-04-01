using MULTITUDE.Canvas;
using System;
using System.Windows;

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
