using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MULTITUDE.Class.Facility
{
    [Serializable]
    class MultiDict<TKey, TValue> // no (collection) base class
    {
        private Dictionary<TKey, List<TValue>> _data = new Dictionary<TKey, List<TValue>>();
        public Dictionary<TKey, List<TValue>> Data { get { return _data; } }

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

        public bool ContainsKey(TKey k)
        {
            return _data.ContainsKey(k);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Remove(TKey k, TValue v)
        {
            if (_data.ContainsKey(k) == false) throw new InvalidOperationException("Requested dictionary key doesn't exist.");
            else
            {
                if (_data[k].Contains(v) == false) throw new InvalidOperationException("Requested dictionary value doesn't exist.");
                else _data[k].Remove(v);

                if (_data[k].Count == 0) _data.Remove(k);
            }
        }

        // Ref: https://stackoverflow.com/questions/3850930/multi-value-dictionary
    }

    [Serializable]
    class MultiDictSet<TKey, TValue> // no (collection) base class
    {
        private Dictionary<TKey, HashSet<TValue>> _data = new Dictionary<TKey, HashSet<TValue>>();
        public Dictionary<TKey, HashSet<TValue>> Data { get { return _data; } }

        public void Add(TKey k, TValue v)
        {
            // can be a optimized a little with TryGetValue, this is for clarity
            if (_data.ContainsKey(k))
                _data[k].Add(v);
            else
                _data.Add(k, new HashSet<TValue>() { v });
        }

        public HashSet<TValue> Get(TKey k)
        {
            if (_data.ContainsKey(k) == true) return _data[k];
            else return null;
        }

        public bool ContainsKey(TKey k)
        {
            return _data.ContainsKey(k);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Remove(TKey k, TValue v)
        {
            if (_data.ContainsKey(k) == false) throw new InvalidOperationException("Requested dictionary key doesn't exist.");
            else
            {
                if (_data[k].Contains(v) == false) throw new InvalidOperationException("Requested dictionary value doesn't exist.");
                else _data[k].Remove(v);

                if (_data[k].Count == 0) _data.Remove(k);
            }
        }

        // Ref: https://stackoverflow.com/questions/3850930/multi-value-dictionary
    }

    class DataHelper
    {
    }
}
