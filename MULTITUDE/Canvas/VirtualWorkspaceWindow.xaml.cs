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
using MULTITUDE.Dialog;
using MULTITUDE.CustomControl;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class;
using System.Diagnostics;
using MULTITUDE.Gadget;
using MULTITUDE.Class.Facility;
using MULTITUDE.Popup;
using MULTITUDE.Class.Facility.ClueManagement;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using MULTITUDE.CustomControl.CanvasSpaceWindow;
using System.Windows.Media.Animation;
using Meta.Vlc.Wpf;
using MULTITUDE.CustomControl.DocumentIcons;

namespace MULTITUDE.Canvas
{
    enum VWShiftDirection
    {
        Left,
        Right,
        Up,
        Down,
        None    // For initialization
    }

    /// <summary>
    /// The place where all magic happens
    /// </summary>
    public partial class VirtualWorkspaceWindow : Window, INotifyPropertyChanged, IDocumentPropertyViewableWindow
    {
        #region Interface Elements
        Popup.StatusPromt Status;
        Popup.DocumentPropertyPanel DocPanel;
        Popup.BatchOperationPanelPopup BatchPanel;
        CustomControl.MemoirSearchBoxPopup SharedSearchPopup;
        #endregion

        #region State Construction and Bookkeeping Information
        // Book keeping
        public static VirtualWorkspaceWindow CurrentWindow { get; set; }
        internal VirtualWorkspace CurrentState { get; set; }    // Current opened VW, can be an active VW (i.e. managed under Home.VirtualWorkspaces and is considered Home.ActiveVW), or a document (i.e. managed under Home.Documents)

        // Constructor 
        public VirtualWorkspaceWindow()
        {
            InitializeComponent();
            CurrentWindow = this;
            UpdateCurrentState((App.Current as App).CurrentHome.ActiveVW.GetCurrentOpenVW());

            // Setup timer
            wallpaperTimer = new System.Windows.Threading.DispatcherTimer();
            wallpaperTimer.Tick += DispatcherTimer_TimeUp;
            wallpaperTimer.Interval = new TimeSpan(0, 30, 0);   // 30 min display time
            backgroundMediaRestartTimer = new System.Windows.Threading.DispatcherTimer();
            backgroundMediaRestartTimer.Tick += BackgroundMediaRestartTimer_TimeUp;
            backgroundMediaRestartTimer.Interval = new TimeSpan(0,0,0,0, 100);   // 100 ms

            // Bind event
            vlcPlayer.StateChanged += VlcPlayer_StateChanged;

            // Data Binding Constructions
            VWStackTrace = (App.Current as App).CurrentHome.ActiveVW.GetStackTrace();
        }
        #endregion

        #region Window Initialization and Management Events
        internal void UpdateCurrentState(VirtualWorkspace newState)
        {
            CurrentState = newState;

            if (CurrentState.VWCoordinate == null)
            {
                ConvertVWToVAButton.Visibility = Visibility.Collapsed;
                PackAsDocumentButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                ConvertVWToVAButton.Visibility = Visibility.Visible;
                PackAsDocumentButton.Visibility = Visibility.Visible;
            }
        }
        internal void ReloadContents(VWShiftDirection switchDirection = VWShiftDirection.None)
        {
            // Lock if needed
            if (Home.Current.IsLocked == true) LockscreenVisibility = Visibility.Visible;
            else LockscreenVisibility = Visibility.Collapsed;

            // Reset Interface Elements
            // Reset Batch Operation Panel...
            // Reset User Configuration Contents
            List<GadgetType> gadgets = new List<GadgetType>();
            foreach (VWGadget gadget in CurrentState.Gadgets)
            {
                gadgets.Add(gadget.Type);
            }
            ChangeConfigurations(CurrentState.BackgroundImageClue, CurrentState.BackgroundMelodyClue, gadgets);
            // Reset VW Compass State if it's one of those that should cause this
            RedrawVWLocation();
            // Reset cross-top search bar...

            // Play Animations
            PlayVWShiftAnimation(switchDirection);

            // Generate VW Visual Contents
            // Load document subject name
            VWSubjectName.Text = CurrentState.Name;
            // Generate Document Icons at current page
            UpdatePageDisplay();
            // Generate Gadgets...UpdateGadgetPopups();
            // Change and reload background images
            UpdateWallpaerDisplay();
            // Change and reload background melodies... 
            UpdateRhythmPlay();

            // Show stack trace
            VWStackTrace = Home.Current.ActiveVW.GetStackTrace();
        }

        private void PlayVWShiftAnimation(VWShiftDirection switchDirection)
        {
            // Setup animations
            Storyboard story = this.FindResource("FadeInOutAnimation") as Storyboard;
            // Play Animation to Present VW Visual Contents
            // Faout and fadein VW subject
            Storyboard.SetTarget(story, VWSubjectName);
            story.Begin();
            // Transform out and in tool tray ... Not Used
            // Fadeout and fade in background
            Storyboard.SetTarget(story, BackgroundImage);
            story.Begin();
            // Shift and fade away document icons
            Storyboard.SetTarget(story, DocumentIconsCanvas);
            story.Begin();
            // Generated animation
            DoubleAnimation shiftX = new DoubleAnimation();
            shiftX.From = 0;
            shiftX.To = 0;
            shiftX.Duration = new Duration(TimeSpan.Parse("0:0:1"));
            DoubleAnimation shiftY = new DoubleAnimation();
            shiftY.From = 0;
            shiftY.To = 0;
            shiftY.Duration = new Duration(TimeSpan.Parse("0:0:1"));
            switch (switchDirection)
            {
                case VWShiftDirection.Left:
                    shiftX.To = -DocumentIconsCanvas.ActualWidth;
                    break;
                case VWShiftDirection.Right:
                    shiftX.To = DocumentIconsCanvas.ActualWidth;
                    break;
                case VWShiftDirection.Up:
                    shiftY.To = -DocumentIconsCanvas.ActualHeight;
                    break;
                case VWShiftDirection.Down:
                    shiftY.To = DocumentIconsCanvas.ActualHeight;
                    break;
            }
            // Play Animation
            DocumentIconsCanvasTranslation.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, shiftX);
            DocumentIconsCanvasTranslation.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, shiftY);
        }

        private void RedrawVWLocation()
        {
            VirtualWorkspace active = (App.Current as App).CurrentHome.ActiveVW;

            // Redraw Location Coordiante
            // Get dimensions and related infor
            int row, col, dimension;
            EdgeDirection direction;
            active.LocationToRowCol(out row, out col, out dimension, out direction);

            // Clear previous states
            // Clear Grid states
            SpiralVWCoordinateGrid.Children.Clear();
            SpiralVWCoordinateGrid.ColumnDefinitions.Clear();
            SpiralVWCoordinateGrid.RowDefinitions.Clear();
            // Turn off all highlights
            LocatorCompassTLIcon.IsEnabled = true;
            LocatorCompassTIcon.IsEnabled = true;
            LocatorCompassTRIcon.IsEnabled = true;
            LocatorCompassLIcon.IsEnabled = true;
            LocatorCompassCenterIcon.IsEnabled = true;
            LocatorCompassRIcon.IsEnabled = true;
            LocatorCompassBLIcon.IsEnabled = true;
            LocatorCompassBIcon.IsEnabled = true;
            LocatorCompassBRIcon.IsEnabled = true;

            // Generate Grid Definitions
            for (int i = 0; i < dimension; i++)
            {
                SpiralVWCoordinateGrid.ColumnDefinitions.Add(new ColumnDefinition());
                SpiralVWCoordinateGrid.RowDefinitions.Add(new RowDefinition());
            }

            // Highlight appropriate Locator Compass Icon at direction
            switch (direction)
            {
                case EdgeDirection.LeftEdge:
                    LocatorCompassLIcon.IsEnabled = false;
                    break;
                case EdgeDirection.RightEdge:
                    LocatorCompassRIcon.IsEnabled = false;
                    break;
                case EdgeDirection.TopEdge:
                    LocatorCompassTIcon.IsEnabled = false;
                    break;
                case EdgeDirection.BottomEdge:
                    LocatorCompassBIcon.IsEnabled = false;
                    break;
                case EdgeDirection.Center:
                    LocatorCompassCenterIcon.IsEnabled = false;
                    break;
                case EdgeDirection.TL:
                    LocatorCompassTLIcon.IsEnabled = false;
                    break;
                case EdgeDirection.TR:
                    LocatorCompassTRIcon.IsEnabled = false;
                    break;
                case EdgeDirection.BL:
                    LocatorCompassBLIcon.IsEnabled = false;
                    break;
                case EdgeDirection.BR:
                    LocatorCompassBRIcon.IsEnabled = false;
                    break;
            }
            Home home = (App.Current as App).CurrentHome;
            // Generate Borders
            for (int i = 0; i < dimension; i++)
            {
                for (int j = 0; j < dimension; j++)
                {
                    // Preview Creation
                    ItemsControl newListBox = new ItemsControl();
                    newListBox.SetValue(Grid.RowProperty, i);
                    newListBox.SetValue(Grid.ColumnProperty, j);
                    // VW Information
                    VirtualWorkspace activeVW = home.TryGetActiveVW(VirtualWorkspace.RowColToLocation(SpiralVWCoordinateGrid.ColumnDefinitions.Count, i, j));
                    if (activeVW != null)
                    {
                        ObservableCollection<VWStackTraceView> fancyInformationCollection = new ObservableCollection<VWStackTraceView>();
                        // VW Name
                        fancyInformationCollection.Add(new VWStackTraceView("Subject: " + activeVW.Name));
                        // VW Document Count
                        fancyInformationCollection.Add(new VWStackTraceView(string.Format("Document Count: {0}", activeVW.GetDocumentCount())));
                        // Stack Trace
                        ObservableCollection<VWStackTraceView> stackTrace = activeVW.GetStackTrace();
                        if(stackTrace.Count != 0)
                        {
                            fancyInformationCollection.Add(new VWStackTraceView("Stack Trace:"));
                            foreach (VWStackTraceView view in stackTrace)
                            {
                                fancyInformationCollection.Add(view);
                            }
                        }
                        // newListBox.DataContext = activeVW.GetStackTrace();
                        newListBox.ItemsSource = fancyInformationCollection;
                    }

                    // Preview Settle Down
                    newListBox.MouseLeftButtonDown += Coordinator_MouseLeftButtonDown;
                    if(col == j && row == i) newListBox.IsEnabled = false; // Disable mouse event and Highlight current location
                    SpiralVWCoordinateGrid.Children.Add(newListBox);
                }
            }

            // Reconfigura Location Compass Label: Show translated coordinate
            string coordinateString = string.Format("Coordinate: {0}, {1}", active.VWCoordinate.Value.X, active.VWCoordinate.Value.Y);
            CoordinateCompassCoordinateLabel.Content = coordinateString;
            CoordinateCompassCoordinateLabel.ToolTip = coordinateString;
        }

        // Generate Document Icons on current page
        private void UpdatePageDisplay()
        {
            // Highlight current page button
            // Disable previous highlight
            Page0Button.IsEnabled = true;
            Page1Button.IsEnabled = true;
            Page2Button.IsEnabled = true;
            Page3Button.IsEnabled = true;
            Page4Button.IsEnabled = true;
            // Highlight current
            switch (CurrentState.PageIndex)
            {
                case 0:
                    Page0Button.IsEnabled = false;
                    break;
                case 1:
                    Page1Button.IsEnabled = false;
                    break;
                case 2:
                    Page2Button.IsEnabled = false;
                    break;
                case 3:
                    Page3Button.IsEnabled = false;
                    break;
                case 4:
                    Page4Button.IsEnabled = false;
                    break;
            }

            // Clear current main canvas icons display
            DocumentIconsCanvas.Children.Clear();

            // Display documents on new page
            Home home = (App.Current as App).CurrentHome;
            foreach (DocumentIcon doc in CurrentState.Pages[CurrentState.PageIndex].Documents)
            {
                DisplayDocumentIcon(home.GetDocument(doc.DocumentID), doc);
            }
        }

        // Save and Update New Configurations
        internal void ChangeConfigurations(string wallpaperClue, string rhythmClue, List<GadgetType> gadgets)
        {
            // Save Clue Settings
            CurrentState.BackgroundImageClue = wallpaperClue;
            CurrentState.BackgroundMelodyClue = rhythmClue;

            // Save Gadget settings
            // Create a bunch of new gagdets
            CurrentState.Gadgets = new List<VWGadget>();
            foreach (GadgetType gadget in gadgets)
            {
                CurrentState.Gadgets.Add(new VWGadget(gadget));
            }

            // Update Interface
            // Stop background image and reload
            UpdateWallpaerDisplay();
            // Stop backgrond rhythm and reload
            UpdateRhythmPlay();
            // Close current gdagets (windows and popups) and reload
            // ...UpdateGadgetPopups();
        }

        //private void PlayerOnVideoSourceChanged(object sender, Meta.Vlc.Wpf.VideoSourceChangedEventArgs videoSourceChangedEventArgs)
        //{
        //    testVlcPlayerNakedImage.Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        testVlcPlayerNakedImage.Source = videoSourceChangedEventArgs.NewVideoSource;
        //    }));
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Debug Test
            // testVlcPlayerNaked.LoadMedia(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
            //testVlcPlayerNaked = new Meta.Vlc.Wpf.VlcPlayer();
            //testVlcPlayerNaked.Initialize(@"..\..\libvlc", new string[] { "-I", "dummy", "--ignore-config", "--no-video-title" });
            //testVlcPlayerNaked.VideoSourceChanged += PlayerOnVideoSourceChanged;
            // testVlcPlayerNaked.LoadMedia(@"G:\Temp Movies\Curse.of.the.Golden.Flower.2006.CHINESE.1080p.BluRay.x264.DTS-FGT\Curse.of.the.Golden.Flower.2006.CHINESE.1080p.BluRay.x264.DTS-FGT.mkv"); // Cause exception when not stepping through
            //testVlcPlayerNaked.LoadMedia(@"G:\Temp Movies\【6v电影www.dy131.com】夜宴BD国语中字1280高清.rmvb"); // Not showing anything and is an exception
            //testVlcPlayerNaked.Play();
            // System.Threading.Thread.Sleep(2000);

            // Generate Common Interface Contents
            Status = new Popup.StatusPromt(this);
            DocPanel = new Popup.DocumentPropertyPanel(this);
            SharedSearchPopup = new CustomControl.MemoirSearchBoxPopup();
            BatchPanel = new Popup.BatchOperationPanelPopup(this);
            CustomControl.MemoirSearchBar.SearchPopup = SharedSearchPopup;

            // Configure search bar
            TopSearchBar.Configure(SearchMode.General, InterfaceOption.ExtendedFunctions | 
                InterfaceOption.RoudCornerGemBlue | InterfaceOption.ShowValidationSymbol | 
                InterfaceOption.ShowEnterTextHere | InterfaceOption.ShowDocumentPreview, UsageOption.Searcher);

            // Update status
            Status.Update(Popup.StatusEnum.Welcome, "Welcome to MULTITUDE, select a Home location to begin.", Popup.ActionsEnum.None, Popup.PlacementEnum.UpperRight);

            // Show batch panel at center right
            Rect screenWorkingArea = System.Windows.SystemParameters.WorkArea;
            BatchPanel.Show();  // Necessary to show it atleast once otherwise we cannot position it; In other cases show only when (more than one) documents are selected
            BatchPanel.Left = screenWorkingArea.Right - BatchPanel.ActualWidth - 128;   // Give some gap
            BatchPanel.Top = screenWorkingArea.Bottom / 2 - BatchPanel.ActualHeight / 2; // Center it
            BatchPanel.Hide();

            // Load VW specific contents
            ReloadContents();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bAltF4Pressed)
            {
                // Close highest level of VW stack if available, Disabled during lock
                if (LockscreenVisibility != Visibility.Visible && VWStackTrace.Count > 1)
                {
                    // Open the VW document under current active VW's stack trace
                    UpdateCurrentState(Home.Current.ActiveVW.OpenVWDocument(VWStackTrace[VWStackTrace.Count() - 2].VW));
                    // Update stack trace
                    VWStackTrace = Home.Current.ActiveVW.GetStackTrace();
                    // Reload
                    ReloadContents();
                }

                e.Cancel = true;
                bAltF4Pressed = false;
            }
            else
            {
                // Aync: Auto save
                (App.Current as App).SaveData();

                // Release non-managed resources
                vlcPlayer.Dispose();
                Meta.Vlc.Wpf.ApiManager.ReleaseAll();
            }
        }
        #endregion

        #region UI Interface Events
        #region Dialogs and Popups and Canvas Spaces
        public UserConfigurationDialog PrevOpenedConfigDialog = null;
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrevOpenedConfigDialog == null)
            {
                // Open a dialog for settings, the dialog isn't modal to make it responsive.
                UserConfigurationDialog dialog = new UserConfigurationDialog(this);
                Point position = SettingsButton.PointToScreen(new Point(0d, 0d));
                dialog.Show();
                dialog.Left = position.X + SettingsButton.ActualWidth;
                dialog.Top = position.Y + SettingsButton.ActualHeight;

                PrevOpenedConfigDialog = dialog;
            }
        }

        private bool bShowCanvasSpacePauseReason = false;
        private void ShowCanvasSpaceSetup()
        {
            // <Pending> Redesign

            // Pause current played background rhythme
            vlcPlayer.Pause();
            bShowCanvasSpacePauseReason = true;

            // Hide auxliary panels by changing always on top properties
            if (DocPanel.Visibility == Visibility.Visible) DocPanel.Visibility = Visibility.Collapsed;
            if (Status.Visibility == Visibility.Visible) Status.Visibility = Visibility.Collapsed;
            if (SharedSearchPopup.Visibility == Visibility.Visible) SharedSearchPopup.Visibility = Visibility.Collapsed;

            // Hide batch operation panel if it's opened
            if (BatchPanel.Visibility == Visibility.Visible)
            {
                BatchPanel.Visibility = Visibility.Collapsed;  // Or maybe collapsed for some performance boost?
            }

            this.Visibility = Visibility.Hidden;
        }

        public void RestoreCanvasSpace()
        {
            if(vlcPlayer.State == Meta.Vlc.Interop.Media.MediaState.Paused && bShowCanvasSpacePauseReason == true)
            {
                vlcPlayer.Resume();
                bShowCanvasSpacePauseReason = false;
            }
            this.Visibility = Visibility.Visible;
        }

        private void ClueBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            OpenClueBrowser();
        }

        private void ImageViewerButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDelightfulBrowser();
        }

        private void CollectionCreatorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenCollectionCreator(null);
        }

        private void WebBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            OpenWebBrowser();
        }

        private void TableViewerButton_Click(object sender, RoutedEventArgs e)
        {
            OpenTableViewer();
        }

        private void MDPlusButton_Click(object sender, RoutedEventArgs e)
        {
            OpenMarkdownPlusEditor();
        }

        private void GraphEditorButton_Click(object sender, RoutedEventArgs e)
        {
            OpenGraphEditor();
        }

        private void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            OpenArchiveViewer();
        }

        private void QuickMatchButton_Click(object sender, RoutedEventArgs e)
        {
            OpenQuickMatch();
        }

        private void LeaveButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MediaPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            OpenMediaPlayer();
        }

        private void ForgottenSpaceButton_Click(object sender, RoutedEventArgs e)
        {
            OpenForgottenUniverse();
        }

        private void VoidSpaceButton_Click(object sender, RoutedEventArgs e)
        {
            OpenVoidUniverse();
        }
        #endregion
        #region Canvas Space Window Creation
        private void OpenClueBrowser()
        {
            // Create a view; Or we might want to reuse it because it can get quite heavy
            MULTITUDE.CustomControl.CanvasSpaceWindow.ClueBrowserWindow space = new CustomControl.CanvasSpaceWindow.ClueBrowserWindow(this);
            // Show it
            space.Show();
            ShowCanvasSpaceSetup();
            // Update
            space.Update((App.Current as App).CurrentHome);
        }
        private void OpenForgottenUniverse()
        {
            MULTITUDE.CustomControl.CanvasSpaceWindow.ForgottenUniverseWindow space = new CustomControl.CanvasSpaceWindow.ForgottenUniverseWindow(this);
            // Show it
            space.Show();
            ShowCanvasSpaceSetup();
        }
        private void OpenVoidUniverse()
        {
            // Create a view
            MULTITUDE.CustomControl.CanvasSpaceWindow.VoidUniverseWindow space = new MULTITUDE.CustomControl.CanvasSpaceWindow.VoidUniverseWindow(this);
            // Show it
            space.Show();
            ShowCanvasSpaceSetup();
        }
        private void OpenMarkdownPlusEditor(Document target = null)
        {
            // Create a view
            MULTITUDE.CustomControl.CanvasSpaceWindow.MarkdownPlusEditorWindow space = new MULTITUDE.CustomControl.CanvasSpaceWindow.MarkdownPlusEditorWindow(this);
            // Show space
            space.Show();
            ShowCanvasSpaceSetup();
            // Feed Content
            space.Setup(target);
        }
        private void OpenDelightfulBrowser(Document target = null)
        {
            // Create a view
            MULTITUDE.CustomControl.CanvasSpaceWindow.DelightfulImageBrowser space = new CustomControl.CanvasSpaceWindow.DelightfulImageBrowser(this);
            // Show space
            space.Show();
            ShowCanvasSpaceSetup();
            // Feed Content
            space.Setup(target);
        }
        private void OpenArchiveViewer(Document target = null)
        {
            MULTITUDE.Popup.ArchiveViewer ArchiveViewer = new ArchiveViewer(this, target);
            ArchiveViewer.Show();
        }
        private void OpenWebBrowser(string urlOrKeyWord = null)
        {
            LightWebBrowser browser = new LightWebBrowser(urlOrKeyWord);
            browser.Show();
        }
        private void OpenCollectionCreator(Document target = null)
        {
            // Create a view
            CollectionCreatorWindow creator = new CollectionCreatorWindow(this, target as Archive);
            // Show space
            creator.Show();
            ShowCanvasSpaceSetup();
        }
        private void OpenGraphEditor(Document target = null)
        {
            // Create a view
            MULTITUDE.CustomControl.CanvasSpaceWindow.GraphEditor space = new CustomControl.CanvasSpaceWindow.GraphEditor(this);
            // Show space
            space.Show();
            ShowCanvasSpaceSetup();
            // Feed Content
            space.Setup(target);
        }
        private void OpenQuickMatch()
        {
            throw new NotImplementedException();
        }
        private void OpenTableViewer(DataCollection target = null)
        {
            // Create a view
            MULTITUDE.CustomControl.CanvasSpaceWindow.MarkdownPlusEditorWindow space = new MULTITUDE.CustomControl.CanvasSpaceWindow.MarkdownPlusEditorWindow(this, ContentMode.TableOnly);
            // Show space
            space.Show();
            ShowCanvasSpaceSetup();
            // Feed Content
            space.Setup(target);
        }
        private void OpenMediaPlayer(Document target = null)
        {
            // Create a view
            MULTITUDE.CustomControl.CanvasSpaceWindow.MediaPlayerWindow space = new CustomControl.CanvasSpaceWindow.MediaPlayerWindow(this);
            // Show
            space.Show();
            ShowCanvasSpaceSetup();
            // Feed Content
            if (target != null) space.Update(target);
        }
        #endregion
        #region Drag-n-Drop Interaction
        // http://stackoverflow.com/questions/5662509/drag-and-drop-files-into-wpf
        private async void Window_Drop(object sender, DragEventArgs e)
        {
            // Disabled during lock
            if(LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            #region File drop from external applications e.g. File Explorer
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Check if short cut SHIFT is pressed so we skip dialog
                //. ..

                // <Pending> Duplicate targets handling: either make a copy doc, or a clone doc; Physical details of which still depends on ImportAction which doesn't matter

                // Get all targets
                string[] targets = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Prompt impot options
                Popup.FileDropOptionsPanel dialog = new Popup.FileDropOptionsPanel(targets.Length == 1 ? SystemHelper.IsFolder(targets[0]) == true : true, this);
                if (dialog.ShowDialog() == true)
                {
                    // Get user options
                    ImportAction action = dialog.Action;
                    ImportMode mode = dialog.Mode;

                    // Async Import
                    App app = (App.Current as App);
                    int documentCount;
                    List<Document> unorganizedDocs = /*await*/ app.CurrentHome.ImportTargets(targets, action, mode, out documentCount);

                    // Generate a event description string
                    string statusUpdateString = String.Format(targets.Length > 1 ?
                                        "{0} targets with a total of {1} documents are imported." :
                                        "{0} target with a total of {1} documents is imported.", targets.Length, documentCount);

                    // Generate icons for unorgannized Docs if they are of reasonable amount
                    if (unorganizedDocs.Count <= ((DocumentIconsCanvas.ActualWidth / IconBase.DefaultCanvasIconSize) * (DocumentIconsCanvas.ActualHeight / IconBase.DefaultCanvasIconSize)))
                    {
                        // Get mouse position
                        Point position = e.GetPosition(DocumentIconsCanvas);     // Mouse.GetPosition doesn't work in this case: https://wpf.2000things.com/tag/getposition/
                        double iconLeft = position.X - IconBase.DefaultCanvasIconSize / 2;
                        double iconTop = position.Y - IconBase.DefaultCanvasIconSize / 2;
                        // Generate icons
                        foreach (Document doc in unorganizedDocs)
                        {
                            AddDocumentToCanvasDisplay(doc, iconLeft, iconTop);
                            // Offset for next display
                            iconLeft += IconBase.DefaultCanvasIconSize / 2;
                            // iconTop += IconBase.DefaultIconSize / 2;
                        }

                        // Show status prompt
                        Status.Update(statusUpdateString + string.Format(" {0} documents are added to workspace.", unorganizedDocs.Count));
                    }
                    else
                    {
                        // Show status prompt
                        Status.Update(statusUpdateString + string.Format(" Documents are not added to workspace since there is too many of them. View them in Forgotten Universe."));
                    }

                    // Finish import (file operations)
                    /*await*/
                    app.CurrentHome.FinishImport();   // Or spawn a new task

                    // Show status prompt 
                    Status.Update(String.Format("File operations for {0} targets done.", targets.Length));
                }
                else
                {
                    // Promot status
                    Status.Update("Action cancelled.");
                }
                e.Handled = true;
            }
            #endregion
            #region Document drop from Memoir search bar
            else if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                Document doc = (Document)e.Data.GetData(Document.DragDropFormatString);

                // Add document to panel display
                Point position = e.GetPosition(DocumentIconsCanvas);
                AddDocumentToCanvasDisplay(doc, position.X, position.Y);

                // Show status
                Status.Update(String.Format("Document: {0} added to virtual workspace.", doc.ShortDescription));
            }
            #endregion
            #region Documetn drop from Archive viwer or Folder Navigator
            else if(e.Data.GetDataPresent(DropRequest.DropRequestDropDataFormatString))
            {
                DropRequest request = (DropRequest)e.Data.GetData(DropRequest.DropRequestDropDataFormatString);
                if(request.Type == DropRequestType.SimpleClueReference)
                {
                    string fileOrFolderPath = request.Data as string;

                    // Clean import as a new document without any internalization etc.
                    int notUsed;
                    List<Document> importedDocs = null;
                    if (System.IO.Directory.Exists(fileOrFolderPath)) importedDocs = Home.Current.ImportTargets(new string[] { fileOrFolderPath }, ImportAction.Refer, ImportMode.GenerateVirtualArchive, out notUsed);
                    else importedDocs = Home.Current.ImportTargets(new string[] { fileOrFolderPath }, ImportAction.Refer, ImportMode.NoClassification, out notUsed);

                    if (importedDocs.Count > 1) throw new InvalidOperationException("Unexpected."); // Can be 0, e.g. empty folder
                    // Generate icon
                    else if(importedDocs.Count == 1)
                    {
                        // Get mouse position
                        Point position = e.GetPosition(DocumentIconsCanvas);
                        // Generate icon
                        AddDocumentToCanvasDisplay(importedDocs[0], position.X, position.Y);

                        // Show status
                        Status.Update(String.Format("Document: {0} added to virtual workspace.", importedDocs[0].ShortDescription));
                    }

                    // Finish import (file operations)
                    Home.Current.FinishImport();
                }
            }
            #endregion

            // Hiden additional feedback
            DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Check file type and give an icon (or maybe a full blown control) indicating its dropped effect
            base.OnDragEnter(e);
            
            // If the DataObject contains file
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Show additional feedback
                DocumentDropCursorText.Visibility = Visibility.Visible;
                DocumentDropCursorText.Text = string.Join("\n", (string[])e.Data.GetData(DataFormats.FileDrop));
            }
            // If the DataObject contains documents
            else if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                // Show additional feedback
                DocumentDropCursorText.Visibility = Visibility.Visible;
                DocumentDropCursorText.Text = ((Document)e.Data.GetData(Document.DragDropFormatString)).ShortDescription;
            }
            else if(e.Data.GetDataPresent(DropRequest.DropRequestDropDataFormatString))
            {
                DropRequest request = (DropRequest)e.Data.GetData(DropRequest.DropRequestDropDataFormatString);
                if (request.Type == DropRequestType.SimpleClueReference)
                {
                    // Show additional feedback
                    DocumentDropCursorText.Visibility = Visibility.Visible;
                    DocumentDropCursorText.Text = request.Data as string;
                }
            }

            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            base.OnDragLeave(e);

            if (DocumentDropCursorText.Visibility == Visibility.Visible)
                DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            base.OnDragOver(e);
            e.Effects = DragDropEffects.None;

            // If the DataObject contains file
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Set Effects to notify the drag source what effect
                // the drag-and-drop operation will have. These values are 
                // used by the drag source's GiveFeedback event handler.
                // (Copy if CTRL is pressed; otherwise, move.)
                if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey)) e.Effects = DragDropEffects.Copy;
                else e.Effects = DragDropEffects.Move;

                // Show additional feedback
                Point position = e.GetPosition(DocumentIconsCanvas);
                System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
            }
            // If the DataObject contains documents
            else if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                e.Effects = DragDropEffects.Link;

                // Show additional feedback
                Point position = e.GetPosition(DocumentIconsCanvas);
                System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
            }
            else if(e.Data.GetDataPresent(DropRequest.DropRequestDropDataFormatString))
            {
                e.Effects = DragDropEffects.Link;

                DropRequest request = (DropRequest)e.Data.GetData(DropRequest.DropRequestDropDataFormatString);
                if (request.Type == DropRequestType.SimpleClueReference)
                {
                    // Show additional feedback
                    Point position = e.GetPosition(DocumentIconsCanvas);
                    System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                    System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
                }
            }

            e.Handled = true;
        }

        private void Window_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            base.OnGiveFeedback(e);
            // These Effects values are set in the drop target's
            // DragOver event handler.
            if (e.Effects.HasFlag(DragDropEffects.Link) || e.Effects.HasFlag(DragDropEffects.Copy))
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

        private void AddDocumentToCanvasDisplay(Document doc, double X, double Y)
        {
            // Precalculate location
            CanvasRelativeLocation location = CalculateIconLocation(IconBase.DefaultCanvasIconSize, IconBase.DefaultCanvasIconSize, X, Y);
         
            // Set occupation
            IconArea occupation = new IconArea(location, IconBase.DefaultCanvasIconSize, IconBase.DefaultCanvasIconSize);
            // Add to current page
            DocumentIcon info = CurrentState.Pages[CurrentState.PageIndex].AddDocument(doc, occupation);
            // Display icons
            DisplayDocumentIcon(doc, info);
        }

        // Never do we delete/remove documents when their icons are removed (dereferenced), unless "removed" from document property panel
        private void RemoveDocumentFromCanvasDisplay(IconBase icon)
        {
            // Remove from VW page
            CurrentState.Pages[CurrentState.PageIndex].RemoveDocument(icon.IconInfo);
            // Remove from canvas
            DocumentIconsCanvas.Children.Remove(icon);
        }

        // Calculate icon location according to which edge closer to canvas, input dX and dY are relative offsets, and oldLocation is previous location to be offsetted
        private CanvasRelativeLocation CalculateIconLocation(IconBase icon, CanvasRelativeLocation oldLocation, double dx, double dy)
        {
            double absX = 0, absY = 0;
            double iconWidth = icon.ActualWidth, iconHeight = icon.ActualHeight;
            switch (oldLocation.Relativity)
            {
                case RelativeLocation.UpperLeft:
                    absX = oldLocation.HorizontalDistance + dx;
                    absY = oldLocation.VerticalDistance + dy;
                    break;
                case RelativeLocation.UpperRight:
                    absX = DocumentIconsCanvas.ActualWidth - oldLocation.HorizontalDistance - icon.ActualWidth + dx;
                    absY = oldLocation.VerticalDistance + dy;
                    break;
                case RelativeLocation.LowerLeft:
                    absX = oldLocation.HorizontalDistance + dx;
                    absY = DocumentIconsCanvas.ActualHeight - oldLocation.VerticalDistance - icon.ActualHeight + dy;
                    break;
                case RelativeLocation.LowerRight:
                    absX = DocumentIconsCanvas.ActualWidth - oldLocation.HorizontalDistance - icon.ActualWidth + dx;
                    absY = DocumentIconsCanvas.ActualHeight - oldLocation.VerticalDistance - icon.ActualHeight + dy;
                    break;
            }
            return CalculateIconLocation(iconWidth, iconHeight, absX, absY);
        }


        // Calculate icon location according to which edge closer to canvas; Also do boundary check so icon doesn't go off screen
        // Input X and Y in absolute coordinatesof upper left corner
        private CanvasRelativeLocation CalculateIconLocation(double iconWidth, double iconHeight, double x, double y)
        {
            // Do boundary check first to make things reasonable
            if (x > DocumentIconsCanvas.ActualWidth - IconBase.DefaultCanvasIconSize) x = x % DocumentIconsCanvas.ActualWidth;
            if (y > DocumentIconsCanvas.ActualHeight - IconBase.DefaultCanvasIconSize) y = y % DocumentIconsCanvas.ActualHeight;

            // Generate actual location info
            CanvasRelativeLocation relativeLocation = new CanvasRelativeLocation();
            // Basic infor
            double canvasWidth = DocumentIconsCanvas.ActualWidth;
            double canvasHeight = DocumentIconsCanvas.ActualHeight;
            // Compare horitontal direction
            if(x > canvasWidth / 2) // Right
            {
                // Adjust Horizontal Distance
                relativeLocation.HorizontalDistance = canvasWidth - x - iconWidth;

                // Compare vertical direction
                if (y > canvasHeight / 2) relativeLocation.Relativity = RelativeLocation.LowerRight;
                else relativeLocation.Relativity = RelativeLocation.UpperRight;
            }
            else // Left
            {
                // Adjust Horizontal Distance
                relativeLocation.HorizontalDistance = x;

                // Compare vertical direction
                if (y > canvasHeight / 2) relativeLocation.Relativity = RelativeLocation.LowerLeft;
                else relativeLocation.Relativity = RelativeLocation.UpperLeft;
            }

            // Adjust Vertical Distance
            if(y > canvasHeight /2) relativeLocation.VerticalDistance = canvasHeight - y - iconHeight;
            else relativeLocation.VerticalDistance = y;

            // Return
            return relativeLocation;
        }

        private void DisplayDocumentIcon(Document doc, DocumentIcon info)
        {
            // Generate a new icon
            IconBase newIcon = new IconBase(doc, info);
            newIcon.PreviewMouseLeftButtonDown += IconBase_PreviewMouseLeftButtonDown;
            newIcon.MouseLeftButtonDown += IconBase_MouseLeftButtonDown;
            newIcon.MouseUp += IconBase_MouseUp;
            newIcon.MouseMove += IconBase_MouseMove;

            // Set appropriate Canavs location and size
            SetIconLocation(newIcon, info.Occupation.Location);
            // Adjust size
            newIcon.Width = info.Occupation.Width;
            newIcon.Height = info.Occupation.Height;
            // Add to canvas display
            DocumentIconsCanvas.Children.Add(newIcon);
        }

        private void SetIconLocation(IconBase icon, CanvasRelativeLocation location)
        {
            // Unset
            icon.SetValue(System.Windows.Controls.Canvas.LeftProperty, DependencyProperty.UnsetValue);
            icon.SetValue(System.Windows.Controls.Canvas.RightProperty, DependencyProperty.UnsetValue);
            icon.SetValue(System.Windows.Controls.Canvas.TopProperty, DependencyProperty.UnsetValue);
            icon.SetValue(System.Windows.Controls.Canvas.BottomProperty, DependencyProperty.UnsetValue);
            // Set according
            switch (location.Relativity)
            {
                case RelativeLocation.UpperLeft:
                    System.Windows.Controls.Canvas.SetTop(icon, location.VerticalDistance);
                    System.Windows.Controls.Canvas.SetLeft(icon, location.HorizontalDistance);
                    break;
                case RelativeLocation.UpperRight:
                    System.Windows.Controls.Canvas.SetTop(icon, location.VerticalDistance);
                    System.Windows.Controls.Canvas.SetRight(icon, location.HorizontalDistance);
                    break;
                case RelativeLocation.LowerLeft:
                    System.Windows.Controls.Canvas.SetBottom(icon, location.VerticalDistance);
                    System.Windows.Controls.Canvas.SetLeft(icon, location.HorizontalDistance);
                    break;
                case RelativeLocation.LowerRight:
                    System.Windows.Controls.Canvas.SetBottom(icon, location.VerticalDistance);
                    System.Windows.Controls.Canvas.SetRight(icon, location.HorizontalDistance);
                    break;
            }
        }
        #endregion
        #region Mouse Movement Events
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            if (TimerSettings.Visibility == Visibility.Collapsed && SpiralVWCoordinateGrid.Visibility == Visibility.Collapsed)
            {
                Point mouseLocation = e.GetPosition(DocumentIconsCanvas);

                // Double click instead of single to create a new document, for sometimes one might ust want to click at blank space for fun
                DocumentType targetType = DocumentType.MarkdownPlus;
                if((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) targetType = DocumentType.PlainText;
                // else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) targetType = DocumentType.PlayList; // Home doesn't support create playlist directly like this
                if (SelectedIcons.Count == 0)
                    AddDocumentToCanvasDisplay((App.Current as App).CurrentHome.CreateDocument(targetType), mouseLocation.X, mouseLocation.Y);
                e.Handled = true;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Hidden DragArea; Select items within, release mouse capture
            // Ref: https://www.codeproject.com/Articles/148503/Simple-Drag-Selection-in-WPF - summary: Custom implementation by iterating and using Rect.IntersectsWith
            // Ref: https://msdn.microsoft.com/en-us/library/ms771301.aspx
            // Selection using drag area only if we are draging
            if (DragArea.Width != 0 && DragArea.Height != 0)
            {
                Point dragAreaLocation = DragArea.TransformToVisual(DocumentIconsCanvas).Transform(new Point(0, 0));
                Rect dragArearect = new Rect(dragAreaLocation.X, dragAreaLocation.Y, DragArea.ActualWidth, DragArea.ActualHeight);

                // Add to new selection
                List<IconBase> intersectIcons = new List<IconBase>();
                foreach (FrameworkElement documentIcon in DocumentIconsCanvas.Children)
                {
                    IconBase icon = documentIcon as IconBase;
                    if (icon != null)
                    {
                        Point iconLocation = icon.TransformToVisual(DocumentIconsCanvas).Transform(new Point(0, 0));
                        Rect iconRect = new Rect(iconLocation.X, iconLocation.Y, icon.ActualWidth, icon.ActualHeight);
                        if (dragArearect.Contains(iconRect))
                        {
                            intersectIcons.Add(icon);
                        }
                    }
                }

                // Select new
                AddOrRemoveSelectIcons(intersectIcons);
            }
            else UnSelectIcons();
            // Hide drag area
            DragArea.Width = DragArea.Height = 0;
            DragArea.Visibility = Visibility.Hidden;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Hiden visible coordinate system
            if (SpiralVWCoordinateGrid.Visibility == Visibility.Visible)
                SpiralVWCoordinateGrid.Visibility = Visibility.Collapsed;

            // Show drag area
            if (SelectedIcons.Count == 0)
            {
                Point mousePosition = Mouse.GetPosition(DocumentIconsCanvas);
                System.Windows.Controls.Canvas.SetLeft(DragArea, mousePosition.X);
                System.Windows.Controls.Canvas.SetTop(DragArea, mousePosition.Y);
                DragArea.Width = 0;
                DragArea.Height = 0;
                DragArea.Visibility = Visibility.Visible;
                UnSelectIcons();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Handle mouse dragging of drag area
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // get current mouse position relative to Canvas
                Point mousePosition = Mouse.GetPosition(MainCanvas);
                double dx = 0, dy = 0;
                // If we are dragging toward right (with fixed LeftProperty)
                if (DragArea.ReadLocalValue(System.Windows.Controls.Canvas.LeftProperty) != DependencyProperty.UnsetValue)
                {
                    double prevX = System.Windows.Controls.Canvas.GetLeft(DragArea);
                    dx = mousePosition.X - prevX;

                    if (dx < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.LeftProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetRight(DragArea, MainCanvas.ActualWidth - prevX);
                        dx = -dx;
                    }
                }
                else // We are dragging toward left (with fixed RightProperty)
                {
                    double prevX = System.Windows.Controls.Canvas.GetRight(DragArea);
                    dx = MainCanvas.ActualWidth - mousePosition.X - prevX;

                    if (dx < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.RightProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetLeft(DragArea, MainCanvas.ActualWidth - prevX);
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
                        System.Windows.Controls.Canvas.SetBottom(DragArea, MainCanvas.ActualHeight - prevY);
                        dy = -dy;
                    }
                }
                // We are draggin toward bottom (with fixed BottomProperty)
                else
                {
                    double prevY = System.Windows.Controls.Canvas.GetBottom(DragArea);
                    dy = MainCanvas.ActualHeight - mousePosition.Y - prevY;
                    if (dy < 0)
                    {
                        DragArea.SetValue(System.Windows.Controls.Canvas.BottomProperty, DependencyProperty.UnsetValue);
                        System.Windows.Controls.Canvas.SetTop(DragArea, MainCanvas.ActualHeight - prevY);
                        dy = -dy;
                    }
                }
                // Set relative width/height to a positive number
                DragArea.Width = dx;
                DragArea.Height = dy;
            }
        }
        #endregion
        #region Icon Manipulation
        #region Icon Creation
        public void CreatePlaylistFromMedia(PlayListDocumentIcon mediaDoc)
        {
            Point iconLocation = mediaDoc.PointToScreen(DocumentIconsCanvas.PointToScreen(new Point(0d, 0d)));
            AddDocumentToCanvasDisplay(Home.Current.CreatePlaylistFromMedia(mediaDoc.Document),
                iconLocation.X + IconBase.DefaultBigIconDimension, iconLocation.Y + IconBase.DefaultBigIconDimension);
        }
        #endregion
        #region Icon Selection and Highlight
        private List<IconBase> SelectedIcons = new List<IconBase>();   // Might not contain SelectedIcon
        private Point CursorOffset; // For translation
        // Add to current selections
        private void AddOrRemoveSelectIcons(List<IconBase> icons)
        {
            // Control/Shift selection
            if (!((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
                || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)) UnSelectIcons();

            foreach (IconBase icon in icons)
            {
                AddOrRemoveSelectIcon(icon, true);
            }
        }

        private void AddOrRemoveSelectIcon(IconBase icon, bool unSelectionHandled = false)
        {
            // Control/Shift selection
            if (!((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
                || (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) && unSelectionHandled == false) UnSelectIcons();

            if (SelectedIcons.Contains(icon)) { SelectedIcons.Remove(icon); icon.UnhighlightSelection(); }
            else { SelectedIcons.Add(icon); icon.HighlightSelection(); }

            // Update batch operation panel
            UpdateBatchOperationPanel();
        }

        // Just select
        private void AddSelectIcon(IconBase icon)
        {
            if (SelectedIcons.Contains(icon) == false)
            {
                SelectedIcons.Add(icon); icon.HighlightSelection();
                // Update batch operation panel
                UpdateBatchOperationPanel();
            }            
        }

        // Deselect current selected icons
        private void UnSelectIcons()
        {
            if (SelectedIcons != null)
            {
                foreach (IconBase icon in SelectedIcons)
                {
                    icon.UnhighlightSelection();
                    icon.RequestSave();
                }
                SelectedIcons.Clear();
            }

            // Update batch operation panel
            UpdateBatchOperationPanel();
        }

        private void UpdateBatchOperationPanel()
        {
            if (bUIHidden) { BatchPanel.Visibility = Visibility.Collapsed; return; }
            // Show
            if (SelectedIcons.Count > 1)
            {
                BatchPanel.Visibility = Visibility.Visible;

                // Contents Update
                // ... <Pending>
            }
            // Not show
            else BatchPanel.Visibility = Visibility.Collapsed;
        }
        #endregion
        #region Functional
        private void TranslateIcons(List<IconBase> icons, double dx, double dy)
        {
            foreach (IconBase icon in icons)
            {
                CanvasRelativeLocation newLocation = CalculateIconLocation(icon, icon.IconInfo.Occupation.Location, dx, dy);
                icon.IconInfo.Occupation.Location = newLocation;
                SetIconLocation(icon, newLocation);
            }
        }
        internal void ShowDocumentPanelForDocument(Document doc)
        {
            if (bUIHidden) return;
            DocPanel.Update(doc, this);
        }

        public void DeleteDocumentFromView(Object doc)
        {
            if (doc is Document == false) throw new InvalidOperationException("Object isn't a document type.");

            // Hide Panel
            DocPanel.Visibility = Visibility.Collapsed;
            // Find in current view
            foreach (Object item in DocumentIconsCanvas.Children)
            {
                IconBase icon = (item as IconBase);
                if (icon.Document == doc)
                {
                    // Delete from view
                    RemoveDocumentFromCanvasDisplay(icon);

                    // Also remove document from home
                    Home.Current.Delete(icon.Document);

                    // Other clean up
                    SelectedIcons.Clear();
                    return;
                }
            }

            throw new ArgumentOutOfRangeException("Specified document isn't found in current page.");
        }
        #endregion
        #region Icon Base Mouse Events
        // Selection
        private void IconBase_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //// Disabled during lock
            //if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            //// Book keeping
            //CursorOffset = e.GetPosition(DocumentIconsCanvas);

            //// The below is VW-wise handling of this event
            //AddOrRemoveSelectIcon(sender as IconBase);
            //// e.Handled = true;
        }

        // Translation
        private void IconBase_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Book keeping
            CursorOffset = e.GetPosition(DocumentIconsCanvas);

            // The below is VW-wise handling of this event
            AddOrRemoveSelectIcon(sender as IconBase);

            // Don't do this when used as Preview event
            // Capture mouse event
            (sender as IconBase).CaptureMouse();
            e.Handled = true;
        }

        private void IconBase_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Condition this as a click event
            IconBase icon = (sender as IconBase);
            ShowDocumentPanelForDocument(icon.Document);

            icon.ReleaseMouseCapture();
            e.Handled = true;

            // Hide drag area
            DragArea.Width = DragArea.Height = 0;
            DragArea.Visibility = Visibility.Hidden;
        }

        private void IconBase_MouseMove(object sender, MouseEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            if (e.Handled == true) return;  // If already handled internally

            if (e.LeftButton == MouseButtonState.Pressed && DragArea.Width == 0 && DragArea.Height == 0)
            {
                // Handle icon translation: notice icons cannot be resized together, but trnalsated together
                Point position = e.GetPosition(DocumentIconsCanvas);
                TranslateIcons(SelectedIcons, position.X - CursorOffset.X, position.Y - CursorOffset.Y); // Don't get position relative to this icon but the whole canvas because that will cause problem when we tranlsate: new position will get generated and cause error
                CursorOffset = position;
                e.Handled = true;
            }
        }
        #endregion
        #endregion
        #endregion

        #region Other Interface Elements
        #region VW Creation and Management
        private void Coordinator_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Get item's grid location
            FrameworkElement element = sender as FrameworkElement;
            int col = (int)element.GetValue(Grid.ColumnProperty);
            int row = (int)element.GetValue(Grid.RowProperty);

            // Calculate coordinate
            Coordinate coordinate = VirtualWorkspace.RowColToLocation(SpiralVWCoordinateGrid.ColumnDefinitions.Count, row, col);

            // Get VW at location if not current
            if (CurrentState.VWCoordinate.HasValue == false || coordinate != CurrentState.VWCoordinate.Value)
            {
                SwitchToVWAtCoordinate(coordinate);
            }
        }
        private void SwitchToVWAtCoordinate(Coordinate coordinate)
        {
            // Notice no need to explicitly save VW because it's managed by home
            VirtualWorkspace activeVW = (App.Current as App).CurrentHome.GetActiveVW(coordinate);
            // Update Home
            (App.Current as App).CurrentHome.SetActiveVW(activeVW);
            // Change the VW associated with current scene
            UpdateCurrentState(activeVW.GetCurrentOpenVW());  // Switch to VW at that location, or create a default one
            // Update VW display
            ReloadContents();
            // <Development> Might also want Home to do a saving
        }
        private void VWSubjectName_MouseLeave(object sender, MouseEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            Keyboard.ClearFocus();
        }

        private void VWSubjectName_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            if (CurrentState != null)    // Can be null during initialization
            {
                // Just update VW name
                CurrentState.Name = VWSubjectName.Text;
                // Notice we are not doing a saving in this case -- though might be needed, but since our Home can get quite heavy we don't want to do a full saving that frequent
            }
        }

        private void VWLocationCompass_MouseLEftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Open location panel
            SpiralVWCoordinateGrid.Visibility = Visibility.Visible;
            e.Handled = true;
        }

        private void PackAsDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            // Put current VW into a document and replace current VW
            VirtualWorkspace packedVW = CurrentState;
            VirtualWorkspace replacement = (App.Current as App).CurrentHome.PackVWAndGetANewEmptyOne(CurrentState);
            // Update Home
            UpdateCurrentState(replacement);
            (App.Current as App).CurrentHome.SetActiveVW(CurrentState);
            // Reload contents
            ReloadContents();

            // Open previous VW under current VW's stack trace
            OpenDocument(packedVW, false);
        }

        // Convert to VA, then user can later convert to Clues in Collection browser
        private void ConvertVWToVAButton_Click(object sender, RoutedEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            Home home = (App.Current as App).CurrentHome;
            // Remove current active VW and substitute it with a another one
            VirtualWorkspace prevState = CurrentState;
            VirtualWorkspace replacement = (App.Current as App).CurrentHome.RemoveVWAndGetANewEmptyOne(CurrentState);
            // Update Home
            UpdateCurrentState(replacement);
            home.SetActiveVW(CurrentState);
            // Reload contents
            ReloadContents();

            // Pack the VW into a VA
            Archive archive =  home.ConvertVWToVAAndRegister(prevState);
            // Add to current page
            Point location = FindLocationForNextDocument();
            AddDocumentToCanvasDisplay(archive, location.X, location.Y);

            // Notify user
            Status.Update("A new VA is created.");
        }

        // Return a location that is not obsecured in current VW page
        private Point FindLocationForNextDocument()
        {
            // <Pending>
            return new Point(DocumentIconsCanvas.ActualWidth/2, DocumentIconsCanvas.ActualHeight/2);
        }

        private void VWStack_JumpBackInTrace(object sender, MouseButtonEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            VWStackTraceView view = (sender as FrameworkElement).DataContext as VWStackTraceView;
            Home home = Home.Current;

            // Open the VW document under current active VW's stack trace
            UpdateCurrentState(home.ActiveVW.OpenVWDocument(view.VW));
            // Update stack trave
            VWStackTrace = home.ActiveVW.GetStackTrace();
            // Reload
            ReloadContents();
        }
        #endregion
        #region Page Display Switch
        private void Page0_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState.PageIndex != 0)
            {
                CurrentState.PageIndex = 0;
                UpdatePageDisplay();
                // Might also want to save Home
            }
        }

        private void Page1_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState.PageIndex != 1)
            {
                CurrentState.PageIndex = 1;
                UpdatePageDisplay();
                // Might also want to save Home
            }
        }

        private void Page2_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState.PageIndex != 2)
            {
                CurrentState.PageIndex = 2;
                UpdatePageDisplay();
                // Might also want to save Home
            }
        }

        private void Page3_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState.PageIndex != 3)
            {
                CurrentState.PageIndex = 3;
                UpdatePageDisplay();
                // Might also want to save Home
            }
        }

        private void Page4_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentState.PageIndex != 4)
            {
                CurrentState.PageIndex = 4;
                UpdatePageDisplay();
                // Might also want to save Home
            }
        }

        #endregion
        #region Timer
        private System.Windows.Threading.DispatcherTimer UtilityTimer = null;    // Timer for practical usages
        private void TimerSet_Click(object sender, RoutedEventArgs e)
        {
            // Clear timer if any
            if (UtilityTimer != null) UtilityTimer.Stop();

            // Setup timer
            UtilityTimer = new System.Windows.Threading.DispatcherTimer();
            UtilityTimer.Tick += UtilityTimer_Tick; ;
            UtilityTimer.Interval = new TimeSpan(int.Parse(TimerHourText), int.Parse(TimerMinuteText), int.Parse(TimerSecondText));   // 30 min display time
            UtilityTimer.Start();

            // Notice
            Status.Update(string.Format("Timer \"{0}\" set for {1}:{2}:{3} span.", TimerRemindingMessage, TimerHourText, TimerMinuteText, TimerSecondText));

            // Hide
            TimerSettings.Visibility = Visibility.Collapsed;
        }

        private void UtilityTimer_Tick(object sender, EventArgs e)
        {
            Status.Update(StatusEnum.TimerEvent, TimerRemindingMessage, ActionsEnum.Acknowledge, PlacementEnum.UpperRight);
            UtilityTimer.Stop();
            UtilityTimer = null;
        }

        private void TimerCancel_Click(object sender, RoutedEventArgs e)
        {
            // Hide
            TimerSettings.Visibility = Visibility.Collapsed;
        }

        private void TimerClear_Click(object sender, RoutedEventArgs e)
        {
            // Clear timer if any
            if (UtilityTimer != null)
            {
                UtilityTimer.Stop();
                UtilityTimer = null;
                // Notice
                Status.Update(string.Format("Timer \"{0}\" has been cleared.", TimerRemindingMessage));
            }

            ShowTimerSettings();
        }

        private void ShowTimerSettings()
        {
            // Configure display
            if(UtilityTimer != null)
            {
                SetNewTimerOptions.Visibility = Visibility.Collapsed;
                CancelExistingTimerOptions.Visibility = Visibility.Visible;

                TimerHourTextBox.IsReadOnly = true;
                TimerMinuteTextBox.IsReadOnly = true;
                TimerSecondTextBox.IsReadOnly = true;
                TimerRemindingMessageTextBox.IsReadOnly = true;
            }
            else
            {
                SetNewTimerOptions.Visibility = Visibility.Visible;
                CancelExistingTimerOptions.Visibility = Visibility.Collapsed;

                TimerHourTextBox.IsReadOnly = false;
                TimerMinuteTextBox.IsReadOnly = false;
                TimerSecondTextBox.IsReadOnly = false;
                TimerRemindingMessageTextBox.IsReadOnly = false;
            }

            // Show it
            TimerSettings.Visibility = Visibility.Visible;
        }
        #endregion
        #endregion

        #region VW Background Services
        // Reuse timer, though not sure whether Stop() disposes the timer - what if we do want to dispose it?
        private System.Windows.Threading.DispatcherTimer wallpaperTimer;    // Timer to loop wallpapers
        private List<string> WallpaperImagesFileLocation;   // Loaded image URIs
        private List<string>.Enumerator CurrentDisplayWallpaper;    // Current displayed iamge location
        // Media Play
        private List<string> BackgroundPlayMediaFilesLocation;   // Background medias to play
        private List<string>.Enumerator CurrentPlayBackgroundMedia;
        private Playlist Playlist = null;   // Any playlist loaded to play
        private System.Windows.Threading.DispatcherTimer backgroundMediaRestartTimer;    // We cannot just change state when vlcPlayer stopped because it's not allowed in the event; we need to wait for a while before playing a new one

        private string GetNextMedia()
        {
            while (CurrentPlayBackgroundMedia.MoveNext())
            {
                return CurrentPlayBackgroundMedia.Current;
            }
            // If we are breaking because reaching end, reset and do it again
            CurrentPlayBackgroundMedia = BackgroundPlayMediaFilesLocation.GetEnumerator();
            while (CurrentPlayBackgroundMedia.MoveNext())
            {
                return CurrentPlayBackgroundMedia.Current;
            }
            // If we are breaking again because reaching end then no supported file, return null
            return null;
        }

        private ImageSource GetNextImage()
        {
            bool bSecondRound = false;
            while (CurrentDisplayWallpaper.MoveNext())    // Unsupported format handling
            {
                try
                {
                    ImageSource source = new BitmapImage(new Uri(CurrentDisplayWallpaper.Current));
                    return source; // Exit the loop
                }
                catch { }// Do nothing 
            }
            // If we are breaking because reaching end, reset and do it again (because we might have legit ones at the beginning locations)
            bSecondRound = true;
            CurrentDisplayWallpaper = WallpaperImagesFileLocation.GetEnumerator();
            while (CurrentDisplayWallpaper.MoveNext())    // Unsupported format handling
            {
                try
                {
                    ImageSource source = new BitmapImage(new Uri(CurrentDisplayWallpaper.Current));
                    return source; // Exit the loop
                }
                catch { }   //  Do nothing
            }
            // If we are breaking again because reaching end then no supported format was found, use the default one then
            if (bSecondRound == true) return new BitmapImage(new Uri("pack://application:,,,/Resource/Images/Background.jpg"));
            return null;
        }
        // Stop wallpaper timer and reload wallpapers
        private void UpdateWallpaerDisplay()
        {
            // Find images; If none if found, use a default
            List<Document> foundDocuments;
            WallpaperImagesFileLocation = new List<string>();
            foundDocuments = ClueManager.Manager.GetDocuments(new Clue(CurrentState.BackgroundImageClue));
            // Reload Images Lists
            if (foundDocuments == null || foundDocuments.Count == 0) WallpaperImagesFileLocation.Add("pack://application:,,,/Resource/Images/Background.jpg");
            else
            {
                foreach (Document doc in foundDocuments)
                {
                    WallpaperImagesFileLocation.Add(doc.Path);
                }
            }
            CurrentDisplayWallpaper = WallpaperImagesFileLocation.GetEnumerator();
            // Display the first one
            BackgroundImage.Source = GetNextImage();
            //(this.Background as ImageBrush).ImageSource = new BitmapImage(new Uri(imageLocation));

            // Restart Timer
            wallpaperTimer.Start();
        }
        private void UpdateRhythmPlay()
        {
            // Find images; If none is found, do nothing
            List<Document> foundDocuments;
            BackgroundPlayMediaFilesLocation = new List<string>();
            foundDocuments = ClueManager.Manager.GetDocuments(new Clue(CurrentState.BackgroundMelodyClue));
            // Reload media Lists
            if (foundDocuments != null && foundDocuments.Count > 0)
            {
                foreach (Document doc in foundDocuments)
                {
                    BackgroundPlayMediaFilesLocation.Add(doc.Path);
                }
            }
            else BackgroundPlayMediaFilesLocation.Clear();
            CurrentPlayBackgroundMedia = BackgroundPlayMediaFilesLocation.GetEnumerator();
            // Play the first one
            string next = GetNextMedia();
            if (next != null) PlayMedia(next);
            else vlcPlayer.Stop();
        }
        public VlcPlayer GetVlcPlayerHandler()
        {
            return vlcPlayer;
        }
        private void VlcPlayer_StateChanged(object sender, Meta.Vlc.ObjectEventArgs<Meta.Vlc.Interop.Media.MediaState> e)
        {
            if(e.Value == Meta.Vlc.Interop.Media.MediaState.Ended)
            {
                // Schedule next play
                backgroundMediaRestartTimer.Start();
            }
        }
        private void DispatcherTimer_TimeUp(object sender, EventArgs e)
        {
            // Change current displayed image
            //Image image = new Image();
            //image.Source = new BitmapImage(new Uri(imageLocation));
            BackgroundImage.Source = GetNextImage();
            //(this.Background as ImageBrush).ImageSource = new BitmapImage(new Uri(imageLocation));

            // Restart timer: no need to explicitly restart
            // wallpaperTimer.Start();
        }
        private void BackgroundMediaRestartTimer_TimeUp(object sender, EventArgs e)
        {
            // Play the next
            string next = null;
            if (Playlist != null)
            {
                Document nextDoc = Playlist.MoveToNextMedia();
                if (nextDoc != null) next = nextDoc.Path;
            }
            else next = GetNextMedia();
            if (next != null) PlayMedia(next);

            // Stop timer
            backgroundMediaRestartTimer.Stop();
        }
        #endregion

        #region Keyboard Shortcuts
        // Disable Alt-F4 for closing main window: http://shaviraghu.blogspot.ca/2009/12/introduction-this-article-shows-you-how.html
        // https://stackoverflow.com/questions/13854889/wpf-how-to-distinguish-between-window-close-call-and-system-menu-close-action
        private bool bAltF4Pressed = false;
        private string UnlockingString;
        // Navigation: SHIFT + Arrow key - switch VW
        // (Deprecated) Ctrl+Alt+Delete: End/Restart Windows Explorer
        // (Pending) Canvas View: Ctrl - show space shortcuts; Ctrl + FnKey - Shift to canvs space
        // Ctrl + F: search
        // Ctrl + T: Timer
        // (Pending) Ctrl + L: Lock, show blurred with lock icon lock screen; Secrete password enter with ESC clear to unlock; Make it application-wise lock, not system wise since no point anyway a harddisk can be read using another computer
        // Delete: Delete selected document(s)
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Specially handled during lock
            if (LockscreenVisibility == Visibility.Visible)
            {
                if (e.Key == Key.Escape) UnlockingString = string.Empty;
                else UnlockingString += SystemHelper.GetCharFromKey_SimulatedKeyboard(e.Key);
                if (UnlockingString == Home.Current.Password.Content) UnLock();
                e.Handled = true; return;
            }

            #region Unlocked state actions
            if (LockscreenVisibility != Visibility.Visible)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    switch (e.Key)
                    {
                        case Key.T:
                            if (TimerSettings.Visibility == Visibility.Collapsed) ShowTimerSettings();
                            else if (TimerSettings.Visibility == Visibility.Visible) TimerSettings.Visibility = Visibility.Collapsed;
                            e.Handled = true;
                            break;
                        case Key.F:
                            if (bUIHidden) break;
                            TopSearchBar.DoGetFocus();
                            e.Handled = true;
                            break;
                        case Key.L:
                            Lock();
                            e.Handled = true;
                            break;
                        case Key.U:
                            ToggleHideUI();
                            e.Handled = true;
                            break;
                    }
                    ShowCanvasSpaceButtonShortcutLabels();
                }
                else if (e.Key == Key.Delete)
                {
                    if (SelectedIcons.Count != 0)
                    {
                        foreach (IconBase icon in SelectedIcons)
                        {
                            RemoveDocumentFromCanvasDisplay(icon);
                        }
                        SelectedIcons.Clear();
                    }
                    e.Handled = true;
                }
                else
                {
                    // Canvas Space
                    switch (e.Key)
                    {
                        case Key.F1:
                            OpenWebBrowser(); e.Handled = true; break;
                        case Key.F2:
                            OpenMediaPlayer(); e.Handled = true; break;
                        case Key.F3:
                            OpenTableViewer(); e.Handled = true; break;
                        case Key.F4:
                            OpenDelightfulBrowser(); e.Handled = true; break;
                        case Key.F5:
                            OpenMarkdownPlusEditor(); e.Handled = true; break;
                        case Key.F6:
                            OpenGraphEditor(); e.Handled = true; break;
                        case Key.F7:
                            OpenArchiveViewer(); e.Handled = true; break;
                        case Key.F8:
                            OpenCollectionCreator(); e.Handled = true; break;
                        case Key.F9:
                            OpenClueBrowser(); e.Handled = true; break;
                        case Key.System:
                            if(e.SystemKey == Key.F10)
                            {
                                OpenQuickMatch();
                                e.Handled = true;
                            }
                            break;
                        case Key.F11:
                            OpenForgottenUniverse(); e.Handled = true; break;
                        case Key.F12:
                            OpenVoidUniverse(); e.Handled = true; break;
                    }
                }

                // VW navigation
                if ((Keyboard.IsKeyDown(Key.LeftShift) || e.Key == Key.LeftShift) && SelectedIcons.Count == 0 && VWSubjectName.IsFocused == false)
                {
                    SpiralVWCoordinateGrid.Visibility = Visibility.Visible;
                    switch (e.Key)
                    {
                        case Key.Up:
                            SwitchToVWAtCoordinate(VirtualWorkspace.ShiftUpLocation(Home.Current.ActiveVW.VWCoordinate.Value));
                            break;
                        case Key.Down:
                            SwitchToVWAtCoordinate(VirtualWorkspace.ShiftDownLocation(Home.Current.ActiveVW.VWCoordinate.Value));
                            break;
                        case Key.Left:
                            SwitchToVWAtCoordinate(VirtualWorkspace.ShiftLeftLocation(Home.Current.ActiveVW.VWCoordinate.Value));
                            break;
                        case Key.Right:
                            SwitchToVWAtCoordinate(VirtualWorkspace.ShiftRightLocation(Home.Current.ActiveVW.VWCoordinate.Value));
                            break;
                    }
                    e.Handled = true;
                }
                else SpiralVWCoordinateGrid.Visibility = Visibility.Collapsed;
            }
            #endregion  

            // Alt-F4 handling for closing window
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4) bAltF4Pressed = true;
            else bAltF4Pressed = false; 
        }

        private bool bUIHidden = false;
        private void ToggleHideUI()
        {
            if(bUIHidden)
            {
                LeftSideSpaceSwitchPane.Visibility = Visibility.Visible;
                AbosolutePositionedUI.Visibility = Visibility.Visible;
            }
            else
            {
                LeftSideSpaceSwitchPane.Visibility = Visibility.Collapsed;
                AbosolutePositionedUI.Visibility = Visibility.Collapsed;    // Notice currently Virtual Workspace Stack is hidden as well because it's part of AbsolutePositionedUI, well I think that is expected

                // Hide Panels
                if (DocPanel.Visibility == Visibility.Visible) DocPanel.Visibility = Visibility.Collapsed;
                if (Status.Visibility == Visibility.Visible) Status.Visibility = Visibility.Collapsed;  // Just hide for once, in case other evetns happen we still need to show it
                if (SharedSearchPopup.Visibility == Visibility.Visible) SharedSearchPopup.Visibility = Visibility.Collapsed;
                if (BatchPanel.Visibility == Visibility.Visible) BatchPanel.Visibility = Visibility.Collapsed;

            }
            bUIHidden = !bUIHidden;
        }

        private void ShowCanvasSpaceButtonShortcutLabels()
        {
            if (bUIHidden) return;
            CanvasSpaceShortCutLabelVisibility = Visibility.Visible;
        }
        private void HideCanvasSpaceButtonShortcutLabels()
        {
            CanvasSpaceShortCutLabelVisibility = Visibility.Collapsed;
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // Disabled during lock
            if (LockscreenVisibility == Visibility.Visible) { e.Handled = true; return; }

            if (e.Key == Key.LeftShift) SpiralVWCoordinateGrid.Visibility = Visibility.Collapsed;
            else if (e.Key == Key.LeftCtrl) HideCanvasSpaceButtonShortcutLabels();
        }
        private void Lock()
        {
            // Check user info availability
            if (Home.Current.IsUserSet)
            {
                if (Home.Current.Password.Content.Where(c => "1234567890abcdefghijklmnopqrstuvwxyz".Contains(char.ToLower(c)) == false).Count() > 0)
                    Status.Update("For technical reasons currently lockscreen only supports password combinations of lower/upper english characters and numbers only. Sorry for the inconvinience.");
                else
                {
                    // Reset
                    UnlockingString = string.Empty;
                    // Show Lockscreen
                    LockscreenVisibility = Visibility.Visible;
                    // Update home status
                    Home.Current.Lock();
                }
            }
            else
                Status.Update("Setup user account information first before locking.");
        }
        private void UnLock()
        {
            // Show Lockscreen
            LockscreenVisibility = Visibility.Collapsed;
            // Update home status
            Home.Current.UnLock();
        }
        #endregion

        #region Media and Content Interaction
        public void UpdateStatus(string text)
        {
            Status.Update(text);
        }
        public void StatusPromotCallback(bool bOption)
        {

        }


        private void PlayMedia(string path)
        {
            vlcPlayer.LoadMedia(path);
            vlcPlayer.Tag = path;
            vlcPlayer.Play();

            // <Pending> Show "Notice" status
            Status.UpdateNotice("Now playing: " + System.IO.Path.GetFileName(path));
        }

        private void PlayMedia(Document target)
        {
            PlayMedia(target.Path);
        }

        /// <summary>
        /// Open a document in destinated viewer/editor canvas page or integrated light weight browser
        /// If insepect for certain type isn't available then viewr/editor will be opened
        /// </summary>
        /// <param name="bInspect">If bInspect is specified, we try to open in an integrated light weight browswer rather than full canvas view space</param>
        internal void OpenDocument(Document target, bool bInspect)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) bInspect = true;
            switch (target.Type)
            {
                case DocumentType.PlainText:
                    // Use native editors
                    System.Diagnostics.Process.Start(target.Path);
                    break;
                case DocumentType.MarkdownPlus:
                    OpenMarkdownPlusEditor(target);
                    break;
                case DocumentType.Archive:
                    // View using archive browser
                    if (bInspect) OpenArchiveViewer(target);
                    // Edit using collection editor
                    else OpenCollectionCreator(target);
                    break;
                case DocumentType.VirtualArchive:
                    OpenCollectionCreator(target);
                    break;
                case DocumentType.DataCollection:
                    throw new NotImplementedException(); break;
                case DocumentType.Graph:
                    OpenGraphEditor(target);
                    break;
                case DocumentType.Command:
                    System.Diagnostics.Process.Start(target.Path, (target as Command).Parameters);
                    // <Pending> Better, more useful implementation; Currently commands are a very thin wrapper, e.g. let comamnd provide static facilities to execute itself, with console output only (scripts), output to a file (scripts or console output applications), and execute as normal executable options
                    break;
                case DocumentType.Web:
                    throw new NotImplementedException(); break;
                case DocumentType.PlayList:
                    throw new NotImplementedException(); break;
                case DocumentType.ImagePlus:
                    OpenDelightfulBrowser(target);
                    break;
                case DocumentType.Sound:
                case DocumentType.Video:
                    if (bInspect) PlayMedia(target);
                    else OpenMediaPlayer(target);
                    break;
                case DocumentType.VirtualWorkspace:
                    // Open the VW document under current active VW's stack trace
                    VirtualWorkspace activeVW = (App.Current as App).CurrentHome.ActiveVW;
                    UpdateCurrentState(activeVW.OpenVWDocument(target as VirtualWorkspace));
                    // Update stack trave
                    VWStackTrace = activeVW.GetStackTrace();
                    // Reload
                    ReloadContents();
                    break;
                case DocumentType.Others:
                case DocumentType.Unkown:
                default:
                    try{ System.Diagnostics.Process.Start(target.Path); }
                    catch (Exception){ Status.UpdateNotice("No suitable software to open the file."); }
                    break;
            }
        }
        // Open an webpage in integrated browser: input can be an url or keyword, default Google search
        public void OpenWebpage(string input)
        {
            OpenWebBrowser(input);
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

        private ObservableCollection<VWStackTraceView> _VWStackTrace;
        public ObservableCollection<VWStackTraceView> VWStackTrace
        {
            get { return this._VWStackTrace; }
            set
            {
                if (value != this._VWStackTrace)
                {
                    this._VWStackTrace = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private Visibility _CanvasSpaceShortCutLabelVisibility = Visibility.Collapsed;
        public Visibility CanvasSpaceShortCutLabelVisibility
        {
            get { return this._CanvasSpaceShortCutLabelVisibility; }
            set
            {
                if (value != this._CanvasSpaceShortCutLabelVisibility)
                {
                    this._CanvasSpaceShortCutLabelVisibility = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Visibility _LockscreenVisibility = Visibility.Collapsed;
        public Visibility LockscreenVisibility
        {
            get { return this._LockscreenVisibility; }
            set
            {
                if (value != this._LockscreenVisibility)
                {
                    this._LockscreenVisibility = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // Timer
        private string _TimerRemindingMessage = "Enter reminding message here.";
        public string TimerRemindingMessage
        {
            get { return this._TimerRemindingMessage; }
            set
            {
                if (value != this._TimerRemindingMessage)
                {
                    this._TimerRemindingMessage = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _TimerHourText = "00";
        public string TimerHourText
        {
            get { return this._TimerHourText; }
            set
            {
                if (value != this._TimerHourText)
                {
                    this._TimerHourText = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _TimerMinuteText = "00";
        public string TimerMinuteText
        {
            get { return this._TimerMinuteText; }
            set
            {
                if (value != this._TimerMinuteText)
                {
                    this._TimerMinuteText = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _TimerSecondText = "00";
        public string TimerSecondText
        {
            get { return this._TimerSecondText; }
            set
            {
                if (value != this._TimerSecondText)
                {
                    this._TimerSecondText = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
