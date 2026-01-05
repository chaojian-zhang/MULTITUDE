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
    /// Interaction logic for MarkdownPlusDocumentIcon.xaml
    /// </summary>
    public partial class MarkdownPlusDocumentIcon : UserControl
    {
        internal MarkdownPlusDocumentIcon(Document doc, MULTITUDE.Class.DocumentIcon iconInfo)
        {
            InitializeComponent();
            IconInfo = iconInfo;
            Document = doc;
            MDEditor.Update(doc);
            if (iconInfo.MDHideBackground == true) HideBackground();
        }
        MULTITUDE.Class.DocumentIcon IconInfo;
        Document Document;

        #region Interface Display
        Brush BackgroundBorderBackground;
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            IconInfo.MDHideBackground = true;
            HideBackground();
        }
        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            IconInfo.MDHideBackground = false;
            ShowBackground();
        }
        private void HideBackground()
        {
            MDEditor.Background.Opacity = 0;
            BackgroundBorderBackground = BackgroundBorder.Background;
            BackgroundBorder.Background = Brushes.Transparent;
        }
        private void ShowBackground()
        {
            MDEditor.Background.Opacity = 1;
            BackgroundBorder.Background = BackgroundBorderBackground;
        }
        #endregion

        private void MDEditor_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;   // So to avoid translation in VW
        }

        private void MDEditor_GotFocus(object sender, RoutedEventArgs e)
        {
            // Also document might have been changed so update it
            SynchronizeFlowDocumentContent();
        }
        
        private void SynchronizeFlowDocumentContent()
        {
            if (Document == null) throw new ArgumentException("Document is null.");

            switch (Document.Type)
            {
                case DocumentType.MarkdownPlus:
                    MarkdownPlus mdDocument = Document as MarkdownPlus;
                    if (mdDocument.FlowDocumentHasChangedSince(MDEditor.Document) == true) MDEditor.Update(mdDocument.GetFlowDocument());
                    break;
                case DocumentType.DataCollection:
                    DataCollection dcDocument = Document as DataCollection;
                    if (dcDocument.TableDataHasChangedSince(MDEditor.Document) == true) MDEditor.Update(dcDocument.GenerateFlowDocument());
                    break;
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            // Save
            (Document as MarkdownPlus).SetFlowDocument(MDEditor.Document);
        }
    }
}
