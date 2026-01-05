using MULTITUDE.Canvas;
using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using MULTITUDE.Class.Facility.ClueManagement;
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

namespace MULTITUDE.CustomControl
{
    /// <summary>
    /// Indicates which general search format we are using during clue segment breakdown
    /// </summary>
    public enum FormatIndicator
    {
        ID,
        Constrained,
        SimpleClue,
        Ambiguous,
        Unrecognized,
        Initiation  // When noneo f the seperator has been used, i.e. the beginning of typing, in which case we suggest some possible clues to type
    }

    public class Triplet<TKey, TValue, TTag>
    {
        public Triplet(TKey key, TValue value, TTag tag)
        {
            this.Key = key;
            this.Value = value;
            this.Tag = tag;
        }

        public TKey Key;
        public TValue Value;
        public TTag Tag;
    }

    public enum SearchComboBoxEnum
    {
        Local,
        Web,
        Everything
    }

    /// <summary>
    /// Popup controls will block IME input, so we need to implement a custom popup, with Owner set to SearchBox custom control so it only displays on top of its owner
    /// </summary>
    public partial class MemoirSearchBoxPopup : Window
    {
        public MemoirSearchBoxPopup()
        {
            InitializeComponent();
            this.Owner = App.Current.MainWindow;

            // Give some defaults
            ParentSearchTextBox = null;
            InterfaceOption = InterfaceOption.NormalTextField;
            SearchMode = SearchMode.General;
            UsageOption = UsageOption.Searcher;
            ComboBoxSelection = SearchComboBoxEnum.Local;
            BoundDocuments = null;
            oldClues = null;
        }

        #region Textbox related UI Synchronization: Alsomost a duplicate of that of MemoirSearchBox.xaml.cs
        // Bookkeeping
        public TextBox ParentSearchTextBox { get; set; }
        public InterfaceOption InterfaceOption { get; set; }
        public SearchMode SearchMode { get; set; }
        public UsageOption UsageOption { get; set; }
        public SearchComboBoxEnum ComboBoxSelection { get; set; }
        internal List<Document> BoundDocuments { get; set; }
        private List<Clue> oldClues { get; set; }

        // Interaction functions: Attach first, then interact within, disable automatically upon losing focus
        internal void Attach(TextBox parentSearchTextBox, SearchMode searchMode, InterfaceOption interfaceOption, List<Document> boundDocuments, SearchComboBoxEnum comboBoxSelection = SearchComboBoxEnum.Local, UsageOption usage = UsageOption.Searcher)  // 0 for local, 1 for web
        {
            // Bookkeping
            ParentSearchTextBox = parentSearchTextBox;
            this.SearchMode = searchMode;
            this.InterfaceOption = interfaceOption;
            this.ComboBoxSelection = comboBoxSelection;
            BoundDocuments = boundDocuments;

            // Adjust text display
            Configure(searchMode, interfaceOption);
            SearchTextBox.Height = parentSearchTextBox.ActualHeight;
            SearchTextBox.Text = ParentSearchTextBox.Text;
            //SearchTextBox.CaretIndex = ParentSearchTextBox.CaretIndex;

            // Adjust width
            Width = parentSearchTextBox.ActualWidth;
            // Adjust display location: https://stackoverflow.com/questions/386731/get-absolute-position-of-element-within-the-window-in-wpf
            Point position = parentSearchTextBox.PointToScreen(new Point(0d, 0d));
            Show(position); /* + parentSearchTextBox.ActualHeight*/

            // More bookkeeping
            if (comboBoxSelection == SearchComboBoxEnum.Local && searchMode == SearchMode.Clues)
                oldClues = Clue.CreateCluesFromText(SearchTextBox.Text);
            else
                oldClues = null;
        }
        private void Configure(SearchMode mode, InterfaceOption options)
        {
            // Interface Configurations
            if ((options & InterfaceOption.RoudCornerGemBlue) == InterfaceOption.RoudCornerGemBlue) SearchTextBox.Style = (Style)FindResource("RoundCornerTextboxStyle");
            else if((options & InterfaceOption.HeavilyDecoratedRoudCorner) == InterfaceOption.HeavilyDecoratedRoudCorner) SearchTextBox.Style = (Style)FindResource("HeavilyDecoratedRoundCornerTextboxStyle");
            else SearchTextBox.Style = (Style)FindResource("MultiLineTextboxStyle");
            if ((options & InterfaceOption.ShowValidationSymbol) == InterfaceOption.ShowValidationSymbol) ValidationSymbol.Visibility = Visibility.Visible;
            else ValidationSymbol.Visibility = Visibility.Hidden;
            //if ((options & InterfaceOption.ShowEnterTextHere) == InterfaceOption.ShowEnterTextHere) SearchTextBox.Text = App.DefaultTextboxText;
            //else SearchTextBox.Text = string.Empty;
            if ((options & InterfaceOption.MultilineEditing) == InterfaceOption.MultilineEditing) SearchTextBox.AcceptsReturn = true;
            else SearchTextBox.AcceptsReturn = false;
            if ((options & InterfaceOption.ShowDocumentPreview) == InterfaceOption.ShowDocumentPreview) DocumentSelectionPane.Visibility = Visibility.Visible;
            if ((options & InterfaceOption.MultipleDocumentSelection) == InterfaceOption.MultipleDocumentSelection) DocumentSelectionPane.SelectionMode = SelectionMode.Multiple;
        }
        private void Detach()
        {
            if (ParentSearchTextBox == null) return;

            // Feed back user entry
            ParentSearchTextBox.Text = SearchTextBox.Text;
            //ParentSearchTextBox.CaretIndex = SearchTextBox.CaretIndex;
            ParentSearchTextBox.Visibility = Visibility.Visible;
            this.Visibility = Visibility.Hidden;

            if (BoundDocuments == null) return;
            // Update Document Clues
            List<Clue> newClues = Clue.CreateCluesFromText(SearchTextBox.Text);
            // If there is only one document, then replace its clues completely: but that is not very efficient per our current clue implementation so we will always be precise using below generic solution
            // If there are many documents, it's a bit more complicated (find shared clues and then replace only selected ones): in short, replace old ones, then add new ones
            foreach (Document doc in BoundDocuments)
            {
                foreach (Clue oldClue in oldClues)
                {
                    doc.RemoveClue(oldClue);
                }
                foreach (Clue newClue in newClues)
                {
                    doc.AddClue(newClue);
                }
            }

            // Reset self
            SearchTextBox.Text = string.Empty;
        }

        // LostFocus isn't working
        // Ref: https://stackoverflow.com/questions/19392036/get-currently-focused-element-control-in-a-wpf-window
        private void SearchPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            Detach();
        }
        #endregion

        #region Popup Related Standalone functions
        // To summary general usage: 1. Configure 2. Bind SecondaryPreviewKeyDownEvent and DocumentSelectionConfirmationEvent 3. Show(Point) 4. Hide() - Provided by parent class 5. Fetch needed information through below three members
        public event PreviewKeyDownHandler SecondaryPreviewKeyDownEvent = delegate {};
        internal event DocumentSelectionConfirmationHandler DocumentSelectionConfirmationEvent = delegate { };
        internal List<Document> Documents { get { return DocumentSelectionPane.ItemsSource as List<Document>; } }
        internal List<Document> Selections { get { Document[] array = new Document[DocumentSelectionPane.SelectedItems.Count]; DocumentSelectionPane.SelectedItems.CopyTo(array, 0); return array.ToList(); } }
        public string SearchString { get { return SearchTextBox.Text; } }
        public void Configure(double width, SearchMode mode, InterfaceOption options)
        {
            // SearchTextBox.Height = height;
            Width = width;
            Configure(mode, options);
        }
        public void Show(Point location)
        {
            // Set
            this.Left = location.X;
            this.Top = location.Y;
            // Show
            this.Visibility = Visibility.Visible;
            // FOcus
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }
        #endregion

        #region Event Handling and Processing Logic
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //<Development> Consider porting Web mode with Lightning's recent urls to boost usability

            // Skip default
            if (SearchTextBox.Text == "" || SearchTextBox.Text == App.DefaultTextboxText) return;

            // If InterfaceOption.ExtendedFunctions is enabled and we have selected web mode then don't do anything here
            if ((InterfaceOption & InterfaceOption.ExtendedFunctions) == InterfaceOption.ExtendedFunctions && ComboBoxSelection == SearchComboBoxEnum.Web) return; // Don't do anything

            // Select only the line current cursor in on for search (e.g. in case of multiline)
            // Ref: https://stackoverflow.com/questions/17909651/c-sharp-get-cursor-line-in-richtextbox
            // Ref: https://stackoverflow.com/questions/31651305/how-to-get-textboxs-line-from-mouse-position
            string searchString = string.Empty;
            try
            {
                int lineIndex = SearchTextBox.GetLineIndexFromCharacterIndex(SearchTextBox.CaretIndex);
                if (lineIndex == -1) return;
                searchString = SearchTextBox.GetLineText(lineIndex);
            }
            catch (Exception) { return; }
            if (string.IsNullOrWhiteSpace(searchString)) { return; }

            // Do search depending on current setting
            Triplet<List<ClueFragment>, List<Document>, FormatIndicator> triplet = await Task.Run(() => SearchTextBox_TextChangedDoSeaerch(searchString));
            // Retrive return values
            List<ClueFragment> nextClues = triplet.Key;
            List<Document> foundDocuments = triplet.Value;
            FormatIndicator formatIndicator = triplet.Tag;

            // Set up return results
            if(nextClues != null)
            {
                for (int i = 0; i < nextClues.Count; i++)
                {
                    if (i < 10)
                    {
                        nextClues[i].Index = "F" + (i + 1); // Use functional keys to quickly select a suggested clue
                    }
                    else nextClues[i].Index = string.Empty;
                }
            }
            ClueFragmentSelectionpane.ItemsSource = nextClues;
            DocumentSelectionPane.ItemsSource = foundDocuments;

            // Validation: If ShowValidationSymbol is enabled then show whether we find anything
            if ((InterfaceOption & InterfaceOption.ShowValidationSymbol) == InterfaceOption.ShowValidationSymbol)
            {
                if (foundDocuments != null && foundDocuments.Count > 0)
                {
                    ValidationSymbolText.Content = "✓";

                    // Also display format indicator of current input format in tooltip when it's a tick
                    switch (formatIndicator)
                    {
                        case FormatIndicator.ID:
                            ValidationSymbolText.ToolTip = "Perform a search using ID";
                            break;
                        case FormatIndicator.Constrained:
                            ValidationSymbolText.ToolTip = "Perform a search using constraints";
                            break;
                        case FormatIndicator.SimpleClue:
                            ValidationSymbolText.ToolTip = "Perform a search using simple clue format";
                            break;
                        case FormatIndicator.Ambiguous:
                            ValidationSymbolText.ToolTip = "Perform an ambiguous serach";
                            break;
                        case FormatIndicator.Initiation:
                            ValidationSymbolText.ToolTip = "Initializing a search";
                            break;
                        default:
                            ValidationSymbolText.ToolTip = "Unrecognized format";
                            break;
                    }

                }
                else ValidationSymbolText.Content = "!";
            }
        }

        private Triplet<List<ClueFragment>, List<Document>, FormatIndicator> SearchTextBox_TextChangedDoSeaerch(string searchString)
        {
            // Prepare return
            List<ClueFragment> nextClues = null;
            List<Document> foundDocuments = null;

            // Extract Contents
            string[] keyPhrases;
            string[] metakeys;
            string[] metavalues;
            string keyword;
            string contentAddress;
            FormatIndicator formatIndicator;
            BreakTextIntoFragments(searchString, out keyPhrases, out metakeys, out metavalues, out contentAddress, out keyword, out formatIndicator);

            switch (SearchMode)
            {
                // Assume inputs will strictly be clues and clues only, potentially with CA but ignored
                case SearchMode.Clues:
                    // Do a search
                    if(keyPhrases != null) ClueManager.Manager.SearchForClueFragments(new Clue(keyPhrases), out nextClues, out foundDocuments); // keyPhrases can be null if we just write something like "[something]" in SearchTextBox
                    break;
                case SearchMode.General:
                    switch (formatIndicator)
                    {
                        case FormatIndicator.ID:
                            ClueManager.Manager.SearchByID(int.Parse(keyPhrases.First()), contentAddress, out foundDocuments);
                            break;
                        case FormatIndicator.Constrained:    // Notice difference in interpreataion of keyphrase as complete clue strings not just fragments
                            ClueManager.Manager.SearchByGeneralConstraints(keyPhrases, contentAddress, keyword, metakeys, metavalues, out nextClues, out foundDocuments);
                            break;
                        case FormatIndicator.SimpleClue:
                            ClueManager.Manager.SearchBySimpleClue(new Clue(keyPhrases), contentAddress, out nextClues, out foundDocuments);
                            break;
                        case FormatIndicator.Ambiguous: // Notice different in interpreataion of below parameters
                            ClueManager.Manager.AmbiguousSearch(keyPhrases, out foundDocuments);
                            break;
                        case FormatIndicator.Initiation:
                            ClueManager.Manager.GetInitialSuggestion(searchString, out nextClues, out foundDocuments);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            // Return
            return new Triplet<List<ClueFragment>, List<Document>, FormatIndicator>(nextClues, foundDocuments, formatIndicator);
        }

        private void ClueButton_Click(object sender, RoutedEventArgs e)
        {
            ClueButton button = sender as ClueButton;
            AppendClueFragment(button.Content as string);
        }

        public delegate void PreviewKeyDownHandler(KeyEventArgs e);
        internal delegate void DocumentSelectionConfirmationHandler(List<Document> documents, string searchString);
        private void DocumentSelectionPane_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                DocumentSelectionConfirmationEvent(Selections, SearchString);
                e.Handled = true;
            }
        }
        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Pass to Secondary Handler
            SecondaryPreviewKeyDownEvent.Invoke(e);
            if (e.Handled == true) return;

            // Number key for clue selection
            #region Functional Key Handling
            if (ClueFragmentSelectionpane.ItemsSource != null)
            {
                List<ClueFragment> clueFragments = ClueFragmentSelectionpane.ItemsSource as List<ClueFragment>;
                switch (e.Key)
                {
                    case Key.F1:
                        if(clueFragments.Count >= 1)
                        {
                            AppendClueFragment(clueFragments[0].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F2:
                        if (clueFragments.Count >= 2)
                        {
                            AppendClueFragment(clueFragments[1].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F3:
                        if (clueFragments.Count >= 3)
                        {
                            AppendClueFragment(clueFragments[2].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F4:
                        if (clueFragments.Count >= 4)
                        {
                            AppendClueFragment(clueFragments[3].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F5:
                        if (clueFragments.Count >= 5)
                        {
                            AppendClueFragment(clueFragments[4].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F6:
                        if (clueFragments.Count >= 6)
                        {
                            AppendClueFragment(clueFragments[5].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F7:
                        if (clueFragments.Count >= 7)
                        {
                            AppendClueFragment(clueFragments[6].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F8:
                        if (clueFragments.Count >= 8)
                        {
                            AppendClueFragment(clueFragments[7].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F9:
                        if (clueFragments.Count >= 9)
                        {
                            AppendClueFragment(clueFragments[8].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.System:
                        if (e.SystemKey == Key.F10 && clueFragments.Count >= 10)
                        {
                            AppendClueFragment(clueFragments[9].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F11:
                        if (clueFragments.Count >= 11)
                        {
                            AppendClueFragment(clueFragments[10].Name);
                            e.Handled = true;
                        }
                        break;
                    case Key.F12:
                        if (clueFragments.Count >= 12)
                        {
                            AppendClueFragment(clueFragments[11].Name);
                            e.Handled = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            #endregion
            // Tab key for autocomplete using the first available clue
            if (e.Key == Key.Tab && ClueFragmentSelectionpane.ItemsSource != null)
            {
                List<ClueFragment> clueFragments = ClueFragmentSelectionpane.ItemsSource as List<ClueFragment>;
                if(clueFragments.Count >= 1)
                {
                    AppendClueFragment(clueFragments[0].Name);
                    e.Handled = true;
                }
            }
            // Up and down arrow key for navigation in history
            if (e.Key == Key.Up || e.Key == Key.Down)
            {

            }
            // Take action on enter key, if not multiline: if multiline interface then ignore enter as action
            if (e.Key == Key.Enter && ((InterfaceOption & InterfaceOption.MultilineEditing) != InterfaceOption.MultilineEditing))
            {
                // If InterfaceOption.ExtendedFunctions is enabled check option in combo box
                if ((InterfaceOption & InterfaceOption.ExtendedFunctions) == InterfaceOption.ExtendedFunctions && ComboBoxSelection == SearchComboBoxEnum.Web)
                {
                    // Search web
                    VirtualWorkspaceWindow current = VirtualWorkspaceWindow.CurrentWindow;
                    current.OpenWebpage(SearchTextBox.Text);
                }
                else
                {
                    // Do nothing
                }
            }
        }

        private void AppendClueFragment(string fragment)
        {
            SearchTextBox.Text = MergeOverlap(SearchTextBox.Text, fragment);   // Do not append - afterwords
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
        }

        // Merge two strings from overlap location, requiring former's trailing sections overlap latter's beginning sections or it's just joined together
        private string MergeOverlap(string former, string latter)
        {
            string formerString = former;
            for (int i = 0; i < former.Length; i++)
            {
                if(latter.IndexOf(former.Substring(i)) == 0)
                {
                    formerString = former.Substring(0, i);
                }
            }

            formerString = formerString.Trim('-');
            return formerString.Length > 0 ? Clue.Concatenate(formerString, latter) : latter;
        }

        // Drag support for DocumentSelectionPane (mainly used with VWWindow and ClueBrowser) 
        private void DocumentSelectionImage_MouseMove(object sender, MouseEventArgs e)
        {
            // If LMB down and is current selection
            if (e.LeftButton == MouseButtonState.Pressed && (sender as FrameworkElement).DataContext == DocumentSelectionPane.SelectedItem)
            {
                // Package the data.
                DataObject data = new DataObject();
                data.SetData(Document.DragDropFormatString, (sender as FrameworkElement).DataContext);

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Link);
                e.Handled = true;
            }
        }

        // Documnet interaction logic for DocumentSelectionPane: double click to open for editing (by send the query to VWWindow)
        // We can handle it using ListBox MouseDouble Click but it's more handy to directly interact with ListBoxItem
        // Ref: https://stackoverflow.com/questions/821564/double-click-a-listbox-item-to-open-a-browser
        private void DocumentIconItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        { 
            VirtualWorkspaceWindow.CurrentWindow.OpenDocument((sender as ListBoxItem).DataContext as Document, true);
        }
        #endregion


        #region Search Helper (Mostly Async)
        /// <summary>
        /// Break an input for all three search modes (ExistingCluesOnly, AllClueCombinations, General) + all formats for general mode (ID, constraint, shorthand, ambiguous): 
        ///  - Break input into clue segments, along with potential CA (without []) and other elements
        /// GUID form: ID0000[ContentElement] 
        /// Constriant form: A-B-C;A-B;…#metaname#metaname@metavalue[ContentElement]"keyword"
        /// Shorthand clue form: A-B-C-clue or meta(value or name, can be partial) (where "clue or metavalue" part is stored as the last keyphrase, not as metavalues array)
        /// Ambiguous form: !A B C
        /// </summary>
        /// <param name="input">User input, user might use [[ ]] and -- in search text to match []- in actual search target, which is not suggested to contain such but anyway</param>
        /// <param name="keyPhrases">Clue key phrases, the last one might indicuate a category or a target document meta</param>
        /// <param name="contentAddress"></param>
        private void BreakTextIntoFragments(string input, out string[] keyPhrases, out string[] metakeys, out string[] metavalues, out string contentAddress, out string keyword, out FormatIndicator formatIndicator)
        {
            // Trim
            input = input.Trim();

            // Set up default values
            keyPhrases = null;
            metakeys = null;
            metavalues = null;
            contentAddress = null;
            keyword = null;

            // Handle ambiguous form
            if ( input[0] == '!' ||
                ((SearchMode == SearchMode.General) && input.Contains('-') == false && input.Contains('[') == false && input.Contains(' ') == true))
            {
                // Setup format
                formatIndicator = FormatIndicator.Ambiguous;
                // Escape   (double space)
                string doubleSpaceEscapeSymbol = "~%S&!";
                keyPhrases = input.Replace("  ", doubleSpaceEscapeSymbol).Split(new char[] { ' ' });
                // Escape back
                for (int i = 0; i < keyPhrases.Length; i++)
                {
                    keyPhrases[i] = keyPhrases[i].Replace(doubleSpaceEscapeSymbol, " ");
                }
                return;
            }

            // Escape [[
            string openSquareEscapeSymbol = "~%O&!";
            string escaped = input.Replace("[[", openSquareEscapeSymbol);    // Notice input is trimmed, and later processing assumes no beginning and trailing spaces
            // Escape ]]
            string closeSquareEscapeSymbol = "~%C&!";
            escaped = escaped.Replace("]]", closeSquareEscapeSymbol);
            // Escape --
            string dashEscapeSymbol = Clue.dashEscapeSymbol;
            escaped = escaped.Replace("--", dashEscapeSymbol);

            // Extract content address: on first and last occurence (so CA might make use of some embeded [] format?)
            int beginIndex = escaped.IndexOf('[');
            int endIndex = escaped.LastIndexOf(']');
            if (beginIndex >= 0 && beginIndex < endIndex)
            {
                contentAddress = escaped.Substring(beginIndex, endIndex - beginIndex);
            }
            else
            {
                contentAddress = null;
            }

            // If we have anything left for actual clues, use it
            if (beginIndex > 0 || beginIndex == -1)
            {
                // Get the part before CEA
                if (beginIndex > 0) escaped = escaped.Substring(0, beginIndex);
                // Extract ID
                if (escaped.Length > 2 && escaped.ToUpper().IndexOf("ID") == 0)
                {
                    keyPhrases = new string[1];
                    keyPhrases[0] = keyPhrases[0].Substring(2);
                }
                else if (escaped.Length <= 2)
                {
                    keyPhrases = new string[] { escaped };
                }
                else
                {
                    // Split: A-B-C;A-B;…#metaname#metaname@metavalue
                    string[] clues = escaped.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    // Extract last clue: A-B-C#metaname#metaname@metavalue or #metaname#metaname@metavalue
                    string lastClue = clues[clues.Length - 1];
                    string metaStrings = lastClue;
                    int nearestSeperatorIndex = Math.Min(lastClue.IndexOf('#') == -1 ? int.MaxValue : lastClue.IndexOf('#'), lastClue.IndexOf('@') == -1 ? int.MaxValue : lastClue.IndexOf('@'));
                    if (nearestSeperatorIndex != int.MaxValue)
                    {
                        metaStrings = lastClue.Substring(nearestSeperatorIndex);
                        clues[clues.Length - 1] = lastClue.Substring(0, nearestSeperatorIndex); // Nearest seperator index could just be 0
                    }
                    else metaStrings = null;
                    if (clues[clues.Length - 1] == "") clues = clues.Take(clues.Length - 1).ToArray(); // Remove empty clues

                    // Extract meta contraints: #metaname#metaname@metavalue, where for meta section order doesn’t matter
                    if (metaStrings != null)
                    {
                        List<string> metavaluesList = new List<string>();
                        metakeys = metaStrings.Split(new char[] { ' ', '#' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < metakeys.Length; i++)
                        {
                            int metaValueSeperatorIndex = metakeys[i].IndexOf('@');
                            if (metaValueSeperatorIndex != -1)
                            {
                                metavaluesList.Add(metakeys[i].Substring(metaValueSeperatorIndex));
                                metakeys[i] = metakeys[i].Substring(0, metaValueSeperatorIndex);
                            }
                        }
                        metakeys = metakeys.Where(x => !string.IsNullOrEmpty(x)).ToArray(); // How expensive is such preparation work....
                        metavalues = metavaluesList.ToArray();
                    }

                    // Assign return values
                    keyPhrases = clues;
                }
            }

            // Extract keyword for contraints
            if (endIndex > 0 && endIndex != escaped.Length - 1)
                keyword = escaped.Substring(endIndex + 1).Replace("\"", "");

            // Return escape back to content
            if (contentAddress != null)
            {
                contentAddress = contentAddress.Replace(openSquareEscapeSymbol, "[");
                contentAddress = contentAddress.Replace(closeSquareEscapeSymbol, "]");
                contentAddress = contentAddress.Replace(dashEscapeSymbol, "-");
            }
            if (keyPhrases != null)
            {
                for (int i = 0; i < keyPhrases.Length; i++)
                {
                    keyPhrases[i] = keyPhrases[i].Replace(openSquareEscapeSymbol, "[");
                    keyPhrases[i] = keyPhrases[i].Replace(closeSquareEscapeSymbol, "]");
                    keyPhrases[i] = keyPhrases[i].Replace(dashEscapeSymbol, "-");
                }
            }

            // Decide on format
            if (keyPhrases != null && keyPhrases[0].ToUpper().IndexOf("ID") == 0) formatIndicator = FormatIndicator.ID;
            else if (input.Contains(';') == true || metakeys != null || metavalues != null) formatIndicator = FormatIndicator.Constrained;
            else if (input.IndexOf('-') > 0 || input.IndexOf('[') > 0)
            {
                keyPhrases = keyPhrases[0].Split(new char[] { '-'}, StringSplitOptions.RemoveEmptyEntries);
                formatIndicator = FormatIndicator.SimpleClue;
            }
            else if (input.Contains('-') == false && input.Contains(' ') == false) formatIndicator = FormatIndicator.Initiation;
            else formatIndicator = FormatIndicator.Unrecognized;
        }
        #endregion

        private void ValidationSymbol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open search gduie popup
            throw new NotImplementedException();
        }

        #region Disable accidental Alt-F4 closing because a popup is kept alive
        private bool bAltF4Pressed = false;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bAltF4Pressed)
            {
                e.Cancel = true;
                bAltF4Pressed = false;
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Alt-F4 handling for closing window
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4) bAltF4Pressed = true;
            else bAltF4Pressed = false;
        }
        #endregion
    }
}
