using System;
using System.IO;

namespace Supremes.Helper;

/// <summary>
/// A character stream whose source is a string
/// </summary>
public class StringReader
{
    public object lockObj;

    private string str;
    private int length;
    private int next = 0;
    private int mark = 0;

    /// <summary>
    /// Creates a new string reader.
    /// </summary>
    /// <param name="s"></param>
    public StringReader(string s)
    {
        this.str = s;
        this.length = s.Length;
        lockObj = this;
    }

    private void EnsureOpen()
    {
        if (str == null)
            throw new IOException("Stream closed");
    }

    /// <summary>
    /// Reads a single character.
    /// </summary>
    /// <returns>The character read, or -1 if the end of the stream has been reached</returns>
    public int Read()
    {
        lock (lockObj)
        {
            EnsureOpen();
            if (next >= length)
                return -1;
            return str[next++];
        }
    }
    
    /// <summary>
    /// Reads characters into a portion of an array.
    /// </summary>
    /// <param name="cbuf">Destination buffer</param>
    /// <param name="off">Offset at which to start writing characters</param>
    /// <param name="len">Maximum number of characters to read</param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public int Read(char[] cbuf, int off, int len)
    {
        lock (lockObj)
        {
            EnsureOpen();
            if ((off < 0) || (off > cbuf.Length) || (len < 0) ||
                ((off + len) > cbuf.Length) || ((off + len) < 0))
            {
                throw new IndexOutOfRangeException();
            }
            else if (len == 0)
            {
                return 0;
            }
            if (next >= length)
                return -1;
            int n = Math.Min(length - next, len);
            str.CopyTo(next, cbuf, off, n);
            next += n;
            return n;
        }
    }
    
    /// <summary>
    /// Skips the specified number of characters in the stream. Returns the number of characters that were skipped.
    /// The ns parameter may be negative, even though the skip method of the Reader superclass throws an exception in this case. Negative values of ns cause the stream to skip backwards. Negative return values indicate a skip backwards. It is not possible to skip backwards past the beginning of the string.
    /// </summary>
    /// <param name="ns">If the entire string has been read or skipped, then this method has no effect and always returns 0.</param>
    /// <returns></returns>
    public long Skip(long ns)
    {
        lock (lockObj)
        {
            EnsureOpen();
            if (next >= length)
                return 0;
            int n = (int)Math.Min(length - next, ns);
            n = Math.Max(-next, n);
            next += n;
            return n;
        }
    }
    
    /// <summary>
    /// Tells whether this stream is ready to be read.
    /// </summary>
    /// <returns>True if the next read() is guaranteed not to block for input</returns>
    public bool Ready()
    {
        lock (lockObj)
        {
            EnsureOpen();
            return true;
        }
    }
    
    /// <summary>
    /// Tells whether this stream supports the mark() operation, which it does.
    /// </summary>
    public bool MarkSupported => true;
    
    /// <summary>
    /// Marks the present position in the stream. Subsequent calls to reset() will reposition the stream to this point.
    /// </summary>
    /// <param name="readAheadLimit">Limit on the number of characters that may be read while still preserving the mark. Because the stream's input comes from a string, there is no actual limit, so this argument must not be negative, but is otherwise ignored.</param>
    /// <exception cref="ArgumentException"></exception>
    public void Mark(int readAheadLimit) {
        if (readAheadLimit < 0){
            throw new ArgumentException("Read-ahead limit < 0");
        }
        lock (lockObj) {
            EnsureOpen();
            mark = next;
        }
    }
    
    /// <summary>
    /// Resets the stream to the most recent mark, or to the beginning of the string if it has never been marked.
    /// </summary>
    public void Reset() {
        lock (lockObj) {
            EnsureOpen();
            next = mark;
        }
    }

    /// <summary>
    /// Closes the stream and releases any system resources associated with it. Once the stream has been closed, further read(), ready(), mark(), or reset() invocations will throw an IOException. Closing a previously closed stream has no effect.
    /// </summary>
    public void Close()
    {
        str = null;
    }
}
