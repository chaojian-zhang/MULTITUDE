using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using MULTITUDE.Dialog;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.Popup
{
    /// <summary>
    /// Simple archive browser for self-contained archive of files; Back button goes up to current archive
    /// </summary>
    public partial class ArchiveViewer : Window, INotifyPropertyChanged
    {
        internal ArchiveViewer(Window owner, Document doc = null)
        {
            InitializeComponent();
            Owner = owner;

            // Initialize parameters
            RootFoldersList = new ObservableCollection<TreeFolderInfo>();

            if (doc != null)
            {
                Archive = doc as Archive;

                // Display current archive
                RootFoldersList.Add(new TreeFolderInfo(null, new System.IO.DirectoryInfo(doc.Path), true));
            }
            else
            {
                // Load root drives
                LoadRootDrives();
            }
        }
        private Archive Archive = null;

        #region Helper
        private void LoadRootDrives()
        {
            // Load root drives
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                string fullText = drive.Name + drive.VolumeLabel;
                RootFoldersList.Add(new TreeFolderInfo(fullText, new DirectoryInfo(drive.Name), true));
            }
        }
        private void LoadSubDirectories()
        {
            // Show subdirectory
            DirectoryInfo[] folders = (new DirectoryInfo(LocationText)).GetDirectories();
            foreach (DirectoryInfo folder in folders)
            {
                RootFoldersList.Add(new TreeFolderInfo(null, folder, true));
            }
        }
        #endregion

        #region Basic UI Interaction
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void LocationLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open an aura first
            ModalDialogAura modalAura = new ModalDialogAura(this);
            modalAura.Show();

            // Open folder browser
            string path = null;
            if (Archive != null) path = Archive.Path;
            OpenFolderDialog dialog = new OpenFolderDialog(this, path);
            if (dialog.ShowDialog() == true)
            {
                LocationText = dialog.ChosenDirectoryPath;

                // Update display
                RootFoldersList.Clear();
                if (string.IsNullOrWhiteSpace(LocationText) == false) LoadSubDirectories();
                else LoadRootDrives();
            }
            else { } // Nothing happens 

            modalAura.Close();
        }
        #endregion

        #region Content Browsing
        private void DirectoryView_Expanded(object sender, RoutedEventArgs e)
        {
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

        private void DirectoryView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeFolderInfo folder = (sender as TreeView).SelectedItem as TreeFolderInfo;
            // Can be a file rather than a folder
            if (folder == null) AddFolderAllowed = false;
            else AddFolderAllowed = true;
        }

        private void TreeviewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Currently renaming is not implemented because it can involve quite some complicated (and unncessary) exceptions
            //// Ref: https://stackoverflow.com/questions/4295897/wpf-double-click-treeviewitem-child-node
            //TreeViewItem item = sender as TreeViewItem;

            //if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            //{
            //    if (item.DataContext is TreeFileInfo)
            //    {
            //        e.Handled = true;
            //    }
            //    else if (item.DataContext is TreeFolderInfo)
            //    {
            //        e.Handled = true;
            //    }
            //    else throw new InvalidOperationException("Unexpected data binding type.");
            //}
            //else
            //{
            //    if (item.DataContext is TreeFileInfo)
            //    {
            //        e.Handled = true;
            //    }
            //    else if (item.DataContext is TreeFolderInfo)
            //    {
            //        e.Handled = true;
            //    }
            //    else throw new InvalidOperationException("Unexpected data binding type.");
            //}
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FileIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FileInfo fileInfo = ((sender as Image).DataContext as TreeFileInfo).Info;

            // If ALT is pressed, open property window
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                SystemInterpService.ShowFileProperties(fileInfo.FullName);
            else
                // Handle as a file, currently our integrated viewer/editors doesn't support opening file directly using integrated canvas spaces so we use system handlers
                System.Diagnostics.Process.Start(fileInfo.FullName);
        }

        private void FolderIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DirectoryInfo dirInfo = ((sender as Image).DataContext as TreeFolderInfo).Info;

            // If ALT is pressed, open property window
            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                SystemInterpService.ShowFileProperties(dirInfo.FullName);
            else
                System.Diagnostics.Process.Start(dirInfo.FullName);
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

        private ObservableCollection<TreeFolderInfo> _RootFoldersList;
        public ObservableCollection<TreeFolderInfo> RootFoldersList
        {
            get { return this._RootFoldersList; }
            set
            {
                if (value != this._RootFoldersList)
                {
                    this._RootFoldersList = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string _LocationText;
        public string LocationText
        {
            get { return this._LocationText; }
            set
            {
                if (value != this._LocationText)
                {
                    this._LocationText = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private bool _AddFolderAllowed = false;
        public bool AddFolderAllowed
        {
            get { return this._AddFolderAllowed; }
            set
            {
                if (value != this._AddFolderAllowed)
                {
                    this._AddFolderAllowed = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
