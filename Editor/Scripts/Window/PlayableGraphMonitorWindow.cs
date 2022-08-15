using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
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

        private Label _nodeDescriptionLabel;

        private Button _showInspectorButton;

        private PlayableGraphView _graphView;

        private VisualElement _nodeInspector;

        private const float _nodeInspectorWidth = 250f;


        private void OnEnable()
        {
            _graphs.AddRange(PlayableUtility.GetAllGraphs());

            PlayableUtility.graphCreated += OnGraphCreated;
            PlayableUtility.destroyingGraph += OnDestroyingGraph;

            #region Toolbar

            _toolbar = new Toolbar();
            rootVisualElement.Add(_toolbar);

            // graph popup
            _graphPopupField = new PopupField<PlayableGraph>(_graphs, 0,
                GraphPopupFieldFormatter, GraphPopupFieldFormatter);
            _toolbar.Add(_graphPopupField);

            // frame all
            var frameAllButton = new Button(OnFrameAllButtonClicked)
            {
                name = "frame-all-button",
                text = "Frame All",
            };
            _toolbar.Add(frameAllButton);

            // inspector button
            _showInspectorButton = new Button(OnInspectorButtonClicked)
            {
                name = "inspector-button",
                text = "Show Inspector",
                style =
                {
                    backgroundColor = GraphTool.GetButtonBackgroundColor(true)
                },
                userData = true
            };
            _toolbar.Add(_showInspectorButton);

            #endregion


            // container
            var container = new VisualElement
            {
                name = "graph-container",
                style =
                {
                    //flexBasis = new StyleLength(1),
                    flexGrow = new StyleFloat(1),
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.FlexStart),
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            rootVisualElement.Add(container);


            #region Graph View

            _graphView = new PlayableGraphView
            {
                style =
                {
                    flexGrow = new StyleFloat(1)
                }
            };

            container.Add(_graphView);

            #endregion

            #region Node Inspector

            _nodeInspector = new VisualElement
            {
                name = "node-inspector",
                style =
                {
                    width = new Length(_nodeInspectorWidth, LengthUnit.Pixel),
                    height = new Length(100, LengthUnit.Percent),
                    backgroundColor = GraphTool.GetNodeInspectorBackgroundColor(),
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };

            var scrollView = new ScrollView
            {
                name = "node-desc-container"
            };
            _nodeDescriptionLabel = new Label
            {
                name = "node-desc-label",
                style =
                {
                    color = GraphTool.GetNodeInspectorTextColor()
                }
            };
            scrollView.Add(_nodeDescriptionLabel);
            _nodeInspector.Add(scrollView);

            container.Add(_nodeInspector);

            #endregion
        }

        private void OnDisable()
        {
            PlayableUtility.graphCreated -= OnGraphCreated;
            PlayableUtility.destroyingGraph -= OnDestroyingGraph;

            GraphTool.ClearGlobalCache();
        }

        private void Update()
        {
            _graphView.Update(_graphPopupField.value);

            DrawGraphNodeInspector();
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
                UpdatePlayableGraphPopupField();
            }
        }

        private void OnDestroyingGraph(PlayableGraph graph)
        {
            _graphs.Remove(graph);
            UpdatePlayableGraphPopupField();
        }

        private void UpdatePlayableGraphPopupField()
        {
            var index = _graphs.IndexOf(_graphPopupField.value);
            if (index == -1 && _graphs.Count > 0)
            {
                index = 0;
            }

            _graphPopupField.index = index;
            _graphPopupField.value = index > -1 ? _graphs[index] : new PlayableGraph();
            _graphPopupField.MarkDirtyRepaint();
        }

        private void DrawGraphNodeInspector()
        {
            if (!(bool)_showInspectorButton.userData)
            {
                return;
            }

            if (_graphView.selection.Count == 1 &&
                _graphView.selection[0] is GraphViewNode node)
            {
                _nodeDescriptionLabel.text = node.GetStateDescription();
                return;
            }

            _nodeDescriptionLabel.text = "Select one node to show details.";
        }

        private void OnFrameAllButtonClicked()
        {
            _graphView.FrameAll();
        }

        private void OnInspectorButtonClicked()
        {
            var showInspector = !(bool)_showInspectorButton.userData;
            _showInspectorButton.userData = showInspector;
            _showInspectorButton.style.backgroundColor = GraphTool.GetButtonBackgroundColor(showInspector);

            _nodeInspector.visible = showInspector;
            _nodeInspector.style.width = showInspector ? _nodeInspectorWidth : 0;
        }
    }
}
