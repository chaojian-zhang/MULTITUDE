using System;
using System.Windows;

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
