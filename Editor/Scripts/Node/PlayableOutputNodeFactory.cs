using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Playables;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableOutputNodeFactory
    {
        public static PlayableOutputNode CreateNode(PlayableOutput playableOutput)
        {
            var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
            var playableOutputEditorName = playableOutput.GetEditorName();

            // default node
            var playableOutputNode = new PlayableOutputNode(playableOutput)
            {
                title = $"{playableOutputTypeName}\n({playableOutputEditorName})",
            };
            playableOutputNode.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());

            return playableOutputNode;
        }
    }
}
