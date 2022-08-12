using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableNodeFactory
    {
        public const string PlayableHeader = "Playable";

        public static PlayableNode CreateNode(Playable playable)
        {
            var playableType = playable.GetPlayableType();
            var playableTypeName = playableType.Name;
            var playableTypeSortName = playableTypeName
                .Remove(playableTypeName.Length - PlayableHeader.Length);
            var nodeTitle = $"{PlayableHeader}\n{playableTypeSortName}";

            PlayableNode playableNode;

            // todo create node by playable type

            if (playableType == typeof(AnimationClipPlayable))
            {
                //goto SET_NODE_TITLE;
            }

            if (playableType == typeof(AnimationMixerPlayable))
            {

            }

            if (playableType == typeof(AnimationLayerMixerPlayable))
            {

            }

            // ...

            // default node
            playableNode = new PlayableNode(playable);

        SET_NODE_TITLE:
            playableNode.title = nodeTitle;

            return playableNode;
        }
    }
}