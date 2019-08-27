using MULTITUDE.Class;
using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using System;
using System.Collections;
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

    /// <summary>
    /// Interaction logic for ClueBrowser.xaml
    /// </summary>
    public partial class ClueBrowser : UserControl
    {
        public ClueBrowser(MULTITUDE.Canvas.VirtualWorkspaceWindow summoner)
        {
            InitializeComponent();
            Summoner = summoner;
        }

        private MULTITUDE.Canvas.VirtualWorkspaceWindow Summoner { get; }

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
            CluesListBox.ItemsSource = ClueHelper.ClueManager.AllClues;    // Seems this is observable so automatically update add event, e.g. during creating alias?
            // It is said we could give the user control a name then use ItemsSource="{Binding Clues, ElementName=ClueBrowserUserControl}" but not working
            // The same for ItemsSource="{Binding ExpandedSecondaryClue, ElementName=ClueBrowserUserControl}" for ExpandedSecondaryClueItemsControl so we did it procedurally
        }

        private void ClueGroupButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #region Clue Space Navigation
        private void ClueSpaceClueNameTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (sender as TextBox).IsReadOnly = false;
        }

        private void ClueSpaceClueNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            FinishEditing(sender as TextBox);
        }

        private void FinishEditing(TextBox box)
        {
            // Save change
            ClueHelper.ClueManager.ChangeExistingClue(box.DataContext as string, box.Text); // Two way binding doesn't affect original value

            // Disable editing
            box.IsReadOnly = true;
        }

        private void ClueSpaceClueNameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                FinishEditing(sender as TextBox);
            }
        }
        #endregion

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Summoner.CloseCanvasSpace(this);
        }

        private void CluesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selection = CluesListBox.SelectedItem as string; // Pending studying: diff between SelectedItem and SelectedValue

            // Find all documents under that clue, then extract all their shared clues, then construct ClueFragments
            // There are two cases:
            // 1. Selection is a single fragment: there are two sub cases - it just stands alone, - it belongs to some fragment set
            // 2. Selection is a fragment set: there are two sub cases - it's a max set - it is a subset of other bigger fragments
            List<ClueFragment> nextFragments;
            List<Document> foundDocuments;
            ClueHelper.ClueManager.SearchForClueFragments(0, ClueHelper.SeperateClueFragments(selection).ToArray(), out nextFragments, out foundDocuments, true);
            ClueGroupsItemsControl.ItemsSource = nextFragments;
            MatchingDocumentsList.ItemsSource = foundDocuments;
        }
    }
}
