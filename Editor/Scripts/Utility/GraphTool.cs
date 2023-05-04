using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Audio;
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

            // Try to use PlayableGraph.m_Handle and PlayableGraph.m_Version
        }

        public static string DurationToString(this Playable playable, string format = "F3")
        {
            var duration = playable.GetDuration();
            var durationStr = duration > float.MaxValue ? "+Inf" : duration.ToString(format);

            return durationStr;
        }
     
        /// <summary>
        /// Wrap the value to the range of [0,1].
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Wrap01(double value)
        {
            if (value < 0)
            {
                var result = 1.0 + value % 1;
                // Prevent wrapping -0 to 1
                if (result == 1)
                {
                    result = 0;
                }

                return result;
            }

            if (value > 1)
            {
                var result = value % 1;
                // Prevent wrapping 1 to 0
                if (result == 0)
                {
                    result = 1;
                }

                return result;
            }

            return value;
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
        public static readonly float ColorAlphaFactor = 1f / 9;

        public static readonly Color PortNegativeWeightColor = new Color32(0, 0, 0, 255);

        public static readonly Color PortOverflowWeightColor = new Color32(177, 255, 255, 255);


        public static Color GetPortColor(float weight)
        {
            if (weight < 0)
            {
                // In theory, there should be no inputs with negative weights in PlayableGraph
                return PortNegativeWeightColor;
            }

            if (weight > 1)
            {
                return PortOverflowWeightColor;
            }

            var alpha = (weight + ColorAlphaFactor) / (1 + ColorAlphaFactor);
            return new Color(1, 1, 1, alpha);
        }

        #endregion


        public static Color GetButtonBackgroundColor(bool isChecked)
        {
            if (isChecked)
            {
                return EditorGUIUtility.isProSkin
                    ? new Color32(70, 96, 124, 255)
                    : // dark
                    new Color32(150, 195, 251, 255); // light
            }

            return EditorGUIUtility.isProSkin
                ? new Color32(88, 88, 88, 255)
                : // dark
                new Color32(228, 228, 228, 255); // light
        }

        public static Color GetNodeInspectorBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color32(50, 50, 50, 255)
                : // dark
                new Color32(175, 175, 175, 255); // light
        }


        #region Node Color

        private static readonly IReadOnlyDictionary<Type, Color32> _specialTypeColors = new Dictionary<Type, Color32>()
        {
            { typeof(AnimationClipPlayable), new Color32(0, 255, 100, 255) },
            { typeof(AnimationMixerPlayable), new Color32(0, 190, 255, 255) },
            { typeof(AnimationLayerMixerPlayable), new Color32(0, 115, 255, 255) },
            { typeof(AnimatorControllerPlayable), new Color32(255, 145, 0, 255) },
            { typeof(AnimationScriptPlayable), new Color32(0, 255, 145, 255) },
            { typeof(AnimationPlayableOutput), new Color32(200, 255, 80, 255) },

            { typeof(ScriptPlayableOutput), new Color32(55, 255, 20, 255) },

            { typeof(AudioClipPlayable), new Color32(255, 190, 19, 255) },
            { typeof(AudioMixerPlayable), new Color32(170, 255, 0, 255) },
            { typeof(AudioPlayableOutput), new Color32(255, 135, 0, 255) },
        };


        public static readonly byte MinRGB = 50;
        public static readonly byte MaxRGB = 200;

        public static Color32 GetNodeColor(Type playableType)
        {
            if (!_specialTypeColors.TryGetValue(playableType, out var color) &&
                !_colorCache.TryGetValue(playableType, out color))
            {
                color = GenerateRandomPlayableColor(_colorCache.Values);
            }

            _colorCache[playableType] = color;

            return color;
        }

        public static Color32 GenerateRandomPlayableColor(IEnumerable<Color32> existedColors)
        {
            while (true)
            {
                Color32 color;
                var leader = URandom.Range(0, 3);
                switch (leader)
                {
                    case 0: // R
                        color = new Color32
                        {
                            r = MaxRGB,
                            g = (byte)URandom.Range(MinRGB, MaxRGB + 1),
                            b = (byte)URandom.Range(MinRGB, MaxRGB + 1),
                            a = 255,
                        };
                        break;
                    case 1: // G
                        color = new Color32
                        {
                            r = (byte)URandom.Range(MinRGB, MaxRGB + 1),
                            g = MaxRGB,
                            b = (byte)URandom.Range(MinRGB, MaxRGB + 1),
                            a = 255,
                        };
                        break;

                    case 2: // B
                        color = new Color32
                        {
                            r = (byte)URandom.Range(MinRGB, MaxRGB + 1),
                            g = (byte)URandom.Range(MinRGB, MaxRGB + 1),
                            b = MaxRGB,
                            a = 255,
                        };
                        break;

                    default: throw new IndexOutOfRangeException();
                }

                if (IsSimilarRGB(color))
                {
                    continue;
                }

                var hasSimilarColor = false;
                if (existedColors != null)
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    foreach (var existedColor in existedColors)
                    {
                        if (IsSimilarColor(existedColor, color))
                        {
                            hasSimilarColor = true;
                            break;
                        }
                    }
                }

                if (!hasSimilarColor)
                {
                    return color;
                }
            }
        }

        public static bool IsSimilarRGB(Color32 color, byte threshold = 50)
        {
            var rg = Mathf.Abs(color.r - color.g);
            var gb = Mathf.Abs(color.g - color.b);
            var br = Mathf.Abs(color.b - color.r);

            return rg <= threshold && gb <= threshold && br <= threshold;
        }

        public static bool IsSimilarColor(Color32 a, Color32 b, byte threshold = 10)
        {
            return Mathf.Abs(a.r - b.r) < threshold &&
                   Mathf.Abs(a.g - b.g) < threshold &&
                   Mathf.Abs(a.b - b.b) < threshold &&
                   Mathf.Abs(a.a - b.a) < threshold;
        }

        public static Color GetNodeInspectorTextColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color32(255, 255, 255, 255)
                : // dark
                new Color32(0, 0, 0, 255); // light
        }

        public static Color GetNodeInvalidColor()
        {
            return Color.red;
        }

        public static Color GetPlayableOutputNodeColor(this PlayableOutput playableOutput)
        {
            return GetNodeColor(playableOutput.GetPlayableOutputType());
        }

        public static Color GetPlayableNodeColor(this Playable playable)
        {
            return GetNodeColor(playable.GetPlayableType());
        }

        #endregion


        private static readonly Dictionary<Type, Color32> _colorCache = new Dictionary<Type, Color32>();

        public static void ClearGlobalCache()
        {
            _colorCache.Clear();
        }
    }
}