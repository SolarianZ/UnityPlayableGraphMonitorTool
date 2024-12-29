using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public sealed class AnimationClipPlayableNode : PlayableNode
    {
        private readonly ObjectField _clipField;

        private readonly ProgressBar _progressBar;


        public AnimationClipPlayableNode()
        {
            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;

            _progressBar = new ProgressBar();
#if !UNITY_2021_1_OR_NEWER
            var progressBarBg = _progressBar.Q<VisualElement>(className: "unity-progress-bar__background");
            progressBarBg.style.height = 17;
#endif
            banner.Add(_progressBar);

            _clipField = new ObjectField
            {
                objectType = typeof(Motion),
            };
            var clipFieldSelector = _clipField.Q(className: "unity-object-field__selector");
            clipFieldSelector.style.display = DisplayStyle.None;
            clipFieldSelector.SetEnabled(false);
            banner.Add(_clipField);
        }

        protected override void OnUpdate(PlayableGraphViewUpdateContext updateContext, bool playableChanged)
        {
            base.OnUpdate(updateContext, playableChanged);

            if (!Playable.IsValid())
            {
                return;
            }


            var clipPlayable = (AnimationClipPlayable)Playable;
            var clip = clipPlayable.GetAnimationClip();
            _clipField.SetValueWithoutNotify(clip);

            // MEMO KEYWORD: CLIP_PROGRESS
            if (updateContext.ShowClipProgressBar)
            {
                _progressBar.style.display = DisplayStyle.Flex;

                // ReSharper disable once TooWideLocalVariableScope
                // ReSharper disable once RedundantAssignment
                var rawProgress01 = 0.0; // used in UNITY_2021_1_OR_NEWER
                double progress01;
                if (clip)
                {
                    var time = Playable.GetTime();
                    rawProgress01 = time / clip.length;

                    if (clip.isLooping)
                    {
                        progress01 = GraphTool.Wrap01(rawProgress01);
                    }
                    else
                    {
                        var speed = Playable.GetSpeed();
                        if (speed > 0 && time >= clip.length)
                        {
                            progress01 = 1;
                        }
                        else if (speed < 0 && time <= 0)
                        {
                            progress01 = 0;
                        }
                        else
                        {
                            progress01 = GraphTool.Wrap01(rawProgress01);
                        }
                    }
                }
                else
                {
                    progress01 = 0;
                }

                // Expensive operations
                _progressBar.SetValueWithoutNotify((float)progress01 * 100);

#if UNITY_2021_1_OR_NEWER
                // Expensive operations
                _progressBar.title = updateContext.ShowClipProgressBarTitle
                    ? (rawProgress01 * 100).ToString("F2")
                    : null;
#endif
            }
            else
            {
                _progressBar.style.display = DisplayStyle.None;
            }
        }

        // public override void Release()
        // {
        //     base.Release();
        //     // Change the value of the ObjectField is expensive, so we don't clear referenced clip asset to save performance 
        //     // _clipField.SetValueWithoutNotify(null);
        // }

        protected override void DrawNodeDescriptionInternal()
        {
            base.DrawNodeDescriptionInternal();

            if (!Playable.IsValid())
            {
                return;
            }

            var animClipPlayable = (AnimationClipPlayable)Playable;

            // IK
            GUILayout.Label(LINE);
            EditorGUILayout.Toggle("ApplyFootIK:", animClipPlayable.GetApplyFootIK());
            EditorGUILayout.Toggle("ApplyPlayableIK:", animClipPlayable.GetApplyPlayableIK());

            // Clip
            GUILayout.Label(LINE);
            var clip = animClipPlayable.GetAnimationClip();
            if (!clip)
            {
                GUILayout.Label("Clip: None");
                return;
            }

            EditorGUILayout.ObjectField("Clip:", clip, typeof(AnimationClip), true);
            GUILayout.Label($"Length: {clip.length.ToString("F3")}(s)");
            GUILayout.Label($"Looped: {clip.isLooping}");
            GUILayout.Label($"WrapMode: {clip.wrapMode}");
            GUILayout.Label($"FrameRate: {clip.frameRate.ToString("F3")}");
            GUILayout.Label($"Empty: {clip.empty}");
            GUILayout.Label($"Legacy: {clip.legacy}");
            GUILayout.Label($"HumanMotion: {clip.humanMotion}");
            GUILayout.Label($"HasMotionCurves: {clip.hasMotionCurves}");
            GUILayout.Label($"HasRootCurves: {clip.hasRootCurves}");
            GUILayout.Label($"HasGenericRootTransform: {clip.hasGenericRootTransform}");
            GUILayout.Label($"HasMotionFloatCurves: {clip.hasMotionFloatCurves}");
            GUILayout.Label($"LocalBounds: ");
            GUILayout.Label($"  Center: {clip.localBounds.center}");
            GUILayout.Label($"  Extends: {clip.localBounds.extents}");
            GUILayout.Label($"ApparentSpeed: {clip.apparentSpeed.ToString("F3")}"); // Motion.cs
            GUILayout.Label($"AverageSpeed: {clip.averageSpeed}");
            GUILayout.Label($"AverageAngularSpeed: {clip.averageAngularSpeed.ToString("F3")}");
            GUILayout.Label($"AverageDuration: {clip.averageDuration.ToString("F3")}");
            GUILayout.Label($"IsHumanMotion: {clip.isHumanMotion}");

            // Event
            GUILayout.Label(LINE);
            var events = clip.events;
            GUILayout.Label(
                events.Length == 0
                    ? "No Event"
                    : (events.Length == 1 ? "1 Event:" : $"{events.Length.ToString()} Events:")
            );
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                var evtPosition = evt.time / clip.length * 100;
                GUILayout.Label($"  #{(i + 1)} {evtPosition:F2}% {evt.functionName}");
            }
        }
    }
}