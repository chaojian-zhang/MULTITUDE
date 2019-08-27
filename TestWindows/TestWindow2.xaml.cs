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

namespace TestWindows
{
    /// <summary>
    /// Interaction logic for TestWindow2.xaml
    /// </summary>
    public partial class TestWindow2 : Window
    {
        public TestWindow2()
        {
            InitializeComponent();
            Console.WriteLine(SystemColors.HighlightBrush.Color);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
