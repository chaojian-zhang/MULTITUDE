using MULTITUDE.Class.DocumentTypes;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl
{
    /// <summary>
    /// Interaction logic for ForgottenUniverse.xaml
    /// </summary>
    public partial class ForgottenUniverse : UserControl
    {
        class FloatingDirection
        {
            public FloatingDirection(double x, double y)
            {
                dx = x;
                dy = y;
            }
            public double dx { get; set; }
            public double dy { get; set; }
        }

        class FloatingElements
        {
            public FloatingElements(Document doc, Random generator, double availableWidth, double availableHeight)
            {
                // Assign
                Doc = doc;
                // Generate
                Direction = new FloatingDirection(generator.Next(-1,1), generator.Next(-2,2));
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
                //Icon = new Label();
                //Icon.Content = "Test Content";
                //double initialMargin = 60;
                //System.Windows.Controls.Canvas.SetLeft(Icon, generator.Next((int)initialMargin, (int)(availableWidth - initialMargin)));
                //System.Windows.Controls.Canvas.SetTop(Icon, generator.Next((int)initialMargin, (int)(availableHeight - initialMargin)));

                // Debug creation
                double initialMargin = 60;
                double initX = generator.Next((int)initialMargin, (int)(availableWidth - initialMargin));
                double initY = generator.Next((int)initialMargin, (int)(availableHeight - initialMargin));
                // Generate a new icon
                Icon = new IconBase(doc, new MULTITUDE.Class.DocumentIcon(doc.GUID, new Class.IconArea(new Class.CanvasRelativeLocation(Class.RelativeLocation.UpperLeft, initX, initY), IconBase.DefaultIconSize, IconBase.DefaultIconSize)));
                // Set appropriate Canavs location and size
                System.Windows.Controls.Canvas.SetLeft(Icon, initX);
                System.Windows.Controls.Canvas.SetTop(Icon, initY);
                // Adjust size
                Icon.Width = IconBase.DefaultIconSize;
                Icon.Height = IconBase.DefaultIconSize;

                // Set up state
                bMoving = true;

                // Basic Event hookup
                Icon.MouseMove += Icon_MouseMove;
                Icon.MouseLeave += Icon_MouseLeave;
            }

            private void Icon_MouseLeave(object sender, MouseEventArgs e)
            {
                bMoving = true;

                // Do smothing <Pending>
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
            }

            private void Icon_MouseMove(object sender, MouseEventArgs e)
            {
                bMoving = false;

                // Undo smothing <Pending>
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
            }

            public FrameworkElement Icon { get; }
            public FloatingDirection Direction { get; }
            public bool bMoving { get; set; }
            Document Doc;
        }

        public ForgottenUniverse(MULTITUDE.Canvas.VirtualWorkspaceWindow summoner)
        {
            InitializeComponent();
            Summoner = summoner;

            // Setup animation timer
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();  // In WPF don't use Threading.Timer
            dispatcherTimer.Tick += DispatcherTimer_TimeUp;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 25);   // 33ms

            // Create random number generator
            rnd = new Random();
        }

        private void DispatcherTimer_TimeUp(object sender, EventArgs e)
        {
            //// Animation 1: Translate
            //// Animate
            //foreach (FloatingElements element in DisplayList)
            //{
            //    if(element.bMoving == true)
            //        TranslateElement(element.Icon, element.Direction.dx, element.Direction.dy);
            //}

            // Animate 2: Fade and rechoose

            // Automatically restart, not need to explicitly reset timer
        }

        private void TranslateElement(FrameworkElement element, double dx, double dy)
        {
            // Get next value
            double newX = (double)element.GetValue(System.Windows.Controls.Canvas.LeftProperty) + dx;
            double newY = (double)element.GetValue(System.Windows.Controls.Canvas.TopProperty) + dy;
            // Clamp direction in available area, wrap around
            if (dx > 0 && newX > ForgottenCanvas.ActualWidth) newX = -element.ActualWidth;
            if (dx < 0 && newX + element.ActualWidth < 0) newX = ForgottenCanvas.ActualWidth;
            if (dy > 0 && newY > ForgottenCanvas.ActualHeight) newY = -element.ActualHeight;
            if (dy < 0 && newY + element.ActualHeight< 0) newY = ForgottenCanvas.ActualHeight;
            // Set new location
            System.Windows.Controls.Canvas.SetLeft(element, newX);
            System.Windows.Controls.Canvas.SetTop(element, newY);
        }

        // Book keeping
        private MULTITUDE.Canvas.VirtualWorkspaceWindow Summoner { get; }
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        // private List<FloatingElements> DisplayList;
        private Random rnd;
        // Deferred update
        List<Document> forgottenDocumentsRef;

        internal void Update(ref List<Document> forgottenDocs)
        {
            // Reorder stuff
            forgottenDocs = forgottenDocs.OrderByDescending(o => o.CreationDate).ToList();  // https://stackoverflow.com/questions/319973/how-to-get-first-n-elements-of-a-list-in-c

            // Save a ref
            forgottenDocumentsRef = forgottenDocs;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Pick 100 least-recent documents from 
            DisplayList = new List<FloatingElements>();
            for (int i = 0; i < (sortedList.Count > 100 ? 100 : sortedList.Count); i++)
            {
                DisplayList.Add(new FloatingElements(sortedList[i], rnd, ForgottenCanvas.ActualWidth, ForgottenCanvas.ActualHeight));
            }
            // Add to display and attach extra event handlers
            foreach (FloatingElements element in DisplayList)
            {
                ForgottenCanvas.Children.Add(element.Icon);
                element.Icon.MouseMove += Icon_MouseMove;
                element.Icon.MouseLeave += Icon_MouseLeave;
            }
            // Update statistics
            ForgottenDocumentCountLabel.Content = string.Format("Total un-clued documents: {0}", forgottenDocumentsRef.Count);
            // Start animation
            dispatcherTimer.Start();
        }

        private void Icon_MouseLeave(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Inisted of floating animation we can also just use some in-place dynamic animation
    }
}
