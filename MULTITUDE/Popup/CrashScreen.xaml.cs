using MULTITUDE.Class.Facility;
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
    /// Interaction logic for CrashScreen.xaml
    /// </summary>
    public partial class CrashScreen : Window, INotifyPropertyChanged
    {
        public CrashScreen(Exception exception)
        {
            Exception = exception;
            InitializeComponent();
        }
        private Exception _Exception;

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Exception Exception
        {
            get { return _Exception; }
            set
            {
                if (value != this._Exception)
                {
                    this._Exception = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("StackTrace");
                    NotifyPropertyChanged("Message");
                    NotifyPropertyChanged("Source");
                }
            }
        }
        public string StackTrace
        {
            get { return Exception.StackTrace; }
        }
        public string Message
        {
            get { return Exception.Message; }
        }
        public string Source
        {
            get { return Exception.Source; }
        }
        #endregion

        private void SendReport_Click(object sender, RoutedEventArgs e)
        {
            // Send request
            NetworkHelper.SendEmailReportAsync(string.Empty,
                string.Format("Exception Message: {0}\nException Stack Trace: {1}\nUser Message: {2}\n User Contact: {3}", Message, StackTrace, UserMessage.Text, UserContact.Text),
                MessageType.ExceptionReport, SendCompletedCallback);
            // Show animation/effect
            FeedbackPage.Visibility = Visibility.Visible;
        }

        private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            // If failed
            if(e == null && sender is Exception)
            {
                FeedbackLabel.Content = "Failed.";
                System.Threading.Thread.Sleep(1000);
            }

            // Otherwise succeed
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
