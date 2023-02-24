using System.Text;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableNode : GraphViewNode
    {
        public Playable Playable { get; private set; }


        public void Update(Playable playable)
        {
            var playableChanged = false;
            if (Playable.GetHandle() != playable.GetHandle())
            {
                Playable = playable;
                playableChanged = true;

                // Expensive operations
                var playableTypeName = Playable.GetPlayableType().Name;
                // var playableHandleTypeName = Playable.GetHandle().GetPlayableType();
                title = playableTypeName;

                this.SetNodeStyle(playable.GetPlayableNodeColor());
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

            OnUpdate(playableChanged);
        }

        protected virtual void OnUpdate(bool playableChanged)
        {
        }


        #region Description

        // For debugging
        public override string ToString()
        {
            if (Playable.IsValid())
            {
                return Playable.GetPlayableType().Name;
            }

            return GetType().Name;
        }

        protected override void AppendNodeDescription(StringBuilder descBuilder)
        {
            if (!Playable.IsValid())
            {
                descBuilder.AppendLine("Invalid Playable");
                return;
            }

            AppendPlayableTypeDescription(descBuilder);
            descBuilder.AppendLine(LINE)
                .AppendLine("IsValid: True")
                .Append("IsNull: ").AppendLine(Playable.IsNull().ToString())
                .Append("IsDone: ").AppendLine(Playable.IsDone().ToString())
                .Append("PlayState: ").AppendLine(Playable.GetPlayState().ToString())
                .Append("Duration: ").Append(Playable.DurationToString()).AppendLine("(s)")
                .Append("Time: ").Append(Playable.GetTime().ToString("F3")).AppendLine("(s)")
                .Append("Speed: ").Append(Playable.GetSpeed().ToString("F3")).AppendLine("x");

            // Inputs
            descBuilder.AppendLine(LINE);
            var inputCount = Playable.GetInputCount();
            descBuilder.AppendLine(
                inputCount == 0
                    ? "No Input"
                    : (inputCount == 1 ? "1 Input:" : $"{inputCount} Inputs:")
            );
            AppendInputPortDescription(descBuilder);

            // Outputs
            descBuilder.AppendLine(LINE);
            var playableOutputCount = Playable.GetOutputCount();
            descBuilder.AppendLine(
                playableOutputCount == 0
                    ? "No Output"
                    : (playableOutputCount == 1 ? "1 Output" : $"{playableOutputCount} Outputs")
            );
        }

        protected virtual void AppendPlayableTypeDescription(StringBuilder descBuilder)
        {
            descBuilder.Append("Type: ").AppendLine(Playable.GetPlayableType()?.Name ?? "");
        }

        protected virtual void AppendInputPortDescription(StringBuilder descBuilder)
        {
            var playableInputCount = Playable.GetInputCount();
            for (int i = 0; i < playableInputCount; i++)
            {
                descBuilder.Append("    #").Append(i.ToString()).Append(" Weight: ")
                    .AppendLine(Playable.GetInputWeight(i).ToString("F3"));
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