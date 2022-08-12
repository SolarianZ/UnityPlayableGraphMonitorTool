using GBG.PlayableGraphMonitor.Editor.Node;
using System.Collections.Generic;
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

        public PlayableGraph GetPlayableGraph()
        {
            return _playableGraph;
        }

        public void Update(PlayableGraph playableGraph)
        {
            if (!_playableGraph.IsValid())
            {
                ClearView();
            }

            if (IsEqual(ref _playableGraph, ref playableGraph))
            {
                DiffOutputNodes();
            }
            else
            {
                // playable graph changed
                _playableGraph = playableGraph;

                PopulateView();
            }

            CalculateLayout();
        }


        private void ClearView()
        {
            foreach (var playableOutputNode in _rootOutputNodes)
            {
                playableOutputNode.RemoveFromContainer();
            }

            _rootOutputNodes.Clear();
        }

        private void PopulateView()
        {
            if (!_playableGraph.IsValid())
            {
                return;
            }

            // create nodes
            for (int i = 0; i < _playableGraph.GetOutputCount(); i++)
            {
                var playableOutput = _playableGraph.GetOutput(i);
                var playableOutputNode = PlayableOutputNodeFactory.CreateNode(playableOutput);
                playableOutputNode.AddToContainer(this);

                _rootOutputNodes.Add(playableOutputNode);
            }

            for (int i = 0; i < _rootOutputNodes.Count; i++)
            {
                //_rootOutputNodes[i].CreateAndConnectInputNodes();
                _rootOutputNodes[i].Update();
            }
        }

        private void DiffOutputNodes()
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

                origin.y += treeSize.y + NodeLayoutInfo.VerticalSpace;
            }
        }


        private static bool IsEqual(ref PlayableGraph a, ref PlayableGraph b)
        {
            if (!a.IsValid())
            {
                return !b.IsValid();
            }

            if (!b.IsValid())
            {
                return false;
            }

            var nameA = a.GetEditorName();
            var nameB = b.GetEditorName();

            return nameA.Equals(nameB);
        }
    }
}