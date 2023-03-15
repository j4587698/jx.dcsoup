﻿using System;
using Supremes.Nodes;

namespace Supremes.Select
{
    /// <summary>
    /// Node visitor interface.
    /// </summary>
    /// <remarks>
    /// Provide an implementing class to
    /// <see cref="NodeTraversor">NodeTraversor</see>
    /// to iterate through nodes.
    /// <p/>
    /// This interface provides two methods,
    /// <c>head</c>
    /// and
    /// <c>tail</c>
    /// . The head method is called when the node is first
    /// seen, and the tail method when all of the node's children have been visited. As an example, head can be used to
    /// create a start tag for a node, and tail to create the end tag.
    /// </remarks>
    internal interface INodeVisitor
    {
        /// <summary>
        /// Callback for when a node is first visited.
        /// </summary>
        /// <param name="node">the node being visited.</param>
        /// <param name="depth">
        /// the depth of the node, relative to the root node. E.g., the root node has depth 0, and a child node
        /// of that will have depth 1.
        /// </param>
        void Head(Node node, int depth);

        /// <summary>
        /// Callback for when a node is last visited, after all of its descendants have been visited.
        /// </summary>
        /// <param name="node">the node being visited.</param>
        /// <param name="depth">
        /// the depth of the node, relative to the root node. E.g., the root node has depth 0, and a child node
        /// of that will have depth 1.
        /// </param>
        void Tail(Node node, int depth)
        {
        }
    }
    
    public class LambdaNodeVisitor : INodeVisitor
    {
        private Action<Node, int> action;
        
        public LambdaNodeVisitor(Action<Node, int> action)
        {
            this.action = action;
        }
        
        public void Head(Node node, int depth)
        {
            action(node, depth);
        }
    }
}
