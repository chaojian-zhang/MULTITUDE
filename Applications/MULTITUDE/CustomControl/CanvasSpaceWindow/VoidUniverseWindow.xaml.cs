using MULTITUDE.Canvas;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for VoidUniverseWindow.xaml
    /// </summary>
    public partial class VoidUniverseWindow : Window
    {
        public VoidUniverseWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            VoidDocumentGrid.ItemsSource = new ObservableCollection<Document>(Home.Current.VoidUniverse);
        }

        private void VoidDocumentGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Console.WriteLine("Hello World");
        }

        private  List<Document> CloneItems(System.Collections.IList items)
        {
            List<Document> clone = new List<Document>();
            foreach (Document doc in items)
            {
                clone.Add(doc);
            }
            return clone;
        }

        private void RecoverButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Object item in CloneItems(VoidDocumentGrid.SelectedItems))
            {
                Document doc = item as Document;
                if (doc != null)
                {
                    // Update home
                    Home.Current.Recover(doc);

                    // Update collection (Simulated)
                    (VoidDocumentGrid.ItemsSource as ObservableCollection<Document>).Remove(doc);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Object item in CloneItems(VoidDocumentGrid.SelectedItems))
            {
                Document doc = item as Document;
                if (doc != null)
                {
                    // Update home
                    Home.Current.Eliminate(doc);

                    // Update collection (Simulated)
                    (VoidDocumentGrid.ItemsSource as ObservableCollection<Document>).Remove(doc);
                }
            }
        }
    }
}
