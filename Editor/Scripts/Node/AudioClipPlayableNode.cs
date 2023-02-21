using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace GBG.PlayableGraphMonitor.Editor.Node
{
    public sealed class AudioClipPlayableNode : PlayableNode
    {
        private readonly ObjectField _clipField;

        private readonly ProgressBar _progressBar;


        public AudioClipPlayableNode(Playable playable) : base(playable)
        {
            title = Playable.GetPlayableType().Name;
            var titleLabel = titleContainer.Q<Label>(name: "title-label");
            titleLabel.style.maxWidth = 220;

            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;

            _progressBar = new ProgressBar();
            banner.Add(_progressBar);

            _clipField = new ObjectField()
            {
                objectType = typeof(AudioClip),
                value = ((AudioClipPlayable)Playable).GetClip(),
            };
            // clipField.SetEnabled(false);
            var clipFieldSelector = _clipField.Q(className: "unity-object-field__selector");
            clipFieldSelector.SetEnabled(false);
            banner.Add(_clipField);
        }

        public override void Update()
        {
            if (Playable.IsValid())
            {
                var clipPlayable = (AudioClipPlayable)Playable;
                var clip = clipPlayable.GetClip();
                _clipField.SetValueWithoutNotify(clip);

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
                var clipPlayable = (AudioClipPlayable)Playable;
                descBuilder.Append("Looped: ").AppendLine(clipPlayable.GetLooped().ToString());

                var clip = clipPlayable.GetClip();
                descBuilder.Append("Clip: ").AppendLine(clip ? clip.name : "None");
                if (clip)
                {
                    descBuilder.Append("Length: ").Append(clip.length.ToString("F3")).AppendLine("(s)");
                }
            }
        }
    }
}