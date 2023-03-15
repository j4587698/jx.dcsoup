using Supremes.Nodes;

namespace Supremes.Select
{
    /// <summary>
    /// Collects a list of elements that match the supplied criteria.
    /// </summary>
    /// <author>Jonathan Hedley</author>
    public class Collector
    {
        public Collector()
        {
        }

        /// <summary>
        /// Build a list of elements,
        /// by visiting root and every descendant of root, and testing it against the evaluator.
        /// </summary>
        /// <param name="eval">Evaluator to test elements against</param>
        /// <param name="root">root of tree to descend</param>
        /// <returns>list of matches; empty if none</returns>
        public static Elements Collect(Evaluator eval, Element root)
        {
            Elements elements = new Elements();
            NodeTraversor.Traverse(new LambdaNodeVisitor((node, depth) =>
            {
                if (node is Element el)
                {
                    if (eval.Matches(root, el))
                        elements.Add(el);
                }
            }), root);
            return elements;
        }

        public static Element FindFirst(Evaluator eval, Element root)
        {
            FirstFinder finder = new FirstFinder(eval);
            return finder.Find(root, root);
        }
        
        public class FirstFinder : NodeFilter
        {
            private Element evalRoot = null;
            private Element match = null;
            private Evaluator eval;

            public FirstFinder(Evaluator eval)
            {
                this.eval = eval;
            }

            public Element Find(Element root, Element start)
            {
                evalRoot = root;
                match = null;
                NodeTraversor.Filter(this, start);
                return match;
            }

            public NodeFilter.FilterResult Head(Node node, int depth)
            {
                if (node is Element)
                {
                    Element el = (Element)node;
                    if (eval.Matches(evalRoot, el))
                    {
                        match = el;
                        return NodeFilter.FilterResult.Stop;
                    }
                }
                return NodeFilter.FilterResult.Continue;
            }

            public NodeFilter.FilterResult Tail(Node node, int depth)
            {
                return NodeFilter.FilterResult.Continue;
            }
        }
    }
}
