using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MULTITUDE.Class.DocumentTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace MULTITUDE.Class
{
    /// <summary>
    /// Represent search results
    /// </summary>
    internal class ClueFragment
    {
        public ClueFragment(string name, int count, string completeClue, List<Document> relatedDocs)
        {
            Name = name;
            Count = count;
            CompleteClue = completeClue;
            RelatedDocs = relatedDocs;
        }

        // Basic
        public string Index { get; set; }      // https://stackoverflow.com/questions/22378456/how-to-get-the-index-of-the-current-itemscontrol-item, so we need custom logic
        public string Name { get; set; }
        public int Count { get; set; }
        // Auxiliary
        public string CompleteClue { get; set; }    // Complete clue sofar leading to current clue fragment
        public List<Document> RelatedDocs { get; set; } // All related documents under complete clue
    }

    [Serializable]
    class MultiDict<TKey, TValue>  // no (collection) base class
    {
        private Dictionary<TKey, List<TValue>> _data = new Dictionary<TKey, List<TValue>>();

        public void Add(TKey k, TValue v)
        {
            // can be a optimized a little with TryGetValue, this is for clarity
            if (_data.ContainsKey(k))
                _data[k].Add(v);
            else
                _data.Add(k, new List<TValue>() { v });
        }

        public List<TValue> Get(TKey k)
        {
            if (_data.ContainsKey(k) == true) return _data[k];
            else return null;
        }

        // more members
        // Ref: https://stackoverflow.com/questions/3850930/multi-value-dictionary
    }

    // Represetn a clue tree node: this should better be constructed during loading rather than serialized
    class ClueNode
    {
        public ClueNode()
        {
            Children = new Dictionary<string, ClueNode>();
            References = new List<Document>();
        }

        public Dictionary<string, ClueNode> Children { get; set; }
        public List<Document> References { get; set; }  // Reference to documents at current tree level

        public ClueNode GetNode(string[] clueFragments, int currentIndex)
        {
            // If we are the last cluefragment, then just return
            if (clueFragments.Length - 1 == currentIndex)
                return this;
            else
            {
                // If name doesn't exist
                if (Children.ContainsKey(clueFragments[currentIndex + 1]) == false)
                    return null;
                else
                    return Children[clueFragments[currentIndex + 1]].GetNode(clueFragments, currentIndex + 1);
            }
        }
        public List<Document> GetDocuments(string[] clueFragments, int currentIndex)
        {
            // If we are the last cluefragment, then just return
            if (clueFragments.Length - 1 == currentIndex)
                return References;
            else
            {
                // If name doesn't exist
                if (Children.ContainsKey(clueFragments[currentIndex + 1]) == false)
                    return null;
                else
                    return Children[clueFragments[currentIndex + 1]].GetDocuments(clueFragments, currentIndex + 1);
            }
        }
        public void AddDocument(string[] clueFragments, int currentIndex, Document doc)
        {
            // If we are the last cluefragment, then just add the doc
            if(clueFragments.Length - 1 == currentIndex)
                References.Add(doc);    // Notice not checking existence, assumed not existing
            else
            {
                if (Children.ContainsKey(clueFragments[currentIndex + 1]) == false)
                    Children[clueFragments[currentIndex + 1]] = new ClueNode();
                Children[clueFragments[currentIndex + 1]].AddDocument(clueFragments, currentIndex + 1, doc);
            }
        }
    }

    // Represents a home
    [Serializable]
    class Home
    {
        public Home(string location)
        {
            Location = location;
            _CustomDictionary = new Dictionary<string, string>();
            _Documents = new List<Document>();
            _VirtualWorkspaces = new List<VirtualWorkspace>();
            _ForgottenUniverse = new List<Document>();
            _VoidUniverse = new List<Document>();
            _LinkUniverse = new List<Document>();

            _CluePairs = new MultiDict<string, string>();
            Clues = new Dictionary<string, ClueNode>();
            Links = new Dictionary<string, ClueNode>();
            CluesList = new SortedSet<string>();
        }

        #region Home Data
        // Contents, seralized
        public string Location { get; set; }    // Home location; All documents have their location + home location to get their actual file location
        private Dictionary<string, string> _CustomDictionary;   // User defined dictionary to provided customized translation between key phrases; format: phrase = phrase, with spaces trimmed
        private List<Document> _Documents;  // A list of all documents in current home
        private List<VirtualWorkspace> _VirtualWorkspaces;
        // Lists of specialized objects, those are just references (indexes) from above
        private List<Document> _ForgottenUniverse;     // Objects that are not classfied or placed on VW or linked to (i.e. not in link universe)
        private List<Document> _VoidUniverse;   // Objects that are removed yet not deleted from file system
        // Below are book keeping information that can be serialized or not (and generated during loading) if we want to save space
        private List<Document> _LinkUniverse;    // Collection of document with links; Used for validation purpose only when a document is removed; Links form a link universe
        // Property wrappers
        public Dictionary<string, string> CustomDictionary { get { return _CustomDictionary; } }    // Might not want to enable this because synonyms means ambiguity, and also this is designed for "nickname" usage we cannot prevent users from populating it with words of similar meaning, which isn't needed since that's the job for Airi and that makes the whole clue system undetermined; In which case a substitution for this function would be to either use a dedicated clue or a meta to tag a document
        public List<Document> Documents { get { return _Documents; } }  // User shouldn't call Add() or otherwise modify this property directly
        public List<VirtualWorkspace> VirtualWorkspaces { get { return _VirtualWorkspaces; } }
        public List<Document> ForgottenUniverse { get { return _ForgottenUniverse; } }
        public List<Document> VoidUniverse { get { return _VoidUniverse; } }
        public List<Document> LinkUniverse { get { return _LinkUniverse; } }
        #endregion Home Data

        // Should be async ready; <Development> Might not want to make those Serializable
        #region Clue Search Helper Constructs
        private MultiDict<string, string> _CluePairs; // Collection of clues, Used for aiding in auto-completion only
        public MultiDict<string, string> CluePairs { get { return _CluePairs; } }
        public static readonly string dashEscapeSymbol = "~%D&!";  // This isn't necessary for clues normally won't contain escape (if its generated automatically), but we should support it anyway
        private Dictionary<string, ClueNode> Clues { get; set; } // All existing clues assigned to numerous documents; Main acceleration structure for clued search
        public SortedSet<string> CluesList { get; }  // A straight ordered list of all current active clues; Notice set contain no duplicates, unlike set

        // Seperate a clue into different phrases, with -- escaped
        private string[] SeperateClueFragments(string clue) // Escape when necessary, Also does a ToLower() operation
        {
            string escaped = clue.ToLower().Replace("--", dashEscapeSymbol);
            string[] fragments = escaped.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = fragments[i].Replace(dashEscapeSymbol, "-");
            }
            return fragments;
        }
        public void AddNewCluePairs(string clue)
        {
            string[] clueFragments = SeperateClueFragments(clue);
            for (int i = clueFragments.Length - 1; i > 0; i--)
            {
                string formerClue = clueFragments[i - 1];
                string latterClue = clueFragments[i];
                CluePairs.Add(formerClue, latterClue);
            }
        }
        private void GenerateNewCluePairs(List<string> clues)  // Generate new pairs from a given clue
        {
            foreach (string clue in clues)
            {
                AddNewCluePairs(clue);
            }
        }
        private void AddDocumentClues(Document doc)
        {
            foreach (string clue in doc.Clues)
            {
                // Add to clue tree
                AddDocumentToClue(clue, doc);

                // Add to clue list, in order; Ref: https://stackoverflow.com/questions/12172162/how-to-insert-item-into-list-in-order
                CluesList.Add(clue);
            }
        }
        public void AddDocumentToClue(string clue, Document doc)
        {
            string[] clueFragments = SeperateClueFragments(clue);
            if (Clues.ContainsKey(clueFragments[0]) == false)
                Clues[clueFragments[0]] = new ClueNode();
            Clues[clueFragments[0]].AddDocument(clueFragments, 0, doc);
        }
        public List<Document> GetDocumentsFromClue(string clue)
        {
            string[] clueFragments = SeperateClueFragments(clue);
            return GetDocumentsFromPhrases(clueFragments);
        }
        public List<Document> GetDocumentsFromPhrases(string[] cluePhrases)
        {
            if (Clues.ContainsKey(cluePhrases[0]) != false)
                return Clues[cluePhrases[0]].GetDocuments(cluePhrases, 0);
            else
                return null;
        }
        public ClueNode GetClueNodeFromPhrases(string[] cluePhrases)
        {
            if (Clues.ContainsKey(cluePhrases[0]) != false)
                return Clues[cluePhrases[0]].GetNode(cluePhrases, 0);
            else
                return null;
        }
        public List<ClueNode> GetClueNodeFromPartialPhrase(string[] cluePhrases)  // Compared with GetClueNodeFromPhrases(), this function doesn't assume the last keyword phrase to be exact
        {
            // Make a partial copy
            string[] partialPhrases = null;
            Array.Copy(cluePhrases, partialPhrases, cluePhrases.Length - 1);
            // Get node up till that partial copy
            ClueNode node = null;
            if (Clues.ContainsKey(partialPhrases[0]) != false)
                node = Clues[partialPhrases[0]].GetNode(partialPhrases, 0);
            else
                return null;
            // Check result
            if (node != null)
            {
                // Find partial matches
                List<ClueNode> partialMatches = new List<ClueNode>();
                string partialPhrase = cluePhrases[cluePhrases.Length - 1];
                foreach (KeyValuePair<string, ClueNode> entry in node.Children)
                {
                    if(entry.Key.IndexOf(partialPhrase) == 0)
                        partialMatches.Add(entry.Value);
                }
                if (partialMatches.Count != 0) return partialMatches;
                else return null;
            }
            else
                return null;
        }

        // Search functions
        public Document GetDocument(int ID)
        {
            if (Documents.Count >= ID)
                return Documents[ID];
            else
                return null;
        }
        /// <summary>
        /// From existing clues find corresponding documents, and also provide a hint about next available clue; Do not return non-existing clues
        /// </summary>
        /// /// <param name="cursorLocation">Which keyPhrase is current cursor on, this is used for more intelligent auto-complete</param>
        /// <param name="keyPhrases"></param>
        /// <param name="nextClues"></param>
        /// <param name="foundDocuments"></param>
        public void SearchExistingClues(int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextClues, out List<Document> foundDocuments)
        {
            // <Dev> Pending implementation cursorLocation

            // Genereate exact clues return
            ClueNode clueNode = GetClueNodeFromPhrases(keyPhrases);
            string currentClueString = string.Join("-", keyPhrases);
            if (clueNode != null)
            {
                // Get next clues
                Dictionary<string, ClueNode> clueNodeChildren = clueNode.Children;
                nextClues = new List<ClueFragment>();
                foreach (KeyValuePair<string, ClueNode> entry in clueNodeChildren)
                {
                    List<Document> clueDocuments = entry.Value.References;
                    nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
                }

                // Get found documents
                foundDocuments = clueNode.References;
            }
            else
            {
                nextClues = null;
                foundDocuments = null;
            }

            // Try again by generating partial clues match return
            if(nextClues == null && foundDocuments == null && keyPhrases.Length > 1)
            {
                List<ClueNode> partialMatches = GetClueNodeFromPartialPhrase(keyPhrases);
                currentClueString = currentClueString.Substring(0, currentClueString.LastIndexOf('-'));
                // Next clues will be a combination of all
                nextClues = new List<ClueFragment>();
                foreach (ClueNode node in partialMatches)
                {
                    foreach (KeyValuePair<string, ClueNode> entry in node.Children)
                    {
                        List<Document> clueDocuments = entry.Value.References;
                        nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
                    }
                }

                // There shall be no found documents so far
            }
        }
        // Search Constriant form (Conditional Match): A-B-C;A-B;…#metaname#metaname(=partial)@metavalue[ContentElement]"keyword" Where for meta section order doesn’t matter; "keyword" means non-specified location; =partial defines a keyword for mata, otherwise existence is enough
        public void SearchByGeneralConstraints(string[] clues, string[] metakeys, string[] metavalues, out List<ClueFragment> nextClues, out List<Document> foundDocuments, bool bDeep = false)
        {
            throw new NotImplementedException();

            //// Try again by generating partial clues match return
            //if (nextClues == null && foundDocuments == null)
            //{
            //    List<ClueNode> partialMatches = GetClueNodeFromPartialPhrase(keyPhrases);
            //    currentClueString = currentClueString.Substring(0, currentClueString.LastIndexOf('-'));
            //    // Next clues will be a combination of all
            //    nextClues = new List<ClueFragment>();
            //    foreach (ClueNode node in partialMatches)
            //    {
            //        foreach (KeyValuePair<string, ClueNode> entry in node.Children)
            //        {
            //            List<Document> clueDocuments = entry.Value.References;
            //            nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
            //        }
            //    }

            //    // There shall be no found documents so far
            //}
        }
        // Search GUID form (Absolute Addressing): ID0000[ContentElement]
        // CA can be null
        public void SearchByID(int ID, string CA, out List<Document> foundDocuments, bool bDeep = false)
        {
            Document doc = GetDocument(ID);
            foundDocuments = new List<Document>();
            foundDocuments.Add(doc);
        }
        // Search Shorthand clue form
        public void SearchBySimpleClue(int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextClues, out List<Document> foundDocuments, bool bDeep = false)
        {
            // <Dev> Pending implementation cursorLocation

            // Parameters
            string lastKeyPhrase = keyPhrases[keyPhrases.Length - 1];   // Can be a clue or a metaname or a metavalue, can be partial
            nextClues = new List<ClueFragment>();
            foundDocuments = new List<Document>();

            // Heuristic: Mostly like the last phrase will be a meta because there's little point just entering a clue
            // Use former clues to confine range
            string[] partialPhrases;
            if (keyPhrases.Length > 1)
            {
                partialPhrases = new string[keyPhrases.Length - 1];
                Array.Copy(keyPhrases, partialPhrases, keyPhrases.Length - 1);
            }
            else partialPhrases = keyPhrases;
            // Get node up till that partial copy
            ClueNode node = null;
            if (Clues.ContainsKey(partialPhrases[0]) != false)
            {
                node = Clues[partialPhrases[0]].GetNode(partialPhrases, 0);

                // Try as meta
                foreach (Document doc in node.References)
                {
                    if(doc.IsPartialCLue(lastKeyPhrase) || doc.IsPartialMetaNameOrValue(lastKeyPhrase))
                        foundDocuments.Add(doc);
                }
                string currentClueString = string.Join("-", partialPhrases);
                if (foundDocuments.Count != 0)
                {
                    // Add further clues at current level
                    foreach (KeyValuePair<string, ClueNode> entry in node.Children)
                    {
                        List<Document> clueDocuments = entry.Value.References;
                        nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
                    }
                }
                else
                {
                    // Try as clue
                    foreach (KeyValuePair<string, ClueNode> entry in node.Children)
                    {
                        List<Document> clueDocuments = entry.Value.References;
                        nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
                        // foundDocuments.AddRange(clueDocuments); // Add all as possible found
                    }
                    foundDocuments = null;  // or we shouldn't have any found
                }
            }
            else
            {
                nextClues = null;
                foundDocuments = null;
            }
        }
        // Provding some suggestions on possible clues to use; aLso randomly throw some documents depending on hit rate?
        public void GetInitialSuggestion(string beginningText, out List<ClueFragment> nextClues, out List<Document> foundDocuments)
        {
            // Find partial matches
            nextClues = new List<ClueFragment>();
            foreach (KeyValuePair<string, ClueNode> entry in Clues)
            {
                if (entry.Key.IndexOf(beginningText) == 0)
                {
                    // Geenerate suggested clues
                    List<Document> clueDocuments = entry.Value.References;
                    nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, entry.Key, clueDocuments));
                }
            }
            if (nextClues.Count == 0) nextClues = null;

            // Get foundDocuments from ambiguous search
            List<ClueFragment> notUsed;
            AmbiguousSearch(new string[] { beginningText}, out notUsed, out foundDocuments);
        }
        // Search Ambiguous: a space demilited list of key words; Search into clues, names and comment; not searching other meta; Provide suggestedKeywords about clues, when there are some documents matching current input keywords
        public void AmbiguousSearch(string[] keywords, out List<ClueFragment> suggestedConstraintClues, out List<Document> foundDocuments, bool bDeep = false)
        {
            // Strategy: count hit points and order all available documents that has gained any hit point

            // Get must match keywords
            List<string> mustMatches = new List<string>();
            List<string> optionalMatches = new List<string>();
            foreach (string keyword in keywords)
            {
                if (keyword[0] == '!')
                    mustMatches.Add(keyword.Substring(1));
                else
                    optionalMatches.Add(keyword);
            }

            // First we get an array of all documents that matches the must match keywords (if any) - compare clues, names, and comment; potentially content if we are in deep mode <pending>
            List<Document> porentialMatches = new List<Document>();
            foreach (Document doc in Documents)
            {
                // if(bDeep) ....
                foreach (string must in mustMatches)
                {
                    if (doc.IsPartialCLue(must) == true || doc.IsPartialMetaNameOrValue(must) == true)
                    {
                        porentialMatches.Add(doc);
                        break;
                    }
                }
            }

            // Filter out documents that failed mustMatches test
            if (mustMatches.Count != 0)
            {
                if (porentialMatches.Count == 0)
                {
                    foundDocuments = null;
                    suggestedConstraintClues = null;
                    return;
                }
            }
            else
                porentialMatches = Documents;

            // Then we compare the result of the keywords and order by hit count
            foreach (Document doc in porentialMatches)
            {
                foreach (string match in optionalMatches)
                {
                    // if(bDeep) ...
                    if (doc.IsPartialCLue(match) || doc.IsPartialMetaNameOrValue(match) )
                        doc.KeywordOccurences++;
                    else
                        doc.KeywordMisses++;
                }
                // Strategy: To improve searching accuracy using heuristics - ....
            }
            // Find priority list
            List<Document> finalMatches = porentialMatches.Where(p => p.KeywordOccurences > 0).ToList();
            List<Document> orderedList = (finalMatches.OrderByDescending(x => (x.KeywordOccurences - x.KeywordMisses)).ToList());
            // Clean search state
            foreach (Document doc in porentialMatches)
            {
                doc.KeywordOccurences = 0;
                doc.KeywordMisses = 0;
            }

            // Return search result (only the former 10)
            // foundDocuments = orderedList.GetRange(0, 10);
            foundDocuments = orderedList;
            suggestedConstraintClues = null;    // <development> don't return suggested clues for now because I don't feel the need
        }
        /// <summary>
        /// Search next available clue from previous usage history
        /// </summary>
        /// <param name="cursorLocation"><Dev>Pending implementation</param>
        /// <param name="currentSearch">Current complete search input, might not be just key phrases</param>
        /// <param name="keyPhrases"></param>
        /// <param name="nextClues"></param>
        /// <param name="foundDocuments"></param>
        public void GetNextClueFromPairs(string currentSearch, int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextClues, out List<Document> foundDocuments)
        {
            // Genereate clues return
            List<string> nextPhrases = CluePairs.Get(keyPhrases.Last());
            nextClues = new List<ClueFragment>();
            foreach (string phrase in nextPhrases)
            {
                List<string> newPhrases = keyPhrases.ToList();
                newPhrases.Add(phrase);
                List<Document> clueDocuments = GetDocumentsFromPhrases(newPhrases.ToArray());
                nextClues.Add(new ClueFragment(phrase, clueDocuments.Count, string.Join("-", newPhrases), clueDocuments));
            }
            foundDocuments = null;

            // Generate found focuments return
            foundDocuments = GetDocumentsFromPhrases(keyPhrases);
        }
        #endregion

        #region Link Search Helper Constructs
        private Dictionary<string, ClueNode> Links { get; } // All clues established inside documents pointing toward other documents

        public List<string> GetLinksToDocument(Document doc)
        {
            return null;
            throw new NotImplementedException();
        }
        #endregion

        #region Access Interface
        // Document processing
        //public void CreateDocument(DocumentType type, string name = null)  // Add a document reference
        //{
        //    // Add document to memeory
        //    Document document = new Document(type, null, name, System.DateTime.Now.ToString("MMMM dd, yyyy HHmmss"));
        //    Documents.Add(document);
        //    document.GUID = Documents.Count - 1;

        //    // Create a physical representation of the document
        //    document.Materialize();

        //    // Save home (async)
        //    Save();
        //}
        public void ImportDocument(Document document)  // Add a document reference
        {
            // Add document to list
            Documents.Add(document);
            document.GUID = Documents.Count-1;

            // Index document in clues
            GenerateNewCluePairs(document.Clues);
            AddDocumentClues(document);
        }
        public void InternalizeAll(ImportAction action)
        {
            foreach (Document doc in Documents)
            {
                doc.Internalize(action);
            }
        }
        #endregion Access Interface

        #region Data Management
        // Serialize: Save current home data into a home data file
        public void Save()
        {
            Stream fileStream = File.Create(Location + HomeDataFileName);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(fileStream, this);
            // serializer.Serialize(TestFileStream, b);
            // serializer.Serialize(TestFileStream, c);
            fileStream.Close();
        }
        public static readonly string HomeDataFileName = @"Data.home";
        // Load home from location or generate one if that's an empty folder/non-existing folder; If a folder has content yet doesn't have a home file then it's invalid
        public static Home Load(string homeLocation)
        {
            string homeFile = homeLocation + HomeDataFileName;
            // If folder exists
            if (System.IO.Directory.Exists(homeLocation))
            {
                // Load existing home
                if(System.IO.File.Exists(homeFile))
                {
                    // Load serialized data
                    Stream fileStream = File.OpenRead(homeFile);
                    BinaryFormatter deserializer = new BinaryFormatter();
                    Home home = (Home)deserializer.Deserialize(fileStream);
                    // b = (Home)deserializer.Deserialize(fileStream);
                    // c = (List<TestClass>)deserializer.Deserialize(fileStream);
                    fileStream.Close();

                    // Generate extra application data
                    // ...

                    return home;
                }
                // Generate a blank home there
                else if(System.IO.Directory.GetFileSystemEntries(homeLocation).Length == 0)
                {
                    // Create a home
                    Home newHome = new Home(homeLocation);
                    // Save home
                    newHome.Save();
                    // Return home
                    return newHome;
                }
                // Non-multitude folder with unrelated contents are not allowed
                else
                {
                    throw new InvalidOperationException("Home location must be empty.");
                }
            }
            // If folder doesn't exist, create one
            else
            {
                // Create a folder
                System.IO.Directory.CreateDirectory(homeLocation);
                // Create a home
                Home newHome = new Home(homeLocation);
                // Save home
                newHome.Save();
                // Return home
                return newHome;
            }
        }
        #endregion Data Management
    }
}
