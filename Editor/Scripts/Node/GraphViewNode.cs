using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UEdge = UnityEditor.Experimental.GraphView.Edge;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public enum NodeFlag : uint
    {
        None = 0,
        Active = 1 << 0,
        Dirty = 1 << 1,
    }

    public readonly struct NodeInput
    {
        public UEdge Edge { get; }

        public GraphViewNode Node { get; }


        public NodeInput(UEdge edge, GraphViewNode node)
        {
            Edge = edge;
            Node = node;
        }
    }

    public abstract class GraphViewNode : UNode
    {
        public IReadOnlyList<Port> InputPorts => InternalInputPorts;

        protected List<Port> InternalInputPorts { get; } = new List<Port>();

        public IReadOnlyList<Port> OutputPorts => InternalOutputPorts;

        protected List<Port> InternalOutputPorts { get; } = new List<Port>();

        protected UGraphView Container { get; set; }

        public IReadOnlyList<NodeInput> Inputs => InternalInputs;

        protected List<NodeInput> InternalInputs { get; } = new List<NodeInput>();

        protected GraphViewNode Parent { get; private set; }


        public virtual void Update() { }


        #region Hierarchy

        public virtual void AddToContainer(UGraphView container)
        {
            Container = container;
            Container.AddElement(this);
        }

        public virtual void RemoveFromContainer()
        {
            // self
            Container.RemoveElement(this);

            // children
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                var input = InternalInputs[i];
                Container.RemoveElement(input.Edge);
                input.Node.RemoveFromContainer();
            }

            InternalInputs.Clear();

            Container = null;
        }


        protected Port InstantiatePort<TPort>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Single, typeof(TPort));
        }

        #endregion


        #region Layout

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

            if (Inputs.Count == 0)
            {
                _hierarchySize = GetNodeSize();
                return _hierarchySize.Value;
            }

            var subHierarchySize = Vector2.zero;
            for (int i = 0; i < Inputs.Count; i++)
            {
                var childSize = Inputs[i].Node.GetHierarchySize();
                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }
            subHierarchySize.y += (Inputs.Count - 1) * NodeLayoutInfo.VerticalSpace;

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
            for (int i = 0; i < Inputs.Count; i++)
            {
                var childNode = Inputs[i];
                var childHierarchySize = childNode.Node.GetHierarchySize();
                childNode.Node.CalculateLayout(origin);

                origin.y += childHierarchySize.y;
            }
        }

        public static Vector2 CalculateSubTreeRootNodePosition(Vector2 subTreeSize, Vector2 subTreeOrigin)
        {
            var subTreePos = subTreeOrigin;
            subTreePos.y += subTreeSize.y / 2;
            return subTreePos;
        }

        #endregion


        #region Flags

        private uint _flags = 0;


        public bool CheckFlag(NodeFlag flag)
        {
            return (_flags & (uint)flag) != 0;
        }

        public void AddFlag(NodeFlag flag)
        {
            _flags |= (uint)flag;
        }

        public void RemoveFlag(NodeFlag flag)
        {
            _flags &= ~(uint)flag;
        }

        #endregion
    }
}
