using System;
using Supremes.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Supremes.Parsers;

namespace Supremes.Nodes
{
    /// <summary>
    /// The attributes of an Element.
    /// </summary>
    /// <remarks>
    /// Attributes are treated as a map: there can be only one value associated with an attribute key.
    /// <p/>
    /// Attribute key and value comparisons are done case insensitively, and keys are normalised to
    /// lower-case.
    /// </remarks>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public sealed class Attributes : IEnumerable<Attribute>
    {
        // The Attributes object is only created on the first use of an attribute; the Element will just have a null
        // Attribute slot otherwise
        internal const string dataPrefix = "data-";

        // Indicates a jsoup internal key. Can't be set via HTML. (It could be set via accessor, but not too worried about
        // that. Suppressed from list, iter.
        private const char InternalPrefix = '/';
        
        private const int InitialCapacity = 3; // sampling found mean count when attrs present = 1.49; 1.08 overall. 2.6:1 don't have any attrs.

        // manages the key/val arrays
        private const int GrowthFactor = 2;
        internal const int NotFound = -1;
        private const string EmptyString = "";

        internal List<string> keys = new(InitialCapacity);
        internal List<object> vals = new(InitialCapacity);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal int IndexOfKey(string key) {
            Validate.NotNull(key);
            return keys.IndexOf(key);
        }
        
        private int IndexOfKeyIgnoreCase(String key) {
            Validate.NotNull(key);
            return keys.FindIndex(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// we track boolean attributes as null in values - they're just keys. so returns empty for consumers
        /// casts to String, so only for non-internal attributes
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string CheckNotNull(object val)
        {
            return val == null ? EmptyString : (string)val;
        }

        /// <summary>
        /// Get an attribute value by key.
        /// Set a new attribute, or replace an existing one by key.
        /// </summary>
        /// <param name="key">the attribute key</param>
        /// <value>attribute value</value>
        /// <returns>the attribute value if set; or empty string if not set.</returns>
        /// <seealso cref="ContainsKey(string)">ContainsKey(string)</seealso>
        public string this[string key]
        {
            get
            {
                Validate.NotEmpty(key);
                var i = IndexOfKey(key);
                return i == NotFound ? EmptyString : CheckNotNull(vals[i]);
            }
            set
            {
                Attribute attr = new Attribute(key, value);
                Put(attr);
            }
        }

        /// <summary>
        /// Get an attribute value by index.
        /// </summary>
        /// <param name="i"></param>
        public string this[int i] => CheckNotNull(vals[i]);
        
        /// <summary>
        /// Get an attribute's value by case-insensitive key
        /// </summary>
        /// <param name="key">the attribute name</param>
        /// <returns>the first matching attribute value if set; or empty string if not set (ora boolean attribute).</returns>
        public string GetIgnoreCase(string key)
        {
            Validate.NotEmpty(key);
            var i = IndexOfKeyIgnoreCase(key);
            return i == NotFound ? EmptyString : CheckNotNull(vals[i]);
        }

        /// <summary>
        /// Get an arbitrary user data object by key.
        /// </summary>
        /// <param name="key">case sensitive key to the object.</param>
        /// <returns>the object associated to this key, or {@code null} if not found.</returns>
        public object GetUserData(string key)
        {
            Validate.NotNull(key);
            if (!IsInternalKey(key))
            {
                key = InternalKey(key);
            }

            var i = IndexOfKeyIgnoreCase(key);
            return i == NotFound ? null : vals[i];
        }

        /// <summary>
        /// Adds a new attribute. Will produce duplicates if the key already exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Attributes Add(string key, string value)
        {
            AddObject(key, value);
            return this;
        }
        
        private void AddObject(string key, object value)
        {
            keys.Add(key);
            vals.Add(value);
        }

        /// <summary>
        /// Set a new attribute, or replace an existing one by key.
        /// </summary>
        /// <param name="key">case sensitive attribute key (not null)</param>
        /// <param name="value">attribute value (may be null, to set a boolean attribute)</param>
        /// <returns>these attributes, for chaining</returns>
        public Attributes Put(string key, string value)
        {
            Validate.NotNull(key);
            var i = IndexOfKey(key);
            if (i != NotFound)
            {
                
                vals[i] = value;
            }
            else
            {
                AddObject(key, value);
            }

            return this;
        }
        
        /// <summary>
        /// Put an arbitrary user-data object by key. Will be treated as an internal attribute, so will not be emitted in HTML.
        /// </summary>
        /// <param name="key">case sensitive key</param>
        /// <param name="value">object value</param>
        /// <returns>these attributes</returns>
        Attributes PutUserData(String key, Object value) {
            Validate.NotNull(key);
            if (!IsInternalKey(key)) key = InternalKey(key);
            Validate.NotNull(value);
            int i = IndexOfKey(key);
            if (i != NotFound)
                vals[i] = value;
            else
                AddObject(key, value);
            return this;
        }
  
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void PutIgnoreCase(string key, string value) {
            int i = IndexOfKeyIgnoreCase(key);
            if (i != NotFound) {
                vals[i] = value;
                if (!keys[i].Equals(key)) // case changed, update
                    keys[i] = key;
            }
            else
                Add(key, value);
        }
        
        /// <summary>
        /// Set a new boolean attribute, remove attribute if value is false.
        /// </summary>
        /// <param name="key">case <b>insensitive</b> attribute key</param>
        /// <param name="value">attribute value</param>
        /// <returns>these attributes, for chaining</returns>
        public Attributes Put(String key, bool value) {
            if (value)
                PutIgnoreCase(key, null);
            else
                Remove(key);
            return this;
        }

        /// <summary>
        /// Set a new attribute, or replace an existing one by key.
        /// </summary>
        /// <param name="attribute">attribute</param>
        public Attributes Put(Attribute attribute)
        {
            Validate.NotNull(attribute);
            Put(attribute.Key, attribute.Value);
            attribute.Parent = this;
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out string value)
        {
            var i = IndexOfKey(key);
            if (i == NotFound)
            {
                value = string.Empty;
                return false;
            }
            value = CheckNotNull(vals[i]);
            return true;
        }

        private void Remove(int index)
        {
            Validate.IsFalse(index >= Count);
            keys.RemoveAt(index);
            vals.RemoveAt(index);
        }

        /// <summary>
        /// Remove an attribute by key.
        /// </summary>
        /// <param name="key">attribute key to remove</param>
        public void Remove(string key)
        {
            Validate.NotEmpty(key);
            var i = IndexOfKey(key);
            if (i != NotFound)
            {
                Remove(i);
            }
        }

        /// <summary>
        /// Remove an attribute by key. <b>Case insensitive.</b>
        /// </summary>
        /// <param name="key">attribute key to remove</param>
        public void RemoveIgnoreCase(string key)
        {
            var i = IndexOfKeyIgnoreCase(key);
            if (i != NotFound)
                Remove(i);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(string key, object value)
        {
            var i = IndexOfKey(key);
            return i != NotFound && vals[i].Equals(value);
        }

        /// <summary>
        /// Tests if these attributes contain an attribute with this key.
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if key exists, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            return IndexOfKey(key) != NotFound;
        }
        
        /// <summary>
        /// Tests if these attributes contain an attribute with this key.
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if key exists, false otherwise</returns>
        public bool ContainsKeyIgnoreCase(string key)
        {
            return IndexOfKeyIgnoreCase(key) != NotFound;
        }
        
        /// <summary>
        /// Check if these attributes contain an attribute with a value for this key.
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if key exists, and it has a value</returns>
        public bool HasDeclaredValueForKey(string key) {
            int i = IndexOfKey(key);
            return i != NotFound && vals[i] != null;
        }
        
        /// <summary>
        /// Check if these attributes contain an attribute with a value for this key.
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if key exists, and it has a value</returns>
        public bool HasDeclaredValueForKeyIgnoreCase(string key) {
            int i = IndexOfKeyIgnoreCase(key);
            return i != NotFound && vals[i] != null;
        }

        /// <summary>
        /// Get the number of attributes in this set.
        /// </summary>
        /// <returns>size</returns>
        public int Count => keys.Count;

        /// <summary>
        /// Test if this Attributes list is empty (size==0).
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Add all the attributes from the incoming set to this set.
        /// </summary>
        /// <param name="incoming">attributes to add to these attributes.</param>
        public void AddAll(Attributes incoming)
        {
            if (incoming == null || incoming.Count == 0)
            {
                return;
            }

            foreach (var attribute in incoming)
            {
                Put(attribute);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Attributes"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Attribute> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return new Attribute(keys[i], vals[i].ToString(), this);
            }
        }
        
        /// <summary>
        /// clear all attributes
        /// </summary>
        public void Clear()
        {
            keys.Clear();
            vals.Clear();
        }

        /// <summary>
        /// Get the attributes as a List, for iteration.
        /// </summary>
        /// <remarks>
        /// Get the attributes as a List, for iteration. Do not modify the keys of the attributes via this view, as changes
        /// to keys will not be recognised in the containing set.
        /// </remarks>
        /// <returns>an view of the attributes as a List.</returns>
        public IReadOnlyList<Attribute> AsList()
        {
            return this.ToList().AsReadOnly();
        }

        /// <summary>
        /// Retrieves a filtered view of attributes that are HTML5 custom data attributes; that is, attributes with keys
        /// starting with
        /// <c>data-</c>
        /// .
        /// </summary>
        /// <returns>map of custom data attributes.</returns>
        public IDictionary<string, string> Dataset => new _Dataset(this);

        /// <summary>
        /// Get the HTML representation of these attributes.
        /// </summary>
        /// <returns>HTML</returns>
        public string Html
        {
            get
            {
                StringBuilder accum = new StringBuilder();
                AppendHtmlTo(accum, (new Document(string.Empty)).OutputSettings);
                // output settings a bit funky, but this html() seldom used
                return accum.ToString();
            }
        }

        internal void AppendHtmlTo(StringBuilder accum, DocumentOutputSettings @out)
        {
            for (int i = 0; i < Count; i++) {
                if (IsInternalKey(keys[i]))
                    continue;
                string key = Attribute.GetValidKey(keys[i], @out.Syntax);
                if (key != null)
                    Attribute.HtmlNoValidate(key, (string) vals[i], accum.Append(' '), @out);
            }
        }

        /// <summary>
        /// Converts the value of this instance to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Html;
        }

        /// <summary>
        /// Compares two <see cref="Attributes"/> instances for equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is not Attributes that)
            {
                return false;
            }

            if (Count != that.Count)
            {
                return false;
            }

            return this.All(x => that.ContainsKey(x.Key) && that[x.Key].Equals(x.Value));
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int result = Count;
            result = 31 * result + keys.GetHashCode();
            result = 31 * result + vals.GetHashCode();
            return result;
        }

        internal Attributes Clone()
        {
            Attributes clone;
            clone = (Attributes)this.MemberwiseClone();
            clone.keys = new List<string>(keys);
            clone.vals = new List<object>(vals);
            return clone;
        }
        
        public void Normalize()
        {
            for (int i = 0; i < Count; i++) {
                keys[i] = keys[i].ToLower();
            }
        }
        
        /// <summary>
        /// Internal method. Removes duplicate attribute by name. Settings for case sensitivity of key names.
        /// </summary>
        /// <param name="settings">case sensitivity</param>
        /// <returns>number of removed dupes</returns>
        public int Deduplicate(ParseSettings settings) {
            if (IsEmpty)
                return 0;
            bool preserve = settings.PreserveAttributeCase;
            int dupes = 0;
            for (int i = 0; i < Count; i++) {
                for (int j = i + 1; j < Count; j++) {
                    if (keys[j] == null)
                        break; // keys.length doesn't shrink when removing, so re-test
                    if ((preserve && keys[i].Equals(keys[j])) || (!preserve && keys[i].Equals(keys[j], StringComparison.OrdinalIgnoreCase))) {
                        dupes++;
                        Remove(j);
                        j--;
                    }
                }
            }
            return dupes;
        }

        private class _Dataset : IDictionary<string, string>
        {
            private readonly Attributes attributes;

            public _Dataset(Attributes enclosing)
            {
                this.attributes = enclosing;
            }

            public void Add(string key, string value)
            {
                string dataKey = Attributes.DataKey(key);
                Attribute attr = new Attribute(dataKey, value);
                attributes.Put(attr);
            }

            public bool ContainsKey(string key)
            {
                string dataKey = Attributes.DataKey(key);
                return attributes.ContainsKey(dataKey);
            }

            public ICollection<string> Keys
            {
                get { return this.Select(a => a.Key).ToArray(); }
            }

            public bool Remove(string key)
            {
                string dataKey = Attributes.DataKey(key);
                attributes.Remove(dataKey);
                return true;
            }

            public bool TryGetValue(string key, out string value)
            {
                return attributes.TryGetValue(key, out value);
            }

            public string this[string key]
            {
                get => attributes[key];
                set => attributes[key] = value;
            }

            public ICollection<string> Values
            {
                get { return this.Select(a => a.Value).ToArray(); }
            }

            public void Add(KeyValuePair<string, string> item)
            {
                this.Add(item.Key, item.Value);
            }

            public void Clear()
            {
                var dataAttrs = GetDataAttributes().ToList();
                foreach (var dataAttr in dataAttrs)
                {
                    attributes.Remove(dataAttr.Key);
                }
            }

            public bool Contains(KeyValuePair<string, string> item)
            {
                return attributes.Contains(item.Key, item.Value);
            }

            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, string> item)
            {
                attributes.Remove(item.Key);
                return true;
            }

            public int Count => attributes.Count;
            public bool IsReadOnly { get; }

            private IEnumerable<Attribute> GetDataAttributes()
            {
                return attributes
                    .Where(a => a.IsDataAttribute());
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return GetDataAttributes()
                    .Select(a => new KeyValuePair<string, string>(a.Key.Substring(dataPrefix.Length) /*substring*/, a.Value))
                    .GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private static string DataKey(string key)
        {
            return dataPrefix + key;
        }
        
        internal static String InternalKey(String key) {
            return InternalPrefix + key;
        }

        private bool IsInternalKey(String key) {
            return key is { Length: > 1 } && key[0] == InternalPrefix;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
