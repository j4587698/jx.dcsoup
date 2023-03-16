using System;
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
        /// Returns space padding
        /// </summary>
        /// <param name="width">amount of padding desired</param>
        /// <returns>string of spaces * width</returns>
        public static string Padding(int width)
        {
            if (width < 0)
            {
                throw new ArgumentException("width must be > 0");
            }
            if (width < padding.Length)
            {
                return padding[width];
            }
            char[] @out = new char[width];
            for (int i = 0; i < width; i++)
            {
                @out[i] = ' ';
            }
            return new string(@out);
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
        public static bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\r';
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
    }
}
