using MULTITUDE.Canvas;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Popup;
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
    /// Interaction logic for ForgottenUniverseWindow.xaml
    /// </summary>
    public partial class ForgottenUniverseWindow : Window, IDocumentPropertyViewableWindow
    {        
        #region Helper Class
        class FadingElement
        {
            public FadingElement(Document doc, Random generator, double availableWidth, double availableHeight, Panel participatingCanvas)
            {
                // Assign
                Doc = doc;
                Canvas = participatingCanvas;
                bHovered = false;
                bBeingInspected = false;
                // Create UI Element <Pending>
                switch (Doc.Type)
                {
                    case DocumentType.PlainText:
                        break;
                    case DocumentType.MarkdownPlus:
                        break;
                    case DocumentType.Archive:
                        break;
                    case DocumentType.VirtualArchive:
                        break;
                    case DocumentType.DataCollection:
                        break;
                    case DocumentType.Graph:
                        break;
                    case DocumentType.Command:
                        break;
                    case DocumentType.Web:
                        break;
                    case DocumentType.PlayList:
                        break;
                    case DocumentType.ImagePlus:
                        break;
                    case DocumentType.Sound:
                        break;
                    case DocumentType.Video:
                        break;
                    case DocumentType.VirtualWorkspace:
                        break;
                    case DocumentType.Others:
                        break;
                    case DocumentType.Unkown:
                        break;
                    default:
                        break;
                }
                // Debug creation
                Icon = new TextBlock();
                ((TextBlock)Icon).Text = Doc.ShortDescription;
                double initialMargin = 60;
                System.Windows.Controls.Canvas.SetLeft(Icon, generator.Next((int)initialMargin, (int)(availableWidth - initialMargin)));
                System.Windows.Controls.Canvas.SetTop(Icon, generator.Next((int)initialMargin, (int)(availableHeight - initialMargin)));

                //// Debug creation
                //double initX = generator.Next((int)InitialMargin, (int)(availableWidth - InitialMargin));
                //double initY = generator.Next((int)InitialMargin, (int)(availableHeight - InitialMargin));
                //// Generate a new icon
                //Icon = new IconBase(doc, new MULTITUDE.Class.DocumentIcon(doc.GUID, new Class.IconArea(new Class.CanvasRelativeLocation(Class.RelativeLocation.UpperLeft, initX, initY), IconBase.DefaultIconSize, IconBase.DefaultIconSize)));
                //// Set appropriate Canavs location and size
                //System.Windows.Controls.Canvas.SetLeft(Icon, initX);
                //System.Windows.Controls.Canvas.SetTop(Icon, initY);
                //// Adjust size
                //Icon.Width = IconBase.DefaultIconSize;
                //Icon.Height = IconBase.DefaultIconSize;

                // Tag
                Icon.Tag = this;
            }

            public FrameworkElement Icon { get; }
            public Document Doc { get; set; }
            public Panel Canvas { get; set; }
            public bool bHovered { get; set; }
            public bool bBeingInspected { get; set; }
        }
        #endregion

        #region Configurations
        private static double InitialMargin = 60;
        const double FadingElementDecrementStepSize = 0.01;
        const int CounterTimeStepSize = 25;  // In ms; 33 is ok, slower is laggy, faster is too quick
        const int AnimatedElementsCount = 10;
        #endregion

        #region Control Data
        // State
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private Random rnd;
        private bool bShowCluelessDocuments = false;

        // Book keeping
        private FadingElement[] CurrentAnimatedElements;
        private List<FadingElement> CurrentDisplayedDocuments;
        private FadingElement CurrentInspection = null;
        // Deferred update
        List<Document> ForgottenDocumentsRef;
        // Document Property Panel
        Popup.DocumentPropertyPanel DocPanel;
        #endregion

        #region Intialization
        public ForgottenUniverseWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;

            // Setup animation timer
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();  // In WPF don't use Threading.Timer
            dispatcherTimer.Tick += DispatcherTimer_TimeUp;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, CounterTimeStepSize);

            // Create random number generator
            rnd = new Random();

            // Initialize pointers
            CurrentAnimatedElements = new FadingElement[AnimatedElementsCount];
            ForgottenDocumentsRef = Home.Current.ForgottenUniverse;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DocPanel = new Popup.DocumentPropertyPanel(this);
            // Post-Loading
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            // Clearn up
            for (int i = 0; i < AnimatedElementsCount; i++) CurrentAnimatedElements[i] = null;
            DocumentCanvas.Children.Clear();
            // Reorder stuff
            ForgottenDocumentsRef = ForgottenDocumentsRef.OrderByDescending(o => o.CreationDate).ToList();  // https://stackoverflow.com/questions/319973/how-to-get-first-n-elements-of-a-list-in-c
            // Pick 100 least-recent documents from home to add to display: 50 to front, 50 to decorative
            CurrentDisplayedDocuments = new List<FadingElement>();
            for (int i = 0; i < (ForgottenDocumentsRef.Count > 100 ? 100 : ForgottenDocumentsRef.Count); i++)
            {
                if (i < 50)
                    CurrentDisplayedDocuments.Add(new FadingElement(ForgottenDocumentsRef[i], rnd, DocumentCanvas.ActualWidth, DocumentCanvas.ActualHeight, DocumentCanvas));
                else
                    CurrentDisplayedDocuments.Add(new FadingElement(ForgottenDocumentsRef[i], rnd, DecorativeDocumentCanvas.ActualWidth, DecorativeDocumentCanvas.ActualHeight, DecorativeDocumentCanvas));
            }
            // Add to display and attach extra event handlers
            foreach (FadingElement element in CurrentDisplayedDocuments)
            {
                element.Canvas.Children.Add(element.Icon);
                element.Icon.MouseMove += Icon_MouseMove;
                element.Icon.MouseLeave += Icon_MouseLeave;
                element.Icon.MouseLeftButtonDown += Icon_MouseLeftButtonDown;
            }
            // Update statistics
            ForgottenDocumentCountLabel.Content = string.Format("Total un-clued documents: {0}", ForgottenDocumentsRef.Count);
            // Start animation
            for (int i = 0; i < (AnimatedElementsCount < ForgottenDocumentsRef.Count ? AnimatedElementsCount : ForgottenDocumentsRef.Count); i++)
            {
                CurrentAnimatedElements[i] = CurrentDisplayedDocuments[rnd.Next(0, CurrentDisplayedDocuments.Count - 1)];   // Perfetly well if repeating a previous one: it's just its fading animation plays faster than others
            }
            dispatcherTimer.Start();
        }

        private void ToggleShowClueless_Click(object sender, RoutedEventArgs e)
        {
            if(bShowCluelessDocuments == false)
            {
                ForgottenDocumentsRef = new List<Document>();
                ForgottenDocumentsRef.AddRange(Home.Current.ForgottenUniverse);
                ForgottenDocumentsRef.AddRange(Home.Current.Documents.Where(item => item.Clues.Count == 0));
                ForgottenDocumentsRef = ForgottenDocumentsRef.Distinct().ToList();
                RefreshDisplay();
                bShowCluelessDocuments = true;
            }
            else
            {
                ForgottenDocumentsRef = Home.Current.ForgottenUniverse;
                RefreshDisplay();
                bShowCluelessDocuments = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }
        #endregion

        #region UI Animation
        // Inisted of floating animation we can also just use some in-place dynamic animation

        // https://stackoverflow.com/questions/17786771/random-double-between-given-numbers
        // https://stackoverflow.com/questions/1064901/random-number-between-2-double-numbers
        private static double RandomDouble(Random generator, double minValue, double maxValue)
        {
            var next = generator.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

        private void DispatcherTimer_TimeUp(object sender, EventArgs e)
        {
            // Animate: Fade and replace
            for (int i = 0; i < CurrentAnimatedElements.Length; i++)
            {
                FadingElement element = CurrentAnimatedElements[i];
                if (element == null) continue;

                if (element.bHovered == false && element.bBeingInspected == false)
                {
                    element.Icon.Opacity -= RandomDouble(rnd, FadingElementDecrementStepSize / 2, FadingElementDecrementStepSize * 2);
                    if (element.Icon.Opacity <= 0.1)
                    {
                        element.Icon.Opacity = 1;
                        // Replace
                        int index = CurrentDisplayedDocuments.IndexOf(element);
                        if (CurrentDisplayedDocuments.Count < ForgottenDocumentsRef.Count)   // Replace current with a document in list that are not displayed
                        {
                            List<Document> replaceList = ForgottenDocumentsRef.Where(p => !CurrentDisplayedDocuments.Any(p2 => p2.Doc == p)).ToList();
                            replaceList.Add(element.Doc);
                            FadingElement replaceElement = new FadingElement(replaceList[rnd.Next(0, replaceList.Count - 1)], rnd, element.Canvas.ActualWidth, element.Canvas.ActualHeight, element.Canvas);
                            CurrentDisplayedDocuments.RemoveAt(index);
                            CurrentDisplayedDocuments.Add(replaceElement);
                            CurrentAnimatedElements[i] = replaceElement;
                        }
                        else // Change its location
                        {
                            // Generate a new location
                            double newX = rnd.Next((int)InitialMargin, (int)(element.Canvas.ActualWidth - InitialMargin));
                            double newY = rnd.Next((int)InitialMargin, (int)(element.Canvas.ActualHeight - InitialMargin));
                            System.Windows.Controls.Canvas.SetLeft(element.Icon, newX);
                            System.Windows.Controls.Canvas.SetTop(element.Icon, newY);
                            // Repalce animated with another one
                            CurrentAnimatedElements[i] = CurrentDisplayedDocuments[rnd.Next(0, CurrentDisplayedDocuments.Count - 1)];
                        }
                    }
                }
                else
                {
                    element.Icon.Opacity = 1;
                }
            }
            // Automatically restart, not need to explicitly reset timer
        }
        #endregion

        #region UI Interaction
        private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FadingElement element = (sender as FrameworkElement).Tag as FadingElement;
            DocPanel.Update(element.Doc, this);
            element.bBeingInspected = true;

            // Update state
            if (CurrentInspection != null) CurrentInspection.bBeingInspected = false;
            CurrentInspection = element;
        }

        private void Icon_MouseLeave(object sender, MouseEventArgs e)
        {
            FadingElement element = (sender as FrameworkElement).Tag as FadingElement;

            // Set state
            element.bHovered = false;

            // Undo smothing <Pending>
            switch (element.Doc.Type)
            {
                case DocumentType.PlainText:
                    break;
                case DocumentType.MarkdownPlus:
                    break;
                case DocumentType.Archive:
                    break;
                case DocumentType.VirtualArchive:
                    break;
                case DocumentType.DataCollection:
                    break;
                case DocumentType.Graph:
                    break;
                case DocumentType.Command:
                    break;
                case DocumentType.Web:
                    break;
                case DocumentType.PlayList:
                    break;
                case DocumentType.ImagePlus:
                    break;
                case DocumentType.Sound:
                    break;
                case DocumentType.Video:
                    break;
                case DocumentType.VirtualWorkspace:
                    break;
                case DocumentType.Others:
                    break;
                case DocumentType.Unkown:
                    break;
                default:
                    break;
            }
        }

        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            FadingElement element = (sender as FrameworkElement).Tag as FadingElement;

            // Set state
            element.bHovered = true;

            // Stop fading it and find a substitute
            // ....

            // Do smothing <Pending>
            switch (element.Doc.Type)
            {
                case DocumentType.PlainText:
                    break;
                case DocumentType.MarkdownPlus:
                    break;
                case DocumentType.Archive:
                    break;
                case DocumentType.VirtualArchive:
                    break;
                case DocumentType.DataCollection:
                    break;
                case DocumentType.Graph:
                    break;
                case DocumentType.Command:
                    break;
                case DocumentType.Web:
                    break;
                case DocumentType.PlayList:
                    break;
                case DocumentType.ImagePlus:
                    break;
                case DocumentType.Sound:
                    break;
                case DocumentType.Video:
                    break;
                case DocumentType.VirtualWorkspace:
                    break;
                case DocumentType.Others:
                    break;
                case DocumentType.Unkown:
                    break;
                default:
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public void DeleteDocumentFromView(object document)
        {
            if (document is Document == false) throw new InvalidOperationException("Object isn't a document type.");

            // Hide Panel
            DocPanel.Visibility = Visibility.Collapsed;

            // Remove document from home
            if (CurrentInspection == null) throw new InvalidOperationException("The doucment property panel should have been hidden because no corresponding doucment exist.");
            Home.Current.Delete(CurrentInspection.Doc);

            // Reload and refresh
            if (bShowCluelessDocuments == true)
            {
                ForgottenDocumentsRef = new List<Document>();
                ForgottenDocumentsRef.AddRange(Home.Current.ForgottenUniverse);
                ForgottenDocumentsRef.AddRange(Home.Current.Documents.Where(item => item.Clues.Count == 0));
                ForgottenDocumentsRef = ForgottenDocumentsRef.Distinct().ToList();
                RefreshDisplay();
            }
            else
            {
                ForgottenDocumentsRef = Home.Current.ForgottenUniverse;
                RefreshDisplay();
            }
        }
        #endregion
    }
}
