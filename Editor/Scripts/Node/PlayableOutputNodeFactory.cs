using UnityEditor.Playables;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableOutputNodeFactory
    {
        public static PlayableOutputNode CreateNode(PlayableOutput playableOutput)
        {
            var playableOutputType = playableOutput.GetPlayableOutputType();
            var playableOutputTypeName = playableOutputType.Name;

            if (playableOutputType == typeof(AnimationPlayableOutput))
            {

            }

            // ...

            // default node
            var playableOutputEditorName = playableOutput.GetEditorName();
            var playableOutputNode = new PlayableOutputNode(playableOutput)
            {
                title = $"{playableOutputTypeName} ({playableOutputEditorName})"
            };
            return playableOutputNode;
        }
    }
}
