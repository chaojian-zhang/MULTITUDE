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
using MULTITUDE.Class.DocumentTypes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MULTITUDE.Canvas;

namespace MULTITUDE.CustomControl.CanvasSpaceWindow
{
    public enum ContentMode
    {
        FlowDocument,
        TableOnly
    }

    /// <summary>
    /// Interaction logic for MarkdownPlusEditorWindow.xaml
    /// </summary>
    public partial class MarkdownPlusEditorWindow : Window, INotifyPropertyChanged
    {
        #region Initialization and Destruction
        public MarkdownPlusEditorWindow(Window owner, ContentMode mode = ContentMode.FlowDocument)
        {
            InitializeComponent();
            Owner = owner;
            ContentMode = mode;
        }

        internal void Setup(Document doc = null)
        {
            // If doc is null, create one
            if (doc == null)
            {
                if (CurrentDocument != null) doc = CurrentDocument;   // Use the document we used last time
                else
                {
                    switch (ContentMode)
                    {
                        case ContentMode.FlowDocument:
                            doc = MULTITUDE.Class.Home.Current.CreateDocument(DocumentType.MarkdownPlus, "Unnamed Document");
                            break;
                        case ContentMode.TableOnly:
                            doc = MULTITUDE.Class.Home.Current.CreateDocument(DocumentType.DataCollection, "Unnamed Table");
                            break;
                        default:
                            break;
                    }
                }
            }

            // Update UI
            Document = doc;
            CurrentDocument = doc;
            NotifyPropertyChanged("DocumentName");
            // Update UI for Table Collection
            if (CurrentDocument is DataCollection) UseTableUI();
            // Update Editor
            MarkdownRichTextEditor.Update(doc, false, ContentMode);
            MarkdownRichTextEditor.Focus();
        }
        Document Document;

        private void UseTableUI()
        {
            // MD+ Editor Width
            DataCollection collection = CurrentDocument as DataCollection;
            const double UIDesirableUnitSize = 256;
            double desirableWidth = collection.GetDesirableUnits() * UIDesirableUnitSize;
            MarkdownRichTextEditor.Width = desirableWidth > MarkdownRichTextEditor.Width ? MarkdownRichTextEditor.Width : desirableWidth ;
            // Sizing
            MarkdownRichTextEditor.Margin = new Thickness(20, 80, 20, 10);
            // Background and Effects
            MarkdownRichTextEditor.Background = Brushes.Transparent;
        }

        private void CommitFlowDocumentContent()
        {
            switch (Document.Type)
            {
                case DocumentType.MarkdownPlus:
                    (Document as MarkdownPlus).SetFlowDocument(MarkdownRichTextEditor.Document);
                    break;
                case DocumentType.DataCollection:
                    (Document as DataCollection).RefreshTableData(MarkdownRichTextEditor.Document);
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MarkdownRichTextEditor.Close();
            CommitFlowDocumentContent();
            (Owner as VirtualWorkspaceWindow).RestoreCanvasSpace();
        }
        #endregion

        #region Bookeeping Data
        private ContentMode ContentMode;    
        private static Document CurrentDocument = null; // The previously opened/created document while target is null; The document is saved and edited as usual but kept as a state of editor per MULTITUDE session
        #endregion

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string DocumentName
        {
            get { return (CurrentDocument == null)? null : CurrentDocument.Name; }    // Can be null udring initliazation
            set
            {
                if (value != CurrentDocument.Name)
                {
                    CurrentDocument.Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
