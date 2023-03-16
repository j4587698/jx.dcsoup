using System.Collections.Generic;
using Supremes.Helper;
using Supremes.Nodes;

namespace Supremes.Parsers
{
    /// <author>Jonathan Hedley</author>
    internal abstract class TreeBuilder
    {
        internal Parser parser;
        
        internal CharacterReader reader;

        internal Tokeniser tokeniser;

        internal Document doc;

        internal DescendableLinkedList<Element> stack;

        internal string baseUri;

        internal Token currentToken;

        internal ParseErrorList errors;

        internal abstract IReadOnlyList<Node> ParseFragment(string inputFragment, Element context, string baseUri, Parser parser);

        // current doc we are building into
        // the stack of open elements
        // current base uri, for creating new elements
        // currentToken is used only for error tracking.
        // null when not tracking errors
        internal virtual void InitialiseParse(string input, string baseUri, Parser parser)
        {
            Validate.NotNull(input, "String input must not be null");
            Validate.NotNull(baseUri, "BaseURI must not be null");
            doc = new Document(baseUri);
            reader = new CharacterReader(input);
            this.parser = parser;
            tokeniser = new Tokeniser(reader, errors);
            stack = new DescendableLinkedList<Element>();
            this.baseUri = baseUri;
        }

        internal Document Parse(string input, string baseUri)
        {
            return Parse(input, baseUri, ParseErrorList.NoTracking());
        }

        internal virtual Document Parse(string input, string baseUri, ParseErrorList errors)
        {
            InitialiseParse(input, baseUri, errors);
            RunParser();
            return doc;
        }

        internal void RunParser()
        {
            while (true)
            {
                Token token = tokeniser.Read();
                Process(token);
                if (token.type == TokenType.EOF)
                {
                    break;
                }
            }
        }

        internal abstract bool Process(Token token);

        internal Element CurrentElement()
        {
            return stack.Last.Value;
        }
    }
}
