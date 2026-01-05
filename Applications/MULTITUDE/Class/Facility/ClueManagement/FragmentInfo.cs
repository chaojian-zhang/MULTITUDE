using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    [Serializable]
    internal class FragmentInfo
    {
        public string Name { get; set; }

        public HashSet<string> Relatives { get; set; }  // All related fragments
        public List<FragmentGroup> Groups { get; set; }
        public List<Document> Documents { get; set; }
    }

    [Serializable]
    internal class FragmentGroup
    {
        public Clue AssiciatedClue { get; set; }
        public List<Document> Documents { get; set; }
    }
}