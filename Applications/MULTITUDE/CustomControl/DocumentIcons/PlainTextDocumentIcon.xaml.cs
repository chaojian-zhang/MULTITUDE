using MULTITUDE.Class.DocumentTypes;
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

namespace MULTITUDE.CustomControl.DocumentIcons
{
    /// <summary>
    /// Interaction logic for PlainTextDocumentIcon.xaml
    /// </summary>
    public partial class PlainTextDocumentIcon : UserControl
    {
        internal PlainTextDocumentIcon(Document doc)
        {
            InitializeComponent();

            PlainTextDocument = doc as PlainText;
            if (PlainTextDocument == null) throw new InvalidOperationException("Input document type is invalid.");
        }

        // Bookeeping
        PlainText PlainTextDocument;

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = (sender as TextBox);
            PlainTextDocument.Content = txt.Text;
            PlainTextDocument.RecentIndex = txt.CaretIndex;
        }

        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox txt = (sender as TextBox);
            txt.Text = PlainTextDocument.Content;

            txt.Focus();
            txt.CaretIndex = PlainTextDocument.RecentIndex;
            Keyboard.ClearFocus();
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
        }
    }
}
