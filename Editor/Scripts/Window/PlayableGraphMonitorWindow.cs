using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.GraphView;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using PlayableUtility = UnityEditor.Playables.Utility;

namespace GBG.PlayableGraphMonitor.Editor
{
    public class PlayableGraphMonitorWindow : EditorWindow
    {
        [MenuItem("Window/Analysis/PlayableGraph Monitor")]
        public static PlayableGraphMonitorWindow Open()
        {
            return GetWindow<PlayableGraphMonitorWindow>("Playable Graph Monitor");
        }


        private readonly List<PlayableGraph> _graphs = new List<PlayableGraph>();

        private Toolbar _toolbar;

        private PopupField<PlayableGraph> _graphPopupField;

        private PlayableGraph _selectedGraph;

        private PlayableGraphView _graphView;

        private bool _isGraphsChanged;


        private void OnEnable()
        {
            _graphs.AddRange(PlayableUtility.GetAllGraphs());

            PlayableUtility.graphCreated += OnGraphCreated;
            PlayableUtility.destroyingGraph += OnDestroyingGraph;

            #region Toolbar

            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            _graphPopupField = new PopupField<PlayableGraph>(_graphs, 0,
                GraphPopupFieldFormatter, GraphPopupFieldFormatter);
            _toolbar.Add(_graphPopupField);

            #endregion

            #region Graph View

            _graphView = new PlayableGraphView
            {
                style =
                {
                    flexGrow = new StyleFloat(1) // grow to fill parent space
                }
            };
            rootVisualElement.Add(_graphView);

            #endregion
        }

        private void OnDisable()
        {
            PlayableUtility.graphCreated -= OnGraphCreated;
            PlayableUtility.destroyingGraph -= OnDestroyingGraph;
        }

        private void Update()
        {
            _graphView.SetPlayableGraph(_graphPopupField.value, _isGraphsChanged);
            _isGraphsChanged = false;
        }

        private string GraphPopupFieldFormatter(PlayableGraph graph)
        {
            if (graph.IsValid())
            {
                return graph.GetEditorName();
            }

            return "No PlayableGraph";
        }

        private void OnGraphCreated(PlayableGraph graph)
        {
            if (!_graphs.Contains(graph))
            {
                _graphs.Add(graph);
                _graphPopupField.MarkDirtyRepaint();
                _isGraphsChanged = true;
            }
        }

        private void OnDestroyingGraph(PlayableGraph graph)
        {
            _graphs.Remove(graph);
            _graphPopupField.MarkDirtyRepaint();
            _isGraphsChanged = true;
        }
    }
}