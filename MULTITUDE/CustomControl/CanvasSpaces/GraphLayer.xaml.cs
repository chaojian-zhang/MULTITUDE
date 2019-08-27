using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.CustomControl.Components;
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

namespace MULTITUDE.CustomControl.CanvasSpaces
{
    #region Helper Data Structures
    public class GraphNodeView: INotifyPropertyChanged
    {
        internal GraphNodeView(GraphNode node, GraphLayer owner)
        {
            // Book Keeping
            Node = node;
            node.View = this;
            // Visual Generation
            VisualElement = new GraphNodeControl(this, owner);
            // Positioning
            System.Windows.Controls.Canvas.SetLeft(VisualElement, node.Location.X);
            System.Windows.Controls.Canvas.SetTop(VisualElement, node.Location.Y);
        }
        internal GraphNodeView(Document doc, string searchString, Point neutralLocation, GraphLayer owner)
        {
            // Create Node
            Node = new GraphNode(doc.GUID, searchString, this);
            Node.Location = neutralLocation;
            // General Visual
            VisualElement = new GraphNodeControl(this, owner);
            // Positioning
            System.Windows.Controls.Canvas.SetLeft(VisualElement, neutralLocation.X);
            System.Windows.Controls.Canvas.SetTop(VisualElement, neutralLocation.Y);
        }
        // neutralLocation is location with scale = 1
        public GraphNodeView(NodeType type, Point neutralLocation, GraphLayer owner)
        {
            // Create Node
            Node = new GraphNode(this, type);
            Node.Location = neutralLocation;
            // General Visual
            VisualElement = new GraphNodeControl(this, owner);
            // Positioning
            System.Windows.Controls.Canvas.SetLeft(VisualElement, neutralLocation.X);
            System.Windows.Controls.Canvas.SetTop(VisualElement, neutralLocation.Y);
        }

        public GraphNodeView(GraphNodeView refNode, Point neutralLocation, GraphLayer owner)
        {
            // Create Node
            Node = new GraphNode(refNode.Node, this);
            Node.Location = neutralLocation;
            // General Visual
            VisualElement = new GraphNodeControl(this, owner);
            // Positioning
            System.Windows.Controls.Canvas.SetLeft(VisualElement, neutralLocation.X);
            System.Windows.Controls.Canvas.SetTop(VisualElement, neutralLocation.Y);
        }

        internal GraphNode Node { get; set; }
        public GraphNodeControl VisualElement { get; set; }

        #region Data Binding
        public void UpdateDescription()
        {
            NotifyPropertyChanged("Description");

            // Also update users of this view
            VisualElement.OwnerGraphLayer.UpdateReferenceNodeDescription(this);
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool Contains(string searchString)
        {
            switch (Node.Type)
            {
                case NodeType.SimpleText:
                    return (Node.Title.ToLower().Contains(searchString.ToLower()) || Node.Content.ToLower().Contains(searchString.ToLower()));
                case NodeType.Link:
                    return Home.Current.GetDocument(Node.LinkGUID).IsValueAnywherePresent(searchString.ToLower());
                case NodeType.RichFlowText:
                    TextRange range = new TextRange(Node._MDDocument.ContentStart, Node._MDDocument.ContentEnd);
                    return range.Text.ToLower().Contains(searchString.ToLower());
                case NodeType.Jumper:
                    return Node.Ref.View.Contains(searchString);
                default:
                    return false;
            }
        }

        public string Description
        {
            get
            {
                switch (Node.Type)
                {
                    case NodeType.SimpleText:
                        return string.Format("[{0}] {1}", Node.Type.ToString(), Node.Title);
                    case NodeType.Link:
                        return string.Format("[{0}] {1}", Node.Type.ToString(), Home.Current.GetDocument(Node.LinkGUID).ShortDescription);
                    case NodeType.RichFlowText:
                        TextRange range = new TextRange(Node._MDDocument.ContentStart, Node._MDDocument.ContentEnd);
                        return string.Format("[{0}] {1}...", Node.Type.ToString(), range.Text.Substring(0, 20 < range.Text.Length ? 20 : range.Text.Length));
                    case NodeType.Jumper:
                        return string.Format("[{0}] To: {1}", Node.Type.ToString(), Node.Ref.View.Description);
                    default:
                        return Node.Title;
                }
            }
        }
        #endregion

        public static readonly string DragDropFormatString = "GraphNodeView DragDrop Format";

        // Input delta should be netrual with scale = 1
        public void Translate(double dx, double dy)
        {
            // Update Data
            Node.Translate(dx, dy);
            // Update UI
            System.Windows.Controls.Canvas.SetLeft(VisualElement, Node.Location.X);
            System.Windows.Controls.Canvas.SetTop(VisualElement, Node.Location.Y);
        }

        internal Rect GetRect()
        {
            return new Rect((double)this.VisualElement.GetValue(System.Windows.Controls.Canvas.LeftProperty), (double)this.VisualElement.GetValue(System.Windows.Controls.Canvas.TopProperty),
                this.VisualElement.ActualWidth, this.VisualElement.ActualHeight);
        }
    }

    public class GraphNodeConnection
    {
        internal GraphNodeConnection(GraphNodeView start, GraphNodeView end, LineGeometry line)
        {
            Start = start;
            End = end;
            Line = line;
            Connection = new GraphConnection(start.Node, end.Node);
        }

        private GraphConnection Connection;
        public GraphNodeView Start { get; set; }
        public GraphNodeView End { get; set; }
        public LineGeometry Line { get; set; }
    }
    #endregion

    /// <summary>
    /// Interaction logic for GraphLayer.xaml
    /// </summary>
    public partial class GraphLayer : UserControl, INotifyPropertyChanged
    {
        internal GraphLayer(MULTITUDE.CustomControl.CanvasSpaceWindow.GraphEditor parentEditor, Graph doc)
        {
            InitializeComponent();
            _Editor = parentEditor;
            Graph = doc;
            SelectedNodes = new List<GraphNodeView>();
            Bookmarks = new ObservableCollection<GraphNodeView>();
            NotifyPropertyChanged("DocumentName");

            // Construct a memor popup
            SearchPopup = new MemoirSearchBoxPopup();
            SearchPopup.Configure(SearchBarWidth, SearchMode.General, InterfaceOption.NormalTextField | InterfaceOption.HeavilyDecoratedRoudCorner | InterfaceOption.ShowDocumentPreview | InterfaceOption.MultipleDocumentSelection);
            SearchPopup.SecondaryPreviewKeyDownEvent += new MemoirSearchBoxPopup.PreviewKeyDownHandler(MemoirBarKeyDownHandler);
            SearchPopup.DocumentSelectionConfirmationEvent += new MemoirSearchBoxPopup.DocumentSelectionConfirmationHandler(MemoirBarDocumentSelectionConfirmationHandler);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Layout related loading
            InitiateNodes();
            this.UpdateLayout();
            InitiateConnections();
            // Load Bookmarks
            LoadBookmarks();
        }

        private void InitiateNodes()
        {
            List<GraphNode> nodes = Graph.Data.Nodes;
            Nodes = new List<GraphNodeView>();
            foreach (GraphNode node in nodes)
            {
                // Create Node
                GraphNodeView newNode = new GraphNodeView(node, this);
                Nodes.Add(newNode);
                // Add UI
                if (node.Type == NodeType.Link && Home.Current.IsDocumentOfType(node.LinkGUID,DocumentType.ImagePlus) == true) GraphLayerImageCanvas.Children.Add(newNode.VisualElement);
                else GraphLayerCanvas.Children.Add(newNode.VisualElement);
            }
            // Update for flow documents
            this.UpdateLayout();
            foreach (GraphNodeView node in Nodes)
            {
                node.VisualElement.AdjustFlowDocumentPreviewSize();
            }
        }
        private void InitiateConnections()
        {
            List<GraphConnection> connections = Graph.Data.Connections;
            Connections = new List<GraphNodeConnection>();
            foreach (GraphConnection connection in connections)
            {
                // Create
                LineGeometry line = new LineGeometry();
                GraphNodeConnection newConnection = new GraphNodeConnection(connection.NodeA.View, connection.NodeB.View, line);
                Connections.Add(newConnection);
                // Update
                UpdateLine(newConnection);
                // Add UI
                Connectors.Children.Add(line);
            }
        }
        private void LoadBookmarks()
        {
            foreach (GraphNode markedItem in Graph.Data.Bookmarks)
            {
                Bookmarks.Add(markedItem.View);
            }
        }
        // Should be called during closing
        public void SaveGraphData()
        {
            // Overriding existing doc
            Graph.SaveData(Nodes, Connections, Bookmarks);
            // Close popups if not already done so
            SearchPopup.Hide();
        }

        #region Configurations
        public static readonly double SearchBarWidth = 250;
        #endregion

        #region Data Update
        private MemoirSearchBoxPopup SearchPopup;
        private MULTITUDE.CustomControl.CanvasSpaceWindow.GraphEditor _Editor;
        public MULTITUDE.CustomControl.CanvasSpaceWindow.GraphEditor Editor { get { return _Editor; } }
        private Graph Graph;
        private List<GraphNodeView> Nodes;
        private List<GraphNodeConnection> Connections;
        #endregion

        #region Node Creation Helpers
        internal void CreateNewLine(GraphNodeView start, GraphNodeView end)
        {
            // Create
            LineGeometry line = new LineGeometry();
            Connectors.Children.Add(line);
            GraphNodeConnection newConnection = new GraphNodeConnection(start, end, line);
            Connections.Add(newConnection);

            // Update
            UpdateLine(newConnection);
        }

        internal void UpdateLine(GraphNodeConnection connection)
        {
            GraphNodeView start = connection.Start;
            GraphNodeView end = connection.End;
            Rect startRect = start.GetRect();
            Rect endRect = end.GetRect();

            double horizontalGap = endRect.X > (startRect.X + startRect.Width) ? endRect.X - (startRect.X + startRect.Width) : (endRect.X > startRect.X ? endRect.X - startRect.X : (startRect.X - (endRect.X + endRect.Width)));
            double verticalGap = endRect.Y > (startRect.Y + startRect.Height) ? endRect.Y - (startRect.Y + startRect.Height) : (endRect.Y > startRect.Y ? endRect.Y - startRect.Y : (startRect.Y - (endRect.Y + endRect.Height)));

            #region Positional Comparison for Center-Edge Positioning
            if (endRect.X > (startRect.X + startRect.Width))
            {
                if (horizontalGap > verticalGap)
                {
                    connection.Line.StartPoint = new Point(startRect.X + startRect.Width, startRect.Y + startRect.Height / 2);
                    connection.Line.EndPoint = new Point(endRect.X, endRect.Y + endRect.Height / 2);
                }
                else
                {
                    if (endRect.Y > (startRect.Y + startRect.Height))
                    {
                        connection.Line.StartPoint = new Point(startRect.X + startRect.Width / 2, startRect.Y + startRect.Height);
                        connection.Line.EndPoint = new Point(endRect.X + endRect.Width / 2, endRect.Y);
                    }
                    else
                    {
                        connection.Line.StartPoint = new Point(startRect.X + startRect.Width / 2, startRect.Y);
                        connection.Line.EndPoint = new Point(endRect.X + endRect.Width / 2, endRect.Y + endRect.Height);
                    }
                }
            }
            else if (endRect.X > startRect.X)
            {
                if(endRect.Y > startRect.Y)
                {
                    connection.Line.StartPoint = new Point(startRect.X + startRect.Width / 2, startRect.Y + startRect.Height);
                    connection.Line.EndPoint = new Point(endRect.X + endRect.Width / 2, endRect.Y);
                }
                else
                {
                    connection.Line.StartPoint = new Point(startRect.X + startRect.Width / 2, startRect.Y);
                    connection.Line.EndPoint = new Point(endRect.X + endRect.Width / 2, endRect.Y + endRect.Height);
                }
            }
            else
            {
                if (horizontalGap > verticalGap)
                {
                    connection.Line.StartPoint = new Point(endRect.X + endRect.Width, endRect.Y + endRect.Height / 2);
                    connection.Line.EndPoint = new Point(startRect.X, startRect.Y + startRect.Height / 2);
                }
                else
                {
                    if (startRect.Y > (endRect.Y + endRect.Height))
                    {
                        connection.Line.StartPoint = new Point(endRect.X + endRect.Width / 2, endRect.Y + endRect.Height);
                        connection.Line.EndPoint = new Point(startRect.X + startRect.Width / 2, startRect.Y);
                    }
                    else
                    {
                        connection.Line.StartPoint = new Point(endRect.X + endRect.Width / 2, endRect.Y);
                        connection.Line.EndPoint = new Point(startRect.X + startRect.Width / 2, startRect.Y + startRect.Height);
                    }
                }
            }
            #endregion
        }
        
        internal void UpdateConnect(GraphNodeView node)
        {
            // Update all connections related
            foreach (GraphNodeConnection connection in Connections)
            {
                if (connection.Start == node || connection.End == node)
                    UpdateLine(connection);
            }
            // Otherwise nothing happens
        }

        internal void Connect(GraphNodeView node1, GraphNodeView node2, bool bRemoveIfExisting)
        {
            // Find existing ones
            int index = -1;
            for (int i = 0; i < Connections.Count(); i++)
            {
                GraphNodeConnection connection = Connections[i];
                if ((connection.Start == node1 && connection.End == node2)
                    || (connection.Start == node2 && connection.End == node1)) { index = i; break; }
            }
            if(index != -1)
            {
                if (bRemoveIfExisting)
                {
                    Connectors.Children.Remove(Connections[index].Line);
                    Connections.RemoveAt(index);
                }
                else UpdateLine(Connections[index]);
            }
            else
                // Create a new connection if not already connected
                CreateNewLine(node1, node2);
        }

        internal void Disconnect(GraphNodeView node1, GraphNodeView node2)
        {
            // Connect if not already connected
            int index = -1;
            for (int i = 0; i < Connections.Count; i++)
            {
                // Otherwise if already exist update it
                GraphNodeConnection connection = Connections[i];
                if ((connection.Start == node1 && connection.End == node2)
                    || (connection.Start == node2 && connection.End == node1)) index  = i;
            }
            // Remove
            if (index != -1)
            {
                Connectors.Children.Remove(Connections[index].Line);
                Connections.RemoveAt(index);
            }
            else throw new ArgumentOutOfRangeException("Not connected.");
        }
        #endregion

        #region Drag and Drop Support
        private void GraphLayerGrid_DragEnter(object sender, DragEventArgs e)
        {
            // Check drop type and give an icon indicating its dropped effect
            base.OnDragEnter(e);

            // If the DataObject contains documents
            if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                // Show additional feedback
                DocumentDropCursorText.Visibility = Visibility.Visible;
                DocumentDropCursorText.Text = ((Document)e.Data.GetData(Document.DragDropFormatString)).ShortDescription;
            }
            else if(e.Data.GetDataPresent(GraphNodeView.DragDropFormatString))
            {
                // Show additional feedback
                DocumentDropCursorText.Visibility = Visibility.Visible;
                DocumentDropCursorText.Text = "Add Jumper Here";
            }

            e.Handled = true;
        }

        private void GraphLayerGrid_DragLeave(object sender, DragEventArgs e)
        {
            base.OnDragLeave(e);

            if (DocumentDropCursorText.Visibility == Visibility.Visible)
                DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }

        private void GraphLayerGrid_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = DragDropEffects.None;

            // If the DataObject contains documents
            if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                e.Effects = DragDropEffects.Link;

                // Show additional feedback
                Point position = e.GetPosition(GraphLayerGrid);
                System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
            }
            else if(e.Data.GetDataPresent(GraphNodeView.DragDropFormatString))
            {
                e.Effects = DragDropEffects.Link;

                // Show additional feedback
                Point position = e.GetPosition(GraphLayerGrid);
                System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
            }

            e.Handled = true;
        }

        private void GraphLayerGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                Document doc = (Document)e.Data.GetData(Document.DragDropFormatString);

                Point absPosition = e.GetPosition(Window.GetWindow(this));
                CreateReferenceNodeAtCanvasLocation(new Point((absPosition.X - CanvasTranslation.X) / CanvasScale.ScaleX,
                    (absPosition.Y - CanvasTranslation.Y) / CanvasScale.ScaleY), doc, SearchPopup.SearchString);

                // Hide Memoir
                SearchPopup.Hide();
            }
            else if (e.Data.GetDataPresent(GraphNodeView.DragDropFormatString))
            {
                GraphNodeView node = (GraphNodeView)e.Data.GetData(GraphNodeView.DragDropFormatString);

                Point absPosition = e.GetPosition(Window.GetWindow(this));
                CreateJumperNodeAtCanvasLocation(new Point((absPosition.X - CanvasTranslation.X) / CanvasScale.ScaleX,
                    (absPosition.Y - CanvasTranslation.Y) / CanvasScale.ScaleY), node);
            }

            // Hide additional feedback
            DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }

        private void GraphLayerGrid_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            if (e.Effects.HasFlag(DragDropEffects.Link))
            {
                Mouse.SetCursor(Cursors.Cross);
            }
            else if (e.Effects.HasFlag(DragDropEffects.Move))
            {
                Mouse.SetCursor(Cursors.Pen);
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }
        #endregion

        #region View and Layout Helpers
        public void ZoomDefaultScale()
        {
            CanvasScale.ScaleX = CanvasScale.ScaleY = 1;
        }
        public void TranslateCanvas(double dx, double dy)
        {
            CanvasTranslation.X += dx;
            CanvasTranslation.Y += dy;
        }
        public void ZoomToFit()
        {
            // Configureation
            double margin = 20;
            // Get metrics
            double xmin = 0, xmax = 0;
            double ymin = 0, ymax = 0;
            // Select
            List<GraphNodeView> zoomNodes = SelectedNodes.Count != 0 ? SelectedNodes : Nodes;
            if (zoomNodes.Count() > 0)
            {
                xmin = zoomNodes[0].Node.Location.X - margin;
                xmax = zoomNodes[0].Node.Location.X + zoomNodes[0].VisualElement.ActualWidth + margin;
                ymin = zoomNodes[0].Node.Location.Y - margin;
                ymax = zoomNodes[0].Node.Location.Y + zoomNodes[0].VisualElement.ActualHeight + margin;
            }
            foreach (GraphNodeView item in zoomNodes)
            {
                if (item.Node.Location.X - margin < xmin) xmin = item.Node.Location.X - margin;
                else if(item.Node.Location.X + item.VisualElement.ActualWidth + margin > xmax) xmax = item.Node.Location.X + item.VisualElement.ActualWidth + margin;
                if (item.Node.Location.Y - margin < ymin) ymin = item.Node.Location.Y - margin;
                else if(item.Node.Location.Y + item.VisualElement.ActualHeight + margin > ymax) ymax = item.Node.Location.Y + item.VisualElement.ActualHeight + margin;
            }
            // Destination Scale
            double destScale = Math.Max(0.1, Math.Min(GraphLayerGrid.ActualWidth / (xmax - xmin), GraphLayerGrid.ActualHeight / (ymax - ymin)));
            if (destScale > 1) destScale = 1;    // Do not zoom too in
            Point trans = new Point((xmax + xmin) / 2 * destScale, (ymax + ymin) / 2 * destScale);
            // Zoom to
            CanvasScale.ScaleX = CanvasScale.ScaleY = destScale;
            CanvasTranslation.X = GraphLayerGrid.ActualWidth / 2 - trans.X;
            CanvasTranslation.Y = GraphLayerGrid.ActualHeight / 2 - trans.Y;
        }

        internal void JumpViewToNode(GraphNode refNode)
        {
            // Find the node
            foreach (GraphNodeView item in Nodes)
            {
                if(item == refNode.View)
                {
                    // Select
                    Reselect(new List<GraphNodeView>() { item });
                    // Zoom
                    ZoomToFit();
                    // Return
                    return;
                }
            }

            // Exception
            throw new InvalidOperationException("Graph node doesn't exist or has been removed.");
        }
        /// <summary>
        /// Zoom point relative to RenderCanvas (and make sure it's not just e.GetPoint(RenderCanvas) for that doesn't count existing tranlstion), in current physical distance with scale applied
        /// We zoom toward the cursor position, exactly keeping that point intact
        /// Summary of derivation process: Well it can be hard just to think or write code; Spend some time drawing on a paper - and give explicit quantities to represent quantities, and write down their relations, then we will see how things relate to each other in a clearer way
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="zoomPoint"></param>
        public void Zoom(double delta, Point zoomPoint)
        {
            // Record basic data
            double xOffset = zoomPoint.X / CanvasScale.ScaleX;
            double yOffset = zoomPoint.Y / CanvasScale.ScaleY;
            // Do scaling
            CanvasScale.ScaleX = CanvasScale.ScaleY = CanvasScale.ScaleX + delta;
            if (CanvasScale.ScaleX < 0.3) CanvasScale.ScaleX = CanvasScale.ScaleY = 0.3;
            // Collect extra information
            //double requiredXDistance = GraphLayerGrid.ActualWidth / 2;
            //double requiredYDistance = GraphLayerGrid.ActualHeight / 2;
            double requiredXDistance = zoomPoint.X + CanvasTranslation.X;
            double requiredYDistance = zoomPoint.Y + CanvasTranslation.Y;
            // Calculate results
            CanvasTranslation.X = requiredXDistance - xOffset * CanvasScale.ScaleX;  // Notice independent of previous translation: well actually it depends, but inlcuded in zoomPoint and above
            CanvasTranslation.Y = requiredYDistance - yOffset * CanvasScale.ScaleY;  // Notice independent of previous translation
        }
        public void AlignLeftSelections()
        {
            throw new NotImplementedException();
        }
        public void AlignRightSelections()
        {
            throw new NotImplementedException();
        }
        public void AlignUpSelections()
        {
            throw new NotImplementedException();
        }
        public void AlignBottomSelections()
        {
            throw new NotImplementedException();
        }
        public void AlignHCenterSelections()
        {
            throw new NotImplementedException();
        }
        public void AlignYCenterSelections()
        {
            throw new NotImplementedException();
        }
        public void DistributeHCenterSelections()
        {
            throw new NotImplementedException();
        }
        public void DistributeYCenterSelections()
        {
            throw new NotImplementedException();
        }
        // Delta in screen coordinates
        public void TranslateNodes(List<GraphNodeView> nodes, double dx, double dy)
        {
            foreach (GraphNodeView node in nodes)
            {
                TranslateNode(node, dx, dy);
            }
        }
        // Delta in screen coordinates
        private void TranslateNode(GraphNodeView node, double dx, double dy)
        {
            // Data and visual
            node.Translate((dx) / CanvasScale.ScaleX, (dy) / CanvasScale.ScaleX);
            // Connection
            UpdateConnect(node);
        }
        #endregion

        #region Node Creation and Manipulation
        #region Selection
        private List<GraphNodeView> SelectedNodes;
        private void SelectAll()
        {
            foreach (GraphNodeView node in Nodes)
            {
                SelectNode(node);
            }
        }
        private void SelectNodes(List<GraphNodeView> nodes)
        {
            foreach (GraphNodeView node in nodes)
            {
                SelectNode(node);
            }
        }
        private void SelectNode(GraphNodeView node)
        {
            if (SelectedNodes.Contains(node) == false) { SelectedNodes.Add(node); node.VisualElement.Highlight(); }
        }
        private void ToggleSelection(GraphNodeView node)
        {
            if (SelectedNodes.Contains(node) == false) { SelectedNodes.Add(node); node.VisualElement.Highlight(); }
            else { SelectedNodes.Remove(node); node.VisualElement.Unhighlight(); }
        }
        private void SelectNodeWithSearchHighlight(GraphNodeView node, string searchString)
        {
            // Deselect previous
            UnselectAll();
            // Select New
            SelectedNodes.Add(node);
            node.VisualElement.Highlight(searchString);
        }
        private void Reselect(List<GraphNodeView> nodes)
        {
            // Deselect previous
            UnselectAll();
            foreach (GraphNodeView node in nodes)
            {
                SelectedNodes.Add(node);
                node.VisualElement.Highlight();
            }
        }
        private void UnselectAll()
        {
            // Update Node Highlight
            foreach (GraphNodeView node in SelectedNodes)
            {
                node.VisualElement.Unhighlight();
            }
            // Clear
            SelectedNodes.Clear();
        }
        private void DeleteNode(GraphNodeView node)
        {
            // Delete Node from Data
            Nodes.Remove(node);

            // Delete Connections
            List<GraphNodeConnection> toRemove = new List<GraphNodeConnection>();
            foreach (GraphNodeConnection item in Connections)
            {
                if (item.Start == node || item.End == node) toRemove.Add(item);
                Connectors.Children.Remove(item.Line);
            }
            foreach (GraphNodeConnection item in toRemove)
            {
                Connections.Remove(item);
            }

            // Remove from selection if any
            SelectedNodes.Remove(node);

            // Remove from bookmarks
            RemoveBookmark(node);

            // Remove the node From Layer
            if (node.Node.Type == NodeType.Link && Home.Current.IsDocumentOfType(node.Node.LinkGUID, DocumentType.ImagePlus) == true) GraphLayerImageCanvas.Children.Remove(node.VisualElement);
            else GraphLayerCanvas.Children.Remove(node.VisualElement);

            // [Order Of this Operation Matters because it's recursive] Remove from jumper nodes if any
            List<GraphNodeView> toRemoveJumpers = Nodes.Where(item => (item.Node.Type == NodeType.Jumper && item.Node.Ref == node.Node)).ToList();
            foreach (GraphNodeView jumper in toRemoveJumpers)
            {
                DeleteNode(jumper);
            }
        }
        private void DeleteSelections()
        {
            List<GraphNodeView> toRemove = new List<GraphNodeView>(SelectedNodes);
            SelectedNodes.Clear();
            foreach (GraphNodeView item in toRemove)
            {
                DeleteNode(item);
            }
        }
        #endregion
        #region Creation
        // Input location should be relative to original canvas with scale = 1
        private void CreateTextNodeAtCanvasLocation(Point location)
        {
            // Create Node
            GraphNodeView newNode = new GraphNodeView(NodeType.SimpleText, location, this);
            Nodes.Add(newNode);
            // Selete Node
            UnselectAll();
            SelectNode(newNode);
            // Add UI
            GraphLayerCanvas.Children.Add(newNode.VisualElement);
        }
        private void CreateRichTextNodeAtCanvasLocation(Point location)
        {
            // Create Node
            GraphNodeView newNode = new GraphNodeView(NodeType.RichFlowText, location, this);
            Nodes.Add(newNode);
            // Selete Node
            UnselectAll();
            SelectNode(newNode);
            // Add UI
            GraphLayerCanvas.Children.Add(newNode.VisualElement);
        }
        private void CreateReferenceNodeAtCanvasLocation(Point location, Document doc, string searchString)
        {
            // Create Node
            GraphNodeView newNode = new GraphNodeView(doc, searchString, location, this);
            Nodes.Add(newNode);
            // Selete Node
            UnselectAll();
            SelectNode(newNode);
            // Add UI
            if (doc.Type == DocumentType.ImagePlus) GraphLayerImageCanvas.Children.Add(newNode.VisualElement);
            else GraphLayerCanvas.Children.Add(newNode.VisualElement);
            // Update
            this.UpdateLayout();
            newNode.VisualElement.AdjustFlowDocumentPreviewSize();
        }
        private void CreateJumperNodeAtCanvasLocation(Point location, GraphNodeView refNode)
        {
            // Create Node
            GraphNodeView newNode = new GraphNodeView(refNode, location, this);
            Nodes.Add(newNode);
            // Selete Node
            UnselectAll();
            SelectNode(newNode);
            // Add UI
            GraphLayerCanvas.Children.Add(newNode.VisualElement);
        }
        #endregion
        #region Connection
        private void ConnectNodes()
        {
            if (SelectedNodes.Count() >= 2)
            {
                // Simple connection
                if (SelectedNodes.Count() == 2)
                    Connect(SelectedNodes[0], SelectedNodes[1], true);
                else
                {
                    #region Pattern Connection
                    // Patter recognitions with very specific rules: To make things much easier for us, we require a particular selection order as well
                    // - A row (roughly overlapping the same line) with one bottom/up (not going out of height boundary), 
                    // - A column (roughly overlapping the same line) with one left/right (not going out of width boundary)
                    // - Horizontally/vertically paralell nodes are connected with each other (identified as 1. roughly occuprying the same horizontal/veritcal line 2. parallel ones roughly occupy the line), in strict pairs
                    Rect comparingRect = SelectedNodes.Last().GetRect();
                    // For all every others
                    double xmin = SelectedNodes.First().Node.Location.X, xmax = xmin + SelectedNodes.First().VisualElement.ActualWidth;
                    double ymin = SelectedNodes.First().Node.Location.Y, ymax = ymin + SelectedNodes.First().VisualElement.ActualHeight;
                    for (int i = 1; i < SelectedNodes.Count() - 1; i++)
                    {
                        GraphNodeView node = SelectedNodes[i];
                        if (node.Node.Location.X < xmin) xmin = node.Node.Location.X;
                        else if (node.Node.Location.X + node.VisualElement.ActualWidth > xmax) xmax = node.Node.Location.X + node.VisualElement.ActualWidth;
                        if (node.Node.Location.Y < ymin) xmin = node.Node.Location.Y;
                        else if (node.Node.Location.Y + node.VisualElement.ActualHeight > ymax) ymax = node.Node.Location.Y + node.VisualElement.ActualHeight;
                    }
                    // Patterneed connection: HOrizontal to top/bottom
                    if (comparingRect.X > xmin && comparingRect.X + comparingRect.Width < xmax)
                    {
                        for (int i = 0; i < SelectedNodes.Count() - 1; i++)
                        {
                            Connect(SelectedNodes[i], SelectedNodes.Last(), true);
                        }
                    }
                    // Patterneed connection: Vertical to left/right
                    else if (comparingRect.Y > ymin && comparingRect.Y + comparingRect.Height < ymax)
                    {
                        for (int i = 0; i < SelectedNodes.Count() - 1; i++)
                        {
                            Connect(SelectedNodes[i], SelectedNodes.Last(), true);
                        }
                    }
                    // Patterneed connection: Parallel
                    else if (SelectedNodes.Count() % 2 == 0)
                    {
                        int half = SelectedNodes.Count() / 2;
                        for (int i = 0; i < half; i++)
                        {
                            Connect(SelectedNodes[i], SelectedNodes[i + half], true);
                        }
                    }
                    // Pattern Connection: to last
                    else
                    {
                        for (int i = 0; i < SelectedNodes.Count() - 1; i++)
                        {
                            Connect(SelectedNodes[i], SelectedNodes.Last(), true);
                        }
                    }
                    #endregion
                }
            }
        }
        #endregion
        #region Memoir
        private Point MemoirBarAppearLocation;
        private void ShowMemoirBar()
        {
            MemoirBarAppearLocation = Mouse.GetPosition(Window.GetWindow(this));
            SearchPopup.Show(MemoirBarAppearLocation);
        }

        private void ExecuteMemoirActions(List<Document> selections, string searchString)
        {
            // Create a bunch of reference nodes
            foreach (Document doc in selections)
            {
                CreateReferenceNodeAtCanvasLocation(new Point((MemoirBarAppearLocation.X - CanvasTranslation.X) / CanvasScale.ScaleX,
                    (MemoirBarAppearLocation.Y - CanvasTranslation.Y) / CanvasScale.ScaleY), doc, searchString);
            }
        }

        public void MemoirBarKeyDownHandler(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ExecuteMemoirActions(SearchPopup.Selections, SearchPopup.SearchString);
                SearchPopup.Hide();
                e.Handled = true;
            }
        }

        internal void MemoirBarDocumentSelectionConfirmationHandler(List<Document> documents, string searchString)
        {
            ExecuteMemoirActions(documents, searchString);
            SearchPopup.Hide();
        }
        #endregion
        #region Panels Related
        private void AddBookmark(GraphNodeView node)
        {
            if(Bookmarks.Contains(node) == false) Bookmarks.Add(node);
        }

        private void RemoveBookmark(GraphNodeView node)
        {
            Bookmarks.Remove(node);
        }

        private void ListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement item = sender as FrameworkElement;
            if(item != null && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Bookmarks.Remove(item.DataContext as GraphNodeView);
            }
        }

        private void BookmarkList_MouseMove(object sender, MouseEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            // If LMB down and is current selection
            if (e.LeftButton == MouseButtonState.Pressed && listBox.SelectedItem != null)
            {
                // Package the data.
                DataObject data = new DataObject();
                data.SetData(GraphNodeView.DragDropFormatString, listBox.SelectedItem);

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Link);
                e.Handled = true;
            }
        }
        #endregion
        #region Short Cuts
        // Textbox sometimes take over certain keys, e.g. spave bar; But we shouldn't handle preview keydown directly because we cannot tell whether those are actual keystrokes inside a textbox

        // Surprisingly for usercontrol to receive keydown its elements need to have focus
        public void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            #region View Related
            if (e.Key == Key.F9) { ZoomDefaultScale(); e.Handled = true; }
            else if (e.Key == Key.System && e.SystemKey == Key.F10) { ZoomToFit(); e.Handled = true; }
            #endregion
            #region Alignment and Distribution
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        AlignLeftSelections();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        AlignRightSelections();
                        e.Handled = true;
                        break;
                    case Key.Up:
                        AlignUpSelections();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        AlignBottomSelections();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.Delete)
            {
                DeleteSelections();
                e.Handled = true;
            }
            #endregion
            #region Access Short Cuts
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.A:
                        SelectAll();
                        e.Handled = true;
                        break;
                    case Key.L:
                        ConnectNodes();
                        e.Handled = true;
                        break;
                    case Key.F:
                        SearchPanel.Visibility = Visibility.Visible;
                        SearchTextBox.Focus();
                        SearchTextBox.SelectAll();
                        e.Handled = true;
                        break;
                }
            }
            else if (e.Key == Key.F6) { ConnectNodes(); e.Handled = true; }
            else if (e.Key == Key.Space)
            {
                // Show Memoir at location
                ShowMemoirBar();
            }
            #endregion
        }
        #endregion
        #region Content Search
        int CurrentFoundNodeIndex = 0;
        string SearchString;
        List<GraphNodeView> FoundNodes = new List<GraphNodeView>();
        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (SearchTextBox.Text != SearchString)
                {
                    SearchString = SearchTextBox.Text;
                    if (string.IsNullOrWhiteSpace(SearchString)) FoundNodes.Clear();
                    else
                    {
                        FoundNodes = Nodes.Where(item => item.Contains(SearchString)).ToList();
                        CurrentFoundNodeIndex = -1;
                        CycleFoundNodes();
                    }
                }
                else CycleFoundNodes((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);
                e.Handled = true;
            }
            else if(e.Key == Key.F3)
            {
                CycleFoundNodes((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);
                e.Handled = true;
            }
            else if(e.Key == Key.Escape)
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Visibility = Visibility.Collapsed;
            e.Handled = true;
        }

        private void CycleFoundNodes(bool bReverse = false)
        {
            if (FoundNodes.Count != 0)
            {
                if(bReverse)
                {
                    if (CurrentFoundNodeIndex == -1 || CurrentFoundNodeIndex == 0) CurrentFoundNodeIndex = FoundNodes.Count - 1;
                    else CurrentFoundNodeIndex--;
                }
                else
                {
                    if (CurrentFoundNodeIndex == FoundNodes.Count - 1) CurrentFoundNodeIndex = 0;
                    else CurrentFoundNodeIndex++;
                }
                SelectNodeWithSearchHighlight(FoundNodes[CurrentFoundNodeIndex], SearchString);
            }
            else return;
        }
        #endregion
        #region Mouse
        private Point CurrentPoint;
        private bool bOnNodeImageHandle = false;
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (bOnNodeImageHandle == true) { bOnNodeImageHandle = false; return; }

            // Unselect nodes
            if (e.LeftButton == MouseButtonState.Pressed && SelectedNodes.Count() != 0 &&
                !(((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) || ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)))
            {
                UnselectAll();
            }

            Point mousePosition = Mouse.GetPosition(Window.GetWindow(this));
            // Show drag area(RMB)
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Controls.Canvas.SetLeft(DragArea, mousePosition.X);
                System.Windows.Controls.Canvas.SetTop(DragArea, mousePosition.Y);
                DragArea.Width = 0;
                DragArea.Height = 0;
                DragArea.Visibility = Visibility.Visible;
                e.Handled = true;
            }
            // Add Node (RMB)
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt
                    || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Point location = e.GetPosition(GraphLayerCanvas);    // Location is random mess
                    Point position = e.GetPosition(GraphLayerGrid);
                    CreateRichTextNodeAtCanvasLocation(new Point((position.X - CanvasTranslation.X) / CanvasScale.ScaleX, (position.Y - CanvasTranslation.Y) / CanvasScale.ScaleY));
                    e.Handled = true;
                }
                else
                {
                    // Point location = e.GetPosition(GraphLayerCanvas);    // Location is random mess
                    Point position = e.GetPosition(GraphLayerGrid);
                    CreateTextNodeAtCanvasLocation(new Point((position.X - CanvasTranslation.X) / CanvasScale.ScaleX, (position.Y - CanvasTranslation.Y) / CanvasScale.ScaleY));
                    e.Handled = true;
                }
            }
            CurrentPoint = mousePosition;
            bOnNodeImageHandle = false;
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Select Nodes using drag area
            if (DragArea.Width != 0 && DragArea.Height != 0)
            {
                Point dragAreaLocation = new Point(System.Windows.Controls.Canvas.GetLeft(DragArea), System.Windows.Controls.Canvas.GetTop(DragArea));
                // Convert drag area rect to GraphLayerCanvas space
                Rect dragArearect = new Rect(dragAreaLocation.X, dragAreaLocation.Y, DragArea.ActualWidth, DragArea.ActualHeight);

                // Add to new selection
                List<GraphNodeView> intersectNodes = new List<GraphNodeView>();
                foreach (GraphNodeView item in Nodes)
                {
                    FrameworkElement view = item.VisualElement;

                    Point nodeLocation = view.TransformToVisual(GraphLayerGrid).Transform(new Point(0, 0));
                    Rect nodeRect = new Rect(nodeLocation.X, nodeLocation.Y, view.ActualWidth * CanvasScale.ScaleX, view.ActualHeight * CanvasScale.ScaleX);  // Notice RenderSize is calculated only for the visual itself's render transform, not after its parents
                    if (dragArearect.Contains(nodeRect))
                    {
                        intersectNodes.Add(item);
                    }
                }

                // Select
                SelectNodes(intersectNodes);
            }

            // Hide drag area
            DragArea.Visibility = Visibility.Hidden;
            DragArea.Width = DragArea.Height = 0;
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            #region Drag Selection (LMB)
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // get current mouse position relative to Canvas
                Point mousePosition = Mouse.GetPosition(GraphLayerGrid);
                double dx = 0, dy = 0;
                // If we are dragging toward right (with fixed LeftProperty)
                if (DragArea.ReadLocalValue(System.Windows.Controls.Canvas.LeftProperty) != DependencyProperty.UnsetValue)
                {
                    double prevX = System.Windows.Controls.Canvas.GetLeft(DragArea);
                    dx = mousePosition.X - prevX;

                    if (dx < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.LeftProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetRight(DragArea, GraphLayerGrid.ActualWidth - prevX);
                        dx = -dx;
                    }
                }
                else // We are dragging toward left (with fixed RightProperty)
                {
                    double prevX = System.Windows.Controls.Canvas.GetRight(DragArea);
                    dx = GraphLayerGrid.ActualWidth - mousePosition.X - prevX;

                    if (dx < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.RightProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetLeft(DragArea, GraphLayerGrid.ActualWidth - prevX);
                        dx = -dx;
                    }
                }
                // If we are dragging toward bottom (with fixed TopProperty)
                if (DragArea.ReadLocalValue(System.Windows.Controls.Canvas.TopProperty) != DependencyProperty.UnsetValue)
                {
                    double prevY = System.Windows.Controls.Canvas.GetTop(DragArea);
                    dy = mousePosition.Y - prevY;
                    if (dy < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.TopProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetBottom(DragArea, GraphLayerGrid.ActualHeight - prevY);
                        dy = -dy;
                    }
                }
                // We are draggin toward bottom (with fixed BottomProperty)
                else
                {
                    double prevY = System.Windows.Controls.Canvas.GetBottom(DragArea);
                    dy = GraphLayerGrid.ActualHeight - mousePosition.Y - prevY;
                    if (dy < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.BottomProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetTop(DragArea, GraphLayerGrid.ActualHeight - prevY);
                        dy = -dy;
                    }
                }
                // Set relative width/height to a positive number
                DragArea.Width = dx;
                DragArea.Height = dy;
                e.Handled = true;
            }
            #endregion
            #region Pan (MMB)
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point position = e.GetPosition(Window.GetWindow(this));
                TranslateCanvas(position.X - CurrentPoint.X, position.Y - CurrentPoint.Y);
                CurrentPoint = position;
                e.Handled = true;
            }
            #endregion
        }
        #endregion
        #region Node Translation Handling and Related
        private bool bTranslating = false;
        private Point TranlsationInitiationPoint;
        public void NodeImageHandle_MouseDown(GraphNodeView node, Image controlHandle, MouseButtonEventArgs e)
        {
            // Add to Bookmark with Alt
            if (e.LeftButton == MouseButtonState.Pressed & ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                AddBookmark(node);
                e.Handled = true;
            }
            // Translation using LMB and MMB
            else if (e.LeftButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed)
            {
                TranlsationInitiationPoint = CurrentPoint = e.GetPosition(Window.GetWindow(this));
                bTranslating = true;
                controlHandle.CaptureMouse();

                bOnNodeImageHandle = true;
                e.Handled = true;
            }
            // Delete Node using RMB
            else if(e.RightButton == MouseButtonState.Pressed)
            {
                DeleteNode(node);

                bOnNodeImageHandle = true;
                e.Handled = true;
            }
        }

        public void NodeImageHandle_MouseUp(GraphNodeView node, Image controlHandle, MouseButtonEventArgs e)
        {
            // Select nodes with/without Shift
            Point position = e.GetPosition(Window.GetWindow(this));
            if (((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) 
                || (TranlsationInitiationPoint == position))
            {
                ToggleSelection(node);
            }

            // Done with translation
            bTranslating = false;
            controlHandle.ReleaseMouseCapture();
            e.Handled = true;
        }

        internal void NodeImageHandle_MouseMove(GraphNodeView node, MouseEventArgs e)
        {
            if (bTranslating == true && (e.LeftButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Pressed))
            {
                Point position = e.GetPosition(Window.GetWindow(this));
                if (SelectedNodes.Count != 0 && SelectedNodes.Contains(node))   // Translate all selection only when we are selected as well
                {
                    TranslateNodes(SelectedNodes, position.X - CurrentPoint.X, position.Y - CurrentPoint.Y);
                    CurrentPoint = position;
                    e.Handled = true;
                }
                else
                {
                    TranslateNode(node, position.X - CurrentPoint.X, position.Y - CurrentPoint.Y);
                    CurrentPoint = position;
                    e.Handled = true;
                }
            }
        }
        private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double magicalScale = 120 * 3;
            double delta = (e.Delta / magicalScale);
            Point position = e.GetPosition(GraphLayerGrid);/*e.GetPosition(RenderCanvas) is random mess I don't know what it used to do the calculation, does it count translation and scale or not*/
            Zoom(delta, new Point(position.X - CanvasTranslation.X, position.Y - CanvasTranslation.Y));
        }
        #endregion
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

        internal void UpdateReferenceNodeDescription(GraphNodeView refNode)
        {
            // Notify update of users of the node
            foreach (GraphNodeView view in Nodes)
            {
                if (view.Node.Type == NodeType.Jumper && view.Node.Ref == refNode.Node)
                    view.UpdateDescription();
            }
        }

        private ObservableCollection<GraphNodeView> _Bookmarks;
        public ObservableCollection<GraphNodeView> Bookmarks
        {
            get { return _Bookmarks; }
            set
            {
                if (value != _Bookmarks)
                {
                    _Bookmarks = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string DocumentName
        {
            get { return (Graph == null) ? null : Graph.Name; }    // Can be null during initliazation
            set
            {
                if (value != Graph.Name)
                {
                    Graph.Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
