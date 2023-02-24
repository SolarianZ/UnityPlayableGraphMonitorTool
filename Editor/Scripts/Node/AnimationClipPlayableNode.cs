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

        protected override void OnUpdate(bool playableChanged)
        {
            base.OnUpdate(playableChanged);

            if (!Playable.IsValid())
            {
                return;
            }

            // TODO OPTIMIZABLE: Expensive operations
            
            var clipPlayable = (AnimationClipPlayable)Playable;
            var clip = clipPlayable.GetAnimationClip();
            _clipField.SetValueWithoutNotify(clip);

            var duration = clip ? clip.length : float.PositiveInfinity;
            var progress = (float)(Playable.GetTime() / duration) % 1.0f * 100;
            _progressBar.SetValueWithoutNotify(progress);
            _progressBar.MarkDirtyRepaint();
        }

        public override void Release()
        {
            base.Release();

            _clipField.SetValueWithoutNotify(null);
        }

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