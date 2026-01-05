using MULTITUDE.Class.DocumentTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MULTITUDE.CustomControl
{
    /// <summary>
    /// Interaction logic for ForgottenDocument.xaml
    /// </summary>
    public partial class ForgottenDocument : UserControl
    {
        public ForgottenDocument()
        {
            InitializeComponent();
        }

        private UIElement StaticUI { get; set; }
        private UIElement DynamicUI { get; set; }

        // Take form of appraopriate document
        internal void TakeFormOf(Document doc)
        {
            switch (doc.Type)
            {
                case DocumentType.PlainText:
                    // Extract Content
                    Label label = new();
                    string docContent = (doc as PlainText).Content;
                    if (docContent.Length > 50) label.Content = docContent.Substring(0, 50) + "...";
                    else label.Content = docContent;

                    //StaticUI
                    //PresentationGrid.Children.Add();
                    break;
                case DocumentType.MarkdownPlus:
                    break;
                case DocumentType.Archive:
                    break;
                case DocumentType.VirtualArchive:
                    break;
                case DocumentType.DataCollection:
                    break;
                case DocumentType.Graph:
                    break;
                case DocumentType.Command:
                    break;
                case DocumentType.Web:
                    break;
                case DocumentType.PlayList:
                    break;
                case DocumentType.ImagePlus:
                    break;
                case DocumentType.Sound:
                    break;
                case DocumentType.Video:
                    break;
                case DocumentType.Others:
                    break;
                case DocumentType.Unkown:
                    break;
                default:
                    break;
            }
        }

        private void PresentationGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            // Stop flowing and show dynamic content
        }

        private void PresentationGrid_MouseLeave(object sender, MouseEventArgs e)
        {

        }
    }
}
