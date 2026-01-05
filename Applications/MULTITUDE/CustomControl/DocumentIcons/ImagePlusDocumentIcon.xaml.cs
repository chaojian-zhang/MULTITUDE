using MULTITUDE.Class.DocumentTypes;
using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MULTITUDE.CustomControl.DocumentIcons
{
    /// <summary>
    /// Interaction logic for ImagePlusDocumentIcon.xaml
    /// </summary>
    public partial class ImagePlusDocumentIcon : UserControl
    {
        internal ImagePlusDocumentIcon(Document doc)
        {
            InitializeComponent();

            // Just show it
            if (doc.Type != DocumentType.ImagePlus) throw new InvalidCastException("Only images can be viewed using image document icons.");
            DocumentImage.Source = new BitmapImage(new Uri(doc.Path));
        }
    }
}
