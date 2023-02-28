using System;
using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using PlayableUtility = UnityEditor.Playables.Utility;


namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow : EditorWindow, IHasCustomMenu
    {
        #region Static

        // ReSharper disable once Unity.IncorrectMethodSignature
        [MenuItem("Tools/Bamboo/PlayableGraph Monitor")]
        [MenuItem("Window/Analysis/PlayableGraph Monitor")]
        public static PlayableGraphMonitorWindow Open()
        {
            return GetWindow<PlayableGraphMonitorWindow>("Playable Graph Monitor");
        }

        public static PlayableGraphMonitorWindow Open(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            var window = Open();
            window._viewUpdateContext.NodeExtraLabelTable = nodeExtraLabelTable;
            return window;
        }

        public static bool TrySetNodeExtraLabelTable(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            if (_instance == null)
            {
                return false;
            }

            _instance._viewUpdateContext.NodeExtraLabelTable = nodeExtraLabelTable;
            return true;
        }

        private static long GetCurrentEditorTimeMs()
        {
            return (long)(EditorApplication.timeSinceStartup * 1000);
        }

        private static PlayableGraphMonitorWindow _instance;

        #endregion


        private readonly List<PlayableGraph> _graphs = new List<PlayableGraph>
        {
            // An invalid graph, for compatible with Unity 2019
            new PlayableGraph()
        };

        [SerializeField]
        private PlayableGraphViewUpdateContext _viewUpdateContext = new PlayableGraphViewUpdateContext();

        private long _nextUpdateViewTimeMS;

        // ReSharper disable once IdentifierTypo
        private bool _updateNodesMovability;


        private void OnEnable()
        {
            _instance = this;
            _graphs.AddRange(PlayableUtility.GetAllGraphs());
            PlayableUtility.graphCreated += OnGraphCreated;
            PlayableUtility.destroyingGraph += OnDestroyingGraph;

            CreateToolbar();

            var container = CreateGraphViewAndInspectorContainer();
            CreateGraphView(container);
            CreateNodeInspector(container);

            UpdatePlayableGraphPopupField();
        }

        private void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            PlayableUtility.graphCreated -= OnGraphCreated;
            PlayableUtility.destroyingGraph -= OnDestroyingGraph;

            GraphTool.ClearGlobalCache();
        }

        private void Update()
        {
            UpdateGraphView();
            DrawGraphNodeInspector();

            if (_updateNodesMovability)
            {
                _graphView.SetNodesMovability(_refreshRate == RefreshRate.Manual);
            }
        }

        private void UpdateGraphView()
        {
            var currentTimeMS = GetCurrentEditorTimeMs();
            if (currentTimeMS < _nextUpdateViewTimeMS)
            {
                return;
            }

            _nextUpdateViewTimeMS = currentTimeMS + (long)_refreshRate;

            // Hide error tip
            _errorTipLabel.style.display = DisplayStyle.None;

            try
            {
                _viewUpdateContext.PlayableGraph = _graphPopupField.value;
                _graphView.Update(_viewUpdateContext);
            }
            catch (StackOverflowException soe)
            {
                // Stop refreshing and stop calculating layout
                _refreshRateField.value = RefreshRate.Manual;
                _autoLayoutToggle.value = false;

                // Log errors
                var playableGraphName = _graphPopupField.value.GetEditorName();
                var message = $"There may be cycles in the PlayableGraph '{playableGraphName}'." +
                              $"You can set the refresh rate to '{RefreshRate.Manual}' and disable 'Auto Layout'" +
                              "and drag nodes manually to find out the cycle.";
                Debug.LogError(message, this);
                Debug.LogException(soe, this);

                // Display error tip
                _errorTipLabel.style.display = DisplayStyle.Flex;
            }
        }

        private VisualElement CreateGraphViewAndInspectorContainer()
        {
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

            return container;
        }


        #region PlayableGraph Events

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

        #endregion


        #region Context Menu

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Keep updating edges when mouse leave GraphView(will degrade performance)"),
                _viewUpdateContext.KeepUpdatingEdges, OnToggleKeepUpdatingEdges);
        }

        private void OnToggleKeepUpdatingEdges()
        {
            _viewUpdateContext.KeepUpdatingEdges = !_viewUpdateContext.KeepUpdatingEdges;
        }

        #endregion
    }
}