using Meta.Vlc.Wpf;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl.Components
{
    /// <summary>
    /// Interaction logic for MediaControls.xaml
    /// </summary>
    public partial class MediaControls : UserControl, INotifyPropertyChanged
    {
        public MediaControls()
        {
            InitializeComponent();

            // Initialize some members
            PlayButtonPlayIcon = (DrawingImage)FindResource("MediaPlayRightTriangle");
            PlayButtonPauseIcon = (DrawingImage)FindResource("MediaPauseDoubleLines");

            // Timer
            MediaAutoplayTimer = new System.Windows.Threading.DispatcherTimer();
            MediaAutoplayTimer.Tick += MediaAutoplayTimer_TimeUp;
            MediaAutoplayTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);   // 1000 ms
        }

        internal void Setup(VlcPlayer player, Document doc, bool bAutoPlay = false)
        {
            _AssociatedPlayer = player;
            _MediaDocument = doc;

            // Validate Document Type
            if (MediaDocument.Type != DocumentType.Sound && MediaDocument.Type != DocumentType.Video && MediaDocument.Type != DocumentType.PlayList)
                throw new InvalidOperationException("Documents set to MediaControls needs to be either sound or video or Playlists.");

            // Get the document to be played
            SetupMedia();

            // Update Binding
            _AssociatedPlayer.StateChanged += _AssociatedPlayer_StateChanged;

            // Auto play
            if (bAutoPlay) ScheduleAutoplay();
        }
        private void SetupMedia()
        {
            if (MediaDocument.Type == DocumentType.Sound || MediaDocument.Type == DocumentType.Video)
                PlayingDocument = MediaDocument;
            else
                PlayingDocument = (MediaDocument as Playlist).GetCurrentMedia();
        }

        private System.Windows.Threading.DispatcherTimer MediaAutoplayTimer;

        #region UI Interactions
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            AssociatedPlayer.Stop();
        }

        private void _AssociatedPlayer_StateChanged(object sender, Meta.Vlc.ObjectEventArgs<Meta.Vlc.Interop.Media.MediaState> e)
        {
            // In case of shared player, e.g. in VW
            if (PlayingDocument == null || (sender as VlcPlayer).Tag as Document != PlayingDocument) // PlayingDocument can be null if it's a play list
            {
                PlayButtonIcon.Source = PlayButtonPlayIcon;
                return;
            }

            if(e.Value == Meta.Vlc.Interop.Media.MediaState.Ended || 
                e.Value == Meta.Vlc.Interop.Media.MediaState.Paused ||
                e.Value == Meta.Vlc.Interop.Media.MediaState.Stopped)
                PlayButtonIcon.Source = PlayButtonPlayIcon;
            else PlayButtonIcon.Source = PlayButtonPauseIcon; 

            if(e.Value == Meta.Vlc.Interop.Media.MediaState.Ended)
            {
                GetNextPlayMedia();

                // Schedule next play
                MediaAutoplayTimer.Start();
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the document to be played
            if (MediaDocument.Type == DocumentType.Sound || MediaDocument.Type == DocumentType.Video)
                PlayingDocument = MediaDocument;
            else
            {
                PlayingDocument = (MediaDocument as Playlist).MoveToPreviousMedia();
                if (PlayingDocument == null) PlayingDocument = (MediaDocument as Playlist).GetCurrentMedia();
            }

            LoadMedia();
            Play();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the document to be played
            GetNextPlayMedia();

            LoadMedia();
            Play();
        }

        private void GetNextPlayMedia()
        {
            if (MediaDocument.Type == DocumentType.Sound || MediaDocument.Type == DocumentType.Video)
                PlayingDocument = MediaDocument;
            else
            {
                PlayingDocument = (MediaDocument as Playlist).MoveToNextMedia();
                if (PlayingDocument == null) PlayingDocument = (MediaDocument as Playlist).GetCurrentMedia();
            }
        }

        private void ScheduleAutoplay()
        {
            // We can not play it directly because if the file cannot be loaded instantly there will be threading issue in vlcPlayer
            MediaAutoplayTimer.Start();
        }

        private void MediaAutoplayTimer_TimeUp(object sender, EventArgs e)
        {
            // Play
            Play();

            // Stop timer
            MediaAutoplayTimer.Stop();
        }

        private void LoadMedia()
        {
            if(PlayingDocument != null)
            {
                AssociatedPlayer.LoadMedia(PlayingDocument.Path);
                AssociatedPlayer.Tag = PlayingDocument;
            }
            else
                AssociatedPlayer.Tag = null;
            // AssociatedPlayer.Play();
            // AssociatedPlayer.Pause();
        }

        private void Play()
        {
            if (PlayingDocument == null) SetupMedia();
            if (PlayingDocument == null) { PlayButtonIcon.Source = PlayButtonPlayIcon; return; }

            // In case of shared player, e.g. in VW, pause it first then play ours
            if (AssociatedPlayer.Tag as Document != PlayingDocument)
            {
                AssociatedPlayer.Pause();
                LoadMedia();
            }

            // Check current play state
            if (AssociatedPlayer.State == Meta.Vlc.Interop.Media.MediaState.Playing)
            {
                AssociatedPlayer.Pause();
            }
            else if (AssociatedPlayer.State == Meta.Vlc.Interop.Media.MediaState.Stopped
                || AssociatedPlayer.State == Meta.Vlc.Interop.Media.MediaState.Ended
                || AssociatedPlayer.State == Meta.Vlc.Interop.Media.MediaState.NothingSpecial)
            {
                AssociatedPlayer.Play();
            }
            else if (AssociatedPlayer.State == Meta.Vlc.Interop.Media.MediaState.Paused)
            {
                AssociatedPlayer.Resume();
            }
            else
            {
                AssociatedPlayer.Pause();
            }

            // Play Media
            // AssociatedPlayer.PauseOrResume();
        }

        private DrawingImage PlayButtonPlayIcon;
        private DrawingImage PlayButtonPauseIcon;
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void VolumeImage_Click(object sender, MouseButtonEventArgs e)
        {
            // Mute
            // Also update icon
        }
        private void SliderWithProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(AssociatedPlayer != null)    // Can be  null during initialization
                AssociatedPlayer.Volume = (int)VolumeSlider.Value;
        }

        private void DockPanel_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;   // Avoid tralation in VW
        }
        #endregion

        #region Data Binding
        #region Interface Definitions
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        internal VlcPlayer _AssociatedPlayer;
        internal Document _MediaDocument;
        internal Document MediaDocument // The governing media of current player, e.g. the specific sound or video or playlist file
        {
            get { return _MediaDocument; }
            set
            {
                if (value != this._MediaDocument)
                {
                    this._MediaDocument = value;
                    NotifyPropertyChanged();
                }
            }
        }
        internal VlcPlayer AssociatedPlayer
        {
            get { return _AssociatedPlayer; }
            set
            {
                if (value != this._AssociatedPlayer)
                {
                    this._AssociatedPlayer = value;
                    NotifyPropertyChanged();
                }
            }
        }

        internal Document _PlayingDocument = null;    // The current document being played, for sound/video it is the media document itself, for playlist it's the item in the playlist
        internal Document PlayingDocument
        {
            get { return _PlayingDocument; }
            set
            {
                if (value != this._PlayingDocument)
                {
                    this._PlayingDocument = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
