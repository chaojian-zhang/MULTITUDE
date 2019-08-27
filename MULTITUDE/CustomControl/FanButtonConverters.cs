using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MULTITUDE.CustomControl
{
    // FanButton: Origin at center, shapes drawing relative to that origin, spread out; doesn't follow rectangular area definitions

    public class ValueToIsLargeArcConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double Angle = (double)value;
            return Angle > 180;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    class RadiusToSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double Radius = (double)values[0];

            return new Size(Radius, Radius);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    class AngleToLeftPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double Angle = (double)values[0];
            double Radius = (double)values[1];

            // Get triangular angle in radian
            double rAngle = (Angle / 2d) * 0.017453292519943295;
            // Calculate the left point distance from x and y axis
            double dx = Math.Sin(rAngle) * Radius;
            double dy = Math.Cos(rAngle) * Radius;

            // Return actual point location, where dx is positive and dy is negative
            return new Point(-dx, -dy);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    class AngleToRightPointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double Angle = (double)values[0];
            double Radius = (double)values[1];

            // Get triangular angle in radian
            double rAngle = (Angle / 2d) * 0.017453292519943295;
            // Calculate the left point distance from x and y axis
            double dx = Math.Sin(rAngle) * Radius;
            double dy = Math.Cos(rAngle) * Radius;

            // Return actual point location, where dx is positive and dy is negative
            return new Point(dx, -dy);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    // Ideally we find the biggest square inside a fan despite its orientation, so a text can be placed in a non-distracting way
    // Current implementation lets the sqaure rotate with shape
    //class AngleToLocationConverterX : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        double Angle = (double)values[0];
    //        double MaxRadius = (double)values[1];
    //        double MinRadius = (double)values[2];
    //        double Rotation = (double)values[3];
    //        double WidthOffset = (double)values[4] / 2;

    //        // Get triangular angle in radian
    //        double rCenterAngle_RightEdge = (-Rotation + 90 + Angle / 2d) * 0.017453292519943295;
    //        double rCenterAngle_LeftEdge = (-Rotation + 90 - Angle / 2d) * 0.017453292519943295;
    //        // Calculate the point distances from x and y axis
    //        double RTx = Math.Cos(rCenterAngle_RightEdge) * MaxRadius;
    //        double RBx = Math.Cos(rCenterAngle_RightEdge) * MinRadius;
    //        double LTx = Math.Cos(rCenterAngle_LeftEdge) * MaxRadius;
    //        double LBx = Math.Cos(rCenterAngle_LeftEdge) * MinRadius;
    //        double[] Xs = { RTx, RBx, LTx, LBx };
    //        // Find minimum X and Y
    //        double minX = Xs.Min();
    //        // FInd maximum X and Y
    //        double maxX = Xs.Max();
    //        // Get and return a proper location
    //        return (minX + maxX) / 2;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    //class AngleToLocationConverterY : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        double Angle = (double)values[0];
    //        double MaxRadius = (double)values[1];
    //        double MinRadius = (double)values[2];
    //        double Rotation = (double)values[3];
    //        double HeightOffset = (double)values[4] / 2;

    //        // Get triangular angle in radian
    //        double rCenterAngle_RightEdge = (-Rotation + 90 + Angle / 2d) * 0.017453292519943295;
    //        double rCenterAngle_LeftEdge = (-Rotation + 90 - Angle / 2d) * 0.017453292519943295;
    //        // Calculate the point distances from x and y axis
    //        double RTy = Math.Sin(rCenterAngle_RightEdge) * MaxRadius;
    //        double RBy = Math.Sin(rCenterAngle_RightEdge) * MinRadius;
    //        double LTy = Math.Sin(rCenterAngle_LeftEdge) * MaxRadius;
    //        double LBy = Math.Sin(rCenterAngle_LeftEdge) * MinRadius;
    //        double[] Ys = { RTy, RBy, LTy, LBy };
    //        // Find minimum Y
    //        double minY = Ys.Min();
    //        // Find maximum Y
    //        double maxY = Ys.Max();
    //        // Get and return a proper location
    //        return -(maxY + maxY)/2;
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        throw new NotSupportedException();
    //    }
    //}

    public class WidthToOriginX : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double Width = (double)value;
            return Width/2;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    
    public class HeightToOriginY : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double Height = (double)value;
            return Height/2;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}