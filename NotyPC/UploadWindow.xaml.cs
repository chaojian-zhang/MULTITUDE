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
using System.Net.Http;
using Ookii.Dialogs.Wpf;
using System.IO;
using Newtonsoft.Json;

// A bunch of other helper comments e.g. how UTF is checed see original archieved version of this code

namespace MULTITUDE
{
    /// <summary>
    /// Interaction logic for UploadWindow.xaml
    /// </summary>
    public partial class UploadWindow : Window
    {
        public UploadWindow()
        {
            InitializeComponent();

            // Load all drives and populate the directory view
            List<JFolder> drivesList = new List<JFolder>();
            DriveInfo[] drivesInfo = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drivesInfo)
            {
                JFolder driveFolder = new JFolder(drive.ToString());
                driveFolder.Folders.Add(new JFolder(TempLoadingFolderString));
                drivesList.Add(driveFolder);
            }
            DirectoryView.ItemsSource = drivesList;
        }

        // Whole Disk JSON Helper
        private readonly static string TempLoadingFolderString = "Loading...";

        // Used to expand current folder by one layer: load all items under this folder
        private void FolderGenerator(JFolder currentFolder)
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
                    elementFolder.bSelected = currentFolder.bSelected;  // Let Children share the same status of selection as parent
                    elementFolder.Folders.Add(new JFolder(TempLoadingFolderString));
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

        private JFolder JUnifiedFolder;
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Status Update
            StatusLabel.Content = "Generating Folder Structure...";

            // If Unified folder doesn't exist then create one
            if(JUnifiedFolder == null)
            {
                // Dispatch a thread to handling generation stuff
                await Task.Factory.StartNew(FolderIterator);
            }

            // Status Update
            StatusLabel.Content = "Folder Structure Mapped Successfully, Now Connecting To Server...";

            // QuickMatch® (1/4): Append a folder for QuickMatch usage
            // Make a copy of JRootFolder to add something interseting to it: QuickMatch® Folders
            JFolder JRootFolderCopy = new JFolder(JUnifiedFolder.FolderName);  // I am not providing a copy constructor in wish that we won't need to use it anywhere else
            JRootFolderCopy.Files = JUnifiedFolder.Files;  // Shallow Copy
            JRootFolderCopy.Folders = JUnifiedFolder.Folders.ToList(); // Semi-Deep Copy because we will add new items
            JRootFolderCopy.Folders.Add(new JFolder(App.QuickMatchFolderName)); // Add a blank folder

            // Send Request and Get Content
            MyFormUrlEncodedContent postContent = new MyFormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", App.username),
                new KeyValuePair<string, string>("password", App.password),
                new KeyValuePair<string, string>("filecontent", JsonConvert.SerializeObject(JRootFolderCopy, Formatting.Indented))  // Notice that we are only adding this folder here for uploading purpose, for HTML and Local saving we don't do that
            });

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync(App.RESTServiceAddress, postContent);

            // Status Update accordingly
            string responseString = response.Content.ReadAsStringAsync().Result;
            if (responseString  == "File successfully received.")
            {
                StatusLabel.Content = "Succeeded!";
            }
            else
            {
                StatusLabel.Content = responseString;
            }
        }

        // Used by SubmitButton: This function generates a complete JSON tree of all folders that are selected by user, then append all drive folders to a Unified Folder to conform to our standard
        // Just note that this is not complicated by Windows OS, since on Linux we will need to do some fair amount of work as well: mostly to expand folders that are not expanded by user
        private void FolderIterator()
        {
            // Create a new Unified Folder
            JUnifiedFolder = new JFolder(App.UnifiedFolderName);

            // Get all drives we have loaded
            List<JFolder> DisplayDrivesList = DirectoryView.ItemsSource as List<JFolder>;
            List<JFolder> JSONDrivesTree= new List<JFolder>();

            // For each drive, create a tree of all selected folders and their subitems; If folder not completely loaded yet then load it, to not interfering with display, we need to deep copy a new tree from drive down
            // Optimization: This part might be multi-threaded in the future
            foreach (JFolder folder in DisplayDrivesList)
            {
                // Create copies of drives
                JFolder folderCopy = ExpandFolderRecursive(folder);
                if(folderCopy != null)
                {
                    JSONDrivesTree.Add(folderCopy);

                    // Also add current drive to the submitted JSON
                    folderCopy.Parent = JUnifiedFolder;   // Before this all parents for drives are null
                    JUnifiedFolder.Folders.Add(folderCopy);
                }
            }
        }

        //// Return a DEEP copy of current folder, parent set
        //private JFolder ExpandFolderRecursive(JFolder currentFolder)
        //// Debug: What if the folder and its direct children isn't selected yet its children's children are selected? - Just do an additional check to see whether any of the cildren is expanded if so then keep checking instead of ignoring
        //// Debug: Loading... is uploaded as well, this is a bug -> This was because children folders weren't expanded at right parent
        //{
        //    JFolder currentFolderCopy = new JFolder(currentFolder.FolderName);    // Notice we are still not using a copy constructor because the function is very specific and we are not copying all children items at this step
        //    currentFolderCopy.Parent = currentFolder.Parent;

        //    // If the folder isn't already loaded then load it if selected - but put loaded content to the copied JFolder
        //    if ((currentFolder.Folders.Count == 1) && (currentFolder.Folders[0].FolderName == TempLoadingFolderString))
        //    {
        //        if (currentFolder.bSelected == true)
        //        {
        //            // Load all content
        //            FolderGenerator(currentFolderCopy);
        //        }

        //        // Then do the same to subfolders (of CURRENTFOLDER_COPY) if that subfolder is selected; If that folder isn't selected, however, check whether its subfolders are expanded, if so, make sure to further check those children as well
        //        List<JFolder> newFolders = new List<JFolder>();
        //        foreach (JFolder subFolder in currentFolderCopy.Folders)
        //        {
        //            newFolders.Add(ExpandFolderRecursive(subFolder));
        //        }
        //        currentFolderCopy.Folders = newFolders;
        //    }
        //    else
        //    {
        //        // If the folder already loaded and is selected then we reference all its files and just get their alredy set properties about Loading and Encoding
        //        if (currentFolder.bSelected == true)
        //        {
        //            currentFolderCopy.Files = currentFolder.Files; // Since we by no means will edit such contents so we just refer it - this also have the added benefit that when file contents are checked/unchecked the generated JSON is reflected automatically
        //        }

        //        // Then do the same to subfolders (of CURRENTFOLDER) if that subfolder is selected; If that folder isn't selected, however, check whether its subfolders are expanded, if so, make sure to further check those children as well
        //        foreach (JFolder subFolder in currentFolder.Folders)
        //        {
        //            currentFolderCopy.Folders.Add(ExpandFolderRecursive(subFolder));
        //        }
        //    }

        //    return currentFolderCopy;
        //}

        // Return a DEEP copy of current folder
        // This implements the same function but stated in a slightly different way as above; and the above was probably wrong
        //  Bug1: For an expanded folder if none of its children is selected its subfolders are still uploaded; Solution see below @Bug1
        //  Bug2: For a folder when its children are not expanded they are not uploaded
        private JFolder ExpandFolderRecursive(JFolder currentFolder)
        {
            JFolder currentFolderCopy = new JFolder(currentFolder.FolderName);    // Notice we are still not using a copy constructor because the function is very specific and we are not copying all children items at this step
            currentFolderCopy.Parent = currentFolder.Parent;

            JFolder phatom = new JFolder(currentFolder.FolderName);
            phatom.Parent = currentFolder.Parent;
            phatom.bSelected = currentFolder.bSelected; // @Bug2: This line is crucial otherwise later  FolderGenerator(phatom); generates incomplete result

            // If the folder isn't already loaded
            if ((currentFolder.Folders.Count == 1) && (currentFolder.Folders[0].FolderName == TempLoadingFolderString))
            {
                // Load it if selected - but put loaded content to the phatom JFolder
                if (currentFolder.bSelected == true)
                {
                    // Load all content
                    FolderGenerator(phatom);

                    // We also reference all its files and just get their alredy set properties about Loading and Encoding
                    currentFolderCopy.Files = phatom.Files;
                }
                // If it is not selected, then no need to bother since it isn't expanded and none of its children can be selected and it is not necessary to upload this folder
                // @Bug1: If we want to recreate the bug, uncomment below line.
                else
                {
                    return null;
                    // return currentFolderCopy;    // If we don't return null and instead return currentFolderCopy then we append an empty folder to the tree
                }
            }
            // If the folder is already loaded
            else
            {
                // If selected then we reference all its files and just get their alredy set properties about Loading and Encoding
                if (currentFolder.bSelected == true)
                {
                    currentFolderCopy.Files = currentFolder.Files; // Since we by no means will edit such contents so we just refer it - this also have the added benefit that when file contents are checked/unchecked the generated JSON is reflected automatically
                }

                // If it is not selected, then we still need to check its subfolders, see below
                phatom.Folders = currentFolder.Folders;
                phatom.Files = currentFolder.Files;
            }

            // Check all subfolders, expand if necessary, and reference all files that we need to refer
            foreach (JFolder subFolder in phatom.Folders)
            {
                JFolder returnValue = ExpandFolderRecursive(subFolder);
                if (returnValue != null)
                {
                    currentFolderCopy.Folders.Add(returnValue);
                }
            }

            // Do a final check
            // @Bug1: If we don't return null, a folder expanded contained expanded unselected folders will finally return a copy of itself without any children
            if(currentFolderCopy.Folders.Count != 0 || currentFolderCopy.Files.Count  != 0)
            {
                return currentFolderCopy;
            }
            else
            {
                return null;
            }
        }

        private async void SaveJSONButton_Click(object sender, RoutedEventArgs e)
        {
            // Status Update
            StatusLabel.Content = "Generating Folder Structure...";

            // If Unified folder doesn't exist then create one, otherwise generate one; What about changed made after?
            if (JUnifiedFolder == null)
            {
                // Dispatch a thread to handling generation stuff
                await Task.Factory.StartNew(FolderIterator);
                // FolderIterator();
            }

            // Select Path To Save File
            VistaSaveFileDialog saveFileDialog = new VistaSaveFileDialog();
            saveFileDialog.DefaultExt = ".json";
            saveFileDialog.FileName = App.DefaultJSONFileName;
            saveFileDialog.Filter = "JSON Files(*.json) | *.json";
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            saveFileDialog.Title = "Select where to save JSON file";
            saveFileDialog.ValidateNames = true;
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                // Save File Content & Update Status
                System.IO.File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(JUnifiedFolder, Formatting.Indented));
                StatusLabel.Content = "Saved to file: " + saveFileDialog.FileName;
            }
        }

        // Parameters
        static string HTMLStringStart =
@"<!DOCTYPE html>
<html>
    <head>
        <title>Folder Structure Tree</title>
    </head>
    <body>
        ";
        static string HTMLContent;
        static string HTMLStringEnd =
@"
    </body>
</html>";
        private async void SaveHTMLButton_Click(object sender, RoutedEventArgs e)
        {
            // Status Update
            StatusLabel.Content = "Generating Folder Structure...";

            // If Unified folder doesn't exist then create one, otherwise generate one
            if (JUnifiedFolder == null)
            {
                // Dispatch a thread to handling generation stuff
                await Task.Factory.StartNew(FolderIterator);
            }

            // Generate HTML fomr JSON
            // Add beginning
            HTMLContent += HTMLStringStart;
            // Recursion
            string indentation = "";
            int level = 0;
            RecursiveHTML(JUnifiedFolder, indentation, level);
            // Add end
            HTMLContent += HTMLStringEnd;

            // Select Path To Save File
            VistaSaveFileDialog saveFileDialog = new VistaSaveFileDialog();
            saveFileDialog.DefaultExt = ".html";
            saveFileDialog.FileName = "Folder Structure";
            saveFileDialog.Filter = "HTML Files(*.html) | *.html";
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            saveFileDialog.Title = "Select where to save HTML file";
            saveFileDialog.ValidateNames = true;
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                // Save File Content & Update Status
                System.IO.File.WriteAllText(saveFileDialog.FileName, HTMLContent);
                StatusLabel.Content = "Saved to file: " + saveFileDialog.FileName;
            }
        }

        private void RecursiveHTML(JFolder folder, string currentIndentation, int indentationLevel)
        {
            // Do it once and use it multiple times
            string folderPath = App.GetParentFolderPath(folder);

            // Iterate folders
            foreach (JFolder sub in folder.Folders)
            {
                HTMLContent = HTMLContent + currentIndentation + String.Concat(Enumerable.Repeat("&emsp;", indentationLevel)) + 
                    String.Format("<a href = \"file:///{0}\">{1}</a>", folderPath + "\\" + sub.FolderName, sub.FolderName) + "<br>\r\n";
                RecursiveHTML(sub, currentIndentation + "\t", indentationLevel + 1);
            }

            // Iterate files
            foreach (JFile file in folder.Files)
            {
                HTMLContent = HTMLContent + currentIndentation + String.Concat(Enumerable.Repeat("&emsp;", indentationLevel)) + 
                    String.Format("<a href = \"file:///{0}\">{1}</a>", folderPath + "\\" + file.FileName, file.FileName) +
                "<br>\r\n";
            }
        }

        // Parameters
        string PlainFolderStructure = "<Folder Structure>\n";
        private async void SavePlainTextButton_Click(object sender, RoutedEventArgs e)
        {
            // Status Update
            StatusLabel.Content = "Generating Folder Structure...";

            // If Unified folder doesn't exist then create one, otherwise generate one
            if (JUnifiedFolder == null)
            {
                // Dispatch a thread to handling generation stuff
                await Task.Factory.StartNew(FolderIterator);
            }

            // Generate text from JSON
            string indentation = "";
            RecursiveText(JUnifiedFolder, indentation);

            // Select Path To Save File
            VistaSaveFileDialog saveFileDialog = new VistaSaveFileDialog();
            saveFileDialog.DefaultExt = ".txt";
            saveFileDialog.FileName = "Folder Structure";
            saveFileDialog.Filter = "Text Files(*.txt) | *.txt";
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            saveFileDialog.Title = "Select where to save Text file";
            saveFileDialog.ValidateNames = true;
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                // Save File Content & Update Status
                System.IO.File.WriteAllText(saveFileDialog.FileName, PlainFolderStructure);
                StatusLabel.Content = "Saved to file: " + saveFileDialog.FileName;
            }
        }

        private void RecursiveText(JFolder folder, string currentIndentation)
        {
            // Iterate folders
            foreach (JFolder sub in folder.Folders)
            {
                PlainFolderStructure = PlainFolderStructure + currentIndentation + "▷" + sub.FolderName + "\n";
                RecursiveText(sub, currentIndentation + "\t");
            }

            // Iterate files
            foreach (JFile file in folder.Files)
            {
                PlainFolderStructure = PlainFolderStructure + currentIndentation + file.FileName + "\n";
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract Item from metadata
            JFile item = (sender as Button).Tag as JFile;

            // Get file pathh
            string filePath = App.GetParentFolderPath(item.Parent) + item.FileName;

            // Open file in system editor
            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (System.ComponentModel.Win32Exception) {}
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract Item from metadata
            JFolder item = (sender as Button).Tag as JFolder;

            // Get folder pathh
            string folderPath = App.GetParentFolderPath(item);

            // Open folder in system explorer
            System.Diagnostics.Process.Start(folderPath);
        }

        private void FileContentCheckBox_bInclude_Checked(object sender, RoutedEventArgs e)
        {
            // Append content to JFile object
            JFile item = (sender as CheckBox).Tag as JFile;
            item.bInclude = true;

            // Update Item Content
            UpdateItemContent(item);
        }

        private void FileContentCheckBox_bInclude_Unchecked(object sender, RoutedEventArgs e)
        {
            // Clear content from JFIle Object
            JFile item = (sender as CheckBox).Tag as JFile;
            item.bInclude = false;

            // Update Item Content
            UpdateItemContent(item);
        }

        private void FileEncodingCheckBox_bANSI_Checked(object sender, RoutedEventArgs e)
        {
            // Append content to JFile object
            JFile item = (sender as CheckBox).Tag as JFile;
            item.bANSI = true;

            // Add Content to Item
            UpdateItemContent(item);
        }

        private void FileEncodingCheckBox_bANSI_Unchecked(object sender, RoutedEventArgs e)
        {
            // Append content to JFile object
            JFile item = (sender as CheckBox).Tag as JFile;
            item.bANSI = false;

            // Add Content to Item
            UpdateItemContent(item);
        }

        private void FolderSelectionCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            // Changes are made so if previously we generated a mapping then discard that: For efficiency we could search for that and then remove it only but not necessary for our case
            if(JUnifiedFolder != null)
            {
                JUnifiedFolder = null;
            }

            // If a folder is unchecked, uncheck all its children as well
            JFolder folder = (sender as CheckBox).Tag as JFolder;
            foreach (JFolder subFolder in folder.Folders)
            {
                subFolder.bSelected = false;
            }
        }

        private void FolderSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Changes are made so if previously we generated a mapping then discard that: For efficiency we could search for that and then remove it only but not necessary for our case
            if (JUnifiedFolder != null)
            {
                JUnifiedFolder = null;
            }

            // If a folder is checked, check all its children as well
            JFolder folder = (sender as CheckBox).Tag as JFolder;
            foreach (JFolder subFolder in folder.Folders)
            {
                subFolder.bSelected = true;
            }
        }

        private void UpdateItemContent(JFile file)
        {
            if(file.bInclude == true)
            {
                // Get Filepath
                string filePath = App.GetParentFolderPath(file.Parent) + file.FileName;

                // Convert it to UTF8, then add content to JSON
                var checker = new Unicode.Utf8Checker();
                // If UTF8 BOM
                if (checker.Check(filePath) == true)
                {
                    // In this case we don't care whether user said it's ANSI
                    file.TextContent = File.ReadAllText(filePath);
                }
                else
                {
                    // If asserted ANSI by user
                    if(file.bANSI)
                    {
                        file.TextContent = File.ReadAllText(filePath, Encoding.Default);
                    }
                    // Then UTF8
                    else
                    {
                        file.TextContent = File.ReadAllText(filePath);
                    }
                }
            }
            else
            {
                file.TextContent = null;
            }
        }

        private void DirectoryView_Expanded(object sender, RoutedEventArgs e)
        {
            // Extra Folder
            JFolder folder = (e.OriginalSource as TreeViewItem).DataContext as JFolder;   // Or e.Source.SelectedValue or e.Source.SelectedItem, see debug watch

            // If the folder isn't already loaded then load it
            if ((folder.Folders.Count == 1) && (folder.Folders[0].FolderName == TempLoadingFolderString))
            {
                // Clear existing content
                folder.Folders.Clear();

                // Add new content
                FolderGenerator(folder);
                folder.bExpanded = true;

                // Update Display
                List<JFolder> DrivesList = DirectoryView.ItemsSource as List<JFolder>;
                DirectoryView.ItemsSource = null;
                DirectoryView.ItemsSource = DrivesList;

                // Notice currently this is called multiple times when a lot of files are opened
            }
        }

        private void DirectoryView_Collapsed(object sender, RoutedEventArgs e)
        {
            // Extra Folder
            JFolder folder = (e.OriginalSource as TreeViewItem).DataContext as JFolder;   // Or e.Source.SelectedValue or e.Source.SelectedItem, see debug watch

            folder.bExpanded = false;

            List<JFolder> DrivesList = DirectoryView.ItemsSource as List<JFolder>;
            DirectoryView.ItemsSource = null;
            DirectoryView.ItemsSource = DrivesList;
        }
    }
}
