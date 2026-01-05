using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    public class PrimaryClueInfo : INotifyPropertyChanged
    {
        public PrimaryClueInfo(string name, bool primary, PrimaryClueInfo parent)
        {
            _Name = name;
            _Parent = parent;
            if (primary) _Clues = new ObservableCollection<PrimaryClueInfo>();
            else _Clues = null;
            _IsSelected = false;
            _IsExpanded = false;
        }

        public List<PrimaryClueInfo> Match(string text)
        {
            List<PrimaryClueInfo> found = new List<PrimaryClueInfo>();
            if (_Name .Contains(text)) found.Add(this);
            if(_Clues != null && _Clues.Count != 0)
            {
                foreach (PrimaryClueInfo clue in _Clues)
                {
                    if (clue._Name.Contains(text)) found.Add(clue);    // No recursing is needed because we have only two layers
                }
            }
            return found;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _Name;
        private ObservableCollection<PrimaryClueInfo> _Clues;      // Though defined as PrimaryClueInfo, we only just want to use its IsSelected property, and we have only two layers
        private PrimaryClueInfo _Parent;    // For primary clue, parent is null
        private bool _IsSelected;
        private bool _IsExpanded;

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
        public ObservableCollection<PrimaryClueInfo> Clues
        {
            get { return this._Clues; }
            set
            {
                if (value != this._Clues)
                {
                    this._Clues = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsSelected
        {
            get { return this._IsSelected; }
            set
            {
                if (value != this._IsSelected)
                {
                    this._IsSelected = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsExpanded
        {
            get { return this._IsExpanded; }
            set
            {
                if (value != this._IsExpanded)
                {
                    this._IsExpanded = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public PrimaryClueInfo Parent
        {
            get { return this._Parent; }
            set
            {
                if (value != this._Parent)
                {
                    this._Parent = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

    /// <summary>
    /// A helper construct to facilitate in document categorization and searching; Does not store clues and their relations to documents directly (unaware of underlying clue storage structure) because the strucutre(implementation) is likely to change in the furture, so we use generic interface to perform such operations
    /// General usage guidance: Generally our user interaction should only do exact clue search, and we somehow work around that to enable users to have some ambiguity
    /// </summary>
    [Serializable]
    internal class ClueManager
    {
        #region States
        public static ClueManager Manager;  // A global reference to this instance

        public ClueManager()
        {
            Manager = this;
            ClueTree = new Dictionary<string, ClueTreeNode>();
        }
        public Dictionary<string, ClueTreeNode> ClueTree { get; set; }

        // Allows for partial searching: both for ambiguity, and for begin at center of a clue (this support will greatly increase the complexity of existing algorithms because a subclue can belong to many primary clues and this makes giving suggestions complicated; It can also cause confusion since which primary clue do we use anyway?)
        // public MultiDictSet<string, ClueTreeNode> Fragments { get; set; }
        #endregion

        #region Clue Management Interface: Notice we only manage clues themselves, not taking care of updating documents
        // Record a clue
        public void AddClue(Clue clue, Document doc)
        {
            string[] fragments = clue.Fragments;

            if (ClueTree.ContainsKey(fragments[0]) == false) ClueTree[fragments[0]] = new ClueTreeNode(fragments[0], null); 

            ClueTree[fragments[0]].AddDocument(fragments, 0, doc);
        }

        public void AddAllClues(Document doc)
        {
            foreach (Clue clue in doc.Clues)
            {
                AddClue(clue, doc);
            }
        }

        public void AddClue(Clue clue, List<Document> docs)
        {
            string[] fragments = clue.Fragments;

            if (ClueTree.ContainsKey(fragments[0]) == false) ClueTree[fragments[0]] = new ClueTreeNode(fragments[0], null);

            ClueTree[fragments[0]].AddDocument(fragments, 0, docs);
        }

        // Return affected documents: Notice for alias affected documents is only up until current node, not children nodes
        public List<Document> AddClueAlias(Clue oldClue, Clue newAlias)
        {
            List<Document> documents = GetDocuments(oldClue);
            AddClue(newAlias, documents);

            return documents;
        }

        public void RemoveClue(Clue clue, Document doc)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) throw new InvalidOperationException("The specified document hasn't been categorized under this clue");
            ClueTree[fragments[0]].RemoveDocument(fragments, 0, doc);
        }

        // Remove all cllues of a document from Clue manager, the clues of document itself is, naturally, managed by document itself, not by ClueManager
        public void RemoveAllClues(Document doc)
        {
            foreach (Clue clue in doc.Clues)
            {
                RemoveClue(clue, doc);
            }
        }

        /// <summary>
        /// Clue can begin with primary fragment or be subclue (i.e. middle or trailing sections) of any primary clue
        /// </summary>
        /// <param name="clue"></param>
        /// <returns>return empty not null if not found</returns>
        public List<Document> GetDocuments(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) return new List<Document>();
            else return ClueTree[fragments[0]].GetDocuments(fragments, 0);
        }

        public List<Document> GetDocuments(List<Clue> clues)
        {
            List<Document> documents = new List<Document>();
            foreach (Clue clue in clues)
            {
                documents.AddRange(GetDocuments(clue));
            }
            return documents.Distinct().ToList();
        }

        public void ChangeClue(Clue oldClue, Clue newClue, Document doc)
        {
            RemoveClue(oldClue, doc);
            AddClue(newClue, doc);
        }
        // Return affected documents: only immediate documents, i.e. those who are actually specified under that exact clue is affected
        public List<Document> ChangeClue(Clue oldClue, Clue newClue)
        {
            // <Development> Multithread it and I believe the speech will be moderate; Consider parallel LInq

            // Remove documents from old node
            ClueTreeNode node = GetTreeNode(oldClue);
            List<Document> temp = node.Documents;
            node.Documents = new List<Document>();

            // Remove old node from parent if it's empty
            if(node.Branches.Count == 0)
            {
                if (node.Parent == null) ClueTree.Remove(node.Name); 
                else node.Parent.Branches.Remove(node.Name);
            }

            // Move those documents to a new node
            ClueTreeNode newNode = CreateTreeNode(newClue);
            newNode.Documents = temp;
            
            return temp;
        }

        public ClueTreeNode GetTreeNode(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) return null;
            return ClueTree[fragments[0]].GetTreeNode(fragments, 0);
        }

        public ClueTreeNode CreateTreeNode(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) ClueTree[fragments[0]] = new ClueTreeNode(fragments[0], null);
            return ClueTree[fragments[0]].CreateTreeNode(fragments, 0);
        }

        public ClueTreeNode GetParentTreeNode(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) return null;
            return ClueTree[fragments[0]].GetParentTreeNode(fragments, 0);
        }

        /// <summary>
        /// Exclusive
        /// </summary>
        /// <param name="clue"></param>
        /// <returns></returns>
        public List<string> GetPossibleFragments(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (fragments.Length == 1)
                return ClueTree.Keys.Where(item => item.Contains(fragments[0]) && item != fragments[0]).ToList();
            else
            {
                if (ClueTree.ContainsKey(fragments[0])) return ClueTree[fragments[0]].GetPossibleFragments(fragments, 0);
                else return null;
            }
        }

        /// <summary>
        /// Exclusive
        /// </summary>
        /// <param name="clue"></param>
        /// <returns></returns>
        public List<ClueFragment> GetPossibleClueFragments(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (fragments.Length == 1)
            {
                string[] possibleFragments = ClueTree.Keys.Where(item => item.Contains(fragments[0]) && item != fragments[0]).ToArray();
                List<ClueFragment> results = new List<ClueFragment>();
                foreach (string frag in possibleFragments)
                {
                    results.Add(new ClueFragment(frag, ClueTree[frag].Documents.Count, ClueTree[frag].Documents));
                }
                return results;
            }
            else
            {
                if (ClueTree.ContainsKey(fragments[0])) return ClueTree[fragments[0]].GetPossibleClueFragments(fragments, 0);
                else return null;
            }
        }
        #endregion

        #region Search Interface - General Search Functions
        public List<string> GetAllPrimaryFragments()
        {
            return ClueTree.Keys.ToList();
        }

        public List<string> GetAllClueStrings()
        {
            List<Clue> results = new List<Clue>();
            Home home = (App.Current as App).CurrentHome;
            foreach (Document doc in home.Documents)
            {
                results.AddRange(doc.Clues);
            }

            return results.Distinct().Select(item => item.ToString()).ToList();
        }

        public ObservableCollection<PrimaryClueInfo> GetPrimaryClueInfo()
        {
            ObservableCollection<PrimaryClueInfo> primaryClues = new ObservableCollection<PrimaryClueInfo>();

            foreach (string primaryFragment in ClueTree.Keys)
            {
                PrimaryClueInfo newPrimaryClue = new PrimaryClueInfo(primaryFragment, true, null);
                primaryClues.Add(newPrimaryClue);
                foreach (string clue in ClueTree[primaryFragment].GetAllClues())
                {
                    if(clue != primaryFragment) // GetAllClues() can return just self if that's all it has (i.e. the fragment has no branches, i.e. the primary fragments themselves)
                        newPrimaryClue.Clues.Add(new PrimaryClueInfo(clue, false, newPrimaryClue));
                }
            }

            return primaryClues;
        }

        /// <summary>
        /// Get all document with specific type under that clue
        /// </summary>
        /// <param name="clue"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Document> GetDocumentsFilterByType(Clue clue, DocumentType type)
        {
            return GetDocuments(clue).Where(a => a.Type == type).ToList();
        }

        public List<Document> GetDocumentsFilterByType(Clue clue, List<DocumentType> types)
        {
            return GetDocuments(clue).Where(a => types.Any(type => type == a.Type)).ToList();
        }

        public List<Document> GetDocumentsFilterByType(List<DocumentType> types)
        {
            return Home.Current.Documents.Where(a => types.Any(type => type == a.Type)).ToList();
        }

        public List<Document> GetDocumentsFilterByType(List<Clue> clues, DocumentType type)
        {
            return GetDocuments(clues).Where(a => a.Type == type).ToList();
        }


        public List<Document> GetDocumentsFilterByType(List<Clue> clues, List<DocumentType> types)
        {
            return GetDocuments(clues).Where(a => types.Any(type => type == a.Type)).ToList();
        }

        public List<Document> GetDocumentsAndCluesFilterByType(out List<Clue> clues, DocumentType type)
        {
            List<Document> Documents = Home.Current.Documents.Where(a => a.Type == type).ToList();
            clues = new List<Clue>();
            foreach (Document doc in Documents)
            {
                clues.AddRange(doc.Clues);
            }
            clues = clues.Distinct().ToList();
            return Documents;
        }

        /// <summary>
        /// Get documents that satisfies all clues
        /// </summary>
        /// <param name="clues"></param>
        /// <returns></returns>
        public List<Document> GetUnion(List<Clue> clues)
        {
            List<Document> documents = new List<Document>();
            foreach (Clue clue in clues)
            {
                documents.AddRange(GetDocuments(clue));
            }
            return documents.Distinct().ToList();
        }

        public List<Document> GetIntersect(List<Clue> clues)
        {
            IEnumerable<Document> documents = new List<Document>();
            documents = GetDocuments(clues[0]);
            for (int i = 1; i < clues.Count; i++)
            {
                documents = documents.Intersect(GetDocuments(clues[i]));
            }
            
            return documents.ToList();
        }

        internal void GetSiblings(Clue clue, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) throw new IndexOutOfRangeException("The partial clue doesn't exit.");

            // Retrieve
            foundDocuments = ClueTree[fragments[0]].GetDocuments(fragments, 0);
            ClueTreeNode note = GetParentTreeNode(clue);
            nextFragments = note.GetClueFraments();
        }

        internal void GetBranches(Clue clue, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) throw new IndexOutOfRangeException("The partial clue doesn't exit.");

            // Retrieve
            foundDocuments = ClueTree[fragments[0]].GetDocuments(fragments, 0);
            ClueTreeNode note = GetTreeNode(clue);
            nextFragments = note.GetClueFraments();
        }

        internal List<ClueFragment> GetBranches(Clue clue)
        {
            string[] fragments = clue.Fragments;
            if (ClueTree.ContainsKey(fragments[0]) == false) throw new IndexOutOfRangeException("The partial clue doesn't exit.");

            // Retrieve
            ClueTreeNode note = GetTreeNode(clue);
            return note.GetClueFraments();
        }
        #endregion

        #region Search Functions - Return results for UI display
        // Search GUID form (Absolute Addressing): ID0000[ContentElement]
        // CA can be null
        public void SearchByID(int ID, string CA, out List<Document> foundDocuments)
        {
            if(CA == null)
            {
                Document doc = (App.Current as App).CurrentHome.GetDocument(ID);
                foundDocuments = new List<Document>();
                foundDocuments.Add(doc);
            }
            else
            {
                // <Development> When CA is not null, do a deep search
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Given a bunch of keyphrases, suggest found documents and next steps
        /// </summary>
        /// <param name="keyPhrases"></param>
        /// <param name="nextFragments"></param>
        /// <param name="foundDocuments"></param>
        public void SearchForClueFragments(Clue clue, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        {
            // If we have an exact match, then foundDocuments are results of that match, and nextFragments are sibglings of that match
            ClueTreeNode node = GetTreeNode(clue);
            if(node != null)
            {
                foundDocuments = node.Documents;
                nextFragments = node.GetClueFraments();
            }
            // If we don't have an exact match, then foundDocuments are none, and nextFragmetns are possible fragments to use
            else
            {
                foundDocuments = null;  // Or empty
                nextFragments = GetPossibleClueFragments(clue);
            }
        }

        // Search Shorthand clue form
        public void SearchBySimpleClue(Clue clue, string CA, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        {
            if (CA == null)
            {
                // Do a first order search
                SearchForClueFragments(clue, out nextFragments, out foundDocuments);
                if((nextFragments == null || nextFragments.Count == 0) &&
                    (foundDocuments == null || foundDocuments.Count == 0))
                {
                    // Do a second order search
                    List<string> temp = clue.Fragments.ToList();
                    temp.RemoveAt(clue.Fragments.Length - 1);
                    Clue trimmed = new Clue(temp.ToArray());
                    SearchForClueFragments(trimmed, out nextFragments, out foundDocuments);

                    // Do a third order search
                    string lastFragment = clue.Fragments[clue.Fragments.Length - 1];
                    if (foundDocuments == null || foundDocuments.Count == 0) nextFragments = nextFragments.Where(item => item.Name.IndexOf(lastFragment) == 0).ToList();
                    else foundDocuments = foundDocuments.Where(item => item.IsPartialMetaNameOrValue(lastFragment)).ToList();
                }
            }
            else
            {
                // <Development> When CA is not null, do a deep search
                throw new NotImplementedException();
            }
        }

        // Search Constriant form (Conditional Match): A-B-C;A-B;…#metaname#metaname(=partial)@metavalue[ContentElement]"keyword" Where for meta section order doesn’t matter; "keyword" means non-specified location; =partial defines a keyword for mata, otherwise existence is enough (exact)
        public void SearchByGeneralConstraints(string[] clueStrings, string CA, string keyword, string[] metakeys, string[] metavalues, out List<ClueFragment> nextClues, out List<Document> foundDocuments)
        {
            if(CA == null)
            {
                if (CA == null && keyword == null && metakeys == null && metavalues == null)
                {
                    IEnumerable<Document> totalFound = new List<Document>();
                    List<ClueFragment> foundClues = null;
                    foreach (string clue in clueStrings)
                    {
                        SearchForClueFragments(new Clue(clue), out foundClues, out foundDocuments);
                        totalFound = totalFound.Intersect(foundDocuments);
                    }
                    foundDocuments = totalFound.ToList();
                    nextClues = foundClues;
                }
                else
                {
                    nextClues = null;
                    foundDocuments = GetIntersect(clueStrings.Select(item => new Clue(item)).ToList()).
                                           Where(item => item.IsMetanamePresent(metakeys) && item.IsPartialMetaValue(metavalues)).
                                           Where(item => item.IsValueAnywherePresent(keyword)).ToList();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // Provding some suggestions on possible clues to use
        // Condition: (input.Contains('-') == false && input.Contains(' ') == false, i.e. beginning entry of a word
        // Rule: treat as a part of a clue, then treat as a name or meta
        public void GetInitialSuggestion(string beginningText, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        {
            Clue clue = new Clue(beginningText);
            foundDocuments = null;
            if (ClueTree.ContainsKey(beginningText)) foundDocuments = GetDocuments(clue);
            if (foundDocuments == null || foundDocuments.Count == 0) AmbiguousSearch(new string[] { beginningText }, out foundDocuments);

            if (ClueTree.ContainsKey(beginningText)) nextFragments = GetBranches(clue);
            else nextFragments = GetPossibleClueFragments(clue);
        }

        // Search Ambiguous: a space demilited list of key words; Search into clues, names and comment; not searching other meta
        // Functions just like Quick Match, with a bit more flexibilily provided by keywords
        // When CA is not null, it searches content as well
        // Strategy: count hit points and order all available documents that has gained any hit point
        // Users should be really assured that unless a must match is specified, we might produce results that makes no sense
        public void AmbiguousSearch(string[] keywords, out List<Document> foundDocuments)
        {
            // Get must match keywords
            List<string> mustMatches = new List<string>(), optionalMatches = new List<string>();
            foreach (string keyword in keywords)
            {
                if (keyword[0] == '!')
                    mustMatches.Add(keyword.Substring(1));
                else
                    optionalMatches.Add(keyword);
            }

            // First we get an array of all documents that matches the must match keywords (if any) - compare clues, metas(names, comment etc.) and content
            Home home = (App.Current as App).CurrentHome;
            List<Document> satisfyingDocuments;
            if (mustMatches.Count > 0) satisfyingDocuments = home.Documents.Where(item => item.IsValueAnywherePresent(mustMatches.ToArray())).ToList();
            else satisfyingDocuments = home.Documents;  // Notice search doesn't count those in forgotten universe, also ignoring void universe.

            // Then we compare the result of the keywords and order by hit count (notice optionalmatches can be absent, so we cannot just select)
            foreach (string match in optionalMatches)
            {
                foreach (Document doc in satisfyingDocuments)
                {
                    if (doc.IsValueAnywherePresent(match)) doc.KeywordOccurences++;
                    else doc.KeywordMisses++;
                }
            }
            // Generate final list
            List<Document> orderedList = (satisfyingDocuments.OrderByDescending(x => (x.KeywordOccurences - x.KeywordMisses)).ToList());

            // Clean search state
            foreach (Document doc in satisfyingDocuments)
            {
                doc.KeywordOccurences = 0;
                doc.KeywordMisses = 0;
            }

            // Return search result (only the former 30)
            foundDocuments = orderedList.GetRange(0, orderedList.Count > 30 ? 30 : orderedList.Count);
        }
        #endregion
    }
}
