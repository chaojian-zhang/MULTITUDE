using System.Windows;
using System.Windows.Input;

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
