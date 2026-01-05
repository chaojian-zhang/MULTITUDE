using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// Represent search results, ready for data binding
    /// </summary>
    internal class ClueFragment: INotifyPropertyChanged
    {
        public ClueFragment(string name, int count, List<Document> relatedDocs)
        {
            _Name = name;
            _Count = count;
            _RelatedDocs = relatedDocs;
        }

        // Basic
        public string _Index;      // https://stackoverflow.com/questions/22378456/how-to-get-the-index-of-the-current-itemscontrol-item, so we need custom logic
        public string _Name;
        public int _Count;  // All documents categorized under that specific fragment (not in any sense a bigger clue)
        public List<Document> _RelatedDocs; // All related documents under complete clue

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string Index
        {
            get { return this._Index; }
            set
            {
                if (value != this._Index)
                {
                    this._Index = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Name
        {
            get { return this._Name; }
            set
            {
                if (value != this._Name)
                {
                    this._Name = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int Count
        {
            get { return this._Count; }
            set
            {
                if (value != this._Count)
                {
                    this._Count = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public List<Document> RelatedDocs
        {
            get { return this._RelatedDocs; }
            set
            {
                if (value != this._RelatedDocs)
                {
                    this._RelatedDocs = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion
    }
}