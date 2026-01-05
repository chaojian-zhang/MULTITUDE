using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// A helper construct to facilitate in document categorization and searching; Does not store clues and their relations to documents directly (unaware of underlying clue storage structure) because the strucutre(implementation) is likely to change in the furture, so we use generic interface to perform such operations
    /// </summary>
    [Serializable]
    internal class ClueManager
    {
        #region States
        public static ClueManager Manager;  // A global reference to this instance

        public ClueManager()
        {
            Clues = new Clues();
            Manager = this;
            
        }

        public Clues Clues { get; set; }    // Defines clue retrieving and adding
        #endregion

        #region Clue Management Interface
        // Record a clue
        public void AddDocumentToClues(Clue clue, Document doc)
        {
            Clues.AddDocument(clue, doc);
        }

        public void RemoveDocumentFromClues(Clue clue, Document doc)
        {
            Clues.RemoveDocument(clue, doc);
        }
        // Remove document from all clues
        public void RemoveDocumentFromClues(Document doc)
        {
            foreach (Clue clue in doc.Clues)
            {
                RemoveDocumentFromClues(clue, doc);
            }
        }
        #endregion

        #region Search Functions
        /// <summary>
        /// Given a sequence of fragments, find all clues that partially contain or equal to the sequence, return all documents under those clues
        /// </summary>
        /// <param name="clueString"></param>
        /// <returns></returns>
        private List<Document> GetPartialMatchingClueDocuments(string clueString)
        {
            return Clues.GetUnion(Clues.GetLargerClues(new Clue(clueString)));
        }

        ///// <summary>
        ///// Given a seuqnece of phrases, generate suggestions about potential fragments that can together form a valid fragment group
        ///// An almost exactly the same implimentation as above, but with added return values
        ///// </summary>
        ///// <param name="keyPhrases"></param>
        ///// <param name="nextFragments"></param>
        ///// <param name="foundDocuments"></param>
        //public void FindPotentialGroupFragments(string[] keyPhrases, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        //{
        //    nextFragments = new List<ClueFragment>();
        //    foundDocuments = new List<Document>();

        //    List<Clue> foundClues = GetContainingClues(keyPhrases);
        //    // Return all suggestions
        //    List<string> suggestions = new List<string>();
        //    foreach (Clue c in foundClues)
        //    {
        //        suggestions.AddRange(c.Fragments);
        //    }
        //    suggestions = suggestions.Distinct().ToList();
        //    foreach (string suggestion in suggestions)
        //    {
        //        List<Document> relatedDocuments = GetPartialMatchingClueDocuments(suggestion);  // <Debug> Cautious this can be very expensive
        //        nextFragments.Add(new ClueFragment(suggestion, relatedDocuments.Count, relatedDocuments.ToList()));
        //    }
        //    // If we have an exact match (i.e. 1. we find some clue, 2. there is no more fragment to show) then show foundDocuments
        //    if(foundClues.Count != 0 && suggestions.Count == 0)
        //    {
        //        foundDocuments = Clues[foundClues[0]].ToList();
        //    }
        //}

        /// <summary>
        /// An advanced version of FindPotentialGroupFragments, with support for ambiguous inputs at some keyphrase location
        /// </summary>
        /// <param name="cursorLocation">Which keyPhrase is current cursor on, this is used for more intelligent auto-complete</param>
        /// <param name="keyPhrases"></param>
        /// <param name="nextFragments"></param>
        /// <param name="foundDocuments"></param>
        /// <param name="bExactPhrase">If true then don't do partial match for phrase at cursor location</param>
        public void SearchForClueFragments(int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextFragments, out List<Document> foundDocuments, bool bExactPhrase)
        {
            // Do an exact match first
            FindPotentialGroupFragments(keyPhrases, out nextFragments, out foundDocuments);
            // If we find nothing, do an ambiguous search
            if(foundDocuments.Count == 0 && bExactPhrase == false)
            {
                // Do a partial match, without the keyphrase at cursor location
                List<string> partialKeyPhrase = keyPhrases.ToList();
                partialKeyPhrase.RemoveAt(cursorLocation);
                List<ClueFragment> newFragments;
                FindPotentialGroupFragments(partialKeyPhrase.ToArray(), out newFragments, out foundDocuments);
                // If we find anything, partially match with the phrase current cursor location is entring
                nextFragments.Clear();
                if(newFragments.Count != 0)
                {
                    foreach (ClueFragment frag in newFragments)
                    {
                        if (frag.Name.Contains(keyPhrases[cursorLocation])) nextFragments.Add(frag);
                    }
                }
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
        // Notice this search is similar to SearchForClueFragments() in form but detials (expesially conditions) differ
        public void SearchBySimpleClue(int cursorLocation, string[] keyPhrases, out List<ClueFragment> nextFragments, out List<Document> foundDocuments, bool bDeep = false)
        {
            // Do an exact match first
            FindPotentialGroupFragments(keyPhrases, out nextFragments, out foundDocuments);
            // If we find nothing, do a search with less keyphrases, and treat last one as a meta
            if (foundDocuments.Count == 0)
            {
                // Do a partial match, without the last keyphrase
                List<string> partialKeyPhrase = keyPhrases.ToList();
                partialKeyPhrase.RemoveAt(partialKeyPhrase.Count - 1);
                List<Document> potentiallyDocuments;
                FindPotentialGroupFragments(partialKeyPhrase.ToArray(), out nextFragments, out potentiallyDocuments);
                // If we find anything, restrict scope using last meta
                foundDocuments.Clear();
                if (potentiallyDocuments.Count != 0)
                {
                    foreach (Document doc in potentiallyDocuments)
                    {
                        if (doc.IsPartialMetaNameOrValue(keyPhrases.Last())) foundDocuments.Add(doc);
                    }
                }
            }

            // If we still find nothing, do an ambiguous search with less keyphrases, removing the keyphrase at cursor location
            if (foundDocuments.Count == 0)
            {
                // Do a partial match, without the keyphrase at cursor location
                List<string> partialKeyPhrase = keyPhrases.ToList();
                partialKeyPhrase.RemoveAt(cursorLocation);
                List<ClueFragment> newFragments;
                FindPotentialGroupFragments(partialKeyPhrase.ToArray(), out newFragments, out foundDocuments);
                // If we find anything, partially match with fragments with the phrase current cursor location is entring
                nextFragments.Clear();
                if (foundDocuments.Count != 0)  // Notice the condition
                {
                    foreach (ClueFragment frag in newFragments)
                    {
                        if (frag.Name.Contains(keyPhrases[cursorLocation])) nextFragments.Add(frag);
                    }
                }
            }
        }

        // Provding some suggestions on possible clues to use; aLso randomly throw some documents depending on hit rate?
        // Condition: (input.Contains('-') == false && input.Contains(' ') == false, i.e. beginning entry of a word
        // Rule: treat as a part of a clue, then treat as a name or meta
        public void GetInitialSuggestion(string beginningText, out List<ClueFragment> nextFragments, out List<Document> foundDocuments)
        {
            List<Clue> foundClues = new List<Clue>();
            foreach (KeyValuePair<Clue, HashSet<Document>> clue in Clues)
            {
                if (clue.Key.Overlaps(beginningText))
                {
                    foundClues.Add(clue.Key);
                }
            }
            // Return all suggestions
            List<string> suggestions = new List<string>();
            foreach (Clue c in foundClues)
            {
                suggestions.AddRange(c.Fragments);
            }
            suggestions = suggestions.Distinct().ToList();
            nextFragments = new List<ClueFragment>();
            foundDocuments = new List<Document>();
            foreach (string suggestion in suggestions)
            {
                List<Document> relatedDocuments = GetPartialMatchingClueDocuments(suggestion);  // <Debug> Cautious this can be very expensive
                foundDocuments.AddRange(relatedDocuments); // Return all founddocuments
                nextFragments.Add(new ClueFragment(suggestion, relatedDocuments.Count, relatedDocuments.ToList()));
            }

            // If we didn't find any matches
            if (foundDocuments.Count == 0)
            {
                // Do ambiguous serach
                AmbiguousSearch(new string[] { beginningText }, out nextFragments, out foundDocuments);
            }
        }

        // Search Ambiguous: a space demilited list of key words; Search into clues, names and comment; not searching other meta; Provide suggestedKeywords about clues, when there are some documents matching current input keywords
        // Just like Quick Match
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
            foreach (string must in mustMatches)
            {
                // if(bDeep) ....
                foreach (Document doc in home.Documents)
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
            foreach (string match in optionalMatches)
            {
                foreach (Document doc in porentialMatches)
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
    }
}
