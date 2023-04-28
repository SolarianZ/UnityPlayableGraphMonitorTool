using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Pool;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;
using UEdge = UnityEditor.Experimental.GraphView.Edge;


namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    [Serializable]
    public class PlayableGraphViewUpdateContext
    {
        [NonSerialized]
        public PlayableGraph PlayableGraph;

        [SerializeField]
        public bool AutoLayout = true;

        /// <summary>
        /// Keep updating edges when mouse leave GraphView(will degrade performance).
        /// </summary>
        [SerializeField]
        public bool KeepUpdatingEdges = true;

        [SerializeField]
        public bool ShowClipProgressBar = true;

        [SerializeField]
        public bool ShowClipProgressBarTitle;

        [NonSerialized]
        public IReadOnlyDictionary<PlayableHandle, string> NodeExtraLabelTable;
    }

    public class PlayableGraphView : UGraphView
    {
        private PlayableGraph _playableGraph;

        private readonly Action _frameAllAction;

        private bool _isViewFocused;


        #region Properties for connection and layout

        private readonly EdgePool _edgePool;

        private readonly PlayableOutputNodePoolFactory _outputNodePoolFactory;

        private readonly PlayableNodePoolFactory _playableNodePoolFactory;

        private readonly Dictionary<PlayableHandle, PlayableOutputGroup> _outputGroups =
            new Dictionary<PlayableHandle, PlayableOutputGroup>();

        private readonly List<PlayableOutputGroup> _dormantOutputGroups = new List<PlayableOutputGroup>();

        private readonly List<Playable> _rootPlayables = new List<Playable>();

        #endregion


        public PlayableGraphView()
        {
            SetupZoom(0.1f, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            _edgePool = new EdgePool(this);
            _outputNodePoolFactory = new PlayableOutputNodePoolFactory(this);
            _playableNodePoolFactory = new PlayableNodePoolFactory(this);
            _frameAllAction = () => FrameAll();

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }


        #region Update View

        public bool Update(PlayableGraphViewUpdateContext context)
        {
            var success = true;

            var playableGraphChanged = !GraphTool.IsEqual(ref _playableGraph, ref context.PlayableGraph);
            _playableGraph = context.PlayableGraph;

            RecycleAllNodesAndEdges();
            if (!_playableGraph.IsValid())
            {
                RemoveUnusedElementsFromView();
                return true;
            }

            success &= AllocAndSetupAllNodes(context);
            ConnectNodes();
            if (context.AutoLayout)
            {
                // NOTE: If there are cycles in PlayableGraph, here may throw a StackOverflowException
                CalculateLayout();
            }

            UpdateActiveEdges(context);
            RemoveUnusedElementsFromView();

            if (playableGraphChanged)
            {
                schedule.Execute(_frameAllAction);
            }

            return success;
        }

        /****************************************************************************************************/

        private void RecycleAllNodesAndEdges()
        {
            _edgePool.RecycleAllActiveEdges();
            _outputNodePoolFactory.RecycleAllActiveNodes();
            _playableNodePoolFactory.RecycleAllActiveNodes();
        }

        /****************************************************************************************************/

        private bool AllocAndSetupAllNodes(PlayableGraphViewUpdateContext context)
        {
            // Setup PlayableOutputNodes
            var outputCount = _playableGraph.GetOutputCount();
            for (int i = 0; i < outputCount; i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var outputNode = _outputNodePoolFactory.Alloc(playableOutput);
                outputNode.Update(playableOutput, i);
            }

            // Setup PlayableNodes
            _rootPlayables.Clear();
            var rootPlayableCount = _playableGraph.GetRootPlayableCount();

            // All Playable has a parent Playable, that means there is at least one cycle in the PlayableGraph
            if (rootPlayableCount == 0 && _playableGraph.GetPlayableCount() > 0)
            {
                for (int i = 0; i < outputCount; i++)
                {
                    var playableOutput = _playableGraph.GetOutput(i);
                    var fakeRootPlayable = playableOutput.GetSourcePlayable();
                    // TODO FIXME: Add fakeRootPlayable to _rootPlayables will cause Unity Editor crash
                    // _rootPlayables.Add(fakeRootPlayable);
                    AllocAndSetupPlayableNodeTree(context, fakeRootPlayable, true);
                }

                // throw new NoRootPlayableException();
                return false;
            }

            for (int i = 0; i < rootPlayableCount; i++)
            {
                var rootPlayable = _playableGraph.GetRootPlayable(i);
                _rootPlayables.Add(rootPlayable);
                AllocAndSetupPlayableNodeTree(context, rootPlayable, true);
            }

            return true;

            // OLD_VERSION
            // for (int i = 0; i < outputCount; i++)
            // {
            //     var playableOutput = _playableGraph.GetOutput(i);
            //     var inputPlayable = playableOutput.GetSourcePlayable();
            //     AllocAndSetupPlayableNodeTree(updateContext, inputPlayable);
            // }
        }

        private void AllocAndSetupPlayableNodeTree(PlayableGraphViewUpdateContext context, Playable parentPlayable,
            bool isRootPlayable)
        {
            if (!parentPlayable.IsValid())
            {
                return;
            }

            var playableNode = _playableNodePoolFactory.Alloc(parentPlayable);
            playableNode.IsRootPlayable = isRootPlayable;
            playableNode.Update(context, parentPlayable);

            for (int i = 0; i < parentPlayable.GetInputCount(); i++)
            {
                var inputPlayable = parentPlayable.GetInput(i);
                if (_playableNodePoolFactory.IsNodeActive(inputPlayable))
                {
                    continue;
                }

                AllocAndSetupPlayableNodeTree(context, inputPlayable, false);
            }
        }

        /****************************************************************************************************/

        private void ConnectNodes()
        {
            // PlayableOutputNodes
            foreach (var parentNode in _outputNodePoolFactory.GetActiveNodes())
            {
                var childPlayable = parentNode.PlayableOutput.GetSourcePlayable();
                if (!childPlayable.IsValid())
                {
                    // PlayableOutput may have no source input
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
                        // Playable may have no input
                        continue;
                    }

                    var childNode = _playableNodePoolFactory.GetActiveNode(childPlayable);
                    childNode.ParentNode = parentNode;

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

        /****************************************************************************************************/

        private void CalculateLayout()
        {
            RecycleOutputGroups();
            if (!_playableGraph.IsValid())
            {
                return;
            }

            CollectPlayableOutputGroups();

            // Layout root Playables and PlayableOutputs
            var nodeOrigin = Vector2.zero;
            for (int i = 0; i < _rootPlayables.Count; i++)
            {
                // Layout root Playables
                var rootPlayable = _rootPlayables[i];
                var rootPlayableNode = _playableNodePoolFactory.GetActiveNode(rootPlayable);
                rootPlayableNode.CalculateLayout(new Vector2(nodeOrigin.x - GraphViewNode.MaxNodeSize.x, nodeOrigin.y),
                    out var rootPlayableNodePosition, out var rootPlayableTreeSize);

                // Ensure the vertical size of the Playable tree is greater than the vertical size of its output group
                var rootPlayableHandle = rootPlayable.GetHandle();
                var outputGroup = _outputGroups[rootPlayableHandle];
                var minVerticalSize = outputGroup.GetOutputsVerticalSize(_outputGroups);
                if (rootPlayableTreeSize.y < minVerticalSize)
                {
                    rootPlayableTreeSize.y = minVerticalSize;
                }

                // Layout PlayableOutputs
                outputGroup.LayoutOutputNodes(new Vector2(GraphViewNode.HORIZONTAL_SPACE, rootPlayableNodePosition.y),
                    out var outputBottom);
                _outputGroups.Remove(rootPlayableHandle);

                nodeOrigin.y += rootPlayableTreeSize.y + GraphViewNode.VERTICAL_SPACE;
                if (nodeOrigin.y < outputBottom + GraphViewNode.VERTICAL_SPACE)
                {
                    nodeOrigin.y = outputBottom + GraphViewNode.VERTICAL_SPACE;
                }
            }

            // Layout PlayableOutputs which has no source Playable
            Playable invalidPlayable = default;
            if (_outputGroups.TryGetValue(invalidPlayable.GetHandle(), out var noSourceOutputGroup))
            {
                var verticalSize = noSourceOutputGroup.GetOutputsVerticalSize(_outputGroups);
                noSourceOutputGroup.LayoutOutputNodes(
                    new Vector2(GraphViewNode.HORIZONTAL_SPACE, nodeOrigin.y + verticalSize / 2f),
                    out var _
                );
            }

            // OLD_VERSION
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
                outputGroup.Clear();
                _dormantOutputGroups.Add(outputGroup);
            }

            _outputGroups.Clear();
        }

        private void CollectPlayableOutputGroups()
        {
            // Prepare root Playables output groups
            for (int i = 0; i < _rootPlayables.Count; i++)
            {
                var rootPlayable = _rootPlayables[i];
                var rootPlayableHandle = rootPlayable.GetHandle();
                if (!_outputGroups.TryGetValue(rootPlayableHandle, out var outputGroup))
                {
                    outputGroup = AllocOutputGroup();
                    outputGroup.SourceNode = _playableNodePoolFactory.GetActiveNode(rootPlayable);
                    _outputGroups.Add(rootPlayableHandle, outputGroup);
                }
            }

            // Collect output groups
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
                    _playableNodePoolFactory.TryGetActiveNode(sourcePlayable, out var sourcePlayableNode);
                    outputGroup.SourceNode = sourcePlayableNode;
                    _outputGroups.Add(sourcePlayableHandle, outputGroup);

                    // If a new group instance is allocated here, that means the source Playable is not a root Playable
                    // PlayableOutput may have no source input
                    var rootPlayableNode = sourcePlayableNode?.FindRootPlayableNode();
                    if (rootPlayableNode != null)
                    {
                        _outputGroups[rootPlayableNode.Playable.GetHandle()].ChildOutputGroups.Add(outputGroup);
                    }
                }

                var outputNode = _outputNodePoolFactory.GetActiveNode(playableOutput);
                outputGroup.OutputNodes.Add(outputNode);
            }
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

        /****************************************************************************************************/

        private void RemoveUnusedElementsFromView()
        {
            _edgePool.RemoveDormantEdgesFromView();
            _outputNodePoolFactory.RemoveDormantNodesFromView();
            _playableNodePoolFactory.RemoveDormantNodesFromView();
        }

        #endregion


        #region Node Management

        // ReSharper disable once IdentifierTypo
        public void SetNodesMovability(bool movable)
        {
            foreach (var outputNode in _outputNodePoolFactory.GetActiveNodes())
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


        #region Edge Management

        private void UpdateActiveEdges(PlayableGraphViewUpdateContext updateContext)
        {
            if (_isViewFocused || !updateContext.KeepUpdatingEdges)
            {
                return;
            }

            foreach (var edge in _edgePool.GetActiveEdges())
            {
                // Expensive operations
                edge.UpdateEdgeControl();
            }
        }


        private void OnPointerEnter(PointerEnterEvent evt)
        {
            _isViewFocused = true;
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            _isViewFocused = false;
        }

        #endregion


        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // Disable contextual menu
        }
    }
}