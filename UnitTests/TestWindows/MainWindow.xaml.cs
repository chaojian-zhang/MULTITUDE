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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            TestWindow2 window1 = new TestWindow2();
            TestWindow2 window2 = new TestWindow2();
            window1.Owner = this;
            window2.Owner = this;
            window1.Show();
            window2.Show();
        }
    }
}
