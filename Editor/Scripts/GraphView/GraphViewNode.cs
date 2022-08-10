using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Assertions;
using UNode = UnityEditor.Experimental.GraphView.Node;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public abstract class GraphViewNode : UNode
    {
        public PlayableGraphView Owner { get; }

        public int Depth { get; }

        public IReadOnlyList<Port> InputPorts => InternalInputPorts;

        public IReadOnlyList<Port> OutputPorts => InternalOutputPorts;

        protected List<Port> InternalInputPorts { get; private set; } = new List<Port>();

        protected List<Port> InternalOutputPorts { get; private set; } = new List<Port>();


        protected GraphViewNode(PlayableGraphView owner, int depth)
        {
            Owner = owner;
            Depth = depth;
        }

        protected Port InstantiatePort<TPort>(Direction direction)
        {
            return InstantiatePort(Orientation.Horizontal, direction, Port.Capacity.Single, typeof(TPort));
        }
    }
}