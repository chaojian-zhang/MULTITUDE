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

namespace MULTITUDE.Popup
{
    public enum StatusEnum
    {
        Event,  // Show 7 seconds
        Notice, // Brief form of event, show only 2 seconds
        TimerEvent,
        Welcome,
        Error
    }

    public enum ActionsEnum
    {
        AcceptCancel,
        YesNo,
        Acknowledge,
        None
    }

    public enum PlacementEnum
    {
        UpperRight,
        LowerRight
    }

    /// <summary>
    /// A Paragon like promt appearing in corner of application showing updates and optionally disappear after some time
    /// The promt will be always on top
    /// We support three kinds of updates: List view, Simple string with image, delegate actions, progress circle with string (a form of string + animation but not generalized for other animations)
    /// Currently used for: Moving files status, loading user home status, loading VW status, automatic saving status
    /// </summary>
    public partial class StatusPromt : Window
    {
        public StatusPromt(Window owner)
        {
            InitializeComponent();
            Owner = owner;

            // Setup timer
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();  // In WPF don't use Threading.Timer
            dispatcherTimer.Tick += DispatcherTimer_TimeUp;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 7);
        }

        // State information
        private StatusEnum Status { get; set; }
        private String Text { get; set; }
        private ActionsEnum Action { get; set; }
        // Callback
        public delegate void ActionCallBack(bool bChoice);
        private ActionCallBack CallBack;

        // Update content, play animation, play sound, display for a while, then automatically hide
        // Callback only used when there are two buttons
        // Might also consider adding a log, especially for errors
        public void Update(StatusEnum status, string text, ActionsEnum action, PlacementEnum placement, ActionCallBack callback = null)
        {
            // Save parameters
            Status = status;
            Text = text;
            Action = action;
            CallBack = callback;

            // Reconfigure timer
            dispatcherTimer.Interval = new TimeSpan(0, 0, 7);

            // Update Content
            StatusLabel.Content = Status.ToString();    // Update status label
            switch (Status)   // Update status icon
            {
                case StatusEnum.Event:
                    // StatusIcon.Source = ...
                    break;
                case StatusEnum.TimerEvent:
                    // StatusIcon.Source = ...
                    break;
                case StatusEnum.Notice:
                    // StatusIcon.Source = ...
                    dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
                    break;
                case StatusEnum.Welcome:
                    // StatusIcon.Source = ...
                    break;
                case StatusEnum.Error:
                    // StatusIcon.Source = ...
                    break;
                default:
                    break;
            }
            StatusTextBlock.Text = Text;    // Update status text
            switch (Action) // Update buttons
            {
                case ActionsEnum.AcceptCancel:
                    LeftButton.Content = "Accept";
                    LeftButton.Visibility = Visibility.Visible;
                    RightButton.Content = "Cancel";
                    RightButton.Visibility = Visibility.Visible;
                    CenterButton.Visibility = Visibility.Hidden;
                    break;
                case ActionsEnum.YesNo:
                    LeftButton.Content = "Yes";
                    LeftButton.Visibility = Visibility.Visible;
                    RightButton.Content = "No";
                    RightButton.Visibility = Visibility.Visible;
                    CenterButton.Visibility = Visibility.Hidden;
                    break;
                case ActionsEnum.Acknowledge:
                    LeftButton.Visibility = Visibility.Hidden;
                    RightButton.Visibility = Visibility.Hidden;
                    CenterButton.Content = "Ackownledge";
                    CenterButton.Visibility = Visibility.Visible;
                    break;
                case ActionsEnum.None:
                    LeftButton.Visibility = Visibility.Hidden;
                    RightButton.Visibility = Visibility.Hidden;
                    CenterButton.Visibility = Visibility.Hidden;
                    break;
                default:
                    break;
            }

            // Play blink animation
            // ...

            // Play notif souond
            // ...

            // Restart timer
            dispatcherTimer.Stop();
            dispatcherTimer.Start();

            // Set window location
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            switch (placement)
            {
                case PlacementEnum.UpperRight:
                    this.Left = desktopWorkingArea.Right - this.Width - 50;
                    this.Top = 50;
                    break;
                case PlacementEnum.LowerRight:
                    this.Left = desktopWorkingArea.Right - this.Width - 50;
                    this.Top = desktopWorkingArea.Bottom - this.ActualHeight - 80;
                    break;
                default:
                    break;
            }

            // Display Contents
            this.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Display simple text as an event 
        /// </summary>
        /// <param name="text"></param>
        public void Update(string text)
        {
            Update(StatusEnum.Event, text, ActionsEnum.None, PlacementEnum.UpperRight);
        }

        /// <summary>
        /// Display simple text as a notice (for 2 seconds) 
        /// </summary>
        /// <param name="text"></param>
        public void UpdateNotice(string text)
        {
            Update(StatusEnum.Notice, text, ActionsEnum.None, PlacementEnum.UpperRight);
        }

        // Reuse timer, though not sure whether Stop() disposes the timer - what if we do want to dispose it?
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private void DispatcherTimer_TimeUp(object sender, EventArgs e)
        {
            // Stop timer
            dispatcherTimer.Stop();
            // Hide window
            this.Hide();
        }


        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            e.Handled = true;
            CallBack(true);
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            e.Handled = true;
            CallBack(false);
        }

        private void CenterButton_Click(object sender, RoutedEventArgs e)
        {
            // Do nothing, but hide window
            this.Hide();
            e.Handled = true;
        }

        #region Non-Distractive Hidding Handling
        private bool bAltF4Pressed = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Only hide, not actually closed by user actions
            if (bAltF4Pressed)
            {
                e.Cancel = true;
                bAltF4Pressed = false;
                this.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Alt-F4 handling for hidding window
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4) bAltF4Pressed = true;
            else bAltF4Pressed = false;

            // ESC for hidding window
            if (e.Key == Key.Escape) this.Hide();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
        }
        #endregion
    }
}
