using Supremes.Helper;
using System.Globalization;

namespace Supremes.Parsers
{
    /// <summary>
    /// CharacterReader consumes tokens off a string.
    /// </summary>
    /// <remarks>
    /// To replace the old TokenQueue.
    /// </remarks>
    internal class CharacterReader
    {
        internal const char EOF = '\uffff';
        
        private const int maxStringCacheLen = 12;

        private readonly string input;

        private readonly int length;

        private int pos = 0;

        private int mark = 0;
        
        private const int stringCacheSize = 512;
        private string[] stringCache = new string[stringCacheSize]; // 在此文档中保存重用的字符串，以减少垃圾。

        internal CharacterReader(string input)
        {
            Validate.NotNull(input);
            this.input = input;
            this.length = this.input.Length;
        }

        internal int Pos()
        {
            return pos;
        }

        internal bool IsEmpty()
        {
            return pos >= length;
        }

        internal char Current()
        {
            return pos >= length ? EOF : input[pos];
        }

        internal char Consume()
        {
            char val = pos >= length ? EOF : input[pos];
            pos++;
            return val;
        }

        internal void Unconsume()
        {
            pos--;
        }

        internal void Advance()
        {
            pos++;
        }

        internal void Mark()
        {
            mark = pos;
        }

        internal void RewindToMark()
        {
            pos = mark;
        }

        internal string ConsumeAsString()
        {
            return input.Substring(pos++, 1);
        }

        /// <summary>
        /// Returns the number of characters between the current position and the next instance of the input char
        /// </summary>
        /// <param name="c">scan target</param>
        /// <returns>
        /// offset between current position and next instance of target. -1 if not found.
        /// </returns>
        internal int NextIndexOf(char c)
        {
            // doesn't handle scanning for surrogates
            for (int i = pos; i < length; i++)
            {
                if (c == input[i])
                {
                    return i - pos;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the number of characters between the current position and the next instance of the input sequence
        /// </summary>
        /// <param name="seq">scan target</param>
        /// <returns>
        /// offset between current position and next instance of target. -1 if not found.
        /// </returns>
        internal int NextIndexOf(string seq)
        {
            // doesn't handle scanning for surrogates
            char startChar = seq[0];
            for (int offset = pos; offset < length; offset++)
            {
                // scan to first instance of startchar:
                if (startChar != input[offset])
                {
                    while (++offset < length && startChar != input[offset])
                    {
                    }
                }
                int i = offset + 1;
                int last = i + seq.Length - 1;
                if (offset < length && last <= length)
                {
                    for (int j = 1; i < last && seq[j] == input[i]; i++, j++)
                    {
                    }
                    if (i == last)
                    {
                        // found full sequence
                        return offset - pos;
                    }
                }
            }
            return -1;
        }

        internal string ConsumeTo(char c)
        {
            int offset = NextIndexOf(c);
            if (offset != -1)
            {
                string consumed = input.Substring(pos, offset);
                pos += offset;
                return consumed;
            }
            else
            {
                return ConsumeToEnd();
            }
        }

        internal string ConsumeTo(string seq)
        {
            int offset = NextIndexOf(seq);
            if (offset != -1)
            {
                string consumed = input.Substring(pos, offset);
                pos += offset;
                return consumed;
            }
            else
            {
                return ConsumeToEnd();
            }
        }

        internal string ConsumeToAny(params char[] chars)
        {
            int start = pos;
            while (pos < length)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    if (input[pos] == chars[i])
                    {
                        goto OUTER_break;
                    }
                }
                pos++;
            }
        OUTER_break:
            return pos > start ? input.Substring(start, pos - start) : string.Empty;
        }

        internal string ConsumeToEnd()
        {
            string data = input.Substring(pos, length - pos);
            pos = length;
            return data;
        }

        internal string ConsumeLetterSequence()
        {
            int start = pos;
            while (pos < length)
            {
                char c = input[pos];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }
            return input.Substring(start, pos - start);
        }

        internal string ConsumeLetterThenDigitSequence()
        {
            int start = pos;
            while (pos < length)
            {
                char c = input[pos];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }
            while (!IsEmpty())
            {
                char c = input[pos];
                if (c >= '0' && c <= '9')
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }
            return input.Substring(start, pos - start);
        }

        internal string ConsumeHexSequence()
        {
            int start = pos;
            while (pos < length)
            {
                char c = input[pos];
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }
            return input.Substring(start, pos - start);
        }

        internal string ConsumeDigitSequence()
        {
            int start = pos;
            while (pos < length)
            {
                char c = input[pos];
                if (c >= '0' && c <= '9')
                {
                    pos++;
                }
                else
                {
                    break;
                }
            }
            return input.Substring(start, pos - start);
        }

        internal bool Matches(char c)
        {
            return !IsEmpty() && input[pos] == c;
        }

        internal bool Matches(string seq)
        {
            int scanLength = seq.Length;
            if (scanLength > length - pos)
            {
                return false;
            }
            for (int offset = 0; offset < scanLength; offset++)
            {
                if (seq[offset] != input[pos + offset])
                {
                    return false;
                }
            }
            return true;
        }

        internal bool MatchesIgnoreCase(string seq)
        {
            int scanLength = seq.Length;
            if (scanLength > length - pos)
            {
                return false;
            }
            for (int offset = 0; offset < scanLength; offset++)
            {
                char upScan = System.Char.ToUpper(seq[offset]);
                char upTarget = System.Char.ToUpper(input[pos + offset]);
                if (upScan != upTarget)
                {
                    return false;
                }
            }
            return true;
        }

        internal bool MatchesAny(params char[] seq)
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = input[pos];
            foreach (char seek in seq)
            {
                if (seek == c)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool MatchesLetter()
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = input[pos];
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        internal bool MatchesDigit()
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = input[pos];
            return (c >= '0' && c <= '9');
        }

        internal bool MatchConsume(string seq)
        {
            if (Matches(seq))
            {
                pos += seq.Length;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool MatchConsumeIgnoreCase(string seq)
        {
            if (MatchesIgnoreCase(seq))
            {
                pos += seq.Length;
                return true;
            }
            else
            {
                return false;
            }
        }

        internal bool ContainsIgnoreCase(string seq)
        {
            // used to check presence of </title>, </style>. only finds consistent case.
            string loScan = seq.ToLowerInvariant();
            string hiScan = seq.ToUpperInvariant();

            return (NextIndexOf(loScan) > -1) || (NextIndexOf(hiScan) > -1);
        }

        public override string ToString()
        {
            return input.Substring(pos, length - pos);
        }
        
        private static string CacheString(char[] charBuf, string[] stringCache, int start, int count) {
            // limit (no cache):
            if (count > maxStringCacheLen)
                return new string(charBuf, start, count);
            if (count < 1)
                return "";

            // calculate hash:
            int hash = 0;
            for (int i = 0; i < count; i++) {
                hash = 31 * hash + charBuf[start + i];
            }

            // get from cache
            int index = hash & (stringCacheSize - 1);
            string cached = stringCache[index];

            if (cached != null && RangeEquals(charBuf, start, count, cached)) // positive hit
                return cached;
            else {
                cached = new string(charBuf, start, count);
                stringCache[index] = cached; // add or replace, assuming most recently used are most likely to recur next
            }

            return cached;
        }

        private static bool RangeEquals(char[] charBuf, int start, int count, string cached) {
            if (cached.Length != count) {
                return false;
            }

            for (int i = 0; i < count; i++) {
                if (charBuf[start + i] != cached[i]) {
                    return false;
                }
            }

            return true;
        }
    }
    
    
}
