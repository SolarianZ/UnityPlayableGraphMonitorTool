using System.Text;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;
#if !UNITY_2021_1_OR_NEWER
using UnityEditor.UIElements; // ProgressBar
#endif

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public sealed class AnimationClipPlayableNode : PlayableNode
    {
        public override string title
        {
            get => _mainTitle;
            set
            {
                _mainTitle = value;
                var clipPlayable = (AnimationClipPlayable)Playable;
                var clip = clipPlayable.GetAnimationClip();
                var clipName = clip ? clip.name : "None";
                base.title = $"{_mainTitle}\n({clipName})";
            }
        }

        private string _mainTitle;

        private readonly ProgressBar _progressBar;


        public AnimationClipPlayableNode(Playable playable) : base(playable)
        {
            title = Playable.GetPlayableType().Name;
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.maxWidth = 220;

            _progressBar = new ProgressBar();
            // insert between title and port container
            titleContainer.parent.Insert(1, _progressBar);
        }

        public override void Update()
        {
            if (Playable.IsValid())
            {
                var clipPlayable = (AnimationClipPlayable)Playable;
                var clip = clipPlayable.GetAnimationClip();
                var duration = clip ? clip.length : float.PositiveInfinity;
                var progress = (float)(Playable.GetTime() / duration) % 1.0f * 100;
                _progressBar.SetValueWithoutNotify(progress);
                _progressBar.MarkDirtyRepaint();
            }

            base.Update();
        }


        protected override void AppendStateDescriptions(StringBuilder descBuilder)
        {
            base.AppendStateDescriptions(descBuilder);

            if (Playable.IsValid())
            {
                var clipPlayable = (AnimationClipPlayable)Playable;
                descBuilder.Append("ApplyFootIK: ").AppendLine(clipPlayable.GetApplyFootIK().ToString())
                    .Append("ApplyPlayableIK: ").AppendLine(clipPlayable.GetApplyPlayableIK().ToString())
                    .AppendLine();

                var clip = clipPlayable.GetAnimationClip();
                descBuilder.Append("Clip: ").AppendLine(clip ? clip.name : "None");
                if (clip)
                {
                    descBuilder.Append("IsLooping: ").AppendLine(clip.isLooping.ToString())
                        .Append("Length: ").Append(clip.length.ToString("F3")).AppendLine("(s)");
                }
            }
        }
    }
}
