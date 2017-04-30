using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using Ookii.Dialogs.Wpf;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using UnManaged;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MULTITUDE
{
    // Notice in this window QuickMatch isn't used for specific QuickMatch folders and as a matter of fact we don't even recognize it, but used for searching
    public partial class MagicWindow : Window
    {
        public MagicWindow()
        {
            InitializeComponent();

            // Magic Explorer will be built as a seperate program and we might use command line in the future

            // Check out whether a default JSON structure is available, near the executable program
            string DefaultJSONFileLocation = AppDomain.CurrentDomain.BaseDirectory + "\\" + App.DefaultJSONFileName;
            if (File.Exists(DefaultJSONFileLocation))
            {
                UpdateFolderStructureView(DefaultJSONFileLocation);
                FocusOnSearchBox();
            }

            // Check out Reference Directory
            string ReferenceDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\" + App.ReferenceDirectoryName;
            if (Directory.Exists(ReferenceDirectory) == false)
            {
                Directory.CreateDirectory(ReferenceDirectory);
            }

            // Check out UserHome
            string UserDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\" + App.UserHome;
            if (Directory.Exists(UserDirectory) == false)
            {
                Directory.CreateDirectory(UserDirectory);
            }

            // Check out AppHome
            string AppDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\" + App.AppHome;
            if (Directory.Exists(AppDirectory) == false)
            {
                Directory.CreateDirectory(AppDirectory);
            }

            // Adjust Caret Thickness
            // https://www.codeproject.com/Articles/633935/Customizing-the-Caret-of-a-WPF-TextBox
            // http://stackoverflow.com/questions/894196/windows-forms-how-to-do-a-thick-blinking-cursor-on-a-textbox   -- WinForm
            this.SearchKeywordBox.SelectionChanged += (sender, e) => MoveCustomCaret();
            this.SearchKeywordBox.LostFocus += (sender, e) => Caret.Visibility = Visibility.Collapsed;
            this.SearchKeywordBox.GotFocus += (sender, e) => Caret.Visibility = Visibility.Visible;

            // Register Global Hotkeys
            HotKey _hotKey = new HotKey(Key.Q, KeyModifier.Shift | KeyModifier.Ctrl, ShowWindow);

            // Simple Implementation for keyboard shortcuts
            // http://stackoverflow.com/questions/813389/how-to-capture-ctrl-tab-and-ctrl-shift-tab-in-wpf
            AddHandler(Keyboard.KeyDownEvent, (KeyEventHandler)HandleKeyDownEvent);

            // Initialize voice recognition
            voiceEngine.updateGrammar(new List<string>() { "tell me about weather", "thank you" }, SpeechHandler);
        }

        Airi.TheSystem.Perception.Voice voiceEngine = new Airi.TheSystem.Perception.Voice();

        void SpeechHandler(Airi.TheSystem.Perception.Voice.SpeechStatus status, List<string> messages)
        {
            if(status!= Airi.TheSystem.Perception.Voice.SpeechStatus.Rejected && messages.Count != 0)
            {
                switch (messages[0])
                {
                    case "tell me about weather":
                        voiceEngine.SynthesizeSpeech("Working on it...");
                        //List<Tuple<string, Airi.TheSystem.Perception.Voice.SpeechTone>> sentences = 
                        //    new List<Tuple<string, Airi.TheSystem.Perception.Voice.SpeechTone>>() { new Tuple<string, Airi.TheSystem.Perception.Voice.SpeechTone>(Airi.Facilities.Weather.GetWeather(), Airi.TheSystem.Perception.Voice.SpeechTone.Normal) };                
                        //voiceEngine.BuildSpeech(sentences);
                        voiceEngine.SynthesizeSpeech(Airi.Facilities.Weather.GetWeather());
                        break;
                    case "thank you":
                        voiceEngine.SynthesizeSpeech("You are welcome");
                        break;
                    default:
                        break;
                }
            }
        }

        private void MoveCustomCaret()
        {
            var caretLocation = SearchKeywordBox.GetRectFromCharacterIndex(SearchKeywordBox.CaretIndex).Location;

            if (!double.IsInfinity(caretLocation.X))
            {
                Canvas.SetLeft(Caret, caretLocation.X);
            }

            if (!double.IsInfinity(caretLocation.Y))
            {
                Canvas.SetTop(Caret, caretLocation.Y);
            }
        }

        // Input Handling
        private void ShowWindow(HotKey hotKey)
        {
            // http://stackoverflow.com/questions/5531548/how-to-restore-a-minimized-window-in-code-behind

            // First Show Window
            // this.Visibility = Visibility.Visible;    // Not needed
            this.WindowState = WindowState.Normal;

            //// Then Set Location
            //// http://stackoverflow.com/questions/6071372/maximize-wpf-window-on-the-current-screen
            //// http://stackoverflow.com/questions/1927540/how-to-get-the-size-of-the-current-screen-in-wpf
            //double width = System.Windows.SystemParameters.PrimaryScreenWidth;
            //double height = System.Windows.SystemParameters.PrimaryScreenHeight;
            //this.Left = 120;//(width - this.Width) / 2;
            //this.Top = 120; //(height - this.Height) / 2;

            // Also set focus
            this.Activate(); // http://stackoverflow.com/questions/257587/bring-a-window-to-the-front-in-wpf
            Keyboard.Focus(SearchKeywordBox);
        }

        enum ContentSearchMode
        {
            SingleFile,
            MultiFile,
            SingleFolder,
            MultiFolder,
            Advanced
        }
        ContentSearchMode CurrentSearchMode = ContentSearchMode.SingleFile;
        enum AiriMode
        {
            Activated,
            Disabled
        }
        AiriMode CurrentAiriMode = AiriMode.Activated;
        JFile PreviousOpenFile = null;
        // App-wise Short cut setup
        // http://stackoverflow.com/questions/4682915/defining-menuitem-shortcuts
        // http://stackoverflow.com/questions/3574405/c-wpf-implement-keyboard-shortcuts
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/2b2121f9-ef4e-4e38-8442-763f608e1837/how-to-create-keyboard-shortcuts-in-wpf?forum=wpf input bindings
        // Also see personal WPF notes
        private void HandleKeyDownEvent(object sender, KeyEventArgs e)
        {
            // Example: if (e.Key == Key.H && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))

            // Ctrl-H Open Home Folder (Application Home)
            if (e.Key == Key.H && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                System.Diagnostics.Process.Start(AppDomain.CurrentDomain.BaseDirectory + "\\" + App.UserHome);
                e.Handled = true;
            }
            // ESC: Hide Application (Minimize)
            if (e.Key == Key.Escape)
            {
                this.WindowState = WindowState.Minimized;
                e.Handled = true;
            }
            // Ctrl-O: Load JSON file shortcut: I know this is a bit not so professional but WHY I CANNOT FIND MY VS PROJECT for 墨迹笔记？！
            if(e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                LoadJSONFile_MouseDown(null, null);
                e.Handled = true;
            }
            // Ctrl-F: Search
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Keyboard.Focus(SearchKeywordBox);
                SearchKeywordBox.SelectAll();
                e.Handled = true;
            }
            // Ctrl/Shift/Alt-P: Open previously opened file
            if (e.Key == Key.P)
            {
                // If used SHIFT then open file location
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    OpenFolderForInspection(PreviousOpenFile.Parent);
                    e.Handled = true;
                }
                // If used Alt to open properties
                else if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                {
                    SystemInterpService.ShowFileProperties(App.GetParentFolderPath(PreviousOpenFile.Parent) + PreviousOpenFile.FileName);
                    e.Handled = true;
                }
                // Otherwise just open file
                else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    OpenFileForEditing(PreviousOpenFile);
                    e.Handled = true;
                }
            }
            // F1: File Mode
            if (e.Key == Key.F1)
            {
                if (CurrentSearchMode == ContentSearchMode.SingleFile)
                {
                    UpdateSearchMode(ContentSearchMode.MultiFile);
                }
                else
                {
                    FolderCheckBox.IsChecked = false;   // Notice when the state of check box changes it will trigger its checked/unchecked handler where we have already set the value
                    UpdateSearchMode(ContentSearchMode.SingleFile);
                }
                e.Handled = true;
            }
            // F2: Folder Mode
            if (e.Key == Key.F2)
            {
                if (CurrentSearchMode == ContentSearchMode.SingleFolder)
                {
                    UpdateSearchMode(ContentSearchMode.MultiFolder);
                }
                else
                {
                    FolderCheckBox.IsChecked = true;
                    UpdateSearchMode(ContentSearchMode.SingleFolder);
                }
                e.Handled = true;
            }
            // F3: Airi on/off
            if (e.Key == Key.F3)
            {
                if (CurrentAiriMode == AiriMode.Activated)
                {
                    UpdateAiriMode(AiriMode.Disabled);
                    voiceEngine.Deactivate();
                }
                else
                {
                    UpdateAiriMode(AiriMode.Activated);
                    voiceEngine.Activate();
                }
                e.Handled = true;
            }
            // F4: Dictionary Mode
        }

        void UpdateSearchMode(ContentSearchMode newSearchMode)
        {
            // Update Search Mode
            CurrentSearchMode = newSearchMode;

            // Update Search Mode Text
            switch (CurrentSearchMode)
            {
                case ContentSearchMode.SingleFile:
                    SearchModeLabel.Content = "Search Single File";
                    break;
                case ContentSearchMode.MultiFile:
                    SearchModeLabel.Content = "Search Multi Files";
                    break;
                case ContentSearchMode.SingleFolder:
                    SearchModeLabel.Content = "Search Single Folder";
                    break;
                case ContentSearchMode.MultiFolder:
                    SearchModeLabel.Content = "Search Multi Folders";
                    break;
                case ContentSearchMode.Advanced:
                    SearchModeLabel.Content = "Advanced";
                    break;
                default:
                    break;
            }

            // Force Research if we have any input
            SearchForContent();
        }

        void UpdateAiriMode(AiriMode newAiriMode)
        {
            // Update Airi Mode
            CurrentAiriMode = newAiriMode;

            // Update Interface Hint
            switch (CurrentAiriMode)
            {
                case AiriMode.Activated:
                    AiriLabel.Content = "·";
                    break;
                case AiriMode.Disabled:
                    AiriLabel.Content = "";
                    break;
                default:
                    break;
            }
            switch (CurrentSearchMode)
            {
                case ContentSearchMode.SingleFile:
                    SearchModeLabel.Content = "Search Single File";
                    break;
                case ContentSearchMode.MultiFile:
                    SearchModeLabel.Content = "Search Multi Files";
                    break;
                case ContentSearchMode.SingleFolder:
                    SearchModeLabel.Content = "Search Single Folder";
                    break;
                case ContentSearchMode.MultiFolder:
                    SearchModeLabel.Content = "Search Multi Folders";
                    break;
                case ContentSearchMode.Advanced:
                    SearchModeLabel.Content = "Advanced";
                    break;
                default:
                    break;
            }
        }

        // JSON Generation
        private JFolder JRootFolder;
        private JFolder JRootFolder_Filtered;

        // MultiThreading
        private List<Task> searchingTaskList = new List<Task>();
        private List<CancellationTokenSource> searchingTaskTokenSourceList = new List<CancellationTokenSource>(); // Current Deployed Tokens: The first being oldest, the last being newest
        // private List<string> historyKeywordsList = new List<string>();   // Not neeeded and might cause IndexOutofRange if not used properly

        private void OpenFileForEditing(JFile file)
        {
            // Get file pathh
            string filePath = App.GetParentFolderPath(file.Parent) + file.FileName;

            // If file exist on local machine
            if(File.Exists(filePath))
            {
                try
                {
                    // Open file in system editor
                    System.Diagnostics.Process.Start(filePath);

                    // Update Status
                    StatusLabel.Content = "File opened.";
                }
                catch (System.ComponentModel.Win32Exception) { }
            }
            // Other wise we use a Reference file
            else
            {
                // Update Status
                StatusLabel.Content = "Original file cannot be found, use a reference file instead.";

                string referenceFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + App.ReferenceDirectoryName + "\\" + file.FileName;
                // If not already exists then create one
                if(File.Exists(referenceFilePath) == false)
                {
                    File.WriteAllText(referenceFilePath, string.Format(
                           "You are writing in a reference file! Original file cannot be found on this computer, we created a reference file for editing purpose.\n<Original file location>{0}\n<Current file location>{1}", filePath, referenceFilePath));
                }

                // Then open it
                try
                {
                    // Open file in system editor
                    System.Diagnostics.Process.Start(referenceFilePath);
                }
                catch (System.ComponentModel.Win32Exception) { }
            }

            // Auto Hide
            this.WindowState = WindowState.Minimized;
        }


        // http://stackoverflow.com/questions/19197376/check-if-task-is-already-running-before-starting-new
        // https://msdn.microsoft.com/en-us/library/dd997396(v=vs.110).aspx
        // http://stackoverflow.com/questions/7450789/what-is-the-correct-way-to-cancel-multiple-tasks-in-c-sharp
        // Every time TextChanged, unless it changed to blank, we will dispatch a new task searching, while cancelling and collecting previous tasks
        private void SearchKeywordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchForContent();
        }

        List<JFolder> updatedFolders = new List<JFolder>();
        void SearchForContent()
        {
            // Return to normal if no text in search box
            if (SearchKeywordBox.Text == "")
            {
                // Unexpand previously expanded folders if any
                foreach (JFolder folder in updatedFolders)
                {
                    JFolder parent = folder.Parent;
                    while (parent != null)
                    {
                        parent.bExpanded = false;
                        parent = parent.Parent;
                    }
                }
                updatedFolders.Clear();

                // Cancel any running task
                lock (searchingTaskList)
                {
                    for (int i = 0; i < searchingTaskList.Count; i++)
                    {
                        // If an uncompleted task
                        if (searchingTaskList[i].IsCompleted == false ||
                                           searchingTaskList[i].Status == TaskStatus.Running ||
                                           searchingTaskList[i].Status == TaskStatus.WaitingToRun ||
                                           searchingTaskList[i].Status == TaskStatus.WaitingForActivation)
                        {
                            // Cancel tasks
                            // searchingTaskTokenSourceList.Last().Cancel(); -> This seem like a mistake, why at the last instead of the index itself?
                            searchingTaskTokenSourceList[i].Cancel();
                        }
                    }
                }

                // Revert back to display
                List<JFolder> Roots = new List<JFolder>();
                Roots.Add(JRootFolder);
                DirectoryView.ItemsSource = Roots;

                // Status Update
                StatusLabel.Content = "Show folder structure.";

                return; // Return for now because cancelling tasks can take some time to respond, we will garbage collect tokens at next time text change in search box
            }
            else
            {
                // Update Status
                StatusLabel.Content = "Working on matching files...";

                // Dispatch Searching Task for Data Generation
                DispatchNewJob();
            }

            // Collect and finish previously completed/cancelled tasks
            lock (searchingTaskList)
            {
                for (int i = 0; i < searchingTaskList.Count; i++)
                {
                    if (searchingTaskList[i].IsCompleted == true || searchingTaskList[i].IsCanceled == true)
                    {
                        // Remove Tasks and dispose token
                        searchingTaskList.RemoveAt(i);
                        searchingTaskTokenSourceList[i].Dispose();
                        searchingTaskTokenSourceList.RemoveAt(i);
                        // historyKeywordsList.RemoveAt(i);
                    }
                }
            }
        }

        private async void DispatchNewJob()
        {
            // http://stackoverflow.com/questions/30248572/how-to-catch-an-operationcanceledexception-when-using-continuewith
            /* Multi-Threaded Version*/
            try
            {
                // Create token for use
                Task searchingTask;
                lock (searchingTaskList)    // Notice that lock only gurantees that no two threads are to enter this block of code at the same time but not necessarily guarantee that different threads not accesssing this
                                            // locked object at the same time, meaning at the time a new job is dispatched, a previous one could have been cancelled/completed and code second above in SearchKeywordBox_TextChanged's
                                            // lock section could have been executing, which will cause some elements in our lists being deleted etc.
                {
                    // Backup keywords for later use
                    string keywords = SearchKeywordBox.Text;
                    bool? bFolderMode = FolderCheckBox.IsChecked;
                    // historyKeywordsList.Add(keywords);
                    // Create tokens
                    CancellationTokenSource tokenSource = new CancellationTokenSource();
                    searchingTaskTokenSourceList.Add(tokenSource);
                    // http://stackoverflow.com/questions/8127316/passing-a-method-parameter-using-task-factory-startnew
                    searchingTask = Task.Factory.StartNew(() => FileFilter(keywords, tokenSource.Token, bFolderMode), tokenSource.Token);
                    searchingTaskList.Add(searchingTask);
                }

                await searchingTask;
            }
            catch (TaskCanceledException) { return; }

            // After Everything is done without being cancelled we fetch our search results and show it
            List<JFolder> Roots = new List<JFolder>();
            Roots.Add(JRootFolder_Filtered);
            DirectoryView.ItemsSource = Roots;

            // Update Status
            StatusLabel.Content = "Job Done!";
        }

        // http://stackoverflow.com/questions/7343211/cancelling-a-task-is-throwing-an-exception
        private Object newLocker = new Object();
        private void FileFilter(string keywrods, CancellationToken token, bool? bSearchFolder)
        {
            try
            {
                // Wait for 10 ms in case someone is typing fast so we don't waste resources
                Thread.Sleep(10);

                // Were we already canceled?
                token.ThrowIfCancellationRequested();

                // Do the job for some time
                // ... Currenlty we do not bother dividing the job into smaller pieces

                // Poll to make sure we are not cancelling before continue on more job
                //if (token.IsCancellationRequested)
                //{
                //    // No clean up to do, so we just stop ourselves
                //    token.ThrowIfCancellationRequested();
                //}
                //else
                //{
                    // Lock so we don't mess things up
                    lock (newLocker)
                    {
                        if(bSearchFolder == true)
                        {
                            JRootFolder_Filtered = QuickMatch.QuickMatchFolder(keywrods, JRootFolder, CurrentSearchMode == ContentSearchMode.MultiFolder);
                        }
                        else
                        {
                            JRootFolder_Filtered = QuickMatch.QuickMatchFile(keywrods, JRootFolder, CurrentSearchMode == ContentSearchMode.MultiFile);
                        }
                    }
                    /* Here are some information on lock vs mutex
                     * http://stackoverflow.com/questions/34524/what-is-a-mutex
                     * http://stackoverflow.com/questions/3735293/what-is-the-difference-between-lock-and-mutex
                     * https://msdn.microsoft.com/en-us/library/c5kehkcz.aspx
                     * http://stackoverflow.com/questions/6029804/how-does-lock-work-exactly
                     */
                //}

                // Were we actually canceled?
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {

            }
        }

        private void LoadJSONFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Select and open a file
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.DefaultExt = ".json";
            openFileDialog.DefaultExt = "JSON Files(*.json) | *.json";
            openFileDialog.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            openFileDialog.Title = "Select which JSON file to load";
            openFileDialog.ValidateNames = true;
            openFileDialog.FileName = App.DefaultJSONFileName;
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                UpdateFolderStructureView(openFileDialog.FileName);
                this.WindowState = WindowState.Normal;
                this.Activate();
                FocusOnSearchBox();
            }

            SearchKeywordBox.IsEnabled = true;
        }

        string CurrentFolderStructurePath;
        private void UpdateFolderStructureView(string filePath)
        {
            CurrentFolderStructurePath = filePath;  // For later reference during folder operations
            string fileContent = File.ReadAllText(filePath);

            // Read file and update view
            // Status Update
            StatusLabel.Content = "Loading...";

            // Do Statistics
            JFolder.StatisticAmount = 0;
            JFile.StatisticAmount = 0;

            // Deserialize Results
            JRootFolder = JsonConvert.DeserializeObject<JFolder>(fileContent);
            // Establish Helper Members
            App.EstablishParents(JRootFolder);

            // Store Statistics and Reset them
            int TotalFolders = JFolder.StatisticAmount;
            int TotalFiles = JFile.StatisticAmount;
            JFolder.StatisticAmount = 0;
            JFile.StatisticAmount = 0;

            // Render Objects
            List<JFolder> Roots = new List<JFolder>();
            Roots.Add(JRootFolder);
            DirectoryView.ItemsSource = Roots;

            // Update Status
            StatusLabel.Content = String.Format("Folder Structure Loaded: Folders{0}(Total), Files{1}(Total)! ", TotalFolders, TotalFiles);

            // ENable Searching
            SearchKeywordBox.IsEnabled = true;
        }

        // Use a dedicated function for dedicated purpose
        private void FocusOnSearchBox()
        {
            // Focus on searching box
            Keyboard.Focus(SearchKeywordBox);
        }

        private void MinimizeBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
            // this.Visibility = Visibility.Hidden;    // http://stackoverflow.com/questions/357076/best-way-to-hide-a-window-from-the-alt-tab-program-switcher
            // Somehow when this is set the window will get only partially rendered when recall
            // So we save the window in Alt-Tab and don't bother adding a taskbar icon for reopening it
        }

        private void CloseBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                    // Not called when "ShowWindow()" executes because the window is still not visible
                    break;
                case WindowState.Minimized:
                    // Clear Searching for next time
                    SearchKeywordBox.Text = "";
                    break;
                case WindowState.Maximized:
                    // Never Used
                    break;
                default:
                    break;
            }
        }

        private void SearchKeywordBox_KeyDown(object sender, KeyEventArgs e)
        {
            // http://stackoverflow.com/questions/3099472/previewkeydown-is-not-seeing-alt-modifiers
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Enter key for quick search
            if (key == Key.Enter)
            {
                // Application short cut
                if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    // Theoratically we can search in index list and open any file with .exe extension (pending implementation) but applications in app home has priority (and doesn't need a suffix)
                    // Pending ignoring upper/lower case

                    // Check file exit
                    string AppPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + App.AppHome + "\\" + SearchKeywordBox.Text;
                    if(!File.Exists(AppPath))
                    {
                        AppPath += ".exe";
                        if (!File.Exists(AppPath))
                        {
                            AppPath = null;
                        }
                    }

                    if(AppPath != null)
                        System.Diagnostics.Process.Start(AppPath);
                    else
                    {
                        // update status for information
                        StatusLabel.Content = "Executable doesn't exist.";
                    }
                }

                JFolder CurrentDisplay = (DirectoryView.ItemsSource as List<JFolder>).First();

                // If no match found
                if (CurrentDisplay == null)
                {
                    StatusLabel.Content = "No file to open.";
                }
                // If more than one match if found
                if (CurrentDisplay.Files.Count > 1)
                {
                    StatusLabel.Content = "More than one match is found, please open using tree view.";
                }
                // Otherwise there can be only one match
                else
                {
                    if(FolderCheckBox.IsChecked == true) // We are displaying folder serach results
                    {
                        // Get JFolder first
                        List<JFolder> subFolders = CurrentDisplay.Folders;
                        while (subFolders[0].Folders.Count != 0)
                        {
                            subFolders = subFolders[0].Folders;
                        }
                        JFolder foundFolder = subFolders[0];

                        // If used Alt to open properties
                        if((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                        {
                            SystemInterpService.ShowFileProperties(App.GetParentFolderPath(foundFolder));
                        }
                        // Otherwise open folder
                        else
                        {
                            OpenFolderForInspection(foundFolder);
                        }
                    }
                    else // We are displaying file search results
                    {
                        // Get JFile first
                        List<JFolder> subFolders = CurrentDisplay.Folders;
                        while (subFolders[0].Folders.Count != 0)
                        {
                            subFolders = subFolders[0].Folders;
                        }
                        JFile foundFile = subFolders[0].Files[0];
                        // If used SHIFT then open file location
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            OpenFolderForInspection(foundFile.Parent);
                        }
                        // If used Alt to open properties
                        else if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                        {
                            SystemInterpService.ShowFileProperties(App.GetParentFolderPath(foundFile.Parent) + foundFile.FileName);
                        }
                        // Otherwise just open file
                        else
                        {
                            OpenFileForEditing(foundFile);
                            PreviousOpenFile = foundFile;   // For later quick access
                        }
                    }
                }
            }
        }

        private void SearchKeywordBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Down Arrow to navigate in TreeView
            if (e.Key == Key.Down)
            {
                Keyboard.Focus(DirectoryView);
                e.Handled = true;
            }
        }

        private void FileImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Extract Item from metadata
            JFile item = (sender as Image).Tag as JFile;

            OpenFileForEditing(item);
        }

        private void FolderImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Extract Item from metadata
            JFolder item = (sender as Image).Tag as JFolder;

            OpenFolderForInspection(item);
        }

        private void OpenFolderForInspection(JFolder folder)
        {
            if (folder.FolderName != App.UnifiedFolderName)
            {
                // Get folder pathh
                string folderPath = App.GetParentFolderPath(folder);

                if (Directory.Exists(folderPath))
                {
                    // Open folder in system explorer
                    System.Diagnostics.Process.Start(folderPath);

                    // Auto Hide
                    this.WindowState = WindowState.Minimized;
                }
                else
                {
                    // Update status
                    StatusLabel.Content = "Folder cannot be found on this computer, might you be using offline structures?";
                }
            }
        }

        private void DirectoryView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DirectoryShadow.Height = e.NewSize.Height;
        }

        // Hides window when not working
        private void window_Deactivated(object sender, EventArgs e) // "LostFocus" event will be called whenver childelement lost focus, so not a good place for this
        {
            this.WindowState = WindowState.Minimized;
        }

        private void window_Activated(object sender, EventArgs e)
        {
            // Play Storyboard
            Storyboard sb = this.FindResource("Window_Fade_In") as Storyboard;
            sb.Begin();
        }

        // http://stackoverflow.com/questions/2280049/why-is-the-treeviewitems-mousedoubleclick-event-being-raised-multiple-times-per
        private void OnTreeViewItemMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;

            if (item.IsSelected)
            {
                // Open folder or file depending on item type
                JFolder folder = item.DataContext as JFolder;
                if(folder != null)
                {
                    // Don't do this since by default this should be expanding folder action
                    // OpenFolderForInspection(folder);
                    // e.Handled = true;
                }
                else
                {
                    JFile file = item.DataContext as JFile;
                    OpenFileForEditing(file);
                    e.Handled = true;
                }
            }
        }

        private void OnTreeViewItemPreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

            // Enter key for quick search
            if (key == Key.Enter)
            {
                // If SHIFT is pressed, then open file location if file, otherwise ignore
                if((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Make sure some node is selected and that represents a file
                    JFile file = DirectoryView.SelectedItem as JFile;
                    if (file != null)
                    {
                        OpenFolderForInspection(file.Parent);
                        e.Handled = true;
                    }
                }
                // If ALT is pressed, then open property window
                else if((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                {
                    // Open as a file
                    JFile file = DirectoryView.SelectedItem as JFile;
                    if (file != null)
                    {
                        SystemInterpService.ShowFileProperties(App.GetParentFolderPath(file.Parent) + file.FileName);
                        e.Handled = true;
                    }
                    // Open as a folder
                    JFolder folder = DirectoryView.SelectedItem as JFolder;
                    if (folder != null)
                    {
                        SystemInterpService.ShowFileProperties(App.GetParentFolderPath(folder));
                        e.Handled = true;
                    }
                }
                // Open as is
                else
                {
                    // Open as a file
                    JFile file = DirectoryView.SelectedItem as JFile;
                    if (file != null)
                    {
                        OpenFileForEditing(file);
                        e.Handled = true;
                    }
                    // Open as a folder
                    JFolder folder = DirectoryView.SelectedItem as JFolder;
                    if (folder != null)
                    {
                        OpenFolderForInspection(folder);
                        e.Handled = true;
                    }
                }
            }
        }

        // Status Label should be alive and keep talking... So we make it simpler to update
        private void UpdateStatus(string newStatus)
        {
            // Update Content
            StatusLabel.Content = newStatus;

            // Then we might need to enable auto scrolling for longer contents, add some suggestions etc.

            // Or bold text for certain cases (might pass extra parameters in)
        }

        // Context Menu
        private void SolutionTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //TreeViewItem SelectedItem = SolutionTree.SelectedItem as TreeViewItem;
            //switch (SelectedItem.Tag.ToString())
            //{
            //    case "Solution":
            //        SolutionTree.ContextMenu = SolutionTree.Resources["SolutionContext"] as System.Windows.Controls.ContextMenu;
            //        break;
            //    case "Folder":
            //        SolutionTree.ContextMenu = SolutionTree.Resources["FolderContext"] as System.Windows.Controls.ContextMenu;
            //        break;
            //}
        }

        private async void MenuItem_UpdateFolder_Click(object sender, RoutedEventArgs e)
        {
            // Update Json structure at current location
            JFolder folder = (sender as MenuItem).DataContext as JFolder;

            // Update Status
            UpdateStatus("Working on it...");

            // Dispatch a thread for generating content and save to JSON file because that can be quite intensive
            await Task.Factory.StartNew(() => UpdateFolderContent(folder));

            // Render Objects 
            List<JFolder> Roots = new List<JFolder>();
            Roots.Add(JRootFolder);
            DirectoryView.ItemsSource = Roots;
            // Automatically scroll to previous location
            // ...

            // Update Status
            UpdateStatus("Folder structure updated to " + CurrentFolderStructurePath);
        }

        private void UpdateFolderContent(JFolder selectedFolder)
        {
            // Add to updated folder list for later clean up
            updatedFolders.Add(selectedFolder);

            // Clear Previous Contents
            selectedFolder.Folders.Clear();
            selectedFolder.Files.Clear();
            FolderGeneratorRecursive(selectedFolder);

            // Expand all parents
            JFolder tempRef = selectedFolder;
            while (tempRef != null)
            {
                tempRef.bExpanded = true;
                tempRef = tempRef.Parent;
            }

            // Save JSon file
            System.IO.File.WriteAllText(CurrentFolderStructurePath, JsonConvert.SerializeObject(JRootFolder, Formatting.Indented)); // Pending Debug
        }

        // Generate all contents under current folder
        private void FolderGeneratorRecursive(JFolder currentFolder)
        {
            // First get folder path
            string FolderPath = App.GetParentFolderPath(currentFolder);

            // Then add items to folder
            try
            {
                foreach (string elementPath in Directory.EnumerateDirectories(FolderPath))  // It seems DirectorInfo can provide directory name directly using ToString()
                {
                    string FolderName = elementPath.Substring(elementPath.LastIndexOf("\\") + 1);

                    // Generate JFolders
                    JFolder elementFolder = new JFolder(FolderName);
                    elementFolder.Parent = currentFolder;
                    FolderGeneratorRecursive(elementFolder);
                    currentFolder.Folders.Add(elementFolder);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }

            try
            {
                foreach (string elementPath in Directory.EnumerateFiles(FolderPath))
                {
                    string FileName = System.IO.Path.GetFileName(elementPath);

                    // Generate JFiles
                    JFile elementFile = new JFile(FileName);
                    elementFile.Parent = currentFolder;
                    currentFolder.Files.Add(elementFile);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (System.IO.PathTooLongException) { }
        }

        private void MenuItem_CreateFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_AddFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_FolderProperty_Click(object sender, RoutedEventArgs e)
        {
            // Get folder path
            JFolder folder = (sender as MenuItem).DataContext as JFolder;
            string folderPath = App.GetParentFolderPath(folder);

            SystemInterpService.ShowFileProperties(folderPath);
        }

        private void MenuItem_CopyFilePath_Click(object sender, RoutedEventArgs e)
        {
            JFile file = (sender as MenuItem).DataContext as JFile;

            // Get file pathh
            string filePath = App.GetParentFolderPath(file.Parent) + file.FileName;

            Clipboard.SetText(filePath);

            UpdateStatus("Path copied to clipboard");
        }

        private void MenuItem_OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            JFile file = (sender as MenuItem).DataContext as JFile;

            OpenFolderForInspection(file.Parent);
        }

        private void MenuItem_ShowWhere_Click(object sender, RoutedEventArgs e)
        {
            JFile file = (sender as MenuItem).DataContext as JFile;

            // Get file pathh
            string filePath = App.GetParentFolderPath(file.Parent) + file.FileName;

            UpdateStatus(filePath);
        }

        private void MenuItem_FileProperty_Click(object sender, RoutedEventArgs e)
        {
            // Get file path
            JFile file = (sender as MenuItem).DataContext as JFile;
            string filePath = App.GetParentFolderPath(file.Parent) + file.FileName;

            SystemInterpService.ShowFileProperties(filePath);
        }

        private void FolderCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateSearchMode(ContentSearchMode.SingleFolder);
        }

        private void FolderCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSearchMode(ContentSearchMode.SingleFile);
        }


    }
}