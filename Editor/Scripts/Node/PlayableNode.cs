using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode : GraphViewNode
    {
        public Playable Playable { get; private set; }

        public bool IsRootPlayable { get; set; }

        public Vector2 Position { get; private set; }

        public PlayableNode ParentNode { get; set; }

        public string ExtraLabel { get; private set; }


        public void Update(PlayableGraphViewUpdateContext updateContext, Playable playable)
        {
            var playableChanged = false;
            if (Playable.GetHandle() != playable.GetHandle())
            {
                Playable = playable;
                playableChanged = true;

                this.SetNodeStyle(playable.GetPlayableNodeColor());
            }

            var extraLabel = GetExtraNodeLabel(updateContext.NodeExtraLabelTable);
            if (playableChanged || ExtraLabel != extraLabel)
            {
                ExtraLabel = extraLabel;

                var playableTypeName = Playable.GetPlayableType().Name;
                var nodeTitle = string.IsNullOrEmpty(ExtraLabel)
                    ? playableTypeName
                    : $"[{ExtraLabel}]\n{playableTypeName}";

                // Expensive operation
                title = nodeTitle;
            }

            if (!Playable.IsValid())
            {
                style.backgroundColor = GraphTool.GetNodeInvalidColor();
                return;
            }

            SyncPorts(out var portChanged);
            // RefreshExpandedState(); // Expensive
            if (portChanged)
            {
                RefreshPorts();
            }

            OnUpdate(updateContext, playableChanged);
        }

        protected virtual void OnUpdate(PlayableGraphViewUpdateContext updateContext, bool playableChanged)
        {
        }

        public PlayableNode FindRootPlayableNode()
        {
            var node = this;
            while (node != null)
            {
                if (node.IsRootPlayable)
                {
                    return node;
                }

                node = node.ParentNode;
            }

            throw new Exception("This should not happen!");
        }

        public override void Release()
        {
            base.Release();
            IsRootPlayable = false;
            Position = default;
            ParentNode = null;
        }

        public override void SetPosition(Rect newPos)
        {
            Position = newPos.position;
            base.SetPosition(newPos);
        }


        #region Description

        // For debugging
        public override string ToString()
        {
            if (Playable.IsValid())
            {
                if (string.IsNullOrEmpty(ExtraLabel))
                {
                    return Playable.GetPlayableType().Name;
                }

                return $"[{ExtraLabel}] {Playable.GetPlayableType().Name}";
            }

            return GetType().Name;
        }

        public string GetExtraNodeLabel(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            if (nodeExtraLabelTable == null || !Playable.IsValid())
            {
                return null;
            }

            var playableHandle = Playable.GetHandle();
            nodeExtraLabelTable.TryGetValue(playableHandle, out var label);

            return label;
        }


        protected override void AppendNodeDescription(StringBuilder descBuilder)
        {
            if (!Playable.IsValid())
            {
                descBuilder.AppendLine("Invalid Playable");
                return;
            }

            // Extra label
            if (!string.IsNullOrEmpty(ExtraLabel))
            {
                descBuilder.Append("ExtraLabel: ").AppendLine(ExtraLabel)
                    .AppendLine(LINE);
            }

            // Type
            AppendPlayableTypeDescription(descBuilder);

            // Playable
            descBuilder.AppendLine(LINE)
                .AppendLine("IsValid: True")
                .Append("IsNull: ").AppendLine(Playable.IsNull().ToString())
                .Append("IsDone: ").AppendLine(Playable.IsDone().ToString())
                .Append("PlayState: ").AppendLine(Playable.GetPlayState().ToString())
                .Append("Speed: ").Append(Playable.GetSpeed().ToString("F3")).AppendLine("x")
                .Append("Duration: ").Append(Playable.DurationToString()).AppendLine("(s)")
                .Append("PreviousTime: ").Append(Playable.GetPreviousTime().ToString("F3")).AppendLine("(s)")
                .Append("Time: ").Append(Playable.GetTime().ToString("F3")).AppendLine("(s)")
                .Append("LeadTime: ").Append(Playable.GetLeadTime().ToString("F3")).AppendLine("s")
                .Append("PropagateSetTime: ").AppendLine(Playable.GetPropagateSetTime().ToString())
                .Append("TraversalMode: ").AppendLine(Playable.GetTraversalMode().ToString());

            // Inputs
            descBuilder.AppendLine(LINE);
            var inputCount = Playable.GetInputCount();
            descBuilder.AppendLine(
                inputCount == 0
                    ? "No Input"
                    : (inputCount == 1 ? "1 Input:" : $"{inputCount.ToString()} Inputs:")
            );
            AppendInputPortDescription(descBuilder);

            // Outputs
            descBuilder.AppendLine(LINE);
            var playableOutputCount = Playable.GetOutputCount();
            descBuilder.AppendLine(
                playableOutputCount == 0
                    ? "No Output"
                    : (playableOutputCount == 1 ? "1 Output" : $"{playableOutputCount.ToString()} Outputs")
            );
        }

        protected virtual void AppendPlayableTypeDescription(StringBuilder descBuilder)
        {
            descBuilder.Append("Type: ").AppendLine(Playable.GetPlayableType()?.Name ?? "?")
                .Append("HandleHashCode: ").AppendLine(Playable.GetHandle().GetHashCode().ToString());
        }

        protected virtual void AppendInputPortDescription(StringBuilder descBuilder)
        {
            var playableInputCount = Playable.GetInputCount();
            for (int i = 0; i < playableInputCount; i++)
            {
                descBuilder.Append("  #").Append(i.ToString()).Append(" Weight: ")
                    .AppendLine(Playable.GetInputWeight(i).ToString("F3"));
            }
        }

        #endregion


        #region Port

        /// <summary>
        /// Find the output port that is connected to <see cref="outputPlayable"/>.
        /// </summary>
        /// <param name="outputPlayable"></param>
        /// <returns></returns>
        public Port FindOutputPort(Playable outputPlayable)
        {
            // If the output port of the Playable at index i is connected to a PlayableOutput,
            // Playable.GetOutput(i) will return an invalid Playable.

            // TODO FIXME: If multiple output ports of `outputPlayable` connect to different input ports of the same `Playable`,
            // this method cannot distinguish between these output ports.

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var output = Playable.GetOutput(i);
                if (output.GetHandle() == outputPlayable.GetHandle())
                {
                    return OutputPorts[i];
                }
            }

            return null;
        }

        private void SyncPorts(out bool portChanged)
        {
            portChanged = false;
            var isPlayableValid = Playable.IsValid();

            // Input ports
            var inputCount = isPlayableValid ? Playable.GetInputCount() : 0;
            var redundantInputPortCount = InputPorts.Count - inputCount;
            for (int i = 0; i < redundantInputPortCount; i++)
            {
                // Port won't change frequently, so there's no PortPool
                inputContainer.Remove(InputPorts[i]);
                InputPorts.RemoveAt(i);
                portChanged = true;
            }

            var missingInputPortCount = inputCount - InputPorts.Count;
            for (int i = 0; i < missingInputPortCount; i++)
            {
                var inputPort = InstantiatePort<Playable>(Direction.Input);
                inputPort.portName = $"Input {InputPorts.Count}";
                inputPort.portColor = GraphTool.GetPortColor(Playable.GetInputWeight(i));

                inputContainer.Add(inputPort);
                InputPorts.Add(inputPort);
                portChanged = true;
            }

            // Output ports
            var outputCount = isPlayableValid ? Playable.GetOutputCount() : 0;
            var redundantOutputPortCount = OutputPorts.Count - outputCount;
            for (int i = 0; i < redundantOutputPortCount; i++)
            {
                // Port won't change frequently, so there's no PortPool
                outputContainer.Remove(OutputPorts[i]);
                OutputPorts.RemoveAt(i);
                portChanged = true;
            }

            var missingOutputPortCount = outputCount - OutputPorts.Count;
            for (int i = 0; i < missingOutputPortCount; i++)
            {
                var outputPort = InstantiatePort<Playable>(Direction.Output);
                outputPort.portName = $"Output {OutputPorts.Count}";
                outputPort.portColor = GraphTool.GetPortColor(1);

                outputContainer.Add(outputPort);
                OutputPorts.Add(outputPort);
                portChanged = true;
            }
        }

        #endregion
    }
}