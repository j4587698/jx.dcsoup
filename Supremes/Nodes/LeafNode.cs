using System;
using System.Collections.Generic;

namespace Supremes.Nodes;

public abstract class LeafNode : Node
{
    internal Object value; // either a string value, or an attribute map (in the rare case multiple attributes are set)

    protected override bool HasAttributes => value is Attributes;

    public override Attributes Attributes
    {
        get
        {
            EnsureAttributes();
            return (Attributes)value;
        }

    }

    private void EnsureAttributes()
    {
        if (!HasAttributes)
        {
            Object coreValue = value;
            Attributes attributes = new Attributes();
            value = attributes;
            if (coreValue != null)
                attributes.Put(NodeName, (String)coreValue);
        }
    }

    internal string CoreValue
    {
        get => Attr(NodeName);
        set => Attr(NodeName, value);
    }


    public override string Attr(string key)
    {
        if (!HasAttributes)
        {
            return NodeName.Equals(key) ? (string)value : string.Empty;
        }

        return base.Attr(key);
    }

    public override Node Attr(string key, string value)
    {
        if (!HasAttributes && key.Equals(NodeName))
        {
            this.value = value;
        }
        else
        {
            EnsureAttributes();
            base.Attr(key, value);
        }

        return this;
    }

    public override bool HasAttr(string key)
    {
        EnsureAttributes();
        return base.HasAttr(key);
    }

    public override Node RemoveAttr(string key)
    {
        EnsureAttributes();
        return base.RemoveAttr(key);
    }

    public override string AbsUrl(string key)
    {
        EnsureAttributes();
        return base.AbsUrl(key);
    }

    public override string BaseUri => HasParent ? Parent.BaseUri : "";

    protected override void DoSetBaseUri(string baseUri)
    {
        // noop
    }

    public override int ChildNodeSize => 0;

    public override Node Empty()
    {
        return this;
    }

    protected override List<Node> EnsureChildNodes()
    {
        return EmptyNodes;
    }

    protected LeafNode DoClone(Node parent)
    {
        LeafNode clone = (LeafNode)base.DoClone(parent);

        // Object value could be plain string or attributes - need to clone
        if (HasAttributes)
            clone.value = ((Attributes)value).Clone();

        return clone;
    }
}