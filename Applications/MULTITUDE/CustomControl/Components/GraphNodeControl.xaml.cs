using MULTITUDE.Canvas;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.CustomControl.CanvasSpaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
    /// <summary>
    /// Interaction logic for GraphNodeControl.xaml
    /// </summary>
    public partial class GraphNodeControl : UserControl, INotifyPropertyChanged
    {
        internal GraphNodeControl(GraphNodeView view, GraphLayer manager)
        {
            View = view;    // The visual element of the view is this
            Node = view.Node;
            _OwnerGraphLayer = manager;
            InitializeComponent();

            // Initialize resources
            if(bResourceInitialized == false)
            {
                JumperImageStyle = NodeImage.FindResource("JumperStyleImage") as Style;
                TextNodeStyle = this.FindResource("TextNode") as Style;
                TextNodeHighLightStyle = this.FindResource("TextNodeHighlight") as Style;
                ImageNodeStyle = this.FindResource("ImageNode") as Style;
                ImageNodeHighLightStyle = this.FindResource("ImageNodeHighlight") as Style;
                JumperNodeStyle = this.FindResource("JumperNode") as Style;
                JumperNodeHighlightStyle = this.FindResource("JumperNodeHighlight") as Style;
                bResourceInitialized = true;
            }

            // Switch Style
            switch (Node.Type)
            {
                case NodeType.SimpleText:
                    // Do nothing more setup, default assumed
                    // Show 
                    NodeTextExpander.Visibility = Visibility.Visible;
                    break;
                case NodeType.Link:
                    SetupInterfaceForLink();
                    break;
                case NodeType.RichFlowText:
                    SetupInterfaceForFlowText();
                    break;
                case NodeType.Jumper:
                    SetupInterfaceForJumper();
                    break;
            }
        }

        private void NodeImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Node.Type != NodeType.Jumper) throw new InvalidOperationException("Node isn't a jumper type.");
            _OwnerGraphLayer.JumpViewToNode(Node.Ref);
        }

        private void NodeImage_MouseEnter(object sender, MouseEventArgs e)
        {
            // Update tooltip for jumper
            if(Node.Type == NodeType.Jumper)
            {
                NodeImage.ToolTip = "Jump to " + Node.Ref.View.Description;
            }
        }

        private void SetupInterfaceForJumper()
        {
            NodeImage.Source = JumperNodeIcon;
            NodeImage.Width = JumperNodeIcon.PixelWidth;
            NodeImage.Height = JumperNodeIcon.PixelHeight;
            // Style
            NodeImage.Style = JumperImageStyle;
            // Show 
            NodeImage.Visibility = Visibility.Visible;
            // Tool tip
            NodeImage.ToolTip = "Jump to " + Node.Ref.View.Description;
        }

        private void SetupInterfaceForFlowText()
        {
            // Interface
            MarkdownPlusDocument.Visibility = Visibility.Visible;
            MarkdownPlusDocument.Update(View.Node.MDDocument, true, CanvasSpaceWindow.ContentMode.FlowDocument);    // We are just using the doucment declared in the node, so no need to explicitly save it
            // Adjust size
            MarkdownPlusDocument.Width = 350;
        }

        private void SetupInterfaceForLink()
        {
            Document doc = Home.Current.GetDocument(View.Node.LinkGUID);
            switch (doc.Type)
            {
                case DocumentType.ImagePlus:
                    // Image: BitmapImage set source to loaded image
                    // Blocked
                    BitmapImage image = LoadImage(doc as ImagePlus);
                    NodeImage.Source = image;
                    NodeImage.Width = image.PixelWidth;
                    NodeImage.Height = image.PixelHeight;
                    // Show 
                    NodeImage.Visibility = Visibility.Visible;
                    break;
                case DocumentType.PlainText:
                    // TextBlock: Show Content
                    TextReferenceBlock.Text = (doc as PlainText).Content;
                    // Show
                    TextReferenceBlock.Visibility = Visibility.Visible;
                    break;
                case DocumentType.MarkdownPlus:
                    // TextBlock: Show Content
                    FlowDocumentBox.Document = (doc as MarkdownPlus).GetFlowDocument();
                    // Show
                    FlowDocumentBox.Visibility = Visibility.Visible;
                    break;
                default:
                    // TextBlock: Show Content
                    GeneralReferenceInfoLabel.Content = doc.Description;
                    HyperLink.Tag = doc;
                    // Show
                    HyperLink.IsEnabled = true;
                    TextReferenceBlock.Visibility = Visibility.Visible;
                    break;
            }
        }

        private static BitmapImage LoadImage(ImagePlus image)
        {
            BitmapImage newImage = new BitmapImage(new Uri(image.Path));    // Not using OnLoad Cache Option for optimization
            // newImage.Freeze();
            return newImage;
        }

        private GraphNodeView View;
        private GraphNode Node;
        private GraphLayer _OwnerGraphLayer;
        public GraphLayer OwnerGraphLayer { get { return _OwnerGraphLayer; } }

        #region Static References
        public static bool bResourceInitialized = false;
        public static Style JumperImageStyle;
        public static Style TextNodeStyle;
        public static Style TextNodeHighLightStyle;
        public static Style ImageNodeStyle;
        public static Style ImageNodeHighLightStyle;
        public static Style JumperNodeStyle;
        public static Style JumperNodeHighlightStyle;
        public static readonly BitmapImage JumperNodeIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        #endregion

        #region Interactions
        public void AdjustFlowDocumentPreviewSize()
        {
            if(this.Type == NodeType.Link)
            {
                // Adjust size
                double currentHeight = FlowDocumentBox.ActualHeight;
                double currentWidth = FlowDocumentBox.ActualWidth;
                FlowDocumentBox.Width = (currentHeight * 3 > currentWidth * 2) ? currentWidth = currentHeight : currentWidth;
                // Automatic methods don't work: https://stackoverflow.com/questions/2364970/measure-string-inside-richtextbox-control/2365687#2365687; https://stackoverflow.com/questions/10347518/how-to-make-a-rich-textbox-automatically-size-in-wpf
            }
        }
        public void Highlight(string searchString = null)
        {
            switch (Node.Type)
            {
                case NodeType.SimpleText:
                    NodeBorder.Style = TextNodeHighLightStyle;
                    // If highlight with text selection
                    if (searchString != null) { SimpleTextSearchTextSelect(searchString); }
                    break;
                case NodeType.Link:
                    HighlightOnNodeType(); // Depending on node type
                                           // If highlight with text selection
                    if (searchString != null) { /* Do nothing */}
                    break;
                case NodeType.RichFlowText:
                    NodeBorder.Style = TextNodeHighLightStyle;
                    // If highlight with text selection
                    if (searchString != null) { RichTextSearchTextSelect(searchString); }
                    break;
                case NodeType.Jumper:
                    NodeBorder.Style = JumperNodeHighlightStyle;
                    // If highlight with text selection
                    if (searchString != null) { /* Do nothing */}
                    break;
            }
        }
        public void Unhighlight()
        {
            switch (Node.Type)
            {
                case NodeType.SimpleText:
                    NodeBorder.Style = TextNodeStyle;
                    break;
                case NodeType.Link:
                    UnhighlightOnNodeType();
                    break;
                case NodeType.RichFlowText:
                    NodeBorder.Style = TextNodeStyle;
                    break;
                case NodeType.Jumper:
                    NodeBorder.Style = JumperNodeStyle;
                    break;
            }
        }
        private void HighlightOnNodeType()
        {
            switch (Node.Type)
            {
                case NodeType.Link:
                    // Image
                    if (Home.Current.GetDocument(Node.LinkGUID).Type == DocumentType.ImagePlus) NodeBorder.Style = ImageNodeHighLightStyle;
                    // Others
                    else NodeBorder.Style = TextNodeHighLightStyle;
                    break;
                case NodeType.Jumper:
                    throw new NotImplementedException();
                    break;
                case NodeType.SimpleText:
                case NodeType.RichFlowText:
                default:
                    NodeBorder.Style = TextNodeHighLightStyle;
                    break;
            }
        }
        private void UnhighlightOnNodeType()
        {
            switch (Node.Type)
            {
                case NodeType.Link:
                    // Image
                    if(Home.Current.GetDocument(Node.LinkGUID).Type == DocumentType.ImagePlus) NodeBorder.Style = ImageNodeStyle;
                    // Others
                    else NodeBorder.Style = TextNodeStyle;
                    break;
                case NodeType.Jumper:
                    throw new NotImplementedException();
                    break;
                case NodeType.SimpleText:
                case NodeType.RichFlowText:
                default:
                    NodeBorder.Style = TextNodeStyle;
                    break;
            }
        }
        private void SimpleTextSearchTextSelect(string searchString)
        {
            // Highlight title if available
            int index = Node.Title.ToLower().IndexOf(searchString.ToLower());
            if(index != -1)
            {
                NodeTextHeader.Select(index, searchString.Length);
            }
            else
            {
                // Highlight content if available
                index = Node.Content.ToLower().IndexOf(searchString.ToLower());
                if(index != -1)
                {
                    NodeTextContent.Select(index, searchString.Length);
                }
                // Invalid operation, because this function should be callled only when we are sure there is something to be found
                else
                {
                    throw new InvalidOperationException("Specified search string doesn't exist.");
                }
            }
        }
        private void RichTextSearchTextSelect(string searchString)
        {
            MarkdownPlusDocument.HighlightSelection(searchString);
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Document doc = (sender as Hyperlink).Tag as Document;
            if (doc.Type != DocumentType.Graph)
                (App.Current.MainWindow as VirtualWorkspaceWindow).OpenDocument(doc, false);
            else
                OwnerGraphLayer.Editor.OpenLayer(doc as Graph);
        }
        #endregion

        #region Translation Handling
        private void NodeImageHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.LeftButton == MouseButtonState.Pressed)
            {
                NodeTextExpander.IsExpanded = !(NodeTextExpander.IsExpanded);
            }

            _OwnerGraphLayer.NodeImageHandle_MouseDown(View, ControlHandle, e);
        }

        private void NodeImageHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _OwnerGraphLayer.NodeImageHandle_MouseUp(View, ControlHandle, e);
        }

        private void NodeImageHandle_MouseMove(object sender, MouseEventArgs e)
        {
            _OwnerGraphLayer.NodeImageHandle_MouseMove(View, e);
        }

        private void NodeTextExpander_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && e.LeftButton == MouseButtonState.Pressed)
            {
                Expander ex = (sender as Expander);
                ex.IsExpanded = !(ex.IsExpanded);
            }
        }
        #endregion

        #region UI Hanlding
        private void NodeTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto adjust content sizing depending on current size and header length
            if (NodeTextContent != null && NodeTextHeader != null && NodeTextContent.MaxWidth != NodeTextHeader.ActualWidth)    // Can be null during initialization
                NodeTextContent.MaxWidth = NodeTextHeader.ActualWidth;

            // Header automatically expands to any width and height; while content is limited to header width with its own expandable height; Neither uses scrolling

            // <Debug> There is some problem when header 's text is pasted - content seem to lose ability to wrap.
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

        public string NodeTitle
        {
            get { return Node.Title; }
            set
            {
                if (value != Node.Title)
                {
                    Node.Title = value;
                    NotifyPropertyChanged();

                    // Extra Notification
                    View.UpdateDescription();
                }
            }
        }
        public string NodeContent
        {
            get { return Node.Content; }
            set
            {
                if (value != Node.Content)
                {
                    Node.Content = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public NodeType Type
        {
            get { return Node.Type; }
            set
            {
                if (value != Node.Type)
                {
                    Node.Type = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
