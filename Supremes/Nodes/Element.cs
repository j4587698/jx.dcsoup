using Supremes.Helper;
using Supremes.Parsers;
using Supremes.Select;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Supremes.Internal;

namespace Supremes.Nodes
{
    /// <summary>
    /// A HTML element consists of a tag name, attributes, and child nodes
    /// (including text nodes and other elements).
    /// </summary>
    /// <remarks>
    /// From an Element, you can extract data, traverse the node graph, and manipulate the HTML.
    /// </remarks>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public class Element : Node
    {
        private static readonly List<Element> EmptyChildren = new List<Element>();
        private static readonly Regex ClassSplit = new Regex("\\s+");
        private static readonly string BaseUriKey = Attributes.InternalKey("baseUri");
        private Tag tag;
        private WeakReference<List<Element>> shadowChildrenRef; // points to child elements shadowed from node children
        internal List<Node> childNodes;
        internal Attributes attributes; // field is nullable but all methods for attributes are non-null

        /// <summary>
        /// Create a new, standalone element.
        /// </summary>
        /// <param name="tag"></param>
        public Element(string tag): this(Tag.ValueOf(tag), string.Empty, null)
        {
        }
        
        /// <summary>
        /// Create a new, standalone Element.
        /// </summary>
        /// <remarks>
        /// (Standalone in that is has no parent.)
        /// </remarks>
        /// <param name="tag">tag of this element</param>
        /// <param name="baseUri">the base URI</param>
        /// <param name="attributes">initial attributes</param>
        /// <seealso cref="AppendChild(Node)">AppendChild(Node)</seealso>
        /// <seealso cref="AppendElement(string)">AppendElement(string)</seealso>
        internal Element(Tag tag, string baseUri, Attributes attributes = null)
        {
            Validate.NotNull(tag);
            childNodes = EmptyNodes;
            this.attributes = attributes;
            this.tag = tag;
            if (baseUri != null)
            {
                this.SetBaseUri(baseUri);
            }
        }

        /// <summary>
        /// Internal test to check if a nodelist object has been created.
        /// </summary>
        protected bool HasChildNodes => !Equals(childNodes, EmptyNodes);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override List<Node> EnsureChildNodes()
        {
            if (Equals(childNodes, EmptyNodes))
            {
                childNodes = new NodeList(this, 4);
            }

            return childNodes;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool HasAttributes => attributes != null;

        /// <summary>
        /// 
        /// </summary>
        public override Attributes Attributes
        {
            get
            {
                attributes ??= new Attributes();
                return attributes;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string BaseUri => SearchUpForAttribute(this, BaseUriKey);
        
        private static string SearchUpForAttribute(Element start, string key) {
            Element el = start;
            while (el != null) {
                if (el.attributes != null && el.attributes.ContainsKey(key))
                    return el.attributes[key];
                el = el.Parent;
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUri"></param>
        protected override void DoSetBaseUri(string baseUri)
        {
            attributes.Put(BaseUriKey, baseUri);
        }

        /// <summary>
        /// 
        /// </summary>
        public override int ChildNodeSize => childNodes.Count;

        /// <summary>
        /// 
        /// </summary>
        public override string NodeName => tag.Name;

        /// <summary>
        /// Get the normalized name of this Element's tag. This will always be the lower-cased version of the tag, regardless
        /// of the tag case preserving setting of the parser. For e.g., {@code <DIV>} and {@code <div>} both have a
        /// normal name of {@code div}.
        /// </summary>
        public new string NormalName => tag.NormalName;

        /// <summary>
        /// Get or Set the name of the tag for this element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// E.g. <c>div</c>
        /// </para>
        /// <para>
        /// For example, convert a <c>&lt;span&gt;</c> to a <c>&lt;div&gt;</c>
        /// with <c>el.TagName = "div";</c> .
        /// </para>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>the new tag name</value>
        /// <returns>the tag name</returns>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string TagName
        {
            get => tag.Name;
            set
            {
                Validate.NotEmpty(value, "Tag name must not be empty.");
                tag = Tag.ValueOf(value, NodeUtils.Parser(this).Settings);
            }
        }

        /// <summary>
        /// Get the Tag for this element.
        /// </summary>
        /// <returns>the tag object</returns>
        public Tag Tag => tag;

        /// <summary>
        /// Test if this element is a block-level element.
        /// </summary>
        /// <remarks>
        /// (E.g.
        /// <c>&lt;div&gt; == true</c>
        /// or an inline element
        /// <c>&lt;p&gt; == false</c>
        /// ).
        /// </remarks>
        /// <returns>true if block, false if not (and thus inline)</returns>
        public bool IsBlock => tag.IsBlock;

        /// <summary>
        /// Get the <c>id</c> attribute of this element.
        /// </summary>
        /// <returns>The id attribute, if present, or an empty string if not.</returns>
        public string Id
        {
            get => attributes != null ? attributes.GetIgnoreCase("id") : "";
            set
            {
                Validate.NotNull(value);
                Attr("id", value);
            }
        } 

        /// <summary>
        /// Set an attribute value on this element.
        /// </summary>
        /// <remarks>
        /// If this element already has an attribute with the
        /// key, its value is updated; otherwise, a new attribute is added.
        /// </remarks>
        /// <returns>this element</returns>
        public new Element Attr(string attributeKey, string attributeValue)
        {
            base.Attr(attributeKey, attributeValue);
            return this;
        }
        
        /// <summary>
        /// Set a boolean attribute value on this element. Setting to <code>true</code> sets the attribute value to "" and
        /// marks the attribute as boolean so no value is written out. Setting to <code>false</code> removes the attribute
        /// with the same key if it exists.
        /// </summary>
        /// <param name="attributeKey">the attribute key</param>
        /// <param name="attributeValue">the attribute value</param>
        /// <returns></returns>
        public Element Attr(string attributeKey, bool attributeValue)
        {
            attributes.Put(attributeKey, attributeValue);
            return this;
        }

        /// <summary>
        /// Get this element's HTML5 custom data attributes.
        /// </summary>
        /// <remarks>
        /// Each attribute in the element that has a key
        /// starting with "data-" is included the dataset.
        /// <p/>
        /// E.g., the element
        /// <c>&lt;div data-package="jsoup" data-language="Java" class="group"&gt;...</c>
        /// has the dataset
        /// <c>package=jsoup, language=java</c>
        /// .
        /// <p/>
        /// This map is a filtered view of the element's attribute map. Changes to one map (add, remove, update) are reflected
        /// in the other map.
        /// <p/>
        /// You can find elements that have data attributes using the
        /// <c>[^data-]</c>
        /// attribute key prefix selector.
        /// </remarks>
        /// <returns>
        /// a map of
        /// <c>key=value</c>
        /// custom data attributes.
        /// </returns>
        public IDictionary<string, string> Dataset => attributes.Dataset;

        /// <summary>
        /// Gets this element's parent element.
        /// </summary>
        /// <returns></returns>
        public new Element Parent => (Element)parentNode;

        /// <summary>
        /// Get this element's parent and ancestors, up to the document root.
        /// </summary>
        /// <returns>this element's stack of parents, closest first.</returns>
        public Elements Parents
        {
            get
            {
                Elements parents = new Elements();
                var parent = Parent;
                while (parent != null && !parent.IsNode("#root"))
                {
                    parents.Add(parent);
                    parent = parent.Parent;
                }
                return parents;
            }
        }

        /// <summary>
        /// Get a child element of this element, by its 0-based index number.
        /// </summary>
        /// <remarks>
        /// Note that an element can have both mixed Nodes and Elements as children. This method inspects
        /// a filtered list of children that are elements, and the index is based on that filtered list.
        /// </remarks>
        /// <param name="index">the index number of the element to retrieve</param>
        /// <returns>
        /// the child element, if it exists, otherwise throws an
        /// <c>IndexOutOfBoundsException</c>
        /// </returns>
        /// <seealso cref="Node.ChildNode(int)">Node.ChildNode(int)</seealso>
        public Element Child(int index)
        {
            return ChildElementsList()[index];
        }
        
        /// <summary>
        ///  Get the number of child nodes of this element that are elements.
        /// </summary>
        public int ChildrenSize => ChildElementsList().Count;

        /// <summary>
        /// Get this element's child elements.
        /// </summary>
        /// <remarks>
        /// This is effectively a filter on
        /// <see cref="Node.ChildNodes">Node.ChildNodes</see>
        /// to get Element nodes.
        /// </remarks>
        /// <returns>
        /// child elements. If this element has no children, returns an
        /// empty list.
        /// </returns>
        /// <seealso cref="Node.ChildNodes">Node.ChildNodes</seealso>
        public Elements Children => new(ChildElementsList());

        /// <summary>
        /// Maintains a shadow copy of this element's child elements. If the nodelist is changed, this cache is invalidated.
        /// </summary>
        /// <returns></returns>
        internal List<Element> ChildElementsList() {
            if (ChildNodeSize == 0)
                return EmptyChildren; // short circuit creating empty

            if (shadowChildrenRef == null || shadowChildrenRef.TryGetTarget(out var children)) {
                children = childNodes.OfType<Element>().ToList();
                shadowChildrenRef = new WeakReference<List<Element>>(children);
            }
            return children;
        }

        internal override void NodelistChanged()
        {
            base.NodelistChanged();
            shadowChildrenRef = null;
        }

        /// <summary>
        /// Get this element's child text nodes.
        /// </summary>
        /// <remarks>
        /// The list is unmodifiable but the text nodes may be manipulated.
        /// <p/>
        /// This is effectively a filter on
        /// <see cref="Node.ChildNodes">Node.ChildNodes</see>
        /// to get Text nodes.
        /// </remarks>
        /// <returns>
        /// child text nodes. If this element has no text nodes, returns an
        /// empty list.
        /// <p/>
        /// For example, with the input HTML:
        /// <c><![CDATA[<p>One <span>Two</span> Three <br /> Four</p>]]></c>
        /// with the
        /// <c>p</c>
        /// element selected:
        /// <ul>
        /// <li>
        /// <c>p.Text</c>
        /// =
        /// <c>"One Two Three Four"</c>
        /// </li>
        /// <li>
        /// <c>p.OwnText</c>
        /// =
        /// <c>"One Three Four"</c>
        /// </li>
        /// <li>
        /// <c>p.Children</c>
        /// =
        /// <c>Elements[&lt;span&gt;, &lt;br /&gt;]</c>
        /// </li>
        /// <li>
        /// <c>p.ChildNodes</c>
        /// =
        /// <c>List&lt;Node&gt;["One ", &lt;span&gt;, " Three ", &lt;br /&gt;, " Four"]</c>
        /// </li>
        /// <li>
        /// <c>p.TextNodes</c>
        /// =
        /// <c>List&lt;TextNode&gt;["One ", " Three ", " Four"]</c>
        /// </li>
        /// </ul>
        /// </returns>
        public IReadOnlyList<TextNode> TextNodes
        {
            get
            {
                List<TextNode> textNodes = childNodes.OfType<TextNode>().ToList();
                return textNodes.AsReadOnly();
            }
        }

        /// <summary>
        /// Get this element's child data nodes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The list is unmodifiable but the data nodes may be manipulated.
        /// </para>
        /// <para>
        /// This is effectively a filter on
        /// <see cref="Node.ChildNodes">Node.ChildNodes</see>
        /// to get Data nodes.
        /// </para>
        /// </remarks>
        /// <returns>
        /// child data nodes. If this element has no data nodes, returns an
        /// empty list.
        /// </returns>
        /// <seealso cref="Data">Data</seealso>
        public IReadOnlyList<DataNode> DataNodes
        {
            get
            {
                List<DataNode> dataNodes = childNodes.OfType<DataNode>().ToList();
                return dataNodes.AsReadOnly();
            }
        }

        /// <summary>
        /// Find elements that match the
        /// <see cref="Supremes.Select.Selector">Supremes.Select.Selector</see>
        /// CSS query, with this element as the starting context. Matched elements
        /// may include this element, or any of its children.
        /// </summary>
        /// <remarks>
        /// This method is generally more powerful to use than the DOM-type
        /// <c>GetElementBy*</c>
        /// methods, because
        /// multiple filters can be combined, e.g.:
        /// <ul>
        /// <li>
        /// <c>el.Select("a[href]")</c>
        /// - finds links (
        /// <c>a</c>
        /// tags with
        /// <c>href</c>
        /// attributes)
        /// </li>
        /// <li>
        /// <c>el.Select("a[href*=example.com]")</c>
        /// - finds links pointing to example.com (loosely)
        /// </li>
        /// </ul>
        /// <p/>
        /// See the query syntax documentation in
        /// <see cref="Supremes.Select.Selector">Supremes.Select.Selector</see>
        /// .
        /// </remarks>
        /// <param name="cssQuery">
        /// a
        /// <see cref="Supremes.Select.Selector">Supremes.Select.Selector</see>
        /// CSS-like query
        /// </param>
        /// <returns>elements that match the query (empty if none match)</returns>
        /// <seealso cref="Supremes.Select.Selector">Supremes.Select.Selector</seealso>
        public Elements Select(string cssQuery)
        {
            return Selector.Select(cssQuery, this);
        }

        /// <summary>
        /// Find elements that match the supplied Evaluator. This has the same functionality as {@link #select(String)}, but
        /// may be useful if you are running the same query many times (on many documents) and want to save the overhead of
        /// repeatedly parsing the CSS query.
        /// </summary>
        /// <param name="evaluator">an element evaluator</param>
        /// <returns>an {@link Elements} list containing elements that match the query (empty if none match)</returns>
        public Elements Select(Evaluator evaluator)
        {
            return Selector.Select(evaluator, this);
        }
        
        /// <summary>
        /// Find the first Element that matches the {@link Selector} CSS query, with this element as the starting context.
        /// <p>This is effectively the same as calling {@code element.select(query).first()}, but is more efficient as query
        /// execution stops on the first hit.</p>
        /// <p>Also known as {@code querySelector()} in the Web DOM.</p>
        /// </summary>
        /// <param name="cssQuery">cssQuery a {@link Selector} CSS-like query</param>
        /// <returns>the first matching element, or <b>{@code null}</b> if there is no match.</returns>
        public Element SelectFirst(string cssQuery)
        {
            return Selector.SelectFirst(cssQuery, this);
        }
        
        /// <summary>
        /// Finds the first Element that matches the supplied Evaluator, with this element as the starting context, or
        /// {@code null} if none match.
        /// </summary>
        /// <param name="evaluator">an element evaluator</param>
        /// <returns>the first matching element (walking down the tree, starting from this element), or {@code null} if none match.</returns>
        public Element SelectFirst(Evaluator evaluator)
        {
            return Collector.FindFirst(evaluator, this);
        }
        
        /// <summary>
        /// Just like {@link #selectFirst(String)}, but if there is no match, throws an {@link IllegalArgumentException}. This
        /// is useful if you want to simply abort processing on a failed match.
        /// </summary>
        /// <param name="cssQuery">a {@link Selector} CSS-like query</param>
        /// <returns>the first matching element</returns>
        public Element ExpectFirst(string cssQuery) {
            return (Element) Validate.EnsureNotNull(
                Selector.SelectFirst(cssQuery, this),
                Parent != null ?
                    "No elements matched the query '{0}' on element '{1}'.":
                    "No elements matched the query '{0}' in the document."
                , cssQuery, this.TagName
            );
        }
        
        /// <summary>
        /// Checks if this element matches the given {@link Selector} CSS query. Also knows as {@code matches()} in the Web DOM.
        /// </summary>
        /// <param name="cssQuery">a {@link Selector} CSS query</param>
        /// <returns>if this element matches the query</returns>
        public bool Is(string cssQuery)
        {
            return Is(QueryParser.Parse(cssQuery));
        }

        /// <summary>
        /// Check if this element matches the given evaluator.
        /// </summary>
        /// <param name="evaluator">evaluator an element evaluator</param>
        /// <returns>if this element matches</returns>
        public bool Is(Evaluator evaluator)
        {
            return evaluator.Matches(Root as Element, this);
        }
        
        /// <summary>
        /// Find the closest element up the tree of parents that matches the specified CSS query. Will return itself, an
        /// ancestor, or {@code null} if there is no such matching element.
        /// </summary>
        /// <param name="cssQuery">a {@link Selector} CSS query</param>
        /// <returns>the closest ancestor element (possibly itself) that matches the provided evaluator. {@code null} if not found.</returns>
        public Element Closest(string cssQuery) {
            return Closest(QueryParser.Parse(cssQuery));
        }
        
        /// <summary>
        /// Find the closest element up the tree of parents that matches the specified evaluator. Will return itself, an
        /// ancestor, or {@code null} if there is no such matching element.
        /// </summary>
        /// <param name="evaluator">a query evaluator</param>
        /// <returns>the closest ancestor element (possibly itself) that matches the provided evaluator. {@code null} if not found.</returns>
        public Element Closest(Evaluator evaluator) {
            Validate.NotNull(evaluator);
            Element el = this;
            Element root = Root as Element;
            do {
                if (evaluator.Matches(root, el))
                    return el;
                el = el.Parent;
            } while (el != null);
            return null;
        }
        
        /// <summary>
        /// Find Elements that match the supplied XPath expression.
        /// <p>Note that for convenience of writing the Xpath expression, namespaces are disabled, and queries can be
        /// expressed using the element's local name only.</p>
        /// <p>By default, XPath 1.0 expressions are supported. If you would to use XPath 2.0 or higher, you can provide an
        /// alternate XPathFactory implementation:</p>
        /// <ol>
        /// <li>Add the implementation to your classpath. E.g. to use <a href="https://www.saxonica.com/products/products.xml">Saxon-HE</a>, add <a href="https://mvnrepository.com/artifact/net.sf.saxon/Saxon-HE">net.sf.saxon:Saxon-HE</a> to your build.</li>
        /// <li>Set the system property <code>javax.xml.xpath.XPathFactory:jsoup</code> to the implementing classname. E.g.:<br>
        /// <code>System.setProperty(W3CDom.XPathFactoryProperty, "net.sf.saxon.xpath.XPathFactoryImpl");</code>
        /// </li>
        /// </ol>
        /// </summary>
        /// <param name="xpath">XPath expression</param>
        /// <returns>matching elements, or an empty list if none match.</returns>
        public Elements SelectXpath(string xpath) {
            return new Elements(NodeUtils.SelectXpath<Element>(xpath, this, typeof(Element)));
        }
        
        /// <summary>
        /// Find Nodes that match the supplied XPath expression.
        /// <p>For example, to select TextNodes under {@code p} elements: </p>
        /// <pre>List&lt;TextNode&gt; textNodes = doc.selectXpath("//body//p//text()", TextNode.class);</pre>
        /// <p>Note that in the jsoup DOM, Attribute objects are not Nodes. To directly select attribute values, do something
        /// like:</p>
        /// <pre>List&lt;String&gt; hrefs = doc.selectXpath("//a").eachAttr("href");</pre>
        /// </summary>
        /// <param name="xpath">XPath expression</param>
        /// <typeparam name="T">the jsoup node type to return</typeparam>
        /// <returns>a list of matching nodes</returns>
        public List<T> SelectXpath<T>(string xpath) where T : Node {
            return NodeUtils.SelectXpath<T>(xpath, this, typeof(T));
        }

        
        /// <summary>
        /// Add a node child node to this element.
        /// </summary>
        /// <param name="child">node to add.</param>
        /// <returns>this element, so that you can add more child nodes or elements.</returns>
        public Element AppendChild(Node child)
        {
            Validate.NotNull(child);
            
            ReparentChild(child);
            EnsureChildNodes();
            childNodes.Add(child);
            child.siblingIndex = childNodes.Count - 1;
            return this;
        }
        
        /// <summary>
        /// Insert the given nodes to the end of this Element's children.
        /// </summary>
        /// <param name="children">nodes to add</param>
        /// <returns>this Element, for chaining</returns>
        public Element AppendChildren(IEnumerable<Node> children) {
            InsertChildren(-1, children);
            return this;
        }

        /// <summary>
        /// Add this element to the supplied parent element, as its next child.
        /// </summary>
        /// <param name="parent">element to which this element will be appended</param>
        /// <returns>this element, so that you can continue modifying the element</returns>
        public Element AppendTo(Element parent) {
            Validate.NotNull(parent);
            parent.AppendChild(this);
            return this;
        }

        /// <summary>
        /// Add a node to the start of this element's children.
        /// </summary>
        /// <param name="child">node to add.</param>
        /// <returns>this element, so that you can add more child nodes or elements.</returns>
        public Element PrependChild(Node child)
        {
            Validate.NotNull(child);
            AddChildren(0, child);
            return this;
        }
        
        /// <summary>
        /// Add a node to the start of this element's children.
        /// </summary>
        /// <param name="child">nodes to add</param>
        /// <returns>this Element, for chaining</returns>
        public Element PrependChild(IEnumerable<Node> child)
        {
            Validate.NotNull(child);
            InsertChildren(0, child);
            return this;
        }

        /// <summary>
        /// Inserts the given child nodes into this element at the specified index.
        /// </summary>
        /// <remarks>
        /// Current nodes will be shifted to the
        /// right. The inserted nodes will be moved from their current parent. To prevent moving, copy the nodes first.
        /// </remarks>
        /// <param name="index">
        /// 0-based index to insert children at. Specify
        /// <c>0</c>
        /// to insert at the start,
        /// <c>-1</c>
        /// at the
        /// end
        /// </param>
        /// <param name="children">child nodes to insert</param>
        /// <returns>this element, for chaining.</returns>
        public Element InsertChildren(int index, IEnumerable<Node> children)
        {
            Validate.NotNull(children, "Children collection to be inserted must not be null.");
            int currentSize = ChildNodeSize;
            if (index < 0)
            {
                index += currentSize + 1;
            }
            // roll around
            Validate.IsTrue(index >= 0 && index <= currentSize, "Insert position out of bounds.");
            AddChildren(index, children.ToArray());
            return this;
        }
        
        /// <summary>
        /// Inserts the given child nodes into this element at the specified index.
        /// </summary>
        /// <remarks>
        /// Current nodes will be shifted to the
        /// right. The inserted nodes will be moved from their current parent. To prevent moving, copy the nodes first.
        /// </remarks>
        /// <param name="index">
        /// 0-based index to insert children at. Specify
        /// <c>0</c>
        /// to insert at the start,
        /// <c>-1</c>
        /// at the
        /// end
        /// </param>
        /// <param name="children">child nodes to insert</param>
        /// <returns>this element, for chaining.</returns>
        public Element InsertChildren(int index, params Node[] children)
        {
            return InsertChildren(index, children.ToList());
        }

        /// <summary>
        /// Create a new element by tag name, and add it as the last child.
        /// </summary>
        /// <param name="tagName">
        /// the name of the tag (e.g.
        /// <c>div</c>
        /// ).
        /// </param>
        /// <returns>
        /// the new element, to allow you to add content to it, e.g.:
        /// <c>parent.AppendElement("h1").Attr("id", "header").Text("Welcome");</c>
        /// </returns>
        public Element AppendElement(string tagName)
        {
            Element child = new Element(Tag.ValueOf(tagName, NodeUtils.Parser(this).Settings), BaseUri);
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Create a new element by tag name, and add it as the first child.
        /// </summary>
        /// <param name="tagName">
        /// the name of the tag (e.g.
        /// <c>div</c>
        /// ).
        /// </param>
        /// <returns>
        /// the new element, to allow you to add content to it, e.g.:
        /// <c>parent.PrependElement("h1").Attr("id", "header").Text("Welcome");</c>
        /// </returns>
        public Element PrependElement(string tagName)
        {
            Element child = new Element(Tag.ValueOf(tagName, NodeUtils.Parser(this).Settings), BaseUri);
            PrependChild(child);
            return child;
        }

        /// <summary>
        /// Create and append a new TextNode to this element.
        /// </summary>
        /// <param name="text">the unencoded text to add</param>
        /// <returns>this element</returns>
        public Element AppendText(string text)
        {
            Validate.NotNull(text);
            TextNode node = new TextNode(text);
            AppendChild(node);
            return this;
        }

        /// <summary>
        /// Create and prepend a new TextNode to this element.
        /// </summary>
        /// <param name="text">the unencoded text to add</param>
        /// <returns>this element</returns>
        public Element PrependText(string text)
        {
            Validate.NotNull(text);
            TextNode node = new TextNode(text);
            PrependChild(node);
            return this;
        }

        /// <summary>
        /// Add inner HTML to this element.
        /// </summary>
        /// <remarks>
        /// The supplied HTML will be parsed, and each node appended to the end of the children.
        /// </remarks>
        /// <param name="html">HTML to add inside this element, after the existing HTML</param>
        /// <returns>this element</returns>
        /// <seealso cref="Html">Html</seealso>
        public Element Append(string html)
        {
            Validate.NotNull(html);
            IReadOnlyList<Node> nodes = NodeUtils.Parser(this).ParseFragmentInput(html, this, BaseUri);
            AddChildren(nodes.ToArray());
            return this;
        }

        /// <summary>
        /// Add inner HTML into this element.
        /// </summary>
        /// <remarks>
        /// The supplied HTML will be parsed, and each node prepended to the start of the element's children.
        /// </remarks>
        /// <param name="html">HTML to add inside this element, before the existing HTML</param>
        /// <returns>this element</returns>
        /// <seealso cref="Html">Html</seealso>
        public Element Prepend(string html)
        {
            Validate.NotNull(html);
            var nodes = NodeUtils.Parser(this).ParseFragmentInput(html, this, BaseUri);
            AddChildren(0, nodes.ToArray());
            return this;
        }

        /// <summary>
        /// Insert the specified HTML into the DOM before this element (as a preceding sibling).
        /// </summary>
        /// <param name="html">HTML to add before this element</param>
        /// <returns>this element, for chaining</returns>
        /// <seealso cref="After(string)">After(string)</seealso>
        public override Node Before(string html)
        {
            return (Element)base.Before(html);
        }

        /// <summary>
        /// Insert the specified node into the DOM before this node (as a preceding sibling).
        /// </summary>
        /// <param name="node">to add before this element</param>
        /// <returns>this Element, for chaining</returns>
        /// <seealso cref="After(Node)">After(Node)</seealso>
        public override Node Before(Node node)
        {
            return (Element)base.Before(node);
        }

        /// <summary>
        /// Insert the specified HTML into the DOM after this element (as a following sibling).
        /// </summary>
        /// <param name="html">HTML to add after this element</param>
        /// <returns>this element, for chaining</returns>
        /// <seealso cref="Before(string)">Before(string)</seealso>
        public override Node After(string html)
        {
            return (Element)base.After(html);
        }

        /// <summary>
        /// Insert the specified node into the DOM after this node (as a following sibling).
        /// </summary>
        /// <param name="node">to add after this element</param>
        /// <returns>this element, for chaining</returns>
        /// <seealso cref="Before(Node)">Before(Node)</seealso>
        public override Node After(Node node)
        {
            return (Element)base.After(node);
        }

        /// <summary>
        /// Remove all of the element's child nodes.
        /// </summary>
        /// <remarks>
        /// Any attributes are left as-is.
        /// </remarks>
        /// <returns>this element</returns>
        public override Node Empty()
        {
            childNodes.Clear();
            return this;
        }

        /// <summary>
        /// Wrap the supplied HTML around this element.
        /// </summary>
        /// <param name="html">
        /// HTML to wrap around this element, e.g.
        /// <c><![CDATA[<div class="head"></div>]]></c>
        /// . Can be arbitrarily deep.
        /// </param>
        /// <returns>this element, for chaining.</returns>
        public new Element Wrap(string html)
        {
            return (Element)base.Wrap(html);
        }

        /// <summary>
        /// Get a CSS selector that will uniquely select this element.
        /// </summary>
        /// <remarks>
        /// If the element has an ID, returns #id;
        /// otherwise returns the parent (if any) CSS selector, followed by '&gt;',
        /// followed by a unique selector for the element (tag.class.class:nth-child(n)).
        /// </remarks>
        /// <returns>the CSS Path that can be used to retrieve the element in a selector.</returns>
        public string CssSelector
        {
            get
            {
                if (Id.Length > 0)
                {
                    // prefer to return the ID - but check that it's actually unique first!
                    string idSel = "#" + TokenQueue.EscapeCssIdentifier(Id);
                    Document doc = OwnerDocument;
                    if (doc != null) {
                        Elements els = doc.Select(idSel);
                        if (els.Count == 1 && els[0] == this) // otherwise, continue to the nth-child impl
                            return idSel;
                    } else {
                        return idSel; // no ownerdoc, return the ID selector
                    }
                }
                // Escape tagname, and translate HTML namespace ns:tag to CSS namespace syntax ns|tag
                string tagName = TokenQueue.EscapeCssIdentifier(TagName).Replace("\\:", "|");
                StringBuilder selector = StringUtil.BorrowBuilder().Append(tagName);
                // string classes = StringUtil.Join(classNames().stream().map(TokenQueue::escapeCssIdentifier).iterator(), ".");
                StringUtil.StringJoiner escapedClasses = new StringUtil.StringJoiner(".");
                foreach (string name in ClassNames) escapedClasses.Add(TokenQueue.EscapeCssIdentifier(name));
                string classes = escapedClasses.Complete();
                if (classes.Length > 0)
                    selector.Append('.').Append(classes);

                if (Parent is null or Document) // don't add Document to selector, as will always have a html node
                    return StringUtil.ReleaseBuilder(selector);

                selector.Insert(0, " > ");
                if (Parent.Select(selector.ToString()).Count > 1)
                    selector.Append($":nth-child({ElementSiblingIndex + 1})");

                return Parent.CssSelector + StringUtil.ReleaseBuilder(selector);
            }
        }

        /// <summary>
        /// Get sibling elements.
        /// </summary>
        /// <remarks>
        /// If the element has no sibling elements, returns an empty list. An element is not a sibling
        /// of itself, so will not be included in the returned list.
        /// </remarks>
        /// <returns>sibling elements</returns>
        public Elements SiblingElements
        {
            get
            {
                if (parentNode == null)
                {
                    return new Elements(0);
                }
                IList<Element> elements = Parent.ChildElementsList();
                Elements siblings = new Elements(elements.Count - 1);
                foreach (Element el in elements)
                {
                    if (!Equals(el, this))
                    {
                        siblings.Add(el);
                    }
                }
                return siblings;
            }
        }

        /// <summary>
        /// Gets the next sibling element of this element.
        /// </summary>
        /// <remarks>
        /// E.g., if a
        /// <c>div</c>
        /// contains two
        /// <c>p</c>
        /// s,
        /// the
        /// <c>NextElementSibling</c>
        /// of the first
        /// <c>p</c>
        /// is the second
        /// <c>p</c>
        /// .
        /// <p/>
        /// This is similar to
        /// <see cref="Node.NextSibling">Node.NextSibling</see>
        /// , but specifically finds only Elements
        /// </remarks>
        /// <returns>the next element, or null if there is no next element</returns>
        /// <seealso cref="PreviousElementSibling">PreviousElementSibling</seealso>
        public Element NextElementSibling
        {
            get
            {
                if (parentNode == null)
                {
                    return null;
                }
                IList<Element> siblings = Parent.ChildElementsList();
                int index = IndexInList(this, siblings);
                return siblings.Count > index + 1 ? siblings[index + 1] : null;
            }
        }
        
        /// <summary>
        /// Get each of the sibling elements that come after this element.
        /// </summary>
        /// <returns>each of the element siblings after this element, or an empty list if there are no next sibling elements</returns>
        public Elements NextElementSiblings()
        {
            return NextElementSiblings(true);
        }

        /// <summary>
        /// Gets the previous element sibling of this element.
        /// </summary>
        /// <returns>the previous element, or null if there is no previous element</returns>
        /// <seealso cref="NextElementSibling">NextElementSibling</seealso>
        public Element PreviousElementSibling
        {
            get
            {
                if (parentNode == null)
                {
                    return null;
                }
                IList<Element> siblings = Parent.Children;
                int index = IndexInList(this, siblings);
                if (index > 0)
                {
                    return siblings[index - 1];
                }
                else
                {
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Get each of the element siblings before this element.
        /// </summary>
        /// <returns>the previous element siblings, or an empty list if there are none.</returns>
        public Elements PreviousElementSiblings() {
            return NextElementSiblings(false);
        }
        
        private Elements NextElementSiblings(bool next) {
            Elements els = new Elements();
            if (parentNode == null)
                return  els;
            els.Add(this);
            return next ?  els.NextAll() : els.PrevAll();
        }

        /// <summary>
        /// Gets the first element sibling of this element.
        /// </summary>
        /// <returns>the first sibling that is an element (aka the parent's first element child)
        /// </returns>
        public Element FirstElementSibling
        {
            get
            {
                IList<Element> siblings = Parent?.ChildElementsList();
                return siblings is { Count: > 1 } ? siblings[0] : this;
            }
        }

        /// <summary>
        /// Get the list index of this element in its element sibling list.
        /// </summary>
        /// <remarks>
        /// I.e. if this is the first element sibling, returns 0.
        /// </remarks>
        /// <returns>position in element sibling list</returns>
        public int ElementSiblingIndex => Parent == null ? 0 : IndexInList(this, Parent.ChildElementsList());

        /// <summary>
        /// Gets the last element sibling of this element
        /// </summary>
        /// <returns>
        /// the last sibling that is an element (aka the parent's last element child)
        /// </returns>
        public Element LastElementSibling
        {
            get
            {
                IList<Element> siblings = Parent?.ChildElementsList();
                return siblings is { Count :> 1 } ? siblings[siblings.Count - 1] : this;
            }
        }

        private static int IndexInList(Element search, IList<Element> elements)
        {
            int size = elements.Count;
            for (int i = 0; i < size; i++)
            {
                if (elements[i] == search)
                {
                    return i;
                }
            }

            return 0;
        }
        
        /// <summary>
        /// Gets the first child of this Element that is an Element, or {@code null} if there is none.
        /// </summary>
        /// <returns>the first Element child node, or null.</returns>
        public Element FirstElementChild()
        {
            var size = ChildNodeSize;
            if (size == 0) return null;
            var children = EnsureChildNodes();
            for (int i = 0; i < size; i++)
            {
                var node = children[i];
                if (node is Element element) return element;
            }
            return null;
        }

        /// <summary>
        /// Gets the last child of this Element that is an Element, or @{code null} if there is none.
        /// </summary>
        /// <returns>the last Element child node, or null.</returns>
        public Element LastElementChild() {
            var size = ChildNodeSize;
            if (size == 0) return null;
            var children = EnsureChildNodes();
            for (var i = size -1; i >= 0; i--) {
                var node = children[i];
                if (node is Element element) return element;
            }
            return null;
        }


        // DOM type methods
        
        /// <summary>
        /// Finds elements, including and recursively under this element,
        /// with the specified tag name.
        /// </summary>
        /// <param name="tagName">The tag name to search for (case insensitively).</param>
        /// <returns>
        /// a matching unmodifiable list of elements.
        /// Will be empty if this element and none of its children match.
        /// </returns>
        public Elements GetElementsByTag(string tagName)
        {
            Validate.NotEmpty(tagName);
            tagName = Normalizer.Normalize(tagName);
            return Collector.Collect(new Evaluator.Tag(tagName), this);
        }

        /// <summary>
        /// Find an element by ID, including or under this element.
        /// </summary>
        /// <remarks>
        /// Note that this finds the first matching ID, starting with this element. If you search down from a different
        /// starting point, it is possible to find a different element by ID. For unique element by ID within a Document,
        /// use
        /// <see cref="Element.GetElementById(string)">Element.GetElementById(string)</see>
        /// </remarks>
        /// <param name="id">The ID to search for.</param>
        /// <returns>The first matching element by ID, starting with this element, or null if none found.
        /// </returns>
        public Element GetElementById(string id)
        {
            Validate.NotEmpty(id);
            Elements elements = Collector.Collect(new Evaluator.ID(id), this);
            if (elements.Count > 0)
            {
                return elements[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Find elements that have this class, including or under this element.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// <p/>
        /// Elements can have multiple classes (e.g.
        /// <c>&lt;div class="header round first"&gt;</c>
        /// . This method
        /// checks each class, so you can find the above with
        /// <c>el.GetElementsByClass("header");</c>
        /// .
        /// </remarks>
        /// <param name="className">the name of the class to search for.</param>
        /// <returns>elements with the supplied class name, empty if none</returns>
        /// <seealso cref="HasClass(string)">HasClass(string)</seealso>
        /// <seealso cref="ClassNames">ClassNames</seealso>
        public Elements GetElementsByClass(string className)
        {
            Validate.NotEmpty(className);
            return Collector.Collect(new Evaluator.Class(className), this);
        }

        /// <summary>
        /// Find elements that have a named attribute set.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="key">
        /// name of the attribute, e.g.
        /// <c>href</c>
        /// </param>
        /// <returns>elements that have this attribute, empty if none</returns>
        public Elements GetElementsByAttribute(string key)
        {
            Validate.NotEmpty(key);
            key = key.Trim();
            return Collector.Collect(new Evaluator.Attribute(key), this);
        }

        /// <summary>
        /// Find elements that have an attribute name starting with the supplied prefix.
        /// </summary>
        /// <remarks>
        /// Use
        /// <c>data-</c>
        /// to find elements
        /// that have HTML5 datasets.
        /// </remarks>
        /// <param name="keyPrefix">
        /// name prefix of the attribute e.g.
        /// <c>data-</c>
        /// </param>
        /// <returns>elements that have attribute names that start with with the prefix, empty if none.
        /// </returns>
        public Elements GetElementsByAttributeStarting(string keyPrefix)
        {
            Validate.NotEmpty(keyPrefix);
            keyPrefix = keyPrefix.Trim();
            return Collector.Collect(new Evaluator.AttributeStarting(keyPrefix), this);
        }

        /// <summary>
        /// Find elements that have an attribute with the specific value.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="key">name of the attribute</param>
        /// <param name="value">value of the attribute</param>
        /// <returns>elements that have this attribute with this value, empty if none</returns>
        public Elements GetElementsByAttributeValue(string key, string value)
        {
            return Collector.Collect(new Evaluator.AttributeWithValue(key, value), this);
        }

        /// <summary>
        /// Find elements that either do not have this attribute,
        /// or have it with a different value.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="key">name of the attribute</param>
        /// <param name="value">value of the attribute</param>
        /// <returns>elements that do not have a matching attribute</returns>
        public Elements GetElementsByAttributeValueNot(string key, string value)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueNot(key, value), this);
        }

        /// <summary>
        /// Find elements that have attributes that start with the value prefix.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="key">name of the attribute</param>
        /// <param name="valuePrefix">start of attribute value</param>
        /// <returns>elements that have attributes that start with the value prefix</returns>
        public Elements GetElementsByAttributeValueStarting(string key, string valuePrefix)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueStarting(key, valuePrefix), this);
        }

        /// <summary>
        /// Find elements that have attributes that end with the value suffix.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="key">name of the attribute</param>
        /// <param name="valueSuffix">end of the attribute value</param>
        /// <returns>elements that have attributes that end with the value suffix</returns>
        public Elements GetElementsByAttributeValueEnding(string key, string valueSuffix)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueEnding(key, valueSuffix), this);
        }

        /// <summary>
        /// Find elements that have attributes whose value contains the match string.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="key">name of the attribute</param>
        /// <param name="match">substring of value to search for</param>
        /// <returns>elements that have attributes containing this text</returns>
        public Elements GetElementsByAttributeValueContaining(string key, string match)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueContaining(key, match), this);
        }

        /// <summary>
        /// Find elements that have attributes whose values match the supplied regular expression.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="pattern">compiled regular expression to match against attribute values
        /// </param>
        /// <returns>elements that have attributes matching this regular expression</returns>
        public Elements GetElementsByAttributeValueMatching(string key, Regex pattern)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueMatching(key, pattern), this);
        }
        //public Elements GetElementsByAttributeValueMatching(string key, Sharpen.Pattern pattern)

        /// <summary>
        /// Find elements that have attributes whose values match the supplied regular expression.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="regex">regular expression to match against attribute values. You can use <a href="http://java.sun.com/docs/books/tutorial/essential/regex/pattern.html#embedded">embedded flags</a> (such as (?i) and (?m) to control regex options.
        /// </param>
        /// <returns>elements that have attributes matching this regular expression</returns>
        public Elements GetElementsByAttributeValueMatching(string key, string regex)
        {
            var pattern = new Regex(regex, RegexOptions.Compiled); // may throw an exception
            return GetElementsByAttributeValueMatching(key, pattern);
        }

        /// <summary>
        /// Find elements whose sibling index is less than the supplied index.
        /// </summary>
        /// <param name="index">0-based index</param>
        /// <returns>elements less than index</returns>
        public Elements GetElementsByIndexLessThan(int index)
        {
            return Collector.Collect(new Evaluator.IndexLessThan(index), this);
        }

        /// <summary>
        /// Find elements whose sibling index is greater than the supplied index.
        /// </summary>
        /// <param name="index">0-based index</param>
        /// <returns>elements greater than index</returns>
        public Elements GetElementsByIndexGreaterThan(int index)
        {
            return Collector.Collect(new Evaluator.IndexGreaterThan(index), this);
        }

        /// <summary>
        /// Find elements whose sibling index is equal to the supplied index.
        /// </summary>
        /// <param name="index">0-based index</param>
        /// <returns>elements equal to index</returns>
        public Elements GetElementsByIndexEquals(int index)
        {
            return Collector.Collect(new Evaluator.IndexEquals(index), this);
        }

        /// <summary>
        /// Find elements that contain the specified string.
        /// </summary>
        /// <remarks>
        /// The search is case insensitive. The text may appear directly
        /// in the element, or in any of its descendants.
        /// </remarks>
        /// <param name="searchText">to look for in the element's text</param>
        /// <returns>elements that contain the string, case insensitive.</returns>
        /// <seealso cref="Element.Text">Element.Text</seealso>
        public Elements GetElementsContainingText(string searchText)
        {
            return Collector.Collect(new Evaluator.ContainsText(searchText), this);
        }

        /// <summary>
        /// Find elements that directly contain the specified string.
        /// </summary>
        /// <remarks>
        /// The search is case insensitive. The text must appear directly
        /// in the element, not in any of its descendants.
        /// </remarks>
        /// <param name="searchText">to look for in the element's own text</param>
        /// <returns>elements that contain the string, case insensitive.</returns>
        /// <seealso cref="Element.OwnText">Element.OwnText</seealso>
        public Elements GetElementsContainingOwnText(string searchText)
        {
            return Collector.Collect(new Evaluator.ContainsOwnText(searchText), this);
        }

        /// <summary>
        /// Find elements whose text matches the supplied regular expression.
        /// </summary>
        /// <param name="pattern">regular expression to match text against</param>
        /// <returns>elements matching the supplied regular expression.</returns>
        /// <seealso cref="Element.Text">Element.Text</seealso>
        public Elements GetElementsMatchingText(Regex pattern)
        {
            return Collector.Collect(new Evaluator.MatchesText(pattern), this);
        }

        /// <summary>
        /// Find elements whose text matches the supplied regular expression.
        /// </summary>
        /// <param name="regex">regular expression to match text against. You can use <a href="http://java.sun.com/docs/books/tutorial/essential/regex/pattern.html#embedded">embedded flags</a> (such as (?i) and (?m) to control regex options.
        /// </param>
        /// <returns>elements matching the supplied regular expression.</returns>
        /// <seealso cref="Element.Text">Element.Text</seealso>
        public Elements GetElementsMatchingText(string regex)
        {
            var pattern = new Regex(regex, RegexOptions.Compiled); // may throw an exception
            return GetElementsMatchingText(pattern);
        }

        /// <summary>
        /// Find elements whose own text matches the supplied regular expression.
        /// </summary>
        /// <param name="pattern">regular expression to match text against</param>
        /// <returns>elements matching the supplied regular expression.</returns>
        /// <seealso cref="Element.OwnText">Element.OwnText</seealso>
        public Elements GetElementsMatchingOwnText(Regex pattern)
        {
            return Collector.Collect(new Evaluator.MatchesOwnText(pattern), this);
        }

        /// <summary>
        /// Find elements whose text matches the supplied regular expression.
        /// </summary>
        /// <param name="regex">regular expression to match text against. You can use <a href="http://java.sun.com/docs/books/tutorial/essential/regex/pattern.html#embedded">embedded flags</a> (such as (?i) and (?m) to control regex options.
        /// </param>
        /// <returns>elements matching the supplied regular expression.</returns>
        /// <seealso cref="Element.OwnText">Element.OwnText</seealso>
        public Elements GetElementsMatchingOwnText(string regex)
        {
            Regex pattern = new Regex(regex, RegexOptions.Compiled); // may throw an exception
            return GetElementsMatchingOwnText(pattern);
        }

        /// <summary>
        /// Find all elements under this element (including self, and children of children).
        /// </summary>
        /// <returns>all elements</returns>
        public Elements GetAllElements()
        {
            return Collector.Collect(new Evaluator.AllElements(), this);
        }

        /// <summary>
        /// Get or Set the combined text of this element and all its children.
        /// </summary>
        /// <remarks>
        /// <para>
        /// when get, whitespace is normalized and trimmed.
        /// <p/>
        /// For example, given HTML
        /// <c>&lt;p&gt;Hello  &lt;b&gt;there&lt;/b&gt; now! &lt;/p&gt;</c>,
        /// <c>p.Text</c> returns <c>"Hello there now!"</c>
        /// </para>
        /// <para>
        /// when set, any existing contents (text or elements) will be cleared.
        /// </para>
        /// </remarks>
        /// <value>unencoded text</value>
        /// <returns>unencoded text, or empty string if none.</returns>
        /// <seealso cref="OwnText">OwnText</seealso>
        /// <seealso cref="TextNodes">TextNodes</seealso>
        public virtual string Text
        {
            get
            {
                StringBuilder accum = StringUtil.BorrowBuilder();
                NodeTraversor.Traverse(new TextVisitor(accum), this);
                return StringUtil.ReleaseBuilder(accum).Trim();
            }
            set
            {
                Validate.NotNull(value);
                Empty();
                var owner = OwnerDocument;
                if (owner != null && owner.Parser.IsContentForTagData(NormalName))
                {
                    AppendChild(new DataNode(value));
                }
                else
                {
                    AppendChild(new TextNode(value));
                }
            }
        }
        
        /// <summary>
        /// Get the non-normalized, decoded text of this element and its children, including only any newlines and spaces
        /// present in the original source.
        /// </summary>
        public string WholeText
        {
            get
            {
                var accum = StringUtil.BorrowBuilder();
                NodeTraversor.Traverse(new LambdaNodeVisitor((node, depth) => AppendWholeText(node, accum)), this);
                return StringUtil.ReleaseBuilder(accum);
            }
        }

        private static void AppendWholeText(Node node, StringBuilder accum) {
            if (node is TextNode textNode) {
                accum.Append(textNode.WholeText);
            } else if (node.IsNode("br")) {
                accum.Append("\n");
            }
        }
        
        /// <summary>
        /// Get the non-normalized, decoded text of this element, <b>not including</b> any child elements, including only any
        /// newlines and spaces present in the original source.
        /// </summary>
        public string WholeOwnText
        {
            get
            {
                var accum = StringUtil.BorrowBuilder();
                var size = ChildNodeSize;
                for (var i = 0; i < size; i++)
                {
                    var node = ChildNodes()[i];
                    AppendWholeText(node, accum);
                }

                return StringUtil.ReleaseBuilder(accum);
            }
        }

        private sealed class TextVisitor : INodeVisitor
        {
            internal TextVisitor(StringBuilder accum)
            {
                this.accum = accum;
            }

            public void Head(Node node, int depth)
            {
                if (node is TextNode textNode)
                {
                    AppendNormalisedText(accum, textNode);
                }
                else
                {
                    if (node is Element element)
                    {
                        if (accum.Length > 0 && (element.IsBlock || element.TagName.Equals("br")) && 
                            !TextNode.LastCharIsWhitespace(accum))
                        {
                            accum.Append(" ");
                        }
                    }
                }
            }

            public void Tail(Node node, int depth)
            {
                if (node is Element element)
                {
                    Node next = node.NextSibling;
                    if (element.IsBlock && (next is TextNode || (next is Element element1 && element1.tag.FormatAsBlock))
                                        && !TextNode.LastCharIsWhitespace(accum))
                    {
                        accum.Append(" ");
                    }
                }
            }

            private readonly StringBuilder accum;
        }

        /// <summary>
        /// Gets the text owned by this element only;
        /// does not get the combined text of all children.
        /// </summary>
        /// <remarks>
        /// For example, given HTML
        /// <c>&lt;p&gt;Hello &lt;b&gt;there&lt;/b&gt; now!&lt;/p&gt;</c>
        /// ,
        /// <c>p.OwnText</c>
        /// returns
        /// <c>"Hello now!"</c>
        /// ,
        /// whereas
        /// <c>p.Text</c>
        /// returns
        /// <c>"Hello there now!"</c>
        /// .
        /// Note that the text within the
        /// <c>b</c>
        /// element is not returned, as it is not a direct child of the
        /// <c>p</c>
        /// element.
        /// </remarks>
        /// <returns>unencoded text, or empty string if none.</returns>
        /// <seealso cref="Text">Text</seealso>
        /// <seealso cref="TextNodes">TextNodes</seealso>
        public string OwnText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                AppendOwnTextTo(sb);
                return sb.ToString().Trim();
            }
        }

        private void AppendOwnTextTo(StringBuilder accum)
        {
            foreach (Node child in childNodes)
            {
                if (child is TextNode textNode)
                {
                    AppendNormalisedText(accum, textNode);
                }
                else if (child.IsNode("br") && !TextNode.LastCharIsWhitespace(accum))
                {
                    accum.Append(" ");
                }
            }
        }

        private static void AppendNormalisedText(StringBuilder accum, TextNode textNode)
        {
            string text = textNode.WholeText;
            if (PreserveWhitespace(textNode.ParentNode) || textNode is CDataNode)
            {
                accum.Append(text);
            }
            else
            {
                StringUtil.AppendNormalisedWhitespace(accum, text, TextNode.LastCharIsWhitespace(accum));
            }
        }

        internal static bool PreserveWhitespace(Node node)
        {
            // looks only at this element and five levels up, to prevent recursion & needless stack searches
            if (node is Element el) {
                int i = 0;
                do {
                    if (el.tag.PreserveWhitespace)
                        return true;
                    el = el.Parent;
                    i++;
                } while (i < 6 && el != null);
            }
            return false;
        }

        /// <summary>
        /// Test if this element has any text content (that is not just whitespace).
        /// </summary>
        /// <returns>true if element has non-blank text content.</returns>
        public bool HasText
        {
            get
            {
                bool hasText = false;
                Filter(new LambdaNodeFilter((node, depth) => {
                    if (node is TextNode textNode) {
                        if (!textNode.IsBlank) {
                            hasText = true;
                            return NodeFilter.FilterResult.Stop;
                        }
                    }
                    return NodeFilter.FilterResult.Continue;
                }));
                return hasText;
            }
        }

        /// <summary>
        /// Get the combined data of this element.
        /// </summary>
        /// <remarks>
        /// Data is e.g. the inside of a
        /// <c>script</c>
        /// tag.
        /// </remarks>
        /// <returns>the data, or empty string if none</returns>
        /// <seealso cref="DataNodes">DataNodes</seealso>
        public string Data
        {
            get
            {
                StringBuilder sb = StringUtil.BorrowBuilder();
                Traverse(new LambdaNodeVisitor((childNode, depth) => {
                    if (childNode is DataNode data) {
                        sb.Append(data.WholeData);
                    } else if (childNode is Comment comment) {
                        sb.Append(comment.Data);
                    } else if (childNode is CDataNode cDataNode) {
                        // this shouldn't really happen because the html parser won't see the cdata as anything special when parsing script.
                        // but in case another type gets through.
                        sb.Append(cDataNode.WholeText);
                    }
                }));
                return StringUtil.ReleaseBuilder(sb);
            }
        }

        /// <summary>
        /// Gets the literal value of this element's "class" attribute,
        /// which may include multiple class names, space separated.
        /// </summary>
        /// <remarks>
        /// (E.g. on <c>&lt;div class="header gray"&gt;</c> returns,
        /// "<c>header gray</c>")
        /// </remarks>
        /// <returns>
        /// The literal class attribute, or <b>empty string</b>
        /// if no class attribute set.
        /// </returns>
        public string ClassName => Attr("class").Trim();

        /// <summary>
        /// Get or Set all of the element's class names.
        /// </summary>
        /// <remarks>
        /// <para>
        /// E.g. on element
        /// <c>&lt;div class="header gray"&gt;</c>, 
        /// this property returns a set of two elements
        /// <c>"header", "gray"</c>.
        /// Note that modifications to this set are not pushed to
        /// the backing <c>class</c> attribute;
        /// use the
        /// <see cref="ClassNames">ClassNames</see>
        /// method to persist them.
        /// </para>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>the new set of classes</value>
        /// <returns>set of classnames, empty if no class attribute</returns>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public ICollection<string> ClassNames
        {
            get
            {
                string[] names = ClassSplit.Split(ClassName);
                var classNames = new LinkedHashSet<string>(names);
                classNames.Remove("");
                return classNames;
            }
            set
            {
                Validate.NotNull(value);
                if (ClassNames.Count == 0)
                {
                    attributes.Remove("class");
                }
                else
                {
                    attributes["class"] = string.Join(" ", value);
                }
                
            }
        }

        /// <summary>
        /// Tests if this element has a class.
        /// </summary>
        /// <remarks>
        /// Case insensitive.
        /// </remarks>
        /// <param name="className">name of class to check for</param>
        /// <returns>true if it does, false if not</returns>
        public bool HasClass(string className)
        {
            if (attributes == null)
                return false;

            string classAttr = attributes.GetIgnoreCase("class");
            int len = classAttr.Length;
            int wantLen = className.Length;

            if (len == 0 || len < wantLen) {
                return false;
            }

            // if both lengths are equal, only need compare the className with the attribute
            if (len == wantLen) {
                return className.Equals(classAttr, StringComparison.InvariantCultureIgnoreCase);
            }

            // otherwise, scan for whitespace and compare regions (with no string or arraylist allocations)
            bool inClass = false;
            int start = 0;
            for (int i = 0; i < len; i++) {
                if (char.IsWhiteSpace(classAttr[i])) {
                    if (inClass) {
                        // white space ends a class name, compare it with the requested one, ignore case
                        if (i - start == wantLen && classAttr.Substring(start, wantLen).Equals(className, StringComparison.InvariantCultureIgnoreCase)) {
                            return true;
                        }
                        inClass = false;
                    }
                } else {
                    if (!inClass) {
                        // we're in a class name : keep the start of the substring
                        inClass = true;
                        start = i;
                    }
                }
            }

            // check the last entry
            if (inClass && len - start == wantLen) {
                return classAttr.Substring(start, wantLen).Equals(className, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// Add a class name to this element's
        /// <c>class</c>
        /// attribute.
        /// </summary>
        /// <param name="className">class name to add</param>
        /// <returns>this element</returns>
        public Element AddClass(string className)
        {
            Validate.NotNull(className);
            ICollection<string> classes = ClassNames;
            classes.Add(className);
            ClassNames = classes;
            return this;
        }

        /// <summary>
        /// Remove a class name from this element's
        /// <c>class</c>
        /// attribute.
        /// </summary>
        /// <param name="className">class name to remove</param>
        /// <returns>this element</returns>
        public Element RemoveClass(string className)
        {
            Validate.NotNull(className);
            ICollection<string> classes = ClassNames;
            classes.Remove(className);
            ClassNames = classes;
            return this;
        }

        /// <summary>
        /// Toggle a class name on this element's
        /// <c>class</c>
        /// attribute: if present, remove it; otherwise add it.
        /// </summary>
        /// <param name="className">class name to toggle</param>
        /// <returns>this element</returns>
        public Element ToggleClass(string className)
        {
            Validate.NotNull(className);
            ICollection<string> classes = ClassNames;
            if (classes.Contains(className))
            {
                classes.Remove(className);
            }
            else
            {
                classes.Add(className);
            }
            ClassNames = classes;
            return this;
        }

        /// <summary>
        /// Get or Set the value of a form element (input, textarea, etc).
        /// </summary>
        /// <remarks>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>value to set</value>
        /// <returns>the value of the form element, or empty string if not set.</returns>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string Val
        {
            get
            {
                if (NormalName.Equals("textarea"))
                {
                    return Text;
                }
                else
                {
                    return Attr("value");
                }
            }
            set
            {
                if (NormalName.Equals("textarea"))
                {
                    Text = value;
                }
                else
                {
                    Attr("value", value);
                }
            }
        }

        internal bool ShouldIndent(DocumentOutputSettings @out) {
            return @out.PrettyPrint && IsFormatAsBlock(@out) && !IsInlineable(@out) && !PreserveWhitespace(parentNode);
        }

        internal override void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            if (ShouldIndent(@out))
            {
                if (accum is StringBuilder)
                {
                    if (((StringBuilder)accum).Length > 0)
                        Indent(accum, depth, @out);
                }
                else
                {
                    Indent(accum, depth, @out);
                }
            }

            accum.Append('<').Append(TagName);
            attributes?.AppendHtmlTo(accum, @out);

            // selfclosing includes unknown tags, isEmpty defines tags that are always empty
            if (childNodes.Count == 0 && tag.IsSelfClosing)
            {
                if (@out.Syntax == DocumentSyntax.Html && tag.IsEmpty)
                    accum.Append('>');
                else
                    accum.Append(" />"); // <img> in html, <img /> in xml
            }
            else
                accum.Append('>');
        }

        internal override void AppendOuterHtmlTailTo(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            if (!(childNodes.Count == 0 && tag.IsSelfClosing))
            {
                if (@out.PrettyPrint
                    && (childNodes.Count > 0
                        && (tag.FormatAsBlock
                            || !PreserveWhitespace(ParentNode))
                        || (@out.Outline && (childNodes.Count > 1 || 
                                             (childNodes.Count == 1 && childNodes[0] is Element)))))
                {
                    Indent(accum, depth, @out);
                }
                accum.Append("</").Append(TagName).Append(">");
            }
        }

        /// <summary>
        /// Get Or Set the element's inner HTML.
        /// </summary>
        /// <remarks>
        /// <para>
        /// when get on a <c>&lt;div&gt;</c> with one empty <c>&lt;p&gt;</c>,
        /// would return <c>&lt;p&gt;&lt;/p&gt;</c>.
        /// (Whereas
        /// <see cref="Node.OuterHtml">Node.OuterHtml</see>
        /// would return
        /// <c>&lt;div&gt;&lt;p&gt;&lt;/p&gt;&lt;/div&gt;</c>.)
        /// </para>
        /// <para>
        /// when set, clears the existing HTML first.
        /// </para>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>HTML to parse and set into this element</value>
        /// <returns>String of HTML.</returns>
        /// <seealso cref="Node.OuterHtml">Node.OuterHtml</seealso>
        /// <seealso cref="Append(string)">Append(string)</seealso>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string Html
        {
            get
            {
                StringBuilder accum = StringUtil.BorrowBuilder();
                AppendHtmlTo(accum);
                var html = StringUtil.ReleaseBuilder(accum);
                return NodeUtils.OutputSettings(this).PrettyPrint
                    ? html.Trim()
                    : html;
            }
            set
            {
                Empty();
                Append(value);
            }
        }

        private void AppendHtmlTo(StringBuilder accum)
        {
            foreach (Node node in childNodes)
            {
                ((Node)node).AppendOuterHtmlTo(accum);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Node ShallowClone()
        {
            return new Element(tag, BaseUri, attributes?.Clone());
        }

        internal override Node DoClone(Node parent)
        {
            var clone = base.DoClone(parent) as Element;
            clone.attributes = attributes?.Clone();
            clone.childNodes = new NodeList(clone, childNodes.Count);
            clone.childNodes.AddRange(childNodes);
            return clone;
        }

        public override Node ClearAttributes()
        {
            if (attributes != null)
            {
                base.ClearAttributes();
                attributes = null;
            }

            return this;
        }

        /// <summary>
        /// Perform the supplied action on this Element and each of its descendant Elements, during a depth-first traversal.
        /// Elements may be inspected, changed, added, replaced, or removed.
        /// </summary>
        /// <param name="action">the function to perform on the element</param>
        /// <returns> this Element, for chaining</returns>
        public Element ForEach(Action<Element> action)
        {
            Validate.NotNull(action);
            NodeTraversor.Traverse(new LambdaNodeVisitor((node, depth) => {
                if (node is Element element)
                    action(element);
            }), this);
            return this;
        }
        
        private bool IsFormatAsBlock(DocumentOutputSettings @out)
        {
            return tag.FormatAsBlock || (Parent != null && Parent.Tag.FormatAsBlock) || @out.Outline;
        }

        private bool IsInlineable(DocumentOutputSettings @out)
        {
            if (!tag.IsInline)
                return false;
            return (Parent == null || Parent.IsBlock)
                   && !IsEffectivelyFirst()
                   && !@out.Outline
                && !IsNode("br");
        }

        /// <summary>
        /// Converts the value of this instance to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return OuterHtml;
        }

        /// <summary>
        /// Compares two <see cref="Element"/> instances for equality.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            return this == o;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // todo: fixup, not very useful
            int result = base.GetHashCode();
            unchecked
            {
                result = 31 * result + (tag != null ? tag.GetHashCode() : 0);
            }
            return result;
        }


        private class NodeList: List<Node>
        {
            private readonly Element owner;

            public NodeList(Element owner, int initialCapacity) : base(initialCapacity) {
                this.owner = owner;
            }

            public void OnContentsChanged() {
                owner.NodelistChanged();
            }
        }
    }
}
