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
    public struct EmptyAnimationJob : IAnimationJob
    {
        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        public void ProcessAnimation(AnimationStream stream)
        {
        }
    }

    [RequireComponent(typeof(Animator))]
    public class LargeAnimationGraph : MonoBehaviour
    {
        public AnimationClip Clip;

        public byte Depth = 4;

        public byte Branch = 3;

        public bool ExtraLabel;

        private PlayableGraph _graph;

        private readonly Dictionary<PlayableHandle, string> _extraLabelTable = new Dictionary<PlayableHandle, string>();


        public void RecreatePlayableGraph()
        {
            _extraLabelTable.Clear();

            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _graph = PlayableGraph.Create(nameof(LargeAnimationGraph));

            var animOutput = AnimationPlayableOutput.Create(_graph, "AnimOutput", GetComponent<Animator>());
            if (Depth == 0)
            {
                _graph.Play();
                return;
            }

            if (Depth == 1)
            {
                var animPlayable = AnimationClipPlayable.Create(_graph, Clip);
                _extraLabelTable.Add(animPlayable.GetHandle(), "Depth=1,Branch=1");
                var scriptPlayable = AnimationScriptPlayable.Create(_graph, new EmptyAnimationJob());
                _extraLabelTable.Add(scriptPlayable.GetHandle(), "Depth=0,Branch=0");
                scriptPlayable.AddInput(animPlayable, 0, 1f);
                animOutput.SetSourcePlayable(scriptPlayable);
                _graph.Play();

                return;
            }

            var rootMixer = AnimationMixerPlayable.Create(_graph);
            animOutput.SetSourcePlayable(rootMixer);
            _extraLabelTable.Add(rootMixer.GetHandle(), "Depth=0,Branch=0");

            CreatePlayableTree(rootMixer, 1);

            _graph.Play();
        }

        private void CreatePlayableTree(Playable parent, int parentDepth)
        {
            // Don't handle root node
            Assert.IsTrue(parent.IsValid());
            Assert.IsTrue(parentDepth > 0);

            // No leaf node
            if (parentDepth == Depth)
            {
                return;
            }

            // Leaf nodes
            if (parentDepth == Depth - 1)
            {
                for (int b = 0; b < Branch; b++)
                {
                    var animPlayable = AnimationClipPlayable.Create(_graph, Clip);
                    _extraLabelTable.Add(animPlayable.GetHandle(), $"Depth={parentDepth + 2},Branch={b}");
                    var scriptPlayable = AnimationScriptPlayable.Create(_graph, new EmptyAnimationJob());
                    _extraLabelTable.Add(scriptPlayable.GetHandle(), $"Depth={parentDepth + 1},Branch={b}");
                    scriptPlayable.AddInput(animPlayable, 0, 1f);

                    parent.AddInput(scriptPlayable, 0, 1f / Branch);
                }

                return;
            }

            for (int b = 0; b < Branch; b++)
            {
                var mixer = AnimationMixerPlayable.Create(_graph);
                parent.AddInput(mixer, 0, 1f / Branch);
                _extraLabelTable.Add(mixer.GetHandle(), $"Depth={parentDepth + 1},Branch={b}");

                CreatePlayableTree(mixer, parentDepth + 1);
            }
        }

        private void UpdateNodeExtraLabelTable()
        {
#if UNITY_EDITOR
            var table = ExtraLabel ? _extraLabelTable : null;
            PlayableGraphMonitorWindow.TrySetNodeExtraLabelTable(table);
#endif
        }


        private void OnValidate()
        {
            if (_graph.IsValid())
            {
                RecreatePlayableGraph();
                UpdateNodeExtraLabelTable();
            }
        }

        private void Start()
        {
            RecreatePlayableGraph();
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