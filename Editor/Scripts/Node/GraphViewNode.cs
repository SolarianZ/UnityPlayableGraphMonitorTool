using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public abstract class GraphViewNode : UNode
    {
        public int Depth { get; }

        public IReadOnlyList<Port> InputPorts => InternalInputPorts;

        protected List<Port> InternalInputPorts { get; } = new List<Port>();

        public IReadOnlyList<Port> OutputPorts => InternalOutputPorts;

        protected List<Port> InternalOutputPorts { get; } = new List<Port>();

        // public IReadOnlyList<Edge> InputEdges => InternalInputEdges;

        protected List<Edge> InternalInputEdges { get; } = new List<Edge>();

        protected UGraphView Container { get; set; }

        protected GraphViewNode Parent { get; private set; }

        protected new List<GraphViewNode> Children { get; } = new List<GraphViewNode>();


        private Vector2? _hierarchySize;


        protected GraphViewNode(int depth)
        {
            Depth = depth;
        }

        protected Port InstantiatePort<TPort>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Single, typeof(TPort));
        }


        public Vector2 GetNodeSize()
        {
            return NodeLayoutInfo.StandardNodeSize;
            //return worldBound.size;
        }

        public Vector2 GetHierarchySize()
        {
            if (_hierarchySize != null)
            {
                return _hierarchySize.Value;
            }

            if (Children.Count == 0)
            {
                _hierarchySize = GetNodeSize();
                return _hierarchySize.Value;
            }

            var subHierarchySize = Vector2.zero;
            for (int i = 0; i < Children.Count; i++)
            {
                var childSize = Children[i].GetHierarchySize();
                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }
            subHierarchySize.y += (Children.Count - 1) * NodeLayoutInfo.VerticalSpace;

            var hierarchySize = GetNodeSize() + new Vector2(NodeLayoutInfo.HorizontalSpace, 0);
            hierarchySize.y = Mathf.Max(hierarchySize.y, subHierarchySize.y);
            _hierarchySize = hierarchySize;

            return _hierarchySize.Value;
        }

        public void CalculateLayout(Vector2 origin)
        {
            var subTreeSize = GetHierarchySize();
            var nodePos = CalculateSubTreeRootNodePosition(subTreeSize, origin);
            SetPosition(new Rect(nodePos, Vector2.zero));

            origin.x -= GetNodeSize().x - NodeLayoutInfo.HorizontalSpace;
            for (int i = 0; i < Children.Count; i++)
            {
                var childNode = Children[i];
                var childHierarchySize = childNode.GetHierarchySize();
                childNode.CalculateLayout(origin);

                origin.y += childHierarchySize.y;
            }
        }

        public static Vector2 CalculateSubTreeRootNodePosition(Vector2 subTreeSize, Vector2 subTreeOrigin)
        {
            var subTreePos = subTreeOrigin;
            subTreePos.y += subTreeSize.y / 2;
            return subTreePos;
        }

        public virtual void AddToContainer(UGraphView container)
        {
            Container = container;
            Container.AddElement(this);
        }

        public virtual void RemoveFromContainer()
        {
            // self
            Container.RemoveElement(this);

            // input edges
            for (int i = 0; i < InternalInputEdges.Count; i++)
            {
                Container.RemoveElement(InternalInputEdges[i]);
            }

            InternalInputEdges.Clear();

            // children
            foreach (var childNode in Children)
            {
                childNode.RemoveFromContainer();
            }

            Children.Clear();

            Container = null;
        }

        public abstract void CreateAndConnectInputNodes();
    }
}
