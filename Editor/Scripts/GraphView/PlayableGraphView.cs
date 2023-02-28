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

        private readonly PlayableOutputNodePool _outputNodePool;

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
            _outputNodePool = new PlayableOutputNodePool(this);
            _playableNodePoolFactory = new PlayableNodePoolFactory(this);
            _frameAllAction = () => FrameAll();

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }


        #region Update View

        public void Update(PlayableGraphViewUpdateContext context)
        {
            var playableGraphChanged = !GraphTool.IsEqual(ref _playableGraph, ref context.PlayableGraph);
            _playableGraph = context.PlayableGraph;

            RecycleAllNodesAndEdges();
            if (!_playableGraph.IsValid())
            {
                RemoveUnusedElementsFromView();
                return;
            }

            AllocAndSetupAllNodes(context, playableGraphChanged);
            ConnectNodes();
            if (context.AutoLayout)
            {
                // NOTE: If there are cycles in PlayableGraph, here will throw a StackOverflowException
                CalculateLayout();
            }

            UpdateActiveEdges(context);
            RemoveUnusedElementsFromView();

            if (playableGraphChanged)
            {
                schedule.Execute(_frameAllAction);
            }
        }

        /****************************************************************************************************/

        private void RecycleAllNodesAndEdges()
        {
            _edgePool.RecycleAllActiveEdges();
            _outputNodePool.RecycleAllActiveNodes();
            _playableNodePoolFactory.RecycleAllActiveNodes();
        }

        /****************************************************************************************************/

        private void AllocAndSetupAllNodes(PlayableGraphViewUpdateContext context, bool playableGraphChanged)
        {
            // Setup PlayableOutputNodes
            var outputCount = _playableGraph.GetOutputCount();
            for (int i = 0; i < outputCount; i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var outputNode = _outputNodePool.Alloc(playableOutput);
                outputNode.Update(playableOutput, i);
            }

            // Setup PlayableNodes
            _rootPlayables.Clear();
            var rootPlayableCount = _playableGraph.GetRootPlayableCount();

            // All Playable has a parent Playable, that means there must be at least one cycle in the PlayableGraph
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

                if (playableGraphChanged)
                {
                    Debug.LogError(
                        $"All Playable has a parent Playable, that means there is at least one cycle in the PlayableGraph '{_playableGraph.GetEditorName()}'!" +
                        "If each Playable in a group of Playables is an input to the other Playables in the group, " +
                        "and none of them are connected to a PlayableOutput, " +
                        "then this group of Playables will not be displayed in the view." +
                        $"You can set the refresh rate to '{RefreshRate.Manual}' and disable 'Auto Layout' and drag nodes manually to find out the displayed cycle.");
                }

                return;
                // throw new NoRootPlayableException();
            }

            for (int i = 0; i < rootPlayableCount; i++)
            {
                var rootPlayable = _playableGraph.GetRootPlayable(i);
                _rootPlayables.Add(rootPlayable);
                AllocAndSetupPlayableNodeTree(context, rootPlayable, true);
            }

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
            foreach (var parentNode in _outputNodePool.GetActiveNodes())
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

                nodeOrigin.y += rootPlayableNodePosition.y + rootPlayableTreeSize.y / 2f + GraphViewNode.VERTICAL_SPACE;
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

                var outputNode = _outputNodePool.GetActiveNode(playableOutput);
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
            _outputNodePool.RemoveDormantNodesFromView();
            _playableNodePoolFactory.RemoveDormantNodesFromView();
        }

        #endregion


        #region Node Management

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


        class PlayableOutputGroup
        {
            public PlayableNode SourceNode { get; set; }
            public List<PlayableOutputNode> OutputNodes { get; } = new List<PlayableOutputNode>();
            public List<PlayableOutputGroup> ChildOutputGroups { get; } = new List<PlayableOutputGroup>();


            public float GetOutputsVerticalSize(IReadOnlyDictionary<PlayableHandle, PlayableOutputGroup> outputGroups)
            {
                var verticalSize = 0f;
                foreach (var outputNode in OutputNodes)
                {
                    verticalSize += outputNode.GetNodeSize().y + GraphViewNode.VERTICAL_SPACE;
                }

                foreach (var childOutputGroup in ChildOutputGroups)
                {
                    var childGroup = outputGroups[childOutputGroup.SourceNode.Playable.GetHandle()];
                    verticalSize += childGroup.GetOutputsVerticalSize(outputGroups);
                }

                return verticalSize;
            }

            public void LayoutOutputNodes(Vector2 outputAnchor, out float bottom)
            {
                var outputCount = OutputNodes.Count;
                var mid = outputCount / 2;
                var top = outputAnchor.y;

                // Direct outputs
                if ((outputCount & 1) == 1) // Odd
                {
                    OutputNodes[mid].SetPosition(new Rect(outputAnchor, Vector2.zero));
                    bottom = outputAnchor.y + OutputNodes[mid].GetNodeSize().y;

                    for (int i = 1; i <= mid; i++)
                    {
                        // Upward
                        var outputNodeA = OutputNodes[mid - i];
                        var outputPositionA = new Vector2(
                            outputAnchor.x,
                            outputAnchor.y - (GraphViewNode.VERTICAL_SPACE + outputNodeA.GetNodeSize().y) * i
                        );
                        outputNodeA.SetPosition(new Rect(outputPositionA, Vector2.zero));
                        top = outputPositionA.y;

                        // Downward
                        var outputNodeB = OutputNodes[mid + i];
                        var outputPositionB = new Vector2(
                            outputAnchor.x,
                            outputAnchor.y + (GraphViewNode.VERTICAL_SPACE + outputNodeB.GetNodeSize().y) * i
                        );
                        outputNodeB.SetPosition(new Rect(outputPositionB, Vector2.zero));
                        bottom = outputPositionB.y + outputNodeB.GetNodeSize().y;
                    }
                }
                else // Even
                {
                    bottom = outputAnchor.y;

                    for (int i = 0; i < mid; i++)
                    {
                        // Upward
                        var outputNodeA = OutputNodes[mid - i - 1];
                        var outputPositionA = new Vector2(
                            outputAnchor.x,
                            outputAnchor.y - GraphViewNode.VERTICAL_SPACE * (i + 1.5f) - outputNodeA.GetNodeSize().y * i
                        );
                        outputNodeA.SetPosition(new Rect(outputPositionA, Vector2.zero));
                        top = outputPositionA.y;

                        // Downward
                        var outputNodeB = OutputNodes[mid + i];
                        var outputPositionB = new Vector2(
                            outputAnchor.x,
                            outputAnchor.y + GraphViewNode.VERTICAL_SPACE * (i + 1.5f) + outputNodeB.GetNodeSize().y * i
                        );
                        outputNodeB.SetPosition(new Rect(outputPositionB, Vector2.zero));
                        bottom = outputPositionB.y + outputNodeB.GetNodeSize().y;
                    }
                }

                // Child outputs
                var lastUpperChildIndex = SortAndFindLastUpperChildSourcePlayableNodeIndex();
                // Upper children
                for (int i = 0; i <= lastUpperChildIndex; i++)
                {
                    var childOutputGroup = ChildOutputGroups[i];
                    for (int j = childOutputGroup.OutputNodes.Count - 1; j >= 0; j--)
                    {
                        var outputNode = childOutputGroup.OutputNodes[j];
                        var outputPosition = new Vector2(
                            outputAnchor.x,
                            top - GraphViewNode.VERTICAL_SPACE - outputNode.GetNodeSize().y
                        );
                        outputNode.SetPosition(new Rect(outputPosition, Vector2.zero));
                        top = outputPosition.y;
                    }
                }

                // Lower children
                for (int i = lastUpperChildIndex + 1; i < ChildOutputGroups.Count; i++)
                {
                    var childOutputGroup = ChildOutputGroups[i];
                    for (int j = childOutputGroup.OutputNodes.Count - 1; j >= 0; j--)
                    {
                        var outputNode = childOutputGroup.OutputNodes[j];
                        var outputPosition = new Vector2(
                            outputAnchor.x,
                            bottom + GraphViewNode.VERTICAL_SPACE + outputNode.GetNodeSize().y
                        );
                        outputNode.SetPosition(new Rect(outputPosition, Vector2.zero));
                        bottom = outputPosition.y;
                    }
                }
            }

            private int SortAndFindLastUpperChildSourcePlayableNodeIndex()
            {
                if (ChildOutputGroups.Count == 0)
                {
                    return -1;
                }

                ChildOutputGroups.Sort(SortOutputGroupBySourceNodePositionAsc);

                if (ChildOutputGroups[0].SourceNode.Position.y >= SourceNode.Position.y)
                {
                    return -1;
                }

                if (ChildOutputGroups[ChildOutputGroups.Count - 1].SourceNode.Position.y <= SourceNode.Position.y)
                {
                    return ChildOutputGroups.Count - 1;
                }

                for (int i = 0; i < ChildOutputGroups.Count - 1; i++)
                {
                    if (ChildOutputGroups[i].SourceNode.Position.y <= SourceNode.Position.y &&
                        ChildOutputGroups[i + 1].SourceNode.Position.y >= SourceNode.Position.y)
                    {
                        return i;
                    }
                }

                throw new Exception("This should not happen!");
            }

            private static int SortOutputGroupBySourceNodePositionAsc(PlayableOutputGroup a, PlayableOutputGroup b)
            {
                if (a.SourceNode.Position.y < b.SourceNode.Position.y) return -1;
                if (a.SourceNode.Position.y > b.SourceNode.Position.y) return 1;
                return 0;
            }


            public void Clear()
            {
                SourceNode = null;
                OutputNodes.Clear();
                ChildOutputGroups.Clear();
            }
        }
    }
}