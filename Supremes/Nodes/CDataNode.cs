using System.Text;

namespace Supremes.Nodes;

public class CDataNode : TextNode
{
    public CDataNode(string text) : base(text)
    {
    }

    public override string NodeName => "#cdata";

    public override string Text => WholeText;

    internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings outputSettings)
    {
        accum.Append("<![CDATA[").Append(WholeText);
    }

    internal override void AppendOuterHtmlTailTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
    {
        accum.Append("]]>");
    }
}