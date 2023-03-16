using System;
using System.Collections.Generic;
using Supremes.Helper;
using Supremes.Parsers;

namespace Supremes.Nodes;

/// <summary>
/// Internal helpers for Nodes, to keep the actual node APIs relatively clean. A jsoup internal class, so don't use it as
/// there is no contract API).
/// </summary>
public class NodeUtils
{
    /// <summary>
    /// Get the output setting for this node,  or if this node has no document (or parent), retrieve the default output
    /// settings
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static DocumentOutputSettings OutputSettings(Node node)
    {
        Document owner = node.OwnerDocument;
        return owner != null ? owner.OutputSettings : (new Document("")).OutputSettings;
    }

    /// <summary>
    /// Get the parser that was used to make this node, or the default HTML parser if it has no parent.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static Parser Parser(Node node)
    {
        Document doc = node.OwnerDocument;
        return doc is { Parser: { } } ? doc.Parser : new Parser(new HtmlTreeBuilder());
    }

    /// <summary>
    /// This impl works by compiling the input xpath expression, and then evaluating it against a W3C Document converted
    /// from the original jsoup element. The original jsoup elements are then fetched from the w3c doc user data (where we
    /// stashed them during conversion). This process could potentially be optimized by transpiling the compiled xpath
    /// expression to a jsoup Evaluator when there's 1:1 support, thus saving the W3C document conversion stage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="xpath"></param>
    /// <param name="el"></param>
    /// <param name="nodeType"></param>
    /// <returns></returns>
    // public static List<T> SelectXpath<T>(string xpath, Element el, Type nodeType) where T : Node
    // {
    //     Validate.NotEmpty(xpath);
    //     Validate.NotNull(el);
    //     Validate.NotNull(nodeType);
    //
    //     W3CDom w3c = new W3CDom().NamespaceAware(false);
    //     org.w3c.dom.Document wDoc = w3c.FromJsoup(el);
    //     org.w3c.dom.Node contextNode = w3c.ContextNode(wDoc);
    //     NodeList nodeList = w3c.SelectXpath(xpath, contextNode);
    //     return w3c.SourceNodes(nodeList, nodeType);
    // }
}