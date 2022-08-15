using System.Text;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationClipPlayableNode : PlayableNode
    {
        public AnimationClipPlayableNode(Playable playable) : base(playable)
        {
        }


        protected override void AppendStateDescriptions(StringBuilder descBuilder)
        {
            base.AppendStateDescriptions(descBuilder);

            if (Playable.IsValid())
            {
                var clipPlayable = (AnimationClipPlayable)Playable;
                descBuilder.Append("ApplyFootIK: ").AppendLine(clipPlayable.GetApplyFootIK().ToString())
                    .Append("ApplyPlayableIK: ").AppendLine(clipPlayable.GetApplyPlayableIK().ToString())
                    .AppendLine();

                var clip = clipPlayable.GetAnimationClip();
                descBuilder.Append("Clip: ").AppendLine(clip ? clip.name : "None");
                if (clip)
                {
                    descBuilder.Append("IsLooping: ").AppendLine(clip.isLooping.ToString())
                        .Append("Length: ").Append(clip.length.ToString("F3")).AppendLine("(s)");
                }
            }
        }
    }
}