using GBG.PlayableGraphMonitor.Editor.Node;
using System;
using System.Collections.Generic;
using UnityEditor;
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

        public static string DurationToString(this Playable playable, string format = "F3")
        {
            var duration = playable.GetDuration();
            var durationStr = duration > float.MaxValue ? "+Inf" : duration.ToString(format);

            return durationStr;
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


        #region Port Color

        // Ensure edge is always visible.
        public const float ColorAlphaFactor = 1f / 9;


        public static Color GetPortColor(float weight)
        {
            var alpha = (weight + ColorAlphaFactor) / (1 + ColorAlphaFactor);
            return new Color(1, 1, 1, alpha);
        }

        #endregion



        public static Color GetButtonBackgroundColor(bool isChecked)
        {
            if (isChecked)
            {
                return EditorGUIUtility.isProSkin ?
                    new Color32(70, 96, 124, 255) : // dark
                    new Color32(150, 195, 251, 255); // light

            }

            return EditorGUIUtility.isProSkin ?
                new Color32(88, 88, 88, 255) : // dark
                new Color32(228, 228, 228, 255); // light
        }

        public static Color GetNodeInspectorBackgroundColor()
        {
            return EditorGUIUtility.isProSkin ?
                new Color32(50, 50, 50, 255) : // dark
                new Color32(175, 175, 175, 255); // light
        }


        #region Node Color

        public static Color GetNodeInspectorTextColor()
        {
            return EditorGUIUtility.isProSkin ?
                new Color32(255, 255, 255, 255) : // dark
                new Color32(0, 0, 0, 255); // light
        }

        public static Color GetNodeInvalidColor()
        {
            return Color.red;
        }

        // (0, ?, 255, 255) for PlayableOutput nodes
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

        // (0, 255, ?, 255) for Playable nodes
        public static Color GetPlayableNodeColor(this ref Playable playable)
        {
            if (playable.IsPlayableOfType<AnimationClipPlayable>())
            {
                return new Color32(0, 255, 51, 255);
            }

            if (playable.IsPlayableOfType<AnimationMixerPlayable>())
            {
                return new Color32(0, 255, 102, 255);
            }

            if (playable.IsPlayableOfType<AnimationLayerMixerPlayable>())
            {
                return new Color32(0, 255, 153, 255);
            }

            if (playable.IsPlayableOfType<AnimationScriptPlayable>())
            {
                return new Color32(0, 255, 204, 255);
            }

            return GetRandomColorForType(playable.GetPlayableType());
        }

        #endregion


        // reserve (0, ?, 255, 255) for PlayableOutput nodes
        // reserve (0, 255, ?, 255) for Playable nodes
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

        public static void ClearGlobalCache()
        {
            _colorCache.Clear();
        }
    }
}