using GBG.PlayableGraphMonitor.Editor.GraphView;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Collections.Generic;
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

#if UNITY_2021_1_OR_NEWER
        private HelpBox _errorMessage;
#else
        private IMGUIContainer _errorMessageContainer;
        private string _errorMessage;
        private void DrawImGuiErrorMessage() => EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
#endif

        private long _nextUpdateViewTimeMS;

        // ReSharper disable once IdentifierTypo
        private bool _updateNodesMovability;


        private void OnEnable()
        {
            _instance = this;
            _graphs.AddRange(PlayableUtility.GetAllGraphs());
            PlayableUtility.graphCreated += OnGraphCreated;
            PlayableUtility.destroyingGraph += OnDestroyingGraph;

#if UNITY_2021_1_OR_NEWER
            _errorMessage = new HelpBox()
            {
                messageType = HelpBoxMessageType.Error,
                style =
                {
                    display = DisplayStyle.None,
                }
            };
            rootVisualElement.Add(_errorMessage);
#else
            _errorMessageContainer = new IMGUIContainer(DrawImGuiErrorMessage)
            {
                style =
                {
                    display = DisplayStyle.None,
                }
            };
            rootVisualElement.Add(_errorMessageContainer);
#endif


            CreateToolbar();

            var container = CreateGraphViewAndInspectorContainer();
            CreateGraphView(container);
            CreateSidebar(container);

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
            // TODO FIXME CLIP_PROGRESS: If I reverse the order of these two method calls, the progress bar on the Clip node will become inaccurate. Why???
            DrawInspector();
            UpdateGraphView();

            if (_updateNodesMovability)
            {
                _graphView.SetNodesMovability(!_autoLayoutToggle.value);
            }
        }

        private void ShowButton(Rect position)
        {
            if (GUI.Button(position, EditorGUIUtility.IconContent("_Help"), GUI.skin.FindStyle("IconButton")))
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityPlayableGraphMonitorTool");
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

            // Hide error message
#if UNITY_2021_1_OR_NEWER
            _errorMessage.style.display = DisplayStyle.None;
#else
            _errorMessageContainer.style.display = DisplayStyle.None;
#endif
            _viewUpdateContext.PlayableGraph = _graphPopupField.value;
            if (!_graphView.Update(_viewUpdateContext))
            {
                // Stop refreshing and stop calculating layout
                _refreshRateField.value = RefreshRate.Manual;
                _autoLayoutToggle.value = false;

                var errorMessage =
                    $"All Playable has a parent Playable, that means there is at least one cycle in the PlayableGraph '{_viewUpdateContext.PlayableGraph.GetEditorName()}'!\n" +
                    "If there is a group of Playables where each Playable serves as an input to another one or more Playables in the group (i.e., there is no root Playable), " +
                    "and none of them are connected to a PlayableOutput, then this group of Playables will not appear in the graph view.\n" +
                    $"You can set the refresh rate to '{RefreshRate.Manual}' and disable 'Auto Layout' and drag nodes manually to find out the displayed cycle.";
                // Display error message
#if UNITY_2021_1_OR_NEWER
                _errorMessage.text = errorMessage;
                _errorMessage.style.display = DisplayStyle.Flex;
#else
                _errorMessage = errorMessage;
                _errorMessageContainer.style.display = DisplayStyle.Flex;
#endif
                Debug.LogError(errorMessage);
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
            // Source Code
            menu.AddItem(new GUIContent("Source Code"), false, () =>
            {
                Application.OpenURL("https://github.com/SolarianZ/UnityPlayableGraphMonitorTool");
            });
        }

        #endregion
    }
}