using System.Text;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode : GraphViewNode
    {
        public PlayableOutput PlayableOutput { get; private set; }


        public PlayableOutputNode()
        {
            CreatePorts();
            // RefreshExpandedState(); // Expensive
            RefreshPorts();
        }

        public void Update(PlayableOutput playableOutput)
        {
            var playableOutputChanged = false;
            if (PlayableOutput.GetHandle() != playableOutput.GetHandle())
            {
                PlayableOutput = playableOutput;
                playableOutputChanged = true;

                // Expensive operations
                var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
                var playableOutputEditorName = playableOutput.GetEditorName();
                title = $"[{playableOutputEditorName}]\n{playableOutputTypeName}";

                this.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());
            }

            OnUpdate(playableOutputChanged);
        }

        protected virtual void OnUpdate(bool playableOutputChanged)
        {
        }


        #region Description

        // For debugging
        public override string ToString()
        {
            if (PlayableOutput.IsOutputValid())
            {
                return PlayableOutput.GetPlayableOutputType().Name;
            }

            return GetType().Name;
        }

        protected override void AppendNodeDescription(StringBuilder descBuilder)
        {
            if (!PlayableOutput.IsOutputValid())
            {
                descBuilder.AppendLine("Invalid PlayableOutput");
                return;
            }

            descBuilder.Append("Type: ").AppendLine(PlayableOutput.GetPlayableOutputType().Name)
                .AppendLine(LINE)
                .Append("Name: ").AppendLine(PlayableOutput.GetEditorName())
                .AppendLine(LINE)
                .AppendLine("IsValid: True")
                .Append("IsNull: ").AppendLine(PlayableOutput.IsOutputNull().ToString())
                .Append("ReferenceObject: ").AppendLine(PlayableOutput.GetReferenceObject()?.name ?? "Null")
                .Append("UserData: ").AppendLine(PlayableOutput.GetUserData()?.name ?? "Null")
                .AppendLine(LINE)
                .AppendLine("Source Input:")
                .Append("    SourceOutputPort: ").AppendLine(PlayableOutput.GetSourceOutputPort().ToString())
                .Append("    Weight: ").AppendLine(PlayableOutput.GetWeight().ToString("F3"));
        }

        #endregion


        #region Port

        public Port InputPort { get; private set; }


        private void CreatePorts()
        {
            InputPort = InstantiatePort<Playable>(Direction.Input);
            InputPort.portName = "Source";
            InputPort.portColor = Color.white;

            inputContainer.Add(InputPort);
            InputPorts.Add(InputPort);
        }

        #endregion
    }
}