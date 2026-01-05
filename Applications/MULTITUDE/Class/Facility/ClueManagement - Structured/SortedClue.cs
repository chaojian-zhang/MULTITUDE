using MULTITUDE.Class.DocumentTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// A functional yet extremely slow way to manage clues
/// </summary>
namespace MULTITUDE.Class.Facility.ClueManagement
{
    [Serializable]
    internal class SortedClueList : IEnumerable<KeyValuePair<Clue, HashSet<Document>>>
    {
        public SortedClueList()
        {
            Keys = new List<Clue>();
            Values = new List<HashSet<Document>>();
        }

        public List<Clue> Keys { get; set; }
        public List<HashSet<Document>> Values { get; set; }     // I doubt how much a hashset can help us

        public bool ContainsKey(Clue clue)
        {
            foreach (Clue c in Keys)
            {
                if (c == clue) return true;
            }
            return false;
        }

        public void Remove(Clue clue)
        {
            int toRemove = -1;
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i] == clue) toRemove = i;
            }
            if (toRemove != -1)
            {
                Keys.RemoveAt(toRemove);
                Values.RemoveAt(toRemove);
            }
            // Throw an exception if not found is better
            throw new ArgumentOutOfRangeException("Key not found.");
        }

        public HashSet<Document> this[Clue key]
        {
            get
            {
                for (int i = 0; i < Keys.Count; i++)
                {
                    if (Keys[i] == key) return Values[i];
                }
                // Otherwise create one
                Keys.Add(key);
                HashSet<Document> newValue = new HashSet<Document>();
                Values.Add(newValue);
                return newValue;
            }
            set
            {
                for (int i = 0; i < Keys.Count; i++)
                {
                    if (Keys[i] == key) { Values[i] = value; return; }
                }
                // Otherwise create one
                Keys.Add(key);
                Values.Add(value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Clue, HashSet<Document>>> GetEnumerator()
        {
            return new SortedClueListEnumerator(Keys, Values);
        }
    }

    // When you implement IEnumerable, you must also implement IEnumerator.
    public class SortedClueListEnumerator : IEnumerator<KeyValuePair<Clue, HashSet<Document>>>
    {
        internal SortedClueListEnumerator(List<Clue> keysRef, List<HashSet<Document>> valuesRef)
        {
            _KeysRef = keysRef;
            _ValuesRef = valuesRef;
        }

        List<Clue> _KeysRef;
        List<HashSet<Document>> _ValuesRef;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public bool MoveNext()
        {
            position++;
            return (position < _KeysRef.Count);
        }

        public void Reset()
        {
            position = -1;
        }

        public void Dispose()
        {
            // https://stackoverflow.com/questions/3061612/ienumerator-is-it-normal-to-have-an-empty-dispose-method
        }

        KeyValuePair<Clue, HashSet<Document>> IEnumerator<KeyValuePair<Clue, HashSet<Document>>>.Current
        {
            get
            {
                return (KeyValuePair<Clue, HashSet<Document>>)Current;
            }
        }

        public object Current
        {
            get
            {
                return new KeyValuePair<Clue, HashSet<Document>>(_KeysRef[position], _ValuesRef[position]);
            }
        }
    }
}
