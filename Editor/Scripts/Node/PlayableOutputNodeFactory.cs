using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Playables;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableOutputNodeFactory
    {
        public const string PlayableOutputHeader = "PlayableOutput";

        public static PlayableOutputNode CreateNode(PlayableOutput playableOutput)
        {
            var playableOutputType = playableOutput.GetPlayableOutputType();
            var playableOutputTypeName = playableOutputType.Name;
            var playableOutputTypeSortName = playableOutputTypeName
                .Remove(playableOutputTypeName.Length - PlayableOutputHeader.Length);
            var playableOutputEditorName = playableOutput.GetEditorName();
            var nodeTitle = $"{PlayableOutputHeader}\n" +
                            $"{playableOutputTypeSortName} ({playableOutputEditorName})";

            if (playableOutputType == typeof(AnimationPlayableOutput))
            {

            }

            // todo create node by playable output type

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
