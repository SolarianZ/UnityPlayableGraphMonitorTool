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
    public class CircularReference : MonoBehaviour
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
            _graph = PlayableGraph.Create(nameof(CircularReference));

            // Playable A
            var playableA = AnimationMixerPlayable.Create(_graph, 2);
            playableA.SetOutputCount(3);
            _extraLabelTable.Add(playableA.GetHandle(), "A");

            // Playable B
            var playableB = AnimationMixerPlayable.Create(_graph, 2);
            playableB.SetOutputCount(2);
            _extraLabelTable.Add(playableB.GetHandle(), "B");

            // Playable C
            var playableC = AnimationMixerPlayable.Create(_graph, 2);
            playableC.SetOutputCount(2);
            _extraLabelTable.Add(playableC.GetHandle(), "C");

            // Connection
            // A0 -> AnimationPlayableOutput
            // A1 -> B0
            // A2 -> C0
            // B0 -> A0
            // B1 -> C1
            // C0 -> A1
            // C1 -> B1
            playableA.ConnectInput(0, playableB, 0, 1f);
            playableA.ConnectInput(1, playableC, 0, 1f);
            playableB.ConnectInput(0, playableA, 1, 1f);
            playableB.ConnectInput(1, playableC, 1, 1f);
            playableC.ConnectInput(0, playableA, 2, 1f);
            playableC.ConnectInput(1, playableB, 1, 1f);

            // Outputs
            var animator = GetComponent<Animator>();
            var animOutput = AnimationPlayableOutput.Create(_graph, "AnimOutput", animator);
            animOutput.SetSourcePlayable(playableA, 0);

            // Playable D and E, with cycle and do not connect to PlayableOutput
            // D and E will not show in PlayableGraph Monitor
            var playableD = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableD.GetHandle(), "D");
            var playableE = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableE.GetHandle(), "E");
            playableD.ConnectInput(0, playableE, 0, 1f);
            playableE.ConnectInput(0, playableD, 0, 1f);
            Debug.LogError("D and E will not show in PlayableGraph Monitor!", this);

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