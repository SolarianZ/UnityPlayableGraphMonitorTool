using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Tests
{
    [RequireComponent(typeof(Animator))]
    public class CircularReference : MonoBehaviour
    {
        public bool ExtraLabel = false;

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
            _graph = PlayableGraph.Create(nameof(CircularReference));

            // Playable A
            var playableA = AnimationMixerPlayable.Create(_graph, 1);
            playableA.SetOutputCount(2);
            _extraLabelTable.Add(playableA.GetHandle(), "A");

            // Playable B
            var playableB = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableB.GetHandle(), "B");

            playableA.ConnectInput(0, playableB, 0, 1f);
            playableB.ConnectInput(0, playableA, 1, 1f);

            // Outputs
            var animator = GetComponent<Animator>();
            var animOutput = AnimationPlayableOutput.Create(_graph, "AnimOutput", animator);
            animOutput.SetSourcePlayable(playableA, 0);

            _graph.Play();

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