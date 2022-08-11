using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.GraphView;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Assertions;
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

        protected UGraphView Container { get;  set; }

        // public IReadOnlyList<Edge> InputEdges => InternalInputEdges;

        protected List<Edge> InternalInputEdges { get; } = new List<Edge>();

        protected List<GraphViewNode> ChildNodes { get; } = new List<GraphViewNode>();


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
            foreach (var childNode in ChildNodes)
            {
                childNode.RemoveFromContainer();
            }

            ChildNodes.Clear();

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