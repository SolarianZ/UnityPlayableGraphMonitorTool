using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UEdge = UnityEditor.Experimental.GraphView.Edge;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public enum NodeFlag : uint
    {
        None = 0,

        Active = 1 << 0,

        HierarchyDirty = 1 << 1,
    }

    public readonly struct NodeInput
    {
        public readonly UEdge Edge;

        public readonly GraphViewNode Node;

        public readonly int PortIndex;


        public NodeInput(UEdge edge, GraphViewNode node, int portIndex)
        {
            Edge = edge;
            Node = node;
            PortIndex = portIndex;
        }
    }

    public abstract class GraphViewNode : UNode
    {
        public IReadOnlyList<Port> OutputPorts => InternalOutputPorts;

        protected List<Port> InternalInputPorts { get; } = new List<Port>();

        protected List<Port> InternalOutputPorts { get; } = new List<Port>();

        protected List<NodeInput> InternalInputs { get; } = new List<NodeInput>();

        protected UGraphView Container { get; private set; }

        protected GraphViewNode Parent { get; private set; }


        protected GraphViewNode()
        {
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;

            // Hide collapse button
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;
        }

        public virtual void Update()
        {
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // disable contextual menu
        }


        #region Description

        private StringBuilder _descBuilder;


        public string GetStateDescription()
        {
            if (_descBuilder == null)
            {
                _descBuilder = new StringBuilder();
            }

            _descBuilder.Clear();
            AppendStateDescriptions(_descBuilder);

            return _descBuilder.ToString();
        }


        protected abstract void AppendStateDescriptions(StringBuilder descBuilder);

        #endregion


        #region Hierarchy

        public void AddToView(UGraphView container, GraphViewNode parentNode)
        {
            Container = container;
            Container.AddElement(this);
            Parent = parentNode;
        }

        public void RemoveFromView()
        {
            // self
            Container.RemoveElement(this);

            // children
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                var input = InternalInputs[i];
                Container.RemoveElement(input.Edge);
                input.Node.RemoveFromView();
            }

            InternalInputs.Clear();

            Container = null;
            Parent = null;
        }


        protected Port InstantiatePort<TPort>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Single, typeof(TPort));
        }

        #endregion


        #region Layout

        public const int HORIZONTAL_SPACE = 80;

        public const int VERTICAL_SPACE = 60;

        public static readonly Vector2 StandardNodeSize = new Vector2(400, 150);

        private Vector2? _hierarchySize;


        public Vector2 GetNodeSize()
        {
            return StandardNodeSize;
            //return worldBound.size;
        }

        public Vector2 GetHierarchySize()
        {
            if (_hierarchySize != null)
            {
                return _hierarchySize.Value;
            }

            if (InternalInputs.Count == 0)
            {
                _hierarchySize = GetNodeSize();
                return _hierarchySize.Value;
            }

            var subHierarchySize = Vector2.zero;
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                var childSize = InternalInputs[i].Node.GetHierarchySize();
                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }

            subHierarchySize.y += (InternalInputs.Count - 1) * VERTICAL_SPACE;

            var hierarchySize = GetNodeSize() + new Vector2(HORIZONTAL_SPACE, 0);
            hierarchySize.y = Mathf.Max(hierarchySize.y, subHierarchySize.y);
            _hierarchySize = hierarchySize;

            return _hierarchySize.Value;
        }

        public void CalculateLayout(Vector2 origin)
        {
            var subTreeSize = GetHierarchySize();
            var nodePos = CalculateSubTreeRootNodePosition(subTreeSize, origin);
            SetPosition(new Rect(nodePos, Vector2.zero));

            origin.x -= GetNodeSize().x - HORIZONTAL_SPACE;
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                var childNode = InternalInputs[i];
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

        private uint _flags;


        public bool CheckFlag(NodeFlag flag)
        {
            return (_flags & (uint)flag) != 0;
        }

        public void AddFlag(NodeFlag flag)
        {
            var oldFlags = _flags;
            _flags |= (uint)flag;

            if (oldFlags != _flags)
            {
                OnFlagsChanged();
            }
        }

        public void RemoveFlag(NodeFlag flag)
        {
            var oldFlags = _flags;
            _flags &= ~(uint)flag;

            if (oldFlags != _flags)
            {
                OnFlagsChanged();
            }
        }


        private void OnFlagsChanged()
        {
            if (CheckFlag(NodeFlag.HierarchyDirty))
            {
                _hierarchySize = null;
                RemoveFlag(NodeFlag.HierarchyDirty);

                Parent?.AddFlag(NodeFlag.HierarchyDirty);
            }
        }

        #endregion
    }
}