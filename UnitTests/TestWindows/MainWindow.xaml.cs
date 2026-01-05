using System.Windows;

namespace TestWindows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Console.WriteLine("Hello");
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            TestWindow2 window1 = new();
            TestWindow2 window2 = new();
            window1.Owner = this;
            window2.Owner = this;
            window1.Show();
            window2.Show();
        }
    }
}
