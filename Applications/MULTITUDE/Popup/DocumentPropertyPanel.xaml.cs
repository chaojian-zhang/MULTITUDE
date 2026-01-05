using MULTITUDE.Canvas;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.CustomControl;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.Popup
{
    /// <summary>
    /// Interaction logic for DocumentPropertyPanel.xaml
    /// </summary>
    public partial class DocumentPropertyPanel : Window
    {
        public DocumentPropertyPanel(Window owner)
        {
            InitializeComponent();
            Owner = owner;  // When owner is set there is no need to set TopMost for this window will always be above owner
        }

        internal Document TargetDoc { get; set; }   // Might need to turn this into a dependecy property for UI to automatically update on change
        internal void Update(Document doc, Window owner)
        {
            // Positioning: only change location if not currently visible to avoid flickering distraction
            if(this.Visibility != Visibility.Visible)
            {
                Point position = Mouse.GetPosition(owner);
                this.Left = position.X + ActualWidth / 2;
                this.Top = position.Y + ActualHeight / 2;
            }

            // Save target for later use
            TargetDoc = doc;
            // Set up parameters: For a bunch of practical reasons we didn't use data binding(because we can't.. Especially since cannot figure out a way to use data context)
            DocumentGUIDLabel.Content = doc.GUID;
            DocumentCreationDateLabel.Content = doc.CreationDate;
            DocumentTypeComboBox.SelectedValue = doc.Type;
            DocumentNameTextBox.Text = doc.Name;
            DocumentCommentTextBox.Text = doc.Comment;
            // Set up clues
            DocumentCluesTextBox.Configure(SearchMode.Clues, InterfaceOption.NormalTextField | InterfaceOption.MultilineEditing, UsageOption.Creator, new List<Document> { doc });

            // Set up preview area depending on doc type
            switch (doc.Type)
            {
                case DocumentType.PlainText:
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

            // Set up meta page
            MetaListBox.ItemsSource = null;
            MetaListBox.ItemsSource = TargetDoc.Metadata;

            this.Visibility = Visibility.Visible;
        }

        #region Basic Interface Handling
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            (Owner as VirtualWorkspaceWindow).OpenDocument(TargetDoc, false);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            (this.Owner as IDocumentPropertyViewableWindow).DeleteDocumentFromView(TargetDoc);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void AddMetaButton_Click(object sender, RoutedEventArgs e)
        {
            TargetDoc.AddMeta("new meta", "enter value here");
            MetaListBox.ItemsSource = null;
            MetaListBox.ItemsSource = TargetDoc.Metadata;
        }

        private bool bInMetaPage = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Play animation to show hiden canvas, click again to hide
            if(bInMetaPage == false)
            {
                DoubleAnimation revealWidth = new DoubleAnimation();
                revealWidth.From = 0;
                revealWidth.To = MetaPageContainerCanvas.ActualWidth;
                revealWidth.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));
                DoubleAnimation revealHeight = new DoubleAnimation();
                revealHeight.From = 0;
                revealHeight.To = MetaPageContainerCanvas.ActualHeight;
                revealHeight.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));

                // Set up meta page
                MetaListBox.ItemsSource = null;
                MetaListBox.ItemsSource = TargetDoc.Metadata;

                MetaPageDisplayCanvas.BeginAnimation(System.Windows.Controls.Canvas.WidthProperty, revealWidth);
                MetaPageDisplayCanvas.BeginAnimation(System.Windows.Controls.Canvas.HeightProperty, revealHeight);
                bInMetaPage = true;
            }
            else
            {
                DoubleAnimation hide = new DoubleAnimation();
                hide.To = 0;
                hide.Duration = new Duration(TimeSpan.Parse("0:0:0.3"));

                MetaPageDisplayCanvas.BeginAnimation(System.Windows.Controls.Canvas.WidthProperty, hide);
                MetaPageDisplayCanvas.BeginAnimation(System.Windows.Controls.Canvas.HeightProperty, hide);

                // Reflect change in main properties
                DocumentNameTextBox.Text = TargetDoc.Name;
                DocumentCommentTextBox.Text = TargetDoc.Comment;

                bInMetaPage = false;
            }
        }
        #endregion

        #region Data Editing
        private void DocumentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is gonna be complicated...
            // throw new NotImplementedException();

            // Well thinking of not supporting dynamic type change for now unless documents are compatible
        }

        private void DocumentNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TargetDoc != null)
                TargetDoc.Name = DocumentNameTextBox.Text;

            // <development> Check name already used by other documents and warn the user; Suggest using unique names for files.
        }

        private void DocumentCommentTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TargetDoc != null)
                TargetDoc.Comment = DocumentCommentTextBox.Text;
        }

        string OldText = string.Empty;
        private void Metaname_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;

            if (TargetDoc != null)
                TargetDoc.ChangeMeta(OldText, txt.Text);

            OldText = txt.Text;
        }

        private void Metaname_GotFocus(object sender, RoutedEventArgs e)
        {
            OldText = (sender as TextBox).Text;
        }

        private void Metavalue_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;

            if (TargetDoc != null)
                TargetDoc.AddMeta((txt.Tag as TextBox).Text, txt.Text);
        }
        #endregion
    }
}
