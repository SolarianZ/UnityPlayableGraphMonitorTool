using System;
using System.Text;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
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

            PreparePorts();
            RefreshPorts();
            RefreshExpandedState();
        }

        public override void Update()
        {
            base.Update();

            if (!Playable.IsValid())
            {
                style.backgroundColor = GraphTool.GetNodeInvalidColor();
                return;
            }

            PreparePorts();

            // mark all child nodes inactive
            for (int i = 0; i < InternalInputs.Count; i++)
            {
                InternalInputs[i].Node.RemoveFlag(NodeFlag.Active);
            }

            // diff child nodes
            var skipPort = 0;
            for (int i = 0; i < Playable.GetInputCount(); i++)
            {
                // update self port color
                var inputWeight = Playable.GetInputWeight(i);
                InternalInputPorts[i].portColor = GraphTool.GetPortColor(inputWeight);

                var inputPlayable = Playable.GetInput(i);
                if (!inputPlayable.IsValid())
                {
                    skipPort++;
                    continue;
                }

                // TODO FIXME: A Playable may has multi output ports,
                // and if all these output ports connected to same input port,
                // 'i' will be greater than InternalInputs.Count, so there is a skipPort check.
                var index = i - skipPort;
                if (index < InternalInputs.Count)
                {
                    var childNodeIndex = FindChildPlayableNode(inputPlayable);
                    if (childNodeIndex >= 0)
                    {
                        var input = InternalInputs[index];
                        input.Node.AddFlag(NodeFlag.Active);
                        InternalInputs[index] = new NodeInput(input.Edge, input.Node, i);
                        continue;
                    }
                }

                // create new node
                var inputPlayableNode = PlayableNodeFactory.CreateNode(inputPlayable);
                inputPlayableNode.AddToView(Container, this);
                inputPlayableNode.AddFlag(NodeFlag.Active);

                var inputPlayableNodeOutputPort = inputPlayableNode.OutputPorts[0];
                var selfInputPort = InternalInputPorts[i];
                var edge = selfInputPort.ConnectTo(inputPlayableNodeOutputPort);
                Container.AddElement(edge);
                edge.capabilities &= ~Capabilities.Movable;
                edge.capabilities &= ~Capabilities.Deletable;
                edge.capabilities &= ~Capabilities.Selectable;
                InternalInputs.Add(new NodeInput(edge, inputPlayableNode, i));

                AddFlag(NodeFlag.HierarchyDirty);
            }

            // check and update children
            for (int i = InternalInputs.Count - 1; i >= 0; i--)
            {
                var input = InternalInputs[i];

                // remove inactive child node
                if (!input.Node.CheckFlag(NodeFlag.Active))
                {
                    Container.RemoveElement(input.Edge);
                    input.Node.RemoveFromView();

                    InternalInputs.RemoveAt(i);

                    AddFlag(NodeFlag.HierarchyDirty);

                    continue;
                }

                // update child port color
                var inputWeight = Playable.GetInputWeight(input.PortIndex);
                input.Node.OutputPorts[0].portColor = GraphTool.GetPortColor(inputWeight);
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

        protected void PreparePorts()
        {
            // Input ports
            var realInputCount = Playable.GetInputCount();
            var cachedInputCount = InternalInputPorts.Count;
            var portChanged = realInputCount != cachedInputCount;
            if (realInputCount < cachedInputCount)
            {
                var count = cachedInputCount - realInputCount;
                for (int i = 0; i < count; i++)
                {
                    var index = cachedInputCount - i - 1;
                    InternalInputPorts.RemoveAt(index);
                    inputContainer.RemoveAt(index);
                }
            }
            else if (realInputCount > cachedInputCount)
            {
                var count = realInputCount - cachedInputCount;
                for (int i = 0; i < count; i++)
                {
                    var index = i + cachedInputCount;
                    var inputPort = InstantiatePort<Playable>(Direction.Input);
                    inputPort.portName = $"Input {index}";
                    inputPort.portColor = GraphTool.GetPortColor(Playable.GetInputWeight(index));
                    InternalInputPorts.Add(inputPort);
                    inputContainer.Add(inputPort);
                }
            }

            // Output ports
            var realOutputCount = Playable.GetOutputCount();
            if (realOutputCount > 1)
            {
                Debug.LogError("Playable Graph Monitor doesn't support node with multiple output port.");
            }

            var cachedOutputCount = InternalOutputPorts.Count;
            portChanged |= realOutputCount != cachedOutputCount;
            if (realOutputCount < cachedOutputCount)
            {
                var count = cachedOutputCount - realOutputCount;
                for (int i = 0; i < count; i++)
                {
                    var index = cachedOutputCount - i - 1;
                    InternalOutputPorts.RemoveAt(index);
                    outputContainer.RemoveAt(index);
                }
            }
            else if (realOutputCount > cachedOutputCount)
            {
                var count = realOutputCount - cachedOutputCount;
                for (int i = 0; i < count; i++)
                {
                    var index = i + cachedOutputCount;
                    var outputPort = InstantiatePort<Playable>(Direction.Output);
                    outputPort.portName = $"Output {index}";
                    outputPort.portColor = GraphTool.GetPortColor(1);
                    InternalOutputPorts.Add(outputPort);
                    outputContainer.Add(outputPort);
                }
            }

            if (portChanged)
            {
                RefreshPorts();
            }
        }


        // ReSharper disable once UnusedMember.Local
        [Obsolete]
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
                inputPort.portColor = GraphTool.GetPortColor(Playable.GetInputWeight(i));
                inputContainer.Add(inputPort);
                InternalInputPorts.Add(inputPort);
            }

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var outputPort = InstantiatePort<Playable>(Direction.Output);
                outputPort.portName = $"Output {i}";
                outputPort.portColor = GraphTool.GetPortColor(1);
                outputContainer.Add(outputPort);
                InternalOutputPorts.Add(outputPort);
            }
        }


        #region Description

        public override string ToString()
        {
            if (Playable.IsValid())
            {
                return Playable.GetPlayableType().Name;
            }

            return GetType().Name;
        }

        protected override void AppendStateDescriptions(StringBuilder descBuilder)
        {
            descBuilder.Append("Type: ").AppendLine(PlayableType.Name)
                .Append("IsValid: ").AppendLine(Playable.IsValid().ToString());
            if (Playable.IsValid())
            {
                descBuilder.Append("IsDone: ").AppendLine(Playable.IsDone().ToString())
                    .Append("PlayState: ").AppendLine(Playable.GetPlayState().ToString())
                    .Append("Speed: ").Append(Playable.GetSpeed().ToString("F3")).AppendLine("x")
                    .Append("Duration: ").Append(Playable.DurationToString()).AppendLine("(s)")
                    .Append("Time: ").Append(Playable.GetTime().ToString("F3")).AppendLine("(s)");
                for (int i = 0; i < Playable.GetInputCount(); i++)
                {
                    descBuilder.Append("#").Append(i.ToString()).Append(" InputWeight: ")
                        .AppendLine(Playable.GetInputWeight(i).ToString("F3"));
                }

                descBuilder.Append("OutputCount: ").AppendLine(Playable.GetOutputCount().ToString());
            }
        }

        #endregion
    }
}