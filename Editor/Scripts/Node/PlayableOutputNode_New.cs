using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class PlayableOutputNode_New : GraphViewNode_New
    {
        public PlayableOutput PlayableOutput { get; private set; }

        public Port InputPort { get; private set; }

        public void Setup(PlayableOutput playableOutput)
        {
            PlayableOutput = playableOutput;

            var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
            var playableOutputEditorName = playableOutput.GetEditorName();
            title = $"{playableOutputTypeName}\n({playableOutputEditorName})";

            // this.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());

            throw new System.NotImplementedException();
        }
    }
}