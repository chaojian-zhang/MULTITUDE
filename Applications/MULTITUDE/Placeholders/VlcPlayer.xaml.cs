using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.Placeholders
{
    public enum VlcMediaState
    {
        Ended,
        Paused,
        Stopped,
        Playing,
        NothingSpecial
    }
    public struct VlcEventArgs
    {
        public VlcMediaState Value { get; set; }
    }
    /// <summary>
    /// Interaction logic for VlcPlayer.xaml
    /// </summary>
    public partial class VlcPlayer : UserControl, IDisposable
    {
        public VlcPlayer()
        {
            InitializeComponent();
        }

        public VlcMediaState State { get; set; }
        public int Volume { get; set; }
        public double Position { get; set; }
        public DateTime Time { get; set; }
        public double Length { get; set; }

        public event Action<object, VlcEventArgs> StateChanged;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        internal void LoadMedia(string path)
        {
            throw new NotImplementedException();
        }

        internal void Pause()
        {
            throw new NotImplementedException();
        }

        internal void Play()
        {
            throw new NotImplementedException();
        }

        internal void Resume()
        {
            throw new NotImplementedException();
        }

        internal void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
