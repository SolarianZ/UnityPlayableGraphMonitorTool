using System.Text;
using GBG.PlayableGraphMonitor.Editor.GraphView;
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
#if !UNITY_2021_1_OR_NEWER
            var progressBarBg = _progressBar.Q<VisualElement>(className: "unity-progress-bar__background");
            progressBarBg.style.height = 17;
#endif
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

        protected override void OnUpdate(PlayableGraphViewUpdateContext updateContext, bool playableChanged)
        {
            base.OnUpdate(updateContext, playableChanged);

            if (!Playable.IsValid())
            {
                return;
            }

            var clipPlayable = (AudioClipPlayable)Playable;
            var clip = clipPlayable.GetClip();
            _clipField.SetValueWithoutNotify(clip);

            if (updateContext.ShowClipProgressBar)
            {
                _progressBar.style.display = DisplayStyle.Flex;
                var duration = clip ? clip.length : float.PositiveInfinity;
                var progress = (float)(Playable.GetTime() / duration) % 1.0f * 100;
                // Expensive operations
                _progressBar.SetValueWithoutNotify(progress);
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