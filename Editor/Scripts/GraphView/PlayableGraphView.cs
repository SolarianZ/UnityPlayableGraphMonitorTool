using System.Collections;
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
        private PlayableGraph _playableGraph;

        private readonly List<PlayableOutputNode> _rootOutputNodes = new List<PlayableOutputNode>();


        private readonly EdgePool _edgePool;

        private readonly PlayableOutputNodePool _outputNodePool;

        private readonly PlayableNodePool _playableNodePool;

        private readonly List<PlayableOutputNode_New> _outputNodeList = new List<PlayableOutputNode_New>();

        private readonly Dictionary<PlayableHandle, PlayableNode_New> _playableNodeTable =
            new Dictionary<PlayableHandle, PlayableNode_New>();


        public IEnumerator UpdateIncremental()
        {
            RecycleAllNodesAndEdges();

            yield return null;

            // Call these two methods continuously in case PlayableGraph topology changed
            AllocAndSetupAllNodes();
            ConnectNodes();

            yield return null;

            CalculateLayout_New();
        }

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
        }

        private void RecycleAllNodesAndEdges()
        {
            _edgePool.RecycleAllActiveEdges();
            _outputNodePool.RecycleAllActiveNodes();
            _playableNodePool.RecycleAllActiveNodes();

            _outputNodeList.Clear();
            _playableNodeTable.Clear();
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

            var playableNode = _playableNodePool.Alloc();
            // todo: Playable in timeline may appears more than once
            _playableNodeTable[rootPlayable.GetHandle()] = playableNode;
            // _playableNodeTable.Add(rootPlayable.GetHandle(), playableNode);

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

                    var inputNode = _playableNodeTable[inputPlayable.GetHandle()];
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
            foreach (var playableNode in _playableNodeTable.Values)
            {
                var inputIndex = -1;
                foreach (var inputPlayable in playableNode.GetInputPlayables())
                {
                    inputIndex++;
                    if (!inputPlayable.IsValid())
                    {
                        continue;
                    }

                    var inputNode = _playableNodeTable[inputPlayable.GetHandle()];
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
            _outputNodePool = new PlayableOutputNodePool(this);
            _playableNodePool = new PlayableNodePool(this);
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