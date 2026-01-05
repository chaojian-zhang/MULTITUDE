using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// A representation of all clues along with their associated docuemtns
    /// Provides a set of definitive retrieving and adding interface functions for ClueManager to use
    /// </summary>
    [Serializable]
    class Clues
    {
        #region Data
        public Dictionary<string, FragmentInfo> Fragments;
        public List<Clue> ClueList; // Name only, collision free due to how we insert it, O(1) complexity for inserting; Used for retriving information about all clues only
        #endregion

        #region Document Interface
        public void AddDocument(Clue clue, Document doc)
        {
            bool bFound = true;
            foreach (string fragment in clue.Fragments)
            {
                if (Fragments.ContainsKey(fragment) == false)
                    bFound = false;
            }
            if(bFound == false)
        }
        public void RemoveDocument(Clue clue, Document doc)
        {

        }
        #endregion

        #region Search Interface
        /// <summary>
        /// Get all document under that clue
        /// </summary>
        /// <param name="clue"></param>
        /// <returns></returns>
        public List<Document> GetDocuments(Clue clue)
        {

        }
        /// <summary>
        /// Get all document with specific type under that clue
        /// </summary>
        /// <param name="clue"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<Document> GetDocumentsFilterByType(Clue clue, DocumentType type)
        {

        }
        /// <summary>
        /// Get clues that contains current clue, excluding current clue
        /// Notice a clue might not exist in order of a tree sturcture
        /// </summary>
        /// <param name="clue"></param>
        /// <returns></returns>
        public List<Clue> GetLargerClues(Clue clue)
        {

        }
        /// <summary>
        /// Get documents that satisfies all clues
        /// </summary>
        /// <param name="clues"></param>
        /// <returns></returns>
        public List<Document> GetUnion(List<Clue> clues)
        {

        }
        #endregion
    }

    // Also might want to implement
    // NameToDocuments = new MultiDict<string, Document>();
    // public MultiDict<string, Document> NameToDocuments { get; set; }    // Acceleration facility: A name can point to many documents; In essence, (exact) document names is a form of identification



    // Should be async ready
    #region Clue Manipulations
    /// <summary>
    /// Given a clue string, add it to current set
    /// Input clue stirng can be in any order (e.g. C-A-B)
    /// </summary>
    /// <param name="clueString"></param>
    public Clue Add(string clueString)  // O(N)
    {
        List<string> segments = ClueManager.SeperateClueFragments(clueString);
        Clue newClue = new Clue(segments.ToArray());
        foreach (KeyValuePair<Clue, HashSet<Document>> clue in Clues)
        {
            if (clue.Key.Equals(newClue)) return clue.Key;
        }
        Clues[newClue] = new HashSet<Document>();
        return newClue;
    }

    /// <summary>
    /// Given a clue string, get the clue corresponding to it
    /// Input clue stirng can be in any order (e.g. C-A-B)
    /// </summary>
    /// <param name="clueString"></param>
    /// <returns></returns>
    public Clue Get(string clueString)  // O(N)
    {
        List<string> segments = ClueManager.SeperateClueFragments(clueString);
        Clue compareClue = new Clue(segments.ToArray());
        foreach (KeyValuePair<Clue, HashSet<Document>> clue in Clues)
        {
            if (clue.Key.Equals(compareClue)) return clue.Key;
        }
        return null;
    }

    public void ChangeExistingClue(string originalClueString, string newClueString)
    {
        // Get the clue to change
        List<string> segments = ClueManager.SeperateClueFragments(originalClueString);
        Clue compareClue = new Clue(segments.ToArray());
        for (int i = 0; i < Clues.Keys.Count; i++)
        {
            if (Clues.Keys[i].Equals(compareClue))
            {
                Clues.Keys[i] = Clues.Keys[i].Rename(newClueString); // I wonder how it affects the order of the clue in SortedList

                // Populate change to affected documents (those who are assigned to this clue)
                foreach (Document doc in Clues.Values[i])
                {
                    doc.ChangeClue(compareClue, Clues.Keys[i]);
                }
                return;
            }
        }
        throw new ArgumentException("Specified string does not correspond to an existing clue.");
    }

    // Given a particular clue string (e.g. A-B-C) for the document, record it
    // Input clue stirng can be in any order (e.g. C-A-B)
    public void AddDocumentToClues(Clue compareClue, Document doc)
    {
        foreach (KeyValuePair<Clue, HashSet<Document>> clue in Clues)
        {
            if (clue.Key.Equals(compareClue))
            {
                Clues[clue.Key].Add(doc);
                return;
            }
        }
        Clues[compareClue] = new HashSet<Document>();
        Clues[compareClue].Add(doc);
    }

    public void RemoveDocumentFromClues(Clue compareClue, Document doc)
    {
        foreach (KeyValuePair<Clue, HashSet<Document>> clue in Clues)
        {
            if (clue.Key.Equals(compareClue))
            {
                Clues[clue.Key].Remove(doc);
                if (Clues[clue.Key].Count == 0) Clues.Remove(clue.Key);
            }
        }
        throw new ArgumentException("Specified clue doesn't exist.");
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
}
