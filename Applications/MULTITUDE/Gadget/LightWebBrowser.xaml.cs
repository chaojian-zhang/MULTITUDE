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

namespace MULTITUDE.Gadget
{
    // Design goals:
    // 1. Selection search directly - automatically or context menu option, show in a small pop-up
    // 2. No initial page design, integrate with environment to eliminate that need
    // 3. Frequently used sites recording, with Full-spectrum search bar sharing info
    // 4. Memory conseravtive, light weight
    // 5/ Favorite management using documetns and summary articles. etc. instead of in-browser management

    /// <summary>
    /// Interaction logic for LightWebBrowser.xaml
    /// </summary>
    public partial class LightWebBrowser : Window
    {
        public LightWebBrowser(string navigation = null)
        {
            InitializeComponent();
            StartUpNavigation = Parse(navigation);
        }

        public string StartUpNavigation { get; set; }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.WebBrowser.Navigate(StartUpNavigation);
        }

        // Text can be keyword or complete website
        public void Navigate(string text)
        {
            // Pending handling default search engine
            this.WebBrowser.Navigate(Parse(text));
        }

        // Parse input text to proper URI
        private string Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input) == false)
            {
                Uri uriResult;
                bool bValidHTTPUrl = Uri.TryCreate(input, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (bValidHTTPUrl == true)
                {
                    return input;
                }
                else
                {
                    return @"https://www.google.ca/search?q=" + input;  // A simple google request
                }
            }
            else
            {
                return @"https://www.google.ca";
            }
        }
    }
}
