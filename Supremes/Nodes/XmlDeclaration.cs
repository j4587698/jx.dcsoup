using System.Text;
using Supremes.Helper;

namespace Supremes.Nodes
{
    /// <summary>
    /// An XML Declaration.
    /// </summary>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public class XmlDeclaration : LeafNode
    {
        private readonly bool isProcessingInstruction;

        /// <summary>
        /// Create a new XML declaration
        /// </summary>
        /// <param name="name">data</param>
        /// <param name="isProcessingInstruction">is processing instruction</param>
        public XmlDeclaration(string name, bool isProcessingInstruction)
        {
            Validate.NotNull(name);
            // <! if true, <? if false, declaration (and last data char should be ?)
            value = name;
            this.isProcessingInstruction = isProcessingInstruction;
        }

        public override string NodeName => "#declaration";
        
        public string Name => CoreValue;

        /// <summary>
        /// Get the unencoded XML declaration.
        /// </summary>
        /// <returns>XML declaration</returns>
        public string WholeDeclaration
        {
            get
            {
                var sb = StringUtil.BorrowBuilder();
                GetWholeDeclaration(sb, new DocumentOutputSettings());
                return StringUtil.ReleaseBuilder(sb);
            }
        }

        private void GetWholeDeclaration(StringBuilder accum, DocumentOutputSettings @out)
        {
            foreach (Attribute attribute in Attributes)
            {
                string key = attribute.Key;
                string val = attribute.Value;
                if (!key.Equals(NodeName))
                {
                    // skips coreValue (name)
                    accum.Append(' ');
                    // basically like Attribute, but skip empty vals in XML
                    accum.Append(key);
                    if (!string.IsNullOrEmpty(val))
                    {
                        accum.Append("=\"");
                        Entities.Escape(accum, val, @out, true, false, false, false);
                        accum.Append('"');
                    }
                }
            }
        }

        internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            accum.Append("<").Append(isProcessingInstruction ? "!" : "?").Append(CoreValue);
            GetWholeDeclaration(accum, @out);
            accum.Append(isProcessingInstruction ? "!" : "?").Append(">");
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
    }
}
