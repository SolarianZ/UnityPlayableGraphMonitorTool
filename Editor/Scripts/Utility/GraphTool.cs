using GBG.PlayableGraphMonitor.Editor.Node;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using URandom = UnityEngine.Random;

namespace GBG.PlayableGraphMonitor.Editor.Utility
{
    public static class GraphTool
    {
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


        public static void SetNodeStyle(this GraphViewNode node, Color nodeColor,
            float titleFontSize = 15, Color? titleColor = null)
        {
            // title
            var titleLabel = node.Q("title-label");
            titleLabel.style.fontSize = titleFontSize;
            titleLabel.style.color = titleColor ?? Color.black;
            var titlePanel = node.Q("title");
            titlePanel.style.backgroundColor = nodeColor;
        }


        // Ensure edge is always visible.
        public const float ColorAlphaFactor = 1f / 9;


        public static Color GetPortColor(float weight)
        {
            var alpha = (weight + ColorAlphaFactor) / (1 + ColorAlphaFactor);
            return new Color(1, 1, 1, alpha);
        }

        public static Color GetNodeInvalidColor()
        {
            return Color.red;
        }

        public static Color GetPlayableOutputNodeColor(this ref PlayableOutput playableOutput)
        {
            if (playableOutput.IsPlayableOutputOfType<AnimationPlayableOutput>())
            {
                return new Color32(0, 255, 255, 255);
            }

            if (playableOutput.IsPlayableOutputOfType<ScriptPlayableOutput>())
            {
                return new Color32(0, 204, 255, 255);
            }


            return GetRandomColorForType(playableOutput.GetPlayableOutputType());
        }

        public static Color GetPlayableNodeColor(this ref Playable playable)
        {
            if (playable.IsPlayableOfType<AnimationClipPlayable>())
            {
                return new Color32(0, 255, 102, 255);
            }

            if (playable.IsPlayableOfType<AnimationMixerPlayable>())
            {
                return new Color32(0, 255, 153, 255);
            }

            if (playable.IsPlayableOfType<AnimationLayerMixerPlayable>())
            {
                return new Color32(0, 255, 204, 255);
            }

            return GetRandomColorForType(playable.GetPlayableType());
        }

        public static Color GetRandomColorForType(this Type type)
        {
            if (_colorCache.TryGetValue(type, out var color))
            {
                return color;
            }

            color = ColorPool[URandom.Range(0, ColorPool.Count)];
            _colorCache[type] = color;

            return color;
        }


        public static readonly IReadOnlyList<Color32> ColorPool = new Color32[]
        {
            new Color32(255,0,255,255), new Color32(255,0,153,255),
            new Color32(153,255,0,255), new Color32(204,255,0,255),
            new Color32(255,255,0,255), new Color32(255,204,0,255),
            new Color32(255,153,0,255), new Color32(255,102,0,255),
            new Color32(153,204,255,255), new Color32(153,255,102,255),
            new Color32(153,255,153,255), new Color32(153,255,204,255),
            new Color32(204,255,102,255), new Color32(204,255,255,255),
        };

        private static readonly Dictionary<Type, Color32> _colorCache = new Dictionary<Type, Color32>();


        public static void ClearGlobalCache()
        {
            _colorCache.Clear();
        }
    }
}