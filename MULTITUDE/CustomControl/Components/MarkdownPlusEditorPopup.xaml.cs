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

namespace MULTITUDE.CustomControl.Components
{
    /// <summary>
    /// Interaction logic for MarkdownPlusEditorPopup.xaml
    /// </summary>
    public partial class MarkdownPlusEditorPopup : Window
    {
        public MarkdownPlusEditorPopup(Window owner, MarkdownPlusEditor editor)
        {
            InitializeComponent();
            ParentEditor = editor;
            Owner = owner;

            // Bind event to color picker

        }

        #region Window Handling
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        #endregion

        #region Popup Controls
        MarkdownPlusEditor ParentEditor;
        private void FontSizeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // Update text to current font size
            FontSizeTextBox.Text = ParentEditor.GetSelectionOrCaretFontSize().ToString();
        }

        private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Convert text to double
            double fontSize;
            if(Double.TryParse(FontSizeTextBox.Text, out fontSize))
            {
                // If succeed, update font size
                ParentEditor.SetSelectionOrCaretFontSize(fontSize);
            }
        }

        private void ColorPicker_SelectedColorChangedEvent(SolidColorBrush newColor)
        {
            ParentEditor.ColorText(newColor);
        }

        private void BulletedButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.BulletedParagraph();
        }

        private void NumberedButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.NumberedParagraph();
        }

        private void LatinButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.LatinParagraph();
        }

        private void PlainButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.StrikethroughText();
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UnderlineText();
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.ItalicText();
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.BoldText();
        }

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void H1Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading1FontSize, MarkdownPlusEditor.Heading1Brush);
        }

        private void H2Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading2FontSize, MarkdownPlusEditor.Heading2Brush);
        }

        private void H3Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading3FontSize, MarkdownPlusEditor.Heading3Brush);
        }

        private void H4Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading4FontSize, MarkdownPlusEditor.Heading4Brush);
        }

        private void H5Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading5FontSize, MarkdownPlusEditor.Heading5Brush);
        }

        private void H6Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading6FontSize, MarkdownPlusEditor.Heading6Brush);
        }

        private void H7Button_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.UpdateHeadingStyle(MarkdownPlusEditor.Heading7FontSize, MarkdownPlusEditor.Heading7Brush);
        }

        private void Table_MouseMove(object sender, MouseEventArgs e)
        {
            // Set visible
            if (HoverBorder.Visibility != Visibility.Visible) HoverBorder.Visibility = Visibility.Visible;

            // Set size
            Point p = e.GetPosition(HoverBorder);
            HoverBorder.Width = (p.X+8) - (p.X+8) % 16; // More than half a slot count as a complete slot
            HoverBorder.Height = (p.Y+8) - (p.Y+8) % 16;
        }

        private void Table_MouseLeave(object sender, MouseEventArgs e)
        {
            HoverBorder.Visibility = Visibility.Collapsed;
        }

        private void Table_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get coverage
            Point p = e.GetPosition(HoverBorder);
            double Width = (p.X + 8) - (p.X + 8) % 16;
            double Height = (p.Y + 8) - (p.Y + 8) % 16;
            int x = (int)Width / 16;
            int y = (int)Height / 16;
            ParentEditor.InsertTable(x, y);
        }

        private void TableHorizontalExpand_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.ExpandTableHorizontal();
        }

        private void TableVerticalExpand_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.ExpandTableVertical();
        }

        private void TableShrinkToLeft_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.ShrinkTableToLeft();
        }

        private void TableShrinkToTop_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.ShrinkTableToTop();
        }

        private void ExportToText_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FloatingNote_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.AddFloatingNote();
        }

        private void FloatingImage_Click(object sender, RoutedEventArgs e)
        {
            ParentEditor.AddFloatingImage();
        }
        #endregion

        Point panelLocation;
        private void MinimizePanel_Click(object sender, RoutedEventArgs e)
        {
            switch (FormattingPanel.Visibility)
            {
                case Visibility.Visible:
                    double originalTop = this.Top;
                    double originalLeft = this.Left;
                    panelLocation = ControlsPanel.TransformToVisual(this).Transform(new Point(0, 0));
                    FormattingPanel.Visibility = Visibility.Collapsed;
                    MinimizePanelButton.Content = "+";
                    this.Top = originalTop + panelLocation.Y;
                    this.Left = originalLeft + panelLocation.X;
                    break;
                case Visibility.Hidden:
                case Visibility.Collapsed:
                    FormattingPanel.Visibility = Visibility.Visible;
                    MinimizePanelButton.Content = "-";
                    this.Top -= panelLocation.Y;
                    this.Left -= panelLocation.X;
                    break;
            }
        }
    }
}
