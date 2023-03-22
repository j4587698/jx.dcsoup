#if (NETSTANDARD1_3)
using System.Reflection;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using Supremes.Helper;
using Supremes.Parsers;
using Utf32 = System.Int32;

namespace Supremes.Nodes
{
    /// <summary>
    /// HTML entities, and escape routines.
    /// </summary>
    /// <remarks>
    /// Source: <a href="http://www.w3.org/TR/html5/named-character-references.html#named-character-references">W3C HTML named character references</a>.
    /// </remarks>
    public static class Entities
    {
        public enum EscapeMode
        {
            Xhtml,
            Base,
            Extended
        }
        
        private const int empty = -1;
        private const string emptyName = "";
        
        internal const int codepointRadix = 36;
        private static readonly char[] codeDelims = new char[] {',', ';' };
        
        private static readonly Dictionary<String, String> multipoints = new(); // name -> multiple character references


        private static string[] xhtmlNameKeys;
        private static int[] xhtmlCodeVals;
        private static int[] xhtmlCodeKeys;
        private static string[] xhtmlNameVals;
        
        private static string[] baseNameKeys;
        private static int[] baseCodeVals;
        private static int[] baseCodeKeys;
        private static string[] baseNameVals;
        
        private static string[] extendedNameKeys;
        private static int[] extendedCodeVals;
        private static int[] extendedCodeKeys;
        private static string[] extendedNameVals;
        
        internal static int CodepointForName(EscapeMode e, string name)
        {
            string[] nameKeys;
            int[] codeVals;
            switch (e)
            {
                case EscapeMode.Xhtml:
                    nameKeys = xhtmlNameKeys;
                    codeVals = xhtmlCodeVals;
                    break;
                case EscapeMode.Base:
                    nameKeys = baseNameKeys;
                    codeVals = baseCodeVals;
                    break;
                case EscapeMode.Extended:
                    nameKeys = extendedNameKeys;
                    codeVals = extendedCodeVals;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
            int index = Array.BinarySearch(nameKeys, name);
            return index >= 0 ? codeVals[index] : empty;
        }
        
        internal static string NameForCodepoint(EscapeMode e, int codepoint)
        {
            string[] nameVals;
            int[] codeKeys;
            switch (e)
            {
                case EscapeMode.Xhtml:
                    codeKeys = xhtmlCodeKeys;
                    nameVals = xhtmlNameVals;
                    break;
                case EscapeMode.Base:
                    codeKeys = baseCodeKeys;
                    nameVals = baseNameVals;
                    break;
                case EscapeMode.Extended:
                    codeKeys = extendedCodeKeys;
                    nameVals = extendedNameVals;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
            int index = Array.BinarySearch(codeKeys, codepoint);
            if (index >= 0)
            {
                // the results are ordered so lower case versions of same codepoint come after uppercase, and we prefer to emit lower
                // (and binary search for same item with multi results is undefined
                return (index < nameVals.Length - 1 && codeKeys[index + 1] == codepoint) ?
                    nameVals[index + 1] : nameVals[index];
            }
            return emptyName;
        }

        /// <summary>
        /// Check if the input is a known named entity
        /// </summary>
        /// <param name="name">the possible entity name (e.g. "lt" or "amp")</param>
        /// <returns>true if a known named entity</returns>
        public static bool IsNamedEntity(string name)
        {
            return CodepointForName(EscapeMode.Extended, name) != empty;
        }

        /// <summary>
        /// Check if the input is a known named entity in the base entity set.
        /// </summary>
        /// <param name="name">the possible entity name (e.g. "lt" or "amp")</param>
        /// <returns>true if a known named entity in the base set</returns>
        /// <seealso cref="IsNamedEntity(string)">IsNamedEntity(string)</seealso>
        public static bool IsBaseNamedEntity(string name)
        {
            return CodepointForName(EscapeMode.Base, name) != empty;
        }
        
        /// <summary>
        /// Get the character(s) represented by the named entity
        /// </summary>
        /// <param name="name">entity (e.g. "lt" or "amp")</param>
        /// <returns>the string value of the character(s) represented by this entity, or "" if not defined</returns>
        public static string GetByName(string name)
        {
            if (multipoints.TryGetValue(name, out string val))
                return val;
            int codepoint = CodepointForName(EscapeMode.Extended, name);
            if (codepoint != empty)
                return char.ConvertFromUtf32(codepoint);
            return emptyName;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="codepoints"></param>
        /// <returns></returns>
        public static int CodepointsForName(string name, int[] codepoints)
        {
            if (multipoints.TryGetValue(name, out string val))
            {
                codepoints[0] = char.ConvertToUtf32(val, 0);
                codepoints[1] = char.ConvertToUtf32(val, 1);
                return 2;
            }
            int codepoint = CodepointForName(EscapeMode.Extended, name);
            if (codepoint != empty)
            {
                codepoints[0] = codepoint;
                return 1;
            }
            return 0;
        }

        public static string Escape(string input, DocumentOutputSettings outputSettings)
        {
            if (input == null)
                return "";
            StringBuilder accum = StringUtil.BorrowBuilder();
            try
            {
                Escape(accum, input, outputSettings, false, false, false, false);
            }
            catch (IOException e)
            {
                throw new SerializationException(e.Message); // doesn't happen
            }
            return StringUtil.ReleaseBuilder(accum);
        }
        
        public static string Escape(string inputString)
        {
            if (DefaultOutput == null)
                DefaultOutput = new DocumentOutputSettings();
            return Escape(inputString, DefaultOutput);
        }
        private static DocumentOutputSettings DefaultOutput; // lazy-init, to break circular dependency with OutputSettings

        // this method is ugly, and does a lot. but other breakups cause rescanning and stringbuilder generations
        internal static void Escape(StringBuilder accum, string inputString, DocumentOutputSettings outSettings,
            bool inAttribute, bool normaliseWhite, bool stripLeadingWhite, bool trimTrailing)
        {
            bool lastWasWhite = false;
            bool reachedNonWhite = false;
            EscapeMode escapeMode = outSettings.EscapeMode;
            CharsetEncoder encoder = outSettings.Encoder;
            CoreCharset coreCharset = outSettings.CoreCharset;
            int length = inputString.Length;

            int codePoint;
            bool skipped = false;
            for (int offset = 0; offset < length; offset += codePoint >= 0x010000 ? 2 : 1)
            {
                codePoint = inputString[offset];

                if (normaliseWhite)
                {
                    if (StringUtil.IsWhitespace(codePoint))
                    {
                        if (stripLeadingWhite && !reachedNonWhite) continue;
                        if (lastWasWhite) continue;
                        if (trimTrailing)
                        {
                            skipped = true;
                            continue;
                        }

                        accum.Append(' ');
                        lastWasWhite = true;
                        continue;
                    }
                    else
                    {
                        lastWasWhite = false;
                        reachedNonWhite = true;
                        if (skipped)
                        {
                            accum.Append(' '); // wasn't the end, so need to place a normalized space
                            skipped = false;
                        }
                    }
                }

                if (codePoint < 65536)
                {
                    char c = (char)codePoint;
                    switch (c)
                    {
                        case '&':
                            accum.Append("&amp;");
                            break;
                        case (char)0xA0:
                            if (escapeMode != EscapeMode.Xhtml)
                                accum.Append("&nbsp;");
                            else
                                accum.Append("&#xa0;");
                            break;
                        case '<':
                            if (!inAttribute || escapeMode == EscapeMode.Xhtml || outSettings.Syntax == DocumentSyntax.Xml)
                                accum.Append("&lt;");
                            else
                                accum.Append(c);
                            break;
                        case '>':
                            if (!inAttribute)
                                accum.Append("&gt;");
                            else
                                accum.Append(c);
                            break;
                        case '"':
                            if (inAttribute)
                                accum.Append("&quot;");
                            else
                                accum.Append(c);
                            break;
                        case (char)0x9:
                        case (char)0xA:
                        case (char)0xD:
                            accum.Append(c);
                            break;
                        default:
                            if (c < 0x20 || !CanEncode(coreCharset, c, encoder))
                                AppendEncoded(accum, escapeMode, codePoint);
                            else
                                accum.Append(c);
                            break;
                    }
                }
                else
                {
                    string c = char.ConvertFromUtf32(codePoint);
                    if (encoder.CanEncode(c.ToCharArray()))
                        accum.Append(c);
                    else
                        AppendEncoded(accum, escapeMode, codePoint);
                }
            }
        }

        private static void AppendEncoded(StringBuilder accum, EscapeMode escapeMode, int codePoint) {
            string name = NameForCodepoint(escapeMode, codePoint);
            if (!emptyName.Equals(name)) // ok for identity check
                accum.Append('&').Append(name).Append(';');
            else
                accum.Append("&#x").Append(codePoint.ToString("x")).Append(';');
        }


        internal static string Unescape(string @string)
        {
            return Unescape(@string, false);
        }

        /// <summary>
        /// Unescape the input string.
        /// </summary>
        /// <param name="string"></param>
        /// <param name="strict">if "strict" (that is, requires trailing ';' char, otherwise that's optional)
        /// </param>
        /// <returns></returns>
        internal static string Unescape(string @string, bool strict)
        {
            return Parser.UnescapeEntities(@string, strict);
        }
        
        private static bool CanEncode(CoreCharset charset, char c, CharsetEncoder fallback)
        {
            
            switch (charset)
            {
                case CoreCharset.Ascii:
                    return c < 0x80;
                case CoreCharset.Utf:
                    return true; // Real is: !(char.IsLowSurrogate(c) || char.IsHighSurrogate(c)); - but already checked above
                default:
                    return fallback.CanEncode(new []{c});
            }
        }
        
        internal enum CoreCharset {
            Ascii, 
            Utf, 
            Fallback
        }
        
        internal static CoreCharset CoreCharsetByName(string name)
        {
            name = name.ToUpper();
            if (name.Equals("US-ASCII"))
                return CoreCharset.Ascii;
            if (name.StartsWith("UTF-")) // covers UTF-8, UTF-16, et al
                return CoreCharset.Utf;
            return CoreCharset.Fallback;
        }

        static Entities()
        {
            // xhtml has restricted entities
            LoadEntities(EscapeMode.Xhtml, EntitiesData.xmlPoints, 4); // xhtml entities
            LoadEntities(EscapeMode.Base, EntitiesData.basePoints, 106); // most common / default
            LoadEntities(EscapeMode.Extended, EntitiesData.fullPoints, 2125); // extended and overblown.
        }

        private static void LoadEntities(EscapeMode e, string pointData, int size)
        {
            string[] nameKeys;
            int[] codeVals;
            int[] codeKeys;
            string[] nameVals;
            switch (e)
            {
                case Entities.EscapeMode.Xhtml:
                    xhtmlNameKeys = new string[size];
                    xhtmlCodeVals = new int[size];
                    xhtmlCodeKeys = new int[size];
                    xhtmlNameVals = new string[size];
                    nameKeys = xhtmlNameKeys;
                    codeVals = xhtmlCodeVals;
                    codeKeys = xhtmlCodeKeys;
                    nameVals = xhtmlNameVals;
                    break;
                case Entities.EscapeMode.Base:
                    baseNameKeys = new string[size];
                    baseCodeVals = new int[size];
                    baseCodeKeys = new int[size];
                    baseNameVals = new string[size];
                    nameKeys = baseNameKeys;
                    codeVals = baseCodeVals;
                    codeKeys = baseCodeKeys;
                    nameVals = baseNameVals;
                    break;
                case Entities.EscapeMode.Extended:
                    extendedNameKeys = new string[size];
                    extendedCodeVals = new int[size];
                    extendedCodeKeys = new int[size];
                    extendedNameVals = new string[size];
                    nameKeys = extendedNameKeys;
                    codeVals = extendedCodeVals;
                    codeKeys = extendedCodeKeys;
                    nameVals = extendedNameVals;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e), e, null);
            }
            int i = 0;
            var reader = new CharacterReader(pointData);
            try
            {
                while (!reader.IsEmpty())
                {
                    var name = reader.ConsumeTo('=');
                    reader.Advance();
                    
                    var cp1 = StringUtil.ConvertToDecimal(reader.ConsumeToAny(codeDelims), codepointRadix);
                    var codeDelim = reader.Current();
                    reader.Advance();
                    int cp2;
                    if (codeDelim == ',')
                    {
                        cp2 = StringUtil.ConvertToDecimal(reader.ConsumeTo(';'), codepointRadix);
                    }
                    else
                    {
                        cp2 = empty;
                    }

                    string indexS = reader.ConsumeTo('&');
                    int index = StringUtil.ConvertToDecimal(indexS, codepointRadix);
                    reader.Advance();
                    nameKeys[i] = name;
                    codeVals[i] = cp1;
                    codeKeys[index] = cp1;
                    nameVals[index] = name;

                    if (cp2 != empty)
                    {
                        multipoints.Add(name, new string(new []{(char)cp1, (char)cp2}, 0, 2));
                    }

                    i++;
                }
                Validate.IsTrue(i == size, "Unexpected count of entities loaded");
            }
            finally
            {
                reader.Close();
            }
        }
    }
}
