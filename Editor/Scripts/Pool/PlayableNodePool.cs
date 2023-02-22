using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Pool;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableNodePool
    {
        private readonly UGraphView _container;

        private readonly ObjectPool<PlayableNode_New> _nodePool;

        private readonly List<PlayableNode_New> _activeNodes = new List<PlayableNode_New>();


        public PlayableNodePool(UGraphView container)
        {
            _container = container;
            _nodePool = new ObjectPool<PlayableNode_New>(
                CreateNode, ActiveNode, RecycleNode, DestroyNode
            );
        }

        public PlayableNode_New Alloc()
        {
            var node = _nodePool.Get();
            _activeNodes.Add(node);

            return node;
        }

        public void Recycle(PlayableNode_New node)
        {
            _activeNodes.Remove(node);
            _nodePool.Release(node);
        }

        public void RecycleAllActiveNodes()
        {
            for (int i = _activeNodes.Count - 1; i >= 0; i--)
            {
                _activeNodes.RemoveAt(i);
                _nodePool.Release(_activeNodes[i]);
            }
        }


        // todo: Create concrete node by Playable type
        private PlayableNode_New CreateNode()
        {
            return new PlayableNode_New();
        }

        private void ActiveNode(PlayableNode_New node)
        {
            _container.AddElement(node);
        }

        private void RecycleNode(PlayableNode_New node)
        {
            _container.RemoveElement(node);
            node.Clean();
        }

        private void DestroyNode(PlayableNode_New node)
        {
        }
    }
}