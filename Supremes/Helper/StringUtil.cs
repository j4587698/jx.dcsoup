using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Supremes.Helper
{
    /// <summary>
    /// A minimal String utility class.
    /// </summary>
    /// <remarks>
    /// Designed for internal jsoup use only.
    /// </remarks>
    internal static class StringUtil
    {
        private static readonly string[] padding = new string[] {
            string.Empty,
            " ",
            "  ",
            "   ",
            "    ",
            "     ",
            "      ",
            "       ",
            "        ",
            "         ",
            "          "
        }; // memoised padding up to 10

        /// <summary>
        /// Join a collection of strings by a separator
        /// </summary>
        /// <param name="strings">collection of string objects</param>
        /// <param name="seq">string to place between strings</param>
        /// <returns>joined string</returns>
        public static string Join(IEnumerable strings, string seq)
        {
            return Join(strings.GetEnumerator(), seq);
        }
        
        /// <summary>
        /// Join a collection of strings by a separator
        /// </summary>
        /// <param name="strings">iterator of string objects</param>
        /// <param name="sep">string to place between strings</param>
        /// <returns>joined string</returns>
        public static String Join(IEnumerator strings, String sep) {
            if (!strings.MoveNext())
                return "";
            String start = strings.Current?.ToString();
            if (!strings.MoveNext()) // only one, avoid builder
                return start;

            StringBuilder sb = new StringBuilder(start);
            while (strings.MoveNext()) {
                sb.Append(sep).Append(strings.Current);
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// A StringJoiner allows incremental / filtered joining of a set of stringable objects.
        /// </summary>
        public class StringJoiner
        {
            private StringBuilder sb = BorrowBuilder();
            
            private readonly string separator;
            
            private bool first = true;
            
            /// <summary>
            /// Create a new joiner, that uses the specified separator. MUST call {@link #complete()} or will leak a thread
            /// </summary>
            /// <param name="separator">the token to insert between strings</param>
            public StringJoiner(string separator)
            {
                this.separator = separator;
            }
            
            /// <summary>
            /// Add another item to the joiner, will be separated
            /// </summary>
            /// <param name="stringy"></param>
            /// <returns></returns>
            public StringJoiner Add(object stringy) {
                if (!first)
                    sb.Append(separator);
                sb.Append(stringy);
                first = false;
                return this;
            }
            
            /// <summary>
            /// Append content to the current item; not separated
            /// </summary>
            /// <param name="stringy"></param>
            /// <returns></returns>
            public StringJoiner Append(object stringy) {
                sb.Append(stringy);
                return this;
            }
            
            /// <summary>
            /// Return the joined string, and release the builder back to the pool. This joiner cannot be reused.
            /// </summary>
            /// <returns></returns>
            public string Complete() {
                string str = ReleaseBuilder(sb);
                sb = null;
                return str;
            }
        }

        /// <summary>
        /// Returns space padding, up to a max of maxPaddingWidth.
        /// </summary>
        /// <param name="width">amount of padding desired</param>
        /// <param name="maxPaddingWidth">maximum padding to apply. Set to {@code -1} for unlimited.</param>
        /// <returns>string of spaces * width</returns>
        public static string Padding(int width, int maxPaddingWidth = 30)
        {
            Validate.IsTrue(width >= 0, "width must be >= 0");
            Validate.IsTrue(maxPaddingWidth >= -1);
            if (maxPaddingWidth != -1)
                width = Math.Min(width, maxPaddingWidth);
            if (width < padding.Length)
                return padding[width];        
            char[] outChars = new char[width];
            for (int i = 0; i < width; i++)
                outChars[i] = ' ';
            return new string(outChars);
        }

        /// <summary>
        /// Tests if a string is numeric
        /// </summary>
        /// <remarks>
        /// i.e. contains only digit characters
        /// </remarks>
        /// <param name="string">string to test</param>
        /// <returns>
        /// true if only digit chars, false if empty or null or contains non-digit chrs
        /// </returns>
        public static bool IsNumeric(string @string)
        {
            if (string.IsNullOrEmpty(@string))
            {
                return false;
            }
            int l = @string.Length;
            for (int i = 0; i < l; i++)
            {
                if (!char.IsDigit(@string[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests if a code point is "whitespace" as defined in the HTML spec.
        /// </summary>
        /// <param name="c">UTF-16 character to test</param>
        /// <returns>true if UTF-16 character is whitespace, false otherwise</returns>
        public static bool IsWhitespace(int c)
        {
            return c is ' ' or '\t' or '\n' or '\f' or '\r';
        }

        /// <summary>
        /// Normalise the whitespace within this string
        /// </summary>
        /// <remarks>
        /// multiple spaces collapse to a single, and all whitespace characters
        /// (e.g. newline, tab) convert to a simple space
        /// </remarks>
        /// <param name="string">content to normalise</param>
        /// <returns>normalised string</returns>
        public static string NormaliseWhitespace(string @string)
        {
            StringBuilder sb = new StringBuilder(@string.Length);
            AppendNormalisedWhitespace(sb, @string, false);
            return sb.ToString();
        }

        /// <summary>
        /// After normalizing the whitespace within a string, appends it to a string builder.
        /// </summary>
        /// <param name="accum">builder to append to</param>
        /// <param name="string">string to normalize whitespace within</param>
        /// <param name="stripLeading">
        /// set to true if you wish to remove any leading whitespace
        /// </param>
        /// <returns></returns>
        public static void AppendNormalisedWhitespace(StringBuilder accum, string @string, bool stripLeading)
        {
            bool lastWasWhite = false;
            bool reachedNonWhite = false;
            int len = @string.Length;
            char c;
            for (int i = 0; i < len; i++)
            {
                c = @string[i];
                if (IsWhitespace(c))
                {
                    if ((stripLeading && !reachedNonWhite) || lastWasWhite)
                    {
                        continue;
                    }
                    accum.Append(' ');
                    lastWasWhite = true;
                }
                else
                {
                    accum.Append(c);
                    lastWasWhite = false;
                    reachedNonWhite = true;
                }
            }
        }

        public static bool In(string needle, params string[] haystack)
        {
            foreach (string hay in haystack)
            {
                if (hay.Equals(needle))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="needle"></param>
        /// <param name="haystack"></param>
        /// <returns></returns>
        public static bool InSorted(string needle, string[] haystack)
        {
            return Array.BinarySearch(haystack, needle) >= 0;
        }

        /// <summary>
        /// Tests that a String contains only ASCII characters.
        /// </summary>
        /// <param name="str">scanned string</param>
        /// <returns>true if all characters are in range 0 - 127</returns>
        public static bool IsAscii(string str)
        {
            Validate.NotNull(str);
            for (int i = 0; i < str.Length; i++)
            {
                int c = str[i];
                if (c > 127)
                {
                    return false;
                }
            }
            return true;
        }

        private static readonly Regex ExtraDotSegmentsPattern = new Regex("^/((\\.{1,2}/)+)");
        
        /// <summary>
        /// Create a new absolute URL, from a provided existing absolute URL and a relative URL component.
        /// </summary>
        /// <param name="baseUri">the existing absolute base URL</param>
        /// <param name="relUrl">the relative URL to resolve. (If it's already absolute, it will be returned)</param>
        /// <returns>the resolved absolute URL</returns>
        public static Uri Resolve(Uri baseUri, string relUrl)
        {
            relUrl = StripControlChars(relUrl);
            // workaround: java resolves '//path/file + ?foo' to '//path/?foo', not '//path/file?foo' as desired
            if (relUrl.StartsWith("?"))
                relUrl = baseUri.AbsolutePath + relUrl;
            // workaround: //example.com + ./foo = //example.com/./foo, not //example.com/foo
            Uri url = new Uri(baseUri, relUrl);
            string fixedFile = ExtraDotSegmentsPattern.Replace(url.AbsolutePath, "/");
            if (url.Fragment != null)
            {
                fixedFile = fixedFile + "#" + url.Fragment;
            }
            return new Uri(url.Scheme + "://" + url.Host + ":" + url.Port + fixedFile);;
        }
        
        /// <summary>
        /// Create a new absolute URL, from a provided existing absolute URL and a relative URL component.
        /// </summary>
        /// <param name="baseUrl">the existing absolute base URL</param>
        /// <param name="relUrl">the relative URL to resolve. (If it's already absolute, it will be returned)</param>
        /// <returns>an absolute URL if one was able to be generated, or the empty string if not</returns>
        public static string Resolve(string baseUrl, string relUrl)
        {
            // workaround: java will allow control chars in a path URL and may treat as relative, but Chrome / Firefox will strip and may see as a scheme. Normalize to browser's view.
            baseUrl = StripControlChars(baseUrl); relUrl = StripControlChars(relUrl);
            try
            {
                Uri baseUri;
                try
                {
                    baseUri = new Uri(baseUrl);
                }
                catch (UriFormatException e)
                {
                    // the base is unsuitable, but the attribute/rel may be abs on its own, so try that
                    Uri abs = new Uri(relUrl);
                    return abs.ToString();
                }
                return Resolve(baseUri, relUrl).ToString();
            }
            catch (UriFormatException e)
            {
                // it may still be valid, just that Java doesn't have a registered stream handler for it, e.g. tel
                // we test here vs at start to normalize supported URLs (e.g. HTTP -> http)
                return validUriScheme.IsMatch(relUrl) ? relUrl : "";
            }
        }
        
        private static readonly Regex validUriScheme = new Regex("^[a-zA-Z][a-zA-Z0-9+-.]*:");
        
        private static readonly Regex controlChars = new Regex("[\\x00-\\x1f]*"); // matches ascii 0 - 31, to strip from url
        
        private static string StripControlChars(string input)
        {
            return controlChars.Replace(input, "");
        }
        
        private static readonly ThreadLocal<Stack<StringBuilder>> threadLocalBuilders = new ThreadLocal<Stack<StringBuilder>>(() => new Stack<StringBuilder>());
        
        public static int ConvertToDecimal(string input, int sourceBase) {
            string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
            input = input.ToLower();
            int decimalValue = 0;
            for (int i = 0; i < input.Length; i++) {
                int digitValue = digits.IndexOf(input[i]);
                if (digitValue < 0 || digitValue >= sourceBase) {
                    throw new ArgumentException("Invalid digit: " + input[i]);
                }
                decimalValue = decimalValue * sourceBase + digitValue;
            }

            return decimalValue;
        }
        
        /// <summary>
        /// Maintains cached StringBuilders in a flyweight pattern, to minimize new StringBuilder GCs. The StringBuilder is
        /// prevented from growing too large.
        /// <p>
        /// Care must be taken to release the builder once its work has been completed, with {@link #releaseBuilder}
        /// </summary>
        /// <returns>an empty StringBuilder</returns>
        public static StringBuilder BorrowBuilder() {
            Stack<StringBuilder> builders = threadLocalBuilders.Value;
            return builders.Count == 0 ?
                new StringBuilder(MaxCachedBuilderSize) :
                builders.Pop();
        }
        
        
        public static string ReleaseBuilder(StringBuilder sb)
        {
            Validate.NotNull(sb);
            
            string result = sb.ToString();

            if (sb.Length > MaxCachedBuilderSize)
                sb = new StringBuilder(MaxCachedBuilderSize); // make sure it hasn't grown too big
            else
                sb.Clear(); // make sure it's emptied on release

            Stack<StringBuilder> builders = threadLocalBuilders.Value;
            builders.Push(sb);

            while (builders.Count > MaxIdleBuilders)
            {
                builders.Pop();
            }
            return result;
        }
        
        private const int MaxCachedBuilderSize = 8 * 1024;
        private const int MaxIdleBuilders = 8;
    }
}
