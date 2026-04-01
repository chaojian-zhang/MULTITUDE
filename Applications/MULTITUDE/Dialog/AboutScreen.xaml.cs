using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MULTITUDE.Dialog
{
    /// <summary>
    /// Interaction logic for AboutScreen.xaml
    /// </summary>
    public partial class AboutScreen : Window
    {
        public AboutScreen(Window owner)
        {
            InitializeComponent();
            Owner = owner;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Play shining animation
            // Display current "like" info from server
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Close window
            DialogResult = true;
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            // Open a new process
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;

            // Or better, use our internal Lightning browser
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void HeartLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Play small +1 animation
            // Send info to server
        }
    }
}
