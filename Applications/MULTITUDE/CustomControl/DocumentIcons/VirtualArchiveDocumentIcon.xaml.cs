using MULTITUDE.Class.DocumentTypes;
using System.Windows.Controls;

namespace MULTITUDE.CustomControl.DocumentIcons
{
    /// <summary>
    /// Interaction logic for VirtualArchiveDocumentIcon.xaml
    /// </summary>
    public partial class VirtualArchiveDocumentIcon : UserControl
    {
        internal VirtualArchiveDocumentIcon(Document doc)
        {
            InitializeComponent();

            this.DataContext = doc;
        }
    }
}
