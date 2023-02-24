using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Pool;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UEdge = UnityEditor.Experimental.GraphView.Edge;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableGraphView : UGraphView
    {
        private PlayableGraph _playableGraph;

        private readonly EdgePool _edgePool;

        private readonly PlayableOutputNodePool _outputNodePool;

        private readonly PlayableNodePoolFactory _playableNodePoolFactory;

        private readonly Action _frameAllAction;

        private IReadOnlyDictionary<PlayableHandle, string> _extraNodeLabelTable;

        private bool _isViewFocused;


        public PlayableGraphView()
        {
            SetupZoom(0.1f, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            //this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _edgePool = new EdgePool(this);
            _outputNodePool = new PlayableOutputNodePool(this);
            _playableNodePoolFactory = new PlayableNodePoolFactory(this);
            _frameAllAction = () => FrameAll();

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        public void Update(PlayableGraph playableGraph)
        {
            var newPlayableGraph = !GraphTool.IsEqual(ref _playableGraph, ref playableGraph);
            _playableGraph = playableGraph;

            RecycleAllNodesAndEdges();
            AllocAndSetupAllNodes();
            ConnectNodes();
            CalculateLayout();
            UpdateActiveEdges();
            RemoveUnusedElementsFromView();

            if (newPlayableGraph)
            {
                schedule.Execute(_frameAllAction);
            }
        }

        private void RecycleAllNodesAndEdges()
        {
            _edgePool.RecycleAllActiveEdges();
            _outputNodePool.RecycleAllActiveNodes();
            _playableNodePoolFactory.RecycleAllActiveNodes();
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
                var outputNode = _outputNodePool.Alloc(playableOutput);
                outputNode.Update(playableOutput);
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

            var playableNode = _playableNodePoolFactory.Alloc(rootPlayable);
            var playableNodeExtraLabel = GetExtraNodeLabel(rootPlayable);
            playableNode.Update(rootPlayable, playableNodeExtraLabel);

            for (int i = 0; i < rootPlayable.GetInputCount(); i++)
            {
                var inputPlayable = rootPlayable.GetInput(i);
                AllocAndSetupPlayableNodeTree(inputPlayable);
            }
        }

        private void ConnectNodes()
        {
            // PlayableOutputNodes
            foreach (var parentNode in _outputNodePool.GetActiveNodes())
            {
                var childPlayable = parentNode.PlayableOutput.GetSourcePlayable();
                if (!childPlayable.IsValid())
                {
                    continue;
                }

                var childNode = _playableNodePoolFactory.GetActiveNode(childPlayable);
                var childNodeOutputIndex = parentNode.PlayableOutput.GetSourceOutputPort();
                var childNodeOutputPort = childNode.GetOutputPort(childNodeOutputIndex, true);
                var edge = _edgePool.Alloc(parentNode.InputPort, childNodeOutputPort);
                var weight = parentNode.PlayableOutput.GetWeight();
                Connect(edge, parentNode.InputPort, childNodeOutputPort, weight);
            }

            // PlayableNodes
            foreach (var parentNode in _playableNodePoolFactory.GetActiveNodes())
            {
                var parentPlayable = parentNode.Playable;
                var inputCount = parentPlayable.GetInputCount();
                for (int i = 0; i < inputCount; i++)
                {
                    var childPlayable = parentPlayable.GetInput(i);
                    if (!childPlayable.IsValid())
                    {
                        continue;
                    }

                    var childNode = _playableNodePoolFactory.GetActiveNode(childPlayable);
                    var childNodeOutputPort = childNode.FindOutputPort(parentNode.Playable);
                    var parentNodeInputPort = parentNode.GetInputPort(i);
                    var edge = _edgePool.Alloc(parentNodeInputPort, childNodeOutputPort);
                    var weight = parentPlayable.GetInputWeight(i);
                    Connect(edge, parentNodeInputPort, childNodeOutputPort, weight);
                }
            }
        }

        private static void Connect(UEdge edge, Port inputPort, Port outputPort, float weight)
        {
            Assert.IsTrue(outputPort.direction == Direction.Output);
            Assert.IsTrue(inputPort.direction == Direction.Input);

            var portColor = GraphTool.GetPortColor(weight);
            inputPort.portColor = portColor;
            outputPort.portColor = portColor;

            if (edge.input != inputPort)
            {
                edge.input?.Disconnect(edge);
                edge.input = inputPort;
                inputPort.Connect(edge);
            }

            if (edge.output != outputPort)
            {
                edge.output?.Disconnect(edge);
                edge.output = outputPort;
                outputPort.Connect(edge);
            }
        }

        private void CalculateLayout()
        {
            var origin = Vector2.zero;
            foreach (var outputNode in _outputNodePool.GetActiveNodes())
            {
                var treeSize = outputNode.GetHierarchySize();

                outputNode.CalculateLayout(origin);

                origin.y += treeSize.y + GraphViewNode.VERTICAL_SPACE;
            }
        }

        private void UpdateActiveEdges()
        {
            if (_isViewFocused)
            {
                return;
            }

            foreach (var edge in _edgePool.GetActiveEdges())
            {
                // TODO OPTIMIZABLE: Expensive operations
                edge.UpdateEdgeControl();
            }
        }

        private void RemoveUnusedElementsFromView()
        {
            _edgePool.RemoveDormantEdgesFromView();
            _outputNodePool.RemoveDormantNodesFromView();
            _playableNodePoolFactory.RemoveDormantNodesFromView();
        }


        public void SetExtraNodeLabelTable(IReadOnlyDictionary<PlayableHandle, string> extraNodeLabelTable)
        {
            _extraNodeLabelTable = extraNodeLabelTable;
        }

        public string GetExtraNodeLabel(Playable playable)
        {
            if (_extraNodeLabelTable == null || !playable.IsValid())
            {
                return null;
            }

            var playableHandle = playable.GetHandle();
            _extraNodeLabelTable.TryGetValue(playableHandle, out var label);

            return label;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }


        private void OnPointerEnter(PointerEnterEvent evt)
        {
            _isViewFocused = true;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _isViewFocused = false;
        }
    }
}