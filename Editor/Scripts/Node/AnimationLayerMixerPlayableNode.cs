using System.Text;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationLayerMixerPlayableNode : PlayableNode
    {
        protected override void AppendInputPortDescription(StringBuilder descBuilder)
        {
            var layerMixer = (AnimationLayerMixerPlayable)Playable;
            var inputCount = layerMixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                descBuilder.Append("  #").Append(i.ToString())
                    .Append(" Weight: ").Append(Playable.GetInputWeight(i).ToString("F3"))
                    .Append(" Additive: ").AppendLine(layerMixer.IsLayerAdditive((uint)i).ToString());
            }
        }
    }
}