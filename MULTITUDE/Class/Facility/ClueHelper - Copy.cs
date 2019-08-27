using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    // Represetn a clue tree node used for accelerated accessing: Consider construct this during loading rather than serialized
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
            if (clueFragments.Length - 1 == currentIndex)
                References.Add(doc);    // Notice not checking existence, assumed not existing
            else
            {
                if (Children.ContainsKey(clueFragments[currentIndex + 1]) == false)
                    Children[clueFragments[currentIndex + 1]] = new ClueNode();
                Children[clueFragments[currentIndex + 1]].AddDocument(clueFragments, currentIndex + 1, doc);
            }
        }
    }

    /// <summary>
    /// A representation of a clue: be it a standalone fragment or a fragment group
    /// Pending replace all other clue usages with this class
    /// </summary>
    [Serializable]
    internal class Clue
    {
        public Clue(string[] fragments)
        {
            this.Fragments = fragments;
        }

        private string[] Fragments { get; set; }    // Fragments are all lower cases, and do not repeat; order doesn't matter for equality comparison of clues; Essentially this is a "set" but we didn't use exiplicitly a HashSet for performance considerations

        public string Name { get { return string.Join("-", Fragments); } }
        public int Length { get { return Fragments.Length; } }
        public bool bStandAlone { get { return Length == 1; } } // Whether this is a standalonw single fragment clue
        public bool Contains(Clue subClue) { throw new NotImplementedException(); }
        public bool Equals(Clue compareClue) { throw new NotImplementedException(); }
        public string Rename(string newClue) { throw new NotImplementedException(); }
    }

    /// <summary>
    /// An implementation that allows quick navigation of clues, and guarantees uniqueness (non0redundancy) of clues
    /// </summary>
    // public SortedSet<string> AllClues { get; } // A straight ordered set of all clues, either occuring alone or as a gorup. Notice set contain no duplicates, unlike list. No duplicates, i.e. A-B-C and A-C-B are the same.
    // Ref: https://stackoverflow.com/questions/12172162/how-to-insert-item-into-list-in-order
    [Serializable]
    internal class ClueSet
    {
        public ClueSet()
        {
            Clues = new List<Clue>();
        }

        private List<Clue> Clues;

        public void Add(string clueString)
        {

        }
        public Clue Get(string clueString)
        {

        }
    }

    /// <summary>
    /// Represent search results
    /// </summary>
    [Serializable]
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
    internal class FragmentInfo
    {
        public FragmentInfo(string name, Document doc = null)
        {
            Name = name;
            FullClues = new HashSet<Clue>();
            Documents = new HashSet<Document>();
            if (doc != null) Documents.Add(doc);
        }

        public string Name { get; }
        public HashSet<Clue> FullClues { get; } // Clues that contains this fragment
        public HashSet<Document> Documents { get; } // Documents that are specified under this fragment
    }

    [Serializable]
    internal class ClueHelper
    {
        #region States
        public static ClueHelper ClueManager;
        public ClueHelper()
        {
            Fragments = new Dictionary<string, FragmentInfo>();
            FragmentScopes = new MultiDict<string, Document>();
            ClueSet = new ClueSet();
            ClueManager = this;
        }
        // <Development> Might not want to make those Serializable
        public Dictionary<string, FragmentInfo> Fragments { get; set; }  // A list of clue fragments; Notice each string is just a fragment like A, B, or C
        public MultiDict<string, Document> FragmentScopes { get; set; }  // A list of all scoped fragments listed as a complete clue; Notice each string is a complete clue like A-B-C
        public ClueSet ClueSet { get; set; }
        #endregion

        // Should be async ready
        #region Clue Manipulations
        public void ChangeExistingClue(string originalClueString, string newClueString)
        {
            List<Document> affectedDocuments;
            // Make a change in collections: assume original clue always exists
            // Find the clue
            string[] fragments = SeperateClueFragments(originalClue);
            if(fragments.Length == 1)
            {
                // Change clue name

                // Record affected documents
                affectedDocuments = Fragments[fragments[0]].Documents.ToList();

                // <development>
                // Might also want to consider those documents that refer to these affected docuemtns using such a clue
                // We need a registration mechanism so a clue knows whether or not itself it referenced
            }
            else
            {

            }

            // Populate change to affected documents
        }

        // Given a particular clue A-B-C for the document; This clue constraints a fragment scope for that particular document
        public void AddDocumentClue(string clue, Document doc)  // Assumed clue lower cased
        {
            string[] fragments = ClueHelper.SeperateClueFragments(clue);
            // Add to scope
            if (fragments.Length > 1) FragmentScopes.Add(clue, doc);
            // Add to fragments
            foreach (string fragment in fragments)
            {
                if (Fragments.ContainsKey(fragment))
                    Fragments[fragment].Documents.Add(doc);
                else
                    Fragments[fragment] = new FragmentInfo(fragment, doc);
                Fragments[fragment].FragmentScopes.Add(clue);
            }
            // Add to all collection
            AllClues.Add(clue); // Notice by design (i.e. from where this function is called) clue is guaranteed to be non-duplicate to other already added ones; E.g. if it's manually created in clue creator we will rearrange it if it matches any existing ones.
        }
        public void RemoveDocumentClue(string clue, Document doc)
        {
            string[] fragments = ClueHelper.SeperateClueFragments(clue);
            // Remove from scope
            if (fragments.Length > 1) FragmentScopes.Remove(clue, doc);
            // Remove from fragments
            foreach (string fragment in fragments)
            {
                if (Fragments.ContainsKey(fragment) == false)
                    throw new InvalidOperationException("Key/Value isn't balanced for current operation doesn't have a previous counterpart.");
                else
                    Fragments[fragment].Documents.Remove(doc);
                // Remove from that fragment's scope if it's no longer used by anyone
                if (FragmentScopes.Get(clue) == null)
                    Fragments[fragment].FragmentScopes.Remove(clue);
            }

            // Remove clue from collection if not used by others
            if (FragmentScopes.Get(clue) == null)
                AllClues.Remove(clue);
        }

        //#region Deprecated Dctionary<string, ClueNode> Clue(Tree) construct
        //public void AddDocumentToClue(string clue, Document doc)
        //{
        //    string[] clueFragments = SeperateClueFragments(clue);
        //    if (Clues.ContainsKey(clueFragments[0]) == false)
        //        Clues[clueFragments[0]] = new ClueNode();
        //    Clues[clueFragments[0]].AddDocument(clueFragments, 0, doc);
        //}
        //public List<Document> GetDocumentsFromClue(string clue)
        //{
        //    string[] clueFragments = SeperateClueFragments(clue);
        //    return GetDocumentsFromPhrases(clueFragments);
        //}
        //public List<Document> GetDocumentsFromPhrases(string[] cluePhrases)
        //{
        //    if (Clues.ContainsKey(cluePhrases[0]) != false)
        //        return Clues[cluePhrases[0]].GetDocuments(cluePhrases, 0);
        //    else
        //        return null;
        //}
        //public ClueNode GetClueNodeFromPhrases(string[] cluePhrases)
        //{
        //    if (Clues.ContainsKey(cluePhrases[0]) != false)
        //        return Clues[cluePhrases[0]].GetNode(cluePhrases, 0);
        //    else
        //        return null;
        //}
        //public List<ClueNode> GetClueNodeFromPartialPhrase(string[] cluePhrases)  // Compared with GetClueNodeFromPhrases(), this function doesn't assume the last keyword phrase to be exact
        //{
        //    // Make a partial copy
        //    string[] partialPhrases = null;
        //    Array.Copy(cluePhrases, partialPhrases, cluePhrases.Length - 1);
        //    // Get node up till that partial copy
        //    ClueNode node = null;
        //    if (Clues.ContainsKey(partialPhrases[0]) != false)
        //        node = Clues[partialPhrases[0]].GetNode(partialPhrases, 0);
        //    else
        //        return null;
        //    // Check result
        //    if (node != null)
        //    {
        //        // Find partial matches
        //        List<ClueNode> partialMatches = new List<ClueNode>();
        //        string partialPhrase = cluePhrases[cluePhrases.Length - 1];
        //        foreach (KeyValuePair<string, ClueNode> entry in node.Children)
        //        {
        //            if(entry.Key.IndexOf(partialPhrase) == 0)
        //                partialMatches.Add(entry.Value);
        //        }
        //        if (partialMatches.Count != 0) return partialMatches;
        //        else return null;
        //    }
        //    else
        //        return null;
        //}
        //#endregion
        #endregion

        #region Search Functions
        /// <summary>
        /// Given a clue, find any documents under that clue, or return null if not a valid clue
        /// </summary>
        /// <returns></returns>
        private List<Document> GetMatchingClueDocuments(string clue)
        {
            string[] fragments = SeperateClueFragments(clue.ToLower());
            // Single fragment clue
            if(fragments.Length == 1)
            {
                if (Fragments.ContainsKey(fragments[0]) == true) return Fragments[fragments[0]].Documents.ToList();
                else return null;
            }
            // Multiple fragment clue
            else
            {
                foreach (KeyValuePair<string, List<Document>> fragmentScope in FragmentScopes.Data)
                {
                    if (ClueHelper.IsClueFragmentsMatching(fragments, SeperateClueFragments(fragmentScope.Key)) == true)
                    {
                        return fragmentScope.Value;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Given a fragment, find whether or not it matches any existing fragments - either partial or completely; Return all matches
        /// Partial means anywhere, not just beginning characters; Return value can exclude exact match of partialFragment, if specifed
        /// </summary>
        /// <param name="partialFragment"></param>
        /// <param name="partialMatches"></param>
        /// <param name="bExcludeSearch"></param>
        /// <returns></returns>
        private bool TryGetFragmentFromPartialString(string partialFragment, out List<FragmentInfo> partialMatches, bool bExcludeSearch = false)
        {
            partialMatches = new List<FragmentInfo>();
            if (Fragments.ContainsKey(partialFragment)) partialMatches.Add(Fragments[partialFragment]);
            else
            {
                foreach (KeyValuePair<string, FragmentInfo> entry in Fragments)
                {
                    if (entry.Key.Contains(partialFragment) == true)
                    {
                        if (bExcludeSearch == true && partialFragment == entry.Key) continue;
                        else partialMatches.Add(entry.Value);
                    }
                }
            }

            if (partialMatches.Count != 0) return true;
            else
            {
                partialMatches = null;
                return false;
            }
        }

        /// <summary>
        /// Given a sequence of fragments, find all fragment groups that are either equal or bigger than that sequence; return null if no exact or partial match can be found; Cross-overlap is not legal
        /// </summary>
        private List<string[]> GetMatchingScopes(string[] fragments)
        {
            List<string[]> matchingScopes = new List<string[]>();
            foreach (KeyValuePair<string, List<Document>> fragmentScope in FragmentScopes.Data)
            {
                string[] compareFragments = ClueHelper.SeperateClueFragments(fragmentScope.Key);
                if (ClueHelper.IsClueFragmentsContains(compareFragments, fragments) == true)
                {
                    matchingScopes.Add(compareFragments);
                }
            }

            // If we have any matching scope
            if (matchingScopes.Count > 0)
            {
                return matchingScopes;
            }
            else return null;
        }

        /// <summary>
        /// Given a sequenc of fragments, find out those fragments that potentially share the same fragment group as them
        /// </summary>
        private List<string> GetPotentialGroupFragments(string[] fragments)
        {
            // Check out matching scopes
            List<string[]> matchingScopes = GetMatchingScopes(fragments);

            if (matchingScopes != null)
            {
                // Generate a list of all potential members
                List<string> companionFragments = new List<string>();
                foreach (string[] scope in matchingScopes)
                {
                    companionFragments.AddRange(scope.ToList());
                }
                // Strip out repeating ones and the ones that are given
                companionFragments = companionFragments.Distinct().ToList();
                foreach (string key in fragments)
                {
                    companionFragments.Remove(key);
                }
                return companionFragments;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Given a seuqnece of phrases, generate suggestions about potential fragments that can together form a valid fragment group
        /// Or if it doesn't belong any fragment group (i.e. it's a standalone fragment), just return informaion about itself
        /// </summary>
        /// <param name="keyPhrases"></param>
        /// <param name="nextClues"></param>
        /// <param name="foundDocuments"></param>
        private void FindPotentialGroupFragments(string[] keyPhrases, out List<ClueFragment> nextClues, out List<Document> foundDocuments)
        {
            nextClues = null;
            foundDocuments = null;

            List<string> companionFragments = GetPotentialGroupFragments(keyPhrases);
            if (companionFragments != null)
            {
                // Generate return results
                nextClues = new List<ClueFragment>();
                foreach (string fragment in companionFragments)
                {
                    HashSet<Document> relatedDocuments = Fragments[fragment].Documents;
                    nextClues.Add(new ClueFragment(fragment, relatedDocuments.Count, fragment, relatedDocuments.ToList()));
                }
            }
            else
            {
                if(keyPhrases.Length == 1 && Fragments.ContainsKey(keyPhrases[0]))
                {
                    foundDocuments = Fragments[keyPhrases[0]].Documents.ToList();
                }
            }
        }

        /// <summary>
        /// From existing clues find corresponding documents, and also provide a hint about next available clue; Do not return non-existing clues
        /// </summary>
        /// /// <param name="cursorLocation">Which keyPhrase is current cursor on, this is used for more intelligent auto-complete</param>
        /// <param name="keyPhrases"></param>
        /// <param name="nextClues"></param>
        /// <param name="foundDocuments"></param>
        /// <param name="bExactPhrase">If true then don't do partial match for phrase at cursor location</param>
        public void SearchForClueFragments(int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextClues, out List<Document> foundDocuments, bool bExactPhrase)
        {
            // Defaults
            nextClues = null;
            foundDocuments = null;

            // Check directly whether any group set matches current given prhases, i.e. equal or bigger
            if (bExactPhrase == true)
            {
                FindPotentialGroupFragments(keyPhrases, out nextClues, out foundDocuments);
            }
            else
            {
                if (keyPhrases.Length == 0) { nextClues = null; foundDocuments = null; }

                // Begin with the fragment current cursor at
                string beginningFragment = keyPhrases[cursorLocation];

                List<FragmentInfo> partialMatches;
                // If we find any matches for that fragment
                if (TryGetFragmentFromPartialString(beginningFragment, out partialMatches) == true)
                {
                    // Do an exact match
                    if (partialMatches.Count == 1)
                    {
                        FindPotentialGroupFragments(keyPhrases, out nextClues, out foundDocuments);
                    }
                    // If there are many possible matches, we return all possible matches, and no particular found documents
                    else
                    {
                        nextClues = new List<ClueFragment>();
                        foreach (FragmentInfo info in partialMatches)
                        {
                            nextClues.Add(new ClueFragment(info.Name, info.Documents.Count, info.Name, info.Documents.ToList()));
                        }
                        foundDocuments = null;
                    }
                }

                //{
                //    if (bExactPhrase == true)
                //    {
                //        // Check 
                //        if (Fragments.ContainsKey(beginningFragment) == true) relatedClues = Fragments[beginningFragment].FragmentScopes;
                //        else return;


                //        // Get all possible fragmentGroups for such fragment: the next suggested fragments must be from those groups, restricted by other available phrases
                //        HashSet<string> relatedClues;
                //        if (Fragments.ContainsKey(beginningFragment) == true) relatedClues = Fragments[beginningFragment].FragmentScopes;
                //        else return;

                //        // Get other clues
                //        HashSet<string> tempCheckingClues;
                //        foreach (string phrase in keyPhrases)
                //        {
                //            if (Fragments.ContainsKey(phrase) == true)
                //            {
                //                tempCheckingClues = Fragments[phrase].FragmentScopes;

                //            }
                //            else return;
                //        }

                //        // Get related clue fragments
                //        HashSet<string> relatedFragments = new HashSet<string>();
                //        foreach (string clue in relatedClues)
                //        {
                //            bool bException = false;
                //            // Find any non-match in current clue
                //            foreach (string keyPhrase in keyPhrases)
                //            {
                //                if (clue.Contains(keyPhrase) == false) { bException = true; break; }
                //            }
                //            if (bException == true) continue;
                //            else
                //            {
                //                string[] fragments = ClueHelper.SeperateClueFragments(clue); // Can be redundant; Both hashset and Distinct() can be used here: https://stackoverflow.com/questions/10632776/fastest-way-to-remove-duplicate-value-from-a-list-by-lambda, https://stackoverflow.com/questions/47752/remove-duplicates-from-a-listt-in-c-sharp
                //                foreach (string frag in fragments)
                //                {
                //                    relatedFragments.Add(frag);
                //                }
                //            }
                //        }
                //        relatedFragments.Remove(beginningFragment);
                //        // Generate next clues from other fragments sharing scopes
                //        nextClues = new List<ClueFragment>();
                //        foreach (string fragment in relatedFragments)
                //        {
                //            HashSet<Document> fragmentDocuments = Fragments[fragment].Documents;
                //            nextClues.Add(new ClueFragment(fragment, fragmentDocuments.Count, fragment, fragmentDocuments.ToList()));    // Notice ideally we should return what will happen if we use that fragment following current entry; but it seems current feedback information is already informative enough (i.e. use string currentClueString = string.Join("-", keyPhrases);)
                //        }
                //        // Found documents are documents satisfying all current search fragments, tolerating no partial match
                //        foundDocuments = new List<Document>();
                //        foreach (string phrase in keyPhrases)
                //        {
                //            if (Fragments.ContainsKey(phrase) == false)
                //            {
                //                foundDocuments = null;
                //                return;
                //            }
                //            else foundDocuments.AddRange(Fragments[phrase].Documents);
                //        }
                //        foundDocuments = foundDocuments.Distinct().ToList();
                //    }
                //}
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
            Document doc = (App.Current as App).CurrentHome.GetDocument(ID);
            foundDocuments = new List<Document>();
            foundDocuments.Add(doc);
        }

        // Search Shorthand clue form
        public void SearchBySimpleClue(int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextClues, out List<Document> foundDocuments, bool bDeep = false)
        {
            throw new NotImplementedException();
            // <Dev> Pending implementation cursorLocation

            //// Parameters
            //string lastKeyPhrase = keyPhrases[keyPhrases.Length - 1];   // Can be a clue or a metaname or a metavalue, can be partial
            //nextClues = new List<ClueFragment>();
            //foundDocuments = new List<Document>();

            //// Heuristic: Mostly like the last phrase will be a meta because there's little point just entering a clue
            //// Use former clues to confine range
            //string[] partialPhrases;
            //if (keyPhrases.Length > 1)
            //{
            //    partialPhrases = new string[keyPhrases.Length - 1];
            //    Array.Copy(keyPhrases, partialPhrases, keyPhrases.Length - 1);
            //}
            //else partialPhrases = keyPhrases;
            //// Get node up till that partial copy
            //ClueNode node = null;
            //if (Clues.ContainsKey(partialPhrases[0]) != false)
            //{
            //    node = Clues[partialPhrases[0]].GetNode(partialPhrases, 0);

            //    // Try as meta
            //    foreach (Document doc in node.References)
            //    {
            //        if(doc.IsPartialCLue(lastKeyPhrase) || doc.IsPartialMetaNameOrValue(lastKeyPhrase))
            //            foundDocuments.Add(doc);
            //    }
            //    string currentClueString = string.Join("-", partialPhrases);
            //    if (foundDocuments.Count != 0)
            //    {
            //        // Add further clues at current level
            //        foreach (KeyValuePair<string, ClueNode> entry in node.Children)
            //        {
            //            List<Document> clueDocuments = entry.Value.References;
            //            nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
            //        }
            //    }
            //    else
            //    {
            //        // Try as clue
            //        foreach (KeyValuePair<string, ClueNode> entry in node.Children)
            //        {
            //            List<Document> clueDocuments = entry.Value.References;
            //            nextClues.Add(new ClueFragment(entry.Key, clueDocuments.Count, currentClueString + '-' + entry.Key, clueDocuments));
            //            // foundDocuments.AddRange(clueDocuments); // Add all as possible found
            //        }
            //        foundDocuments = null;  // or we shouldn't have any found
            //    }
            //}
            //else
            //{
            //    nextClues = null;
            //    foundDocuments = null;
            //}
        }
        // A much simplied version for quick usages, e.g. by VW
        public void SearchBySimpleClue(string clue, out List<Document> foundDocuments)
        {
            // Get documents under that clue, if any; no ambiguity and partial entry
            foundDocuments = GetMatchingClueDocuments(clue);
        }

        // Provding some suggestions on possible clues to use; aLso randomly throw some documents depending on hit rate?
        public void GetInitialSuggestion(string beginningText, out List<ClueFragment> nextClues, out List<Document> foundDocuments)
        {
            // Initialize
            nextClues = null;
            foundDocuments = null;

            List<FragmentInfo> partialMatches;
            // If we find any matches for that fragment
            if (TryGetFragmentFromPartialString(beginningText, out partialMatches, true) == true)
            {
                // Geenerate suggested clues
                nextClues = new List<ClueFragment>();
                foreach (FragmentInfo info in partialMatches)
                {
                    nextClues.Add(new ClueFragment(info.Name, info.Documents.Count, info.Name, info.Documents.ToList()));
                }
            }

            //// Found doucments are listed if an exact match
            //if(Fragments.ContainsKey(beginningText))
            //{
            //    foundDocuments = Fragments[beginningText].Documents;
            //}

            // Get foundDocuments from ambiguous search
            List<ClueFragment> notUsed;
            AmbiguousSearch(new string[] { beginningText }, out notUsed, out foundDocuments);
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
            Home home = (App.Current as App).CurrentHome;
            List<Document> porentialMatches = new List<Document>();
            foreach (Document doc in home.Documents)
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
                porentialMatches = home.Documents;

            // Then we compare the result of the keywords and order by hit count
            foreach (Document doc in porentialMatches)
            {
                foreach (string match in optionalMatches)
                {
                    // if(bDeep) ...
                    if (doc.IsPartialCLue(match) || doc.IsPartialMetaNameOrValue(match))
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
        #endregion

        #region Static Members
        static public readonly string dashEscapeSymbol = "~%D&!";  // This isn't necessary for clues normally won't contain escape (if its generated automatically), but we should support it anyway

        // Seperate a clue into different phrases (or "tags" or "key phrases" -- for consistency we will call it a tag, and ideally a tag is just a word, not a phrase, but allowable to be a phrase)
        // With -- escaped; Also does a ToLower() operation.
        static public string[] SeperateClueFragments(string clue)
        {
            string escaped = clue.ToLower().Replace("--", dashEscapeSymbol);
            string[] fragments = escaped.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = fragments[i].Replace(dashEscapeSymbol, "-");
            }
            return fragments;
        }

        // Determine whether two set of clues are the same
        static public bool IsCluesMatching(string clueA, string clueB)
        {
            return IsClueFragmentsMatching(SeperateClueFragments(clueA), SeperateClueFragments(clueB));
        }

        // Determine whether two set of clue fragments are the same
        static public bool IsClueFragmentsMatching(string[] fragmentsA, string[] fragmentsB)
        {
            if (fragmentsA.Length != fragmentsB.Length) return false;

            // Notice we are not checking re-occuring fragments; It's bad to waste storage but re-ocurring fragments are not illegal or affect logic in any way
            foreach (string f in fragmentsA)
            {
                bool bFoundInB = false;
                foreach (string f2 in fragmentsB)
                {
                    if (f == f2)
                    {
                        bFoundInB = true;
                        break;
                    }
                }
                if (bFoundInB == false) return false;
            }
            return true;
        }

        // Determine whether set A of clue fragments contains set B: i.e. set A is either bigger or equal to set B
        static public bool IsClueFragmentsContains(string[] fragmentsA, string[] fragmentsB)
        {
            if (fragmentsA.Length < fragmentsB.Length) return false;

            // Check whether any element in smaller set, i.e. B, appears in bigger set, i.e. A
            foreach (string f in fragmentsB)
            {
                bool bFoundInB = false;
                foreach (string f2 in fragmentsA)
                {
                    if (f == f2)
                    {
                        bFoundInB = true;
                        break;
                    }
                }
                if (bFoundInB == false) return false;
            }
            return true;
        }
        #endregion
    }
}
