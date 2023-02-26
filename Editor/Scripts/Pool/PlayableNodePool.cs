using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableNodePool<T> : IPlayableNodePool where T : PlayableNode, new()
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<PlayableHandle, T> _activePlayableNodeTable =
            new Dictionary<PlayableHandle, T>();

        private readonly Dictionary<PlayableHandle, T> _dormantPlayableNodeTable =
            new Dictionary<PlayableHandle, T>();


        public PlayableNodePool(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public bool IsNodeActive(Playable playable)
        {
            return _activePlayableNodeTable.ContainsKey(playable.GetHandle());
        }

        public T GetActiveNode(Playable playable)
        {
            return _activePlayableNodeTable[playable.GetHandle()];
        }

        public bool TryGetActiveNode(Playable playable, out T node)
        {
            return _activePlayableNodeTable.TryGetValue(playable.GetHandle(), out node);
        }

        public IEnumerable<T> GetActiveNodes()
        {
            return _activePlayableNodeTable.Values;
        }

        public T Alloc(Playable playable)
        {
            var handle = playable.GetHandle();
            if (_activePlayableNodeTable.TryGetValue(handle, out var node))
            {
                return node;
            }

            // if (!_dormantPlayableNodeTable.Remove(handle, out node)) // Unavailable in Unity 2019
            // {
            //     node = new PlayableNode_New();
            //     _graphView.AddElement(node);
            // }
            if (_dormantPlayableNodeTable.TryGetValue(handle, out node))
            {
                _dormantPlayableNodeTable.Remove(handle);
            }
            else
            {
                node = new T();
                _graphView.AddElement(node);
            }

            _activePlayableNodeTable.Add(handle, node);

            return node;
        }

        public void Recycle(T node)
        {
            node.Release();
            var handle = node.Playable.GetHandle();
            _activePlayableNodeTable.Remove(handle);

            // _dormantPlayableNodeTable.TryAdd(handle, node); // Unavailable in Unity 2019
            _dormantPlayableNodeTable[handle] = node;
        }

        public void RecycleAllActiveNodes()
        {
            foreach (var node in _activePlayableNodeTable.Values)
            {
                node.Release();
                var handle = node.Playable.GetHandle();
                _dormantPlayableNodeTable.Add(handle, node);
            }

            _activePlayableNodeTable.Clear();
        }

        public void RemoveDormantNodesFromView()
        {
            _graphView.DeleteElements(_dormantPlayableNodeTable.Values);
            _dormantPlayableNodeTable.Clear();
        }


        #region Interface

        PlayableNode IPlayableNodePool.GetActiveNode(Playable playable)
        {
            return GetActiveNode(playable);
        }

        bool IPlayableNodePool.TryGetActiveNode(Playable playable, out PlayableNode node)
        {
            if (TryGetActiveNode(playable, out var tNode))
            {
                node = tNode;
                return true;
            }

            node = null;
            return false;
        }

        IEnumerable<PlayableNode> IPlayableNodePool.GetActiveNodes()
        {
            return GetActiveNodes();
        }

        PlayableNode IPlayableNodePool.Alloc(Playable playable)
        {
            return Alloc(playable);
        }

        void IPlayableNodePool.Recycle(PlayableNode node)
        {
            Recycle((T)node);
        }

        #endregion
    }
}