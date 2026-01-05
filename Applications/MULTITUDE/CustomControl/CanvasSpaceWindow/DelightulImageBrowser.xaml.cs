using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility.ClueManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl.CanvasSpaceWindow
{
    public enum CurrentImageViewerMode
    {
        Navigator,
        Presentation
    }

    /// <summary>
    /// Interaction logic for DelightfulImageBrowser.xaml
    /// </summary>
    public partial class DelightfulImageBrowser : Window, INotifyPropertyChanged
    {
        #region Construction and Interaction Interface with Other Components
        public DelightfulImageBrowser(Window owner)
        {
            InitializeComponent();
            Owner = owner;

            PresentationImages = new ObservableCollection<BitmapImage>();

            // Bind events to ClueFilterComboBox to get updates
            ClueFilterComboBox.ClueFilterUpdatedEvent += DiscoverNewImages;

            // Initialize background worker
            this.BackgroundWorker = new BackgroundWorker();
            this.BackgroundWorker.WorkerReportsProgress = true;
            this.BackgroundWorker.WorkerSupportsCancellation = true;
            this.BackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorker_DoWork);
            this.BackgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            this.BackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorker_RunWorkerCompleted);
        }

        internal void Setup(Document initialDoc = null)
        {
            if(initialDoc != null)
            {
                // Default hanlding
                if(initialDoc.Clues.Count == 0)
                    // Just show it
                    PresenterSelectedImage = NavigatorSelectedImage = new BitmapImage(new Uri(initialDoc.Path));
                else
                    // Setup using clues provided by the document by update and search images of that clue using ComboBox
                    ClueFilterComboBox.Update(initialDoc.Clues, DocumentType.ImagePlus);
                // Change selected image display to the image indicated by the document
                TargetDocument = initialDoc;
            }
            else
            {
                // Setup using general clues
                ClueFilterComboBox.Update(null, DocumentType.ImagePlus);
                // Set other initial parameters
                PresenterSelectedImage = NavigatorSelectedImage = null;
            }
        }

        // Event Handlers
        void DiscoverNewImages(List<Document> newImages, List<Clue> availableClues)
        {
            // <Development> Notice we can address upto only 4GB due to dependecy on x86 libraries (VLC and Cef)
            // <Pending> Pending solution regarding memory related issues

            // Clean up UI
            LeftArrow.Opacity = InterfaceInactiveOpacity;
            RightArrow.Opacity = InterfaceInactiveOpacity;

            // Use a background worker to do the work instead of Task.Run()
            if (this.BackgroundWorker != null && this.BackgroundWorker.IsBusy)
            {
                this.BackgroundWorker.CancelAsync();
                RestartWorkerRequestImages = newImages;
            }
            else
            {
                this.BackgroundWorker.RunWorkerAsync(newImages);
                RestartWorkerRequestImages = null;
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }

        #region Background Work
        private List<Document> RestartWorkerRequestImages;
        private List<Document> FoundImages;
        private Document TargetDocument = null;
        private int TargetDocumentIndex = -1;
        private List<BitmapImage> LoadedImages;
        private BackgroundWorker BackgroundWorker;

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Do not access the form's BackgroundWorker reference directly.
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument.
            List<Document> arg = (List<Document>)e.Argument;

            // Start the time-consuming operation.
            e.Result = LoadImagesInBackground(bw, arg);

            // If the operation was canceled by the user, 
            // set the DoWorkEventArgs.Cancel property to true.
            if (bw.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            double progressValue = e.ProgressPercentage;
            if (e.UserState != null)
            {
                BitmapImage image = e.UserState as BitmapImage;
                // ...
            }
        }

        // The function can be cancelled, it can raise an exception, or it can exit normally and return a result. 
        // Don't temper with UI in this function
        internal bool LoadImagesInBackground(BackgroundWorker bw, List<Document> newImages)
        {
            // Update current documents list 
            FoundImages = newImages;
            LoadedImages = new List<BitmapImage>();
            // Acyn load images into memory
            for (int i = 0; i < newImages.Count; i++)
            {
                if (bw.CancellationPending) return false;
                try
                {
                    BitmapImage newImage = new BitmapImage();
                    using (var fs = new FileStream(newImages[i].Path, FileMode.Open))
                    {
                        newImage.BeginInit();
                        newImage.StreamSource = fs;
                        newImage.CacheOption = BitmapCacheOption.OnLoad;
                        newImage.EndInit();
                    }// https://stackoverflow.com/questions/28364439/how-to-dispose-bitmapimage-cache
                    // <Development> Consider Delay Creation: https://msdn.microsoft.com/en-us/library/system.windows.media.imaging.bitmapimage.cacheoption(v=vs.110).aspx

                    // BitmapImage newImage = new BitmapImage(new Uri(newImages[i].Path));

                    newImage.Freeze();  // https://stackoverflow.com/questions/26361020/error-must-create-dependencysource-on-same-thread-as-the-dependencyobject-even
                    LoadedImages.Add(newImage);
                    BackgroundWorker.ReportProgress(i/newImages.Count, newImage);

                    // Extra book keeping
                    if (TargetDocument != null && newImages[i] == TargetDocument) TargetDocumentIndex = i;
                }
                catch (NotSupportedException) { }
            }
            return true;
        }

        // Update UI in this function
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                // The user canceled the operation.
                // ... Might want to do something
                FoundImages = null;
                LoadedImages = null;

                // Restart work if requested
                if(RestartWorkerRequestImages != null)
                    this.BackgroundWorker.RunWorkerAsync(RestartWorkerRequestImages);
            }
            else if (e.Error != null)
            {
                // There was an error during the operation.
                // ... Might want to do something
                FoundImages = null;
                LoadedImages = null;
            }
            else
            {
                // The operation completed normally. We can access result from e.Result
                // ... Might want to do something

                // Update Image scroll dock panel by setting data bound properties; Setup a selected image for display using the first image found under that clue
                DistributeImageDisplay(0);

                // Update Presentation Panel
                PresentationImages = new ObservableCollection<BitmapImage>(LoadedImages);

                // Don't change SelectedImage for Presentatioan and Navigation for now
                // ... 
            }

            // Clear state
            TargetDocument = null;
            TargetDocumentIndex = -1;
        }

        int CurrentSelectionIndex = 0;
        private void DistributeImageDisplay(int currentSelectionIndex)
        {
            if (LoadedImages != null && LoadedImages.Count == 0) { NavigatorSelectedImage = PresenterSelectedImage = null; return; }
            if (LoadedImages == null || currentSelectionIndex < 0 || currentSelectionIndex >= LoadedImages.Count) throw new Exception("Unexpected setup");

            // Also play animation
            Storyboard story = ImagePreviewScrollPane.FindResource("BlinkAnimation") as Storyboard;
            Storyboard.SetTarget(story, ImagePreviewScrollPane);
            story.Begin();

            // Set selection; Check out availability as well
            if (LoadedImages.Count > 0)
            {
                if (TargetDocument != null && TargetDocumentIndex != -1) { NavigatorSelectedImage = PresenterSelectedImage = LoadedImages[TargetDocumentIndex]; }
                else NavigatorSelectedImage = PresenterSelectedImage = LoadedImages[currentSelectionIndex];

                // Set side images; Check out availability as well
                if (currentSelectionIndex - 3 >= 0 && currentSelectionIndex - 3 < LoadedImages.Count) NavigatorL3Image = LoadedImages[currentSelectionIndex - 3]; else NavigatorL3Image = null;
                if (currentSelectionIndex - 2 >= 0 && currentSelectionIndex - 2 < LoadedImages.Count) NavigatorL2Image = LoadedImages[currentSelectionIndex - 2]; else NavigatorL2Image = null;
                if (currentSelectionIndex - 1 >= 0 && currentSelectionIndex - 1 < LoadedImages.Count) NavigatorL1Image = LoadedImages[currentSelectionIndex - 1]; else NavigatorL1Image = null;
                if (currentSelectionIndex + 1 < LoadedImages.Count) NavigatorR1Image = LoadedImages[currentSelectionIndex + 1]; else NavigatorR1Image = null;
                if (currentSelectionIndex + 2 < LoadedImages.Count) NavigatorR2Image = LoadedImages[currentSelectionIndex + 2]; else NavigatorR2Image = null;
                if (currentSelectionIndex + 3 < LoadedImages.Count) NavigatorR3Image = LoadedImages[currentSelectionIndex + 3]; else NavigatorR3Image = null;

                // Extra UI Updating
                if (NavigatorL1Image != null) LeftArrow.Opacity = 1; else LeftArrow.Opacity = InterfaceInactiveOpacity;
                if (NavigatorR1Image != null) RightArrow.Opacity = 1; else RightArrow.Opacity = InterfaceInactiveOpacity;
            }
            else
            {
                NavigatorSelectedImage = PresenterSelectedImage = null;
                NavigatorL1Image = null;
                NavigatorL2Image = null;
                NavigatorL3Image = null;
                NavigatorR1Image = null;
                NavigatorR2Image = null;
                NavigatorR3Image = null;
            }

            // Book keeping
            CurrentSelectionIndex = currentSelectionIndex;
        }
        #endregion
        #endregion

        #region General UI Handling
        #region Configurations
        public static readonly string ModeShiftButtonNavigatorModeText = "Presentation Mode";   // I.e. Click to change to Presentation Mode
        public static readonly string ModeShiftButtonPresenterModeText = "Navigation Mode"; // I.e. Click to change to Navigation Mode (and Editing)
        public static readonly double InterfaceInactiveOpacity = 0.3;
        #endregion


        private CurrentImageViewerMode CurrentMode;
        private void ModeShiftButton_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentMode)
            {
                case CurrentImageViewerMode.Navigator:
                    // Change Button Text
                    ModeShiftButton.Content = ModeShiftButtonPresenterModeText;
                    // Toggle Available Pane
                    ImageNavigatorGrid.Visibility = Visibility.Collapsed;
                    ImagePresenterGrid.Visibility = Visibility.Visible;
                    // Book Keeping
                    CurrentMode = CurrentImageViewerMode.Presentation;
                    break;
                case CurrentImageViewerMode.Presentation:
                    // Change Button Text
                    ModeShiftButton.Content = ModeShiftButtonNavigatorModeText;
                    // Toggle Available Pane
                    ImageNavigatorGrid.Visibility = Visibility.Visible;
                    ImagePresenterGrid.Visibility = Visibility.Collapsed;
                    // Book Keeping
                    CurrentMode = CurrentImageViewerMode.Navigator;
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Navigator Mode Handling
        #region Navigation Image Scaling and Translation
        private bool bTranslatingImage = false;
        private Point CurrentPosition;
        private void NavigatorHighlightImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                CurrentPosition = e.GetPosition(this);
                bTranslatingImage = true;
                NavigatorHighlightImage.CaptureMouse();
            }
        }

        private void NavigatorHighlightImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bTranslatingImage = false;
            NavigatorHighlightImage.ReleaseMouseCapture();
        }
        private void NavigatorHighlightImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double magicalScale = 120;
            double scaleFactor = 5;
            // Traslate using layout trnasform: zoom on or zoom out
            double scaleAcceleration = (NavigationImageScale.ScaleX - HighlightImageSmallScaleLimit) / (HighlightImageLargeScaleLimit - HighlightImageSmallScaleLimit);   // Square interp
            scaleAcceleration *= scaleAcceleration * scaleAcceleration;
            double delta = (e.Delta / magicalScale / scaleFactor) * (scaleAcceleration + 1);
            ScaleHighlightImage(delta);
        }

        double HighlightImageSmallScaleLimit = 0.1;
        double HighlightImageLargeScaleLimit = 25;
        private void ScaleHighlightImage(double delta)
        {
            if (NavigationImageScale.ScaleX + delta > HighlightImageSmallScaleLimit && NavigationImageScale.ScaleX + delta < HighlightImageLargeScaleLimit)
            {
                NavigationImageScale.ScaleX += delta;
                NavigationImageScale.ScaleY += delta;
            }
            // Clamping
            if (NavigationImageScale.ScaleX < HighlightImageSmallScaleLimit)
            {
                NavigationImageScale.ScaleX = HighlightImageSmallScaleLimit;
                NavigationImageScale.ScaleY = HighlightImageSmallScaleLimit;
            }
            if (NavigationImageScale.ScaleX > HighlightImageLargeScaleLimit)
            {
                NavigationImageScale.ScaleX = HighlightImageLargeScaleLimit;
                NavigationImageScale.ScaleY = HighlightImageLargeScaleLimit;
            }
        }

        private void NavigatorHighlightImage_MouseMove(object sender, MouseEventArgs e)
        {
            // Reszie using layout trnasform
            if (bTranslatingImage == true && e.LeftButton == MouseButtonState.Pressed)
            {
                Point newPosition = e.GetPosition(this);
                NavigationImageTranslation.X += newPosition.X - CurrentPosition.X;
                NavigationImageTranslation.Y += newPosition.Y - CurrentPosition.Y;
                CurrentPosition = newPosition;
            }
        }
        #endregion
        #region Navigation Buttons
        private void LeftArrow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShiftImageLeft();
        }

        private void RightArrow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShiftImageRight();
        }

        private void L3Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(NavigatorL3Image != null)
            {
                DistributeImageDisplay(CurrentSelectionIndex - 3);
            }
        }

        private void L2Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NavigatorL2Image != null)
            {
                DistributeImageDisplay(CurrentSelectionIndex - 2);
            }
        }
        private void L1Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NavigatorL1Image != null)
            {
                DistributeImageDisplay(CurrentSelectionIndex - 1);
            }
        }

        private void R1Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NavigatorR1Image != null)
            {
                DistributeImageDisplay(CurrentSelectionIndex +1);
            }
        }

        private void R2Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NavigatorR2Image != null)
            {
                DistributeImageDisplay(CurrentSelectionIndex +2);
            }
        }

        private void R3Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (NavigatorR3Image != null)
            {
                DistributeImageDisplay(CurrentSelectionIndex + 3);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Navigation using basic arrow keys
            if(e.Key == Key.Left)
            {
                ShiftImageLeft();
                e.Handled = true;
            }
            else if(e.Key == Key.Right)
            {
                ShiftImageRight();
                e.Handled = true;
            }
            else if (e.Key == Key.Up && ImagePresenterGrid.Visibility == Visibility.Collapsed)
            {
                ScaleHighlightImage(0.5);
                e.Handled = true;
            }
            else if (e.Key == Key.Down && ImagePresenterGrid.Visibility == Visibility.Collapsed)
            {
                ScaleHighlightImage(-0.5);
                e.Handled = true;
            }

            // If in presentation mode
            else if (ImagePresenterGrid.Visibility == Visibility.Visible)
            {
                // Navigation using WASD
                switch (e.Key)
                {
                    case Key.A:
                        ShiftImageLeft();
                        e.Handled = true;   // Put this statement only at those places whwere we actually handled things, otherwise Alt-F4 might lose effect
                        break;
                    case Key.D:
                        ShiftImageRight();
                        e.Handled = true;
                        break;
                    case Key.S:
                        // Actually this operation doesn't make sense
                        break;
                    case Key.W:
                        // Actually this operation doesn't make sense
                        break;
                }

                if (e.Key == Key.Escape && PresentationHighlightImage.Visibility == Visibility.Visible)
                {
                    // Close highlight
                    PresentationHighlightImage.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                }
            }
        }
        private void ShiftImageLeft()
        {
            if (CurrentSelectionIndex > 0) DistributeImageDisplay(CurrentSelectionIndex - 1);
        }
        private void ShiftImageRight()
        {
            if (CurrentSelectionIndex < LoadedImages.Count - 1) DistributeImageDisplay(CurrentSelectionIndex + 1);
        }
        #endregion
        #endregion

        #region Presentation Mode Handling
        private void PresentationImageItem_MouseClick(object sender, MouseButtonEventArgs e)
        {
            // Highlight the selected image
            PresenterSelectedImage = (sender as FrameworkElement).DataContext as BitmapImage;
            // Show Highlight
            PresentationHighlightImage.Visibility = Visibility.Visible;
        }
        private void PresentationHighlightImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Close highlight
            PresentationHighlightImage.Visibility = Visibility.Collapsed;
        }

        private void RotateCClock_Click(object sender, RoutedEventArgs e)
        {
            PresentationHighlightImageRotation.Angle -= 90;
        }

        private void RotateClock_Click(object sender, RoutedEventArgs e)
        {
            PresentationHighlightImageRotation.Angle += 90;
        }
        #endregion

        #region Data Binding
        #region Interface Definitions
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region Navigator Data
        public BitmapImage _NavigatorSelectedImage;
        public BitmapImage _NavigatorL1Image;
        public BitmapImage _NavigatorL2Image;
        public BitmapImage _NavigatorL3Image;
        public BitmapImage _NavigatorR1Image;
        public BitmapImage _NavigatorR2Image;
        public BitmapImage _NavigatorR3Image;
        public BitmapImage NavigatorSelectedImage
        {
            get { return _NavigatorSelectedImage; }
            set
            {
                if (value != this._NavigatorSelectedImage)
                {
                    this._NavigatorSelectedImage = value;
                    NotifyPropertyChanged();
                }
            }
        }   // Notice for binding to work it need to be public, not internal
        public BitmapImage NavigatorL1Image
        {
            get { return _NavigatorL1Image; }
            set
            {
                if (value != this._NavigatorL1Image)
                {
                    this._NavigatorL1Image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage NavigatorL2Image
        {
            get { return _NavigatorL2Image; }
            set
            {
                if (value != this._NavigatorL2Image)
                {
                    this._NavigatorL2Image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage NavigatorL3Image
        {
            get { return _NavigatorL3Image; }
            set
            {
                if (value != this._NavigatorL3Image)
                {
                    this._NavigatorL3Image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage NavigatorR1Image
        {
            get { return _NavigatorR1Image; }
            set
            {
                if (value != this._NavigatorR1Image)
                {
                    this._NavigatorR1Image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage NavigatorR2Image
        {
            get { return _NavigatorR2Image; }
            set
            {
                if (value != this._NavigatorR2Image)
                {
                    this._NavigatorR2Image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage NavigatorR3Image
        {
            get { return _NavigatorR3Image; }
            set
            {
                if (value != this._NavigatorR3Image)
                {
                    this._NavigatorR3Image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
        #region Presenter Data
        public ObservableCollection<BitmapImage> _PresentationImages;
        public BitmapImage _PresenterSelectedImage;
        public BitmapImage PresenterSelectedImage
        {
            get { return _PresenterSelectedImage; }
            set
            {
                if (value != this._PresenterSelectedImage)
                {
                    this._PresenterSelectedImage = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ObservableCollection<BitmapImage> PresentationImages
        {
            get { return _PresentationImages; }
            set
            {
                if (value != this._PresentationImages)
                {
                    this._PresentationImages = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #endregion
    }
}
