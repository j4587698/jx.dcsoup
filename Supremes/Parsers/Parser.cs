using Supremes.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using Supremes.Helper;

namespace Supremes.Parsers
{
    /// <summary>
    /// Parses HTML into a
    /// <see cref="Supremes.Nodes.Document">Supremes.Nodes.Document</see>
    /// . Generally best to use one of the  more convenient parse methods
    /// in
    /// <see cref="Supremes.Dcsoup">Supremes.Dcsoup</see>
    /// .
    /// </summary>
    public class Parser
    {
        private ParseErrorList errors;

        /// <summary>
        /// Create a new Parser, using the specified TreeBuilder
        /// </summary>
        /// <param name="treeBuilder">TreeBuilder to use to parse input into Documents.</param>
        public Parser(TreeBuilder treeBuilder)
        {
            // by default, error tracking is disabled.
            this.TreeBuilder = treeBuilder;
            Settings = treeBuilder.DefaultSettings;
            errors = ParseErrorList.NoTracking();
        }
        
        /// <summary>
        /// Creates a new Parser as a deep copy of this; including initializing a new TreeBuilder. Allows independent (multi-threaded) use.
        /// </summary>
        /// <returns>a copied parser</returns>
        public Parser NewInstance =>  new Parser(this);

        private Parser(Parser copy)
        {
            TreeBuilder = copy.TreeBuilder.NewInstance; // because extended
            errors = new ParseErrorList(copy.errors); // only copies size, not contents
            Settings = new ParseSettings(copy.Settings);
            TrackPosition = copy.TrackPosition;
        }

        /// <summary>
        /// Parse HTML into a Document
        /// </summary>
        /// <param name="html"></param>
        /// <param name="baseUri"></param>
        /// <returns></returns>
        public Document ParseInput(string html, string baseUri)
        {
            Document doc = TreeBuilder.Parse(new StringReader(html), baseUri, this);
            return doc;
        }
        
        public List<Node> ParseFragmentInput(String fragment, Element context, String baseUri) {
            return TreeBuilder.ParseFragment(fragment, context, baseUri, this);
        }

        // gets & sets

        /// <summary>
        /// Get the TreeBuilder currently in use.
        /// </summary>
        /// <returns>current TreeBuilder.</returns>
        internal TreeBuilder TreeBuilder { get; set; }


        /// <summary>
        /// Check if parse error tracking is enabled.
        /// </summary>
        /// <returns>current track error state.</returns>
        public bool CanTrackErrors => errors.MaxSize > 0;

        /// <summary>
        /// Enable or disable parse error tracking for the next parse.
        /// </summary>
        /// <param name="maxErrors">
        /// the maximum number of errors to track. Set to 0 to disable.
        /// </param>
        /// <returns>this, for chaining</returns>
        public Parser SetTrackErrors(int maxErrors)
        {
            errors = maxErrors > 0 ? ParseErrorList.Tracking(maxErrors) : ParseErrorList.NoTracking();
            return this;
        }

        /// <summary>
        /// Retrieve the parse errors, if any, from the last parse.
        /// </summary>
        /// <returns>list of parse errors, up to the size of the maximum errors tracked.</returns>
        public ParseErrorList Errors => errors;

        // builders

        /// <summary>
        /// Create a new HTML parser.
        /// </summary>
        /// <remarks>
        /// This parser treats input as HTML5, and enforces the creation of a normalised document,
        /// based on a knowledge of the semantics of the incoming tags.
        /// </remarks>
        /// <returns>a new HTML parser.</returns>
        public static Parser HtmlParser => new Parser(new HtmlTreeBuilder());

        /// <summary>
        /// Create a new XML parser.
        /// </summary>
        /// <remarks>
        /// This parser assumes no knowledge of the incoming tags and does not treat it as HTML,
        /// rather creates a simple tree directly from the input.
        /// </remarks>
        /// <returns>a new simple XML parser.</returns>
        public static Parser XmlParser => new Parser(new XmlTreeBuilder());

        /// <summary>
        /// Check if parse error tracking is enabled.
        /// </summary>
        public bool IsTrackErrors => errors.MaxSize > 0;

        /// <summary>
        /// Enable or disable source position tracking. If enabled, Nodes will have a Position to track where in the original
        /// input source they were created from.
        /// </summary>
        public bool TrackPosition { get; set; } = false;

        /// <summary>
        /// Get / Set the ParseSettings of this Parser, to control the case sensitivity of tags and attributes.
        /// </summary>
        public ParseSettings Settings { get; set; }

        /// <summary>
        /// (An internal method, visible for Element. For HTML parse, signals that script and style text should be treated as
        /// Data Nodes).
        /// </summary>
        /// <param name="normalName"></param>
        /// <returns></returns>
        public bool IsContentForTagData(String normalName) {
            return TreeBuilder.IsContentForTagData(normalName);
        }
        
        // utility methods
        
        /// <summary>
        /// Parse HTML into a Document.
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>parsed Document</returns>
        public static Document Parse(string html, string baseUri)
        {
            TreeBuilder treeBuilder = new HtmlTreeBuilder();
            return treeBuilder.Parse(new StringReader(html), baseUri, new Parser(treeBuilder));
        }

        /// <summary>
        /// Parse a fragment of HTML into a list of nodes.
        /// </summary>
        /// <remarks>
        /// The context element, if supplied, supplies parsing context.
        /// </remarks>
        /// <param name="fragmentHtml">the fragment of HTML to parse</param>
        /// <param name="context">
        /// (optional) the element that this HTML fragment is being parsed for (i.e. for inner HTML). This
        /// provides stack context (for implicit element creation).
        /// </param>
        /// <param name="baseUri">
        /// base URI of document (i.e. original fetch location), for resolving relative URLs.
        /// </param>
        /// <returns>
        /// list of nodes parsed from the input HTML. Note that the context element, if supplied, is not modified.
        /// </returns>
        public static List<Node> ParseFragment(string fragmentHtml, Element context, string baseUri)
        {
            HtmlTreeBuilder treeBuilder = new HtmlTreeBuilder();
            return treeBuilder.ParseFragment(fragmentHtml, context, baseUri, new Parser(treeBuilder));
        }
        
        /// <summary>
        /// Parse a fragment of HTML into a list of nodes. The context element, if supplied, supplies parsing context.
        /// </summary>
        /// <param name="fragmentHtml">the fragment of HTML to parse</param>
        /// <param name="context">(optional) the element that this HTML fragment is being parsed for (i.e. for inner HTML). This provides stack context (for implicit element creation).</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <param name="errorList">list to add errors to</param>
        /// <returns>list of nodes parsed from the input HTML. Note that the context element, if supplied, is not modified.</returns>
        public static IReadOnlyList<Node> ParseFragment(string fragmentHtml, Element context, string baseUri, ParseErrorList errorList)
        {
            HtmlTreeBuilder treeBuilder = new HtmlTreeBuilder();
            Parser parser = new Parser(treeBuilder);
            parser.errors = errorList;
            return treeBuilder.ParseFragment(fragmentHtml, context, baseUri, parser);
        }

        /// <summary>
        /// Parse a fragment of XML into a list of nodes.
        /// </summary>
        /// <param name="fragmentXml">the fragment of XML to parse</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>list of nodes parsed from the input XML.</returns>
        public static IReadOnlyList<Node> ParseXmlFragment(string fragmentXml, string baseUri)
        {
            XmlTreeBuilder treeBuilder = new XmlTreeBuilder();
            return treeBuilder.ParseFragment(fragmentXml, baseUri, new Parser(treeBuilder));
        }

        /// <summary>
        /// Parse a fragment of HTML into the
        /// <c>body</c>
        /// of a Document.
        /// </summary>
        /// <param name="bodyHtml">fragment of HTML</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>Document, with empty head, and HTML parsed into body</returns>
        public static Document ParseBodyFragment(string bodyHtml, string baseUri)
        {
            Document doc = Document.CreateShell(baseUri);
            Element body = doc.Body;
            List<Node> nodeList = ParseFragment(bodyHtml, body, baseUri);
            Node[] nodes = nodeList.ToArray();
            // the node list gets modified when re-parented
            for (int i = nodes.Length - 1; i > 0; i--) {
                nodes[i].Remove();
            }
            foreach (Node node in nodes)
            {
                body.AppendChild(node);
            }
            return doc;
        }

        /// <summary>
        /// Utility method to unescape HTML entities from a string
        /// </summary>
        /// <param name="string">HTML escaped string</param>
        /// <param name="inAttribute">if the string is to be escaped in strict mode (as attributes are)</param>
        /// <returns>an unescaped string</returns>
        public static string UnescapeEntities(string @string, bool inAttribute)
        {
            Tokeniser tokeniser = new Tokeniser(new CharacterReader(@string), ParseErrorList.NoTracking());
            return tokeniser.UnescapeEntities(inAttribute);
        }
    }
}
