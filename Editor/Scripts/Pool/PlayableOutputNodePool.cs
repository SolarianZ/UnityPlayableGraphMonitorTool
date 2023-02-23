using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Pool;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableOutputNodePool
    {
        private readonly UGraphView _container;
        
        private readonly ObjectPool<PlayableOutputNode_New> _nodePool;

        private readonly List<PlayableOutputNode_New> _activeNodes = new List<PlayableOutputNode_New>();


        public PlayableOutputNodePool(UGraphView container)
        {
            _container = container;
            _nodePool = new ObjectPool<PlayableOutputNode_New>(
                CreateNode, ActiveNode, RecycleNode, DestroyNode
            );
        }

        public PlayableOutputNode_New Alloc()
        {
            var node = _nodePool.Get();
            _activeNodes.Add(node);

            return node;
        }

        public void Recycle(PlayableOutputNode_New node)
        {
            _activeNodes.Remove(node);
            _nodePool.Release(node);
        }

        public void RecycleAllActiveNodes()
        {
            for (int i = _activeNodes.Count - 1; i >= 0; i--)
            {
                _nodePool.Release(_activeNodes[i]);
                _activeNodes.RemoveAt(i);
            }
        }


        private PlayableOutputNode_New CreateNode()
        {
            return new PlayableOutputNode_New();
        }

        private void ActiveNode(PlayableOutputNode_New node)
        {
            _container.AddElement(node);
        }

        private void RecycleNode(PlayableOutputNode_New node)
        {
            _container.RemoveElement(node);
            node.Clean();
        }

        private void DestroyNode(PlayableOutputNode_New node)
        {
        }
    }
}