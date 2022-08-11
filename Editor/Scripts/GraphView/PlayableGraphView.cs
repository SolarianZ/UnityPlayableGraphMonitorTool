using GBG.PlayableGraphMonitor.Editor.Node;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using UGraphView = UnityEditor.Experimental.GraphView.GraphView;

namespace GBG.PlayableGraphMonitor.Editor.GraphView
{
    public class PlayableGraphView : UGraphView
    {
        private PlayableGraph _playableGraph;

        private readonly List<PlayableOutputNode> _playableOutputNodes = new List<PlayableOutputNode>();


        public PlayableGraphView()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        }

        public PlayableGraph GetPlayableGraph()
        {
            return _playableGraph;
        }

        public void SetPlayableGraph(PlayableGraph playableGraph, bool forceRepaint)
        {
            if (!forceRepaint && IsEqual(ref _playableGraph, ref playableGraph))
            {
                return;
            }

            _playableGraph = playableGraph;

            ClearView();

            PopulateView();

            EditorApplication.delayCall += () => FrameAll();
        }


        private void ClearView()
        {
            foreach (var playableOutputNode in _playableOutputNodes)
            {
                playableOutputNode.RemoveFromContainer();
            }

            _playableOutputNodes.Clear();

            GraphViewNode.LayoutInfo.Reset();
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
                var playableOutputTypeName = playableOutput.GetPlayableOutputType().Name;
                var playableOutputEditorName = playableOutput.GetEditorName();
                var playableOutputNode = new PlayableOutputNode(0, playableOutput)
                {
                    title = $"{playableOutputTypeName} ({playableOutputEditorName})"
                };
                playableOutputNode.AddToContainer(this);

                _playableOutputNodes.Add(playableOutputNode);
            }

            for (int i = 0; i < _playableOutputNodes.Count; i++)
            {
                _playableOutputNodes[i].CreateAndConnectInputNodes();
            }

            // calculate node layout
            var origin = Vector2.zero;
            for (int i = 0; i < _playableOutputNodes.Count; i++)
            {
                var outputNode = _playableOutputNodes[i];
                var hierarchySize = outputNode.GetHierarchySize();
                outputNode.CalculateLayout(hierarchySize, origin);

                origin.y += hierarchySize.y + NodeLayoutInfo.VerticalSpace;
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