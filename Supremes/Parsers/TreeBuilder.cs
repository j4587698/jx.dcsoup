using System.Collections.Generic;
using Supremes.Helper;
using Supremes.Nodes;

namespace Supremes.Parsers
{
    /// <author>Jonathan Hedley</author>
    public abstract class TreeBuilder
    {
        internal Parser parser;
        
        internal CharacterReader reader;

        internal Tokeniser tokeniser;

        internal Document doc;

        internal List<Element> stack;

        internal string baseUri;

        internal Token currentToken;

        internal ParseSettings settings;

        internal Dictionary<string, Tag> seenTags;

        private Token.StartTag start = new Token.StartTag();
        
        private Token.EndTag end = new Token.EndTag();
        
        internal abstract ParseSettings DefaultSettings { get; }
        
        private bool trackSourceRange;  // optionally tracks the source range of nodes
        
        // current doc we are building into
        // the stack of open elements
        // current base uri, for creating new elements
        // currentToken is used only for error tracking.
        // null when not tracking errors
        internal virtual void InitialiseParse(StringReader input, string baseUri, Parser parser)
        {
            Validate.NotNull(input, "String input must not be null");
            Validate.NotNull(baseUri, "BaseURI must not be null");
            Validate.NotNull(parser);
            
            doc = new Document(baseUri);
            doc.Parser = parser;
            settings = parser.Settings;
            reader = new CharacterReader(input);
            this.parser = parser;
            trackSourceRange = parser.IsTrackPosition;
            reader.TrackNewlines(parser.IsTrackErrors || trackSourceRange);
            tokeniser = new Tokeniser(reader, parser.Errors);
            stack = new List<Element>(32);
            seenTags = new Dictionary<string, Tag>();
            this.baseUri = baseUri;
        }

        internal Document Parse(StringReader input, string baseUri, Parser parser)
        {
            InitialiseParse(input, baseUri, parser);
            RunParser();
            reader.Close();
            reader = null;
            tokeniser = null;
            stack = null;
            seenTags = null;
            return doc;
        }
        
        internal abstract TreeBuilder NewInstance { get; }

        internal abstract IReadOnlyList<Node> ParseFragment(string inputFragment, Element context, string baseUri, Parser parser);
        
        internal void RunParser()
        {
            while (true)
            {
                Token token = tokeniser.Read();
                Process(token);
                token.Reset();
                if (token.type == TokenType.EOF)
                {
                    break;
                }
            }
        }

        internal abstract bool Process(Token token);
        
        internal bool ProcessStartTag(string name) {
            // these are "virtual" start tags (auto-created by the treebuilder), so not tracking the start position
            var start = this.start;
            if (currentToken == start) { // don't recycle an in-use token
                return Process(new Token.StartTag().Name(name));
            }
            return Process(start.Reset().Name(name));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="attrs"></param>
        /// <returns></returns>
        public bool ProcessStartTag(string name, Attributes attrs) {
            Token.StartTag start = this.start;
            if (currentToken == start) { // don't recycle an in-use token
                return Process(new Token.StartTag().NameAttr(name, attrs));
            }
            start.Reset();
            start.NameAttr(name, attrs);
            return Process(start);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool ProcessEndTag(string name)
        {
            if (currentToken == end) // don't recycle an in-use token
            {
                return Process(new Token.EndTag().Name(name));
            }
            return Process(end.Reset().Name(name));
        }

        /// <summary>
        /// Get the current element (last on the stack). If all items have been removed, returns the document instead
        /// (which might not actually be on the stack; use stack.size() == 0 to test if required.
        /// </summary>
        /// <returns>the last element on the stack, if any; or the root document</returns>
        internal Element CurrentElement()
        {
            var count = stack.Count;
            return count > 0 ? stack[stack.Count - 1] : doc;
        }
        
        /// <summary>
        /// Checks if the Current Element's normal name equals the supplied name.
        /// </summary>
        /// <param name="normalName">name to check</param>
        /// <returns>true if there is a current element on the stack, and its name equals the supplied</returns>
        internal bool CurrentElementIs(string normalName) {
            if (stack.Count == 0)
                return false;
            Element current = CurrentElement();
            return current != null && current.NormalName.Equals(normalName);
        }

        /// <summary>
        /// If the parser is tracking errors, add an error at the current position.
        /// </summary>
        /// <param name="msg">error message template</param>
        /// <param name="args">template arguments</param>
        internal void Error(string msg, params object[] args) {
            ParseErrorList errors = parser.Errors;
            if (errors.CanAddError)
                errors.Add(new ParseError(reader, msg, args));
        }
        
        /// <summary>
        /// (An internal method, visible for Element. For HTML parse, signals that script and style text should be treated as
        /// Data Nodes).
        /// </summary>
        /// <param name="normalName"></param>
        /// <returns></returns>
        internal virtual bool IsContentForTagData(string normalName) {
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        internal Tag TagFor(string tagName, ParseSettings settings) {
            if (!seenTags.TryGetValue(tagName, out var tag)) {
                tag = Tag.ValueOf(tagName, settings);
                seenTags.Add(tagName, tag);
            }
            return tag;
        }

        /// <summary>
        /// Called by implementing TreeBuilders when a node has been inserted. This implementation includes optionally tracking
        /// the source range of the node.
        /// </summary>
        /// <param name="node">the node that was just inserted</param>
        /// <param name="token">the (optional) token that created this node</param>
        internal void OnNodeInserted(Node node, Token token) {
            TrackNodePosition(node, token, true);
        }

        /// <summary>
        /// Called by implementing TreeBuilders when a node is explicitly closed. This implementation includes optionally
        /// tracking the closing source range of the node.
        /// </summary>
        /// <param name="node">the node being closed</param>
        /// <param name="token">the end-tag token that closed this node</param>
        internal void OnNodeClosed(Node node, Token token) {
            TrackNodePosition(node, token, false);
        }
        
        private void TrackNodePosition(Node node, Token token, bool start) {
            if (trackSourceRange && token != null) {
                int startPos = token.StartPos;
                if (startPos == Token.Unset) return; // untracked, virtual token

                Range.Position startRange = new Range.Position(startPos, reader.LineNumber(startPos), reader.ColumnNumber(startPos));
                int endPos = token.EndPos;
                Range.Position endRange = new Range.Position(endPos, reader.LineNumber(endPos), reader.ColumnNumber(endPos));
                Range range = new Range(startRange, endRange);
                range.Track(node, start);
            }
        }

    }
}
