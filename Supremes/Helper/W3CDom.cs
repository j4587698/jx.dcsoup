using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Supremes.Nodes;
using Supremes.Select;

namespace Supremes.Helper;

/// <summary>
/// Helper class to transform a {@link org.jsoup.nodes.Document} to a {@link org.w3c.dom.Document org.w3c.dom.Document},
/// for integration with toolsets that use the W3C DOM.
/// </summary>
public class W3CDom
{

    /// <summary>
    /// Get / Set the namespace aware setting. This impacts the factory that is used to create W3C nodes from jsoup nodes.
    /// </summary>
    public bool NamespaceAware { get; set; } = true;

    /// <summary>
    /// Convert a jsoup DOM to a W3C Document. The created nodes will link back to the original
    /// jsoup nodes in the user property {@link #SourceProperty} (but after conversion, changes on one side will not
    /// flow to the other). The input Element is used as a context node, but the whole surrounding jsoup Document is
    /// converted. (If you just want a subtree converted, use {@link #convert(org.jsoup.nodes.Element, Document)}.)
    /// </summary>
    /// <param name="input">jsoup element or doc</param>
    /// <returns>a W3C DOM Document representing the jsoup Document or Element contents.</returns>
    public XDocument FromJsoup(Element input)
    {
        Validate.NotNull(input);

        var outDoc = new XDocument();
        var context = (input is Document) ? input.Child(0) : input;
        outDoc.Add(new XElement(context.TagName, context.Attributes.Select(a => new XAttribute(a.Key, a.Value))));

        Document inDoc = input.OwnerDocument;
        DocumentType doctype = inDoc?.DocumentType;
        if (doctype != null)
        {
            var doctypeNode = new XDocumentType(doctype.Name, doctype.PublicId, doctype.SystemId, null);
            outDoc.AddFirst(doctypeNode);
        }

        Convert(inDoc != null ? inDoc : input, outDoc.Root);

        return outDoc;
    }


    protected class W3CBuilder : INodeVisitor
    {
        private const string xmlnsKey = "xmlns";
        private const string xmlnsPrefix = "xmlns:";

        private readonly XDocument doc;
        private bool namespaceAware = true;
        private readonly Stack<Dictionary<string, string>> namespacesStack = new Stack<Dictionary<string, string>>(); // stack of namespaces, prefix => urn
        private XElement dest;
        private DocumentSyntax syntax = DocumentSyntax.Xml; // the syntax (to coerce attributes to). From the input doc if available.
        private readonly Element contextElement;

        public W3CBuilder(XDocument doc)
        {
            this.doc = doc;
            namespacesStack.Push(new Dictionary<string, string>());
            dest = doc.Root;
            contextElement = (Element)doc.Annotation<Element>(""); // Track the context jsoup Element, so we can save the corresponding w3c element
        }
    }
}
