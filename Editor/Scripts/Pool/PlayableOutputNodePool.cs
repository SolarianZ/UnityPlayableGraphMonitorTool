using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using UnityEngine.Playables;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class PlayableOutputNodePool
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<PlayableOutputHandle, PlayableOutputNode> _activePlayableNodeTable =
            new Dictionary<PlayableOutputHandle, PlayableOutputNode>();

        private readonly Dictionary<PlayableOutputHandle, PlayableOutputNode> _dormantPlayableNodeTable =
            new Dictionary<PlayableOutputHandle, PlayableOutputNode>();


        public PlayableOutputNodePool(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public PlayableOutputNode GetActiveNode(PlayableOutput playable)
        {
            return _activePlayableNodeTable[playable.GetHandle()];
        }

        public IEnumerable<PlayableOutputNode> GetActiveNodes()
        {
            return _activePlayableNodeTable.Values;
        }

        public PlayableOutputNode Alloc(PlayableOutput playable)
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
                node = new PlayableOutputNode();
                _graphView.AddElement(node);
            }

            _activePlayableNodeTable.Add(handle, node);

            return node;
        }

        public void Recycle(PlayableOutputNode node)
        {
            node.Release();
            var handle = node.PlayableOutput.GetHandle();
            _activePlayableNodeTable.Remove(handle);

            // _dormantPlayableNodeTable.TryAdd(handle, node); // Unavailable in Unity 2019
            _dormantPlayableNodeTable[handle] = node;
        }

        public void RecycleAllActiveNodes()
        {
            foreach (var node in _activePlayableNodeTable.Values)
            {
                node.Release();
                var handle = node.PlayableOutput.GetHandle();
                _dormantPlayableNodeTable.Add(handle, node);
            }

            _activePlayableNodeTable.Clear();
        }

        public void RemoveDormantNodesFromView()
        {
            _graphView.DeleteElements(_dormantPlayableNodeTable.Values);
            _dormantPlayableNodeTable.Clear();
        }
    }
}