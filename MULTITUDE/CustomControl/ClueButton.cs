using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MULTITUDE.CustomControl
{
    /// <summary>
    /// Defines a button that have round corner shape with a round corner text label
    /// </summary>
    public class ClueButton : Button
    {
        static ClueButton()
        {
            // Override Style
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ClueButton), new FrameworkPropertyMetadata(typeof(ClueButton)));

            // Register dependency properties
            SuffixLabelContentProperty = DependencyProperty.Register("SuffixLabelContent", typeof(string), typeof(ClueButton), new UIPropertyMetadata("0"));
            PrefixLabelContentProperty = DependencyProperty.Register("PrefixLabelContent", typeof(string), typeof(ClueButton), new UIPropertyMetadata("0"));
            LabelForegroundProperty = DependencyProperty.Register("LabelForeground", typeof(Brush), typeof(ClueButton));
            LabelBackgroundProperty = DependencyProperty.Register("LabelBackground", typeof(Brush), typeof(ClueButton));
            LabelFontSizeProperty = DependencyProperty.Register("LabelFontSize", typeof(double), typeof(ClueButton), new UIPropertyMetadata(0d));
        }

        // Depedency property definitions
        /// <summary>
        /// A label description for the button, used for displaying numbers
        /// </summary>
        public string SuffixLabelContent
        {
            get { return (string)GetValue(SuffixLabelContentProperty); }
            set { SetValue(SuffixLabelContentProperty, value); }
        }
        public string PrefixLabelContent
        {
            get { return (string)GetValue(PrefixLabelContentProperty); }
            set { SetValue(PrefixLabelContentProperty, value); }
        }
        public Brush LabelForeground
        {
            get { return (Brush)GetValue(LabelForegroundProperty); }
            set { SetValue(LabelForegroundProperty, value); }
        }
        public Brush LabelBackground
        {
            get { return (Brush)GetValue(LabelBackgroundProperty); }
            set { SetValue(LabelBackgroundProperty, value); }
        }
        public double LabelFontSize
        {
            get { return (double)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }
        public static DependencyProperty SuffixLabelContentProperty;
        public static DependencyProperty PrefixLabelContentProperty;
        public static DependencyProperty LabelForegroundProperty;
        public static DependencyProperty LabelBackgroundProperty;
        public static DependencyProperty LabelFontSizeProperty;
    }
}
