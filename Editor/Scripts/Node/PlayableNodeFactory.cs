using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableNodeFactory
    {
        public static PlayableNode CreateNode(Playable playable)
        {
            var playableType = playable.GetPlayableType();
            var playableTypeName = playableType.Name;

            if (playableType == typeof(AnimationClipPlayable))
            {

            }

            if (playableType == typeof(AnimationMixerPlayable))
            {

            }

            if (playableType == typeof(AnimationLayerMixerPlayable))
            {

            }

            // ...

            // default node
            var playableNode = new PlayableNode(playable)
            {
                title = playableTypeName
            };
            return playableNode;
        }
    }
}