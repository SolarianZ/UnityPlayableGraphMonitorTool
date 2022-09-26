using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Playables;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableOutputNodeFactory
    {
        public const string PLAYABLE_OUTPUT_HEADER = "PlayableOutput";

        public static PlayableOutputNode CreateNode(PlayableOutput playableOutput)
        {
            var playableOutputType = playableOutput.GetPlayableOutputType();
            var playableOutputTypeName = playableOutputType.Name;
            var playableOutputTypeSortName = playableOutputTypeName
                .Remove(playableOutputTypeName.Length - PLAYABLE_OUTPUT_HEADER.Length);
            var playableOutputEditorName = playableOutput.GetEditorName();
            var nodeTitle = $"{PLAYABLE_OUTPUT_HEADER}\n" +
                            $"{playableOutputTypeSortName} ({playableOutputEditorName})";

            // default node
            var playableOutputNode = new PlayableOutputNode(playableOutput)
            {
                title = nodeTitle,
            };
            playableOutputNode.SetNodeStyle(playableOutput.GetPlayableOutputNodeColor());

            return playableOutputNode;
        }
    }
}
