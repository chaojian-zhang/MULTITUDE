using MULTITUDE.Canvas;
using MULTITUDE.Class;
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

namespace MULTITUDE.CustomControl
{
    // General search format: keyphrase-keyphrase-keyphrase[ContentAddressing], or ID0000[CA]
    // Search formats: 
    //      1. GUID form (Absolute Addressing): ID0000[ContentElement] 
    //      2. Constriant form (Conditional Match): A-B-C;A-B;…#metaname#metaname@metavalue[ContentElement]"keyword" Where for meta section order doesn’t matter; "keyword" means non-specified location and must be after [] if any; Notice A-B-C must strictly be clues and a single ; must follow each clue except the last one
    //      3. Shorthand clue form (Semi-conditional Match): A-B-C-clue or meta(value or name, can be partial), where the last phrase after – isn’t a clue but actually a meta
    //      4. Ambiguous: a space demilited list of key words; Search into clues, names and comment; not searching other meta; Supports escape single space using double spaces; use ! to begine a must match keyword
    //      Clues related search is per character exact match by design due to autocompletion, while metavalue can be a partial match.
    //      By design a search either has result or not, and if not it's considered invalid search.
    public enum SearchMode  // Also if InterfaceOption.ExtendedFunctions is enabled check option in combo box; If ShowValidationSymbol is enabled then show related signals; If InterfaceOption.ShowEnterTextHere is enabled then show default text
    {
        ExistingCluesOnly,  // A search mode dedicated specifically for clues; Display all used clues in pallet popup; Ignore content links; Used for finding catogory match and specifying clues
        AllClueCombinations,   // A way to provide possible combination of clues from previous usages, effectively sub-sections of other clues; Ignore content links; Used for specifying clues
        General,  // General purpose search use above 4 formats
        Deep // Same as general, but accept "keyword", and for ambiuous form we search into all meta and content (if accessible e.g. text)
    }

    [Flags]
    public enum InterfaceOption
    {
        RoudCornerGemBlue = 1,
        NormalTextField = 2,    // Default
        ExtendedFunctions = 4,   // Web suport etc.
        MultilineEditing = 8,
        ShowValidationSymbol = 16,
        ShowEnterTextHere = 32,
        ShowDocumentPreview = 64    // Show matching documents in preview panel
    }

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

    /// <summary>
    /// An iconic search bar providign both searching and Clue creation functions depending on configuration; Also support web browser as a drop down option if configured; An extensive popup showing all search contents
    /// Can be and is embeded many places for it can also serve as a simple text box
    /// Also has multiline supoprt to convert to List<string> (Used in property panel)
    /// Support drag-n-drop to other controls
    /// </summary>
    public partial class MemoirSearchBar : UserControl
    {
        public MemoirSearchBar()
        {
            InitializeComponent();
            Home = (App.Current as App).CurrentHome;
        }

        // Return parameters
        Document SelectedDocument { get; set; }
        static internal List<ClueFragment> RecentClues { get; set; } // Shared among Search bars; Instead of fragments those ClueFramgents' names contain complete clues

        // Book keeping
        public SearchMode searchMode { get; set; }
        public InterfaceOption interfaceOption { get; set; }
        private Home Home { get; set; }

        // Static
        public static readonly string ValidationPassText = "...";
        public static readonly string ValidationFailText = "No match was found.";

        // Configure functionality, style, and display of the search bar
        public void Configure(SearchMode mode, InterfaceOption options)
        {
            // Save in case later use
            searchMode = mode;
            interfaceOption = options;
            // Interface Configurations
            if ((options & InterfaceOption.ExtendedFunctions) == InterfaceOption.ExtendedFunctions)
            {
                SearchModeComboBox.Visibility = Visibility.Visible;
            }
            if ((options & InterfaceOption.RoudCornerGemBlue) == InterfaceOption.RoudCornerGemBlue)
            {
                SearchTextBox.Style = (Style)FindResource("RoundCornerTextboxStyle");
            }
            if((options & InterfaceOption.ShowValidationSymbol) == InterfaceOption.ShowValidationSymbol)
            {
                ValidationSymbol.Visibility = Visibility.Visible;
            }
            if ((options & InterfaceOption.ShowEnterTextHere) == InterfaceOption.ShowEnterTextHere)
            {
                SearchTextBox.Text = App.DefaultTextboxText;
            }
            if ((options & InterfaceOption.MultilineEditing) == InterfaceOption.MultilineEditing)
            {
                SearchTextBox.AcceptsReturn = true;
            }
        }

        #region Searching UI Interaction
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Skip default
            if (SearchTextBox.Text == "" || SearchTextBox.Text == App.DefaultTextboxText) return;

            // If InterfaceOption.ExtendedFunctions is enabled and we have selected web mode then don't do anything here
            if((interfaceOption & InterfaceOption.ExtendedFunctions) == InterfaceOption.ExtendedFunctions && SearchModeComboBox.SelectedIndex == 1) return; // Don't do anything

            // Do search depending on current setting
            Triplet<List<ClueFragment>, List<Document>, FormatIndicator> triplet = await SearchTextBox_TextChangedDoSeaerch();
            // Retrive return values
            List<ClueFragment> nextClues = triplet.Key;
            List<Document> foundDocuments = triplet.Value;
            FormatIndicator formatIndicator = triplet.Tag;

            // Set up return results
            ClueFragmentSelectionpane.ItemsSource = nextClues;
            DocumentSelectionPane.ItemsSource = foundDocuments;

            // Validation: If ShowValidationSymbol is enabled then show whether we find anything
            if ((interfaceOption & InterfaceOption.ShowValidationSymbol) == InterfaceOption.ShowValidationSymbol)
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

            // Show search popup
            SearchPopup.IsOpen = true;
        }

        private async Task<Triplet<List<ClueFragment>, List<Document>, FormatIndicator>> SearchTextBox_TextChangedDoSeaerch()
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
            BreakTextIntoFragments(SearchTextBox.Text, out keyPhrases, out metakeys, out metavalues, out contentAddress, out keyword, out formatIndicator);

            switch (searchMode)
            {
                // Assume inputs will strictly be clues and clues only, potentially with CA but ignored
                case SearchMode.ExistingCluesOnly:
                    // Do a search (Consider make it async)
                    Home.SearchExistingClues(keyPhrases, out nextClues, out foundDocuments);
                    break;
                case SearchMode.AllClueCombinations:
                    Home.GetNextClueFromPairs(SearchTextBox.Text, keyPhrases, out nextClues, out foundDocuments);  // keyPhrases can be null if we just write something like "[something]" in SearchTextBox
                    break;
                case SearchMode.General:
                    switch (formatIndicator)
                    {
                        case FormatIndicator.ID:
                            Home.SearchByID(int.Parse(keyPhrases.First()), contentAddress, out foundDocuments);
                            break;
                        case FormatIndicator.Constrained:    // Notice difference in interpreataion of keyphrase as complete clues not just phrases (clue fragments)
                            Home.SearchByGeneralConstraints(keyPhrases, metakeys, metavalues, out nextClues, out foundDocuments);
                            break;
                        case FormatIndicator.SimpleClue:
                            Home.SearchBySimpleClue(keyPhrases, out nextClues, out foundDocuments);
                            break;
                        case FormatIndicator.Ambiguous: // Notice different in interpreataion of below parameters
                            Home.AmbiguousSearch(keyPhrases, out nextClues, out foundDocuments);
                            break;
                        case FormatIndicator.Initiation:
                            Home.GetInitialSuggestion(SearchTextBox.Text, out nextClues, out foundDocuments);
                            break;
                        default:
                            break;
                    }
                    break;
                case SearchMode.Deep:
                    switch (formatIndicator)
                    {
                        case FormatIndicator.ID:
                            Home.SearchByID(int.Parse(keyPhrases.First()), contentAddress, out foundDocuments, true);
                            break;
                        case FormatIndicator.Constrained:
                            Home.SearchByGeneralConstraints(keyPhrases, metakeys, metavalues, out nextClues, out foundDocuments, true);
                            break;
                        case FormatIndicator.SimpleClue:
                            Home.SearchBySimpleClue(keyPhrases, out nextClues, out foundDocuments, true);
                            break;
                        case FormatIndicator.Ambiguous: // Notice different in interpreataion of below parameters
                            Home.AmbiguousSearch(keyPhrases, out nextClues, out foundDocuments, true);
                            break;
                        case FormatIndicator.Initiation:
                            Home.GetInitialSuggestion(SearchTextBox.Text, out nextClues, out foundDocuments);
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

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Tab key for autocomplete
            if (e.Key == Key.Tab)
            {

            }
            // Up and down arrow key for navigation in history
            if (e.Key == Key.Up || e.Key == Key.Down)
            {

            }
            // Take action on enter key, if not multiline: if multiline interface then ignore enter as action
            if (e.Key == Key.Enter && ((interfaceOption & InterfaceOption.MultilineEditing) != InterfaceOption.MultilineEditing))
            {
                // If InterfaceOption.ExtendedFunctions is enabled check option in combo box
                if ((interfaceOption & InterfaceOption.ExtendedFunctions) == InterfaceOption.ExtendedFunctions && SearchModeComboBox.SelectedIndex == 1)
                {
                    // Search web
                    VirtualWorkspaceWindow current = VirtualWorkspaceWindow.Current;
                    current.OpenWebpage(SearchTextBox.Text);
                }
                else
                {
                    // Do nothing
                }
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Ref: https://stackoverflow.com/questions/19392036/get-currently-focused-element-control-in-a-wpf-window
            //// If we are still interacting with search bar popup then don't lose focus
            //if (ClueFragmentSelectionpane.IsFocused == false && DocumentSelectionPane.IsFocused == false && RecentCluesPane.IsFocused == false)
            //{
            //    // Hide search popup
            //    SearchPopup.IsOpen = false;

            //    // Interface option
            //    if (SearchTextBox.Text == "" && (interfaceOption & InterfaceOption.ShowEnterTextHere) == InterfaceOption.ShowEnterTextHere)
            //    {
            //        SearchTextBox.Text = App.DefaultTextboxText;
            //    }
            //}
        }
        #endregion

        #region Search Helper (Mostly Async)
        /// <summary>
        /// Break an input for all three search modes (ExistingCluesOnly, AllClueCombinations, General) + all formats for general mode (ID, constraint, shorthand, ambiguous): 
        ///  - Break input into clue segments, along with potential CA (without []) and other elements
        /// GUID form: ID0000[ContentElement] 
        /// Constriant form: A-B-C;A-B;…#metaname#metaname@metavalue[ContentElement]"keyword"
        /// Shorthand clue form: A-B-C-clue or meta(value or name, can be partial) (where "clue or metavalue" part is stored as the last keyphrase, not as metavalues array)
        /// Ambiguous form: A B C
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
            if(input.Contains('-') == false && input.Contains('[') == false && input.Contains(' ') == true)
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
            string dashEscapeSymbol = Home.dashEscapeSymbol;
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
                if(beginIndex > 0) escaped = escaped.Substring(0, beginIndex);
                // Extract ID
                if (escaped.Length > 2 && escaped.ToUpper().IndexOf("ID") == 0)
                {
                    keyPhrases = new string[1];
                    keyPhrases[0] = keyPhrases[0].Substring(2);
                }
                else if(escaped.Length <= 2)
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
                    if(clues[clues.Length -1] == "") clues = clues.Take(clues.Length - 1).ToArray(); // Remove empty clues

                    // Extract meta contraints: #metaname#metaname@metavalue, where for meta section order doesn’t matter
                    if(metaStrings != null)
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
            if(contentAddress != null)
            {
                contentAddress = contentAddress.Replace(openSquareEscapeSymbol, "[");
                contentAddress = contentAddress.Replace(closeSquareEscapeSymbol, "]");
                contentAddress = contentAddress.Replace(dashEscapeSymbol, "-");
            }
            if(keyPhrases != null)
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
            else if (metakeys != null || metavalues != null) formatIndicator = FormatIndicator.Constrained;
            else if (input.IndexOf('-') > 0 || input.IndexOf('[') > 0) formatIndicator = FormatIndicator.SimpleClue;
            else if (input.Contains('-') == false && input.Contains(' ') == false) formatIndicator = FormatIndicator.Initiation; 
            else formatIndicator = FormatIndicator.Unrecognized;
        }
        #endregion
    }
}
