using System;
using System.Windows;
using System.Windows.Data;

namespace TestWindows
{
    /// <summary>
    /// Interaction logic for TextFlowAnimation.xaml
    /// </summary>
    public partial class TextFlowAnimation : Window
    {
        public TextFlowAnimation()
        {
            InitializeComponent();
        }


    }

    public class NegatingConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                return -((double)value);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double)
            {
                return +(double)value;
            }
            return value;
        }
    }
}
