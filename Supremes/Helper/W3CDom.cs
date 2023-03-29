using System.Xml;

namespace Supremes.Helper;

/// <summary>
/// Helper class to transform a {@link org.jsoup.nodes.Document} to a {@link org.w3c.dom.Document org.w3c.dom.Document},
/// for integration with toolsets that use the W3C DOM.
/// </summary>
public class W3CDom
{
    protected XmlDocument factory;
    
    private bool namespaceAware = true;

    public W3CDom()
    {
        factory = new XmlDocument();
        factory.names
    }
}