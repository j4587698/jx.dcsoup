using System;
using Supremes.Helper;
using Supremes.Select;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Supremes.Nodes
{
    /// <summary>
    /// A list of
    /// <see cref="Element">Element</see>
    /// s, with methods that act on every element in the list.
    /// <p/>
    /// To get an
    /// <c>Elements</c>
    /// object, use the
    /// <see cref="Element.Select(string)">Element.Select(string)</see>
    /// method.
    /// </summary>
    /// <author>Jonathan Hedley, jonathan@hedley.net</author>
    public sealed class Elements : List<Element>, ICollection<Element>
    {
        public Elements()
        {
        }

        public Elements(int initialCapacity) : base(initialCapacity)
        {
        }

        public Elements(ICollection<Element> elements) : base(elements)
        {
        }

        public Elements(IList<Element> elements) : base(elements)
        {
        }

        public Elements(params Element[] elements) : this((IList<Element>)elements)
        {
        }

        /// <summary>
        /// Creates a deep copy of these elements.
        /// </summary>
        /// <returns>a deep copy</returns>
        internal Elements Clone()
        {
            Elements elements = new Elements(Count);
            foreach (Element e in this)
            {
            	elements.Add((Element)e.Clone());
            }
            return elements;
        }

        // attribute methods

        /// <summary>
        /// Get an attribute value from the first matched element that has the attribute.
        /// </summary>
        /// <param name="attributeKey">The attribute key.</param>
        /// <returns>
        /// The attribute value from the first matched element that has the attribute.
        /// If no elements were matched (isEmpty() == true),
        /// or if the no elements have the attribute, returns empty string.
        /// </returns>
        /// <seealso cref="HasAttr(string)">HasAttr(string)</seealso>
        public string Attr(string attributeKey)
        {
            foreach (Element element in this)
            {
                if (element.HasAttr(attributeKey))
                {
                    return element.Attr(attributeKey);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if any of the matched elements have this attribute set.
        /// </summary>
        /// <param name="attributeKey">attribute key</param>
        /// <returns>true if any of the elements have the attribute; false if none do.</returns>
        public bool HasAttr(string attributeKey)
        {
            return this.Any(element => element.HasAttr(attributeKey));
        }
        
        /// <summary>
        /// Get the attribute value for each of the matched elements. If an element does not have this attribute, no value is
        /// included in the result set for that element.
        /// </summary>
        /// <param name="attributeKey">the attribute name to return values for. You can add the {@code abs:} prefix to the key to
        /// get absolute URLs from relative URLs, e.g.: {@code doc.select("a").eachAttr("abs:href")} .</param>
        /// <returns>a list of each element's attribute value for the attribute</returns>
        public List<string> EachAttr(string attributeKey) {
            List<string> attrs = new List<string>(Count);
            attrs.AddRange(from element in this where element.HasAttr(attributeKey) select element.Attr(attributeKey));
            return attrs;
        }

        /// <summary>
        /// Set an attribute on all matched elements.
        /// </summary>
        /// <param name="attributeKey">attribute key</param>
        /// <param name="attributeValue">attribute value</param>
        /// <returns>this</returns>
        public Elements Attr(string attributeKey, string attributeValue)
        {
            foreach (Element element in this)
            {
                element.Attr(attributeKey, attributeValue);
            }
            return this;
        }

        /// <summary>
        /// Remove an attribute from every matched element.
        /// </summary>
        /// <param name="attributeKey">The attribute to remove.</param>
        /// <returns>this (for chaining)</returns>
        public Elements RemoveAttr(string attributeKey)
        {
            foreach (Element element in this)
            {
                element.RemoveAttr(attributeKey);
            }
            return this;
        }

        /// <summary>
        /// Add the class name to every matched element's
        /// <c>class</c>
        /// attribute.
        /// </summary>
        /// <param name="className">class name to add</param>
        /// <returns>this</returns>
        public Elements AddClass(string className)
        {
            foreach (Element element in this)
            {
                element.AddClass(className);
            }
            return this;
        }

        /// <summary>
        /// Remove the class name from every matched element's
        /// <c>class</c>
        /// attribute, if present.
        /// </summary>
        /// <param name="className">class name to remove</param>
        /// <returns>this</returns>
        public Elements RemoveClass(string className)
        {
            foreach (Element element in this)
            {
                element.RemoveClass(className);
            }
            return this;
        }

        /// <summary>
        /// Toggle the class name on every matched element's
        /// <c>class</c>
        /// attribute.
        /// </summary>
        /// <param name="className">class name to add if missing, or remove if present, from every element.
        /// </param>
        /// <returns>this</returns>
        public Elements ToggleClass(string className)
        {
            foreach (Element element in this)
            {
                element.ToggleClass(className);
            }
            return this;
        }

        /// <summary>
        /// Determine if any of the matched elements have this class name set in their
        /// <c>class</c>
        /// attribute.
        /// </summary>
        /// <param name="className">class name to check for</param>
        /// <returns>true if any do, false if none do</returns>
        public bool HasClass(string className)
        {
            return this.Any(element => element.HasClass(className));
        }

        /// <summary>
        /// Get the form element's value of the first matched element.
        /// Set the form element's value in each of the matched elements.
        /// </summary>
        /// <remarks>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>The value to set into each matched element</value>
        /// <returns>The form element's value, or empty if not set.</returns>
        /// <seealso cref="Element.Val">Element.Val</seealso>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string Val
        {
            get
            {
                if (Count > 0)
                {
                    return First.Val;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                foreach (Element element in this)
                {
                    element.Val = value;
                }
            }
        }

        /// <summary>
        /// Get the combined text of all the matched elements.
        /// </summary>
        /// <remarks>
        /// Note that it is possible to get repeats if the matched elements contain both parent elements and their own
        /// children, as the Element.text() method returns the combined text of a parent and all its children.
        /// </remarks>
        /// <returns>string of all text: unescaped and no HTML.</returns>
        /// <seealso cref="Element.Text">Element.Text</seealso>
        public string Text
        {
            get
            {
                StringBuilder sb = StringUtil.BorrowBuilder();
                foreach (Element element in this)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(element.Text);
                }
                return StringUtil.ReleaseBuilder(sb);
            }
        }

        /// <summary>
        /// Determine if any of the matched elements have a text content (that is not just whitespace).
        /// </summary>
        /// <returns></returns>
        public bool HasText => this.Any(element => element.HasText);

        /// <summary>
        /// Get the text content of each of the matched elements. If an element has no text, then it is not included in the
        /// result.
        /// </summary>
        public IEnumerable<string> EachText => this.Where(x => x.HasText).Select(x => x.Text);

        /// <summary>
        /// Get the combined inner HTML of all matched elements.
        /// Set the inner HTML of each matched element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>HTML to parse and set into each matched element.</value>
        /// <returns>string of all element's inner HTML.</returns>
        /// <seealso cref="Text">Text</seealso>
        /// <seealso cref="OuterHtml">OuterHtml</seealso>
        /// <seealso cref="Element.Html">Element.Html</seealso>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string Html
        {
            get
            {
                StringBuilder sb = StringUtil.BorrowBuilder();
                foreach (Element element in this)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append("\n");
                    }
                    sb.Append(element.Html);
                }
                return StringUtil.ReleaseBuilder(sb);
            }
            set
            {
                foreach (Element element in this)
                {
                    element.Html = value;
                }
            }
        }

        /// <summary>
        /// Get the combined outer HTML of all matched elements.
        /// </summary>
        /// <returns>string of all element's outer HTML.</returns>
        /// <seealso cref="Text">Text</seealso>
        /// <seealso cref="Html">Html</seealso>
        public string OuterHtml
        {
            get
            {
                StringBuilder sb = StringUtil.BorrowBuilder();
                foreach (Element element in this)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append("\n");
                    }
                    sb.Append(element.OuterHtml);
                }
                return StringUtil.ReleaseBuilder(sb);
            }
        }

        /// <summary>
        /// Get the combined outer HTML of all matched elements.
        /// </summary>
        /// <remarks>
        /// Alias of
        /// <see cref="OuterHtml">OuterHtml</see>
        /// .
        /// </remarks>
        /// <returns>string of all element's outer HTML.</returns>
        /// <seealso cref="Text">Text</seealso>
        /// <seealso cref="Html">Html</seealso>
        public override string ToString()
        {
            return OuterHtml;
        }

        /// <summary>
        /// Get the tag name of the first matched element.
        /// Set the tag name of each matched element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For example, to change each
        /// <c>&lt;i&gt;</c>
        /// to a
        /// <c>&lt;em&gt;</c>
        /// , do
        /// <c>doc.Select("i").TagName = "em";</c>
        /// </para>
        /// <para>
        /// if you want to use fluent API, write <c>using Supremes.Fluent;</c>.
        /// </para>
        /// </remarks>
        /// <value>the new tag name</value>
        /// <returns>The tag name of the first matched element, or empty if not matched.</returns>
        /// <seealso cref="Element.TagName">Element.TagName</seealso>
        /// <seealso cref="Supremes.Fluent.FluentUtility">Supremes.Fluent.FluentUtility</seealso>
        public string TagName
        {
            get
            {
                if (Count > 0)
                {
                    return First.TagName;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                foreach (Element element in this)
                {
                    element.TagName = value;
                }
            }
        }

        /// <summary>
        /// Add the supplied HTML to the start of each matched element's inner HTML.
        /// </summary>
        /// <param name="html">HTML to add inside each element, before the existing HTML</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Prepend(string)">Element.Prepend(string)</seealso>
        public Elements Prepend(string html)
        {
            foreach (Element element in this)
            {
                element.Prepend(html);
            }
            return this;
        }

        /// <summary>
        /// Add the supplied HTML to the end of each matched element's inner HTML.
        /// </summary>
        /// <param name="html">HTML to add inside each element, after the existing HTML</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Append(string)">Element.Append(string)</seealso>
        public Elements Append(string html)
        {
            foreach (Element element in this)
            {
                element.Append(html);
            }
            return this;
        }

        /// <summary>
        /// Insert the supplied HTML before each matched element's outer HTML.
        /// </summary>
        /// <param name="html">HTML to insert before each element</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Node.Before(string)">Node.Before(string)</seealso>
        public Elements Before(string html)
        {
            foreach (Element element in this)
            {
                element.Before(html);
            }
            return this;
        }

        /// <summary>
        /// Insert the supplied HTML after each matched element's outer HTML.
        /// </summary>
        /// <param name="html">HTML to insert after each element</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Node.After(string)">Node.After(string)</seealso>
        public Elements After(string html)
        {
            foreach (Element element in this)
            {
                element.After(html);
            }
            return this;
        }

        /// <summary>
        /// Wrap the supplied HTML around each matched elements.
        /// </summary>
        /// <remarks>
        /// For example, with HTML
        /// <c><![CDATA[<p><b>This</b> is <b>Nsoup</b></p>]]></c>
        /// ,
        /// <c>doc.Select("b").Wrap("&lt;i&gt;&lt;/i&gt;");</c>
        /// becomes
        /// <c><![CDATA[<p><i><b>This</b></i> is <i><b>jsoup</b></i></p>]]></c>
        /// </remarks>
        /// <param name="html">
        /// HTML to wrap around each element, e.g.
        /// <c><![CDATA[<div class="head"></div>]]></c>
        /// . Can be arbitrarily deep.
        /// </param>
        /// <returns>this (for chaining)</returns>
        /// <seealso cref="Element.Wrap(string)">Element.Wrap(string)</seealso>
        public Elements Wrap(string html)
        {
            Validate.NotEmpty(html);
            foreach (Element element in this)
            {
                element.Wrap(html);
            }
            return this;
        }

        /// <summary>
        /// Removes the matched elements from the DOM, and moves their children up into their parents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This has the effect of dropping the elements but keeping their children.
        /// </para>
        /// <para>
        /// This is useful for e.g removing unwanted formatting elements but keeping their contents.
        /// </para>
        /// <para>
        /// E.g. with HTML:
        /// <c>&lt;div&gt;&lt;font&gt;One&lt;/font&gt; &lt;font&gt;&lt;a href="/"&gt;Two&lt;/a&gt;&lt;/font&gt;&lt;/div&gt;</c>
        /// <br/>
        /// <c>doc.Select("font").Unwrap();</c>
        /// <br/>
        /// HTML =
        /// <c>&lt;div&gt;One &lt;a href="/"&gt;Two&lt;/a&gt;&lt;/div&gt;</c>
        /// </para>
        /// </remarks>
        /// <returns>this (for chaining)</returns>
        /// <seealso cref="Node.Unwrap()">Node.Unwrap()</seealso>
        public Elements Unwrap()
        {
            foreach (Element element in this)
            {
                element.Unwrap();
            }
            return this;
        }

        /// <summary>
        /// Empty (remove all child nodes from) each matched element.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is similar to setting the inner HTML of each element to nothing.
        /// </para>
        /// <para>
        /// E.g. HTML:
        /// <c><![CDATA[<div><p>Hello <b>there</b></p> <p>now</p></div>]]></c>
        /// <br />
        /// <c>doc.Select("p").Empty();</c><br />
        /// HTML =
        /// <c><![CDATA[<div><p></p> <p></p></div>]]></c>
        /// </para>
        /// </remarks>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Empty()">Element.Empty()</seealso>
        /// <seealso cref="Remove()">Remove()</seealso>
        public Elements Empty()
        {
            foreach (Element element in this)
            {
                element.Empty();
            }
            return this;
        }

        /// <summary>
        /// Remove each matched element from the DOM.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is similar to setting the outer HTML of each element to nothing.
        /// </para>
        /// <para>
        /// E.g. HTML:
        /// <c><![CDATA[<div><p>Hello</p> <p>there</p> <img /></div>]]></c>
        /// <br />
        /// <c>doc.Select("p").Remove();</c><br />
        /// HTML =
        /// <c><![CDATA[<div> <img /></div>]]></c>
        /// </para>
        /// <para>
        /// Note that this method should not be used to clean user-submitted HTML; rather, use
        /// <see cref="Supremes.Safety.Cleaner">Supremes.Safety.Cleaner</see>
        /// to clean HTML.
        /// </para>
        /// </remarks>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Empty()">Element.Empty()</seealso>
        /// <seealso cref="Empty()">Empty()</seealso>
        public Elements Remove()
        {
            foreach (Element element in this)
            {
                element.Remove();
            }
            return this;
        }

        // filters

        /// <summary>
        /// Find matching elements within this element list.
        /// </summary>
        /// <param name="query">
        /// A
        /// <see cref="Supremes.Select.Selector">Supremes.Select.Selector</see>
        /// query
        /// </param>
        /// <returns>the filtered list of elements, or an empty list if none match.</returns>
        public Elements Select(string query)
        {
            return Selector.Select(query, this);
        }

        /// <summary>
        /// Remove elements from this list that match the
        /// <see cref="Supremes.Select.Selector">Supremes.Select.Selector</see>
        /// query.
        /// </summary>
        /// <remarks>
        /// <para>
        /// E.g. HTML:
        /// <c>&lt;div class=logo&gt;One&lt;/div&gt; &lt;div&gt;Two&lt;/div&gt;</c>
        /// <br />
        /// <c>Elements divs = doc.select("div").not("#logo");</c><br />
        /// Result:
        /// <c>divs: [<div>Two</div>]</c>
        /// </para>
        /// </remarks>
        /// <param name="query">the selector query whose results should be removed from these elements
        /// </param>
        /// <returns>a new elements list that contains only the filtered results</returns>
        public Elements Not(string query)
        {
            Elements @out = Selector.Select(query, this);
            return Selector.FilterOut(this, @out);
            // exclude set. package open so that Elements can implement .not() selector.
        }

        /// <summary>
        /// Get the <i>nth</i> matched element as an Elements object.
        /// </summary>
        /// <remarks>
        /// See also
        /// <see>this[int]</see>
        /// to retrieve an Element.
        /// </remarks>
        /// <param name="index">the (zero-based) index of the element in the list to retain</param>
        /// <returns>
        /// Elements containing only the specified element, or, if that element did not exist, an empty list.
        /// </returns>
        public Elements Eq(int index)
        {
            return this.Count > index
                ? new Elements(this[index])
                : new Elements();
        }

        /// <summary>
        /// Test if any of the matched elements match the supplied query.
        /// </summary>
        /// <param name="query">A selector</param>
        /// <returns>true if at least one element in the list matches the query.</returns>
        public bool Is(string query)
        {
            Evaluator evaluator = QueryParser.Parse(query);
            return this.Any(element => element.Is(evaluator));
        }

        /// <summary>
        /// Get the immediate next element sibling of each element in this list.
        /// </summary>
        /// <returns>next element siblings.</returns>
        public Elements Next()
        {
            return Siblings(null, true, false);
        }

        /// <summary>
        /// Get the immediate next element sibling of each element in this list, filtered by the query.
        /// </summary>
        /// <param name="query">CSS query to match siblings against</param>
        /// <returns>next element siblings.</returns>
        public Elements Next(string query)
        {
            return Siblings(query, true, false);
        }
        
        /// <summary>
        /// Get each of the following element siblings of each element in this list.
        /// </summary>
        /// <returns>all following element siblings.</returns>
        public Elements NextAll()
        {
            return Siblings(null, true, true);
        }
        
        /// <summary>
        /// Get each of the following element siblings of each element in this list, that match the query.
        /// </summary>
        /// <param name="query">CSS query to match siblings against</param>
        /// <returns>all following element siblings.</returns>
        public Elements NextAll(string query)
        {
            return Siblings(query, true, true);
        }

        /// <summary>
        /// Get the immediate previous element sibling of each element in this list.
        /// </summary>
        /// <returns>previous element siblings.</returns>
        public Elements Prev()
        {
            return Siblings(null, false, false);
        }

        /// <summary>
        /// Get the immediate previous element sibling of each element in this list, filtered by the query.
        /// </summary>
        /// <param name="query">CSS query to match siblings against</param>
        /// <returns>previous element siblings.</returns>
        public Elements Prev(string query)
        {
            return Siblings(query, false, false);
        }

        /// <summary>
        /// Get each of the previous element siblings of each element in this list.
        /// </summary>
        /// <returns>all previous element siblings.</returns>
        public Elements PrevAll()
        {
            return Siblings(null, false, true);
        }
        
        /// <summary>
        /// Get each of the previous element siblings of each element in this list, that match the query.
        /// </summary>
        /// <param name="query">CSS query to match siblings against</param>
        /// <returns>all previous element siblings.</returns>
        public Elements PrevAll(string query)
        {
            return Siblings(query, false, true);
        }
        
        private Elements Siblings(string query, bool next, bool all) {
            Elements els = new Elements();
            Evaluator eval = query != null? QueryParser.Parse(query) : null;
            foreach (Element e in this)
            {
                var a = e;
                do {
                    Element sib = next ? a.NextElementSibling : a.PreviousElementSibling;
                    if (sib == null) break;
                    if (eval == null)
                        els.Add(sib);
                    else if (sib.Is(eval))
                        els.Add(sib);
                    a = sib;
                } while (all);
            }
            return els;
        }

        /// <summary>
        /// Get all of the parents and ancestor elements of the matched elements.
        /// </summary>
        /// <returns>all of the parents and ancestor elements of the matched elements</returns>
        public Elements Parents
        {
            get
            {
                LinkedHashSet<Element> combo = new LinkedHashSet<Element>();
                foreach (Element e in this)
                {
                    combo.AddRange(e.Parents);
                }
                return new Elements(combo);
            }
        }

        // list-like methods

        /// <summary>
        /// Get the first matched element.
        /// </summary>
        /// <returns>The first matched element, or <c>null</c> if contents is empty.</returns>
        public Element First => Count == 0 ? null : this[0];

        /// <summary>
        /// Get the last matched element.
        /// </summary>
        /// <returns>The last matched element, or <c>null</c> if contents is empty.</returns>
        public Element Last => this.Count == 0 ? null : this[Count - 1];

        /// <summary>
        /// Perform a depth-first traversal on each of the selected elements.
        /// </summary>
        /// <param name="nodeVisitor">the visitor callbacks to perform on each node</param>
        /// <returns>this, for chaining</returns>
        internal Elements Traverse(INodeVisitor nodeVisitor)
        {
            Validate.NotNull(nodeVisitor);
            NodeTraversor.Traverse(nodeVisitor, this);
            return this;
        }
        
        /// <summary>
        /// Perform a depth-first filtering on each of the selected elements.
        /// </summary>
        /// <param name="nodeFilter">the filter callbacks to perform on each node</param>
        /// <returns>this, for chaining</returns>
        public Elements Filter(NodeFilter nodeFilter) {
            NodeTraversor.Filter(nodeFilter, this);
            return this;
        }

        /// <summary>
        /// Get the
        /// <see cref="FormElement">FormElement</see>
        /// forms from the selected elements, if any.
        /// </summary>
        /// <returns>
        /// a list of
        /// <see cref="FormElement">FormElement</see>
        /// s pulled from the matched elements. The list will be empty if the elements contain
        /// no forms.
        /// </returns>
        public IReadOnlyList<FormElement> Forms
        {
            get
            {
                List<FormElement> forms = this.OfType<FormElement>().ToList();
                return forms.AsReadOnly();
            }
        }
        
        /// <summary>
        /// Get {@link Comment} nodes that are direct child nodes of the selected elements.
        /// </summary>
        /// <returns>Comment nodes, or an empty list if none.</returns>
        public List<Comment> Comments() {
            return ChildNodesOfType<Comment>();
        }
        
        /// <summary>
        /// Get {@link TextNode} nodes that are direct child nodes of the selected elements.
        /// </summary>
        /// <returns>TextNode nodes, or an empty list if none.</returns>
        public List<TextNode> TextNodes() {
            return ChildNodesOfType<TextNode>();
        }
        
        /// <summary>
        /// Get {@link DataNode} nodes that are direct child nodes of the selected elements. DataNode nodes contain the
        /// content of tags such as {@code script}, {@code style} etc and are distinct from {@link TextNode}s.
        /// </summary>
        /// <returns>Comment nodes, or an empty list if none.</returns>
        public List<DataNode> DataNodes() {
            return ChildNodesOfType<DataNode>();
        }
        
        private List<T> ChildNodesOfType<T>() where T : Node
        {
            List<T> nodes = new List<T>();
            foreach (Element el in this)
            {
                for (int i = 0; i < el.ChildNodes().Count; i++)
                {
                    Node node = el.ChildNodes()[i];
                    if (node is T node1)
                        nodes.Add(node1);
                }
            }
            return nodes;
        }


        // implements List<Element> delegates:
        
        
        #region collection interface

        bool ICollection<Element>.IsReadOnly => false;
        

        #endregion
    }
}
