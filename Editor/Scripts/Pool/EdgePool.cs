using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Pool;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UEdge = UnityEditor.Experimental.GraphView.Edge;

namespace GBG.PlayableGraphMonitor.Editor.Pool
{
    public class EdgePool
    {
        private readonly UGraphView _container;

        private readonly ObjectPool<UEdge> _edgePool;

        private readonly List<UEdge> _activeEdges = new List<UEdge>();


        public EdgePool(UGraphView container)
        {
            _container = container;
            _edgePool = new ObjectPool<UEdge>(
                CreateEdge, ActiveEdge, RecycleEdge, DestroyEdge
            );
        }

        public UEdge Alloc()
        {
            var edge = _edgePool.Get();
            _activeEdges.Add(edge);

            return edge;
        }

        public void Recycle(UEdge edge)
        {
            _activeEdges.Remove(edge);
            _edgePool.Release(edge);
        }

        public void RecycleAllActiveEdges()
        {
            for (int i = _activeEdges.Count - 1; i >= 0; i--)
            {
                _edgePool.Release(_activeEdges[i]);
                _activeEdges.RemoveAt(i);
            }
        }


        private UEdge CreateEdge()
        {
            var edge = new UEdge();
            edge.capabilities &= ~Capabilities.Movable;
            edge.capabilities &= ~Capabilities.Deletable;
            edge.capabilities &= ~Capabilities.Selectable;
            return edge;
        }

        private void ActiveEdge(UEdge edge)
        {
            _container.AddElement(edge);
        }

        private void RecycleEdge(UEdge edge)
        {
            edge.input = null;
            edge.output = null;
            _container.RemoveElement(edge);
        }

        private void DestroyEdge(UEdge edge)
        {
        }
    }
}