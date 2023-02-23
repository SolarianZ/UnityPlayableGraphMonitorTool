using System.Collections.Generic;
using System.Text;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode_New : GraphViewNode_New
    {
        public void Setup(Playable playable)
        {
            Playable = playable;
            PoolKey = Playable.IsValid() ? Playable.GetHandle().GetHashCode() : 0;

            SyncPorts(out var portChanged);
            // RefreshExpandedState(); // Expensive
            // if (portChanged)
            // {
            //     RefreshPorts();
            // }
            RefreshPorts();

            // todo: Update Description
        }


        #region Playable

        public Playable Playable { get; private set; }


        public override IEnumerable<Playable> GetInputPlayables()
        {
            if (!Playable.IsValid())
            {
                yield break;
            }

            for (int i = 0; i < Playable.GetInputCount(); i++)
            {
                var inputPlayable = Playable.GetInput(i);
                yield return inputPlayable;
            }
        }

        public override IEnumerable<Playable> GetOutputPlayables()
        {
            if (!Playable.IsValid())
            {
                yield break;
            }

            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var outputPlayable = Playable.GetOutput(i);
                yield return outputPlayable;
            }
        }

        #endregion


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
            descBuilder.Append("Type: ").AppendLine(Playable.GetPlayableType()?.Name ?? "")
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


        #region Port

        public Port FindConnectedOutputPort(Playable connectedOutputPlayable)
        {
            for (int i = 0; i < Playable.GetOutputCount(); i++)
            {
                var output = Playable.GetOutput(i);
                if (output.GetHandle() == connectedOutputPlayable.GetHandle())
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
                // todo: use port pool
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
                // todo: use port pool
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