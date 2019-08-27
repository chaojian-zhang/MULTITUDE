using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility.ClueManagement
{
    /// <summary>
    /// A representation of a clue: be it a standalone fragment or a fragment group
    /// Pending replace all other clue usages with this class
    /// Implemnted as a literal type, i.e. after constructed no internal data changes
    /// Clues are fragment-order netural, i.e. A-B-C and C-B-A refer to the same clue
    /// </summary>
    [Serializable]
    internal class Clue : IComparable<Clue>
    {
        // Per design input fragments will be already Distinct()ed and ToLower()ed.
        public Clue(string[] fragments)
        {
            foreach (string frag in fragments)
            {
                if (frag.IndexOfAny(InvalidClueCharacters.ToCharArray()) != -1)
                    throw new InvalidOperationException("Clue string contains invalid characters.");
            }

            this._Fragments = fragments;
        }

        #region Configurations
        public static string InvalidClueCharacters = "+*/\\~`{}\"[]:?!;|<>,=";
        #endregion

        public Clue(string clueString)
        {
            // Also validate tag: No special symbols like punctuation and special characters are allowed; no OS invalid characters are allowed for maximum file system compatibility; clue chain symbol - is allowed
            if (ValidateString(clueString) == false) 
                throw new InvalidOperationException("Clue string contains invalid characters.");    // Might want to be less strict, at least don't raise an exception immediately; I.e. the caller can call the validation function and do a replacement first, then if it's still illegal we should raise an exception here

            this._Fragments = SeperateClueFragments(clueString).ToArray();
        }

        private string[] _Fragments;
        public string[] Fragments { get { return _Fragments; } }   // Fragments are all lower cases, and do not repeat; order doesn't matter for equality comparison of clues; Essentially this is a "set" but we didn't use exiplicitly a HashSet for performance considerations

        public bool IsEmpty { get { return _Fragments.Length == 0 || string.IsNullOrEmpty(_Fragments[0]); } }
        public string Name { get { return string.Join("-", Fragments); } }
        public int Length { get { return Fragments.Length; } }
        public bool bStandAlone { get { return Length == 1; } } // Whether this is a standalonw single fragment clue
        public bool Contains(Clue subClue)  // Determine whether this equals or is a bigger set than subClue; Notice the smallest unit is a fragment and it needs to be exact
        {
            if (this.Length < subClue.Length || subClue.IsEmpty) return false;

            // Check whether any element in smaller set, i.e. B, appears in bigger set, i.e. A
            foreach (string f in subClue.Fragments)
            {
                bool bFoundInB = false;
                foreach (string f2 in this.Fragments)
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
        public bool Overlaps(string keyword)    // Determines whether or not any of the fragments contains the keyword, this is the most ambiguous form of clue search
        {
            foreach (string f in Fragments)
            {
                if (f.Contains(keyword)) return true;
            }
            return false;
        }
        public bool Contains(string fragment)  // Determine whether a fragment is contained in this clue
        {
            return Fragments.Contains(fragment);
        }
        public bool Equals(Clue compareClue)    // Determine whether two clues are the same
        {
            if (this.Length != compareClue.Length) return false;

            // Check occurence of each fargment
            foreach (string f in compareClue.Fragments)
            {
                bool bFoundInB = false;
                foreach (string f2 in this.Fragments)
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
        public Clue Rename(string newClue)
        {
            // When a clue is renamed, it's just renamed; Documents are not affected by this operation for links are based on GUIDs
            // _Fragments = ClueHelper.SeperateClueFragments(newClue).ToArray();
            return new Clue(newClue);

            // If we however don't create a new one but change value in place, it can cause strange inconsistentcy effects where users got unexpected changes in value
        }

        #region Type implementations
        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is Clue))
                return false;
            else
                return this.Equals((Clue)obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(Clue other)
        {
            if (this.Contains(other) == true) return 1;
            else if (this == other) return 0;
            else return -1;
        }

        //add this code to class ThreeDPoint as defined previously
        //
        public static bool operator ==(Clue a, Clue b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the values are equal
            return a.Equals(b);
        }

        public static bool operator !=(Clue a, Clue b)
        {
            return !(a == b);
        }
        // https://msdn.microsoft.com/ru-ru/library/ms173147(v=vs.80).aspx
        #endregion

        #region Static Members
        public static readonly string dashEscapeSymbol = "~%D&!";  // This isn't necessary for clues normally won't contain escape (if its generated automatically), but we should support it anyway

        // Fragments can be just a fragment or exsiting chained fragments
        public static string Concatenate(params string[] fragments)
        {
            return string.Join("-", fragments);
        }

        public Clue Concatenate(params Clue[] Clues)
        {
            List<string> fragments = new List<string>();
            foreach (Clue clue in Clues)
            {
                fragments.AddRange(clue.Fragments);
            }
            return new Clue(string.Join("-", fragments));
        }

        // Seperate a clue into different phrases (or "tags" or "key phrases" -- for consistency we will call it a tag, and ideally a tag is just a word, not a phrase, but allowable to be a phrase)
        // With -- escaped; Also does a ToLower() operation.
        public static List<string> SeperateClueFragments(string clue)
        {
            string escaped = clue.ToLower().Replace("--", dashEscapeSymbol);
            string[] fragments = escaped.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < fragments.Length; i++)
            {
                fragments[i] = fragments[i].Replace(dashEscapeSymbol, "-");
            }

            // Also remove redundancy
            return fragments.Distinct().ToList();
        }

        /// <summary>
        /// Given a multiline text, create a bunch of clues from it
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static List<Clue> CreateCluesFromText(string text)
        {
            List<Clue> clues = new List<Clue>();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                clues.Add(new Clue(line));
            }
            return clues;
        }

        // Ref: https://support.microsoft.com/en-us/help/905231/information-about-the-characters-that-you-cannot-use-in-site-names,-folder-names,-and-file-names-in-sharepoint, https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
        public static bool ValidateString(string clueString)
        {
            return clueString.IndexOfAny(InvalidClueCharacters.ToCharArray()) == -1;
        }
        #endregion
    }

}
