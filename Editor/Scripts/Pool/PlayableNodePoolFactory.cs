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

        private readonly Dictionary<Type, IPlayableNodePool> _playableNodePoolTable =
            new Dictionary<Type, IPlayableNodePool>(5);


        public PlayableNodePoolFactory(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public void RecycleAllActiveNodes()
        {
            foreach (var playableNodePool in _playableNodePoolTable.Values)
            {
                playableNodePool.RecycleAllActiveNodes();
            }
        }

        public PlayableNode Alloc(Playable playable)
        {
            var playableNodePool = GetOrCreatePlayableNodePool(playable);
            return playableNodePool.Alloc(playable);
        }

        public bool IsNodeActive(Playable playable)
        {
            if (!playable.IsValid())
            {
                return false;
            }

            var playableNodePool = GetOrCreatePlayableNodePool(playable);
            return playableNodePool.IsNodeActive(playable);
        }

        public PlayableNode GetActiveNode(Playable playable)
        {
            if (!playable.IsValid())
            {
                return null;
            }

            var playableNodePool = GetOrCreatePlayableNodePool(playable);
            return playableNodePool.GetActiveNode(playable);
        }

        public bool GetActiveNode(Playable playable, out PlayableNode node)
        {
            var playableNodePool = GetOrCreatePlayableNodePool(playable);
            return playableNodePool.TryGetActiveNode(playable, out node);
        }

        public IEnumerable<PlayableNode> GetActiveNodes()
        {
            foreach (var playableNodePool in _playableNodePoolTable.Values)
            {
                foreach (var playableNode in playableNodePool.GetActiveNodes())
                {
                    yield return playableNode;
                }
            }
        }

        public void RemoveDormantNodesFromView()
        {
            foreach (var playableNodePool in _playableNodePoolTable.Values)
            {
                playableNodePool.RemoveDormantNodesFromView();
            }
        }


        private IPlayableNodePool GetOrCreatePlayableNodePool(Playable playable)
        {
            var playableType = playable.GetPlayableType();
            if (_playableNodePoolTable.TryGetValue(playableType, out var playableNodePool))
            {
                return playableNodePool;
            }


            if (playableType == typeof(AnimationClipPlayable))
            {
                playableNodePool = new PlayableNodePool<AnimationClipPlayableNode>(_graphView);
                _playableNodePoolTable.Add(playableType, playableNodePool);
                return playableNodePool;
            }

            if (playableType == typeof(AnimationLayerMixerPlayable))
            {
                playableNodePool = new PlayableNodePool<AnimationLayerMixerPlayableNode>(_graphView);
                _playableNodePoolTable.Add(playableType, playableNodePool);
                return playableNodePool;
            }

            if (playableType == typeof(AnimationScriptPlayable))
            {
                playableNodePool = new PlayableNodePool<AnimationScriptPlayableNode>(_graphView);
                _playableNodePoolTable.Add(playableType, playableNodePool);
                return playableNodePool;
            }

            if (playableType == typeof(AudioClipPlayable))
            {
                playableNodePool = new PlayableNodePool<AudioClipPlayableNode>(_graphView);
                _playableNodePoolTable.Add(playableType, playableNodePool);
                return playableNodePool;
            }

            // Default node pool
            playableNodePool = new PlayableNodePool<PlayableNode>(_graphView);
            _playableNodePoolTable.Add(playableType, playableNodePool);
            return playableNodePool;
        }
    }
}