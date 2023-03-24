using System;
using System.Collections.Generic;
using Supremes.Helper;
using System.Globalization;
using System.IO;
using StringReader = Supremes.Helper.StringReader;

namespace Supremes.Parsers
{
    /// <summary>
    /// CharacterReader consumes tokens off a string.
    /// </summary>
    /// <remarks>
    /// To replace the old TokenQueue.
    /// </remarks>
    public class CharacterReader
    {
        internal const char EOF = '\uffff';
        
        private const int maxStringCacheLen = 12;
        public const int maxBufferLen = 1024 * 32;
        public const int readAheadLimit = (int)(maxBufferLen * 0.75);
        private const int minReadAheadLen = 1024;

        private char[] charBuf;
        private StringReader reader;
        private int bufLength;
        private int bufSplitPoint;
        private int bufPos;
        private int readerPos;
        private int bufMark = -1;
        private const int stringCacheSize = 512;
        private string[] stringCache = new string[stringCacheSize];

        private List<int>? newlinePositions = null;
        private int lineNumberOffset = 1;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="sz"></param>
        public CharacterReader(StringReader input, int sz) {
            Validate.NotNull(input);
            Validate.IsTrue(input.MarkSupported);
            reader = input;
            charBuf = new Char[Math.Min(sz, maxBufferLen)];
            BufferUp();
        }
        
        public CharacterReader(StringReader input): this(input, maxBufferLen)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public CharacterReader(string input): this(new StringReader(input), maxBufferLen)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void Close() {
            if (reader == null)
                return;
            try {
                reader.Close();
            } finally {
                reader = null;
                charBuf = null;
                stringCache = null;
            }
        }
        
        private bool readFully; // if the underlying stream has been completely read, no value in further buffering
        
        private void BufferUp()
        {
            if (readFully || bufPos < bufSplitPoint)
                return;
            
            int pos;
            int offset;
            if (bufMark != -1)
            {
                pos = bufMark;
                offset = bufPos - bufMark;
            }
            else
            {
                pos = bufPos;
                offset = 0;
            }

            try
            {
                long skipped = reader.Skip(pos);
               reader.Mark(maxBufferLen);

                int read = 0;
                while (read <= minReadAheadLen)
                {
                    int remaining = charBuf.Length - read;
                    int thisRead = reader.Read(charBuf, read, remaining);
                    if (thisRead == 0)
                    {
                        readFully = true;
                        break;
                    }
                    
                    read += thisRead;
                }

                reader.Reset();

                if (read > 0)
                {
                    Validate.IsTrue(skipped == pos); // Previously asserted that there is room in buf to skip, so this will be a WTF
                    bufLength = read;
                    readerPos += pos;
                    bufPos = offset;
                    if (bufMark != -1)
                        bufMark = 0;
                    bufSplitPoint = Math.Min(bufLength, readAheadLimit);
                }
            }
            catch (IOException e)
            {
                throw new UncheckedIOException(e);
            }

            ScanBufferForNewlines(); // if enabled, we index newline positions for line number tracking
            lastIcSeq = null; // cache for last containsIgnoreCase(seq)
        }

        public int Pos()
        {
            return readerPos + bufPos;
        }
        
        /// <summary>
        /// Enables or disables line number tracking. By default, will be <b>off</b>.Tracking line numbers improves the legibility of parser error messages, for example. Tracking should be enabled before any content is read to be of use.
        /// </summary>
        /// <param name="track">set tracking on|off</param>
        public void TrackNewlines(bool track)
        {
            if (track && newlinePositions == null)
            {
                newlinePositions = new List<int>(maxBufferLen / 80); // rough guess of likely count
                ScanBufferForNewlines(); // first pass when enabled; subsequently called during bufferUp
            }
            else if (!track)
                newlinePositions = null;
        }
        
        public bool IsTrackNewlines => newlinePositions != null;

        /// <summary>
        /// Get the current line number (that the reader has consumed to). Starts at line #1.
        /// </summary>
        /// <returns>the current line number, or 1 if line tracking is not enabled.</returns>
        public int LineNumber()
        {
            return LineNumber(Pos());
        }

        internal int LineNumber(int pos) {
            // note that this impl needs to be called before the next buffer up or line numberoffset will be wrong. if that
            // causes issues, can remove the reset of newlinepositions during buffer, at the cost of a larger tracking array
            if (!IsTrackNewlines)
                return 1;

            int i = LineNumIndex(pos);
            if (i == -1)
                return lineNumberOffset; // first line
            return i + lineNumberOffset + 1;
        }
        
        /// <summary>
        /// Get the current column number (that the reader has consumed to). Starts at column #1.
        /// </summary>
        /// <returns>the current column number</returns>
        public int ColumnNumber()
        {
            return ColumnNumber(Pos());
        }

        internal string CursorPos()
        {
            return $"{LineNumber()}:{ColumnNumber()}";
        }
        
        internal int ColumnNumber(int pos) {
            if (!IsTrackNewlines)
                return pos + 1;

            int i = LineNumIndex(pos);
            if (i == -1)
                return pos + 1;
            return pos - newlinePositions[i] + 1;
        }
        
        private int LineNumIndex(int pos) {
            if (!IsTrackNewlines) return 0;
            int i = newlinePositions.BinarySearch(pos);
            if (i < -1) i = Math.Abs(i) - 2;
            return i;
        }
        
        private void ScanBufferForNewlines() {
            if (!IsTrackNewlines)
                return;

            if (newlinePositions.Count > 0) {
                // work out the line number that we have read up to (as we have likely scanned past this point)
                int index = LineNumIndex(readerPos);
                if (index == -1) index = 0; // first line
                int linePos = newlinePositions[index];
                lineNumberOffset += index; // the num lines we've read up to
                newlinePositions.Clear();
                newlinePositions.Add(linePos); // roll the last read pos to first, for cursor num after buffer
            }

            for (int i = bufPos; i < bufLength; i++) {
                if (charBuf[i] == '\n')
                    newlinePositions.Add(1 + readerPos + i);
            }
        }

        internal bool IsEmpty()
        {
            BufferUp();
            return bufPos >= bufLength;
        }
        
        private bool IsEmptyNoBufferUp() {
            return bufPos >= bufLength;
        }

        internal char Current()
        {
            BufferUp();
            return IsEmptyNoBufferUp() ? EOF : charBuf[bufPos];
        }

        internal char Consume()
        {
            BufferUp();
            char val = IsEmptyNoBufferUp() ? EOF : charBuf[bufPos];
            bufPos++;
            return val;
        }

        internal void Unconsume()
        {
            if (bufPos < 1)
                throw new UncheckedIOException(new IOException("WTF: No buffer left to unconsume.")); // a bug if this fires, need to trace it.
            bufPos--;
        }

        internal void Advance()
        {
            bufPos++;
        }

        internal void Mark()
        {
            // make sure there is enough look ahead capacity
            if (bufLength - bufPos < minReadAheadLen)
                bufSplitPoint = 0;

            BufferUp();
            bufMark = bufPos;
        }
        
        internal void Unmark() {
            bufMark = -1;
        }

        internal void RewindToMark()
        {
            if (bufMark == -1)
                throw new UncheckedIOException(new IOException("Mark invalid"));
            
            bufPos = bufMark;
            Unmark();
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
            BufferUp();
            for (int i = bufPos; i < bufLength; i++) {
                if (c == charBuf[i])
                    return i - bufPos;
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
            BufferUp();
            // doesn't handle scanning for surrogates
            char startChar = seq[0];
            for (int offset = bufPos; offset < bufLength; offset++) {
                // scan to first instance of startchar:
                if (startChar != charBuf[offset])
                    while(++offset < bufLength && startChar != charBuf[offset]) { /* empty */ }
                int i = offset + 1;
                int last = i + seq.Length-1;
                if (offset < bufLength && last <= bufLength) {
                    for (int j = 1; i < last && seq[j] == charBuf[i]; i++, j++) { /* empty */ }
                    if (i == last) // found full sequence
                        return offset - bufPos;
                }
            }
            return -1;
        }

        internal string ConsumeTo(char c)
        {
            int offset = NextIndexOf(c);
            if (offset != -1)
            {
                string consumed = CacheString(charBuf, stringCache, bufPos, offset);
                bufPos += offset;
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
                string consumed = CacheString(charBuf, stringCache, bufPos, offset);
                bufPos += offset;
                return consumed;
            }
            else if (bufLength - bufPos < seq.Length)
            {
                // nextIndexOf() did a bufferUp(), so if the buffer is shorter than the search string, we must be at EOF
                return ConsumeToEnd();
            }
            else
            {
                // the string we're looking for may be straddling a buffer boundary, so keep (length - 1) characters
                // unread in case they contain the beginning of the search string
                int endPos = bufLength - seq.Length + 1;
                String consumed = CacheString(charBuf, stringCache, bufPos, endPos - bufPos);
                bufPos = endPos;
                return consumed;
            }
        }

        internal string ConsumeToAny(params char[] chars)
        {
            BufferUp();
            int pos = bufPos;
            int start = pos;
            int remaining = bufLength;
            char[] val = charBuf;
            int charLen = chars.Length;

            while (pos < remaining)
            {
                for (int i = 0; i < charLen; i++)
                {
                    if (val[pos] == chars[i])
                        goto OUTER;
                }

                pos++;
            }

            OUTER:
            bufPos = pos;
            return pos > start ? CacheString(charBuf, stringCache, start, pos - start) : "";
        }
        
        internal string ConsumeToAnySorted(params char[] chars) {
            BufferUp();
            int pos = bufPos;
            int start = pos;
            int remaining = bufLength;
            char[] val = charBuf;

            while (pos < remaining) {
                if (Array.BinarySearch(chars, val[pos]) >= 0)
                    break;
                pos++;
            }
            bufPos = pos;
            return bufPos > start ? CacheString(charBuf, stringCache, start, pos -start) : "";
        }

        internal string ConsumeData()
        {
            // &, <, null
            //bufferUp(); // no need to bufferUp, just called consume()
            int pos = bufPos;
            int start = pos;
            int remaining = bufLength;
            char[] val = charBuf;

            while (pos < remaining)
            {
                switch (val[pos])
                {
                    case '&':
                    case '<':
                    case TokeniserState.nullChar:
                        goto OUTER;
                    default:
                        pos++;
                        break;
                }
            }

            OUTER:
            bufPos = pos;
            return pos > start ? CacheString(charBuf, stringCache, start, pos - start) : "";
        }
        
        internal string ConsumeAttributeQuoted(bool single)
        {
            // null, " or ', &
            //bufferUp(); // no need to bufferUp, just called consume()
            int pos = bufPos;
            int start = pos;
            int remaining = bufLength;
            char[] val = charBuf;

             while (pos < remaining)
            {
                switch (val[pos])
                {
                    case '&':
                    case TokeniserState.nullChar:
                        goto OUTER;
                    case '\'':
                        if (single) goto OUTER;
                        break;
                    case '"':
                        if (!single) goto OUTER;
                        break;
                    default:
                        pos++;
                        break;
                }
            }
             
             OUTER:
            bufPos = pos;
            return pos > start ? CacheString(charBuf, stringCache, start, pos - start) : "";
        }

        internal string ConsumeRawData() {
            // <, null
            //bufferUp(); // no need to bufferUp, just called consume()
            int pos = bufPos;
            int start = pos;
            int remaining = bufLength;
            char[] val = charBuf;

            while (pos < remaining) {
                switch (val[pos]) {
                    case '<':
                    case TokeniserState.nullChar:
                        goto OUTER;
                    default:
                        pos++;
                        break;
                }
            }
            OUTER: 
            bufPos = pos;
            return pos > start ? CacheString(charBuf, stringCache, start, pos -start) : "";
        }
        
        internal string ConsumeTagName() {
            // '\t', '\n', '\r', '\f', ' ', '/', '>'
            // NOTE: out of spec, added '<' to fix common author bugs; does not stop and append on nullChar but eats
            BufferUp();
            int pos = bufPos;
            int start = pos;
            int remaining = bufLength;
            char[] val = charBuf;

             while (pos < remaining) {
                switch (val[pos]) {
                    case '\t':
                    case '\n':
                    case '\r':
                    case '\f':
                    case ' ':
                    case '/':
                    case '>':
                    case '<':
                        goto OUTER;
                }
                pos++;
            }

             OUTER:
            bufPos = pos;
            return pos > start ? CacheString(charBuf, stringCache, start, pos -start) : "";
        }

        internal string ConsumeToEnd()
        {
            BufferUp();
            String data = CacheString(charBuf, stringCache, bufPos, bufLength - bufPos);
            bufPos = bufLength;
            return data;
        }

        internal string ConsumeLetterSequence()
        {
            BufferUp();
            int start = bufPos;
            while (bufPos < bufLength) {
                char c = charBuf[bufPos];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || char.IsLetter(c))
                    bufPos++;
                else
                    break;
            }

            return CacheString(charBuf, stringCache, start, bufPos - start);
        }

        internal string ConsumeLetterThenDigitSequence()
        {
            BufferUp();
            int start = bufPos;
            while (bufPos < bufLength) {
                char c = charBuf[bufPos];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || char.IsLetter(c))
                    bufPos++;
                else
                    break;
            }
            while (!IsEmptyNoBufferUp()) {
                char c = charBuf[bufPos];
                if (c >= '0' && c <= '9')
                    bufPos++;
                else
                    break;
            }

            return CacheString(charBuf, stringCache, start, bufPos - start);
        }

        internal string ConsumeHexSequence()
        {
            BufferUp();
            int start = bufPos;
            while (bufPos < bufLength) {
                char c = charBuf[bufPos];
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                    bufPos++;
                else
                    break;
            }
            return CacheString(charBuf, stringCache, start, bufPos - start);
        }

        internal string ConsumeDigitSequence()
        {
            BufferUp();
            int start = bufPos;
            while (bufPos < bufLength) {
                char c = charBuf[bufPos];
                if (c >= '0' && c <= '9')
                    bufPos++;
                else
                    break;
            }
            return CacheString(charBuf, stringCache, start, bufPos - start);
        }

        internal bool Matches(char c)
        {
            return !IsEmpty() && charBuf[bufPos] == c;
        }

        internal bool Matches(string seq)
        {
            BufferUp();
            int scanLength = seq.Length;
            if (scanLength > bufLength - bufPos)
                return false;

            for (int offset = 0; offset < scanLength; offset++)
                if (seq[offset] != charBuf[bufPos +offset])
                    return false;
            return true;
        }

        internal bool MatchesIgnoreCase(string seq)
        {
            BufferUp();
            int scanLength = seq.Length;
            if (scanLength > bufLength - bufPos)
                return false;

            for (int offset = 0; offset < scanLength; offset++) {
                char upScan = char.ToUpper(seq[offset]);
                char upTarget = char.ToUpper(charBuf[bufPos + offset]);
                if (upScan != upTarget)
                    return false;
            }
            return true;
        }

        internal bool MatchesAny(params char[] seq)
        {
            if (IsEmpty())
            {
                return false;
            }
            BufferUp();
            char c = charBuf[bufPos];
            foreach(char seek in seq) {
                if (seek == c)
                    return true;
            }
            return false;
        }
        
        internal bool MatchesAnySorted(char[] seq) {
            BufferUp();
            return !IsEmpty() && Array.BinarySearch(seq, charBuf[bufPos]) >= 0;
        }

        internal bool MatchesLetter()
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = charBuf[bufPos];
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || char.IsLetter(c);
        }
        
        /// <summary>
        /// Checks if the current pos matches an ascii alpha (A-Z a-z) per https://infra.spec.whatwg.org/#ascii-alpha
        /// </summary>
        /// <returns> if it matches or not</returns>
        bool MatchesAsciiAlpha() {
            if (IsEmpty())
                return false;
            char c = charBuf[bufPos];
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        internal bool MatchesDigit()
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = charBuf[bufPos];
            return (c >= '0' && c <= '9');
        }

        internal bool MatchConsume(string seq)
        {
            BufferUp();
            if (Matches(seq)) {
                bufPos += seq.Length;
                return true;
            } else {
                return false;
            }
        }

        internal bool MatchConsumeIgnoreCase(string seq)
        {
            if (MatchesIgnoreCase(seq))
            {
                bufPos += seq.Length;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        // we maintain a cache of the previously scanned sequence, and return that if applicable on repeated scans.
        // that improves the situation where there is a sequence of <p<p<p<p<p<p<p...</title> and we're bashing on the <p
        // looking for the </title>. Resets in bufferUp()
        private string lastIcSeq; // scan cache
        private int lastIcIndex; // nearest found indexOf

        internal bool ContainsIgnoreCase(string seq)
        {
            if (seq.Equals(lastIcSeq)) {
                if (lastIcIndex == -1) return false;
                if (lastIcIndex >= bufPos) return true;
            }
            lastIcSeq = seq;

            String loScan = seq.ToLower();
            int lo = NextIndexOf(loScan);
            if (lo > -1) {
                lastIcIndex = bufPos + lo; return true;
            }

            String hiScan = seq.ToUpper();
            int hi = NextIndexOf(hiScan);
            bool found = hi > -1;
            lastIcIndex = found ? bufPos + hi : -1; // we don't care about finding the nearest, just that buf contains
            return found;
        }

        public override string ToString()
        {
            if (bufLength - bufPos < 0)
                return "";
            return new string(charBuf, bufPos, bufLength - bufPos);
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
        
        // just used for testing
        internal bool RangeEquals(int start, int count, String cached) {
            return RangeEquals(charBuf, start, count, cached);
        }
    }
    
    
}
