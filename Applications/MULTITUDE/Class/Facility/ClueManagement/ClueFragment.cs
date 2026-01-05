using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// Represent search results, ready for data binding
    /// </summary>
    internal class ClueFragment
    {
        public ClueFragment(string name, int count, List<Document> relatedDocs)
        {
            Name = name;
            Count = count;
            RelatedDocs = relatedDocs;
        }

        // Basic
        public string Index { get; set; }      // https://stackoverflow.com/questions/22378456/how-to-get-the-index-of-the-current-itemscontrol-item, so we need custom logic
        public string Name { get; set; }
        public int Count { get; set; }  // All documents categorized under that specific fragment (not in any sense a bigger clue)
        // Auxiliary
        // public string CompleteClue { get; set; }    // Complete clue sofar leading to current clue fragment
        public List<Document> RelatedDocs { get; set; } // All related documents under complete clue
    }
}
