using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl.DocumentIcons
{
    /// <summary>
    /// Interaction logic for ArchiveDocumentIcon.xaml
    /// </summary>
    public partial class ArchiveDocumentIcon : UserControl, INotifyPropertyChanged
    {
        internal ArchiveDocumentIcon(Document doc)
        {
            InitializeComponent();

            // Feed content
            Archive archive = doc as Archive;
            if (archive.IsReal == false) throw new InvalidOperationException("VA cannot be displayed in real archive preview icon.");
            System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(archive.Path);
            List1 = ListFileInfo.PopulateContents(info);

            // Bookeeping
            RootPath = info.FullName;
            CenterList = PageList1;

            // UI Setup
            bRoot = true;
            BackFolderName = info.Name;
            Path = info.FullName;
            Current = info;
            CenterList = PageList1;
        }

        // Book keeping
        string RootPath;
        System.IO.DirectoryInfo Current;
        ListBox CenterList;
        #region Interface Events
        private void IconImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(((sender as Image).DataContext as ListFileInfo).Path);
            // See ArchiveViewer for optional actions; Currently we don't support open non-documents
        }

        private void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = sender as ListBox;
            ListFileInfo selection = box.SelectedItem as ListFileInfo;

            // If a file do nothing, If a folder then Generate
            if (selection!= null && selection.Type == ListFileType.Folder)
            {
                // Load
                if(box.ItemsSource == List1) List2 = ListFileInfo.PopulateContents(selection.Path);
                else List1 = ListFileInfo.PopulateContents(selection.Path);

                // Change
                if (box == PageList1) PageList2.ItemsSource = (box.ItemsSource == List1) ? List2 : List1;
                else PageList1.ItemsSource = (box.ItemsSource == List1) ? List2 : List1;

                // Animation switch
                AnimateRight(box);

                // UI Setup
                bRoot = false;
                BackFolderName = selection.Name;
                Path = selection.Path;
                Current = new System.IO.DirectoryInfo(selection.Path);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            System.IO.DirectoryInfo Parent = Current.Parent;
            if(Current.FullName != RootPath && Parent != null)
            {
                // Load
                if (CenterList.ItemsSource == List1) List2 = ListFileInfo.PopulateContents(Parent);
                else List1 = ListFileInfo.PopulateContents(Parent);

                // Change
                if (CenterList == PageList1) PageList2.ItemsSource = (CenterList.ItemsSource == List1) ? List2 : List1;
                else PageList1.ItemsSource = (CenterList.ItemsSource == List1) ? List2 : List1;

                // Animate Switch
                AnimateLeft(CenterList);

                // UI Setup
                bRoot = Parent.FullName == RootPath ? true : false;
                BackFolderName = Parent.Name;
                Path = Parent.FullName;
                Current = Parent;
            }
        }

        private void CoppyPathButton_Click(object sender, RoutedEventArgs e)
        {
            // Command = "Copy" CommandTarget = "{Binding ElementName=LocationTextbox}" // Will not work since TextBox is read only
            System.Windows.Clipboard.SetText(LocationTextbox.Text);
        }

        private void AnimateRight(ListBox current)
        {
            // Basic Info
            double translateDistance = ListCanvas.ActualWidth;
            TranslateTransform centerList = (current == PageList1) ? PageList1Translate : PageList2Translate;
            TranslateTransform rightList = (current == PageList1) ? PageList2Translate : PageList1Translate;

            // Move current to left, move spare one to center
            DoubleAnimation translateXCenterToLeft = new DoubleAnimation();
            translateXCenterToLeft.From = 0;
            translateXCenterToLeft.To = -translateDistance;
            translateXCenterToLeft.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));
            DoubleAnimation translateXRightToCenter = new DoubleAnimation();
            translateXRightToCenter.From = translateDistance;
            translateXRightToCenter.To = 0;
            translateXRightToCenter.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));

            // Play it
            centerList.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, translateXCenterToLeft);
            rightList.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, translateXRightToCenter);

            // Book keeping
            CenterList = (current == PageList1) ? PageList2 : PageList1;
        }

        private void AnimateLeft(ListBox current)
        {
            // Basic Info
            double translateDistance = ListCanvas.ActualWidth;
            TranslateTransform centerList = (current == PageList1) ? PageList1Translate : PageList2Translate;
            TranslateTransform leftList = (current == PageList1) ? PageList2Translate : PageList1Translate;

            // Move current to left, move spare one to center
            DoubleAnimation translateXCenterToRight = new DoubleAnimation();
            translateXCenterToRight.From = 0;
            translateXCenterToRight.To = translateDistance;
            translateXCenterToRight.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));
            DoubleAnimation translateXLeftToCenter = new DoubleAnimation();
            translateXLeftToCenter.From = -translateDistance;
            translateXLeftToCenter.To = 0;
            translateXLeftToCenter.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));

            // Play it
            centerList.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, translateXCenterToRight);
            leftList.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, translateXLeftToCenter);

            // Book keeping
            CenterList = (current == PageList1) ? PageList2 : PageList1;
        }
        #endregion

        #region Drag-drop Support
        private void PageList_MouseMove(object sender, MouseEventArgs e)
        {
            // If LMB down and is current selection
            ListBox box = sender as ListBox;
            if (box != null && e.LeftButton == MouseButtonState.Pressed && box.SelectedItem != null)
            {
                // Package the data: path of the file/folder
                DataObject data = new DataObject();
                data.SetData(DropRequest.DropRequestDropDataFormatString, new DropRequest(DropRequestType.SimpleClueReference, (box.SelectedItem as ListFileInfo).Path));

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Link);
                e.Handled = true;
            }
        }

        // Overwrites VW
        private void FolderNavigator_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void FolderNavigator_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void FolderNavigator_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void FolderNavigator_DragLeave(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

        #region Interface Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _bRoot;
        private string _path;
        private string _BackFolderName;
        private ObservableCollection<ListFileInfo> _List1;
        private ObservableCollection<ListFileInfo> _List2;

        public bool bRoot
        {
            get { return _bRoot; }
            set
            {
                if (value != this._bRoot)
                {
                    this._bRoot = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Path
        {
            get { return _path; }
            set
            {
                if (value != this._path)
                {
                    this._path = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string BackFolderName
        {
            get { return _BackFolderName; }
            set
            {
                if (value != this._BackFolderName)
                {
                    this._BackFolderName = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ObservableCollection<ListFileInfo> List1
        {
            get { return _List1; }
            set
            {
                if (value != this._List1)
                {
                    this._List1 = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ObservableCollection<ListFileInfo> List2
        {
            get { return _List2; }
            set
            {
                if (value != this._List2)
                {
                    this._List2 = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
