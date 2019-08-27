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

namespace MULTITUDE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constants
        public static readonly string ApplicationUserInfo = "MULTITUDE Current User";
        #endregion

        #region Application State Information
        // We don't identify particular users since there is no point doing that
        // User home locations, can be folder or drives; Those folders/drives are supposded to be managed completely by us, and no external application should modify anything there
        // The priority of UserHome is specified by its index, the first location is always used first unless it's full
        public List<string> UserHomeLocations { get; set; }

        // Information about organization units
        internal StorageManager Storage { get; set; }
        #endregion Application State Information

        #region Event Handling
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Read current user home locations if it exist
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(ApplicationUserInfo, FileMode.Open, f))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        UserHomeLocations.Add(line);
                        line = reader.ReadLine();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // Ask user which user home to use; or to be more user friendly we might want to use a default one untill user explicitly specify one
                // ...
                throw new NotImplementedException();
            }

            // Use storage data in all UserHome to construct memory for OUs
            // ... 
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            // Auto save
            // ...
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Auto save
            // ...
        }
        #endregion Event Handling

        #region Interface Functions
        // --------------------- User management --------------------- //
        public void SelectUser()
        {
            // Validate inputs: any one of the user home locations should be empty or non-existing folders; Otherwise check whether other MULTITUDE profiles exist there and ask User whether we should manage them as well

            // Write current user home locations
            IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreForAssembly();
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(ApplicationUserInfo, FileMode.Create, f))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                foreach (string location in UserHomeLocations)
                    writer.WriteLine(location);
            }
        }

        // --------------------- File management --------------------- //
        // Move file from source to application internal storage
        public void MoveFile(string source)
        {
            throw new NotImplementedException();
        }
        // Copy file from source to application internal storage
        public void CopyFile(string source)
        {
            throw new NotImplementedException();
        }
        #endregion Interface Functions
    }
}
