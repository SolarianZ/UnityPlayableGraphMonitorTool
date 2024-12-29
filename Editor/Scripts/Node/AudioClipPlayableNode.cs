using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor;
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

            var clipPlayable = (AudioClipPlayable)Playable;
            var clip = clipPlayable.GetClip();
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

                    if (clipPlayable.GetLooped())
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

            var clipPlayable = (AudioClipPlayable)Playable;
            GUILayout.Label(LINE);
            var clip = clipPlayable.GetClip();
            if (!clip)
            {
                GUILayout.Label("Clip: None");
                return;
            }

            EditorGUILayout.ObjectField("Clip:", clip, typeof(AudioClip), true);
            GUILayout.Label($"Length: {clip.length:F3}(s)");
            EditorGUILayout.Toggle("Looped:", clipPlayable.GetLooped());
            GUILayout.Label($"Channels: {clip.channels}");
            GUILayout.Label($"Ambisonic: {clip.ambisonic}");
            GUILayout.Label($"Frequency: {clip.frequency}");
            GUILayout.Label($"Samples: {clip.samples}");
            GUILayout.Label($"LoadState: {clip.loadState}");
            GUILayout.Label($"LoadType: {clip.loadType}");
            GUILayout.Label($"LoadInBackground: {clip.loadInBackground}");
            GUILayout.Label($"PreloadAudioData: {clip.preloadAudioData}");
        }
    }
}