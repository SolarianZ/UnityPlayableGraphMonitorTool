using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public abstract class GraphViewNode : UNode
    {
        internal static NodeLayoutInfo LayoutInfo;

        public int Depth { get; }

        public IReadOnlyList<Port> InputPorts => InternalInputPorts;

        protected List<Port> InternalInputPorts { get; } = new List<Port>();

        public IReadOnlyList<Port> OutputPorts => InternalOutputPorts;

        protected List<Port> InternalOutputPorts { get; } = new List<Port>();

        protected UGraphView Container { get; set; }

        // public IReadOnlyList<Edge> InputEdges => InternalInputEdges;

        protected List<Edge> InternalInputEdges { get; } = new List<Edge>();

        protected GraphViewNode Parent { get; private set; }

        protected new List<GraphViewNode> Children { get; } = new List<GraphViewNode>();


        private Vector2? _hierarchySize;


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

            if (Children.Count == 1)
            {
                var selfSize = GetNodeSize();
                var childHierarchySize = Children[0].GetHierarchySize();
                _hierarchySize = new Vector2
                {
                    x = selfSize.x + childHierarchySize.x + NodeLayoutInfo.HorizontalSpace,
                    y = selfSize.y + childHierarchySize.y + NodeLayoutInfo.VerticalSpace
                };

                return _hierarchySize.Value;
            }

            var hierarchySize = GetNodeSize() + new Vector2(NodeLayoutInfo.HorizontalSpace, 0);
            for (int i = 0; i < Children.Count; i++)
            {
                var childSize = Children[i].GetHierarchySize();
                hierarchySize.x += childSize.x;
                hierarchySize.y = Mathf.Max(hierarchySize.y, childSize.y);
            }

            hierarchySize.y += (Children.Count - 1) * NodeLayoutInfo.VerticalSpace;
            _hierarchySize = hierarchySize;

            return _hierarchySize.Value;
        }

        public void CalculateLayout(Vector2 treeSize, Vector2 origin)
        {
            var hierarchySize = GetHierarchySize();
            var nodePos = CalculateSubTreeRootPosition(treeSize, hierarchySize, origin);
            SetPosition(new Rect(nodePos, Vector2.zero));

            origin.x -= GetNodeSize().x - NodeLayoutInfo.HorizontalSpace;
            for (int i = 0; i < Children.Count; i++)
            {
                var childNode = Children[i];
                var childHierarchySize = childNode.GetHierarchySize();
                childNode.CalculateLayout(treeSize, origin);

                origin.y += childHierarchySize.y + NodeLayoutInfo.VerticalSpace;
            }
        }

        public static Vector2 CalculateSubTreeRootPosition(Vector2 treeSize, Vector2 subTreeSize, Vector2 subTreeOrigin)
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


        protected GraphViewNode(int depth)
        {
            Depth = depth;
        }

        protected Port InstantiatePort<TPort>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Single, typeof(TPort));
        }
    }
}
