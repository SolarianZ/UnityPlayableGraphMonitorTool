using UnityEngine;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Editor.Utility
{
    public static class GraphTool
    {
        // Ensure edge is always visible.
        private const float _colorAlphaFactor = 1f / 9;


        public static Color GetPortColor(PlayableOutput playableOutput)
        {
            var portColor = Color.white;
            portColor.a = 1;

            var playableOutputType = playableOutput.GetPlayableOutputType();
            // todo set port color by playable output type

            return portColor;
        }

        public static Color GetPortColor(Playable playable, float weight)
        {
            var portColor = Color.white;
            portColor.a = (weight + _colorAlphaFactor) / (1 + weight);

            var playableType = playable.GetPlayableType();
            // todo set port color by playable type

            return portColor;
        }

        public static Color GetPortInvalidColor(float weight)
        {
            var portColor = Color.red;
            portColor.a = (weight + _colorAlphaFactor) / (1 + weight);

            return portColor;
        }

        public static bool IsEqual(ref PlayableGraph a, ref PlayableGraph b)
        {
            if (!a.IsValid())
            {
                return !b.IsValid();
            }

            if (!b.IsValid())
            {
                return false;
            }

            var nameA = a.GetEditorName();
            var nameB = b.GetEditorName();

            return nameA.Equals(nameB);
        }
    }
}