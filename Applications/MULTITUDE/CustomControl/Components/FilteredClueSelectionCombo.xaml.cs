using MULTITUDE.Class.DocumentTypes;
using MULTITUDE.Class.Facility;
using MULTITUDE.Class.Facility.ClueManagement;
using System;
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

namespace MULTITUDE.CustomControl.Components
{
    /// <summary>
    /// FilteredClueSelectionCombo to search for clues which contains images, or videos and other media files which matches target type
    /// </summary>
    public partial class FilteredClueSelectionCombo : UserControl, INotifyPropertyChanged
    {
        public FilteredClueSelectionCombo()
        {
            InitializeComponent();
            AvailableClueStrings = new ObservableCollection<string>();
        }

        #region Interaction Logic
        private List<Document> FoundDocuments;
        private List<Clue> AvailableClues;
        private DocumentType Type;
        /// <summary>
        /// Given some clues, fetch target type locations under those clues
        /// If null, fetch clues under home
        /// </summary>
        internal void Update(List<Clue> cluesSetup, DocumentType type)
        {
            AvailableClueStrings = new ObservableCollection<string>();
            Type = type;
            if (cluesSetup != null)
            {
                // Search using given clues
                FoundDocuments = ClueManager.Manager.GetDocumentsFilterByType(cluesSetup, type);
                AvailableClues = cluesSetup;
            }
            else
            {
                // Search using home
                ClueManager.Manager.GetDocumentsAndCluesFilterByType(out AvailableClues, type);
                FoundDocuments = new List<Document>();  // Make it empty
            }

            // Update clues combo and add all as selected
            foreach (Clue clue in AvailableClues)
            {
                // Combo
                AvailableClueStrings.Add(clue.Name);

                // List
                if (cluesSetup != null) // Select available clues only if we have a solid target; otherwise there might be too many things to load
                {
                    ListBoxItem item = new ListBoxItem();
                    item.Content = clue.Name;
                    SelectedClues.Items.Add(item);
                }
            }

            // Invoke External Handlers to do an update
            ClueFilterUpdatedEvent.Invoke(FoundDocuments, AvailableClues);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClueSelectionComboBox.SelectedItem == null) return;

            // Add to selection if not already exist
            string selectedClue = ClueSelectionComboBox.SelectedItem as string;
            foreach (ListBoxItem item in SelectedClues.Items)
            {
                if ((string)item.Content == selectedClue) return;
            }
            // Add
            ListBoxItem newItem = new ListBoxItem();
            newItem.Content = selectedClue;
            SelectedClues.Items.Add(newItem);

            SearchUsingSelectedClues();
        }

        private void SelectedClues_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Remove from interface
            SelectedClues.Items.Remove((sender as ListBox).SelectedItem as ListBoxItem);

            SearchUsingSelectedClues();
        }

        private void SearchUsingSelectedClues()
        {
            // Invoke External Handlers to do an update
            List<Clue> availableClues = new List<Clue>();
            foreach (ListBoxItem item in SelectedClues.Items)
            {
                availableClues.Add(new Clue(item.Content as string));
            }
            List<Document> foundDocuments = ClueManager.Manager.GetDocumentsFilterByType(availableClues, Type);
            ClueFilterUpdatedEvent.Invoke(foundDocuments, availableClues); 
            
            // <Development> Seem not very economical
        }
        #endregion

        #region Interface Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<string> _AvailableClueStrings;

        public ObservableCollection<string> AvailableClueStrings
        {
            get { return _AvailableClueStrings; }
            set
            {
                if (value != this._AvailableClueStrings)
                {
                    this._AvailableClueStrings = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #region Events Exposing
        // Event type definition
        internal delegate void ClueFilterEventHandler(List<Document> TargetLocations, List<Clue> availableClues);
        // Expose event for hooking up
        internal event ClueFilterEventHandler ClueFilterUpdatedEvent;
        #endregion
    }
}
