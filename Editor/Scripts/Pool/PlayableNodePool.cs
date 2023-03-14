using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableNodePool<T> : IPlayableNodePool where T : PlayableNode, new()
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<PlayableHandle, T> _activeNodeTable =
            new Dictionary<PlayableHandle, T>();

        private readonly Dictionary<PlayableHandle, T> _dormantNodeTable =
            new Dictionary<PlayableHandle, T>();


        public PlayableNodePool(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public bool IsNodeActive(Playable playable)
        {
            return _activeNodeTable.ContainsKey(playable.GetHandle());
        }

        public T GetActiveNode(Playable playable)
        {
            return _activeNodeTable[playable.GetHandle()];
        }

        public bool TryGetActiveNode(Playable playable, out T node)
        {
            return _activeNodeTable.TryGetValue(playable.GetHandle(), out node);
        }

        public IEnumerable<T> GetActiveNodes()
        {
            return _activeNodeTable.Values;
        }

        public T Alloc(Playable playable)
        {
            var handle = playable.GetHandle();
            if (_activeNodeTable.TryGetValue(handle, out var node))
            {
                return node;
            }

            // if (!_dormantNodeTable.Remove(handle, out node)) // Unavailable in Unity 2019
            // {
            //     node = new T();
            //     _graphView.AddElement(node);
            // }
            if (_dormantNodeTable.TryGetValue(handle, out node))
            {
                _dormantNodeTable.Remove(handle);
            }
            else
            {
                node = new T();
                _graphView.AddElement(node);
            }

            _activeNodeTable.Add(handle, node);

            return node;
        }

        public void Recycle(T node)
        {
            node.Release();
            var handle = node.Playable.GetHandle();
            _activeNodeTable.Remove(handle);

            // _dormantNodeTable.TryAdd(handle, node); // Unavailable in Unity 2019
            _dormantNodeTable[handle] = node;
        }

        public void RecycleAllActiveNodes()
        {
            foreach (var node in _activeNodeTable.Values)
            {
                node.Release();
                var handle = node.Playable.GetHandle();
                _dormantNodeTable.Add(handle, node);
            }

            _activeNodeTable.Clear();
        }

        public void RemoveDormantNodesFromView()
        {
            _graphView.DeleteElements(_dormantNodeTable.Values);
            _dormantNodeTable.Clear();
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