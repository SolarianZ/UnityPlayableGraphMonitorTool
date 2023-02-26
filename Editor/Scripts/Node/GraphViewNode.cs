using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public abstract class GraphViewNode : UNode
    {
        protected GraphViewNode()
        {
            capabilities &= ~Capabilities.Collapsible;
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Droppable;
            capabilities &= ~Capabilities.Renamable;
#if UNITY_2021_1_OR_NEWER
            capabilities &= ~Capabilities.Copiable;
#endif

            style.maxWidth = StandardNodeSize.x - 100;

            // Hide collapse button
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;
        }


        public virtual void Release()
        {
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }


        #region Description

        private StringBuilder _descBuilder;

        protected const string LINE = "----------";


        public string GetNodeDescription()
        {
            if (_descBuilder == null)
            {
                _descBuilder = new StringBuilder();
            }

            _descBuilder.Clear();
            AppendNodeDescription(_descBuilder);
            return _descBuilder.ToString();
        }


        protected abstract void AppendNodeDescription(StringBuilder descBuilder);

        #endregion


        #region Layout

        public const int HORIZONTAL_SPACE = 80;

        public const int VERTICAL_SPACE = 60;

        public static readonly Vector2 StandardNodeSize = new Vector2(400, 150);

        private Vector2? _hierarchySize;


        public void CalculateLayout(Vector2 origin, out Vector2 hierarchySize)
        {
            hierarchySize = GetHierarchySize();
            var nodePos = CalculateSubTreeRootNodePosition(hierarchySize, origin);
            SetPosition(new Rect(nodePos, Vector2.zero));

            origin.x -= GetNodeSize().x - HORIZONTAL_SPACE;
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                if (childNode != null)
                {
                    childNode.CalculateLayout(origin, out var childHierarchySize);

                    origin.y += childHierarchySize.y;
                }
            }
        }

        private Vector2 GetHierarchySize()
        {
            if (_hierarchySize != null)
            {
                return _hierarchySize.Value;
            }

            if (InputPorts.Count == 0)
            {
                _hierarchySize = GetNodeSize();
                return _hierarchySize.Value;
            }

            var subHierarchySize = Vector2.zero;
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                var childSize = childNode?.GetHierarchySize() ?? Vector2.zero;
                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }

            subHierarchySize.y += (InputPorts.Count - 1) * VERTICAL_SPACE;

            var hierarchySize = GetNodeSize() + new Vector2(HORIZONTAL_SPACE, 0);
            hierarchySize.y = Mathf.Max(hierarchySize.y, subHierarchySize.y);
            _hierarchySize = hierarchySize;

            return _hierarchySize.Value;
        }

        private Vector2 GetNodeSize()
        {
            return StandardNodeSize;
            //return worldBound.size;
        }

        private static Vector2 CalculateSubTreeRootNodePosition(Vector2 subTreeSize, Vector2 subTreeOrigin)
        {
            var subTreePos = subTreeOrigin;
            subTreePos.y += subTreeSize.y / 2;
            return subTreePos;
        }

        private static GraphViewNode GetFirstConnectedInputNode(Port port)
        {
            Assert.IsTrue(port.direction == Direction.Input);
            var connectedEdge = port.connections.FirstOrDefault();
            var connectedNode = connectedEdge?.output?.node as GraphViewNode;
            return connectedNode;
        }

        #endregion


        #region Port

        protected List<Port> InputPorts { get; } = new List<Port>();

        protected List<Port> OutputPorts { get; } = new List<Port>();


        public Port GetInputPort(int index)
        {
            return InputPorts[index];
        }

        // TODO FIXME: About fallbackOnPort0
        // In the PlayableGraph of a Timeline, all PlayableOutputs will connect to same single TimelinePlayable instance,
        // and PlayableOutput.GetSourceOutputPort() may return a NON-ZERO value but in fact the TimelinePlayable only has ONE output!
        public Port GetOutputPort(int index, bool fallbackOnPort0)
        {
            if (index >= OutputPorts.Count && fallbackOnPort0)
            {
                return OutputPorts[0];
            }

            return OutputPorts[index];
        }


        protected Port InstantiatePort<TPortData>(Direction direction)
        {
            var capacity = direction == Direction.Output ? Port.Capacity.Single : Port.Capacity.Multi;
            return InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(TPortData));
        }

        #endregion
    }
}