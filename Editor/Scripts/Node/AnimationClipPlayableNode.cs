using System.Text;
using GBG.PlayableGraphMonitor.Editor.GraphView;
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
            // clipField.SetEnabled(false);
            var clipFieldSelector = _clipField.Q(className: "unity-object-field__selector");
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

            if (updateContext.ShowClipProgressBar)
            {
                _progressBar.style.display = DisplayStyle.Flex;
                if (clip)
                {
                    if (clip.isLooping)
                    {
                        var progress = (float)(Playable.GetTime() / clip.length) % 1.0f * 100;
                        // Expensive operations
                        _progressBar.SetValueWithoutNotify(progress);
                    }
                    else
                    {
                        var progress = (float)(Playable.GetTime() / clip.length) * 100;
                        progress = Mathf.Clamp(progress, 0, 100);
                        // Expensive operations
                        _progressBar.SetValueWithoutNotify(progress);
                    }
                }
                else
                {
                    // Expensive operations
                    _progressBar.SetValueWithoutNotify(0);
                }
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
                .Append("    Center: ").AppendLine(clip.localBounds.center.ToString())
                .Append("    Extends: ").AppendLine(clip.localBounds.extents.ToString())
                .Append("ApparentSpeed: ").AppendLine(clip.apparentSpeed.ToString("F3")) // Motion.cs
                .Append("AverageSpeed: ").AppendLine(clip.averageSpeed.ToString())
                .Append("AverageAngularSpeed: ").AppendLine(clip.averageAngularSpeed.ToString("F3"))
                .Append("AverageDuration: ").AppendLine(clip.averageDuration.ToString("F3"))
                .Append("IsHumanMotion: ").AppendLine(clip.isHumanMotion.ToString());
        }
    }
}