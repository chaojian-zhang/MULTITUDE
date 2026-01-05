using MULTITUDE.Canvas;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.CustomControl.CanvasSpaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for GraphEditor.xaml
    /// </summary>
    public partial class GraphEditor : Window, INotifyPropertyChanged
    {
        #region Construction and Basic Window Events
        public GraphEditor(Window owner)
        {
            InitializeComponent();
            Owner = owner;
            NotifyPropertyChanged("StackedLayerNames");
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save data
            foreach (GraphLayer layer in _StackedLayers)
            {
                // Commit change
                layer.SaveGraphData();
            }
            GraphGrid.Children.Clear();
            _StackedLayers.Clear();
            NotifyPropertyChanged("StackedLayerNames");
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }
        #endregion

        #region Event Routing
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (_StackedLayers.Count != 0)
            {
                if (e.Key == Key.Escape && _StackedLayers.Count > 1) CloseLayer(_StackedLayers.Last());
                else _StackedLayers.Last().UserControl_KeyDown(sender, e);
            }
        }
        #endregion

        #region Data Setup and Update
        private GraphLayer ActiveLayer;
        private static List<GraphLayer> _StackedLayers = new List<GraphLayer>();
        private static Graph CurrentDocument;
        internal void Setup(Document target = null)
        {
            // If we are holding SHIFT, then open a new one
            if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) CurrentDocument = null;
            // Create one if not supplied
            if (target == null)
            {
                if(CurrentDocument == null)
                    CurrentDocument = Home.Current.CreateDocument(DocumentType.Graph, Graph.DefaultName) as Graph;
            }
            else CurrentDocument = target as Graph;

            // Create a defualt layer
            OpenLayer(CurrentDocument);
        }
        internal void OpenLayer(Graph doc)
        {
            // Create new
            GraphLayer newLayer = new GraphLayer(this, doc);
            _StackedLayers.Add(newLayer);
            // Open for editing
            GraphGrid.Children.Clear();
            GraphGrid.Children.Add(newLayer);
            // Dim Prev if we are not the default one
            if(ActiveLayer != null) ActiveLayer.Opacity = 0.3;
            ActiveLayer = newLayer;
            // Update UI
            NotifyPropertyChanged("StackedLayerNames");
        }
        private void CloseLayer(GraphLayer layer)
        {
            // Restrict it to be only the last one
            if (_StackedLayers.Last() != layer) throw new IndexOutOfRangeException("Specified layer isn't opened or is not the most recent one in current editor.");

            _StackedLayers.Remove(layer);   
            GraphGrid.Children.Remove(layer);
            // Commit change
            layer.SaveGraphData();
            // Return active
            ActiveLayer = _StackedLayers.Last();
            ActiveLayer.Opacity = 1;
            // Show prev one
            GraphGrid.Children.Add(ActiveLayer);
            // Update UI
            NotifyPropertyChanged("StackedLayerNames");
        }
        #endregion

        #region Content Editing
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<string> StackedLayerNames
        {
            get
            {
                int indentation = 0;
                ObservableCollection<string> returnValue = 
                    new ObservableCollection<string>(_StackedLayers.Select(item => new String('\t', indentation++) + "-" + item.DocumentName));
                if(returnValue.Count > 0) returnValue.Insert(0, "Opened Graph Stacks: ");
                return returnValue;
            }
        }
        #endregion
    }
}
