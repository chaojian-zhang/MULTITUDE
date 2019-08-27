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
    /// Defines a custom button that have a fan shape with custom properties and support rotation
    /// 
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
    ///     <MyNamespace:FanButton/>
    ///
    /// </summary>
    public class FanButton : Button
    {
        static FanButton()
        {
            // Override Style
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FanButton), new FrameworkPropertyMetadata(typeof(FanButton)));

            // Register dependency properties
            AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(FanButton), new UIPropertyMetadata(90d));
            MaxRadiusProperty = DependencyProperty.Register("MaxRadius", typeof(double), typeof(FanButton), new UIPropertyMetadata(80d));
            MinRadiusProperty = DependencyProperty.Register("MinRadius", typeof(double), typeof(FanButton), new UIPropertyMetadata(55d));
            RotationProperty = DependencyProperty.Register("Rotation", typeof(double), typeof(FanButton), new UIPropertyMetadata(0d));

            // <Development> Text location control: Ideally we don't use this, see converter implementation comments
            xControlProperty = DependencyProperty.Register("xControl", typeof(double), typeof(FanButton), new UIPropertyMetadata(0d));
            yControlProperty = DependencyProperty.Register("yControl", typeof(double), typeof(FanButton), new UIPropertyMetadata(0d));
        }

        // Depedency property definitions
        /// <summary>
        /// Spread angle of button, in degress
        /// </summary>
        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }
        /// <summary>
        /// Furthest edge distance in pixel
        /// </summary>
        public double MaxRadius
        {
            get { return (double)GetValue(MaxRadiusProperty); }
            set { SetValue(MaxRadiusProperty, value); }
        }
        /// <summary>
        /// Shortest edge distance in pixel
        /// </summary>
        public double MinRadius
        {
            get { return (double)GetValue(MinRadiusProperty); }
            set { SetValue(MinRadiusProperty, value); }
        }
        /// <summary>
        /// Overall rotation of shape, text inside not influenced, clockwise
        /// </summary>
        public double Rotation
        {
            get { return (double)GetValue(RotationProperty); }
            set { SetValue(RotationProperty, value); }
        }
        public static DependencyProperty AngleProperty;
        public static DependencyProperty MaxRadiusProperty;
        public static DependencyProperty MinRadiusProperty;
        public static DependencyProperty RotationProperty;

        // <Development> Text location control: Ideally we don't use this, see converter implementation comments
        public double xControl
        {
            get { return (double)GetValue(xControlProperty); }
            set { SetValue(xControlProperty, value); }
        }
        public double yControl
        {
            get { return (double)GetValue(yControlProperty); }
            set { SetValue(yControlProperty, value); }
        }
        public static DependencyProperty xControlProperty;
        public static DependencyProperty yControlProperty;
    }
}
