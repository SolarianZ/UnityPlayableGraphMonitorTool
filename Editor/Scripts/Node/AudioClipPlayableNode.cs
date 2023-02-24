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


        public AudioClipPlayableNode()
        {
            var banner = mainContainer.Q("divider");
            banner.style.height = StyleKeyword.Auto;

            _progressBar = new ProgressBar();
            banner.Add(_progressBar);

            _clipField = new ObjectField
            {
                objectType = typeof(AudioClip),
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

            var clipPlayable = (AudioClipPlayable)Playable;
            var clip = clipPlayable.GetClip();
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

            var clipPlayable = (AudioClipPlayable)Playable;
            descBuilder.AppendLine(LINE);
            var clip = clipPlayable.GetClip();
            if (!clip)
            {
                descBuilder.AppendLine("Clip: None");
                return;
            }

            descBuilder.Append("Clip: ").AppendLine(clip.name)
                .Append("Length: ").Append(clip.length.ToString("F3")).AppendLine("(s)")
                .Append("Looped: ").AppendLine(clipPlayable.GetLooped().ToString())
                .Append("Channels: ").AppendLine(clip.channels.ToString())
                .Append("Ambisonic: ").AppendLine(clip.ambisonic.ToString())
                .Append("Frequency: ").AppendLine(clip.frequency.ToString())
                .Append("Samples: ").AppendLine(clip.samples.ToString())
                .Append("LoadState: ").AppendLine(clip.loadState.ToString())
                .Append("LoadType: ").AppendLine(clip.loadType.ToString())
                .Append("LoadInBackground: ").AppendLine(clip.loadInBackground.ToString())
                .Append("PreloadAudioData: ").AppendLine(clip.preloadAudioData.ToString());
        }
    }
}