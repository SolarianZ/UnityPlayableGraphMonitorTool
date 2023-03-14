using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Experimental.Playables;
using UnityEngine.Playables;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableOutputNodePoolFactory
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<Type, IPlayableOutputNodePool> _nodePoolTable =
            new Dictionary<Type, IPlayableOutputNodePool>(4);


        public PlayableOutputNodePoolFactory(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public void RecycleAllActiveNodes()
        {
            foreach (var nodePool in _nodePoolTable.Values)
            {
                nodePool.RecycleAllActiveNodes();
            }
        }

        public PlayableOutputNode Alloc(PlayableOutput playableOutput)
        {
            var nodePool = GetOrCreateNodePool(playableOutput);
            return nodePool.Alloc(playableOutput);
        }

        public bool IsNodeActive(PlayableOutput playableOutput)
        {
            if (!playableOutput.IsOutputValid())
            {
                return false;
            }

            var nodePool = GetOrCreateNodePool(playableOutput);
            return nodePool.IsNodeActive(playableOutput);
        }

        public PlayableOutputNode GetActiveNode(PlayableOutput playableOutput)
        {
            var nodePool = GetOrCreateNodePool(playableOutput);
            return nodePool.GetActiveNode(playableOutput);
        }

        public bool TryGetActiveNode(PlayableOutput playableOutput, out PlayableOutputNode node)
        {
            if (!playableOutput.IsOutputValid())
            {
                node = null;
                return false;
            }

            var nodePool = GetOrCreateNodePool(playableOutput);
            return nodePool.TryGetActiveNode(playableOutput, out node);
        }

        public IEnumerable<PlayableOutputNode> GetActiveNodes()
        {
            foreach (var nodePool in _nodePoolTable.Values)
            {
                foreach (var node in nodePool.GetActiveNodes())
                {
                    yield return node;
                }
            }
        }

        public void RemoveDormantNodesFromView()
        {
            foreach (var nodePool in _nodePoolTable.Values)
            {
                nodePool.RemoveDormantNodesFromView();
            }
        }


        private IPlayableOutputNodePool GetOrCreateNodePool(PlayableOutput playable)
        {
            var playableOutputType = playable.GetPlayableOutputType();
            if (_nodePoolTable.TryGetValue(playableOutputType, out var nodePool))
            {
                return nodePool;
            }


            if (playableOutputType == typeof(AnimationPlayableOutput))
            {
                nodePool = new PlayableOutputNodePool<AnimationPlayableOutputNode>(_graphView);
                _nodePoolTable.Add(playableOutputType, nodePool);
                return nodePool;
            }

            if (playableOutputType == typeof(AudioPlayableOutput))
            {
                nodePool = new PlayableOutputNodePool<AudioPlayableOutputNode>(_graphView);
                _nodePoolTable.Add(playableOutputType, nodePool);
                return nodePool;
            }

            if (playableOutputType == typeof(TexturePlayableOutput))
            {
                nodePool = new PlayableOutputNodePool<TexturePlayableOutputNode>(_graphView);
                _nodePoolTable.Add(playableOutputType, nodePool);
                return nodePool;
            }

            // if (playableOutputType == typeof(ScriptPlayableOutput))
            // {
            // }

            // Default node pool
            nodePool = new PlayableOutputNodePool<PlayableOutputNode>(_graphView);
            _nodePoolTable.Add(playableOutputType, nodePool);
            return nodePool;
        }
    }
}