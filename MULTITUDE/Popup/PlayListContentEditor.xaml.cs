using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.Popup
{
    /// <summary>
    /// Interaction logic for PlayListContentEditor.xaml
    /// </summary>
    public partial class PlayListContentEditor : Window, INotifyPropertyChanged
    {
        internal PlayListContentEditor(Playlist playlist)
        {
            InitializeComponent();

            Owner = VirtualWorkspaceWindow.CurrentWindow;
            _PlayList = playlist;
            // Setup
            SetupInterface();
        }

        private void SetupInterface()
        {
            PlayListClueStringsList.ItemsSource = _PlayList.MediaClueStrings;
            DetailedDocumentsList.ItemsSource = _PlayList.GetAllMediasList();
            PlayListOptions.DataContext = _PlayList;
        }

        #region Basic Interface Events
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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

        private Playlist _PlayList;
        internal Playlist PlayList
        {
            get { return this._PlayList; }
            set
            {
                if (value != this._PlayList)
                {
                    this._PlayList = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #region Interface Interactions
        private void NewCategoryTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            if(e.Key == Key.Enter && string.IsNullOrWhiteSpace(textbox.Text) == false)
            {
                _PlayList.AddMediaClueString(textbox.Text);
                textbox.Text = string.Empty;
                // Manual update
                PlayListClueStringsList.ItemsSource = null;
                PlayListClueStringsList.ItemsSource = _PlayList.MediaClueStrings;

                // Update List of found documents
                DetailedDocumentsList.ItemsSource = _PlayList.GetAllMediasList();
            }
        }
        private void PlayListClueStringsList_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            // Update List of found documents
            DetailedDocumentsList.ItemsSource = _PlayList.GetAllMediasList();
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Solution 1
            // _PlayList.RemoveEmptyMediaClueString();  // Not working because at this time data hadn't been updated
            // Solution 2
            TextBox box = (sender as TextBox);
            if (string.IsNullOrWhiteSpace(box.Text))
            {
                _PlayList.RemoveMediaClueString(box.DataContext as string);
                box.Visibility = Visibility.Collapsed;
                // Update List of found documents
                DetailedDocumentsList.ItemsSource = _PlayList.GetAllMediasList();
            }
            // Since binding isn't working we need to manually update
            else
            {
                // <Development> Cautious performance impact
                _PlayList.UpdateMediaClueString(box.DataContext as string, box.Text);
                // Update List of found documents
                DetailedDocumentsList.ItemsSource = _PlayList.GetAllMediasList();
                // Update box itself
                box.DataContext = box.Text;
            }
        }
        #endregion
    }
}
