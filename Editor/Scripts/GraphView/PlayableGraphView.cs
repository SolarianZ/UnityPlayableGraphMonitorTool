using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Pool;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UEdge = UnityEditor.Experimental.GraphView.Edge;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableGraphView : UGraphView
    {
        class PlayableNodeBuffer
        {
            private readonly UGraphView _graphView;

            private readonly Dictionary<PlayableHandle, PlayableNode_New> _activePlayableNodeTable =
                new Dictionary<PlayableHandle, PlayableNode_New>();

            private readonly Dictionary<PlayableHandle, PlayableNode_New> _dormantPlayableNodeTable =
                new Dictionary<PlayableHandle, PlayableNode_New>();


            public PlayableNodeBuffer(UGraphView graphView)
            {
                _graphView = graphView;
            }

            public PlayableNode_New GetActiveNode(Playable playable)
            {
                return _activePlayableNodeTable[playable.GetHandle()];
            }

            public IEnumerable<PlayableNode_New> GetActiveNodes()
            {
                return _activePlayableNodeTable.Values;
            }

            public PlayableNode_New Alloc(Playable playable)
            {
                var handle = playable.GetHandle();
                if (_activePlayableNodeTable.TryGetValue(handle, out var node))
                {
                    return node;
                }

                if (!_dormantPlayableNodeTable.Remove(handle, out node))
                {
                    node = new PlayableNode_New();
                    _graphView.AddElement(node);
                }

                _activePlayableNodeTable.Add(handle, node);

                return node;
            }

            public void Recycle(PlayableNode_New node)
            {
                node.Release();
                var handle = node.Playable.GetHandle();
                _activePlayableNodeTable.Remove(handle);
                _dormantPlayableNodeTable.TryAdd(handle, node);
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
        }


        private PlayableGraph _playableGraph;

        private readonly List<PlayableOutputNode> _rootOutputNodes = new List<PlayableOutputNode>();


        private readonly EdgePool _edgePool;

        private readonly PlayableOutputNodePool _outputNodePool;

        private readonly List<PlayableOutputNode_New> _outputNodeList = new List<PlayableOutputNode_New>();

        private readonly PlayableNodeBuffer _playableNodeBuffer;


        public void Update_New(PlayableGraph playableGraph)
        {
            RecycleAllNodesAndEdges();

            _playableGraph = playableGraph;
            if (!_playableGraph.IsValid())
            {
                return;
            }

            AllocAndSetupAllNodes();
            ConnectNodes();
            CalculateLayout_New();

            _playableNodeBuffer.RemoveDormantNodesFromView();
        }

        private void RecycleAllNodesAndEdges()
        {
            _edgePool.RecycleAllActiveEdges();
            _outputNodePool.RecycleAllActiveNodes();
            _playableNodeBuffer.RecycleAllActiveNodes();

            _outputNodeList.Clear();
        }

        private void AllocAndSetupAllNodes()
        {
            if (!_playableGraph.IsValid())
            {
                return;
            }

            // PlayableOutputNodes
            for (int i = 0; i < _playableGraph.GetOutputCount(); i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var outputNode = _outputNodePool.Alloc();
                _outputNodeList.Add(outputNode);

                outputNode.Setup(playableOutput);
            }

            // PlayableNodes
            for (int i = 0; i < _playableGraph.GetOutputCount(); i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var inputPlayable = playableOutput.GetSourcePlayable();
                AllocAndSetupPlayableNodeTree(inputPlayable);
            }
        }

        private void AllocAndSetupPlayableNodeTree(Playable rootPlayable)
        {
            if (!rootPlayable.IsValid())
            {
                return;
            }

            // todo: Playable in timeline may appears more than once
            var playableNode = _playableNodeBuffer.Alloc(rootPlayable);

            playableNode.Setup(rootPlayable);

            for (int i = 0; i < rootPlayable.GetInputCount(); i++)
            {
                var inputPlayable = rootPlayable.GetInput(i);
                AllocAndSetupPlayableNodeTree(inputPlayable);
            }
        }

        private void ConnectNodes()
        {
            // PlayableOutputNodes
            for (int i = 0; i < _outputNodeList.Count; i++)
            {
                var outputNode = _outputNodeList[i];
                foreach (var inputPlayable in outputNode.GetInputPlayables())
                {
                    if (!inputPlayable.IsValid())
                    {
                        continue;
                    }

                    var inputNode = _playableNodeBuffer.GetActiveNode(inputPlayable);
                    // todo: outputNode.PlayableOutput.GetSourceOutputPort() returns 1 but source playable only has ONE output!
                    // var inputNodeOutputIndex = outputNode.PlayableOutput.GetSourceOutputPort();
                    var inputNodeOutputIndex = 0;
                    var inputNodeOutputPort = inputNode.GetOutputPort(inputNodeOutputIndex);
                    var edge = _edgePool.Alloc();
                    edge.input = outputNode.InputPort;
                    edge.output = inputNodeOutputPort;
                }
            }

            // PlayableNodes
            foreach (var playableNode in _playableNodeBuffer.GetActiveNodes())
            {
                var inputIndex = -1;
                foreach (var inputPlayable in playableNode.GetInputPlayables())
                {
                    inputIndex++;
                    if (!inputPlayable.IsValid())
                    {
                        continue;
                    }

                    var inputNode = _playableNodeBuffer.GetActiveNode(inputPlayable);
                    var inputNodeOutputPort = inputNode.FindConnectedOutputPort(playableNode.Playable);
                    var edge = _edgePool.Alloc();
                    edge.input = playableNode.GetInputPort(inputIndex);
                    edge.output = inputNodeOutputPort;
                }
            }
        }

        private void CalculateLayout_New()
        {
            var origin = Vector2.zero;
            for (int i = 0; i < _outputNodeList.Count; i++)
            {
                var outputNode = _outputNodeList[i];
                var treeSize = outputNode.GetHierarchySize();

                outputNode.CalculateLayout(origin);

                origin.y += treeSize.y + GraphViewNode.VERTICAL_SPACE;
            }
        }


        public PlayableGraphView()
        {
            SetupZoom(0.1f, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            //this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _edgePool = new EdgePool(this);
            _outputNodePool = new(this);
            _playableNodeBuffer = new PlayableNodeBuffer(this);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }


        // --------------- OLD ----------------------

        public void Update(PlayableGraph playableGraph)
        {
            var needFrameAll = !GraphTool.IsEqual(ref _playableGraph, ref playableGraph);

            _playableGraph = playableGraph;

            if (!_playableGraph.IsValid())
            {
                ClearView();
            }

            UpdateView();

            CalculateLayout();

            if (needFrameAll)
            {
                schedule.Execute(() => FrameAll());
            }
        }

        private void ClearView()
        {
            foreach (var playableOutputNode in _rootOutputNodes)
            {
                playableOutputNode.RemoveFromView();
            }

            _rootOutputNodes.Clear();
        }

        private void UpdateView()
        {
            if (!_playableGraph.IsValid())
            {
                return;
            }

            // mark all root nodes inactive
            for (int i = 0; i < _rootOutputNodes.Count; i++)
            {
                _rootOutputNodes[i].RemoveFlag(NodeFlag.Active);
            }

            // diff nodes
            for (int i = 0; i < _playableGraph.GetOutputCount(); i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var rootOutputNodeIndex = FindRootOutputNodeIndex(playableOutput);
                if (rootOutputNodeIndex >= 0)
                {
                    _rootOutputNodes[i].AddFlag(NodeFlag.Active);
                    continue;
                }

                // create new node
                var playableOutputNode = PlayableOutputNodeFactory.CreateNode(playableOutput);
                playableOutputNode.AddToView(this, null);
                playableOutputNode.AddFlag(NodeFlag.Active);

                _rootOutputNodes.Add(playableOutputNode);
            }

            // check and update nodes
            for (int i = _rootOutputNodes.Count - 1; i >= 0; i--)
            {
                var rootOutputNode = _rootOutputNodes[i];
                if (!rootOutputNode.CheckFlag(NodeFlag.Active))
                {
                    rootOutputNode.RemoveFromView();

                    _rootOutputNodes.RemoveAt(i);
                    continue;
                }

                rootOutputNode.Update();
            }
        }

        private int FindRootOutputNodeIndex(PlayableOutput playableOutput)
        {
            for (int i = 0; i < _rootOutputNodes.Count; i++)
            {
                if (_rootOutputNodes[i].PlayableOutput.Equals(playableOutput))
                {
                    return i;
                }
            }

            return -1;
        }

        private void CalculateLayout()
        {
            var origin = Vector2.zero;
            for (int i = 0; i < _rootOutputNodes.Count; i++)
            {
                var outputNode = _rootOutputNodes[i];
                var treeSize = outputNode.GetHierarchySize();

                outputNode.CalculateLayout(origin);

                origin.y += treeSize.y + GraphViewNode.VERTICAL_SPACE;
            }
        }
    }
}