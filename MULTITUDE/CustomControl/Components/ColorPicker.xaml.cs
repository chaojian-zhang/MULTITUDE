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

namespace MULTITUDE.CustomControl.Components
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public ColorPicker()
        {
            InitializeComponent();
            CurrentColor = new SolidColorBrush();
        }

        public SolidColorBrush CurrentColor { get; set; }

        private void UpdateCurrent(SolidColorBrush brush)
        {
            CurrentColor = brush;
        }

        private void ColorImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = Mouse.GetPosition(ColorImage);
            BitmapSource image = ColorImage.Source as BitmapSource;
            CroppedBitmap cb = new CroppedBitmap(image,
                new Int32Rect((int)(position.X / ColorImage.ActualWidth * image.PixelWidth), (int)(position.Y / ColorImage.ActualHeight * image.PixelHeight), 1, 1));
            byte[] pixels = new byte[4];
            cb.CopyPixels(pixels, 4, 0);

            Color SelectedColor = Color.FromArgb(255, pixels[2], pixels[1], pixels[0]);
            CurrentColor.Color = SelectedColor;

            // Update labels
            UpdateLabels();

            if (pixels[3] != 0)
                e.Handled = true;
        }

        private void ColorImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            GetColorUnderCursor();

            e.Handled = true;
        }

        private void GetColorUnderCursor()
        {
            Point position = Mouse.GetPosition(ColorImage);
            BitmapSource image = ColorImage.Source as BitmapSource;
            CroppedBitmap cb = new CroppedBitmap(image,
                new Int32Rect((int)(position.X / ColorImage.ActualWidth * image.PixelWidth), (int)(position.Y / ColorImage.ActualHeight * image.PixelHeight), 1, 1));
            byte[] pixels = new byte[4];
            cb.CopyPixels(pixels, 4, 0);

            Color SelectedColor = Color.FromArgb(255, pixels[2], pixels[1], pixels[0]);
            CurrentColor.Color = SelectedColor;

            // Update labels
            UpdateLabels();

            // Inform Listener
            SelectedColorChangedEvent.Invoke(CurrentColor);
        }

        public delegate void SelectedColorChangedEventHandler(SolidColorBrush newColor);
        public event SelectedColorChangedEventHandler SelectedColorChangedEvent;

        private void UpdateLabels()
        {
            RedLabel.Content = "Red: " + CurrentColor.Color.R.ToString();
            GreenLabel.Content = "Green: " + CurrentColor.Color.G.ToString();
            BlueLabel.Content = "Blue: " + CurrentColor.Color.B.ToString();
            // AlphaLabel.Content = "Alpha: " + CurrentColor.Color.A.ToString();

            SampleColor.Background = CurrentColor;
        }
    }
}
