using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;
using UnityEngine.Playables;
#if UNITY_EDITOR
using GBG.PlayableGraphMonitor.Editor;
#endif


namespace GBG.PlayableGraphMonitor.Tests
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayableDirector))]
    public class MultiPlayableOutput : MonoBehaviour
    {
        public bool ConnectTimelinePlayableToMixer = true;

        public bool ExtraLabel;

        private PlayableGraph _graph;

        private readonly Dictionary<PlayableHandle, string> _extraLabelTable = new Dictionary<PlayableHandle, string>();


        private void UpdateNodeExtraLabelTable()
        {
#if UNITY_EDITOR
            var table = ExtraLabel ? _extraLabelTable : null;
            PlayableGraphMonitorWindow.TrySetNodeExtraLabelTable(table);
#endif
        }


        private void OnValidate()
        {
            UpdateNodeExtraLabelTable();
        }

        private void Start()
        {
            var director = GetComponent<PlayableDirector>();
            _graph = director.playableGraph;

            // Root TimelinePlayable
            var rootPlayable = _graph.GetRootPlayable(0);
            Assert.AreEqual(rootPlayable.GetPlayableType().Name, "TimelinePlayable");
            rootPlayable.SetOutputCount(8);


            // Outputs
            var animator = GetComponent<Animator>();
            // ReSharper disable UnusedVariable
            var animOutput0 = AnimationPlayableOutput.Create(_graph, "CustomAnimOutput0", animator);
            var animOutput1 = AnimationPlayableOutput.Create(_graph, "CustomAnimOutput1", animator);
            var animOutput2 = AnimationPlayableOutput.Create(_graph, "CustomAnimOutput2", animator);
            var animOutput3 = AnimationPlayableOutput.Create(_graph, "CustomAnimOutput3", animator);
            // ReSharper restore UnusedVariable


            // Playables
            var mixer0 = AnimationMixerPlayable.Create(_graph);
            _extraLabelTable.Add(mixer0.GetHandle(), "Mixer0");
            animOutput1.SetSourcePlayable(mixer0);

            var mixer1 = AnimationMixerPlayable.Create(_graph);
            _extraLabelTable.Add(mixer1.GetHandle(), "Mixer1");
            if (ConnectTimelinePlayableToMixer)
            {
                mixer1.AddInput(rootPlayable, 6, 1f);
            }

            UpdateNodeExtraLabelTable();
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}