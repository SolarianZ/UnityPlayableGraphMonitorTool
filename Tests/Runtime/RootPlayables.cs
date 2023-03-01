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
    public class RootPlayables : MonoBehaviour
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
            _graph = PlayableGraph.Create(nameof(RootPlayables));

            // Outputs
            var animator = GetComponent<Animator>();
            var animOutput0 = AnimationPlayableOutput.Create(_graph, "AnimOutput0", animator);
            var animOutput1 = AnimationPlayableOutput.Create(_graph, "AnimOutput1", animator);
            var animOutput2 = AnimationPlayableOutput.Create(_graph, "AnimOutput2", animator);

            // Playable A
            var playableA = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableA.GetHandle(), "A");

            // Playable B
            var playableB = AnimationMixerPlayable.Create(_graph);
            playableB.SetOutputCount(2);
            _extraLabelTable.Add(playableB.GetHandle(), "B");

            // Playable C
            var playableC = AnimationMixerPlayable.Create(_graph);
            _extraLabelTable.Add(playableC.GetHandle(), "C");

            // Playable D
            var playableD = AnimationMixerPlayable.Create(_graph);
            playableD.SetOutputCount(2);
            _extraLabelTable.Add(playableD.GetHandle(), "D");

            // Playable E
            var playableE = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableE.GetHandle(), "E");

            // Connection
            // A0 -> Output0
            // B0 -> A0
            // B1 -> Output1
            animOutput0.SetSourcePlayable(playableA, 0);
            playableA.ConnectInput(0, playableB, 0, 1f);
            animOutput1.SetSourcePlayable(playableB, 1);
            // C0 -> null
            // D0 -> Output2
            // D1 -> E1
            animOutput2.SetSourcePlayable(playableD, 0);
            playableE.ConnectInput(0, playableD, 1, 1f);

            _graph.Play();

            var rootPlayableCount = _graph.GetRootPlayableCount();
            for (int i = 0; i < rootPlayableCount; i++)
            {
                var rootPlayable = _graph.GetRootPlayable(i);
                var rootPlayableLabel = _extraLabelTable[rootPlayable.GetHandle()];
                Debug.Log($"Root Playable: Index={i}, Label={rootPlayableLabel}");
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