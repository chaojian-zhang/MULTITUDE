using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
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

namespace MULTITUDE.CustomControl.CanvasSpaceWindow
{
    /// <summary>
    /// An intelligent player used to play both music and video; For music we have lyrics and background updates, for videos we have notes with a MD+ behind document; Also different media controls and visualizations might appear.
    /// </summary>
    public partial class MediaPlayerWindow : Window
    {
        #region Construction and Destruction
        public MediaPlayerWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

        internal void Update(MULTITUDE.Class.DocumentTypes.Document target)
        {
            // Switch interface elements depending on document type
            switch (target.Type)
            {
                case Class.DocumentTypes.DocumentType.PlayList:
                    UpdateInterfaceVisibility((target as Playlist).GetCurrentMedia().Type);
                    break;
                case Class.DocumentTypes.DocumentType.Sound:
                case Class.DocumentTypes.DocumentType.Video:
                    UpdateInterfaceVisibility(target.Type);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported document type in Media Player.");
            }

            // Setup playback
            MediaControls.Setup(vlcPlayer, target, true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vlcPlayer.Stop();
            // https://stackoverflow.com/questions/13086113/event-to-know-usercontrols-disposal
            vlcPlayer.Dispose();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }
        #endregion

        #region UI Management and Interaction
        private void UpdateInterfaceVisibility(MULTITUDE.Class.DocumentTypes.DocumentType type)
        {
            switch (type)
            {
                case Class.DocumentTypes.DocumentType.Sound:
                    // Hide VLC player (though still using it)
                    vlcPlayer.Visibility = Visibility.Collapsed;
                    // Hide Lyrics
                    MusicLyricListBox.Visibility = Visibility.Visible;
                    // Show Notes
                    NoteSenseBorder.Visibility = Visibility.Collapsed;
                    break;
                case Class.DocumentTypes.DocumentType.Video:
                    // Show VLC player
                    vlcPlayer.Visibility = Visibility.Visible;
                    // Hide Lyrics
                    MusicLyricListBox.Visibility = Visibility.Collapsed;
                    // Show Notes
                    NoteSenseBorder.Visibility = Visibility.Visible;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported document type in visibility setting.");
            }
        }

        private void MusicLyricListBox_MouseMove(object sender, MouseEventArgs e)
        {
            // Lyric scroll and music playback
            // ... 
        }
        #endregion

        #region Playback Interaction
        private void SliderWithProgress_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var value = (float)(e.GetPosition(PlayProgressSlider).X / PlayProgressSlider.ActualWidth);
            PlayProgressSlider.Value = value;
            e.Handled = true;
        }
        #endregion
    }
}
