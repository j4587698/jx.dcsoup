using Supremes.Helper;
using System.Text;
using System.Text.RegularExpressions;

namespace Supremes.Nodes
{
    /// <summary>
    /// A text node.
    /// </summary>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public class TextNode : LeafNode
    {

        /// <summary>
        /// Create a new TextNode representing the supplied (unencoded) text).
        /// </summary>
        /// <param name="text">raw text</param>
        /// <seealso cref="CreateFromEncoded(string, string)">CreateFromEncoded(string, string)
        /// </seealso>
        internal TextNode(string text)
        {
            this.value = text;
        }

        public override string NodeName => "#text";

        /// <summary>
        /// Get or Set text content of this text node.
        /// </summary>
        /// <remarks>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>unencoded text</value>
        /// <returns>Unencoded, normalised text.</returns>
        /// <seealso cref="TextNode.WholeText">TextNode.WholeText</seealso>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public virtual string Text
        {
            get => StringUtil.NormaliseWhitespace(WholeText);
            set => CoreValue = value;
        }

        /// <summary>
        /// Get the (unencoded) text of this text node, including any newlines and spaces present in the original.
        /// </summary>
        /// <returns>text</returns>
        public string WholeText => CoreValue;

        /// <summary>
        /// Test if this text node is blank -- that is, empty or only whitespace (including newlines).
        /// </summary>
        /// <returns>true if this document is empty or only whitespace, false if it contains any text content.
        /// </returns>
        public bool IsBlank => StringUtil.IsBlank(WholeText);

        /// <summary>
        /// Split this text node into two nodes at the specified string offset.
        /// </summary>
        /// <remarks>
        /// After splitting, this node will contain the
        /// original text up to the offset, and will have a new text node sibling containing the text after the offset.
        /// </remarks>
        /// <param name="offset">string offset point to split node at.</param>
        /// <returns>the newly created text node containing the text after the offset.</returns>
        public TextNode SplitText(int offset)
        {
            var text = CoreValue;
            Validate.IsTrue(offset >= 0, "Split offset must be not be negative");
            Validate.IsTrue(offset < text.Length, "Split offset must not be greater than current text length");
            string head = WholeText.Substring(0, offset); /*substring*/
            string tail = WholeText.Substring(offset); /*substring*/
            Text = head;
            TextNode tailNode = new TextNode(tail);
            Parent?.AddChildren(SiblingIndex + 1, tailNode);
            return tailNode;
        }

        internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings outputSettings)
        {
            bool prettyPrint = outputSettings.PrettyPrint;
            Element parent = parentNode is Element element ? element : null;
            bool normaliseWhite = prettyPrint && !Element.PreserveWhitespace(parentNode);
            bool trimLikeBlock = parent != null && (parent.Tag.IsBlock || parent.Tag.FormatAsBlock);
            bool trimLeading = false, trimTrailing = false;

            if (normaliseWhite)
            {
                trimLeading = (trimLikeBlock && siblingIndex == 0) || parentNode is Document;
                trimTrailing = trimLikeBlock && NextSibling == null;

                Node next = NextSibling;
                Node prev = PreviousSibling;
                bool isBlank = IsBlank;
                bool couldSkip = (next is Element element1 && element1.ShouldIndent(outputSettings))
                                 || (next is TextNode node && (node.IsBlank)) 
                                 || (prev is Element prev1 && (prev1.IsBlock || prev.IsNode("br")));
                if (couldSkip && isBlank) return;

                if ((siblingIndex == 0 && parent != null && parent.Tag.FormatAsBlock && !isBlank)
                    || (outputSettings.Outline && SiblingNodes.Count > 0 && !isBlank) 
                    || (siblingIndex > 0 && IsNode(prev, "br")))
                    Indent(accum, depth, outputSettings);
            }

            Entities.Escape(accum, CoreValue, outputSettings, false, normaliseWhite, trimLeading, trimTrailing);
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
        /// Create a new TextNode from HTML encoded (aka escaped) data.
        /// </summary>
        /// <param name="encodedText">Text containing encoded HTML (e.g. &amp;lt;)</param>
        /// <returns>TextNode containing unencoded data (e.g. &lt;)</returns>
        public static TextNode CreateFromEncoded(string encodedText)
        {
            string text = Entities.Unescape(encodedText);
            return new TextNode(text);
        }

        internal static string NormaliseWhitespace(string text)
        {
            text = StringUtil.NormaliseWhitespace(text);
            return text;
        }

        internal static string StripLeadingWhitespace(string text)
        {
            return Regex.Replace(text, "^\\s+", string.Empty); //text.ReplaceFirst("^\\s+", string.Empty);
        }

        internal static bool LastCharIsWhitespace(StringBuilder sb)
        {
            return sb.Length != 0 && sb[sb.Length - 1] == ' ';
        }

    }
}
