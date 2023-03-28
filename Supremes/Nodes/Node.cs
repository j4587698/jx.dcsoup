using Supremes.Helper;
using Supremes.Parsers;
using Supremes.Select;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Supremes.Nodes
{
    /// <summary>
    /// The base, abstract Node model.
    /// </summary>
    /// <remarks>
    /// Elements, Documents, Comments etc are all Node instances.
    /// </remarks>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public abstract class Node
    {
        internal Node parentNode;

        internal static readonly List<Node> EmptyNodes = new();
        
        internal const string EmptyString = "";

        internal int siblingIndex;

        /// <summary>
        /// Default constructor. Doesn't set up base uri, children, or attributes; use with caution.
        /// </summary>
        protected Node()
        {
            
        }

        /// <summary>
        /// Get the node name of this node.
        /// </summary>
        /// <remarks>
        /// Use for debugging purposes and not logic switching (for that, use instanceof).
        /// </remarks>
        /// <returns>node name</returns>
        public abstract string NodeName { get; }
        
        /// <summary>
        /// Get the normalized name of this node. For node types other than Element, this is the same as {@link #nodeName()}.
        /// For an Element, will be the lower-cased tag name.
        /// </summary>
        public string NormalName => NodeName;
        
        /// <summary>
        /// Check if this Node has an actual Attributes object.
        /// </summary> 
        protected abstract bool HasAttributes { get; }
        
        /// Checks if this node has a parent. Nodes won't have parents if (e.g.) they are newly created and not added as a child
        /// to an existing node, or if they are a {@link #shallowClone()}. In such cases, {@link #parent()} will return {@code null}.
        /// @return if this node has a parent.
        public virtual bool HasParent => parentNode != null;

        /// <summary>
        /// Get an attribute's value by its key.
        /// </summary>
        /// <remarks>
        /// To get an absolute URL from an attribute that may be a relative URL, prefix the key with <c><b>abs</b></c>,
        /// which is a shortcut to the
        /// <see cref="AbsUrl(string)">AbsUrl(string)</see>
        /// method.
        /// E.g.: <blockquote><c>String url = a.attr("abs:href");</c></blockquote>
        /// </remarks>
        /// <param name="attributeKey">The attribute key.</param>
        /// <returns>The attribute, or empty string if not present (to avoid nulls).</returns>
        /// <seealso cref="Attributes">Attributes</seealso>
        /// <seealso cref="HasAttr(string)">HasAttr(string)</seealso>
        /// <seealso cref="AbsUrl(string)">AbsUrl(string)</seealso>
        public virtual string Attr(string attributeKey)
        {
            Validate.NotNull(attributeKey);
            if (!HasAttributes)
            {
                return EmptyString;
            }

            var val = Attributes.GetIgnoreCase(attributeKey);
            if (val.Length > 0)
            {
                return val;
            }

            if (attributeKey.StartsWith("abs:", StringComparison.Ordinal))
            {
                return AbsUrl(attributeKey.Substring("abs:".Length));
            }

            return EmptyString;
        }

        /// <summary>
        /// Get all of the element's attributes.
        /// </summary>
        /// <returns>
        /// attributes (which implements iterable, in same order as presented in original HTML).
        /// </returns>
        public abstract Attributes Attributes { get; }
        
        /// <summary>
        ///  Get the number of attributes that this Node has.
        /// </summary>
        /// <returns>the number of attributes</returns>
        public int AttributesSize => HasAttributes ? Attributes.Count : 0;

        /// <summary>
        /// Set an attribute (key=value).
        /// </summary>
        /// <remarks>
        /// If the attribute already exists, it is replaced.
        /// </remarks>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns>this (for chaining)</returns>
        public virtual Node Attr(string attributeKey, string attributeValue)
        {
            attributeKey = NodeUtils.Parser(this).Settings.NormalizeAttribute(attributeKey);
            Attributes.PutIgnoreCase(attributeKey, attributeValue);
            return this;
        }

        /// <summary>
        /// Test if this element has an attribute.
        /// </summary>
        /// <param name="attributeKey">The attribute key to check.</param>
        /// <returns>true if the attribute exists, false if not.</returns>
        public virtual bool HasAttr(string attributeKey)
        {
            Validate.NotNull(attributeKey);
            if (!HasAttributes)
            {
                return false;
            }
            if (attributeKey.StartsWith("abs:", StringComparison.Ordinal))
            {
                string key = attributeKey.Substring("abs:".Length); /*substring*/
                if (Attributes.ContainsKeyIgnoreCase(key) && !AbsUrl(key).Equals(string.Empty))
                {
                    return true;
                }
            }
            return Attributes.ContainsKeyIgnoreCase(attributeKey);
        }

        /// <summary>
        /// Remove an attribute from this element.
        /// </summary>
        /// <param name="attributeKey">The attribute to remove.</param>
        /// <returns>this (for chaining)</returns>
        public virtual Node RemoveAttr(string attributeKey)
        {
            Validate.NotNull(attributeKey);
            if (HasAttributes)
            {
                Attributes.RemoveIgnoreCase(attributeKey);
            }
            return this;
        }
        
        /// <summary>
        ///  Clear (remove) each of the attributes in this node.
        /// </summary>
        /// <returns>this, for chaining</returns>
        public virtual Node ClearAttributes()
        {
            if (HasAttributes)
            {
                Attributes.Clear();
            }
            return this;
        }

        /// <summary>
        ///  Get the base URI that applies to this node. Will return an empty string if not defined. Used to make relative links
        /// absolute.
        /// </summary>
        public abstract string BaseUri { get; }
        
        /// <summary>
        /// Set the baseUri for just this node (not its descendants), if this Node tracks base URIs.
        /// </summary>
        /// <param name="baseUri">baseUri new URI</param>
        protected abstract void DoSetBaseUri(string baseUri);
        
        /// <summary>
        /// Update the base URI of this node and all of its descendants.
        /// </summary>
        /// <param name="baseUri">base URI to set</param>
        public void SetBaseUri(string baseUri) {
            Validate.NotNull(baseUri);
            DoSetBaseUri(baseUri);
        }

        /// <summary>
        /// Get an absolute URL from a URL attribute that may be relative
        /// (i.e. an <c>&lt;a href&gt;</c> or <c>&lt;img src&gt;</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// E.g.: <c>string absUrl = linkEl.AbsUrl("href");</c>
        /// </para>
        /// <para>
        /// If the attribute value is already absolute (i.e. it starts with a protocol, like
        /// <c>http://</c> or <c>https://</c> etc), and it successfully parses as a URL, the attribute is
        /// returned directly. Otherwise, it is treated as a URL relative to the element's
        /// <see cref="BaseUri">BaseUri</see>
        /// , and made
        /// absolute using that.
        /// </para>
        /// <para>
        /// As an alternate, you can use the
        /// <see cref="Attr(string)">Attr(string)</see>
        /// method with the <c>abs:</c> prefix, e.g.:
        /// <c>string absUrl = linkEl.Attr("abs:href");</c>
        /// </para>
        /// <para>
        /// This method add trailing slash to domain name: i.e.
        /// from <c>&lt;a id=2 href='http://jsoup.org'&gt;</c>
        /// to <c>"http://jsoup.org/"</c>
        /// </para>
        /// </remarks>
        /// <param name="attributeKey">The attribute key</param>
        /// <returns>
        /// An absolute URL if one could be made, or an empty string (not null) if the attribute was missing or
        /// could not be made successfully into a URL.
        /// </returns>
        /// <seealso cref="Attr(string)">Attr(string)</seealso>
        /// <seealso cref="System.Uri.TryCreate(string,UriKind,out Uri)">System.Uri.TryCreate(string,UriKind,out Uri)</seealso>
        public virtual string AbsUrl(string attributeKey)
        {
            Validate.NotEmpty(attributeKey);
            if (!(HasAttributes && Attributes.ContainsKeyIgnoreCase(attributeKey))) // not using hasAttr, so that we don't recurse down hasAttr->absUrl
                return "";

            return StringUtil.Resolve(BaseUri, Attributes.GetIgnoreCase(attributeKey));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal abstract List<Node> EnsureChildNodes();
        
        /// <summary>
        /// Get a child node by its 0-based index.
        /// </summary>
        /// <param name="index">index of child node</param>
        /// <returns>
        /// the child node at this index. Throws a
        /// <c>IndexOutOfBoundsException</c>
        /// if the index is out of bounds.
        /// </returns>
        public Node ChildNode(int index)
        {
            return EnsureChildNodes()[index];
        }

        /// <summary>
        /// Get this node's children.
        /// </summary>
        /// <remarks>
        /// Presented as an unmodifiable list: new children can not be added, but the child nodes
        /// themselves can be manipulated.
        /// </remarks>
        /// <returns>list of children. If no children, returns an empty list.</returns>
        public IList<Node> ChildNodes()
        {
            if (ChildNodeSize == 0)
                return EmptyNodes;

            List<Node> children = EnsureChildNodes();
            List<Node> rewrap = new List<Node>(children.Count); // wrapped so that looping and moving will not throw a CME as the source changes
            rewrap.AddRange(children);
            return rewrap.AsReadOnly();
        }

        /// <summary>
        /// Returns a deep copy of this node's children.
        /// </summary>
        /// <remarks>
        /// Changes made to these nodes will not be reflected in the original nodes
        /// </remarks>
        /// <returns>a deep copy of this node's children</returns>
        public IList<Node> ChildNodesCopy()
        {
            var childNodes = EnsureChildNodes();
            IList<Node> children = new List<Node>(childNodes.Count);
            foreach (Node node in childNodes)
            {
                children.Add(node.Clone());
            }
            return children;
        }

        /// <summary>
        /// Get the number of child nodes that this node holds.
        /// </summary>
        /// <returns>the number of child nodes that this node holds.</returns>
        public abstract int ChildNodeSize { get; }

        internal Node[] ChildNodesAsArray => EnsureChildNodes().ToArray();

        /// <summary>
        ///  Delete all this node's children.
        /// </summary>
        /// <returns>this node, for chaining</returns>
        public abstract Node Empty();

        /// <summary>
        /// Gets this node's parent node.
        /// </summary>
        /// <returns>parent node; or null if no parent.</returns>
        public virtual Node Parent => parentNode;

        /// <summary>
        /// Gets this node's parent node. Not overridable by extending classes, so useful if you really just need the Node type.
        /// </summary>
        public Node ParentNode => parentNode;

        /// <summary>
        /// Get this node's root node; that is, its topmost ancestor. If this node is the top ancestor, returns {@code this}.
        /// </summary>
        public Node Root
        {
            get
            {
                var node = this;
                while (node.parentNode != null)
                {
                    node = node.parentNode;
                }

                return node;
            }
        }

        /// <summary>
        /// Gets the Document associated with this Node.
        /// </summary>
        /// <returns>
        /// the Document associated with this Node, or null if there is no such Document.
        /// </returns>
        public Document OwnerDocument
        {
            get
            {
                Node root = Root;
                return root as Document;
            }
        }

        /// <summary>
        /// Remove (delete) this node from the DOM tree.
        /// </summary>
        /// <remarks>
        /// If this node has children, they are also removed.
        /// </remarks>
        public void Remove()
        {
            if (parentNode != null)
            {
                parentNode.RemoveChild(this);
            }
        }

        /// <summary>
        /// Insert the specified HTML into the DOM before this node (i.e. as a preceding sibling).
        /// </summary>
        /// <param name="html">HTML to add before this node</param>
        /// <returns>this node, for chaining</returns>
        /// <seealso cref="After(string)">After(string)</seealso>
        public virtual Node Before(string html)
        {
            AddSiblingHtml(SiblingIndex, html);
            return this;
        }

        /// <summary>
        /// Insert the specified node into the DOM before this node (i.e. as a preceding sibling).
        /// </summary>
        /// <param name="node">to add before this node</param>
        /// <returns>this node, for chaining</returns>
        /// <seealso cref="After(Node)">After(Node)</seealso>
        public virtual Node Before(Node node)
        {
            Validate.NotNull(node);
            Validate.NotNull(parentNode);
            if (Equals(node.parentNode, parentNode))
            {
                node.Remove();
            }
            parentNode.AddChildren(SiblingIndex, node);
            return this;
        }

        /// <summary>
        /// Insert the specified HTML into the DOM after this node (i.e. as a following sibling).
        /// </summary>
        /// <param name="html">HTML to add after this node</param>
        /// <returns>this node, for chaining</returns>
        /// <seealso cref="Before(string)">Before(string)</seealso>
        public virtual Node After(string html)
        {
            AddSiblingHtml(SiblingIndex + 1, html);
            return this;
        }

        /// <summary>
        /// Insert the specified node into the DOM after this node (i.e. as a following sibling).
        /// </summary>
        /// <param name="node">to add after this node</param>
        /// <returns>this node, for chaining</returns>
        /// <seealso cref="Before(Node)">Before(Node)</seealso>
        public virtual Node After(Node node)
        {
            Validate.NotNull(node);
            Validate.NotNull(parentNode);
            if (Equals(node.parentNode, parentNode))
            {
                node.Remove();
            }
            parentNode.AddChildren(SiblingIndex + 1, node);
            return this;
        }

        private void AddSiblingHtml(int index, string html)
        {
            Validate.NotNull(html);
            Validate.NotNull(parentNode);
            Element context = Parent as Element;
            IReadOnlyList<Node> nodes = NodeUtils.Parser(this).ParseFragmentInput(html, context, BaseUri);
            parentNode.AddChildren(index, nodes.ToArray());
        }

        /// <summary>
        /// Wrap the supplied HTML around this node.
        /// </summary>
        /// <param name="html">
        /// HTML to wrap around this element, e.g.
        /// <c><![CDATA[<div class="head"></div>]]></c>
        /// . Can be arbitrarily deep.
        /// </param>
        /// <returns>this node, for chaining.</returns>
        public Node Wrap(string html)
        {
            Validate.NotEmpty(html);
            Element context = Parent as Element ?? this as Element;
            IReadOnlyList<Node> wrapChildren = NodeUtils.Parser(this).ParseFragmentInput(html, context, BaseUri);
            Node wrapNode = wrapChildren[0];
            if (!(wrapNode is Element wrap))
            {
                // nothing to wrap with; noop
                return this;
            }
            Element deepest = GetDeepChild(wrap);
            parentNode?.ReplaceChild(this, wrap);
            deepest.AddChildren(this); // side effect of tricking wrapChildren to lose first
            // remainder (unbalanced wrap, like <div></div><p></p> -- The <p> is remainder
            if (wrapChildren.Count > 0)
            {
                for (int i = 0; i < wrapChildren.Count; i++)
                {
                    Node remainder = (Node)wrapChildren[i];
                    // if no parent, this could be the wrap node, so skip
                    if (Equals(wrap, remainder))
                        continue;

                    remainder.parentNode?.RemoveChild(remainder);
                    wrap.After(remainder);
                }
            }
            return this;
        }

        /// <summary>
        /// Removes this node from the DOM, and moves its children up into the node's parent.
        /// </summary>
        /// <remarks>
        /// This has the effect of dropping the node but keeping its children.
        /// <p/>
        /// For example, with the input html:<br/>
        /// <c><![CDATA[<div>One <span>Two <b>Three</b></span></div>]]></c>
        /// <br/>
        /// Calling
        /// <c>element.Unwrap()</c>
        /// on the
        /// <c>span</c>
        /// element will result in the html:<br/>
        /// <c><![CDATA[<div>One Two <b>Three</b></div>]]></c>
        /// <br/>
        /// and the
        /// <c>"Two "</c>
        /// <see cref="TextNode">TextNode</see>
        /// being returned.
        /// </remarks>
        /// <returns>
        /// the first child of this node, after the node has been unwrapped. Null if the node had no children.
        /// </returns>
        /// <seealso cref="Remove()">Remove()</seealso>
        /// <seealso cref="Wrap(string)">Wrap(string)</seealso>
        public Node Unwrap()
        {
            Validate.NotNull(parentNode);
            Node firstChild = FirstChild();
            parentNode.AddChildren(SiblingIndex, this.ChildNodesAsArray);
            this.Remove();
            return firstChild;
        }

        private Element GetDeepChild(Element el)
        {
            while (el.ChildrenSize > 0) {
                el = el.ChildElementsList()[0];
            }
            return el;
        }
        
        /// <summary>
        /// 
        /// </summary>
        internal virtual void NodelistChanged(){}
        

        /// <summary>
        /// Replace this node in the DOM with the supplied node.
        /// </summary>
        /// <param name="in">the node that will will replace the existing node.</param>
        public void ReplaceWith(Node @in)
        {
            Validate.NotNull(@in);
            Validate.NotNull(parentNode);
            parentNode.ReplaceChild(this, @in);
        }

        internal void SetParentNode(Node parentNode)
        {
            Validate.NotNull(parentNode);
            this.parentNode?.RemoveChild(this);
            this.parentNode = parentNode;
        }

        internal void ReplaceChild(Node @out, Supremes.Nodes.Node @in)
        {
            Validate.IsTrue(Equals(@out.parentNode, this));
            Validate.NotNull(@in);
            @in.parentNode?.RemoveChild(@in);
            int index = @out.SiblingIndex;
            EnsureChildNodes()[index] = @in;
            @in.parentNode = this;
            @in.SiblingIndex = index;
            @out.parentNode = null;
        }

        internal void RemoveChild(Node @out)
        {
            Validate.IsTrue(Equals(@out.parentNode, this));
            int index = @out.SiblingIndex;
            EnsureChildNodes().RemoveAt(index);
            ReindexChildren(index);
            @out.parentNode = null;
        }

        internal void AddChildren(params Node[] children)
        {
            List<Node> nodes = EnsureChildNodes();
            //most used. short circuit addChildren(int), which hits reindex children and array copy
            foreach (Node child in children)
            {
                Node childImpl = child;
                ReparentChild(childImpl);
                nodes.Add(childImpl);
                childImpl.SiblingIndex = nodes.Count - 1;
            }
        }

        internal void AddChildren(int index, params Node[] children)
        {
            Validate.NotNull(children);
            if (children.Length == 0) {
                return;
            }
            List<Node> nodes = EnsureChildNodes();

            // Fast path - if used as a wrap (index=0, children = child[0].parent.children - do inplace
            Node firstParent = children[0].Parent;
            if (firstParent != null && firstParent.ChildNodeSize == children.Length) {
                bool sameList = true;
                List<Node> firstParentNodes = firstParent.EnsureChildNodes();
                // Identity check contents to see if same
                int i = children.Length;
                while (i-- > 0) {
                    if (!Equals(children[i], firstParentNodes[i])) {
                        sameList = false;
                        break;
                    }
                }
                if (sameList) { // Moving, so OK to empty firstParent and short-circuit
                    bool wasEmpty = ChildNodeSize == 0;
                    firstParent.Empty();
                    nodes.InsertRange(index, children);
                    i = children.Length;
                    while (i-- > 0) {
                        children[i].parentNode = this;
                    }
                    if (!(wasEmpty && children[0].SiblingIndex == 0)) // Skip reindexing if we just moved
                        ReindexChildren(index);
                    return;
                }
            }
            
            Validate.NoNullElements(children);
            foreach (var child in children)
            {
                ReparentChild(child);
            }
            nodes.InsertRange(index, children);
            ReindexChildren(index);
        }

        internal void ReparentChild(Node child)
        {
            child.parentNode?.RemoveChild(child);
            child.SetParentNode(this);
        }

        private void ReindexChildren(int start)
        {
            int size = ChildNodeSize;
            if (size == 0) return;
            List<Node> childNodes = EnsureChildNodes();
            for (int i = start; i < size; i++)
            {
                childNodes[i].SiblingIndex = i;
            }
        }

        /// <summary>
        /// Retrieves this node's sibling nodes.
        /// </summary>
        /// <remarks>
        /// Similar to
        /// <see cref="ChildNodes">node.parent.ChildNodes</see>
        /// , but does not
        /// include this node (a node is not a sibling of itself).
        /// </remarks>
        /// <returns>node siblings. If the node has no parent, returns an empty list.</returns>
        public IReadOnlyList<Node> SiblingNodes
        {
            get
            {
                if (parentNode == null)
                {
                    return new ReadOnlyCollection<Node>(Array.Empty<Node>());
                }
                IList<Node> nodes = parentNode.EnsureChildNodes();
                List<Node> siblings = new List<Node>(nodes.Count - 1);
                foreach (Node node in nodes)
                {
                    if (!Equals(node, this))
                    {
                        siblings.Add(node);
                    }
                }
                return siblings.AsReadOnly();
            }
        }

        /// <summary>
        /// Get this node's next sibling.
        /// </summary>
        /// <returns>next sibling, or null if this is the last sibling</returns>
        public Node NextSibling
        {
            get
            {
                if (parentNode == null)
                {
                    return null; // root
                }
                IList<Node> siblings = parentNode.EnsureChildNodes();
                int index = SiblingIndex + 1;
                Validate.NotNull(index);
                return siblings.Count > index ? siblings[index] : null;
            }
        }

        /// <summary>
        /// Get this node's previous sibling.
        /// </summary>
        /// <returns>the previous sibling, or null if this is the first sibling</returns>
        public Node PreviousSibling
        {
            get
            {
                if (parentNode == null)
                {
                    return null; // root
                }
                int index = SiblingIndex;
                return index > 0 ? parentNode.EnsureChildNodes()[index - 1] : null;
            }
        }

        /// <summary>
        /// Get the list index of this node in its node sibling list.
        /// </summary>
        /// <remarks>
        /// I.e. if this is the first node sibling, returns 0.
        /// </remarks>
        /// <returns>position in node sibling list</returns>
        /// <seealso cref="Element.ElementSiblingIndex">Element.ElementSiblingIndex</seealso>
        public int SiblingIndex { get; internal set; }

        /// <summary>
        /// Gets the first child node of this node, or {@code null} if there is none. This could be any Node type, such as an
        /// Element, TextNode, Comment, etc. Use {@link Element#firstElementChild()} to get the first Element child.
        /// </summary>
        /// <returns>the first child node, or null if there are no children.</returns>
        public Node FirstChild() {
            if (ChildNodeSize == 0) return null;
            return EnsureChildNodes()[0];
        }
        
        /// <summary>
        /// Gets the last child node of this node, or {@code null} if there is none.
        /// </summary>
        /// <returns>the last child node, or null if there are no children.</returns>
        public Node LastChild() {
            int size = ChildNodeSize;
            if (size == 0) return null;
            List<Node> children = EnsureChildNodes();
            return children[size - 1];
        }


        /// <summary>
        /// Perform a depth-first traversal through this node and its descendants.
        /// </summary>
        /// <param name="nodeVisitor">the visitor callbacks to perform on each node</param>
        /// <returns>this node, for chaining</returns>
        internal Node Traverse(INodeVisitor nodeVisitor)
        {
            Validate.NotNull(nodeVisitor);
            NodeTraversor.Traverse(nodeVisitor, this);
            return this;
        }

        /// <summary>
        /// Perform the supplied action on this Node and each of its descendants, during a depth-first traversal. Nodes may be
        /// inspected, changed, added, replaced, or removed.
        /// </summary>
        /// <param name="action">action the function to perform on the node</param>
        /// <returns>this Node, for chaining</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Node ForEachNode(Action<Node> action) {
            if (action == null) throw new ArgumentNullException(nameof(action));
            NodeTraversor.Traverse(new LambdaNodeVisitor((node, depth) => action(node)), this);
            return this;
        }
        
        /// <summary>
        /// Perform a depth-first filtering through this node and its descendants.
        /// </summary>
        /// <param name="nodeFilter">the filter callbacks to perform on each node</param>
        /// <returns>this node, for chaining</returns>
        public Node Filter(NodeFilter nodeFilter) {
            Validate.NotNull(nodeFilter);
            NodeTraversor.Filter(nodeFilter, this);
            return this;
        }

        
        /// <summary>
        /// Get the outer HTML of this node.
        /// </summary>
        /// <returns>HTML</returns>
        public virtual string OuterHtml
        {
            get
            {
                StringBuilder accum = StringUtil.BorrowBuilder();
                AppendOuterHtmlTo(accum);
                return StringUtil.ReleaseBuilder(accum);
            }
        }

        internal void AppendOuterHtmlTo(StringBuilder accum)
        {
            NodeTraversor.Traverse(new OuterHtmlVisitor(accum, NodeUtils.OutputSettings(this)), this);
        }

        internal DocumentOutputSettings GetOutputSettings()
        {
            // if this node has no document (or parent), retrieve the default output settings
            return OwnerDocument != null
                ? OwnerDocument.OutputSettings
                : (new Document(string.Empty)).OutputSettings;
        }

        /// <summary>
        /// Get the outer HTML of this node.
        /// </summary>
        /// <param name="accum">accumulator to place HTML into</param>
        /// <param name="depth"></param>
        /// <param name="out"></param>
        internal abstract void AppendOuterHtmlHeadTo(StringBuilder accum, int depth, DocumentOutputSettings @out);

        internal abstract void AppendOuterHtmlTailTo(StringBuilder accum, int depth, DocumentOutputSettings @out);

        /// <summary>
        /// Get the source range (start and end positions) in the original input source that this node was parsed from. Position
        /// tracking must be enabled prior to parsing the content. For an Element, this will be the positions of the start tag.
        /// </summary>
        /// <returns>the range for the start of the node.</returns>
        public Range SourceRange() {
            return Range.Of(this, true);
        }
        
        /// <summary>
        /// Test if this node is not null and has the supplied normal name.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="normalName"></param>
        /// <returns></returns>
        internal static bool IsNode(Node node, string normalName) {
            return node != null && node.NormalName.Equals(normalName);
        }

        /// <summary>
        /// Test if this node has the supplied normal name.
        /// </summary>
        /// <param name="normalName"></param>
        /// <returns></returns>
        public bool IsNode(string normalName) {
            return NormalName.Equals(normalName);
        }

        /// <summary>
        /// Test if this node is the first child, or first following blank text.
        /// </summary>
        /// <returns></returns>
        public bool IsEffectivelyFirst() {
            if (siblingIndex == 0) return true;
            if (siblingIndex == 1) {
                Node prev = PreviousSibling;
                return prev is TextNode { IsBlank: true };
            }
            return false;
        }
        
        /// <summary>
        /// Converts the value of this instance to a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return OuterHtml;
        }

        internal void Indent(StringBuilder accum, int depth, DocumentOutputSettings @out)
        {
            accum.Append("\n").Append(StringUtil.Padding(depth * @out.IndentAmount, @out.MaxPaddingWidth));
        }

        /// <summary>
        /// Compares two <see cref="Node"/> instances for equality.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return this == obj;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        /// <summary>
        /// Check if this node has the same content as another node. A node is considered the same if its name, attributes and content match the
        /// other node; particularly its position in the tree does not influence its similarity.
        /// </summary>
        /// <param name="o">other object to compare to</param>
        /// <returns>true if the content of this node is the same as the other</returns>
        public bool HasSameValue(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            return this.OuterHtml.Equals(((Node)o).OuterHtml);
        }


        /// <summary>
        /// Create a stand-alone, deep copy of this node, and all of its children.
        /// </summary>
        /// <remarks>
        /// The cloned node will have no siblings or parent node.
        /// As a stand-alone object, any changes made to the clone or any of its children
        /// will not impact the original node.
        /// <p/>
        /// The cloned node may be adopted into another Document or node structure using
        /// <see cref="Element.AppendChild(Node)">Element.AppendChild(Node)</see>
        /// .
        /// </remarks>
        /// <returns>stand-alone cloned node</returns>
        public virtual Node Clone()
        {
            Node thisClone = DoClone(null);
            // splits for orphan
            // Queue up nodes that need their children cloned (BFS).
            LinkedList<Node> nodesToProcess = new LinkedList<Node>();
            nodesToProcess.AddLast(thisClone);
            while (nodesToProcess.Count > 0)
            {
                Node currParent = nodesToProcess.First.Value;
                nodesToProcess.RemoveFirst();
                for (int i = 0; i < currParent.ChildNodeSize; i++)
                {
                    List<Node> childNodes = currParent.EnsureChildNodes();
                    Node childClone = childNodes[i].DoClone(currParent);
                    childNodes[i] = childClone;
                    nodesToProcess.AddLast(childClone);
                }
            }
            return thisClone;
        }
        
        /// <summary>
        /// Create a stand-alone, shallow copy of this node. None of its children (if any) will be cloned, and it will have
        /// no parent or sibling nodes.
        /// </summary>
        /// <returns>a single independent copy of this node</returns>
        public virtual Node ShallowClone() {
            return DoClone(null);
        }

        internal virtual Node DoClone(Node parent)
        {
            Node clone = (Node)this.MemberwiseClone();
            clone.parentNode = parent;
            // can be null, to create an orphan split
            clone.SiblingIndex = parent == null ? 0 : SiblingIndex;
            if (parent == null && this is not Document)
            {
                var doc = OwnerDocument;
                if (doc != null)
                {
                    var docClone = doc.ShallowClone();
                    clone.parentNode = docClone;
                    docClone.EnsureChildNodes().Add(clone);
                }
            }
            return clone;
        }

        private class OuterHtmlVisitor : INodeVisitor
        {
            private StringBuilder accum;

            private DocumentOutputSettings @out;

            internal OuterHtmlVisitor(StringBuilder accum, DocumentOutputSettings @out)
            {
                this.accum = accum;
                this.@out = @out;
                @out.PrepareEncoder();
            }

            public void Head(Node node, int depth)
            {
                node.AppendOuterHtmlHeadTo(accum, depth, @out);
            }

            public void Tail(Node node, int depth)
            {
                if (!node.NodeName.Equals("#text"))
                {
                    // saves a void hit.
                    node.AppendOuterHtmlTailTo(accum, depth, @out);
                }
            }
        }
    }
}
