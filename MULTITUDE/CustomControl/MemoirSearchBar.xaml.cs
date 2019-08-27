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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MULTITUDE.CustomControl
{
    // General search format: keyphrase-keyphrase-keyphrase[ContentAddressing], or ID0000[CA]
    // Search formats: 
    //      1. GUID form (Absolute Addressing): ID0000[ContentElement]; Notice all other forms except GUID form are embeded search, rather than registered link, i.e. when linked documents are changed (deleted, or its clue is changed) they won't get noticed.
    //      2. Constriant form (Conditional Match): A-B-C;A-B;…#metaname#metaname(=partial)@metavalue[ContentElement]"keyword" Where for meta section order doesn’t matter; "keyword" means non-specified location and must be after [] if any; Notice A-B-C must strictly be clues and a single ; Must follow each clue except the last one
                // Content wise search, ambiguous content or speicfic addressing, should be implemented by document type itself, i.e. still through [CE]; Notice [CE] functions differently from "keyword".
    //      3. Shorthand clue form (Semi-conditional Match): A-B-C-clue[CA] or meta(value or name, can be partial), where the last phrase after – isn’t a clue but actually a meta
    //      4. Ambiguous: a space demilited list of key words; Search into clues, names and comment; not searching other meta; Supports escape single space using double spaces; use ! to begine a must match keyword
    //      Clues related search is per character exact match by design due to autocompletion, while metavalue can be a partial match.
    //      By design a search either has result or not, and if not it's considered invalid search.
    //      The first 3 formats are "acccurate" while the last one "inaccurate", meaning that all elements of the first 3 formats are either exact or partial match of document information, while for ambiguous search auxiliary (uncertain) keywords can be used
    public enum SearchMode  // Also if InterfaceOption.ExtendedFunctions is enabled check option in combo box; If ShowValidationSymbol is enabled then show related signals; If InterfaceOption.ShowEnterTextHere is enabled then show default text
    {
        Clues,  // A search mode dedicated specifically for clues: Display all used clues in pallet popup; Ignore content links; Used for finding catogory match and specifying clues
        General,  // General purpose search use above 4 formats
    }

    public enum UsageOption
    {
        Linker,     // Entries can register and unregister with Documents so a change in document existence might get notified of document itself upon change or linker -  <Development> Only GUID forms of search have such effect; Other forms can be "bound" to become a GUID form so it becomes a link; Otherwise linker is just an embeded search without registration
            // Llinker has an exter interface feature to allow it to be bound (i.e. tramsform it into a GUID form, and get registered) // <Development> <Deprecated> Not used
        Creator,    // Optimized order for orderability with existing ones, e.g. A-B-C always in A-B-C not in C-B-A; Search in existing clues and allow adding new fragments
                    // When used as a creator , extra validation and replacement can be used as to avoid exceptions of Clues (e.g. invalid characters)
        Searcher,   // Default, no extra feature
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
        ShowDocumentPreview = 64,    // Show matching documents in preview panel
        HeavilyDecoratedRoudCorner = 128,
        MultipleDocumentSelection = 256
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

            // Defaults
            searchMode = SearchMode.Clues;
            interfaceOption = InterfaceOption.NormalTextField;
            usageOption = UsageOption.Searcher;
        }

        // Return parameters
        Document SelectedDocument { get; set; }
        static internal List<ClueFragment> RecentClues { get; set; } // Shared among Search bars; Instead of fragments those ClueFramgents' names contain complete clues

        // Book keeping
        public SearchMode searchMode { get; set; }
        public InterfaceOption interfaceOption { get; set; }
        public UsageOption usageOption { get; set; }
        static public MemoirSearchBoxPopup SearchPopup { get; set; }    // Set by MainWindow (i.e. VW Window), users shall check not null before using though
        internal List<Document> BoundDocuments { get; set; }

        // Static
        public static readonly string ValidationPassText = "Click to open search guide.";
        public static readonly string ValidationFailText = "No match was found. Click to open search guide.";

        // Configure functionality, style, and display of the search bar
        // If not configured, defaults will be used
        internal void Configure(SearchMode mode, InterfaceOption options, UsageOption option, List<Document> boundDocuments = null)
        {
            // Save in case later use
            searchMode = mode;
            interfaceOption = options;
            usageOption = option;
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

            Bind(boundDocuments);
        }

        private void Bind(List<Document> boundDocuments)
        {
            // Book keeping
            BoundDocuments = boundDocuments;

            // Set default text with bound documents common sets
            if (boundDocuments != null && boundDocuments.Count != 0)
            {
                List<Clue> commonClues = new List<Clue>();
                foreach (Document doc in boundDocuments)
                {
                    commonClues.AddRange(doc.Clues);
                }
                commonClues = commonClues.GroupBy(x => x).Where(g => g.Count() == boundDocuments.Count).Select(g => g.Key).ToList();
                // Select items with occurences: https://stackoverflow.com/questions/28955507/select-only-the-values-present-multiple-times-in-a-list-c-sharp-linq

                if ((interfaceOption & InterfaceOption.MultilineEditing) == InterfaceOption.MultilineEditing) SearchTextBox.Text = string.Join("\n", commonClues);
                else SearchTextBox.Text = string.Join(",", commonClues);
            }
            else
            {
                if ((interfaceOption & InterfaceOption.ShowEnterTextHere) == InterfaceOption.ShowEnterTextHere)
                {
                    SearchTextBox.Text = App.DefaultTextboxText;
                }
                else
                {
                    SearchTextBox.Text = "";
                }
            }
        }

        #region Searching UI Interaction
        public void DoGetFocus()
        {
            if (SearchPopup != null)
            {
                SearchTextBox.Visibility = Visibility.Hidden;
                SearchPopup.Attach(SearchTextBox, searchMode, interfaceOption, BoundDocuments, (SearchComboBoxEnum)SearchModeComboBox.SelectedValue, usageOption);
            }
        }
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            DoGetFocus();
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (SearchTextBox.IsFocused == true && SearchPopup != null && SearchPopup.Visibility == Visibility.Hidden //  Do not use this if we want when we type in multiple texts using IME we still get update in corresponding popup
                && SearchTextBox.Text != App.DefaultTextboxText && SearchTextBox.Text != "") // This is necessary to avoid initialization problem for at that time the width of the textbox hasn't been decided yet
            {
                SearchTextBox.Visibility = Visibility.Hidden;
                SearchPopup.Attach(SearchTextBox, searchMode, interfaceOption, BoundDocuments, (SearchComboBoxEnum)SearchModeComboBox.SelectedValue, usageOption);
            }
        }
        #endregion

        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchPopup != null && SearchPopup.Visibility == Visibility.Hidden/* && SearchTextBox.Text != App.DefaultTextboxText && SearchTextBox.Text != ""*/)
            {
                SearchTextBox.Visibility = Visibility.Hidden;
                SearchPopup.Attach(SearchTextBox, searchMode, interfaceOption, BoundDocuments, (SearchComboBoxEnum)SearchModeComboBox.SelectedValue, usageOption);
            }
        }

        private void ValidationSymbol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Open search gduie popup
            throw new NotImplementedException();
        }

        private void SearchTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (SearchPopup != null && SearchPopup.Visibility == Visibility.Hidden /*&& SearchTextBox.Text != App.DefaultTextboxText && SearchTextBox.Text != ""*/)
            {
                SearchTextBox.Visibility = Visibility.Hidden;
                SearchPopup.Attach(SearchTextBox, searchMode, interfaceOption, BoundDocuments, (SearchComboBoxEnum)SearchModeComboBox.SelectedValue, usageOption);
            }
        }
    }
}
