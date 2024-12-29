using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public class AnimationLayerMixerPlayableNode : PlayableNode
    {
        protected override void AppendInputPortDescription()
        {
            var layerMixer = (AnimationLayerMixerPlayable)Playable;
            var inputCount = layerMixer.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                EditorGUI.BeginChangeCheck();
                var weight = EditorGUILayout.Slider($"  #{i} Weight:", Playable.GetInputWeight(i), 0, 1);
                if (EditorGUI.EndChangeCheck())
                    Playable.SetInputWeight(i, weight);
                EditorGUI.BeginChangeCheck();
                var isLayerAdditive = EditorGUILayout.Toggle($"  #{i} Additive:", layerMixer.IsLayerAdditive((uint)i));
                if (EditorGUI.EndChangeCheck())
                    layerMixer.SetLayerAdditive((uint)i, isLayerAdditive);
            }
        }
    }
}