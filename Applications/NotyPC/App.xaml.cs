using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Windows.Data;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Diagnostics;

/* Brief doc: 
 * 1. Once the app is loaded, it is put into listening mode for connection
 * 2. This client will send two things to remote mobile in each sync: a complete description of folder structure, along with all modifiable files
 * 3. During each sync, all modified files will be merged, while appened files be appended, and a new structure is checked and submitted by client
 * 4. When fetch, we check out only ones that are effective
 * 5. UI design, animation, and window communication is really a serious problem and non-trivial task
 */

namespace MULTITUDE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Hack for Self-signed SSL
        static readonly byte[] apiCertHash = { 0x06, 0x54, 0x3f, 0x36, 0x4e, 0xdb, 0x08, 0x8c, 0x00, 0xe3, 0xf1, 0xb9, 0x3d, 0x12, 0x8f, 0x47, 0x17, 0xf6, 0x40, 0x08 };

        // Public Information
        // public static readonly string ServerAddress = "https://note.totalimagine.com/SIS/";
        public static readonly string RESTServiceAddress = "https://note.totalimagine.com/SIS/REST.php";
        public static readonly string QuickMatchFolderName = "QuickMatch";
        public static readonly string UnifiedFolderName = "All Disks";
        public static readonly string DefaultJSONFileName = "Folder Structure.json";
        public static readonly string ReferenceDirectoryName = "Reference";   // Used for cross machine storage: application will save temporary offline files there without any depth of structure
        public static readonly string UserHome = "User Home";   // Used for cross machine storage, specialized folder for personalized organization purpose: application will not touch this folder, for USB-like usage
        public static readonly string AppHome = "App Home";   // Used for application launch shortcuts
        public static string username { get; set; }
        public static string password { get; set; }

        App()
        {
            // Override automatic validation of SSL server certificates.
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertficate;
        }

        /// <summary>
        /// Validates the SSL server certificate.
        /// </summary>
        /// <param name="sender">An object that contains state information for this
        /// validation.</param>
        /// <param name="cert">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the
        /// remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote
        /// certificate.</param>
        /// <returns>Returns a boolean value that determines whether the specified
        /// certificate is accepted for authentication; true to accept or false to
        /// reject.</returns>
        private static bool ValidateServerCertficate(
                    object sender,
                    X509Certificate cert,
                    X509Chain chain,
                    SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                // Good certificate.
                return true;
            }

            bool certMatch = false; // Assume failure
            byte[] certHash = cert.GetCertHash();
            if (certHash.Length == apiCertHash.Length)
            {
                certMatch = true; // Now assume success.
                for (int idx = 0; idx < certHash.Length; idx++)
                {
                    if (certHash[idx] != apiCertHash[idx])
                    {
                        certMatch = false; // No match
                        break;
                    }
                }
            }

            // Return true => allow unauthenticated server,
            //        false => disallow unauthenticated server.
            return certMatch;
        }

        public static string GetParentFolderPath(JFolder folder)
        {
            if (folder.FolderName == UnifiedFolderName)
            {
                return "";
            }
            else if (folder.Parent == null)
            {
                return folder.FolderName + "\\";
            }
            else
            {
                return GetParentFolderPath(folder.Parent) + folder.FolderName + "\\";
            }
        }

        public static void EstablishParents(JFolder currentFolder)
        {
            foreach (JFile file in currentFolder.Files)
            {
                file.Parent = currentFolder;
            }

            foreach (JFolder folder in currentFolder.Folders)
            {
                folder.Parent = currentFolder;
                EstablishParents(folder);
            }
        }
    }

    // QuickMatch® (3/4): Match Files to Specific Destinations as much as we can
    // This class is multi-thread safe (i.e. can be dispatched multiple times in different threads) as long as caller doesn't temper with returned result in any way, or as long as we resolve the problem mentioned in below comment
    public class QuickMatch 
        /* Pending Optimizaing, e.g. searching a cached binary file structure (e.g. layout everything in memory in a way that just makes sense for manual iteration) 
         * instead of iterating through a compete structure; Or we can use LINQ but I am not sure that support ambiguous serach*/
    {
        // Return the file matched in original folder(well the actual return value is explained on the forth line of this comment), we will match only one file: YES, only one file, no choices can be made, this is the key for making this whole thing useful (and be strict to ourselves)
        // Rationale: I have only one file in mind, despite how ambiguous we are identifying it
        // So to be flexible, there really should be two modes: one is to return one file, the other is to return a bunch of files
        // We will return a "partially new" JFolder for representation purpose only: DO NOT TEMPER WITH THE FOLDER because it is not deep copied - sometimes it is e.g. in case we return only one file; other cases not, see "Notice Hacking" below
        public static JFolder QuickMatchFile(string keywordsString, JFolder rootFolder, bool bMultiReturn = false)
        {
            // Delimit keywords
            /* Remove last element in case an extra blank
             * http://stackoverflow.com/questions/26946196/c-sharp-deleting-last-item-from-array-of-string
             * http://stackoverflow.com/questions/6823253/c-sharp-string-split-remove-last-item
             * https://msdn.microsoft.com/en-us/library/kxbw3kwc(v=vs.110).aspx
             */
            char[] delimiters = new Char[] { ' ' };
            string[] keywordsArray = keywordsString.TrimEnd(delimiters).Split(delimiters);

            // If no keywords at all
            if(keywordsArray.Length == 0)
            {
                return null;
            }

            // First we get an array of all JFiles that matches the last keyword
            List<JFile> potentialEndPoints = new List<JFile>();
            KeywordIterator(rootFolder, potentialEndPoints, keywordsArray.First());

            // If we have found only one element, then all good to go; if none was found, then also we end our job; if multiple files are found, we do a second level comparison
            if(potentialEndPoints.Count == 0)
            {
                return null;
            }
            else if (potentialEndPoints.Count == 1)
            {
                // Extract a new JSON and return
                return JBuildRootFromFile(potentialEndPoints[0]);
            }
            else
            {
                // Depending on the number of keywords left, we take next step decision
                if(keywordsArray.Length == 1)   // No extra keywords left, we return all we have found
                {
                    // If no keywords left for comparison, then the user is just looking for all potential matches
                    /// ****** Notice Hacking ****** To save effort and resource we use a trick: Since The user will only display the folder structure, and when user clicks the file image to open the file it will iterate from file to parents to find its path, so we will generate a phatom folder to hold temporary files, without showing detailed structure of each one
                    /// But notice that in this case the folder itself cannot be clicked to open any specific folder
                    JFolder newFolder = new JFolder("Search Results");
                    // Expand Folder
                    newFolder.bExpanded = true;
                    foreach (JFile file in potentialEndPoints)
                    {
                        newFolder.Files.Add(file);
                    }
                    return newFolder;
                }
                else
                {
                    // First do a similarity check for each file
                    foreach (JFile file in potentialEndPoints)
                    {
                        // Form a path string
                        string filePath = App.GetParentFolderPath(file.Parent) + file.FileName;
                        // Evaluate similarity between path string and remaining keywords, add a point for each match
                        // Remember that we are not trying to provide an exact match, but ambiguous in a sense expecially when dealing with CHN characters because no space seperation
                        for (int i = 1; i < keywordsArray.Length; i++)
                        {
                            if (filePath.ToUpper().Contains(keywordsArray[i].ToUpper()))
                            {
                                file.keywordOccurences++;
                            }
                            else
                            {
                                file.keywordMisses++;
                            }
                        }
                    }

                    // Then sort out the file with most points for the last test
                    /* The below creates a new EnumerableItem. 
                     * http://stackoverflow.com/questions/16620135/sort-a-list-of-objects-by-the-value-of-a-property
                     * https://msdn.microsoft.com/en-us/library/bb534966.aspx
                     * http://stackoverflow.com/questions/3309188/how-to-sort-a-listt-by-a-property-in-the-object
                    */
                    List<JFile> orderedList = (potentialEndPoints.OrderByDescending(x => (x.keywordOccurences - x.keywordMisses)).ToList());

                    // Clean keyword occurences for next search
                    foreach (JFile file in potentialEndPoints)
                    {
                        file.keywordOccurences = 0;
                        file.keywordMisses = 0;
                    }

                    // Return only one file or return multiple ones depending on option
                    if(bMultiReturn)
                    {
                        JFolder newFolder = new JFolder("Search Results");
                        // Expand Folder
                        newFolder.bExpanded = true;
                        foreach (JFile file in orderedList)
                        {
                            newFolder.Folders.Add(JBuildRootFromFile(file));
                        }
                        return newFolder;
                    }
                    else
                    {
                        JFile bestMatch = orderedList[0];

                        // Return the only best match we have
                        return JBuildRootFromFile(bestMatch);
                    }
                }
            }
        }

        // Return the folder matched in original folder
        // Rationale: I have only one folder in mind, despite how ambiguous we are identifying it
        // We will return a "partially new" JFolder for representation purpose only: DO NOT TEMPER WITH THE FOLDER because it is not deep copied - sometimes it is e.g. in case we return only one file; other cases not, see "Notice Hacking" below
        public static JFolder QuickMatchFolder(string keywordsString, JFolder rootFolder, bool bMultiFolder = false)
        {
            // Delimit keywords
            char[] delimiters = new Char[] { ' ' };
            string[] keywordsArray = keywordsString.TrimEnd(delimiters).Split(delimiters);

            // If no keywords at all
            if (keywordsArray.Length == 0)
            {
                return null;
            }

            // First we get an array of all JFolders that matches the last keyword
            List<JFolder> potentialEndPoints = new List<JFolder>();
            KeywordIteratorForFolder(rootFolder, potentialEndPoints, keywordsArray.First());

            // If we have found only one element, then all good to go; if none was found, then also we end our job; if multiple files are found, we do a second level comparison
            if (potentialEndPoints.Count == 0)
            {
                return null;
            }
            else if (potentialEndPoints.Count == 1)
            {
                // Extract a new JSON and return
                return JBuildRootFromFolder(potentialEndPoints[0]);
            }
            else
            {
                // Depending on the number of keywords left, we take next step decision
                if (keywordsArray.Length == 1)   // No extra keywords left, we return all we have found
                {
                    // If no keywords left for comparison, then the user is just looking for all potential matches
                    JFolder newFolder = new JFolder("Search Results");
                    // Expand Folder
                    newFolder.bExpanded = true;
                    foreach (JFolder folder in potentialEndPoints)
                    {
                        newFolder.Folders.Add(folder);
                    }
                    return newFolder;
                }
                else
                {
                    // First do a similarity check for each file
                    foreach (JFolder folder in potentialEndPoints)
                    {
                        // Form a path string
                        string folderPath = App.GetParentFolderPath(folder);
                        // Evaluate similarity between path string and remaining keywords, add a point for each match
                        // Remember that we are not trying to provide an exact match, but ambiguous in a sense expecially when dealing with CHN characters because no space seperation
                        for (int i = 1; i < keywordsArray.Length; i++)
                        {
                            if (folderPath.ToUpper().Contains(keywordsArray[i].ToUpper()))
                            {
                                folder.keywordOccurences++;
                            }
                            else
                            {
                                folder.keywordMisses++;
                            }
                        }
                    }

                    // Then sort out the folders with most points for the last test
                    List<JFolder> orderedList = (potentialEndPoints.OrderByDescending(x => (x.keywordOccurences - x.keywordMisses)).ToList());

                    // Clean keyword occurences for next search
                    foreach (JFolder folder in potentialEndPoints)
                    {
                        folder.keywordOccurences = 0;
                        folder.keywordMisses = 0;
                    }

                    // Return only one folder or return multiple folder depending on option
                    if (bMultiFolder)
                    {
                        JFolder newFolder = new JFolder("Search Results");
                        // Expand Folder
                        newFolder.bExpanded = true;
                        foreach (JFolder folder in orderedList)
                        {
                            newFolder.Folders.Add(JBuildRootFromFolder(folder));
                        }
                        return newFolder;
                    }
                    else
                    {
                        JFolder bestMatch = orderedList[0];

                        // Return the only best match we have
                        return JBuildRootFromFolder(bestMatch);
                    }
                }
            }
        }

        // Extract and build a new JFolder from one JFile
        private static JFolder JBuildRootFromFile(JFile file)
        {
            // This is a bit complicated: We first constrct a string of all folder paths
            List<string> folderNames = new List<string>();
            JFolder parent = file.Parent;
            while (parent != null)
            {
                folderNames.Add(parent.FolderName);
                parent = parent.Parent;
            }
            folderNames.Reverse();  // Revese order so things are logical
            // Then we add all those paths to our newly created JFolder
            JFolder representativeFolder = new JFolder(folderNames[0]);
            JFolder currentFolder = representativeFolder;
            // Expand Root Folder
            currentFolder.bExpanded = true;
            for (int i = 1; i < folderNames.Count; i++) // Skipped the root folder name, i.e. when i = 0
            {
                // Add Children and Iterate
                currentFolder.Folders.Add(new JFolder(folderNames[i]));
                currentFolder = currentFolder.Folders[0];   // Go to the only child folder of current folder
                // Expand Folder
                currentFolder.bExpanded = true;
            }
            // Finally we add our JFile
            JFile newFile = new JFile(file.FileName);
            newFile.TextContent = file.TextContent;
            newFile.Appendix = file.Appendix;
            currentFolder.Files.Add(newFile);  // Notice we are NOT just referencing the old - we are creating a complete replicate

            // Establish New ParentalShip
            App.EstablishParents(representativeFolder);

            // Return the JSON obejct
            return representativeFolder;
        }

        // Extract and build a new JFolder from one JFolder
        private static JFolder JBuildRootFromFolder(JFolder folder)
        {
            // This is a bit complicated: We first constrct a string of all folder paths
            List<string> folderNames = new List<string>();
            JFolder parent = folder;
            while (parent != null)
            {
                folderNames.Add(parent.FolderName);
                parent = parent.Parent;
            }
            folderNames.Reverse();  // Revese order so things are logical
            // Then we add all those paths to our newly created JFolder
            JFolder representativeFolder = new JFolder(folderNames[0]);
            JFolder currentFolder = representativeFolder;
            // Expand Root Folder
            currentFolder.bExpanded = true;
            for (int i = 1; i < folderNames.Count; i++) // Skipped the root folder name, i.e. when i = 0
            {
                // Add Children and Iterate
                currentFolder.Folders.Add(new JFolder(folderNames[i]));
                currentFolder = currentFolder.Folders[0];   // Go to the only child folder of current folder
                // Expand Folder
                currentFolder.bExpanded = true;
            }

            // Establish New ParentalShip
            App.EstablishParents(representativeFolder);

            // Return the JSON obejct
            return representativeFolder;
        }

        private static void KeywordIterator(JFolder currentFolder, List<JFile> filesList, string keyword)
        {
            // Match Files
            foreach (JFile file in currentFolder.Files)
            {
                if (file.FileName.ToUpper().Contains(keyword.ToUpper()))
                {
                    filesList.Add(file);
                }
            }

            // Continue Searching
            foreach (JFolder folder in currentFolder.Folders)
            {
                KeywordIterator(folder, filesList, keyword);
            }
        }

        private static void KeywordIteratorForFolder(JFolder currentFolder, List<JFolder>foldersList, string keyword)
        {
            // Match Folder
            if (currentFolder.FolderName.ToUpper().Contains(keyword.ToUpper()))
            {
                foldersList.Add(currentFolder);
            }

            // Continue Searching
            foreach (JFolder folder in currentFolder.Folders)
            {
                KeywordIteratorForFolder(folder, foldersList, keyword);
            }
        }
    }

    // JSON Classes
    //public class JDocument
    //{
    //    // Constructor
    //    public JDocument(string path)
    //    {
    //        RootPath = path;
    //        Folders = new List<JFolder>();
    //        Files = new List<JFile>();
    //    }

    //    // Properties
    //    public string RootPath { get; private set; }
    //    public List<JFolder> Folders { get; set; }
    //    public List<JFile> Files { get; set; } 
    //}

    // Combines two different lists into one for Treeview to consume, combined with templates we achieve a different effect for folders and files
    // See: http://stackoverflow.com/questions/5789149/wpf-complex-hierarchical-data-template
    public class DirectoryItemsSourceCreator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var input = value as JFolder;
            CompositeCollection collection = new CompositeCollection();
            collection.Add(new CollectionContainer() { Collection = input.Folders });
            collection.Add(new CollectionContainer() { Collection = input.Files });
            return collection;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class DirectoryItemsCountCreator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var input = value as JFolder;
            return (input.Folders.Count + input.Files.Count).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TreeViewItemBase : INotifyPropertyChanged
    {
        public TreeViewItemBase()
        {
            bSelectedForUploading = false;
            bShouldExpand = false;
        }

        [JsonIgnore]
        // Whether or not the folder is selected to be included for JSON tree generation
        private bool bSelectedForUploading;

        [JsonIgnore]
        // Whether or not the folder is selected to be included for JSON tree generation
        public bool bSelected
        {
            get { return this.bSelectedForUploading; }
            set
            {
                if (value != this.bSelectedForUploading)
                {
                    this.bSelectedForUploading = value;
                    NotifyPropertyChanged("bSelected");
                }
            }
        }

        [JsonIgnore]
        // Whether or not the folder should Expand in TreeView
        private bool bShouldExpand;

        [JsonIgnore]
        // Whether or not the folder should Expand in TreeView
        public bool bExpanded
        {
            get { return this.bShouldExpand; }
            set
            {
                if (value != this.bShouldExpand)
                {
                    this.bShouldExpand = value;
                    NotifyPropertyChanged("bExpanded");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }


    public class JFolder : TreeViewItemBase, ICloneable
    {
        // Constructor
        public JFolder(string name)
        {
            FolderName = name;
            Folders = new List<JFolder>();
            Files = new List<JFile>();
            Parent = null;
            bMarkRemove = false;

            // Statstics
            StatisticAmount++;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        // Properties
        public string FolderName { get; set; }
        public List<JFolder> Folders { get; set; }
        public List<JFile> Files { get; set; }

        [JsonIgnore]
        public JFolder Parent { get; set; }

        [JsonIgnore]
        public bool bMarkRemove { get; set; }

        [JsonIgnore]
        public static int StatisticAmount { get; set; }

        [JsonIgnore]
        public int keywordOccurences; // Emulating a function of dictionary, for very specfici usage only, see related functions.

        [JsonIgnore]
        public int keywordMisses; // Emulating a function of dictionary, for very specfici usage only, see related functions.
    }

    public class JFile : TreeViewItemBase
    {
        // Instance Constructor
        public JFile(string name)
        {
            FileName = name;
            Appendix = null;
            TextContent = null;
            Parent = null;
            bANSI = true;
            bInclude = false;
            bMarkRemove = false;
            keywordOccurences = 0;

            // Statstics
            StatisticAmount++;
        }

        // Properties
        public string FileName { get; set; }
        public string Appendix { get; set; }
        public string TextContent { get; set; }

        // Helpers
        [JsonIgnore]
        public JFolder Parent { get; set; }

        [JsonIgnore]
        public static int StatisticAmount { get; set; }

        [JsonIgnore]
        public bool bANSI { get; set; } // It will either be {UTF 8 w/ BOM} or {ANSI or w/o UTF8}

        [JsonIgnore]
        public bool bInclude { get; set; }

        [JsonIgnore]
        public bool bMarkRemove { get; set; }

        [JsonIgnore]
        public bool bAppendixFile
        {
            get
            {
                // QuickMatch® (2/4): All QuickMatchFiles are Appendix File, if they do have some TextContent
                if (Parent != null && Parent.FolderName != App.QuickMatchFolderName)
                {
                    return Appendix != null;
                }
                else
                {
                    return TextContent != null; // Notice we are returning bAppendixFile depending on TextContent not Appendix because on Mobile client we will be using TextContent for such files
                }
            }
        } // Either appendix will be null or content will be null or both are null when this property is used; Very specifically, see DownloadWindow.xaml.cs ButtonAppend
        // http://stackoverflow.com/questions/2017642/wpf-binding-based-on-comparison

        [JsonIgnore]
        public bool bContentFile
        {
            get
            {
                // QuickMatch® (2/4): All QuickMatchFiles are Appendix File, if they do have some TextContent
                if( Parent!= null && Parent.FolderName != App.QuickMatchFolderName)
                {
                    return TextContent != null;
                }
                else
                {
                    return false;
                }
            }
        }

        [JsonIgnore]
        public int keywordOccurences; // Emulating a function of dictionary, for very specfici usage only, see related functions.

        [JsonIgnore]
        public int keywordMisses; // Emulating a function of dictionary, for very specfici usage only, see related functions.
    }

    // Hacking of FormUrlEncodedContent
    public class MyFormUrlEncodedContent : ByteArrayContent
    {
        public MyFormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
            : base(MyFormUrlEncodedContent.GetContentByteArray(nameValueCollection))
        {
            base.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        }
        private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
        {
            if (nameValueCollection == null)
            {
                throw new ArgumentNullException("nameValueCollection");
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, string> current in nameValueCollection)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append('&');
                }

                stringBuilder.Append(MyFormUrlEncodedContent.Encode(current.Key));
                stringBuilder.Append('=');
                stringBuilder.Append(MyFormUrlEncodedContent.Encode(current.Value));
            }
            return Encoding.Default.GetBytes(stringBuilder.ToString());
        }
        private static string Encode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }
            return System.Net.WebUtility.UrlEncode(data).Replace("%20", "+");
        }
    }

}


// Global Hotkey mechanism
// http://stackoverflow.com/questions/48935/how-can-i-register-a-global-hot-key-to-say-ctrlshiftletter-using-wpf-and-ne
namespace UnManaged
{
    public class HotKey : IDisposable
    {
        private static Dictionary<int, HotKey> _dictHotKeyToCalBackProc;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, UInt32 fsModifiers, UInt32 vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WmHotKey = 0x0312;

        private bool _disposed = false;

        public Key Key { get; private set; }
        public KeyModifier KeyModifiers { get; private set; }
        public Action<HotKey> Action { get; private set; }
        public int Id { get; set; }

        // ******************************************************************
        public HotKey(Key k, KeyModifier keyModifiers, Action<HotKey> action, bool register = true)
        {
            Key = k;
            KeyModifiers = keyModifiers;
            Action = action;
            if (register)
            {
                Register();
            }
        }

        // ******************************************************************
        public bool Register()
        {
            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
            Id = virtualKeyCode + ((int)KeyModifiers * 0x10000);
            bool result = RegisterHotKey(IntPtr.Zero, Id, (UInt32)KeyModifiers, (UInt32)virtualKeyCode);

            if (_dictHotKeyToCalBackProc == null)
            {
                _dictHotKeyToCalBackProc = new Dictionary<int, HotKey>();
                ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
            }

            _dictHotKeyToCalBackProc.Add(Id, this);

            Debug.Print(result.ToString() + ", " + Id + ", " + virtualKeyCode);
            return result;
        }

        // ******************************************************************
        public void Unregister()
        {
            HotKey hotKey;
            if (_dictHotKeyToCalBackProc.TryGetValue(Id, out hotKey))
            {
                UnregisterHotKey(IntPtr.Zero, Id);
            }
        }

        // ******************************************************************
        private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (!handled)
            {
                if (msg.message == WmHotKey)
                {
                    HotKey hotKey;

                    if (_dictHotKeyToCalBackProc.TryGetValue((int)msg.wParam, out hotKey))
                    {
                        if (hotKey.Action != null)
                        {
                            hotKey.Action.Invoke(hotKey);
                        }
                        handled = true;
                    }
                }
            }
        }

        // ******************************************************************
        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // ******************************************************************
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be _disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be _disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Unregister();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }

    // ******************************************************************
    [Flags]
    public enum KeyModifier
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        NoRepeat = 0x4000,
        Shift = 0x0004,
        Win = 0x0008
    }

    // ******************************************************************
}