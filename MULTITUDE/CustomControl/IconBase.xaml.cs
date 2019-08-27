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
using MULTITUDE.CustomControl.DocumentIcons;

// + IcojBase small icon hover animation: Type on upper left corner, creation date on upper right corner, document name on surface (lower section), with a shadowy cast

namespace MULTITUDE.CustomControl
{
    /// <summary>
    /// Interaction logic for IconBase.xaml
    /// </summary>
    public partial class IconBase : UserControl
    {
        #region Configuration Information
        enum IconState
        {
            TypeIndication,
            ContentPreview
        }
        // Shared resources
        public static readonly double DefaultDocumentIconSize = 64; // For display purpose in Search bar
        public static readonly double DefaultCanvasIconSize = 128;  // For display purpose in Canvas
        public static readonly double DefaultBigIconDimension = (DefaultCanvasIconSize * 2);
        // Small Icons
        internal static readonly BitmapImage PlainTextSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/TextSmallIcon.png"));
        internal static readonly BitmapImage MarkdownPlusSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage ArchiveSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage VirtualArchiveSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage DataCollectionSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage GraphSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage CommandSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage WebSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage PlayListSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage ImagePlusSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage SoundSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage VideoSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage OthersSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        internal static readonly BitmapImage UnkownSmallIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Backbutton.png"));
        // Abstract Icons
        internal static readonly BitmapImage TextVirtualIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/Abstract Icon.png"));
        internal static readonly BitmapImage ArchiveVirtualIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/Folder(Closed) Icon.png"));
        internal static readonly BitmapImage MediaVirtualIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/PlaylistIcon.png"));
        internal static readonly BitmapImage GeneralVirtualIcon = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/File Icon.png"));
        #endregion

        #region Construction and Data
        // Constructor
        internal IconBase(Document doc, MULTITUDE.Class.DocumentIcon iconInfo)
        {
            Document = doc;
            InitializeComponent();
            IconInfo = iconInfo;
        }

        // Book keeping
        internal Document Document { get; set; }
        internal MULTITUDE.Class.DocumentIcon IconInfo { get; set; }    // For updating size and location information related to this icon; Size change handled here, location change however handled by CnavasWindow because LayoutChanged envent might be expensive
        private IconState iconState = IconState.TypeIndication;
        private FrameworkElement previewControl = null;
        #endregion

        #region Sizing and location handling        
        // Interface event handling
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Feedback to state
            IconInfo.Occupation.Width = this.ActualWidth;
            IconInfo.Occupation.Height = this.ActualHeight;

            // Adjust according to document type and current size
            if (this.ActualHeight >= DefaultBigIconDimension || this.ActualWidth >= DefaultBigIconDimension)
            {
                if (iconState == IconState.TypeIndication) { UpdateIcon(IconState.ContentPreview); }
                UpdateToolTip(true);
            }
            else
            {
                if (iconState == IconState.ContentPreview) { UpdateIcon(IconState.TypeIndication); }
                UpdateToolTip(false);
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private Point PrevMouseDownPosition;    // For resizing
        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if(bResizing && e.LeftButton == MouseButtonState.Pressed)
            {
                // Handle resizing - notice mosue is captured
                // Get increment
                Point position = e.GetPosition(Window.GetWindow(this)); // Don't just use this's relative location because after resizing things might change and cause repeated mouse move events
                double dx = position.X - PrevMouseDownPosition.X;
                double dy = position.Y - PrevMouseDownPosition.Y;
                // Calculate target
                double targetWidth = this.ActualWidth + dx;
                double targetHeight = this.ActualHeight + dy;
                if (targetWidth < IconBase.DefaultCanvasIconSize) targetWidth = IconBase.DefaultCanvasIconSize;
                if (targetHeight < IconBase.DefaultCanvasIconSize) targetHeight = IconBase.DefaultCanvasIconSize;
                // Shift mode swtich
                if(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    double widthOffset = (targetWidth % DefaultDocumentIconSize);
                    double heightOffset = (targetHeight % DefaultDocumentIconSize);
                    targetWidth -= widthOffset;
                    targetHeight -= heightOffset;
                    position.X -= widthOffset;  // Compensate simulated scale
                    position.Y -= heightOffset;
                }
                // Set properties
                this.Width = targetWidth;
                this.Height = targetHeight;
                // Book keeping
                PrevMouseDownPosition = position;
                // Avoid translation handling in VW - Also support translation because at quadrants other than first one we need some extra work
                e.Handled = true;
            }
        }

        private static double ResizingBorderTolerance = 20; // In pixels
        private bool bResizing;
        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get resizing condition
            Point location = e.GetPosition(this);
            //if ((location.X <= ResizingBorderTolerance && (location.Y <= ResizingBorderTolerance || this.ActualHeight - location.Y <= ResizingBorderTolerance)) ||
            //        (this.ActualWidth - location.X <= ResizingBorderTolerance && (this.ActualHeight - location.Y <= ResizingBorderTolerance || location.Y <= ResizingBorderTolerance)))
            if ((this.ActualHeight - location.Y <= ResizingBorderTolerance) && (this.ActualWidth - location.X <= ResizingBorderTolerance))  // We only allow lower right corner-resizing, otherwise logic above in UserControl_MouseMove() needs to consider which corner user's mouse iniitally clicked
            {
                bResizing = true;
                // e.Handled = true;    // So VWWindow has a chance to mark this document icon as selected
                PrevMouseDownPosition = e.GetPosition(Window.GetWindow(this)); // Use a different position
                this.CaptureMouse();
            }
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(bResizing)
            {
                bResizing = false;
                this.ReleaseMouseCapture();
            }
        }
        #endregion

        #region Loading and Document Updating
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Add document specific content
            // Update Icon
            // UpdateIcon(IconState.TypeIndication);    // Already called during icon generation setting width and height in SizeChanged event
            // Update Tooltip
            UpdateToolTip();
        }
        #endregion

        #region Icon Update
        private void UpdateToolTip(bool bHide = false)
        {
            ToolTipCluesPanel.ItemsSource = Document.Clues;

            if(bHide || (Document.Clues.Count == 0 && string.IsNullOrWhiteSpace(Document.Name) && string.IsNullOrWhiteSpace(Document.Comment)))
            {
                ToolTipNameLabel.Visibility = Visibility.Collapsed;
                ToolTipCommentLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (Document.Name != null)
                {
                    ToolTipNameLabel.Visibility = Visibility.Visible;
                    ToolTipNameLabel.Content = "Name: " + Document.Name;
                }
                else ToolTipNameLabel.Visibility = Visibility.Collapsed;
                if (Document.Comment != null)
                {
                    ToolTipCommentLabel.Visibility = Visibility.Visible;
                    ToolTipCommentLabel.Content = Document.Comment;
                }
                else ToolTipCommentLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateIcon(IconState newState)
        {
            iconState = newState;

            switch (iconState)
            {
                case IconState.TypeIndication:
                    // Show icon image
                    IconImage.Source = Document.SmallIcon;
                    IconImageBorder.Visibility = Visibility.Visible;

                    // Hide previously shown previewControl
                    if (previewControl != null)
                        previewControl.Visibility = Visibility.Collapsed;
                    break;
                case IconState.ContentPreview:
                    // Hide icon image
                    IconImageBorder.Visibility = Visibility.Collapsed;

                    // Show a previouly hidden preview control or generate a new one
                    if (previewControl != null)
                        previewControl.Visibility = Visibility.Visible;
                    else
                    {
                        previewControl = GeneratePreviewIcon();
                        IconContentGrid.Children.Add(previewControl);
                    }
                    break;
            }
        }

        private UserControl GeneratePreviewIcon()
        {
            switch (Document.Type)
            {
                case DocumentType.PlainText:
                    return new PlainTextDocumentIcon(Document);
                case DocumentType.MarkdownPlus:
                    return new MarkdownPlusDocumentIcon(Document, IconInfo);
                case DocumentType.Archive:
                    return new ArchiveDocumentIcon(Document);
                case DocumentType.VirtualArchive:
                    return new VirtualArchiveDocumentIcon(Document);
                case DocumentType.DataCollection:
                    return new DataCollectionDocumentIcon(Document);
                case DocumentType.Graph:
                    return new GraphDocumentIcon(Document);
                case DocumentType.Command:
                    return new CommandDocumentIcon(Document);
                case DocumentType.Web:
                    return new WebDocumentIcon(Document);
                case DocumentType.PlayList:
                    return new PlayListDocumentIcon(Document);
                case DocumentType.ImagePlus:
                    return new ImagePlusDocumentIcon(Document);
                case DocumentType.Sound:
                    return new PlayListDocumentIcon(Document);
                case DocumentType.Video:
                    return new PlayListDocumentIcon(Document);
                case DocumentType.VirtualWorkspace:
                    return new VirtualArchiveDocumentIcon(Document);
                case DocumentType.Others:
                    return new OthersDocumentIcon(Document);
                case DocumentType.Unkown:
                    return new UnknownDocumentIcon(Document);
            }
            return null;
        }
        #endregion

        #region Interaction Effect
        internal void HighlightSelection()
        {
            SelectionBorder.Visibility = Visibility.Visible;
        }

        internal void UnhighlightSelection()
        {
            SelectionBorder.Visibility = Visibility.Hidden;
        }
        internal void RequestSave()
        {
            Document.RequestSave();
        }
        #endregion

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MULTITUDE.Canvas.VirtualWorkspaceWindow.CurrentWindow.OpenDocument(Document, false);
            e.Handled = true;
        }
    }
}
