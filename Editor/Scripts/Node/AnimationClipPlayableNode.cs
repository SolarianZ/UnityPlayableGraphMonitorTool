using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Text;
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

                var rawProgress01 = 0.0;
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
        //     // Change the value of the ObjectField is expensive, so we dont clear referenced clip asset to save performance 
        //     // _clipField.SetValueWithoutNotify(null);
        // }

        protected override void AppendNodeDescription(StringBuilder descBuilder)
        {
            base.AppendNodeDescription(descBuilder);

            if (!Playable.IsValid())
            {
                return;
            }

            var animClipPlayable = (AnimationClipPlayable)Playable;

            // IK
            descBuilder.AppendLine(LINE)
                .Append("ApplyFootIK: ").AppendLine(animClipPlayable.GetApplyFootIK().ToString())
                .Append("ApplyPlayableIK: ").AppendLine(animClipPlayable.GetApplyPlayableIK().ToString());

            // Clip
            descBuilder.AppendLine(LINE);
            var clip = animClipPlayable.GetAnimationClip();
            if (!clip)
            {
                descBuilder.AppendLine("Clip: None");
                return;
            }

            descBuilder.Append("Clip: ").AppendLine(clip.name)
                .Append("Length: ").Append(clip.length.ToString("F3")).AppendLine("(s)")
                .Append("Looped: ").AppendLine(clip.isLooping.ToString())
                .Append("WrapMode: ").AppendLine(clip.wrapMode.ToString())
                .Append("FrameRate: ").AppendLine(clip.frameRate.ToString("F3"))
                .Append("Empty: ").AppendLine(clip.empty.ToString())
                .Append("Legacy: ").AppendLine(clip.legacy.ToString())
                .Append("HumanMotion: ").AppendLine(clip.humanMotion.ToString())
                .Append("HasMotionCurves: ").AppendLine(clip.hasMotionCurves.ToString())
                .Append("HasRootCurves: ").AppendLine(clip.hasRootCurves.ToString())
                .Append("HasGenericRootTransform: ").AppendLine(clip.hasGenericRootTransform.ToString())
                .Append("HasMotionFloatCurves: ").AppendLine(clip.hasMotionFloatCurves.ToString())
                .AppendLine("LocalBounds: ")
                .Append("  Center: ").AppendLine(clip.localBounds.center.ToString())
                .Append("  Extends: ").AppendLine(clip.localBounds.extents.ToString())
                .Append("ApparentSpeed: ").AppendLine(clip.apparentSpeed.ToString("F3")) // Motion.cs
                .Append("AverageSpeed: ").AppendLine(clip.averageSpeed.ToString())
                .Append("AverageAngularSpeed: ").AppendLine(clip.averageAngularSpeed.ToString("F3"))
                .Append("AverageDuration: ").AppendLine(clip.averageDuration.ToString("F3"))
                .Append("IsHumanMotion: ").AppendLine(clip.isHumanMotion.ToString());

            // Event
            descBuilder.AppendLine(LINE);
            var events = clip.events;
            descBuilder.AppendLine(
                events.Length == 0
                    ? "No Event"
                    : (events.Length == 1 ? "1 Event:" : $"{events.Length.ToString()} Events:")
            );
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                var evtPosition = evt.time / clip.length * 100;
                descBuilder.AppendLine($"  #{(i + 1).ToString()} {evtPosition.ToString("F2")}% {evt.functionName}");
            }
        }
    }
}