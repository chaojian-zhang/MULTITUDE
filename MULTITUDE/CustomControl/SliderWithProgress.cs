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

namespace MULTITUDE.CustomControl
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MULTITUDE.CustomControl"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:MULTITUDE.CustomControl;assembly=MULTITUDE.CustomControl"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:SliderWithProgress/>
    ///
    /// </summary>
    public class SliderWithProgress : Slider
    {
        static SliderWithProgress()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SliderWithProgress), new FrameworkPropertyMetadata(typeof(SliderWithProgress)));

            // Register dependency properties
            SliderWidthProperty = DependencyProperty.Register("SliderWidth", typeof(double), typeof(SliderWithProgress), new UIPropertyMetadata(10d));
            SliderCornerRadiusProperty = DependencyProperty.Register("SliderCornerRadius", typeof(double), typeof(SliderWithProgress), new UIPropertyMetadata(5d));
            SliderBorderColorProperty = DependencyProperty.Register("SliderBorderColor", typeof(Brush), typeof(SliderWithProgress), new UIPropertyMetadata(Brushes.Transparent));
            SliderBackgroundProperty = DependencyProperty.Register("SliderBackgroundColor", typeof(Brush), typeof(SliderWithProgress), new UIPropertyMetadata(Brushes.CadetBlue));
        }

        // Depedency property definitions
        public double SliderWidth
        {
            get { return (double)GetValue(SliderWidthProperty); }
            set { SetValue(SliderWidthProperty, value); }
        }
        public double SliderCornerRadius
        {
            get { return (double)GetValue(SliderCornerRadiusProperty); }
            set { SetValue(SliderCornerRadiusProperty, value); }
        }
        public Brush SliderBorderColor
        {
            get { return (Brush)GetValue(SliderBorderColorProperty); }
            set { SetValue(SliderBorderColorProperty, value); }
        }
        public Brush SliderBackgroundColor
        {
            get { return (Brush)GetValue(SliderBackgroundProperty); }
            set { SetValue(SliderBackgroundProperty, value); }
        }
        public static DependencyProperty SliderWidthProperty;
        public static DependencyProperty SliderCornerRadiusProperty;
        public static DependencyProperty SliderBorderColorProperty;
        public static DependencyProperty SliderBackgroundProperty;
    }
}

// Style Ref:
// 1. https://msdn.microsoft.com/fr-fr/library/ms753256(v=vs.85).aspx
// 2. https://stackoverflow.com/questions/24508167/track-bar-slider-template-for-wpf
// 3. https://stackoverflow.com/questions/383251/how-to-style-the-slider-control-in-wpf
// 4. Also see two types of thunder Player slider: one for progress bar one for volume slider
// Slider parameters: Minimum="0" Maximum="100" IsSelectionRangeEnabled="True" SelectionStart="22" SelectionEnd="65" TickPlacement="BottomRight" TickFrequency="5" Orientation="Vertical" SmallChange="1" (keyboard) LargeChange="5" (click) AutoToolTipPlacement="BottomRight" AutoToolTipPrecision="2" (shows "Value") IsSnapToTickEnabled="True" 
// Other usages (e.g. cycle through enumerated values; Auto tolltip): https://wpf.2000things.com/tag/slider/, http://www.wpf-tutorial.com/misc-controls/the-slider-control/
// Useful events: Thumb.DragCompleted, Thumb.DragStarted, click/keydown events (for manual dragging or placement), ValueChanged, PreviewMouseUp="MySlider_DragCompleted" 

// Progress bar ref:
// 1. https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/progressbar-styles-and-templates
//    https://msdn.microsoft.com/en-us/library/ms750638(v=vs.85).aspx
//    http://www.wpf-tutorial.com/misc-controls/the-progressbar-control/