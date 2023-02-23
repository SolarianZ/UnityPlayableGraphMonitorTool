using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public abstract class GraphViewNode_New : UNode
    {
        protected GraphViewNode_New()
        {
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Deletable;

            // Hide collapse button
            titleButtonContainer.Clear();
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.marginRight = 6;
        }

        // todo: Release referenced assets
        public virtual void Clean()
        {
            Description = null;

            // Disconnect all ports
            for (int i = 0; i < InputPorts.Count; i++)
            {
                InputPorts[i].DisconnectAll();
            }

            for (int i = 0; i < OutputPorts.Count; i++)
            {
                OutputPorts[i].DisconnectAll();
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }


        #region Playable

        public abstract IEnumerable<Playable> GetInputPlayables();

        public abstract IEnumerable<Playable> GetOutputPlayables();

        #endregion


        #region Description

        public string Description { get; private set; }

        private StringBuilder _descBuilder;


        public void UpdateDescription()
        {
            if (_descBuilder == null)
            {
                _descBuilder = new StringBuilder();
            }

            _descBuilder.Clear();
            AppendStateDescriptions(_descBuilder);
            Description = _descBuilder.ToString();
            _descBuilder.Clear();
        }


        protected abstract void AppendStateDescriptions(StringBuilder descBuilder);

        #endregion


        #region Layout

        public const int HORIZONTAL_SPACE = 80;

        public const int VERTICAL_SPACE = 60;

        public static readonly Vector2 StandardNodeSize = new Vector2(400, 150);

        private Vector2? _hierarchySize;


        public Vector2 GetHierarchySize()
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
                var childSize = childNode != null ? childNode.GetHierarchySize() : Vector2.zero;
                subHierarchySize.x = Mathf.Max(subHierarchySize.x, childSize.x);
                subHierarchySize.y += childSize.y;
            }

            subHierarchySize.y += (InputPorts.Count - 1) * VERTICAL_SPACE;

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
            for (int i = 0; i < InputPorts.Count; i++)
            {
                var childNode = GetFirstConnectedInputNode(InputPorts[i]);
                if (childNode != null)
                {
                    var childHierarchySize = childNode.GetHierarchySize();
                    childNode.CalculateLayout(origin);

                    origin.y += childHierarchySize.y;
                }
            }
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

        private static GraphViewNode_New GetFirstConnectedInputNode(Port port)
        {
            Assert.IsTrue(port.direction == Direction.Input);
            var connectedEdge = port.connections.FirstOrDefault();
            var connectedNode = connectedEdge?.output?.node as GraphViewNode_New;
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

        public Port GetOutputPort(int index)
        {
            return OutputPorts[index];
        }


        protected Port InstantiatePort<TPortData>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Multi, typeof(TPortData));
        }

        #endregion
    }
}