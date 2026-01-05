using Meta.Vlc.Wpf;
using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Popup;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl.DocumentIcons
{
    /// <summary>
    /// Interaction logic for PlayListDocumentIcon.xaml
    /// Notice for playlists we never display video images but only sounds
    /// </summary>
    public partial class PlayListDocumentIcon : UserControl, INotifyPropertyChanged
    {
        #region Construction and Destruction
        internal PlayListDocumentIcon(Document doc)
        {
            InitializeComponent();

            Document = doc;

            // Depending on document type this control will be used and displayed slightly differently
            switch (doc.Type)
            {
                case DocumentType.PlayList:
                    PlaylistButton.Visibility = Visibility.Visible;
                    CreatePlaylistButton.Visibility = Visibility.Collapsed;
                    break;
                case DocumentType.Sound:
                    // Display image of sound files
                    break;
                case DocumentType.Video:
                    // Same VideoIcon image but instead of V we have video name text; Or better we have some screenshots about that video, e.g.created by user during watching the movie, though I don't believe screenshot can help make VW very clean-looking
                    break;
            }
            MediaControls.Setup(VirtualWorkspaceWindow.CurrentWindow.GetVlcPlayerHandler(), doc);

            // Manual Binding
            Document.PropertyChanged += Document_PropertyChanged;
            Document_PropertyChanged(null, null);
            MediaControls.PropertyChanged += MediaControls_PropertyChanged;
            PlayingDocument = MediaControls.PlayingDocument;

            NotifyPropertyChanged("DisplayName");
            NotifyPropertyChanged("DisplayText");
        }
        #endregion

        internal Document Document = null;
        private Document PlayingDocument = null;

        #region Interface Interaction
        private void PlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist list = Document as Playlist;
            if (list != null)
            {
                // Show playlist content popup
                PlayListContentEditor popup = new PlayListContentEditor(list);
                popup.Show();
            }
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (Document.Type != DocumentType.PlayList)
            {
                VirtualWorkspaceWindow.CurrentWindow.CreatePlaylistFromMedia(this);
            }
        }
        #endregion

        #region Data Binding
        private void Document_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged("DisplayName");
            NotifyPropertyChanged("DisplayText");
        }
        private void MediaControls_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PlayingDocument = MediaControls.PlayingDocument;
            NotifyPropertyChanged("DisplayName");
            NotifyPropertyChanged("DisplayText");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string DisplayName
        {
            get
            {
                if (Document != null)// Can be null during initialization
                {
                    if (Document.Type == DocumentType.PlayList)
                        return "Playlist: " + Document.Name;
                    else return Document.Name;
                }
                else return null;
            }  
            set
            {
                if (value != Document.Name)
                {
                    Document.Name = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string DisplayText
        {
            get
            {
                if (Document != null) // Can be null during initialization
                {
                    if (Document.Type == DocumentType.PlayList && PlayingDocument != null)
                        return "Now Playing: " + PlayingDocument.Name;
                    else
                        return Document.Comment;
                }
                else return null;
            }  
            set
            {
                if (value != this.Document.Comment)
                {
                    this.Document.Comment = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
