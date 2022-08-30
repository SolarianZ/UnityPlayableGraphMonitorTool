using GBG.PlayableGraphMonitor.Editor.Utility;
using System;
using System.Text;
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
                style.backgroundColor = GraphTool.GetNodeInvalidColor();
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
                // update self port color
                var inputWeight = Playable.GetInputWeight(i);
                InternalInputPorts[i].portColor = GraphTool.GetPortColor(inputWeight);

                var inputPlayable = Playable.GetInput(i);
                if (!inputPlayable.IsValid())
                {
                    continue;
                }

                // FIXME: A Playable may has multi output ports,
                // and if all these output ports connected to same input port,
                // 'i' will be greater than InternalInputs.Count, so there is a check.
                if (i < InternalInputs.Count)
                {
                    var childNodeIndex = FindChildPlayableNode(inputPlayable);
                    if (childNodeIndex >= 0)
                    {
                        var input = InternalInputs[i];
                        input.Node.AddFlag(NodeFlag.Active);
                        InternalInputs[i] = input.Copy(input, i);
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