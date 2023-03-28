using System.Text;
using Supremes.Parsers;

namespace Supremes.Nodes
{
    /// <summary>
    /// A comment node.
    /// </summary>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public sealed class Comment : LeafNode
    {
        /// <summary>
        /// Create a new comment node.
        /// </summary>
        /// <param name="data">The contents of the comment</param>
        /// <param name="baseUri">base URI</param>
        internal Comment(string data)
        {
            value = data;
        }

        public override string NodeName { get; } = "#comment";

        /// <summary>
        /// Get the contents of the comment.
        /// </summary>
        /// <returns>comment content</returns>
        public string Data
        {
            get => CoreValue;
            set => CoreValue = value;
        }

        internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            if (@out.PrettyPrint && ((IsEffectivelyFirst() && ParentNode is Element && ((Element) ParentNode).Tag.FormatAsBlock) || (@out.Outline)))
                Indent(accum, depth, @out);
            accum
                .Append("<!--")
                .Append(Data)
                .Append("-->");
        }

        internal override void AppendOuterHtmlTailTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
        }

        /// <summary>
        /// Converts the value of this instance to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return OuterHtml;
        }
        
        /// <summary>
        /// Check if this comment looks like an XML Declaration.
        /// </summary>
        /// <returns>true if it looks like, maybe, it's an XML Declaration.</returns>
        public bool IsXmlDeclaration()
        {
            string data = Data;
            return IsXmlDeclarationData(data);
        }

        private static bool IsXmlDeclarationData(string data)
        {
            return (data.Length > 1 && (data.StartsWith("!") || data.StartsWith("?")));
        }
        
        /// <summary>
        ///  Attempt to cast this comment to an XML Declaration node.
        /// </summary>
        /// <returns>an XML declaration if it could be parsed as one, null otherwise.</returns>
        public XmlDeclaration AsXmlDeclaration()
        {
            string data = Data;

            XmlDeclaration decl = null;
            string declContent = data.Substring(1, data.Length - 2);
            // make sure this bogus comment is not immediately followed by another, treat as comment if so
            if (IsXmlDeclarationData(declContent))
                return null;

            string fragment = "<" + declContent + ">";
            // use the HTML parser not XML, so we don't get into a recursive XML Declaration on contrived data
            var parser = Parser.HtmlParser;
            parser.Settings = ParseSettings.PreserveCase;;
            Document doc = parser.ParseInput(fragment, BaseUri);
            if (doc.Body.Children.Count > 0)
            {
                Element el = doc.Body.Child(0);
                decl = new XmlDeclaration(NodeUtils.Parser(doc).Settings.NormalizeTag(el.TagName), data.StartsWith("!"));
                decl.Attributes.AddAll(el.Attributes);
            }
            return decl;
        }
    }
}
