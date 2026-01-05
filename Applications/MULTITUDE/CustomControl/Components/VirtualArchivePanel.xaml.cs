using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using MULTITUDE.CustomControl.CanvasSpaceWindow;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl.Components
{
    #region Helper Data Structures
    public enum ArchiveNodeType
    {
        File,   // Refers to actual file
        Folder, // Refers to actual folder
        Link, // Link to a document
        Virtual // Refers to nothing
    }

    public class ArchiveNodeRepresentation : INotifyPropertyChanged
    {
        #region Constructor
        internal ArchiveNodeRepresentation(ArchiveNode node)
        {
            // Bookeeping
            Node = node;
            Type = node.Owner.IsReal ? (node.Children.Count != 0 ? ArchiveNodeType.Folder : ArchiveNodeType.File) : ArchiveNodeType.Virtual;
            if (Type == ArchiveNodeType.Virtual && Node.GetLinkedDocument() != null) Type = ArchiveNodeType.Link;

            Contents = null;
        }
        internal ArchiveNode Node { get; set; }
        internal ArchiveNodeType Type { get; set; }
        private ObservableCollection<ArchiveNodeRepresentation> _Contents;
        #endregion

        #region Interactions
        public void UpdateContents()
        {
            Contents = new ObservableCollection<ArchiveNodeRepresentation>();
            foreach (ArchiveNode child in Node.Children)
            {
                Contents.Add(new ArchiveNodeRepresentation(child));
            }
        }
        public void AddNode()
        {
            Contents.Add(new ArchiveNodeRepresentation(Node.AddNode()));
        }
        #endregion

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public string Name
        {
            get { return Node.Name; }
            set
            {
                if (value != this.Node.Name)
                {
                    this.Node.Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ObservableCollection<ArchiveNodeRepresentation> Contents
        {
            get { return _Contents; }
            set
            {
                if (value != this._Contents)
                {
                    this._Contents = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage Image
        {
            get
            {
                switch (Type)
                {
                    case ArchiveNodeType.File: return ListFileInfo.FileImage;
                    case ArchiveNodeType.Folder: return ListFileInfo.FolderImage;
                    case ArchiveNodeType.Virtual: return null;
                    case ArchiveNodeType.Link: return Node.GetLinkedDocument().VirtualIcon;
                    default: return null;
                }
            }
        }
        #endregion
    }
    #endregion

    /// <summary>
    /// Interaction logic for VirtualArchivePanel.xaml
    /// </summary>
    public partial class VirtualArchivePanel : UserControl, INotifyPropertyChanged
    {
        #region Data and Construction
        private VirtualArchivePanel NextPanel;
        internal VirtualArchivePanel(ArchiveNodeRepresentation node)
        {
            InitializeComponent();
            NodePresentation = node;
            node.UpdateContents();
        }
        internal void Update(ArchiveNodeRepresentation node)
        {
            NodePresentation = node;
            node.UpdateContents();
        }
        #endregion

        #region Interface Handling
        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (sender as TextBox).IsReadOnly = false;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (e.Key == Key.Enter)
            {
                box.IsReadOnly = true;

                // Commit name change
                ArchiveNodeRepresentation node = (box.DataContext as ArchiveNodeRepresentation);
                node.Name = box.Text;
                box.Text = node.Name;   // Might have been stripped down
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).IsReadOnly = true;
        }

        private void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = sender as ListBox;
            object selection = box.SelectedItem;
            if(selection != null)   // Can be null when this panel is reused
            {
                ArchiveNodeRepresentation node = (box.SelectedItem as ArchiveNodeRepresentation);
                // If next panel is not null then use it
                if (NextPanel != null)
                {
                    NextPanel.Update(node);
                    // (Window.GetWindow(this) as CollectionCreatorWindow).RemovePanelsFor(this);
                }
                // Otherwise create one
                else
                {
                    NextPanel = new VirtualArchivePanel(node);
                    (Window.GetWindow(this) as CollectionCreatorWindow).AddNextPanelFor(this, NextPanel);
                }

                // Notify Parent
                (Window.GetWindow(this) as CollectionCreatorWindow).UpdateContent(this, node);

                e.Handled = true;
            }
        }

        private void CreateNewNodeButton_Click(object sender, RoutedEventArgs e)
        {
            ArchiveNodeRepresentation node = (sender as Button).DataContext as ArchiveNodeRepresentation;
            node.AddNode();
        }

        private void IconImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ArchiveNodeRepresentation repre = (sender as Image).DataContext as ArchiveNodeRepresentation;
            switch (repre.Type)
            {
                case ArchiveNodeType.File:
                case ArchiveNodeType.Folder:
                    System.Diagnostics.Process.Start(repre.Node.GetPath());
                    break;
                case ArchiveNodeType.Link:
                    VirtualWorkspaceWindow.CurrentWindow.OpenDocument(repre.Node.GetLinkedDocument(), false);
                    break;
                case ArchiveNodeType.Virtual:
                    break;
                default:
                    break;
            }
        }

        #region Background Interface Overwite: So content holder won't receive those events while mouse in over us
        private void VirtualArchiveNodeList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void VirtualArchiveNodeList_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
        }
        #endregion

        bool bIsDragging = false;
        Point currentPosition;
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            currentPosition = e.GetPosition(this.Parent as FrameworkElement);
            this.CaptureMouse();
            bIsDragging = true;
        }

        //Set the new canvas top and left proeprties here.
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            // Tranlsate
            if (e.LeftButton == MouseButtonState.Pressed && bIsDragging)
            {
                Point newPosition = e.GetPosition(this.Parent as FrameworkElement);
                Point prevLocation = new Point((double)this.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)this.GetValue(System.Windows.Controls.Canvas.TopProperty));
                Point newLocation = new Point(prevLocation.X + newPosition.X - currentPosition.X, prevLocation.Y + newPosition.Y - currentPosition.Y);
                System.Windows.Controls.Canvas.SetLeft(this, newLocation.X);
                System.Windows.Controls.Canvas.SetTop(this, newLocation.Y);

                currentPosition = newPosition;

                if (NextPanel != null)
                {
                    (Window.GetWindow(this) as CollectionCreatorWindow).UpdateConnectionBetween(this, NextPanel);
                }
                (Window.GetWindow(this) as CollectionCreatorWindow).UpdateConnection(this);
            }
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
            bIsDragging = false;
        }
        #endregion

        #region Data Binding
        #region Interface Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region Interface Memebers
        private ArchiveNodeRepresentation _NodePresentation;

        public ArchiveNodeRepresentation NodePresentation
        {
            get { return _NodePresentation; }
            set
            {
                if (value != this._NodePresentation)
                {
                    this._NodePresentation = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
        #endregion
    }
}
