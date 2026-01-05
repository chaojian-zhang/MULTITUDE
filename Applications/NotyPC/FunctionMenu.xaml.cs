using System.Windows;
using System.Windows.Controls;

namespace NotyPC
{
    /// <summary>
    /// Interaction logic for FunctionMenu.xaml
    /// </summary>
    public partial class FunctionMenu : Window
    {
        public FunctionMenu()
        {
            InitializeComponent();
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            // Store information
            App.username = usernameBox.Text;
            App.password = passwordBox.Password;

            // Create Window
            (new DownloadWindow()).Show();
            this.Close();
        }


        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            // Store information
            App.username = usernameBox.Text;
            App.password = passwordBox.Password;

            // Create Window
            (new UploadWindow()).Show();
            this.Close();
        }

        private void usernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (usernameBox.Text != "" && passwordBox.Password != "")
            {
                btnUpload.IsEnabled = true;
                btnDownload.IsEnabled = true;
            }
            else
            {
                btnUpload.IsEnabled = false;
                btnDownload.IsEnabled = false;
            }
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (usernameBox.Text != "" && passwordBox.Password != "")
            {
                btnUpload.IsEnabled = true;
                btnDownload.IsEnabled = true;
            }
            else
            {
                btnUpload.IsEnabled = false;
                btnDownload.IsEnabled = false;
            }
        }

        private void btnMagicExplorer_Click(object sender, RoutedEventArgs e)
        {
            // Create Window
            (new MagicWindow()).Show();
            this.Close();
        }
    }
}
