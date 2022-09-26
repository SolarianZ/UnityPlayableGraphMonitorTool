using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableNodeFactory
    {
        public const string PLAYABLE_HEADER = "Playable";

        public static PlayableNode CreateNode(Playable playable)
        {
            var playableType = playable.GetPlayableType();
            var playableTypeName = playableType.Name;
            var playableTypeSortName = playableTypeName
                .Remove(playableTypeName.Length - PLAYABLE_HEADER.Length);
            var nodeTitle = $"{PLAYABLE_HEADER}\n{playableTypeSortName}";

            // create node by playable type
            PlayableNode playableNode;
            if (playableType == typeof(AnimationClipPlayable))
            {
                playableNode = new AnimationClipPlayableNode(playable);
            }
            else if (playableType == typeof(AnimationLayerMixerPlayable))
            {
                playableNode = new AnimationLayerMixerPlayableNode(playable);
            }
            else
            {
                // default node
                playableNode = new PlayableNode(playable);
            }

            playableNode.title = nodeTitle;
            playableNode.SetNodeStyle(playable.GetPlayableNodeColor());

            return playableNode;
        }
    }
}
