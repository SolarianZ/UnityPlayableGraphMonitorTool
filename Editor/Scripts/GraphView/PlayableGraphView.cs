using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableGraphView : UGraphView
    {
        private PlayableGraph _playableGraph;

        private readonly List<PlayableOutputNode> _rootOutputNodes = new List<PlayableOutputNode>();


        public PlayableGraphView()
        {
            this.AddManipulator(new ContentDragger());
            //this.AddManipulator(new SelectionDragger());
            //this.AddManipulator(new RectangleSelector());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        }

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
                // wait at least 2 frames for view initialization
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.delayCall += () => { FrameAll(); };
                };
            }
        }


        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // disable contextual menu
            //base.BuildContextualMenu(evt);
        }

        private void ClearView()
        {
            foreach (var playableOutputNode in _rootOutputNodes)
            {
                playableOutputNode.RemoveFromContainer();
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
                var rootOutputNodeIndex = FindRootOutputNode(playableOutput);
                if (rootOutputNodeIndex >= 0)
                {
                    _rootOutputNodes[i].AddFlag(NodeFlag.Active);
                    continue;
                }

                // create new node
                var playableOutputNode = PlayableOutputNodeFactory.CreateNode(playableOutput);
                playableOutputNode.AddToContainer(this);
                playableOutputNode.AddFlag(NodeFlag.Active);

                _rootOutputNodes.Add(playableOutputNode);
            }

            // check and update nodes
            for (int i = _rootOutputNodes.Count - 1; i >= 0; i--)
            {
                var rootOutputNode = _rootOutputNodes[i];
                if (!rootOutputNode.CheckFlag(NodeFlag.Active))
                {
                    rootOutputNode.RemoveFromContainer();

                    _rootOutputNodes.RemoveAt(i);
                    continue;
                }

                rootOutputNode.Update();
            }
        }

        private int FindRootOutputNode(PlayableOutput playableOutput)
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

                origin.y += treeSize.y + GraphViewNode.VerticalSpace;
            }
        }
    }
}