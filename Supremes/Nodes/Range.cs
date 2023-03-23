using Supremes.Helper;

namespace Supremes.Nodes;

/// <summary>
/// A Range object tracks the character positions in the original input source where a Node starts or ends. If you want to
/// track these positions, tracking must be enabled in the Parser with
/// Parser.SetTrackPosition(bool).
/// </summary>
public class Range
{
    private readonly Position _start, _end;

    private static readonly string RangeKey = Attributes.InternalKey("jsoup.sourceRange");
    private static readonly string EndRangeKey = Attributes.InternalKey("jsoup.endSourceRange");
    private static readonly Position UntrackedPos = new Position(-1, -1, -1);
    private static readonly Range Untracked = new Range(UntrackedPos, UntrackedPos);

    /// <summary>
    /// Creates a new Range with start and end Positions. Called by TreeBuilder when position tracking is on.
    /// </summary>
    /// <param name="start">the start position</param>
    /// <param name="end">the end position</param>
    public Range(Position start, Position end)
    {
        _start = start;
        _end = end;
    }

    /// <summary>
    /// Get the start position of this node.
    /// </summary>
    public Position Start => _start;

    /// <summary>
    /// Get the end position of this node.
    /// </summary>
    public Position End => _end;

    /// <summary>
    /// Test if this source range was tracked during parsing.
    /// </summary>
    public bool IsTracked => this != Untracked;

    /// <summary>
    /// Retrieves the source range for a given Node.
    /// </summary>
    /// <param name="node">the node to retrieve the position for</param>
    /// <param name="start">if this is the starting range. {@code false} for Element end tags.</param>
    /// <returns>the Range, or the Untracked (-1) position if tracking is disabled.</returns>
    public static Range Of(Node node, bool start)
    {
        string key = start ? RangeKey : EndRangeKey;
        if (!node.HasAttr(key))
            return Untracked;
        else
            return (Range)Validate.EnsureNotNull(node.Attributes.GetUserData(key));
    }

    // Internal jsoup method, called by the TreeBuilder. Tracks a Range for a Node.
    // node: the node to associate this position to
    //start: if this is the starting range. false for Element end tags.
    public void Track(Node node, bool start)
    {
        node.Attributes.PutUserData(start ? RangeKey : EndRangeKey, this);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj == null || GetType() != obj.GetType()) return false;

        Range range = (Range)obj;

        if (!_start.Equals(range._start)) return false;
        return _end.Equals(range._end);
    }

    public override int GetHashCode()
    {
        int result = _start.GetHashCode();
        result = 31 * result + _end.GetHashCode();
        return result;
    }

    // Gets a String presentation of this Range, in the format line,column:pos-line,column:pos.
    // return: a string
    public override string ToString()
    {
        return $"{_start}-{_end}";
    }

    // A Position object tracks the character position in the original input source where a Node starts or ends. If you want to
    // track these positions, tracking must be enabled in the Parser with
    // Parser.SetTrackPosition(bool).
    // See Node.SourceRange()
    public class Position
    {
        private readonly int _pos, _lineNumber, _columnNumber;

        // Create a new Position object. Called by the TreeBuilder if source position tracking is on.
        // pos: position index
        // lineNumber: line number
        // columnNumber: column number
        public Position(int pos, int lineNumber, int columnNumber)
        {
            _pos = pos;
            _lineNumber = lineNumber;
            _columnNumber = columnNumber;
        }

        // Gets the position index (0-based) of the original input source that this Position was read at. This tracks the
        // total number of characters read into the source at this position, regardless of the number of preceeding lines.
        // return: the position, or -1 if untracked.
        public int Pos()
        {
            return _pos;
        }

        // Gets the line number (1-based) of the original input source that this Position was read at.
        // return: the line number, or -1 if untracked.
        public int LineNumber()
        {
            return _lineNumber;
        }

        // Gets the cursor number (1-based) of the original input source that this Position was read at. The cursor number
        // resets to 1 on every new line.
        // return: the cursor number, or -1 if untracked.
        public int ColumnNumber()
        {
            return _columnNumber;
        }

        // Test if this position was tracked during parsing.
        // return: true if this was tracked during parsing, false otherwise (and all fields will be -1).
        public bool IsTracked()
        {
            return this != UntrackedPos;
        }

        // Gets a String presentation of this Position, in the format line,column:pos.
        // return: a string
        public override string ToString()
        {
            return $"{_lineNumber},{_columnNumber}:{_pos}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj == null || GetType() != obj.GetType()) return false;
            Position position = (Position)obj;
            if (_pos != position._pos) return false;
            if (_lineNumber != position._lineNumber) return false;
            return _columnNumber == position._columnNumber;
        }

        public override int GetHashCode()
        {
            int result = _pos;
            result = 31 * result + _lineNumber;
            result = 31 * result + _columnNumber;
            return result;
        }
    }
}