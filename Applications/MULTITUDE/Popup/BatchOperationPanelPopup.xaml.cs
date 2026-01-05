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

namespace MULTITUDE.Popup
{
    /// <summary>
    /// Interaction logic for BatchOperationPanelPopup.xaml
    /// </summary>
    public partial class BatchOperationPanelPopup : Window
    {
        public BatchOperationPanelPopup(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

        private void OperationPanelBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ShowPanelLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CollapsedVisibleArea.Visibility = Visibility.Collapsed;
            ExpandedVisibleArea.Visibility = Visibility.Visible;
        }

        private void HidePanelLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ExpandedVisibleArea.Visibility = Visibility.Collapsed;
            CollapsedVisibleArea.Visibility = Visibility.Visible;

            // Also want it to automatically position itself to right border
            // Might record this.Left and this.ActualWidth here before collapse, then in SizeChanged event do a window positioning
        }
    }
}
