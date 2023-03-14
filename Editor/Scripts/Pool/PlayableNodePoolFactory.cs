using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEngine.Playables;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableNodePoolFactory
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<Type, IPlayableNodePool> _nodePoolTable =
            new Dictionary<Type, IPlayableNodePool>(5);


        public PlayableNodePoolFactory(UGraphView graphView)
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

        public PlayableNode Alloc(Playable playable)
        {
            var nodePool = GetOrCreateNodePool(playable);
            return nodePool.Alloc(playable);
        }

        public bool IsNodeActive(Playable playable)
        {
            if (!playable.IsValid())
            {
                return false;
            }

            var nodePool = GetOrCreateNodePool(playable);
            return nodePool.IsNodeActive(playable);
        }

        public PlayableNode GetActiveNode(Playable playable)
        {
            var nodePool = GetOrCreateNodePool(playable);
            return nodePool.GetActiveNode(playable);
        }

        public bool TryGetActiveNode(Playable playable, out PlayableNode node)
        {
            if (!playable.IsValid())
            {
                node = null;
                return false;
            }

            var nodePool = GetOrCreateNodePool(playable);
            return nodePool.TryGetActiveNode(playable, out node);
        }

        public IEnumerable<PlayableNode> GetActiveNodes()
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


        private IPlayableNodePool GetOrCreateNodePool(Playable playable)
        {
            var playableType = playable.GetPlayableType();
            if (_nodePoolTable.TryGetValue(playableType, out var nodePool))
            {
                return nodePool;
            }

            if (playableType == typeof(AnimationClipPlayable))
            {
                nodePool = new PlayableNodePool<AnimationClipPlayableNode>(_graphView);
                _nodePoolTable.Add(playableType, nodePool);
                return nodePool;
            }

            if (playableType == typeof(AnimationLayerMixerPlayable))
            {
                nodePool = new PlayableNodePool<AnimationLayerMixerPlayableNode>(_graphView);
                _nodePoolTable.Add(playableType, nodePool);
                return nodePool;
            }

            if (playableType == typeof(AnimationScriptPlayable))
            {
                nodePool = new PlayableNodePool<AnimationScriptPlayableNode>(_graphView);
                _nodePoolTable.Add(playableType, nodePool);
                return nodePool;
            }

            if (playableType == typeof(AudioClipPlayable))
            {
                nodePool = new PlayableNodePool<AudioClipPlayableNode>(_graphView);
                _nodePoolTable.Add(playableType, nodePool);
                return nodePool;
            }

            // Default node pool
            nodePool = new PlayableNodePool<PlayableNode>(_graphView);
            _nodePoolTable.Add(playableType, nodePool);
            return nodePool;
        }
    }
}