using GBG.PlayableGraphMonitor.Editor.Utility;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode : GraphViewNode
    {
        public Playable Playable { get; }

        public Type PlayableType => Playable.IsValid() ? Playable.GetPlayableType() : null;


        public PlayableNode(Playable playable)
        {
            Playable = playable;

            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();
        }

        public override void Update()
        {
            base.Update();

            if (!Playable.IsValid())
            {
                foreach (var port in InternalOutputPorts)
                {
                    port.portColor = GraphTool.GetPortInvalidColor(0);
                }
                return;
            }

            // mark all child nodes inactive
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                InternalInputs[i].Node.RemoveFlag(NodeFlag.Active);
            }

            // diff child nodes
            for (int i = 0; i < Playable.GetInputCount(); i++)
            {
                // update port color
                var inputWeight = Playable.GetInputWeight(i);
                InternalInputPorts[i].portColor = GraphTool.GetPortColor(Playable, inputWeight);

                var inputPlayable = Playable.GetInput(i);
                if (!inputPlayable.IsValid())
                {
                    continue;
                }

                var childNodeIndex = FindChildPlayableNode(inputPlayable);
                if (childNodeIndex >= 0)
                {
                    var input = InternalInputs[i];
                    input.Node.AddFlag(NodeFlag.Active);
                    InternalInputs[i] = input.Copy(input, i);
                    continue;
                }

                // create new node
                var inputPlayableNode = PlayableNodeFactory.CreateNode(inputPlayable);
                inputPlayableNode.AddToContainer(Container);
                inputPlayableNode.AddFlag(NodeFlag.Active);

                var inputPlayableNodeOutputPort = inputPlayableNode.OutputPorts[0];
                var selfInputPort = InternalInputPorts[i];
                var edge = selfInputPort.ConnectTo(inputPlayableNodeOutputPort);
                Container.AddElement(edge);
                InternalInputs.Add(new NodeInput(edge, inputPlayableNode, i));
            }

            // update child nodes
            for (int i = InternalInputs.Count - 1; i >= 0; i--)
            {
                var input = InternalInputs[i];

                // remove inactive child node
                if (!input.Node.CheckFlag(NodeFlag.Active))
                {
                    Container.RemoveElement(input.Edge);
                    input.Node.RemoveFromContainer();

                    InternalInputs.RemoveAt(i);
                    continue;
                }

                // update port color
                var inputWeight = Playable.GetInputWeight(input.PortIndex);
                input.Node.OutputPorts[0].portColor = GraphTool.GetPortColor(Playable, inputWeight);
                input.Edge.UpdateEdgeControl();

                InternalInputs[i].Node.Update();
            }
        }


        protected int FindChildPlayableNode(Playable playable)
        {
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                if (((PlayableNode)InternalInputs[i].Node).Playable.Equals(playable))
                {
                    return i;
                }
            }

            return -1;
        }


        private void CreatePorts()
        {
            if (!Playable.IsValid())
            {
                return;
            }

            for (int i = 0; i < Playable.GetInputCount(); i++)
            {
                var inputPort = InstantiatePort<Playable>(Direction.Input);
                inputPort.portName = $"Input {i}";
                inputPort.portColor = GraphTool.GetPortColor(Playable, Playable.GetInputWeight(i));
                inputContainer.Add(inputPort);
                InternalInputPorts.Add(inputPort);
            }

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var outputPort = InstantiatePort<Playable>(Direction.Output);
                outputPort.portName = $"Output {i}";
                outputPort.portColor = GraphTool.GetPortColor(Playable, 1);
                outputContainer.Add(outputPort);
                InternalOutputPorts.Add(outputPort);
            }
        }
    }
}