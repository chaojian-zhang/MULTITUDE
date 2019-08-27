using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Notice tree structure supports only searching full clues, it doesn't support searching partial (not ambiguous) clues,
// For instance, a tree structure for A-B-C-D can search A-B-C but cannot search A-B-D, because the node breaks after B
namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// An implementation that allows quick navigation of clues, and guarantees uniqueness (non-redundancy) of clues
    /// <Debug> Notice internally there might be redundancy of clue instances, but since clues are treated like literal values, and we guarantee only: any added two clues or got two clues using the same clue string, in which ever order, are the same when compared using Equal()
    /// </summary>
    // public SortedSet<string> AllClues { get; } // A straight ordered set of all clues, either occuring alone or as a gorup. Notice set contain no duplicates, unlike list. No duplicates, i.e. A-B-C and A-C-B are the same.
    // Ref: https://stackoverflow.com/questions/12172162/how-to-insert-item-into-list-in-order


    /// <summary>
    /// Each Node Represents a clue fragment
    /// </summary>
    class ClueTreeNode
    {
        public string Name;
        public Dictionary<string, ClueTreeNode> Branches;
        public List<Document> Documents;

        public List<Document> GetDocuments()
        {

        }
    }

    /// <summary>
    /// A Clue indexing facility that greatly accelerates clue navigation
    /// All documents, once classified under a clue, should be found using this facility (unless it has no clue)
    /// </summary>
    class ClueTree
    {
        public Dictionary<string, ClueTreeNode> Nodes;

        #region Interface
        public List<Document> GetDocuments(Clue clue)
        {
            foreach (string fragment in clue.Fragments)
            {
                if (Nodes.ContainsKey(fragment)) return Nodes[fragment].
            }
        }
        public void AddDocument(Clue clue) { }
        public void RemoveDocument(Clue clue, Document doc) { }
        public void RenameClue(Clue oldClue, Clue newClue) { }
        #endregion

        #region Search Helper

        #endregion
    }
}
