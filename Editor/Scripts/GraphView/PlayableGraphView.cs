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
    [Serializable]
    public class PlayableGraphViewUpdateContext
    {
        public PlayableGraph PlayableGraph;

        public bool AutoLayout;

        /// <summary>
        /// Keep updating edges when mouse leave GraphView(will degrade performance).
        /// </summary>
        public bool KeepUpdatingEdges = true;

        public bool ShowClipProgressBar = true;
    }

    public class PlayableGraphView : UGraphView
    {
        private PlayableGraph _playableGraph;

        private readonly EdgePool _edgePool;

        private readonly PlayableOutputNodePool _outputNodePool;

        private readonly PlayableNodePoolFactory _playableNodePoolFactory;

        private readonly Action _frameAllAction;

        private readonly Dictionary<PlayableHandle, PlayableOutputGroup> _outputGroups =
            new Dictionary<PlayableHandle, PlayableOutputGroup>();

        private readonly List<PlayableOutputGroup> _dormantOutputGroups = new List<PlayableOutputGroup>();

        private IReadOnlyDictionary<PlayableHandle, string> _nodeExtraLabelTable;

        private bool _isViewFocused;


        public PlayableGraphView()
        {
            SetupZoom(0.1f, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _edgePool = new EdgePool(this);
            _outputNodePool = new PlayableOutputNodePool(this);
            _playableNodePoolFactory = new PlayableNodePoolFactory(this);
            _frameAllAction = () => FrameAll();

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        public void Update(PlayableGraphViewUpdateContext updateContext)
        {
            var newPlayableGraph = !GraphTool.IsEqual(ref _playableGraph, ref updateContext.PlayableGraph);
            _playableGraph = updateContext.PlayableGraph;

            RecycleAllNodesAndEdges();
            AllocAndSetupAllNodes(updateContext);
            ConnectNodes();
            if (updateContext.AutoLayout)
            {
                // If there are cycles in PlayableGraph, here will throw a StackOverflowException
                CalculateLayout();
            }

            UpdateActiveEdges(updateContext);
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

        private void AllocAndSetupAllNodes(PlayableGraphViewUpdateContext updateContext)
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
                outputNode.Update(playableOutput, i);
            }

            // PlayableNodes
            for (int i = 0; i < _playableGraph.GetOutputCount(); i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var inputPlayable = playableOutput.GetSourcePlayable();
                AllocAndSetupPlayableNodeTree(updateContext, inputPlayable);
            }
        }

        private void AllocAndSetupPlayableNodeTree(PlayableGraphViewUpdateContext updateContext, Playable rootPlayable)
        {
            if (!rootPlayable.IsValid())
            {
                return;
            }

            var playableNode = _playableNodePoolFactory.Alloc(rootPlayable);
            var playableNodeExtraLabel = GetExtraNodeLabel(rootPlayable);
            playableNode.Update(updateContext, rootPlayable, playableNodeExtraLabel);

            for (int i = 0; i < rootPlayable.GetInputCount(); i++)
            {
                var inputPlayable = rootPlayable.GetInput(i);
                if (_playableNodePoolFactory.IsNodeActive(inputPlayable))
                {
                    continue;
                }

                AllocAndSetupPlayableNodeTree(updateContext, inputPlayable);
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
            RecycleOutputGroups();

            if (!_playableGraph.IsValid())
            {
                return;
            }

            // Collect PlayableOutput groups
            var rootPlayableCount = _playableGraph.GetRootPlayableCount();
            var outputCount = _playableGraph.GetOutputCount();
            for (int i = 0; i < outputCount; i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var sourcePlayable = playableOutput.GetSourcePlayable();
                if (!sourcePlayable.IsValid())
                {
                    sourcePlayable = default;
                }

                var sourcePlayableHandle = sourcePlayable.GetHandle();
                if (!_outputGroups.TryGetValue(sourcePlayableHandle, out var outputGroup))
                {
                    outputGroup = AllocOutputGroup();
                    _outputGroups.Add(sourcePlayableHandle, outputGroup);
                }

                var outputNode = _outputNodePool.GetActiveNode(playableOutput);
                outputGroup.OutputNodes.Add(outputNode);
            }

            // Layout root Playables and PlayableOutputs
            var rootPlayableOrigin = Vector2.zero;
            for (int i = 0; i < rootPlayableCount; i++)
            {
                // Layout root Playables
                var playable = _playableGraph.GetRootPlayable(i);
                var playableNode = _playableNodePoolFactory.GetActiveNode(playable);
                playableNode.CalculateLayout(rootPlayableOrigin,
                    out var playableNodePosition, out var playableTreeSize);

                var playableHandle = playable.GetHandle();
                var outputGroup = _outputGroups[playableHandle];
                var minVerticalSize = outputGroup.GetOutputsVerticalSize();
                if (playableTreeSize.y < minVerticalSize)
                {
                    playableTreeSize.y = minVerticalSize;
                }

                rootPlayableOrigin.y += playableTreeSize.y + GraphViewNode.VERTICAL_SPACE;

                // Layout PlayableOutputs
                outputGroup.LayoutOutputNodes(playableNodePosition);
                _outputGroups.Remove(playableHandle);
            }

            // Layout PlayableOutputs which has no source Playable
            Playable invalidPlayable = default;
            if (_outputGroups.TryGetValue(invalidPlayable.GetHandle(), out var isolatedInputOutputGroup))
            {
                isolatedInputOutputGroup.LayoutOutputNodes(rootPlayableOrigin);
            }

            // OLD VERSION
            // var origin = Vector2.zero;
            // foreach (var outputNode in _outputNodePool.GetActiveNodes())
            // {
            //     outputNode.CalculateLayout(origin, out var treeSize);
            //     origin.y += treeSize.y + GraphViewNode.VERTICAL_SPACE;
            // }
        }

        private void RecycleOutputGroups()
        {
            foreach (var outputGroup in _outputGroups.Values)
            {
                outputGroup.OutputNodes.Clear();
                _dormantOutputGroups.Add(outputGroup);
            }

            _outputGroups.Clear();
        }

        private PlayableOutputGroup AllocOutputGroup()
        {
            PlayableOutputGroup outputGroup;
            var dormantOutputGroupCount = _dormantOutputGroups.Count;
            if (dormantOutputGroupCount > 0)
            {
                outputGroup = _dormantOutputGroups[dormantOutputGroupCount - 1];
                _dormantOutputGroups.RemoveAt(dormantOutputGroupCount - 1);
            }
            else
            {
                outputGroup = new PlayableOutputGroup();
            }

            return outputGroup;
        }

        private void UpdateActiveEdges(PlayableGraphViewUpdateContext updateContext)
        {
            if (_isViewFocused || !updateContext.KeepUpdatingEdges)
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


        #region Node Control

        public void SetNodeExtraLabelTable(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            _nodeExtraLabelTable = nodeExtraLabelTable;
        }

        public string GetExtraNodeLabel(Playable playable)
        {
            if (_nodeExtraLabelTable == null || !playable.IsValid())
            {
                return null;
            }

            var playableHandle = playable.GetHandle();
            _nodeExtraLabelTable.TryGetValue(playableHandle, out var label);

            return label;
        }


        // ReSharper disable once IdentifierTypo
        public void SetNodesMovability(bool movable)
        {
            foreach (var outputNode in _outputNodePool.GetActiveNodes())
            {
                var nodeCaps = outputNode.capabilities;
                if (movable) nodeCaps |= Capabilities.Movable;
                else nodeCaps &= ~Capabilities.Movable;
                outputNode.capabilities = nodeCaps;
            }

            foreach (var playableNode in _playableNodePoolFactory.GetActiveNodes())
            {
                var nodeCaps = playableNode.capabilities;
                if (movable) nodeCaps |= Capabilities.Movable;
                else nodeCaps &= ~Capabilities.Movable;
                playableNode.capabilities = nodeCaps;
            }
        }

        #endregion


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


        class PlayableOutputGroup
        {
            public readonly List<PlayableOutputNode> OutputNodes = new List<PlayableOutputNode>();


            public float GetOutputsVerticalSize()
            {
                var verticalSize = 0f;
                foreach (var outputNode in OutputNodes)
                {
                    verticalSize += outputNode.GetNodeSize().y + GraphViewNode.VERTICAL_SPACE;
                }

                return verticalSize;
            }

            public void LayoutOutputNodes(Vector2 outputAnchor)
            {
                var outputOrigin = new Vector2(
                    outputAnchor.x + GraphViewNode.HORIZONTAL_SPACE + GraphViewNode.MaxNodeSize.x,
                    outputAnchor.y
                );

                var outputCount = OutputNodes.Count;
                var mid = outputCount / 2;
                if ((outputCount & 1) == 1) // Odd
                {
                    OutputNodes[mid].SetPosition(new Rect(outputOrigin, Vector2.zero));

                    for (int i = 1; i <= mid; i++)
                    {
                        // Upward
                        var outputNodeA = OutputNodes[mid - i];
                        var outputPositionA = new Vector2(
                            outputOrigin.x,
                            outputOrigin.y - (GraphViewNode.VERTICAL_SPACE + outputNodeA.GetNodeSize().y) * i
                        );
                        outputNodeA.SetPosition(new Rect(outputPositionA, Vector2.zero));

                        // Downward
                        var outputNodeB = OutputNodes[mid + i];
                        var outputPositionB = new Vector2(
                            outputOrigin.x,
                            outputOrigin.y + (GraphViewNode.VERTICAL_SPACE + outputNodeB.GetNodeSize().y) * i
                        );
                        outputNodeB.SetPosition(new Rect(outputPositionB, Vector2.zero));
                    }
                }
                else // Even
                {
                    for (int i = 0; i < mid; i++)
                    {
                        // Upward
                        var outputNodeA = OutputNodes[mid - i - 1];
                        var outputPositionA = new Vector2(
                            outputOrigin.x,
                            outputOrigin.y - GraphViewNode.VERTICAL_SPACE * (i + 1.5f) - outputNodeA.GetNodeSize().y * i
                        );
                        outputNodeA.SetPosition(new Rect(outputPositionA, Vector2.zero));

                        // Downward
                        var outputNodeB = OutputNodes[mid + i];
                        var outputPositionB = new Vector2(
                            outputOrigin.x,
                            outputOrigin.y + GraphViewNode.VERTICAL_SPACE * (i + 1.5f) + outputNodeB.GetNodeSize().y * i
                        );
                        outputNodeB.SetPosition(new Rect(outputPositionB, Vector2.zero));
                    }
                }
            }
        }
    }
}