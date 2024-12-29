using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
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

            style.maxWidth = MaxNodeSize.x - 100;

            // Hide collapse button
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;
        }


        public virtual void Release()
        {
            _cachedHierarchySize = null;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }


        #region Description

        private StringBuilder _descBuilder;

        protected const string LINE = "----------";


        public void DrawNodeDescription()
        {
            var prevSize = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120;
            DrawNodeDescriptionInternal();
            EditorGUIUtility.labelWidth = prevSize;
        }


        protected abstract void DrawNodeDescriptionInternal();

        #endregion


        #region Layout

        public const int HORIZONTAL_SPACE = 80;

        public const int VERTICAL_SPACE = 60;

        public static readonly Vector2 MaxNodeSize = new Vector2(400, 150);

        private Vector2? _cachedHierarchySize;


        public void CalculateLayout(Vector2 origin, out Vector2 nodePosition, out Vector2 hierarchySize)
        {
            var nodeSize = GetNodeSize();
            hierarchySize = CalculateHierarchySize();
            nodePosition = CalculateTreeRootNodePosition(origin, hierarchySize, nodeSize);
            SetPosition(new Rect(nodePosition, Vector2.zero));

            origin.x -= nodeSize.x - HORIZONTAL_SPACE;
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                if (childNode == null)
                {
                    continue;
                }

                childNode.CalculateLayout(origin, out var _, out var childHierarchySize);
                origin.y += childHierarchySize.y;
            }
        }

        public Vector2 CalculateHierarchySize()
        {
            if (_cachedHierarchySize != null)
            {
                return _cachedHierarchySize.Value;
            }

            if (InputPorts.Count == 0)
            {
                _cachedHierarchySize = GetNodeSize();
                return _cachedHierarchySize.Value;
            }

            var subHierarchySize = Vector2.zero;
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                var childSize = childNode?.CalculateHierarchySize() ?? Vector2.zero;
                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }

            subHierarchySize.y += (InputPorts.Count - 1) * VERTICAL_SPACE;

            var nodeSize = GetNodeSize();
            var hierarchySize = new Vector2(
                nodeSize.x + subHierarchySize.x + HORIZONTAL_SPACE,
                Mathf.Max(nodeSize.y, subHierarchySize.y)
            );
            _cachedHierarchySize = hierarchySize;

            return _cachedHierarchySize.Value;
        }

        public Vector2 GetNodeSize()
        {
            return MaxNodeSize;
            //return worldBound.size;
        }

        protected static Vector2 CalculateTreeRootNodePosition(Vector2 treeOrigin, Vector2 treeSize,
            Vector2 rootNodeSize)
        {
            var rootNodePosition = new Vector2(treeOrigin.x, treeOrigin.y + treeSize.y / 2f - rootNodeSize.y / 2f);
            return rootNodePosition;
        }

        protected static GraphViewNode GetFirstConnectedInputNode(Port port)
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