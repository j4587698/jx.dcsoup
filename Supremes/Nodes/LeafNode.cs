using System;
using System.Collections.Generic;

namespace Supremes.Nodes;

abstract class LeafNode : Node
{
    Object value; // either a string value, or an attribute map (in the rare case multiple attributes are set)

        protected bool HasAttributes()
        {
            return value is Attributes;
        }

        public override Attributes Attributes()
        {
            EnsureAttributes();
            return (Attributes)value;
        }

        private void EnsureAttributes()
        {
            if (!HasAttributes())
            {
                Object coreValue = value;
                Attributes attributes = new Attributes();
                value = attributes;
                if (coreValue != null)
                    attributes.Put(NodeName, (String)coreValue);
            }
        }

        String CoreValue()
        {
            return Attr(NodeName);
        }

        void CoreValue(String value)
        {
            Attr(NodeName, value);
        }

        public override String Attr(String key)
        {
            if (!HasAttributes())
            {
                return NodeName.Equals(key) ? (String)value : String.Empty;
            }
            return base.Attr(key);
        }

        public override Node Attr(String key, String value)
        {
            if (!HasAttributes() && key.Equals(NodeName))
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

        public override bool HasAttr(String key)
        {
            EnsureAttributes();
            return base.HasAttr(key);
        }

        public override Node RemoveAttr(String key)
        {
            EnsureAttributes();
            return base.RemoveAttr(key);
        }

        public override String AbsUrl(String key)
        {
            EnsureAttributes();
            return base.AbsUrl(key);
        }

        public override String BaseUri()
        {
            return HasParent() ? parent().baseUri() : "";
        }

        protected override void DoSetBaseUri(String baseUri)
        {
            // noop
        }

        public override int ChildNodeSize()
        {
            return 0;
        }

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
            if (HasAttributes())
                clone.value = ((Attributes)value).Clone();

            return clone;
        }
    }
}