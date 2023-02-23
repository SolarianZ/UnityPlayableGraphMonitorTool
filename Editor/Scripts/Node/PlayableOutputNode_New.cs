using System.Collections.Generic;
using System.Text;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode_New : GraphViewNode_New
    {
        public PlayableOutputNode_New()
        {
            CreatePorts();
            RefreshExpandedState();
            RefreshPorts();
        }

        public void Setup(PlayableOutput playableOutput)
        {
            PlayableOutput = playableOutput;

            var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
            var playableOutputEditorName = playableOutput.GetEditorName();
            title = $"{playableOutputTypeName}\n({playableOutputEditorName})";

            this.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());

            // todo: Update Description
        }


        #region Playable

        public PlayableOutput PlayableOutput { get; private set; }

        public override IEnumerable<Playable> GetInputPlayables()
        {
            yield return PlayableOutput.GetSourcePlayable();
        }

        public override IEnumerable<Playable> GetOutputPlayables()
        {
            yield break;
        }

        #endregion


        #region Description

        public override string ToString()
        {
            if (PlayableOutput.IsOutputValid())
            {
                return PlayableOutput.GetPlayableOutputType().Name;
            }

            return GetType().Name;
        }

        protected override void AppendStateDescriptions(StringBuilder descBuilder)
        {
            descBuilder.Append("Type: ").AppendLine(PlayableOutput.GetPlayableOutputType().Name)
                .Append("IsValid: ").AppendLine(PlayableOutput.IsOutputValid().ToString());
            if (PlayableOutput.IsOutputValid())
            {
                descBuilder.Append("Name: ").AppendLine(PlayableOutput.GetEditorName())
                    .Append("Weight: ").AppendLine(PlayableOutput.GetWeight().ToString("F3"))
                    .Append("ReferenceObject: ").AppendLine(PlayableOutput.GetReferenceObject()?.name ?? "Null")
                    .Append("UserData: ").AppendLine(PlayableOutput.GetUserData()?.name ?? "Null")
                    .Append("SourceOutputPort: ").AppendLine(PlayableOutput.GetSourceOutputPort().ToString());
            }
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