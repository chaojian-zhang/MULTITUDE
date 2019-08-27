using MULTITUDE.Canvas;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.CustomControl.Components;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for CollectionCreatorWindow.xaml
    /// </summary>
    public partial class CollectionCreatorWindow : Window, INotifyPropertyChanged
    {
        #region Construction
        internal CollectionCreatorWindow(Window owner, Archive archive)
        {
            InitializeComponent();
            Owner = owner;

            // If we are holding SHIFT, then open a new one
            // Null opening handlings
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) CurrentArchive = null;
            // Create one if not supplied
            if (archive == null)
            {
                if (CurrentArchive == null)
                    CurrentArchive = Home.Current.CreateDocument(DocumentType.VirtualArchive) as Archive;
            }
            else CurrentArchive = archive;

            // Populate default panels
            double verticalDisplacement = 600;
            for (int i = 0; i < CurrentArchive.Roots.Count; i++)
            {
                ArchiveNode root = CurrentArchive.Roots[i];

                VirtualArchivePanel rootPanel = new VirtualArchivePanel(new ArchiveNodeRepresentation(root));
                System.Windows.Controls.Canvas.SetLeft(rootPanel, 300);
                System.Windows.Controls.Canvas.SetTop(rootPanel, 200 + i * verticalDisplacement);
                VirtualArchiveCanvas.Children.Add(rootPanel);
            }

            // Extra bits of interface initialization
            Connections = new List<PanelConnection>();

            // UI Update
            NotifyPropertyChanged("DocumentName");
        }

        private void CreatorWindow_Closed(object sender, EventArgs e)
        {
            CurrentArchive.MarkDirty();
            CurrentArchive.RequestSave();
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }

        // Bookeeping
        private static Archive CurrentArchive;
        #endregion

        #region Configurations
        public static readonly string DefaultNodeName = "New Node";
        #endregion

        #region Canvas Navigation
        Point CurrentPosition;
        private void VirtualArchiveCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                CurrentPosition = e.GetPosition(this);
                this.CaptureMouse();
            }
        }

        private void VirtualArchiveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point NewPosition = e.GetPosition(this);
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                CanvasTranslation.X += NewPosition.X - CurrentPosition.X;
                CanvasTranslation.Y += NewPosition.Y - CurrentPosition.Y;
            }
            CurrentPosition = NewPosition;
            e.Handled = true;
        }

        private void VirtualArchiveCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.ReleaseMouseCapture();
        }
        #endregion

        #region Interface Interaction
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftShift))
            {
                if (CurrentArchive.IsReal == false)
                {
                    // When clicked in emepty area, create a new root node
                    ArchiveNode newNode = CurrentArchive.AddRootNode(DefaultNodeName);

                    // Create a new panel at location
                    VirtualArchivePanel newPanel = new VirtualArchivePanel(new ArchiveNodeRepresentation(newNode));

                    Point mouse = e.GetPosition(VirtualArchiveCanvas);
                    System.Windows.Controls.Canvas.SetLeft(newPanel, mouse.X);
                    System.Windows.Controls.Canvas.SetTop(newPanel, mouse.Y);
                    VirtualArchiveCanvas.Children.Add(newPanel);
                }
                else
                {
                    (Owner as VirtualWorkspaceWindow).UpdateStatus("Cannot create new root node on an real archive.");
                }
                e.Handled = true;
            }
        }

        private List<PanelConnection> Connections;
        internal void AddNextPanelFor(VirtualArchivePanel startPanel, VirtualArchivePanel nextPanel)
        {
            // Get location
            double xPositionOffset = startPanel.ActualWidth * 1.5;
            Point startPanelPosition = new Point((double)startPanel.GetValue(System.Windows.Controls.Canvas.LeftProperty),
                (double)startPanel.GetValue(System.Windows.Controls.Canvas.TopProperty));
            Point nextPanelPosition = new Point((double)startPanel.GetValue(System.Windows.Controls.Canvas.LeftProperty) + xPositionOffset,
                (double)startPanel.GetValue(System.Windows.Controls.Canvas.TopProperty));

            // Show next panel
            System.Windows.Controls.Canvas.SetLeft(nextPanel, nextPanelPosition.X);
            System.Windows.Controls.Canvas.SetTop(nextPanel, nextPanelPosition.Y);
            VirtualArchiveCanvas.Children.Add(nextPanel);

            // Update
            this.UpdateLayout();
            UpdateConnectionBetween(startPanel, nextPanel);
        }
        //internal void RemovePanelsFor(VirtualArchivePanel nextPanel)  // Not working, and I don't think it's very userful
        //{
        //    // Find any exsiting
        //    PanelConnection connectionToRemove = null;
        //    foreach (PanelConnection connection in Connections)
        //    {
        //        if (connection.StartPanel == nextPanel)
        //        {
        //            connectionToRemove = connection;
        //            break; // We know there will only be one line so we can break now, but if we want there be more lines we can comment this line
        //        }
        //    }

        //    if (connectionToRemove != null)
        //    {
        //        Connectors.Children.Remove(connectionToRemove.Line);
        //        Connections.Remove(connectionToRemove);
        //        RemovePanelsFor(connectionToRemove.EndPanel);
        //    }
        //}
        internal void UpdateConnection(VirtualArchivePanel endPanel)
        {
            Point nextPanelPosition = new Point((double)endPanel.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)endPanel.GetValue(System.Windows.Controls.Canvas.TopProperty));

            // Find any exsiting
            foreach (PanelConnection connection in Connections)
            {
                if (connection.EndPanel == endPanel)
                {
                    Point startPanelPosition = new Point((double)connection.StartPanel.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)connection.StartPanel.GetValue(System.Windows.Controls.Canvas.TopProperty));

                    // Update
                    connection.Line.StartPoint = new Point(startPanelPosition.X + connection.StartPanel.ActualWidth, startPanelPosition.Y + connection.StartPanel.ActualHeight / 2);
                    connection.Line.EndPoint = new Point(nextPanelPosition.X, nextPanelPosition.Y + endPanel.ActualHeight / 2);

                    break; // We know there will only be one line so we can break now, but if we want there be more lines we can comment this line
                }
            }
        }

        internal void UpdateContent(VirtualArchivePanel virtualArchivePanel, ArchiveNodeRepresentation node)
        {
            ContentEditorBorder.DataContext = node.Node;
            if (CurrentArchive.IsReal == false) ContentEditorBorder.Visibility = Visibility.Visible; 
        }

        internal void UpdateConnectionBetween(VirtualArchivePanel startPanel, VirtualArchivePanel nextPanel)
        {
            Point startPanelPosition = new Point((double)startPanel.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)startPanel.GetValue(System.Windows.Controls.Canvas.TopProperty));
            Point nextPanelPosition = new Point((double)nextPanel.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)nextPanel.GetValue(System.Windows.Controls.Canvas.TopProperty));

            LineGeometry line = null;
            // Change exsiting
            foreach (PanelConnection connection in Connections)
            {
                if(connection.StartPanel == startPanel)
                {
                    line = connection.Line;
                    break;
                }
            }
            // Extablish a new one
            if (line == null) { line = new LineGeometry(); Connectors.Children.Add(line); Connections.Add(new PanelConnection(startPanel, nextPanel, line)); }

            // Update
            line.StartPoint = new Point(startPanelPosition.X + startPanel.ActualWidth, startPanelPosition.Y + startPanel.ActualHeight / 2);
            line.EndPoint = new Point(nextPanelPosition.X, nextPanelPosition.Y + nextPanel.ActualHeight / 2);
        }

        #endregion

        #region <Deprecated> Content Editing: below features are no longer needed because not it's fixed inside a grid
        bool bIsEditingContent = false;
        private void ContentEditorBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bIsEditingContent = true;
            (sender as Border).CaptureMouse();
            CurrentPosition = e.GetPosition(this);
        }

        private void ContentEditorBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && bIsEditingContent)
            {
                Point newPosition = e.GetPosition(this);
                Point prevLocation = new Point((double)ContentEditorBorder.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)ContentEditorBorder.GetValue(System.Windows.Controls.Canvas.TopProperty));
                Point newLocation = new Point(prevLocation.X + newPosition.X - CurrentPosition.X, prevLocation.Y + newPosition.Y - CurrentPosition.Y);
                System.Windows.Controls.Canvas.SetLeft(ContentEditorBorder, newLocation.X);
                System.Windows.Controls.Canvas.SetTop(ContentEditorBorder, newLocation.Y);
                CurrentPosition = newPosition;
            }
        }

        private void ContentEditorBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bIsEditingContent = false;
            (sender as Border).ReleaseMouseCapture();
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

        public string DocumentName
        {
            get { return (CurrentArchive == null) ? null : CurrentArchive.Name; }    // Can be null udring initliazation
            set
            {
                if (value != CurrentArchive.Name)
                {
                    CurrentArchive.Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }

    // Data Structure
    public class PanelConnection
    {
        public PanelConnection(VirtualArchivePanel start, VirtualArchivePanel end, LineGeometry line)
        {
            StartPanel = start;
            EndPanel = end;
            Line = line;
        }

        public VirtualArchivePanel StartPanel { get; set; }
        public VirtualArchivePanel EndPanel { get; set; }
        public LineGeometry Line { get; set; }
    }
}
