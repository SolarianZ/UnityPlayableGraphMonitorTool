using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableOutputNodePool<T> : IPlayableOutputNodePool where T : PlayableOutputNode, new()
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<PlayableOutputHandle, T> _activeNodeTable =
            new Dictionary<PlayableOutputHandle, T>();

        private readonly Dictionary<PlayableOutputHandle, T> _dormantNodeTable =
            new Dictionary<PlayableOutputHandle, T>();


        public PlayableOutputNodePool(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public bool IsNodeActive(PlayableOutput playableOutput)
        {
            return _activeNodeTable.ContainsKey(playableOutput.GetHandle());
        }

        public T GetActiveNode(PlayableOutput playableOutput)
        {
            return _activeNodeTable[playableOutput.GetHandle()];
        }

        public bool TryGetActiveNode(PlayableOutput playableOutput, out T node)
        {
            return _activeNodeTable.TryGetValue(playableOutput.GetHandle(), out node);
        }

        public IEnumerable<T> GetActiveNodes()
        {
            return _activeNodeTable.Values;
        }

        public T Alloc(PlayableOutput playable)
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
            var handle = node.PlayableOutput.GetHandle();
            _activeNodeTable.Remove(handle);

            // _dormantNodeTable.TryAdd(handle, node); // Unavailable in Unity 2019
            _dormantNodeTable[handle] = node;
        }

        public void RecycleAllActiveNodes()
        {
            foreach (var node in _activeNodeTable.Values)
            {
                node.Release();
                var handle = node.PlayableOutput.GetHandle();
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

        PlayableOutputNode IPlayableOutputNodePool.GetActiveNode(PlayableOutput playableOutput)
        {
            return GetActiveNode(playableOutput);
        }

        bool IPlayableOutputNodePool.TryGetActiveNode(PlayableOutput playableOutput, out PlayableOutputNode node)
        {
            if (TryGetActiveNode(playableOutput, out var tNode))
            {
                node = tNode;
                return true;
            }

            node = null;
            return false;
        }

        IEnumerable<PlayableOutputNode> IPlayableOutputNodePool.GetActiveNodes()
        {
            return GetActiveNodes();
        }

        PlayableOutputNode IPlayableOutputNodePool.Alloc(PlayableOutput playableOutput)
        {
            return Alloc(playableOutput);
        }

        void IPlayableOutputNodePool.Recycle(PlayableOutputNode node)
        {
            Recycle((T)node);
        }

        #endregion
    }
}