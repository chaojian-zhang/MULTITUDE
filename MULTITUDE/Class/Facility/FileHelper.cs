using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.IO;
using System.Windows.Media.Imaging;

namespace MULTITUDE.Class.Facility
{
    // Combines two different lists into one for Treeview to consume, combined with templates we achieve a different effect for folders and files
    public class DirectoryItemsSourceCreator : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TreeFolderInfo input = value as TreeFolderInfo;
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

    //public class DirectoryItemsCountCreator : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        TreeFolderInfo input = value as TreeFolderInfo;
    //        return (input.Folders.Count + input.Files.Count).ToString();
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    /// <summary>
    /// A class specialzed for data binding
    /// </summary>
    public class TreeFolderInfo : INotifyPropertyChanged
    {
        public TreeFolderInfo(bool bTemp)
        {
            DisplayName = TempLoadingFolderString;
            Info = null;

            bSelected = false;
            bExpanded = false;

            _Files = new ObservableCollection<TreeFileInfo>();
            _Folders = new ObservableCollection<TreeFolderInfo>();
        }

        public TreeFolderInfo(string displayName, DirectoryInfo info, bool bWithTempFolder = false)
        {
            DisplayName = displayName;
            Info = info;

            bSelected = false;
            bExpanded = false;

            _Files = new ObservableCollection<TreeFileInfo>();
            _Folders = new ObservableCollection<TreeFolderInfo>();

            if(bWithTempFolder == true)
            {
                Folders.Add(new TreeFolderInfo(true));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Properties
        private string DisplayName;
        private DirectoryInfo _Info;
        private ObservableCollection<TreeFileInfo> _Files;
        private ObservableCollection<TreeFolderInfo> _Folders;
        private bool _bSelected;
        private bool _bExpanded;

        // Helpers
        public static readonly string TempLoadingFolderString = "Loading...";
        // Used to expand current folder by one layer: load all items under this folder
        public static void FolderGenerator(TreeFolderInfo currentFolder)
        {
            // Clear temp
            currentFolder.Folders.Clear();

            // Add items to folder
            try
            {
                foreach (DirectoryInfo subInfo in currentFolder.Info.EnumerateDirectories())
                {
                    // Generate Folders
                    TreeFolderInfo subFolder = new TreeFolderInfo(null, subInfo);
                    subFolder.bSelected = currentFolder.bSelected;  // Let Children share the same status of selection as parent
                    subFolder.Folders.Add(new TreeFolderInfo(true));
                    currentFolder.Folders.Add(subFolder);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }

            try
            {
                foreach (FileInfo file in currentFolder.Info.EnumerateFiles())
                {
                    // Generate Files
                    TreeFileInfo fileInfo = new TreeFileInfo(file);
                    fileInfo.bSelected = currentFolder.bSelected;
                    currentFolder.Files.Add(fileInfo);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (System.IO.PathTooLongException) { }
        }

        public string ItemsCount { get { if (!(_Folders.Count == 1 && _Folders[0]._Info == null)) { return (_Files.Count + _Folders.Count).ToString(); } else { return "..."; } } }
        public DirectoryInfo Info
        {
            get { return this._Info; }
            set
            {
                if (value != this._Info)
                {
                    this._Info = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ItemsCount");
                }
            }
        }
        public ObservableCollection<TreeFileInfo> Files
        {
            get { return _Files; }
            set
            {
                if (value != this._Files)
                {
                    this._Files = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ItemsCount");
                }
            }
        }
        public ObservableCollection<TreeFolderInfo> Folders
        {
            get { return _Folders; }
            set
            {
                if (value != this.Folders)
                {
                    this.Folders = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ItemsCount");
                }
            }
        }
        public bool bSelected
        {
            get { return _bSelected; }
            set
            {
                if (value != this._bSelected)
                {
                    this._bSelected = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ItemsCount");
                }
            }
        }
        public bool bExpanded
        {
            get { return _bExpanded; }
            set
            {
                if (value != this._bExpanded)
                {
                    this._bExpanded = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ItemsCount");
                }
            }
        }
        public string Name { get { return DisplayName != null ? DisplayName : Info.Name; } }
        public bool IsTemp
        {
            get
            {
                return (Folders.Count == 1) && (Folders[0].Name == TempLoadingFolderString);
            }
        }
    }

    public class TreeFileInfo : INotifyPropertyChanged
    {
        public TreeFileInfo(FileInfo info)
        {
            Info = info;
            bSelected = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Properties
        private FileInfo _Info;
        private bool _bSelected;

        public FileInfo Info
        {
            get { return _Info; }
            set
            {
                if (value != this._Info)
                {
                    this._Info = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool bSelected
        {
            get { return _bSelected; }
            set
            {
                if (value != this._bSelected)
                {
                    this._bSelected = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Name { get { return _Info.Name; } }
    }

    public enum ListFileType
    {
        File,
        Folder,
    }

    /// <summary>
    /// A representation of either a folder or a file
    /// </summary>
    public class ListFileInfo : INotifyPropertyChanged
    {
        public ListFileInfo(ListFileType type, string path, string name)
        {
            switch (type)
            {
                case ListFileType.File:
                    _image = FileImage;
                    break;
                case ListFileType.Folder:
                    _image = FolderImage;
                    break;
                default:
                    break;
            }

            _path = path;
            _name = name;
            Type = type;
        }

        internal static readonly BitmapImage FolderImage = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/Folder(Closed) Icon.png"));
        internal static readonly BitmapImage FileImage = new BitmapImage(new Uri("pack://application:,,,/Resource/Icons/File Icon.png"));

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Properties
        public ListFileType Type { get; set; }
        private string _name;
        private string _path;
        private BitmapImage _image;

        public string Path
        {
            get { return _path; }
            set
            {
                if (value != this._path)
                {
                    this._path= value;
                    NotifyPropertyChanged();
                }
            }
        }
        public BitmapImage Image
        {
            get { return _image; }
            set
            {
                if (value != this._image)
                {
                    this._image = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public static ObservableCollection<ListFileInfo> PopulateContents(string path)
        {
            return PopulateContents(new DirectoryInfo(path));
        }
        public static ObservableCollection<ListFileInfo> PopulateContents(DirectoryInfo dir)
        {
            ObservableCollection<ListFileInfo> contents = new ObservableCollection<ListFileInfo>();

            // Get folders
            try
            {
                foreach (DirectoryInfo subInfo in dir.EnumerateDirectories()) contents.Add(new ListFileInfo(ListFileType.Folder, subInfo.FullName, subInfo.Name));
            }
            catch (UnauthorizedAccessException) { }
            catch (PathTooLongException) { }

            // Get files
            try
            {
                foreach (FileInfo file in dir.EnumerateFiles()) contents.Add(new ListFileInfo(ListFileType.File, file.FullName, file.Name));
            }
            catch (UnauthorizedAccessException) { }
            catch (System.IO.PathTooLongException) { }

            return contents;
        }
    }

    // <Development> For ultimate file safety we might want to check out complete guide here: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/file-system/how-to-copy-delete-and-move-files-and-folders; And checkout complete exceptions that might exist
    class FileHelper
    {
        public static void MoveOrRenameDirectoryOrFile(string oldPath, string newPath)
        {
            if (oldPath == newPath) return;
            try
            {
                System.IO.Directory.Move(oldPath, newPath);    // Be it a file or directory
            }
            catch (System.IO.IOException)   // Cannot move dir across different drives
            {
                System.IO.DirectoryInfo source = new DirectoryInfo(oldPath);
                System.IO.DirectoryInfo dest = new DirectoryInfo(newPath);

                // Target already exist
                if (dest.Exists) throw new InvalidOperationException("Target directory already exists and I don't know how to handle it.");
                // Move across volume
                else
                {
                    ArchiveHelper.CopyFilesRecursively(source, dest);
                    source.Delete(true);
                }
            }
        }
    }
}
