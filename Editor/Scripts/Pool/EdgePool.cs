using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Assertions;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UEdge = UnityEditor.Experimental.GraphView.Edge;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class EdgePool
    {
        private readonly UGraphView _graphView;

        private readonly Dictionary<int, UEdge> _activeEdgeTable = new Dictionary<int, UEdge>();

        private readonly Dictionary<int, UEdge> _dormantEdgeTable = new Dictionary<int, UEdge>();


        public EdgePool(UGraphView graphView)
        {
            _graphView = graphView;
        }

        public UEdge GetActiveEdge(Port inputPort, Port outputPort)
        {
            return _activeEdgeTable[GetEdgeKey(inputPort, outputPort)];
        }

        public IEnumerable<UEdge> GetActiveEdges()
        {
            return _activeEdgeTable.Values;
        }

        public UEdge Alloc(Port inputPort, Port outputPort)
        {
            var key = GetEdgeKey(inputPort, outputPort);
            if (_activeEdgeTable.TryGetValue(key, out var edge))
            {
                return edge;
            }

            // if (!_dormantPlayableNodeTable.Remove(key, out edge)) // Unavailable in Unity 2019
            // {
            //     edge = new UEdge();
            //     _graphView.AddElement(edge);
            // }
            if (_dormantEdgeTable.TryGetValue(key, out edge))
            {
                _dormantEdgeTable.Remove(key);
            }
            else
            {
                edge = new UEdge();
                var edgeCapabilities = edge.capabilities;
                edgeCapabilities &= ~Capabilities.Selectable;
                edgeCapabilities &= ~Capabilities.Collapsible;
                edgeCapabilities &= ~Capabilities.Movable;
                edgeCapabilities &= ~Capabilities.Deletable;
                edgeCapabilities &= ~Capabilities.Droppable;
                edgeCapabilities &= ~Capabilities.Renamable;
#if UNITY_2021_1_OR_NEWER
                edgeCapabilities &= ~Capabilities.Copiable;
#endif
                edge.capabilities = edgeCapabilities;

                _graphView.AddElement(edge);
            }

            _activeEdgeTable.Add(key, edge);

            return edge;
        }

        public void Recycle(UEdge edge)
        {
            var key = GetEdgeKey(edge.input, edge.output);
            _activeEdgeTable.Remove(key);

            // _dormantPlayableNodeTable.TryAdd(key, edge); // Unavailable in Unity 2019
            _dormantEdgeTable[key] = edge;
        }

        public void RecycleAllActiveEdges()
        {
            foreach (var edge in _activeEdgeTable.Values)
            {
                var key = GetEdgeKey(edge.input, edge.output);
                _dormantEdgeTable.Add(key, edge);
            }

            _activeEdgeTable.Clear();
        }

        public void RemoveDormantEdgesFromView()
        {
            _graphView.DeleteElements(_dormantEdgeTable.Values);
            _dormantEdgeTable.Clear();
        }


        private static int GetEdgeKey(Port inputPort, Port outputPort)
        {
            Assert.IsTrue(inputPort == null || inputPort.direction == Direction.Input);
            Assert.IsTrue(outputPort == null || outputPort.direction == Direction.Output);

            unchecked
            {
                return ((inputPort?.GetHashCode() ?? 0) * 397) ^
                       (outputPort?.GetHashCode() ?? 0);
            }
        }
    }
}