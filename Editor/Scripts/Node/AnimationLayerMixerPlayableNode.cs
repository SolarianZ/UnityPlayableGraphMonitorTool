using System.Text;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationLayerMixerPlayableNode : PlayableNode
    {
        public AnimationLayerMixerPlayableNode(Playable playable) : base(playable)
        {
        }


        protected override void AppendStateDescriptions(StringBuilder descBuilder)
        {
            base.AppendStateDescriptions(descBuilder);

            if (Playable.IsValid())
            {
                descBuilder.AppendLine();

                var layerMixer = (AnimationLayerMixerPlayable)Playable;
                for (uint i = 0; i < layerMixer.GetInputCount(); i++)
                {
                    descBuilder.Append("#").Append(i.ToString()).Append(" IsLayerAdditive: ")
                        .AppendLine(layerMixer.IsLayerAdditive(i).ToString());
                }
            }
        }
    }
}