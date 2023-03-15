using Supremes.Nodes;

namespace Supremes.Select
{
    public interface NodeFilter
    {
     /// <summary>
     ///  Filter decision.
     /// </summary>
        enum FilterResult
        {
            /** Continue processing the tree */
            Continue,

            /** Skip the child nodes, but do call {@link NodeFilter#tail(Node, int)} next. */
            SkipChildren,

            /** Skip the subtree, and do not call {@link NodeFilter#tail(Node, int)}. */
            SkipEntirely,

            /** Remove the node and its children */
            Remove,

            /** Stop processing */
            Stop
        }

        /**
     * Callback for when a node is first visited.
     * @param node the node being visited.
     * @param depth the depth of the node, relative to the root node. E.g., the root node has depth 0, and a child node of that will have depth 1.
     * @return Filter decision
     */
        FilterResult Head(Node node, int depth);

        /// <summary>
        /// Callback for when a node is last visited, after all of its descendants have been visited.
        /// <p>This method has a default implementation to return {@link FilterResult#CONTINUE}.</p>
        /// @param node the node being visited.
        /// @param depth the depth of the node, relative to the root node. E.g., the root node has depth 0, and a child node of that will have depth 1.
        /// @return Filter decision
        /// </summary> 
        FilterResult Tail(Node node, int depth)
        {
            return FilterResult.Continue;
        }
    }
}