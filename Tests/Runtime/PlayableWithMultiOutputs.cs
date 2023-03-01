using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using GBG.PlayableGraphMonitor.Editor;
#endif

namespace GBG.PlayableGraphMonitor.Tests
{
    [RequireComponent(typeof(Animator))]
    public class PlayableWithMultiOutputs : MonoBehaviour
    {
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
            _graph = PlayableGraph.Create(nameof(PlayableWithMultiOutputs));

            // Outputs
            var animator = GetComponent<Animator>();
            var animOutput0 = AnimationPlayableOutput.Create(_graph, "AnimOutput0", animator);
            var animOutput1 = AnimationPlayableOutput.Create(_graph, "AnimOutput1", animator);
            var animOutput2 = AnimationPlayableOutput.Create(_graph, "AnimOutput2", animator);

            // A0 -> 0
            // A1 -> 1
            // B0 -> 2
            // B2 -> A0
            // C0 -> B0
            // D1 -> A1
            // D2 -> B1

            // Playable A
            var playableA = AnimationMixerPlayable.Create(_graph, 2);
            playableA.SetOutputCount(2);
            _extraLabelTable.Add(playableA.GetHandle(), "A");
            animOutput0.SetSourcePlayable(playableA, 0);
            animOutput1.SetSourcePlayable(playableA, 1);

            // Playable B
            var playableB = AnimationMixerPlayable.Create(_graph, 2);
            playableB.SetOutputCount(4);
            _extraLabelTable.Add(playableB.GetHandle(), "B");
            playableA.ConnectInput(0, playableB, 2, 0.5f);
            animOutput2.SetSourcePlayable(playableB, 0);

            // Playable C
            var playableC = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableC.GetHandle(), "C");
            playableB.ConnectInput(0, playableC, 0, 0.5f);
            // Playable D
            var playableD = AnimationMixerPlayable.Create(_graph, 1);
            playableD.SetOutputCount(3);
            _extraLabelTable.Add(playableD.GetHandle(), "D");
            playableA.ConnectInput(1, playableD, 1, 0.5f);
            playableB.ConnectInput(1, playableD, 2, 0.5f);

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