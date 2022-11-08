using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public static class PlayableNodeFactory
    {
        public static PlayableNode CreateNode(Playable playable)
        {
            // create node by playable type
            PlayableNode playableNode;
            var playableType = playable.GetPlayableType();
            if (playableType == typeof(AnimationClipPlayable))
            {
                playableNode = new AnimationClipPlayableNode(playable);
            }
            else if (playableType == typeof(AnimationLayerMixerPlayable))
            {
                playableNode = new AnimationLayerMixerPlayableNode(playable);
            }
            else if (playableType == typeof(AudioClipPlayable))
            {
                playableNode = new AudioClipPlayableNode(playable);
            }
            else
            {
                // default node
                playableNode = new PlayableNode(playable);
            }

            playableNode.title = playableType.Name;
            playableNode.SetNodeStyle(playable.GetPlayableNodeColor());

            return playableNode;
        }
    }
}
