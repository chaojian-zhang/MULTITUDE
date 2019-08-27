using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.CustomControl.CanvasSpaceWindow;
using System;
using System.Collections.Generic;
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

namespace MULTITUDE.CustomControl.Components
{
    /// <summary>
    /// A general purpose MD+ editor used both in preview and immersive editor
    /// Document should save at close, but handled by user of this control
    /// </summary>
    public partial class MarkdownPlusEditor : UserControl
    {
        #region Construction and Setup
        public MarkdownPlusEditor()
        {
            InitializeComponent();
            Popup = null;
            //dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            //dispatcherTimer.Tick += DispatcherTimer_Tick;
            //dispatcherTimer.Interval = new TimeSpan(0,0,5);
        }

        internal void Update(Document doc, bool bPreview = true, ContentMode mode = ContentMode.FlowDocument)
        {
            if (doc is MarkdownPlus && doc.Type == DocumentType.MarkdownPlus)   // Extra debugging check
            { } // Nothing happens
            else if (doc is DataCollection && doc.Type == DocumentType.DataCollection)
            { bPreview = true; mode = ContentMode.TableOnly; } // DataCollection don't support advanced formats, and is only edited as a table
            else throw new InvalidOperationException("Invalid document type.");

            // Set binding
            FlowDocument document = null;
            switch (doc.Type)
            {
                case DocumentType.MarkdownPlus:
                    // Unset binding
                    // if (Document.FlowDocument.Parent != null) (Document.FlowDocument.Parent as RichTextBox).Document = null; // Cannot set to null
                    // if (Document.FlowDocument.Parent != null) (Document.FlowDocument.Parent as RichTextBox).Document = new FlowDocument();
                    // No longer needed for it returns a clone now and its parent is always null
                    document = (doc as MarkdownPlus).GetFlowDocument(); // This will be a new document
                    break;
                case DocumentType.DataCollection:
                    document = (doc as DataCollection).GenerateFlowDocument();    // This will be a new doucment each time this is called
                    break;
            }

            Update(document, bPreview, mode);
        }

        /// <summary>
        /// If preview is true no popup will be shown
        /// </summary>
        /// <param name="doc">The documenet to be edited, cannot be null; Can be either a MD+ or a DataCollection </param>
        /// <param name="bPreview">Whether we should display a font style popup</param>
        /// <param name="mode">ContentMode limits available operations </param>
        internal void Update(FlowDocument document, bool bPreview = true, ContentMode mode = ContentMode.FlowDocument)
        {
            if (document == null) throw new InvalidOperationException("Flowdocument cannot be null.");

            // Book keeping
            ContentMode = mode;

            DocumentText.Document = document;
            if (!bPreview && Popup == null) Popup = new MarkdownPlusEditorPopup(Window.GetWindow(this), this);
        }
        private ContentMode ContentMode;
        private MarkdownPlusEditorPopup Popup;
        #endregion

        #region Configurations
        // private static double[] HeadingFontSizes = { 0, 24, 22, 20, 18, 16, 14, 12, 10, 8};    // Index from 1
        public static SolidColorBrush DefaultBrush = Brushes.Black;
        public static double DefaultFontSize = 12;
        public static SolidColorBrush Heading1Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF62B1FF"));
        public static SolidColorBrush Heading2Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF4C9FF1"));
        public static SolidColorBrush Heading3Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF3A95EE"));
        public static SolidColorBrush Heading4Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF94F0AD"));
        public static SolidColorBrush Heading5Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF67D485"));
        public static SolidColorBrush Heading6Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFAECFCF"));
        public static SolidColorBrush Heading7Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFC5E0E0"));
        public static SolidColorBrush Heading7PlusBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFC5E0E0"));
        public static double Heading1FontSize = 24;
        public static double Heading2FontSize = 22;
        public static double Heading3FontSize = 20;
        public static double Heading4FontSize = 18;
        public static double Heading5FontSize = 16;
        public static double Heading6FontSize = 14;
        public static double Heading7FontSize = 12;
        public static double Heading7PlusFontSize = 10;
        public static string TableTitleRowDefaultText = "Title";
        public static string TableHeaderRowDefaultText = "Header";
        public static string TableContentRowDefaultText = "Content";
        public static double TableTitleRowFontSize = 40;
        public static Brush TableTitleRowBackground = Brushes.Silver;
        public static double TableHeaderRowFontSize = 18;
        public static double TableContentRowFontSize = 12;
        public static Brush TableColumnsBackground1 = Brushes.Beige;
        public static Brush TableColumnsBackground2 = Brushes.LightSteelBlue;
        #endregion

        #region Editor Interface
        public FlowDocument Document
        {
            get
            {
                return DocumentText.Document;
            }
        }
        public void HighlightSelection(string searchString)
        {
            TextRange range = new TextRange(DocumentText.Document.ContentStart, DocumentText.Document.ContentEnd);
            int index = range.Text.ToLower().IndexOf(searchString.ToLower());
            if (index == -1) throw new IndexOutOfRangeException("Specified searchstring doesn't exist in the document.");
            TextPointer start = DocumentText.Document.ContentStart.GetPositionAtOffset(index);
            TextPointer end = DocumentText.Document.ContentStart.GetPositionAtOffset(index + searchString.Length);
            DocumentText.Selection.Select(start, end);
        }
        public double GetSelectionOrCaretFontSize()
        {
            if (DocumentText.Selection.IsEmpty == false) return ((double)DocumentText.Selection.GetPropertyValue(FontSizeProperty));
            else return ((double)DocumentText.CaretPosition.Parent.GetValue(FontSizeProperty));
        }
        public void SetSelectionOrCaretFontSize(double size)
        {
            if (ContentMode == ContentMode.TableOnly) return;
            if (DocumentText.Selection.IsEmpty == false) DocumentText.Selection.ApplyPropertyValue(FontSizeProperty, size);
            else DocumentText.CaretPosition.Parent.SetValue(FontSizeProperty, size);
        }
        // Update the heading style of the whole inline (not the paragraph)
        public void UpdateHeadingStyle(double size, SolidColorBrush color)
        {
            if (ContentMode == ContentMode.TableOnly) return;
            // Revert
            if ((double)DocumentText.CaretPosition.Parent.GetValue(FontSizeProperty) == size)
            {
                DocumentText.CaretPosition.Parent.SetValue(FontSizeProperty, DefaultFontSize);
                DocumentText.CaretPosition.Parent.SetValue(ForegroundProperty, DefaultBrush);
            }
            // Update font size and color
            else
            {
                DocumentText.CaretPosition.Parent.SetValue(FontSizeProperty, size);
                DocumentText.CaretPosition.Parent.SetValue(ForegroundProperty, color);
            }

            // <Development> Pending judgement: Another solution
            //if (string.IsNullOrWhiteSpace(DocumentText.Selection.Text) == true) paragraph.FontSize = HeadingFontSizes[1];
            //else DocumentText.Selection.ApplyPropertyValue(FontSizeProperty, HeadingFontSizes[1]);
        }
        public void StrikethroughText()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            // Method 1: Update only the selection, which may be part of a inline
            if (!DocumentText.Selection.IsEmpty)
            {
                Object obj = DocumentText.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                // If set, undo
                if (obj != DependencyProperty.UnsetValue && obj is TextDecorationCollection && (obj as TextDecorationCollection).Count != 0)
                {
                    // DocumentText.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, DependencyProperty.UnsetValue); // Raise exception if we set it like this; And it's no use if we set it too null
                    // (obj as TextDecorationCollection).Clear(); // Raise exception
                    DocumentText.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, new TextDecorationCollection());  // Notice this TextRange is clear enough to merge automatically to other inlines if it was previously a seperate inline created for the textdecoration
                    // TextDecorationCollection textDecoration = obj as TextDecorationCollection;
                }
                // If unset, set
                else
                {
                    DocumentText.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Strikethrough);
                }
            }
            // Method 2: Update the whole inline
            else
            {
                Inline inline = (DocumentText.CaretPosition.Parent as Inline);
                if (inline != null)
                {
                    if (inline.TextDecorations.Count == 0) inline.TextDecorations = TextDecorations.Strikethrough;
                    else inline.TextDecorations = null;
                }
            }
        }
        private void ApplyDefaultStyle()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            // Update only the selection, which may be part of a inline
            if (!DocumentText.Selection.IsEmpty)
            {
                // Font size
                DocumentText.Selection.ApplyPropertyValue(FontSizeProperty, DefaultFontSize);
                // Color
                DocumentText.Selection.ApplyPropertyValue(ForegroundProperty, Brushes.Black);
                // Styles
                // ...
            }
            // Update the whole inline
            else
            {
                // Font size
                DocumentText.CaretPosition.Parent.SetValue(FontSizeProperty, DefaultFontSize);
                // Color
                DocumentText.CaretPosition.Parent.SetValue(ForegroundProperty, Brushes.Black);
                // Styles
                // ...
            }
        }
        // <Pending> Add undo if already set
        public void UnderlineText()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            if (EditingCommands.ToggleUnderline.CanExecute(null, DocumentText) == true)
                EditingCommands.ToggleUnderline.Execute(null, DocumentText);
        }
        // <Pending> Add undo if already set
        public void BoldText()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            if (EditingCommands.ToggleBold.CanExecute(null, DocumentText) == true)
                EditingCommands.ToggleBold.Execute(null, DocumentText);
        }
        // <Pending> Add undo if already set
        public void ItalicText()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            if (EditingCommands.ToggleItalic.CanExecute(null, DocumentText) == true)
                EditingCommands.ToggleItalic.Execute(null, DocumentText);
        }
        // <Pending> Add undo if already set
        public void ColorText(SolidColorBrush color)
        {
            if (ContentMode == ContentMode.TableOnly) return;
            // Update only the selection, which may be part of a inline
            if (!DocumentText.Selection.IsEmpty)
            {
                DocumentText.Selection.ApplyPropertyValue(ForegroundProperty, color);
            }
            // Update the whole inline
            else
            {
                DocumentText.CaretPosition.Parent.SetValue(ForegroundProperty, color);
            }
        }
        // <Pending> Add undo if already set
        public void BulletedParagraph()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            if (EditingCommands.ToggleBullets.CanExecute(null, DocumentText) == true)
                EditingCommands.ToggleBullets.Execute(null, DocumentText);
        }
        // <Pending> Add undo if already set
        public void NumberedParagraph()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            if (EditingCommands.ToggleNumbering.CanExecute(null, DocumentText) == true)
                EditingCommands.ToggleNumbering.Execute(null, DocumentText);
        }
        public void LatinParagraph()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            // First create a list
            if (EditingCommands.ToggleNumbering.CanExecute(null, DocumentText) == true)
                EditingCommands.ToggleNumbering.Execute(null, DocumentText);
            // Then change its format
            ListItem listItem = DocumentText.CaretPosition.Paragraph.Parent as ListItem;
            if (listItem != null) (listItem.Parent as List).MarkerStyle = TextMarkerStyle.LowerLatin;
        }
        public void AddFloatingImage()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            throw new NotImplementedException();
        }
        public void AddFloatingNote()
        {
            if (ContentMode == ContentMode.TableOnly) return;
            throw new NotImplementedException();
        }
        #region Table Operations
        public void InsertTable(int x, int y, bool bTitle = true, bool bHeader = true)   // X for num of columns, Y for num of rows (excluding title and header row, which is created by default)
        {
            // Create a table
            System.Windows.Documents.Table newTable = CreateTable(x, y, bTitle, bHeader);
            // Insert
            Paragraph paragraph = GetFirstLevelParagraphAtCaret();
            if (paragraph != null)    // Can be null if we are just inserting a table withoout any other content
            {
                DocumentText.Document.Blocks.InsertAfter(paragraph, newTable);
            }
            else
                DocumentText.Document.Blocks.Add(newTable);
        }
        internal void ExpandTableHorizontal()
        {
            // Add a column
            System.Windows.Documents.Table table = GetTableAtCaret();
            if (table != null)
            {
                AddTableColumn(table);
            }
            // Otherwise don't do any thing
        }
        internal void ExpandTableVertical()
        {
            // Add a row
            System.Windows.Documents.Table table = GetTableAtCaret();
            if (table != null)
            {
                AddTableRow(table);
            }
            // Otherwise don't do any thing
        }
        internal void ShrinkTableToTop()
        {
            // Remove a row
            System.Windows.Documents.Table table = GetTableAtCaret();
            if (table != null)
            {
                table.RowGroups[0].Rows.RemoveAt(table.RowGroups[0].Rows.Count - 1);
            }
            // Otherwise don't do any thing
        }
        internal void ShrinkTableToLeft()
        {
            // Remove a column
            System.Windows.Documents.Table table = GetTableAtCaret();
            if (table != null && table.Columns.Count - 1 != 0)
            {
                table.Columns.RemoveAt(table.Columns.Count - 1);

                // Also remove cells for each row
                foreach (TableRow row in table.RowGroups[0].Rows)
                {
                    // Determine type of row
                    if (row.Cells[0].ColumnSpan == table.Columns.Count + 1)
                    {
                        // Title row, just change span
                        row.Cells[0].ColumnSpan = table.Columns.Count;
                    }
                    else
                    {
                        // Header or content row, just remove a cell
                        row.Cells.RemoveAt(row.Cells.Count - 1);
                    }
                }
            }
            // Otherwise don't do any thing
        }
        public static System.Windows.Documents.Table CreateTable(int x, int y, bool bTitle = true, bool bHeader = true)
        {
            System.Windows.Documents.Table newTable = new System.Windows.Documents.Table();
            // Notice it's not possible to let tables auto-fit to content width, but it will always stretch to whole width by design
            // Ref: https://social.msdn.microsoft.com/Forums/vstudio/en-US/98348085-a1cb-414f-b082-5a9342ed174c/flowdocument-table-columns-autowidth?forum=wpf
            // Ref: https://stackoverflow.com/questions/1491285/wpf-flowdocument-table-autofit-option -- Using grid obviously doesn't solve the problem since that just makes things complicated
            // Ref: https://msdn.microsoft.com/en-us/library/system.windows.documents.flowdocument.columnwidth(v=vs.110).aspx -- Not related...

            #region Column Definitions
            // Generate columns
            int numberOfColumns = x;
            for (int i = 0; i < numberOfColumns; i++)
            {
                AddTableColumn(newTable);
            }
            #endregion

            // Generate Rows
            // Create and add an empty TableRowGroup to hold the table's Rows.
            newTable.RowGroups.Add(new TableRowGroup());
            TableRow currentRow; // Alias the current working row for easy reference.

            #region Title Row Definition
            // Add the first (title) row.
            if (bTitle)
            {
                newTable.RowGroups[0].Rows.Add(new TableRow());
                currentRow = newTable.RowGroups[0].Rows[newTable.RowGroups[0].Rows.Count - 1];  // Well we could use index 0 for that's the only possibility
                // Global formatting for the title row.
                FormatTitleRow(currentRow);
                // Add the header row with content, 
                currentRow.Cells.Add(new TableCell(new Paragraph(new Run(TableTitleRowDefaultText))));
                // and set the row to span all columns.
                currentRow.Cells[0].ColumnSpan = newTable.Columns.Count;
            }
            #endregion

            #region Header Row Definition
            // Add the second (header) row.
            if (bHeader)
            {
                newTable.RowGroups[0].Rows.Add(new TableRow());
                currentRow = newTable.RowGroups[0].Rows[newTable.RowGroups[0].Rows.Count - 1];
                // Global formatting for the header row.
                FormatHeaderRow(currentRow);
                // Add cells with content to the second row
                for (int i = 0; i < newTable.Columns.Count; i++)
                {
                    currentRow.Cells.Add(new TableCell(new Paragraph(new Run(TableHeaderRowDefaultText))));
                }
            }
            #endregion

            #region Content Rows Definition
            // Add content rows
            for (int i = 0; i < y; i++)
            {
                AddTableRow(newTable);
            }
            #endregion

            return newTable;
        }
        internal static System.Windows.Documents.Table CreateTable(MULTITUDE.Class.DocumentTypes.Table table)
        {
            System.Windows.Documents.Table newTable = new System.Windows.Documents.Table();

            #region Column Definitions
            // Generate columns
            int numberOfColumns = table.Headers.Count;
            for (int i = 0; i < numberOfColumns; i++)
            {
                AddTableColumn(newTable);
            }
            #endregion

            // Generate Rows
            // Create and add an empty TableRowGroup to hold the table's Rows.
            newTable.RowGroups.Add(new TableRowGroup());
            TableRow currentRow; // Alias the current working row for easy reference.

            #region Header Row Definition
            // Add the header row
            if (table.Headers.Count > 0)
            {
                newTable.RowGroups[0].Rows.Add(new TableRow());
                currentRow = newTable.RowGroups[0].Rows[newTable.RowGroups[0].Rows.Count - 1];
                // Global formatting for the header row.
                FormatHeaderRow(currentRow);
                foreach (string header in table.Headers)
                {
                    currentRow.Cells.Add(new TableCell(new Paragraph(new Run(header))));
                }
                #endregion
            }

            #region Content Rows Definition
            // Add content rows
            AddTableRow(newTable, table.Rows);
            #endregion

            return newTable;
        }
        // With style
        public static void AddTableColumn(System.Windows.Documents.Table table)
        {
            table.Columns.Add(new TableColumn());

            // Set alternating background colors for the middle colums.
            if ((table.Columns.Count - 1) % 2 == 0)
                table.Columns[table.Columns.Count - 1].Background = TableColumnsBackground1;
            else
                table.Columns[table.Columns.Count - 1].Background = TableColumnsBackground2;

            // Add cells to existing rows
            if (table.RowGroups.Count != 0)
            {
                foreach (TableRow row in table.RowGroups[0].Rows)
                {
                    // Determine type of row
                    if (row.Cells[0].ColumnSpan == table.Columns.Count - 1)
                    {
                        // Title row, just expand span
                        row.Cells[0].ColumnSpan = table.Columns.Count;
                    }
                    else if (row.FontSize == TableHeaderRowFontSize)
                    {
                        // Header row, add header row cell
                        row.Cells.Add(new TableCell(new Paragraph(new Run(TableHeaderRowDefaultText))));
                    }
                    else
                    {
                        // Add content Row Cell
                        row.Cells.Add(new TableCell(new Paragraph(new Run(TableContentRowDefaultText))));
                    }
                }
            }
        }
        // With style
        public static void AddTableRow(System.Windows.Documents.Table table)
        {
            TableRow currentRow;

            table.RowGroups[0].Rows.Add(new TableRow());
            currentRow = table.RowGroups[0].Rows[table.RowGroups[0].Rows.Count - 1];

            // Global formatting for the row.
            FormatContentRow(currentRow);

            // Add cells with content to this row
            for (int j = 0; j < table.Columns.Count; j++)
            {
                currentRow.Cells.Add(new TableCell(new Paragraph(new Run(TableContentRowDefaultText))));
            }

            // Global formatting for first row cell
            if (currentRow.Cells.Count > 0) FormatCell(currentRow.Cells[0]);
        }
        // With style
        public static void AddTableRow(System.Windows.Documents.Table table, List<List<string>> rows)
        {
            TableRow currentRow;

            foreach (List<string> row in rows)
            {
                table.RowGroups[0].Rows.Add(new TableRow());
                currentRow = table.RowGroups[0].Rows[table.RowGroups[0].Rows.Count - 1];
                // Global formatting for the row.
                FormatContentRow(currentRow);
                foreach (string content in row)
                {
                    currentRow.Cells.Add(new TableCell(new Paragraph(new Run(content))));
                }
                // Global formatting for first row cell
                if(currentRow.Cells.Count > 0) FormatCell(currentRow.Cells[0]);
            }
        }
        private System.Windows.Documents.Table GetTableAtCaret()
        {
            // Get table at location
            FrameworkContentElement block = DocumentText.CaretPosition.Paragraph;
            while (block != null && (block is System.Windows.Documents.Table) == false)
            {
                block = block.Parent as FrameworkContentElement;
            }
            if (block != null && block is System.Windows.Documents.Table)
            {
                return block as System.Windows.Documents.Table;
            }
            return null;
        }
        // Returns a at caret location if it is directly below the document
        private Paragraph GetFirstLevelParagraphAtCaret()
        {
            if (DocumentText.Document.Blocks.Contains(DocumentText.CaretPosition.Paragraph)) return DocumentText.CaretPosition.Paragraph;
            else return null;
        }
        public static void FormatTitleRow(TableRow row)
        {
            // Global formatting for the title row.
            row.Background = TableTitleRowBackground;
            row.FontSize = TableTitleRowFontSize;
            row.FontWeight = System.Windows.FontWeights.Bold;
        }
        public static void FormatHeaderRow(TableRow row)
        {
            // Global formatting for the header row.
            row.FontSize = TableHeaderRowFontSize;
            row.FontWeight = FontWeights.Bold;
        }
        public static void FormatContentRow(TableRow row)
        {
            // Global formatting for the row.
            row.FontSize = TableContentRowFontSize;
            row.FontWeight = FontWeights.Normal;
        }
        public static void FormatCell(TableCell cell)
        {
            // Bold the first cell.
            cell.FontWeight = FontWeights.Bold;
        }
        #endregion
        #region Advanced Table Manipulation
        public void SortAscendAtPlace()
        {
            // Search for table and current column

            // Sort and update whole table
        }
        public void SortDescendAtPlace()
        {
            // Search for table and current column

            // Sort and update whole table
        }
        public void AutoCompleteForTable()
        {
            // <Pending> ...

            // Also pending using Table.DataType to decide restraint keys if for dictionary or Value type data collection.
        }
        #endregion
        #endregion

        #region Shortcuts and Commands
        /// <summary>
        /// Shortcuts: Ctrl+123456789 for heading; Ctrl+BIUS for bold, italic, underline and strikethrough; Ctrl+T for a 2x2 table; number/` + space in a new line automatically creates a list
        /// Table operations: Since keyboard shortcuts for tables are hard to define and not intuitive to remember, we put that in popup UI; But for usability we also provided shortcuts using arrow keys
        /// Ohter default actions: Enter for a new paragraph, ShiftEnter for a new line in the same paragraph
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DocumentText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Keyboard shortcuts handling
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (ContentMode == ContentMode.TableOnly) return;
                // Variables
                Paragraph paragraph = DocumentText.CaretPosition.Paragraph;
                // Actions
                switch (e.Key)
                {
                    #region Heading Shortcuts
                    case Key.D1:
                        UpdateHeadingStyle(Heading1FontSize, Heading1Brush);
                        break;
                    case Key.D2:
                        UpdateHeadingStyle(Heading2FontSize, Heading2Brush);
                        break;
                    case Key.D3:
                        UpdateHeadingStyle(Heading3FontSize, Heading3Brush);
                        break;
                    case Key.D4:
                        UpdateHeadingStyle(Heading4FontSize, Heading4Brush);
                        break;
                    case Key.D5:
                        UpdateHeadingStyle(Heading5FontSize, Heading5Brush);
                        break;
                    case Key.D6:
                        UpdateHeadingStyle(Heading6FontSize, Heading6Brush);
                        break;
                    case Key.D7:
                        UpdateHeadingStyle(Heading7FontSize, Heading7Brush);
                        break;
                    case Key.D8:
                        UpdateHeadingStyle(Heading7PlusFontSize, Heading7PlusBrush);
                        break;
                    case Key.D9:
                        UpdateHeadingStyle(Heading7PlusFontSize, Heading7PlusBrush);
                        break;
                    #endregion
                    case Key.D:
                        ApplyDefaultStyle();
                        break;
                    case Key.S:
                        StrikethroughText();
                        break;
                    case Key.T:
                        InsertTable(2, 2);
                        break;
                    case Key.F:
                        // <Pending>
                        // ShowSearchBar();
                        break;
                    default:
                        return;
                }
                e.Handled = true;
            }
            // else if (Keyboard.IsKeyDown(Key.LeftAlt)) will not work
            else if (((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt))
            {
                switch (e.SystemKey)
                {
                    case Key.Left:
                        ShrinkTableToLeft();
                        break;
                    case Key.Right:
                        ExpandTableHorizontal();
                        break;
                    case Key.Up:
                        ShrinkTableToTop();
                        break;
                    case Key.Down:
                        ExpandTableVertical();
                        break;
                    default:
                        return;
                }
                e.Handled = true;
            }

            //#region Table Mode Validation
            //if (ContentMode == ContentMode.TableOnly)
            //{
            //    if (DocumentText.Selection.Start == DocumentText.Document.ContentStart && DocumentText.Selection.End == DocumentText.Document.ContentEnd) e.Handled = true;
            //}
            //#endregion

            #region List Creation Short-cut Handling
            if (ContentMode == ContentMode.TableOnly) return;
            // Notice we cannot do this in TextChanged because it's illegal to change text in textChanged
            // Notice we also cannot do this in PreviewTextChanged because even though it's legal to change text there, the event doesn't handle spaces
            string textInLine = DocumentText.CaretPosition.GetTextInRun(LogicalDirection.Backward);
            if (textInLine.Length == 3 && (textInLine[0] == '1' || textInLine[0] == '0') && textInLine[1] == '.' && textInLine[2] == ' ')
            {
                NumberedParagraph();
                DocumentText.CaretPosition.DeleteTextInRun(-textInLine.Length);
                // e.Handled = true;
            }
            if (textInLine.Length == 2 && textInLine[0] == '`' && textInLine[1] == ' ')
            {
                BulletedParagraph();
                DocumentText.CaretPosition.DeleteTextInRun(-textInLine.Length);
                // e.Handled = true;
            }
            if (textInLine.Length == 3 && textInLine[0] == 'a' && textInLine[1] == '.' && textInLine[2] == ' ')
            {
                LatinParagraph();
                DocumentText.CaretPosition.DeleteTextInRun(-textInLine.Length);
                // e.Handled = true;
            }
            #endregion
        }

        private void DocumentText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                double sizeChange = e.Delta > 0 ? 1 : -1;
                SetSelectionOrCaretFontSize(GetSelectionOrCaretFontSize() + sizeChange);
                e.Handled = true;
            }
        }
        #endregion

        #region Text Manipulation
        // Save at canvas space closed or some intervals upon text change
        private void DocumentText_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Implement autocomplete
            if(ContentMode == ContentMode.TableOnly)
            {
                // Table content validation
                if (DocumentText.Document.Blocks.Count != 1
                    || DocumentText.Document.Blocks.First() is System.Windows.Documents.Table == false)
                {
                    DocumentText.Document.Blocks.Clear();
                    InsertTable(2, 2, false);
                }

                AutoCompleteForTable();
            }
        }

        internal void Close()
        {
            if (Popup != null && Popup.Visibility == Visibility.Visible)
            { Popup.Visibility = Visibility.Collapsed; Popup.Close(); }
        }
        #endregion

        #region Popup Handling
        private void DocumentText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Popup != null && Popup.Visibility != Visibility.Visible)
            {
                Popup.Visibility = Visibility.Visible;
                Point point = Mouse.GetPosition(null);
                Popup.Left = point.X;
                Popup.Top = point.Y;
            }
            // e.Handled = true;
        }

        private void DocumentText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Popup != null && Popup.Visibility != Visibility.Hidden) Popup.Visibility = Visibility.Hidden;
        }
        #endregion

        #region UI Integration with Content Holder Controls
        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Focusable = true;
            this.DocumentText.Focusable = true;
            this.DocumentText.IsReadOnly = false;
            this.DocumentText.IsReadOnlyCaretVisible = false;
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Focusable = false;
            this.DocumentText.Focusable = false;
            this.DocumentText.IsReadOnly = true;
            this.DocumentText.IsReadOnlyCaretVisible = false;
        }
        #endregion
    }
}
