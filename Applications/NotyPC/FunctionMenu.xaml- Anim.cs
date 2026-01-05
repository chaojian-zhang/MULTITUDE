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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE
{
    /// <summary>
    /// Interaction logic for FunctionMenu.xaml
    /// </summary>
    public partial class FunctionMenu : Window
    {
        public FunctionMenu()
        {
            InitializeComponent();

            // http://stackoverflow.com/questions/7620488/how-to-set-the-location-of-wpf-window-to-the-bottom-right-corner-of-desktop
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            // this.Left = desktopWorkingArea.Left / 2 - this.Width;
            // this.Top = desktopWorkingArea.Top / 2 - this.Height;
            this.Top = 250;
            this.Left = 1024;

            // http://stackoverflow.com/questions/10425654/how-to-change-easingdoublekeyframe-value-at-runtime
            // http://mitesh487.blogspot.ca/2011/11/binding-animation-keyframe-values-in.html
            // https://msdn.microsoft.com/en-us/library/ms742524(v=vs.110).aspx
            // https://www.codeproject.com/Questions/274870/How-to-change-the-value-Storyboard-dynamically-by
        }

        //private enum ShowWindow
        //{
        //    DownloadWindow,
        //    UploadWindow
        //}
        //private ShowWindow showWindow;
        private Window nextWindow;
        // Create window when click the button instead of after animation is finished to make it look smooth

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Store information
            App.username = usernameBox.Text;
            App.password = passwordBox.Password;

            // Create Window
            nextWindow = new DownloadWindow();

            // Play Reverse Animation
            Storyboard sb = this.FindResource("WindowExpand_Rev") as Storyboard;
            sb.Begin();
        }

        // ANother way to play animation in reverse:
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/ac54de71-f750-4940-91a2-231810308727/play-animation-in-reverse?forum=wpf
        // To wait for storyboard to finish: http://stackoverflow.com/questions/14690960/wpf-what-is-the-correct-way-to-wait-for-a-storyboard-animation-to-complete-befo, http://stackoverflow.com/questions/24332479/wpf-waiting-for-animation-to-end-before-handling-event

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            // Store information
            App.username = usernameBox.Text;
            App.password = passwordBox.Password;

            // Create Window
            nextWindow = new UploadWindow();

            // Play Reverse Animation
            Storyboard sb = this.FindResource("WindowExpand_Rev") as Storyboard;
            sb.Begin();
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            // Show Window
            nextWindow.Show();
            this.Close();
        }
    }
}
