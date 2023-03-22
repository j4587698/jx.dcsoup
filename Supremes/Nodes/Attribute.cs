using Supremes.Helper;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Supremes.Nodes
{
    /// <summary>
    /// A single key + value attribute.
    /// </summary>
    /// <remarks>
    /// Keys are trimmed and normalised to lower-case.
    /// </remarks>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public sealed class Attribute
    {
        private static readonly string[] booleanAttributes = new string[] { "allowfullscreen"
            , "async", "autofocus", "checked", "compact", "declare", "default", "defer", "disabled"
            , "formnovalidate", "hidden", "inert", "ismap", "itemscope", "multiple", "muted"
            , "nohref", "noresize", "noshade", "novalidate", "nowrap", "open", "readonly", "required"
            , "reversed", "seamless", "selected", "sortable", "truespeed", "typemustmatch" };

        private string key;

        private string value;

        internal Attributes Parent { get; set; } // used to update the holding Attributes when the key / value is changed via this interface

        public Attribute(string key, string value, Attributes parent = null)
        {
            Validate.NotEmpty(key);
            Validate.NotNull(value);
            this.key = key.Trim();
            this.value = value;
            Parent = parent;
        }

        /// <summary>
        /// Get or set the attribute key.
        /// </summary>
        /// <returns>the attribute key</returns>
        /// <value>the new key; must not be null when set</value>
        public string Key
        {
            get => key;
            set
            {
                Validate.NotNull(value);
                var key = value.Trim();
                Validate.NotEmpty(value);
                if (Parent != null)
                {
                    var i = Parent.IndexOfKey(key);
                    if (i != Attributes.NotFound)
                    {
                        Parent.keys[i] = value;
                    }
                }

                key = value;
            }
        }

        /// <summary>
        /// Get or set the attribute value.
        /// </summary>
        /// <returns>the attribute value</returns>
        /// <value>the new attribute value; must not be null when set</value>
        public string Value
        {
            get => value;
            set
            {
                Validate.NotNull(value);
                if (Parent != null)
                {
                    var i = Parent.IndexOfKey(key);
                    if (i != Attributes.NotFound)
                    {
                        Parent.vals[i] = value;
                    }
                }
                this.value = value;
            }
        }

        /// <summary>
        /// Get the HTML representation of this attribute.
        /// </summary>
        /// <remarks>
        /// e.g. <c>href="index.html"</c>.
        /// </remarks>
        /// <returns>HTML</returns>
        public string Html()
        {
            StringBuilder sb = StringUtil.BorrowBuilder();

            try
            {
                Html(sb, (new Document("")).OutputSettings);
            }
            catch (IOException exception)
            {
                throw new SerializationException(exception.Message);
            }
            return StringUtil.ReleaseBuilder(sb);
        }
        
        protected void Html(StringBuilder accum, DocumentOutputSettings outSettings) {
            Html(key, Value, accum, outSettings);
        }
        
        protected static void Html(string key, string val, StringBuilder accum, DocumentOutputSettings outSettings)
        {
            key = GetValidKey(key, outSettings.Syntax);
            if (key == null) return; // can't write it :(
            HtmlNoValidate(key, val, accum, outSettings);
        }

        public static void HtmlNoValidate(string key, string val, StringBuilder accum, DocumentOutputSettings outputSettings)
        {
            accum.Append(key);
            if (!ShouldCollapseAttribute(key, val, outputSettings))
            {
                accum.Append("=\"");
                Entities.Escape(accum, Attributes.CheckNotNull(val), outputSettings, true, false, false, false);
                accum.Append('"');
            }
        }
        
        private static readonly Regex xmlKeyValid = new Regex("[a-zA-Z_:][-a-zA-Z0-9_:.]*");
        private static readonly Regex xmlKeyReplace = new Regex("[^-a-zA-Z0-9_:.]");
        private static readonly Regex htmlKeyValid = new Regex("[^\\x00-\\x1f\\x7f-\\x9f \"'/=]+");
        private static readonly Regex htmlKeyReplace = new Regex("[\\x00-\\x1f\\x7f-\\x9f \"'/=]");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="syntax"></param>
        /// <returns></returns>
        public static string GetValidKey(string key, DocumentSyntax syntax)
        {
            // we consider HTML attributes to always be valid. XML checks key validity
            if (syntax == DocumentSyntax.Xml && !xmlKeyValid.IsMatch(key))
            {
                key = xmlKeyReplace.Replace(key, "");
                return xmlKeyValid.IsMatch(key) ? key : null; // null if could not be coerced
            }
            else if (syntax == DocumentSyntax.Html && !htmlKeyValid.IsMatch(key))
            {
                key = htmlKeyReplace.Replace(key, "");
                return htmlKeyValid.IsMatch(key) ? key : null; // null if could not be coerced
            }
            return key;
        }

        /// <summary>
        /// Get the string representation of this attribute, implemented as
        /// <see cref="Html">Html</see>
        /// .
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return Html();
        }

        /// <summary>
        /// Create a new Attribute from an unencoded key and a HTML attribute encoded value.
        /// </summary>
        /// <param name="unencodedKey">
        /// assumes the key is not encoded, as can be only run of simple \w chars.
        /// </param>
        /// <param name="encodedValue">HTML attribute encoded value</param>
        /// <returns>attribute</returns>
        internal static Attribute CreateFromEncoded(string unencodedKey, string encodedValue)
        {
            var value = Entities.Unescape(encodedValue, true);
            return new Attribute(unencodedKey, value);
        }

        internal bool IsDataAttribute()
        {
            return key.StartsWith(Attributes.dataPrefix, StringComparison.Ordinal)
                && key.Length > Attributes.dataPrefix.Length;
        }

        /// <summary>
        /// Collapsible if it's a boolean attribute and value is empty or same as name
        /// </summary>
        internal bool ShouldCollapseAttribute(DocumentOutputSettings @out)
        {
            return ShouldCollapseAttribute(key, value, @out);
        }

        internal static bool ShouldCollapseAttribute(string key, string value, DocumentOutputSettings @out)
        {
            return @out.Syntax == DocumentSyntax.Html &&
                   (string.Empty.Equals(value) || string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                   && IsBooleanAttribute(key);
        }
        
        /// <summary>
        /// Checks if this attribute name is defined as a boolean attribute in HTML5
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsBooleanAttribute(string key) {
            return Array.BinarySearch(booleanAttributes, key.ToLower()) >= 0;
        }

        /// <summary>
        /// Compares two <see cref="Attribute"/> instances for equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            Attribute attribute = obj as Attribute;
            if (attribute == null)
            {
                return false;
            }
            if (!key?.Equals(attribute.Key) ?? attribute.Key != null)
            {
                return false;
            }
            if (!value?.Equals(attribute.Value) ?? attribute.Value != null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int result = key != null ? key.GetHashCode() : 0;
            result = 31 * result + (value != null ? value.GetHashCode() : 0);
            return result;
        }

        internal Attribute Clone()
        {
            return (Attribute)this.MemberwiseClone();
            // only fields are immutable strings key and value, so no more deep copy required
        }
    }
}
