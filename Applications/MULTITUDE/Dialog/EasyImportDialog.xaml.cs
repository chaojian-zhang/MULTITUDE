using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using MULTITUDE.Class.Facility;

// MVVM: https://stackoverflow.com/questions/1131937/how-to-make-listview-update-itself-in-wpf

namespace MULTITUDE.Dialog
{
    public partial class EasyImportDialog : Window
    {
        public EasyImportDialog()
        {
            InitializeComponent();

            // Load all drives and populate the directory view
            RootFoldersList = new ObservableCollection<TreeFolderInfo>();
            DriveInfo[] drivesInfo = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drivesInfo)
            {
                string fullText = drive.Name + drive.VolumeLabel;

                TreeFolderInfo rootFolder = new TreeFolderInfo(fullText, new DirectoryInfo(drive.Name));
                rootFolder.Folders.Add(new TreeFolderInfo(true));
                RootFoldersList.Add(rootFolder);
            }
            DirectoryView.ItemsSource = RootFoldersList;
        }

        // Return value
        public ObservableCollection<TreeFolderInfo> RootFoldersList { get; set; }

        //        // Parameters
        //        static string HTMLStringStart =
        //@"<!DOCTYPE html>
        //<html>
        //    <head>
        //        <title>Folder Structure Tree</title>
        //    </head>
        //    <body>
        //        ";
        //        static string HTMLContent;
        //        static string HTMLStringEnd =
        //@"
        //    </body>
        //</html>";

        //        private async void SaveHTMLButton_Click(object sender, RoutedEventArgs e)
        //        {
        //            // Generate HTML fomr JSON
        //            // Add beginning
        //            HTMLContent += HTMLStringStart;
        //            // Recursion
        //            string indentation = "";
        //            int level = 0;
        //            RecursiveHTML(JUnifiedFolder, indentation, level);
        //            // Add end
        //            HTMLContent += HTMLStringEnd;

        //            // Select Path To Save File
        //            VistaSaveFileDialog saveFileDialog = new VistaSaveFileDialog();
        //            saveFileDialog.DefaultExt = ".html";
        //            saveFileDialog.FileName = "Folder Structure";
        //            saveFileDialog.Filter = "HTML Files(*.html) | *.html";
        //            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
        //            saveFileDialog.Title = "Select where to save HTML file";
        //            saveFileDialog.ValidateNames = true;
        //            bool? result = saveFileDialog.ShowDialog();
        //            if (result == true)
        //            {
        //                // Save File Content & Update Status
        //                System.IO.File.WriteAllText(saveFileDialog.FileName, HTMLContent);
        //                StatusLabel.Content = "Saved to file: " + saveFileDialog.FileName;
        //            }
        //        }

        //        private void RecursiveHTML(JFolder folder, string currentIndentation, int indentationLevel)
        //        {
        //            // Do it once and use it multiple times
        //            string folderPath = App.GetParentFolderPath(folder);

        //            // Iterate folders
        //            foreach (JFolder sub in folder.Folders)
        //            {
        //                HTMLContent = HTMLContent + currentIndentation + String.Concat(Enumerable.Repeat("&emsp;", indentationLevel)) + 
        //                    String.Format("<a href = \"file:///{0}\">{1}</a>", folderPath + "\\" + sub.FolderName, sub.FolderName) + "<br>\r\n";
        //                RecursiveHTML(sub, currentIndentation + "\t", indentationLevel + 1);
        //            }

        //            // Iterate files
        //            foreach (JFile file in folder.Files)
        //            {
        //                HTMLContent = HTMLContent + currentIndentation + String.Concat(Enumerable.Repeat("&emsp;", indentationLevel)) + 
        //                    String.Format("<a href = \"file:///{0}\">{1}</a>", folderPath + "\\" + file.FileName, file.FileName) +
        //                "<br>\r\n";
        //            }
        //        }

        //        private void RecursiveText(JFolder folder, string currentIndentation)
        //        {
        //            // Iterate folders
        //            foreach (JFolder sub in folder.Folders)
        //            {
        //                PlainFolderStructure = PlainFolderStructure + currentIndentation + "▷" + sub.FolderName + "\n";
        //                RecursiveText(sub, currentIndentation + "\t");
        //            }

        //            // Iterate files
        //            foreach (JFile file in folder.Files)
        //            {
        //                PlainFolderStructure = PlainFolderStructure + currentIndentation + file.FileName + "\n";
        //            }
        //        }

        #region Selection Handling
        private void FolderSelectionCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            // If a folder is unchecked, uncheck all its children as well
            TreeFolderInfo folder = (sender as CheckBox).DataContext as TreeFolderInfo;
            foreach (TreeFolderInfo subFolder in folder.Folders)
            {
                subFolder.bSelected = false;
            }

            // Notice since we are using dependecy property this will automatically update UI, not just data itself
        }

        private void FolderSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // If a folder is checked, check all its children as well
            TreeFolderInfo folder = (sender as CheckBox).DataContext as TreeFolderInfo;
            foreach (TreeFolderInfo subFolder in folder.Folders)
            {
                subFolder.bSelected = true;
            }
        }

        private void FileSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TreeFileInfo file = (sender as CheckBox).DataContext as TreeFileInfo;
            file.bSelected = true;
        }

        private void FileSelectionCheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            TreeFileInfo file = (sender as CheckBox).DataContext as TreeFileInfo;
            file.bSelected = false;
        }
        #endregion

        #region Expansion Handling
        private void DirectoryView_Expanded(object sender, RoutedEventArgs e)
        {
            // If a folder is checked, check all its children as well
            TreeFolderInfo folder = (e.OriginalSource as TreeViewItem).DataContext as TreeFolderInfo;
            if (folder == null) return; // Can be a file rather than a folder

            // If the folder isn't already loaded then load it
            if (folder.IsTemp)
            {
                // Clear existing content
                folder.Folders.Clear();

                // Add new content
                MULTITUDE.Class.Facility.TreeFolderInfo.FolderGenerator(folder);
            }

            folder.bExpanded = true;
        }

        private void DirectoryView_Collapsed(object sender, RoutedEventArgs e)
        {
            TreeFolderInfo folder = (e.OriginalSource as TreeViewItem).DataContext as TreeFolderInfo;
            if (folder == null) return; // Can be a file rather than a folder
            folder.bExpanded = false;
        }
        #endregion

        #region Dialog and Window Handling
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        #endregion

    }
}
