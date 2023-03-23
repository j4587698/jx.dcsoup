namespace Supremes.Nodes;

public class CDataNode : TextNode
{
    public CDataNode(string text) : base(text)
    {
    }

    public override string NodeName => "#cdata";

    public override string Text => WholeText;
}