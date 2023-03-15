using System.Diagnostics;
using Supremes.Helper;
using Supremes.Nodes;

namespace Supremes.Select
{
    /// <summary>
    /// Depth-first node traversor.
    /// </summary>
    /// <remarks>
    /// Depth-first node traversor. Use to iterate through all nodes under and including the specified root node.
    /// <p/>
    /// This implementation does not use recursion, so a deep DOM does not risk blowing the stack.
    /// </remarks>
    internal class NodeTraversor
    {
       public static void Traverse(INodeVisitor visitor, Node root)
        {
            Validate.NotNull(visitor);
            Validate.NotNull(root);
            Node node = root;
            int depth = 0;

            while (node != null)
            {
                Node parent = node.parentNode; // remember parent to find nodes that get replaced in .head
                int origSize = parent?.ChildNodeSize ?? 0;
                Node next = node.NextSibling;

                visitor.Head(node, depth); // visit current node
                if (parent != null && !node.HasParent()) // removed or replaced
                {
                    if (origSize == parent.ChildNodeSize) // replaced
                    {
                        node = parent.ChildNode(node.SiblingIndex); // replace ditches parent but keeps sibling index
                    }
                    else // removed
                    {
                        node = next;
                        if (node == null) // last one, go up
                        {
                            node = parent;
                            depth--;
                        }
                        continue; // don't tail removed
                    }
                }

                if (node.ChildNodeSize > 0) // descend
                {
                    node = node.ChildNode(0);
                    depth++;
                }
                else
                {
                    while (true)
                    {
                        Debug.Assert(node != null); // as depth > 0, will have parent
                        if (!(node.NextSibling == null && depth > 0)) break;
                        visitor.Tail(node, depth); // when no more siblings, ascend
                        node = node.parentNode;
                        depth--;
                    }
                    visitor.Tail(node, depth);
                    if (node == root)
                        break;
                    node = node.NextSibling;
                }
            }
        }

        public static void Traverse(INodeVisitor visitor, Elements elements)
        {
            Validate.NotNull(visitor);
            Validate.NotNull(elements);
            foreach (Element el in elements)
                Traverse(visitor, el);
        }

        public static NodeFilter.FilterResult Filter(NodeFilter filter, Node root)
        {
            Node node = root;
            int depth = 0;

            while (node != null)
            {
                NodeFilter.FilterResult result = filter.Head(node, depth);
                if (result == NodeFilter.FilterResult.Stop)
                    return result;
                // Descend into child nodes:
                if (result == NodeFilter.FilterResult.Continue && node.ChildNodeSize > 0)
                {
                    node = node.ChildNode(0);
                    ++depth;
                    continue;
                }
                // No siblings, move upwards:
                while (true)
                {
                    Debug.Assert(node != null); // depth > 0, so has parent
                    if (!(node.NextSibling == null && depth > 0)) break;
                    // 'tail' current node:
                    if (result == NodeFilter.FilterResult.Continue || result == NodeFilter.FilterResult.SkipChildren)
                    {
                        result = filter.Tail(node, depth);
                        if (result == NodeFilter.FilterResult.Stop)
                            return result;
                    }
                    Node prev = node; // In case we need to remove it below.
                    node = node.parentNode;
                    depth--;
                    if (result == NodeFilter.FilterResult.Remove)
                        prev.Remove(); // Remove AFTER finding parent.
                    result = NodeFilter.FilterResult.Continue; // Parent was not pruned.
                }
                // 'tail' current node, then proceed with siblings:
                if (result == NodeFilter.FilterResult.Continue || result == NodeFilter.FilterResult.SkipChildren)
                {
                    result = filter.Tail(node, depth);
                    if (result == NodeFilter.FilterResult.Stop)
                        return result;
                }
                if (node == root)
                    return result;
                Node prev1 = node; // In case we need to remove it below.
                node = node.NextSibling;
                if (result == NodeFilter.FilterResult.Remove)
                    prev1.Remove(); // Remove AFTER finding sibling.
            }
            // root == null?
            return NodeFilter.FilterResult.Continue;
        }

        public static void Filter(NodeFilter filter, Elements elements)
        {
            Validate.NotNull(filter);
            Validate.NotNull(elements);
            foreach (Element el in elements)
                if (Filter(filter, el) == NodeFilter.FilterResult.Stop)
                    break;
        }
    }
}
