using Supremes.Helper;
using System.Text;

namespace Supremes.Nodes
{
    /// <summary>
    /// A data node, for contents of style, script tags etc, where contents should not show in text().
    /// </summary>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public sealed class DataNode : LeafNode
    {
        /// <summary>
        /// Create a new DataNode.
        /// </summary>
        /// <param name="data">data contents</param>
        public DataNode(string data)
        {
            value = data;
        }

        /// <summary>
        /// 
        /// </summary>
        public override string NodeName => "#data";

        /// <summary>
        /// Get or Set the data contents of this node.
        /// </summary>
        /// <remarks>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </remarks>
        /// <value>unencoded data</value>
        /// <returns>data will be unescaped and with original new lines, space etc.</returns>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string WholeData
        {
            set => CoreValue = value;
            get => CoreValue;
        }

        internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            accum.Append(WholeData);
            // data is not escaped in return from data nodes, so " in script, style is plain
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
