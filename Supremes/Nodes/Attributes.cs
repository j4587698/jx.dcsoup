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
        private const int NotFound = -1;
        private const string EmptyString = "";

        private List<string> keys = new(InitialCapacity);
        private List<object> vals = new(InitialCapacity);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        int IndexOfKey(string key) {
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
            if (keys.Contains(key))
            {
                keys[key] = value;
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
        public void Put(Attribute attribute)
        {
            Validate.NotNull(attribute);
            if (attributes == null)
            {
                attributes = new LinkedHashMap<string, Attribute>(2);
            }
            attributes[attribute.Key] = attribute;
        }

        /// <summary>
        /// Remove an attribute by key.
        /// </summary>
        /// <param name="key">attribute key to remove</param>
        public void Remove(string key)
        {
            Validate.NotEmpty(key);
            if (attributes == null)
            {
                return;
            }
            attributes.Remove(key.ToLower());
        }

        /// <summary>
        /// Tests if these attributes contain an attribute with this key.
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if key exists, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            return attributes != null && attributes.ContainsKey(key.ToLower());
        }

        /// <summary>
        /// Get the number of attributes in this set.
        /// </summary>
        /// <returns>size</returns>
        public int Count
        {
            get
            {
                if (attributes == null)
                {
                    return 0;
                }
                return attributes.Count;
            }
        }
        
        /// <summary>
        /// Test if this Attributes list is empty (size==0).
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Add all the attributes from the incoming set to this set.
        /// </summary>
        /// <param name="incoming">attributes to add to these attributes.</param>
        public void SetAll(Attributes incoming)
        {
            if (incoming == null || incoming.Count == 0)
            {
                return;
            }
            if (attributes == null)
            {
                attributes = new LinkedHashMap<string, Attribute>(incoming.Count);
            }
            foreach (var pair in incoming.attributes)
            {
                attributes[pair.Key] = pair.Value;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Attributes"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Attribute> GetEnumerator()
        {
            if (attributes != null)
            {
                foreach (var pair in attributes) yield return pair.Value;
            }
        }
        
        /// <summary>
        /// clear all attributes
        /// </summary>
        public void Clear()
        {
            attributes?.Clear();
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
            if (attributes == null)
            {
                return new List<Attribute>(0).AsReadOnly();
            }
            return attributes.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Retrieves a filtered view of attributes that are HTML5 custom data attributes; that is, attributes with keys
        /// starting with
        /// <c>data-</c>
        /// .
        /// </summary>
        /// <returns>map of custom data attributes.</returns>
        public IDictionary<string, string> Dataset
        {
            get { return new Attributes._Dataset(this); }
        }

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
            if (attributes == null)
            {
                return;
            }
            foreach (KeyValuePair<string, Attribute> entry in attributes)
            {
                Attribute attribute = (Attribute)entry.Value;
                accum.Append(" ");
                attribute.AppendHtmlTo(accum, @out);
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
            if (attributes == null || that.attributes == null)
            {
                return (attributes == that.attributes);
            }
            return attributes.SequenceEqual(that.attributes);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return attributes != null ? attributes.GetHashCode() : 0;
        }

        internal Attributes Clone()
        {
            if (attributes == null)
            {
                return new Attributes();
            }
            Attributes clone;
            clone = (Attributes)this.MemberwiseClone();
            clone.attributes = new LinkedHashMap<string, Attribute>(attributes.Count);
            foreach (Attribute attribute in this)
            {
                clone.attributes[attribute.Key] = attribute.Clone();
            }
            return clone;
        }
        
        public void Normalize()
        {
            if (attributes == null)
            {
                return;
            }

            var newMap = new LinkedHashMap<string, Attribute>();
            foreach (var attribute in attributes)
            {
                var key = attribute.Key;
                var value = attribute.Value;

                var newKey = key.ToLower();

                newMap[newKey] = value;
            }

            attributes = newMap;
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
            for (int i = 0; i < attributes.Count; i++) {
                for (int j = i + 1; j < attributes.Count; j++) {
                    if (attributes[j].Key == null)
                        break; // keys.length doesn't shrink when removing, so re-test
                    if ((preserve && attributes[i].Key.Equals(keys[j])) || (!preserve && keys[i].Equals(keys[j], StringComparison.OrdinalIgnoreCase))) {
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
            private readonly LinkedHashMap<string, Attribute> enclosingAttributes;

            public _Dataset(Attributes enclosing)
            {
                if (enclosing.attributes == null)
                {
                    enclosing.attributes = new LinkedHashMap<string, Attribute>(2);
                }
                this.enclosingAttributes = enclosing.attributes;
            }

            public void Add(string key, string value)
            {
                string dataKey = Attributes.DataKey(key);
                Attribute attr = new Attribute(dataKey, value);
                enclosingAttributes.Add(dataKey, attr);
            }

            public bool ContainsKey(string key)
            {
                string dataKey = Attributes.DataKey(key);
                return enclosingAttributes.ContainsKey(dataKey);
            }

            public ICollection<string> Keys
            {
                get { return this.Select(a => a.Key).ToArray(); }
            }

            public bool Remove(string key)
            {
                string dataKey = Attributes.DataKey(key);
                return enclosingAttributes.Remove(dataKey);
            }

            public bool TryGetValue(string key, out string value)
            {
                string dataKey = Attributes.DataKey(key);
                Attribute attr = null;
                if (enclosingAttributes.TryGetValue(dataKey, out attr))
                {
                    value = attr.Value;
                    return true;
                }
                value = null;
                return false;
            }

            public ICollection<string> Values
            {
                get { return this.Select(a => a.Value).ToArray(); }
            }

            public string this[string key]
            {
                get
                {
                    string dataKey = Attributes.DataKey(key);
                    Attribute attr = enclosingAttributes[dataKey];
                    return attr.Value;
                }
                set
                {
                    string dataKey = Attributes.DataKey(key);
                    Attribute attr = new Attribute(dataKey, value);
                    enclosingAttributes[dataKey] = attr;
                }
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
                    enclosingAttributes.Remove(dataAttr.Key);
                }
            }

            private IEnumerable<Attribute> GetDataAttributes()
            {
                return enclosingAttributes
                    .Select(p => (Attribute)p.Value)
                    .Where(a => a.IsDataAttribute());
            }

            public bool Contains(KeyValuePair<string, string> item)
            {
                string value = null;
                return (this.TryGetValue(item.Key, out value) && (value == item.Value));
            }

            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                foreach (var pair in this)
                {
                    array[arrayIndex++] = pair;
                }
            }

            public int Count
            {
                get { return GetDataAttributes().Count(); }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(KeyValuePair<string, string> item)
            {
                return this.Contains(item) && this.Remove(item.Key);
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
        
        static String InternalKey(String key) {
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
