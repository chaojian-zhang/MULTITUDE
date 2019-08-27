using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    [Serializable]
    class ClueTreeNode
    {
        public string Name { get; set; }
        public Dictionary<string, ClueTreeNode> Branches { get; set; }
        public List<Document> Documents { get; set; }
        public ClueTreeNode Parent { get; set; }    // If null it's primary tree node

        public ClueTreeNode(string name, ClueTreeNode parent)
        {
            Name = name;
            Branches = new Dictionary<string, ClueTreeNode>();
            Documents = new List<Document>();
        }

        /// <summary>
        /// Get documents that immediately belongs to this node
        /// </summary>
        /// <param name="fragment"></param>
        /// <returns></returns>
        public List<Document> GetDocuments(string fragment)
        {
            if (Branches.ContainsKey(fragment))
                    return Branches[fragment].Documents;
                else return null;
        }

        /// <summary>
        /// Get count of documents that belong to this and children of this node
        /// </summary>
        /// <returns></returns>
        public int GetDocumentsCount()
        {
            int result = Documents.Count;
            foreach (ClueTreeNode node in Branches.Values)
            {
                result += node.GetDocumentsCount();
            }
            return result;
        }

        // Caller might want to Distinct()
        public void GetAllDocuments(ref List<Document> documentsList)
        {
            documentsList.AddRange(Documents);
            foreach (ClueTreeNode node in Branches.Values)
            {
                node.GetAllDocuments(ref documentsList);
            }
        }

        public List<Document> GetDocuments(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
                return Documents;
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]))
                    return Branches[fragments[currentIndex + 1]].GetDocuments(fragments, currentIndex + 1);
                else return null;
            }
        }

        public void AddDocument(string[] fragments, int currentIndex, Document pending)
        {
            if (currentIndex == fragments.Length - 1)
            {
                foreach (Document doc in Documents)
                {
                    if (doc == pending) return;
                }
                Documents.Add(pending);
            }
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    Branches[fragments[currentIndex + 1]] = new ClueTreeNode(fragments[currentIndex + 1], this);
                Branches[fragments[currentIndex + 1]].AddDocument(fragments, currentIndex + 1, pending);
            }
        }

        public void AddDocument(string[] fragments, int currentIndex, List<Document> pendings)
        {
            if (currentIndex == fragments.Length - 1)
            {
                foreach (Document pending in pendings)
                {
                    bool bAlreadyExist = false;
                    foreach (Document doc in Documents)
                    {
                        if (doc == pending) { bAlreadyExist = true; break; }
                    }
                    if(!bAlreadyExist) Documents.Add(pending);
                }
            }
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    Branches[fragments[currentIndex + 1]] = new ClueTreeNode(fragments[currentIndex + 1], this);
                Branches[fragments[currentIndex + 1]].AddDocument(fragments, currentIndex + 1, pendings);
            }
        }

        public void RemoveDocument(string[] fragments, int currentIndex, Document pending)
        {
            if (currentIndex == fragments.Length - 1)
            {
                int index = Documents.IndexOf(pending);
                if (index >= 0) Documents.RemoveAt(index);
                else throw new InvalidOperationException("The specified document hasn't been categorized under this clue");
            }
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]))
                    Branches[fragments[currentIndex + 1]].RemoveDocument(fragments, currentIndex + 1, pending);
                else throw new InvalidOperationException("The specified document hasn't been categorized under this clue");
            }
        }
        public ClueTreeNode GetParentTreeNode(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
                return Parent;
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]))
                    return Branches[fragments[currentIndex + 1]].GetParentTreeNode(fragments, currentIndex + 1);
                else return null;
            }
        }
        public ClueTreeNode GetTreeNode(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
                return this;
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]))
                    return Branches[fragments[currentIndex + 1]].GetTreeNode(fragments, currentIndex + 1);
                else return null;
            }
        }

        public ClueTreeNode CreateTreeNode(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
                return this;
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    Branches[fragments[currentIndex + 1]] = new ClueTreeNode(fragments[currentIndex + 1], this);

                return Branches[fragments[currentIndex + 1]].CreateTreeNode(fragments, currentIndex + 1);
            }
        }

        public List<string> GetSiblings(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
                return Branches.Keys.ToList();
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    throw new InvalidOperationException("The specified document hasn't been categorized under this clue");
                else return GetSiblings(fragments, currentIndex + 1);
            }
        }

        public void GetTrailingFragments(string[] fragments, int currentIndex, List<string> appendix)
        {
            if (currentIndex == fragments.Length - 1)
                appendix.AddRange(Branches.Keys);
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    throw new InvalidOperationException("The specified document hasn't been categorized under this clue");
                else GetTrailingFragments(fragments, currentIndex + 1, appendix); ;
            }
        }

        public List<ClueFragment> GetClueFraments()
        {
            List<ClueFragment> results = new List<ClueFragment>();
            List<string> siblings = Branches.Keys.ToList();
            foreach (string candidate in siblings)
            {
                results.Add(new ClueFragment(candidate, Branches[candidate].GetDocumentsCount(), GetDocuments(candidate)));
            }
            return results;
        }

        public List<string> GetPossibleFragments(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
                return Branches.Keys.Where(item => item.Contains(fragments[fragments.Length - 1]) && item != fragments[fragments.Length - 1]).ToList();
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    return null;
                else return GetPossibleFragments(fragments, currentIndex + 1);
            }
        }

        public List<ClueFragment> GetPossibleClueFragments(string[] fragments, int currentIndex)
        {
            if (currentIndex == fragments.Length - 1)
            {
                string[] possibleFragments = Branches.Keys.Where(item => item.Contains(fragments[fragments.Length - 1]) && item != fragments[fragments.Length - 1]).ToArray();
                List<ClueFragment> results = new List<ClueFragment>();
                foreach (string frag in possibleFragments)
                {
                    results.Add(new ClueFragment(frag, Branches[frag].GetDocumentsCount(), Branches[frag].Documents));
                }
                return results; 
            }
            else
            {
                if (Branches.ContainsKey(fragments[currentIndex + 1]) == false)
                    return null;
                else return GetPossibleClueFragments(fragments, currentIndex + 1);
            }
        }

        public List<string> GetAllClues()
        {
            List<string> clues = new List<string>();

            if (this.Branches.Count == 0)
                clues.Add(this.Name);
            else
            {
                foreach (ClueTreeNode node in Branches.Values)
                {
                    clues.AddRange(node.GetAllClues());
                }
                for (int i = 0; i < clues.Count; i++)
                {
                    clues[i] = Clue.Concatenate(this.Name, clues[i]);
                }
            }

            return clues;
        }
    }
}
