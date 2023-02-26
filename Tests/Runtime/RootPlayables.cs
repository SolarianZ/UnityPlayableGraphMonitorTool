﻿using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace GBG.PlayableGraphMonitor.Tests
{
    [RequireComponent(typeof(Animator))]
    public class RootPlayables : MonoBehaviour
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
            _graph = PlayableGraph.Create(nameof(RootPlayables));

            // Outputs
            var animator = GetComponent<Animator>();
            var animOutput0 = AnimationPlayableOutput.Create(_graph, "AnimOutput0", animator);
            var animOutput1 = AnimationPlayableOutput.Create(_graph, "AnimOutput1", animator);

            // Playable A
            var playableA = AnimationMixerPlayable.Create(_graph, 1);
            _extraLabelTable.Add(playableA.GetHandle(), "A");

            // Playable B
            var playableB = AnimationMixerPlayable.Create(_graph);
            playableB.SetOutputCount(2);
            _extraLabelTable.Add(playableB.GetHandle(), "B");

            // Connection
            // A0 -> Output0
            // B0 -> A0
            // B1 -> Output1
            animOutput0.SetSourcePlayable(playableA, 0);
            playableA.ConnectInput(0, playableB, 0, 1f);
            animOutput1.SetSourcePlayable(playableB, 1);

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