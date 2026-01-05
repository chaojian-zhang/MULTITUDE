using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using MULTITUDE.Class.Facility.ClueManagement;
using MULTITUDE.Dialog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace MULTITUDE.CustomControl.CanvasSpaces
{
    /// <summary>
    /// Interaction logic for ClueBrowser.xaml
    /// </summary>
    public partial class ClueBrowser : UserControl, INotifyPropertyChanged
    {
        public ClueBrowser()
        {
            InitializeComponent();

            // Data Initialization
            AddedDocuments = new List<Document>();
            AddedClues = new ObservableCollection<string>();

            // Configura search bars
            AliasMemoirBar.Configure(SearchMode.Clues, InterfaceOption.NormalTextField, UsageOption.Creator);
            TopSearchBar.Configure(SearchMode.General, InterfaceOption.ShowValidationSymbol | InterfaceOption.ShowEnterTextHere | InterfaceOption.ShowDocumentPreview, UsageOption.Searcher);
        }

        // Data Bindings (not used)
        //internal SortedSet<string> Clues { get; set; }
        //private string _SelectedPrimaryClue;
        //internal string SelectedPrimaryClue {
        //    get { return _SelectedPrimaryClue; }
        //    set
        //    {
        //        _SelectedPrimaryClue = value;
        //        // Set expandedSecondary...
        //    }
        //}
        //internal List<ClueFragment> ExpandedSecondaryClue { get; set; }

        // Interface
        internal void Update(Home home)
        {
            PrimaryClues = ClueManager.Manager.GetPrimaryClueInfo();    // Seems this is observable so automatically update add event, e.g. during creating alias?
            // It is said we could give the user control a name then use ItemsSource="{Binding Clues, ElementName=ClueBrowserUserControl}" but not working
            // The same for ItemsSource="{Binding ExpandedSecondaryClue, ElementName=ClueBrowserUserControl}" for ExpandedSecondaryClueItemsControl so we did it procedurally

            Home = home;
        }
        Home Home;

        #region Clue Space Navigation
        private List<PrimaryClueInfo> prevSearchResults = null;
        private int prevIndex;
        private void ClueFilter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.F3)
            {
                CycleSearchResults();
                e.Handled = true;
            }
        }
        private void CycleSearchResults()
        {
            if(prevSearchResults != null && prevSearchResults.Count != 0)
            {
                if(prevIndex == -1) prevIndex = 0;
                else
                {
                    prevSearchResults[prevIndex].IsSelected = false;
                    if (prevSearchResults[prevIndex].Parent != null) prevSearchResults[prevIndex].Parent.IsExpanded = false;

                    prevIndex++;
                    if (prevIndex == prevSearchResults.Count) prevIndex = 0;
                }

                // Check
                prevSearchResults[prevIndex].IsSelected = true;
                if (prevSearchResults[prevIndex].Parent != null) prevSearchResults[prevIndex].Parent.IsExpanded = true;
            }
        }
        private async void ClueFiler_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(PrimaryClues != null && PrimaryClues.Count != 0) // Can be null during initialization
            {
                string text = (sender as TextBox).Text;
                List<PrimaryClueInfo> found = await Task.Run(() => SearchMatchingClue(text));
                if (found != null && found.Count != 0) { prevSearchResults = found; prevIndex = -1; CycleSearchResults(); }
            }
        }

        private List<PrimaryClueInfo> SearchMatchingClue(string text)
        {
            List<PrimaryClueInfo> found = new List<PrimaryClueInfo>();
            foreach (PrimaryClueInfo info in PrimaryClues)
            {
                found.AddRange(info.Match(text));
            }
            return found;
        }
        #endregion

        #region Drag and Drop Support
        #region Feed Back Effects
        private void DocumentsList_DragEnter(object sender, DragEventArgs e)
        {
            // Check file type and give an icon (or maybe a full blown control) indicating its dropped effect
            base.OnDragEnter(e);
            
            // If the DataObject contains documents
            if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                // Show additional feedback
                DocumentDropCursorText.Visibility = Visibility.Visible;
                DocumentDropCursorText.Text = ((Document)e.Data.GetData(Document.DragDropFormatString)).ShortDescription;
            }

            e.Handled = true;
        }

        private void DocumentsList_DragLeave(object sender, DragEventArgs e)
        {
            base.OnDragLeave(e);

            if (DocumentDropCursorText.Visibility == Visibility.Visible)
                DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }

        private void DocumentsList_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = DragDropEffects.None;

            // If the DataObject contains documents
            if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                e.Effects = DragDropEffects.Link;

                // Show additional feedback
                Point position = e.GetPosition(UserControlCanvas);
                System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
            }

            e.Handled = true;
        }

        private void DocumentsList_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            if (e.Effects.HasFlag(DragDropEffects.Link))
            {
                Mouse.SetCursor(Cursors.Cross);
            }
            else if (e.Effects.HasFlag(DragDropEffects.Move))
            {
                Mouse.SetCursor(Cursors.Pen);
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }

        private void AddedCluesList_DragEnter(object sender, DragEventArgs e)
        {
            // Check file type and give an icon (or maybe a full blown control) indicating its dropped effect
            base.OnDragEnter(e);

            // If the DataObject contains clue string
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                // Show additional feedback
                DocumentDropCursorText.Visibility = Visibility.Visible;
                DocumentDropCursorText.Text = (string)e.Data.GetData(DataFormats.Text);
            }

            e.Handled = true;
        }

        private void AddedCluesList_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effects = DragDropEffects.None;

            // If the DataObject contains clue string
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effects = DragDropEffects.Move;

                // Show additional feedback
                Point position = e.GetPosition(UserControlCanvas);
                System.Windows.Controls.Canvas.SetLeft(DocumentDropCursorText, position.X);
                System.Windows.Controls.Canvas.SetTop(DocumentDropCursorText, position.Y);
            }

            e.Handled = true;
        }

        private void AddedCluesList_DragLeave(object sender, DragEventArgs e)
        {
            base.OnDragLeave(e);
            if (DocumentDropCursorText.Visibility == Visibility.Visible)
                DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }

        private void AddedCluesList_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            base.OnGiveFeedback(e);

            if (e.Effects.HasFlag(DragDropEffects.Link))
            {
                Mouse.SetCursor(Cursors.Cross);
            }
            else if (e.Effects.HasFlag(DragDropEffects.Move))
            {
                Mouse.SetCursor(Cursors.Pen);
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }
        #endregion
        #region Drag Initiaztion
        private void MatchingDocumentsList_MouseMove(object sender, MouseEventArgs e)
        {
            // If LMB down and is current selection
            ListBox box = sender as ListBox;
            if (box != null && e.LeftButton == MouseButtonState.Pressed && box.SelectedItem != null)
            {
                // Package the data
                DataObject data = new DataObject();
                data.SetData(Document.DragDropFormatString, box.SelectedItem);

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Link);
                e.Handled = true;
            }
        }
        private void CluesListBox_MouseMove(object sender, MouseEventArgs e)
        {
            // If LMB down and is current selection
            TreeView view = sender as TreeView;
            if (view != null && e.LeftButton == MouseButtonState.Pressed && view.SelectedItem != null)
            {
                // Package the data
                DataObject data = new DataObject();
                data.SetData(DataFormats.Text, (view.SelectedItem as PrimaryClueInfo).Name);

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                e.Handled = true;
            }
        }
        #endregion
        #region Dropping
        private List<Document> AddedDocuments;
        private void DocumentsList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Document.DragDropFormatString))
            {
                Document doc = (Document)e.Data.GetData(Document.DragDropFormatString);

                // Add document to panel display
                if (AddedDocuments.Contains(doc) == false)
                {
                    AddedDocuments.Add(doc);
                    DocumentsList.ItemsSource = null;
                    DocumentsList.ItemsSource = AddedDocuments;
                }
            }

            // Hide additional feedback
            DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }
        private void CluesList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string clueString = (string)e.Data.GetData(DataFormats.Text);
                if (Clue.ValidateString(clueString) == true && ClueManager.Manager.GetTreeNode(new Clue(clueString)) != null)
                {
                    // Add text to panel display
                    AddedClues.Add(clueString);
                }
            }

            // Hide additional feedback
            DocumentDropCursorText.Visibility = Visibility.Collapsed;
        }
        #endregion
        #region Extra Managements
        // <Development> What does this do?
        private void DocumentsList_KeyDown(object sender, KeyEventArgs e)
        {
            ListBox box = sender as ListBox;
            if (box.SelectedItems != null && box.SelectedItems.Count != 0 && e.Key == Key.Delete)
            {
                foreach (object item in box.SelectedItems)
                {
                    AddedDocuments.Remove(item as Document);
                }
                box.ItemsSource = null;
                box.ItemsSource = AddedDocuments;
            }
        }

        private void AddedCluesList_KeyDown(object sender, KeyEventArgs e)
        {
            ListBox box = sender as ListBox;
            if (box.SelectedItem != null && e.Key == Key.Delete)
            {
                AddedClues.Remove(box.SelectedItem as string);
            }
        }
        #endregion
        #endregion

        #region Clue Naming and Renaming
        private void ClueNameTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (string.IsNullOrEmpty(box.Text) == false)
                box.IsReadOnly = false; // Enable editing
        }

        private void ClueNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (string.IsNullOrEmpty(box.Text) == false && box.IsReadOnly == false)
                FinishEditing(box.Tag as string, box.Text);
            box.IsReadOnly = true;
        }

        private void ClueNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (string.IsNullOrEmpty(box.Text) == false && box.IsReadOnly == false && e.Key == Key.Enter)
            {
                FinishEditing(box.Tag as string, box.Text);
                box.IsReadOnly = true;
            }
        }

        private void FinishEditing(string oldClueString, string newClueString)
        {
            Clue oldClue = new Clue(oldClueString);
            Clue newClue = new Clue(newClueString);

            // Save Change
            List<Document> affectedDocuments = ClueManager.Manager.ChangeClue(oldClue, newClue);
            foreach (Document doc in affectedDocuments)
            {
                doc.ChangeClue(oldClue, newClue);
            }

            // Reflect change in UI
            PrimaryClues = ClueManager.Manager.GetPrimaryClueInfo();
        }

        private void SaveAlias_Click(object sender, RoutedEventArgs e)
        {
            if (CluesListBox.SelectedItem == null) return;

            PrimaryClueInfo info = CluesListBox.SelectedItem as PrimaryClueInfo;
            Clue oldClue = new Clue(info.Name);
            Clue newAlias = new Clue(AliasMemoirBar.SearchTextBox.Text);

            List<Document> affectedDocuments = ClueManager.Manager.AddClueAlias(oldClue, newAlias);
            foreach (Document doc in affectedDocuments)
            {
                doc.AddClue(newAlias);
            }

            PrimaryClues = ClueManager.Manager.GetPrimaryClueInfo();
        }
        #endregion

        #region Interface Buttons
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // <Development> Might also want to do some saving
            Window.GetWindow(this).Close();
        }

        private void CluesListBox_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Pending studying: diff between SelectedItem and SelectedValue

            if(CluesListBox.SelectedItem != null)   // Can be null during selection
            {
                PrimaryClueInfo clueInfo = CluesListBox.SelectedItem as PrimaryClueInfo;
                string clueString = null;
                if (clueInfo != null)
                {
                    clueString = clueInfo.Name;
                }
                else
                {
                    clueString = CluesListBox.SelectedItem as string;
                }

                // Clear states
                currentClueString = "";
                UpdateSecondaryAndFoundDocumentsPanel(clueString);
            }
        }

        private void ClueGroupButton_Click(object sender, RoutedEventArgs e)
        {
            ClueButton button = sender as ClueButton;
            ClueFragment fragment = button.DataContext as ClueFragment;

            UpdateSecondaryAndFoundDocumentsPanel(fragment.Name);
        }

        private string currentClueString = "";
        private void UpdateSecondaryAndFoundDocumentsPanel(string addition)
        {
            if (currentClueString == "") currentClueString = addition;
            else currentClueString = Clue.Concatenate(currentClueString, addition);

            // Do a search
            List<ClueFragment> nextFragments;
            List<Document> foundDocuments;
            ClueManager.Manager.GetBranches(new Clue(currentClueString), out nextFragments, out foundDocuments);

            // Update Display
            PrimaryClueLabel.Text = currentClueString;
            PrimaryClueLabel.Tag = currentClueString;  // Reference as oldClue incase of change of text
            ClueGroupsItemsControl.ItemsSource = nextFragments;
            MatchingDocumentsList.ItemsSource = foundDocuments;
        }

        private void EasyImportButton_Click(object sender, RoutedEventArgs e)
        {
            Dialog.EasyImportDialog dialog = new Dialog.EasyImportDialog();
            // Aura
            // ...
            if (dialog.ShowDialog() == true)
            {
                // <debug>
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int oldCount = Home.Documents.Count;

                // <Development> Consider multithread this operation, not just async, but multithread, for disk can be better utilied but CPU is limited currently -- but current problem is with clues comparison; Also cautious thread safety when trying to make it multithreaded
                // <Development> Definitely need to make this async, and show some statics after job is done; Also need to update current ClueBrowser panels; Or another way is to halt the UI and show a rotating status circle/bar with current folder (don't show individual files since that would probably be too fast to be informative)
                // Still make UI responsive by allowing user to at least drag things around for fun, or play a small game or watch a computer generated life game -- that is definitely more usable
                ObservableCollection<TreeFolderInfo> roots = dialog.RootFoldersList;
                foreach (TreeFolderInfo root in roots)
                {
                    Home.ImportFolderSelective(root);
                }

                // <debug>
                stopwatch.Stop();
                (App.Current as App).Log(UrgencyLevel.Notice, string.Format("Loading {0} files took {1} seconds.", Home.Documents.Count - oldCount, stopwatch.Elapsed.TotalSeconds));
            }
        }
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

        private ObservableCollection<PrimaryClueInfo> _PrimaryClues;
        private ObservableCollection<string> _AddedClues;

        public ObservableCollection<PrimaryClueInfo> PrimaryClues
        {
            get { return this._PrimaryClues; }
            set
            {
                if (value != this._PrimaryClues)
                {
                    this._PrimaryClues = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public ObservableCollection<string> AddedClues
        {
            get { return this._AddedClues; }
            set
            {
                if (value != this._AddedClues)
                {
                    this._AddedClues = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}
