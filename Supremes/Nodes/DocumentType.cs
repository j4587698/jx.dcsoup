using System.Text;
using Supremes.Helper;

namespace Supremes.Nodes
{
    /// <summary>
    /// A
    /// <c>&lt;!DOCTYPE&gt;</c>
    /// node.
    /// </summary>
    public sealed class DocumentType : LeafNode
    {
        
        public const string PublicKey = "PUBLIC";
        public const string SystemKey = "SYSTEM";
        private const string NameKey = "name";
        private const string PubSysKey = "pubSysKey"; // PUBLIC or SYSTEM
        private const string PublicIdKey = "publicId";
        private const string SystemIdKey = "systemId";
        
        /// <summary>
        /// Create a new doctype element.
        /// </summary>
        /// <param name="name">the doctype's name</param>
        /// <param name="publicId">the doctype's public ID</param>
        /// <param name="systemId">the doctype's system ID</param>
        internal DocumentType(string name, string publicId, string systemId)
        {
            Validate.NotNull(name);
            Validate.NotNull(publicId);
            Validate.NotNull(systemId);
            
            Attr(NameKey, name);
            Attr(PublicIdKey, publicId);
            Attr(SystemIdKey, systemId);
            
            UpdatePubSysKey();
        }
        
        public void SetPubSysKey(string value) {
            if (value != null)
                Attr(PubSysKey, value);
        }

        private void UpdatePubSysKey() {
            if (Has(PublicIdKey)) {
                Attr(PubSysKey, PublicKey);
            } else if (Has(SystemIdKey))
                Attr(PubSysKey, SystemKey);
        }

        /// <summary>
        /// Get this doctype's name (when set, or empty string)
        /// </summary>
        public string Name => Attr(NameKey);

        /// <summary>
        /// Get this doctype's Public ID (when set, or empty string)
        /// </summary>
        public string PublicId => Attr(PublicIdKey);

        /// <summary>
        /// Get this doctype's System ID (when set, or empty string)
        /// </summary>
        public string SystemId => Attr(SystemIdKey);

        public override string NodeName => "#doctype";

        internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            // add a newline if the doctype has a preceding node (which must be a comment)
            if (SiblingIndex > 0 && @out.PrettyPrint)
            accum.Append('\n');

            if (@out.Syntax == DocumentSyntax.Html && !Has(PublicIdKey) && !Has(SystemIdKey)) {
                // looks like a html5 doctype, go lowercase for aesthetics
                accum.Append("<!doctype");
            } else {
                accum.Append("<!DOCTYPE");
            }
            if (Has(NameKey))
                accum.Append(" ").Append(Attr(NameKey));
            if (Has(PubSysKey))
                accum.Append(" ").Append(Attr(PubSysKey));
            if (Has(PublicIdKey))
                accum.Append(" \"").Append(Attr(PublicIdKey)).Append('"');
            if (Has(SystemIdKey))
                accum.Append(" \"").Append(Attr(SystemIdKey)).Append('"');
            accum.Append('>');
        }

        internal override void AppendOuterHtmlTailTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
        }
        
        private bool Has(string attribute) {
            return !StringUtil.IsBlank(Attr(attribute));
        }
    }
}
