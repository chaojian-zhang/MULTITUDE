using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using System.Windows.Data;
using System.Globalization;
using MULTITUDE.Canvas;
using MULTITUDE.Class.Facility;
using MULTITUDE.Class.Facility.ClueManagement;
using System.Collections.ObjectModel;

namespace MULTITUDE
{
    /// <summary>
    /// Type of document to generate and auxliary operations to perform
    /// </summary>
    public enum ImportMode
    {
        GenerateClues,
        GenerateVirtualArchive,
        UseAsArchive,
        NoClassification    // Clean import
    }

    /// <summary>
    /// File operation to perform
    /// </summary>
    public enum ImportAction
    {
        Cut,
        Clone,
        Refer
    }

    // UI Converter
    public class ClueToListConverter : IValueConverter
    {
        // From clue tot text
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<Clue> clue = (List<Clue>)value;
            return string.Join("\n", clue.Select(item => item.Name).ToArray());

        }
        // From text to clue
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (string)value;
            string[] clueStrings = text.Split(new char[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<Clue> clues = clueStrings.Select(item => new Clue(item)).ToList();
            return clues;
        }
    }

    // Regarding serialization: Aim at using serialization
    // Otherwise, output categorization systems fist, from root to children, one by one; Associated for each OU is output in pure string form(though can be binary) so takes a bit extra space but is more condense
    // If we use text format then final generated output should ideally be encrypted and but definitely compressed
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Application Constants
        public static readonly string DefaultTextboxText = "Enter text here";
        #endregion

        #region Application State Information and Data
        // User home location can be folder or drives; Those folders/drives are supposded to be managed completely by us, and no external application should modify anything there except for archives
        // Only one home is active at a time, in the future we will provide importing (make other home documents local) and linking to other homes (enable searching and editing but not cross-refereing contents -- or we might not implement this at all)

        // Book keeping infromation
        internal Home CurrentHome;    // Current active home
        // Saving status synchronization
        private bool bSavingOrLoading { get; set; }  // Whether somewhere else in the application we invoked saving or loading, if that's the case ignore current saving/loading request

        // Facilities
        private LogHelper LogHelper;
        private EverythingService Everything;
        #endregion Application State Information

        #region Configuration Constants
        private static readonly string IsolatedStorageName = "MULTITUDE Session Info";
        private static readonly string DefaultHomeLocation = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DefaultHome");  // https://stackoverflow.com/questions/837488/how-can-i-get-the-applications-path-in-a-net-console-application
        private static readonly string DefaultLogFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
        #endregion

        #region Event Handling
        internal void NewStartRequest(ReadOnlyCollection<string> commandLine)
        {
            throw new NotImplementedException();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // See whether we are open from file/folder directly
            string[] activationData = AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null ? AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData : null;
            string homeLocation = string.Empty;
            if(activationData != null && activationData.Length > 0) 
            {
                string homeFilePath;
                Uri uri = new Uri(activationData[0]);
                homeFilePath = uri.LocalPath;
            }
            else
            {
                // Do registration
                SystemInterpService.RegisterCustomFileType();

                // Before main window is created and anything else happens, we: load previous sessions or load a default one and configure our main window(i.e. VW) well
                // Load Home Indepdent Data
                homeLocation = LoadHomeLocation();
            }
                        
            // Load Home or generate one: Home data, home configurations -- everything user ever specify stores in a home, except home location
            if (!bSavingOrLoading)
                CurrentHome = Home.Load(homeLocation);  // Should be very light and fast, or async if disk reading is required, actual VW initialization are deferred
            // Create facilities
            LogHelper = new LogHelper(DefaultLogFolder);
            // SetupEverythingService();
        }

        private void SetupEverythingService()
        {
            // Check already running
            if(EverythingService.IsEverythingRunning() == true)
            {
                // Notify user but don't do anything
                (MainWindow as VirtualWorkspaceWindow).UpdateStatus("Everything service is not started due to conflict with currently running Everything instance. Close any running Everything before opening MULTITUDE to enable Everything service inside MULTITUDE.");

                Everything = null;
            }
            else
            {
                Everything = new EverythingService(CurrentHome.KnowledgeTime, CurrentHome.OnFileSystemEntryUpdate);
            }
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            // Aync: Auto save
            SaveData();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Asyc: Auto save (Notice MainCanvas window closes immediately, saving is done here in background)
            SaveData();
        }

        public void SaveData()
        {
            // Save application level configurations
            // ...

            if (!bSavingOrLoading)
                CurrentHome.RequestSave();
        }
        #endregion Event Handling

        #region Data Management
        // --------------------- Home and file management --------------------- //
        // Save current home locations to a place easy to retrieve
        private void SaveHomeLocation()
        {
            // Write current user home locations
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(IsolatedStorageName, FileMode.Create, f))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine(Home.Location);
            }
        }
        // Load previous session's home location or use a default one
        private string LoadHomeLocation()
        {
            string HomeLocation;

            // If we have command line parameters, load that location
            string[] commandLineArgs = System.Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1)
            {
                string path = commandLineArgs[1];
                if (System.IO.File.Exists(path) && System.IO.Path.GetFileName(path) == Home.HomeDataFileName) return System.IO.Path.GetDirectoryName(path);
                else if (System.IO.Directory.Exists(path) && (System.IO.File.Exists(System.IO.Path.Combine(path, Home.HomeDataFileName)) || System.IO.Directory.GetFileSystemEntries(path).Count() == 0)) return path;  // This automatically handles relative path
            }

            // Read current user home locations if it exist
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(IsolatedStorageName, FileMode.Open, f))
                using (StreamReader reader = new StreamReader(stream))
                {
                    HomeLocation = reader.ReadLine();
                }
            }
            catch (FileNotFoundException)
            {
                // Use a default location
                HomeLocation = DefaultHomeLocation;
            }

            // Make sure the directory can be accessible, i.e. on a mounted drive
            try{ System.IO.Directory.CreateDirectory(HomeLocation);}
            catch (DirectoryNotFoundException) { HomeLocation = DefaultHomeLocation; }

            return HomeLocation;
        }

        // Reload Function
        public void ReloadHomedata(string newLocation)
        {
            // Save current home before leaving
            CurrentHome.RequestSave();

            // Load and create a different home then update active VW
            CurrentHome = null; // Well it's not an elegant design but Home.Load can somehow refer to CurrentHome (during initialization of first VW we used count of current home's VWs) so we need to reset it first
            CurrentHome = Home.Load(newLocation);
            SaveHomeLocation();

            // Inform main canvas (VW window) to do an update: notice we are still on the same UI thread as the main canvas
            VirtualWorkspaceWindow vwWindow = MainWindow as VirtualWorkspaceWindow;
            vwWindow.CurrentState = CurrentHome.ActiveVW.GetCurrentOpenVW();
            vwWindow.ReloadContents();
        }
        #endregion Data Management

        #region Facilities
        internal void Log(UrgencyLevel level, string content, object counter = null)
        {
            LogHelper.Log(level, content, counter);
        }

        // Ref: https://stackoverflow.com/questions/1472498/wpf-global-exception-handler, https://stackoverflow.com/questions/793100/globally-catch-exceptions-in-a-wpf-application
        // https://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception(v=vs.110).aspx
        // App Shutdown: https://stackoverflow.com/questions/2820357/how-to-exit-a-wpf-app-programmatically
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MULTITUDE.Popup.CrashScreen crashScreen = new Popup.CrashScreen(e.Exception);
            crashScreen.Show();
            App.Current.MainWindow.Close();
            e.Handled = true;
        }
        #endregion
    }
}
