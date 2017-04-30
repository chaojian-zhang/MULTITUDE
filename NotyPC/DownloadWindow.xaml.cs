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
using System.Net;
using Newtonsoft.Json;
using System.IO;

namespace MULTITUDE
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public DownloadWindow()
        {
            InitializeComponent();
        }

        private JFolder JRootFolderExclusive;
        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // Status Update
            StatusLabel.Content = "Loading...";

            // Send Request and Get Content
            FormUrlEncodedContent postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", App.username),
                new KeyValuePair<string, string>("password", App.password)
            });
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PostAsync(App.RESTServiceAddress, postContent);

            string responseString = response.Content.ReadAsStringAsync().Result;
            if (responseString != "Wrong username/password combination." && responseString != "No folder structure file is available.")
            {
                // Status Update
                StatusLabel.Content = "Loaded!";

                // Deserialize a Root for later use, and strip out its QuickMatch
                // WARNING: CAUTIOUS TOO MUCH ASSUMPTION -- we assume the QuickMatch folder lives in the first layer, and is the last element
                JRootFolderExclusive = JsonConvert.DeserializeObject<JFolder>(responseString);
                App.EstablishParents(JRootFolderExclusive); // Establish Helper Class Members
                JRootFolderExclusive.Folders.Remove(JRootFolderExclusive.Folders[JRootFolderExclusive.Folders.Count - 1]);

                // Deserialize Results
                JFolder JTempRootFolder = JsonConvert.DeserializeObject<JFolder>(responseString);
                App.EstablishParents(JTempRootFolder); // Establish Helper Class Members
                // Do a Cleanup on Results
                CleanupMarkFilesDeletion(JTempRootFolder);
                CleanupFiles(JTempRootFolder);
                CleanupMarkFoldersDeletion(JTempRootFolder);
                CleanupFolders(JTempRootFolder);

                // Update View
                List<JFolder> Roots = new List<JFolder>();
                Roots.Add(JTempRootFolder);
                ChangesList.ItemsSource = Roots;
            }
            else
            {
                // Status Update
                StatusLabel.Content = responseString;
            }
        }

        private void CleanupMarkFilesDeletion(JFolder currentFolder)
        {
            // Mark removing all files that are unthouched
            foreach (JFile file in currentFolder.Files)
            {
                file.bMarkRemove = false;   // Mark everyone included, i.e. not to be deleted for now
                if(file.TextContent == null && file.Appendix == null)
                {
                    // currentFolder.Files.Remove(file);    // Cannot execute here
                    file.bMarkRemove = true;
                }
            }

            // Mark subfolders' files
            foreach(JFolder folder in currentFolder.Folders)
            {
                CleanupMarkFilesDeletion(folder);
            }
        }

        private void CleanupFiles(JFolder currentFolder)
        {
            // Remove all file references
            List<JFile> tempFilesList = currentFolder.Files.ToList();    // http://stackoverflow.com/questions/1952185/how-do-i-copy-items-from-list-to-list-without-foreach
            foreach (JFile file in tempFilesList)
            {
                if(file.bMarkRemove == true)
                {
                    currentFolder.Files.Remove(file);
                }
            }

            foreach (JFolder folder in currentFolder.Folders)
            {
                CleanupFiles(folder);
            }
        }

        private bool CleanupMarkFoldersDeletion(JFolder currentFolder)  // Return value is assigned last in programming logic *: Return whether children is empty
        {
            // Check whether we have any subfolders
            bool bChildrenEmpty = true;    // *
            foreach (JFolder folder in currentFolder.Folders)
            {
                if (!CleanupMarkFoldersDeletion(folder))
                {
                    bChildrenEmpty = false; // *
                }
            }

            // End Node Folder where no other folders exist -- check whether we have any files
            //if (currentFolder.Files.Any())
            //{
            //    currentFolder.bMarkRemove = false;
            //}
            //else
            //{
            //    currentFolder.bMarkRemove = true;
            //}
            currentFolder.bMarkRemove = !currentFolder.Files.Any() && bChildrenEmpty;

            //return (!currentFolder.bMarkRemove || bChildrenNotEmpty);    // *
            return currentFolder.bMarkRemove;
        }

        //private bool CleanupMarkFoldersDeletion(JFolder currentFolder) // Returns whether current folder or its sub folder contains files
        //{
        //    // Mark removing all subfolders that contain nothing
        //    bool bSubFolderContainFiles = true;
        //    foreach (JFolder folder in currentFolder.Folders)
        //    {
        //        bSubFolderContainFiles = CleanupMarkFoldersDeletion(folder);
        //    }
        //    // Clean self if no files contained; For root folder we won't delete itself
        //    currentFolder.bMarkRemove = false;
        //    if (currentFolder.Parent != null && currentFolder.Files.Any() == false && bSubFolderContainFiles == false)
        //    {
        //        // currentFolder.Parent.Folders.Remove(currentFolder);
        //        currentFolder.bMarkRemove = true;
        //    }

        //    return (currentFolder.Files.Any() || bSubFolderContainFiles);
        //}

        private void CleanupFolders(JFolder currentFolder)
        {
            // Remove all folder references
            List<JFolder> tempFoldersList = currentFolder.Folders.ToList();
            foreach (JFolder folder in tempFoldersList)
            {
                if (folder.bMarkRemove == true)
                {
                    currentFolder.Folders.Remove(folder);
                }
                else
                {
                    CleanupFolders(folder);
                }
            }

            // Expand Folder
            currentFolder.bExpanded = true;
        }

        // Get file path for specifci JFile in tree, considering QuickMatch
        private string GetFilePath(JFile file)
        {
            // QuickMatch® (4/4): Use QuickMatch® for QuickMatched files
            if (file.Parent.FolderName == App.QuickMatchFolderName)
            {
                JFolder matches = QuickMatch.QuickMatchFile(file.FileName, JRootFolderExclusive);

                // If no match found
                if (matches == null)
                {
                    StatusLabel.Content = "No match found.";
                    return null;
                }
                // If more than one match if found
                if (matches.Files.Count > 1)
                {
                    StatusLabel.Content = "More than one match is found, please do matching manually using magic explorer.";    // Better with a shortcut to it
                    return null;
                }
                // Otherwise there can be only one match
                else
                {
                    // Get JFile first
                    List<JFolder> subFolders = matches.Folders;
                    while(subFolders[0].Folders.Count != 0)
                    {
                        subFolders = subFolders[0].Folders;
                    }
                    JFile foundFile = subFolders[0].Files[0];
                    // Then get the file's path
                    return App.GetParentFolderPath(foundFile.Parent) + foundFile.FileName;
                }
            }
            else
            {
                return App.GetParentFolderPath(file.Parent) + file.FileName;
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract Item from metadata
            JFile item = (sender as Button).Tag as JFile;

            // Open file in system editor
            string filePath = GetFilePath(item);
            if(filePath != null)
            {
                System.Diagnostics.Process.Start(filePath);
            }
        }

        private void AppendFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract Item from metadata
            JFile item = (sender as Button).Tag as JFile;

            // Get file path
            string filePath = GetFilePath(item);
            if (filePath != null)
            {
                // Append content to original file
                if (File.Exists(filePath))
                {
                    // Append to file
                    using (StreamWriter sw = File.AppendText(filePath))
                    // Notice in this case the encoding being used is: UTF-8, meaning if appendix is ASCII then just fine since ANSI and UTF-8 both love ASCII but when source is ANSI while here is UTF-8 then two encodings will be mixed together in a single file; We suggest converting everything into UTF-8 BOM before we upload
                    {
                        sw.WriteLine(item.Appendix);
                    }

                    // Display Result
                    OriginalText.Text = File.ReadAllText(filePath);
                }
                else
                {
                    // Display Error
                    StatusLabel.Content = "File doesn't exist on local drive!";
                }
            }
        }

        private void ReplaceFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract Item from metadata
            JFile item = (sender as Button).Tag as JFile;

            // Get file path
            string filePath = GetFilePath(item);
            if (filePath != null)
            {
                // Write content to original file
                if (File.Exists(filePath))
                {
                    // Write to file
                    File.WriteAllText(filePath, item.TextContent);

                    // Display Result
                    OriginalText.Text = File.ReadAllText(filePath);
                }
                else
                {
                    // Display Error
                    StatusLabel.Content = "File doesn't exist on local drive!";
                }
            }
        }

        // When selected item changes we update its content/appendix in text view
        // Don't use SelectedItemChanged to retrieve items directly when using Data Binding: http://www.wpf-tutorial.com/treeview-control/handling-selection-expansion-state/, http://stackoverflow.com/questions/12856522/how-to-access-data-of-an-treeviewitem-object
        private void ChangesList_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Display Content in right panel
            //  JFile file = (TreeViewItem)e.NewValue as JFile;

            if (ChangesList.SelectedItem != null)
            {
                CopyContentButton.IsEnabled = true;

                JFile file = ChangesList.SelectedItem as JFile;
                if(file != null)
                {
                    // If it's file, then show its content or appendix
                    ReviweText.Text = file.TextContent != null? file.TextContent : (file.Appendix != null? file.Appendix : "No modification is available on this file, and this must be a bug since the file should be cleaned during Cleanup stage!");
                }
                else
                {
                    // It it's not a file, then clear content
                    ReviweText.Text = "";
                }

                // Another way to implement all these would be to add event handling mechanism inside children classes of TreeViewItemBase, i.e. JFile and JFolder
            }
            else
            {
                CopyContentButton.IsEnabled = false;
            }
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

        private void CopyContentButton_Click(object sender, RoutedEventArgs e)
        {
            JFile file = ChangesList.SelectedItem as JFile;
            if (file != null)
            {
                // Clipboard.SetText(ReviweText.Text); // http://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
                Clipboard.SetDataObject(ReviweText.Text);
            }
        }
    }
}

/* Testing
 * 1. Check out the sent back file has clean content (i.e. null)
 * */
