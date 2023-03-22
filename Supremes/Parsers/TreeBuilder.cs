﻿using System.Collections.Generic;
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

        internal List<Element> stack;

        internal string baseUri;

        internal Token currentToken;

        internal ParseErrorList errors;
        
        internal ParseSettings settings;

        internal Dictionary<string, Token.Tag> seenTags;

        private Token.StartTag start = new Token.StartTag();
        
        private Token.EndTag end = new Token.EndTag();
        
        internal abstract ParseSettings DefaultSettings { get; }
        
        private bool trackSourceRange;  // optionally tracks the source range of nodes
        
        // current doc we are building into
        // the stack of open elements
        // current base uri, for creating new elements
        // currentToken is used only for error tracking.
        // null when not tracking errors
        internal virtual void InitialiseParse(string input, string baseUri, Parser parser)
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
            seenTags = new Dictionary<string, Token.Tag>();
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

        internal abstract IReadOnlyList<Node> ParseFragment(string inputFragment, Element context, string baseUri, Parser parser);
        
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
