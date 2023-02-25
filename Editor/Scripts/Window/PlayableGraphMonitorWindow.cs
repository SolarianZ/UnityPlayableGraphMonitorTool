using System.Collections.Generic;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using PlayableUtility = UnityEditor.Playables.Utility;

namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow : EditorWindow
    {
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
            window.SetNodeExtraLabelTable(nodeExtraLabelTable);
            return window;
        }

        public static bool TrySetNodeExtraLabelTable(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            if (_instance == null)
            {
                return false;
            }

            _instance.SetNodeExtraLabelTable(nodeExtraLabelTable);
            return true;
        }

        private static PlayableGraphMonitorWindow _instance;


        private readonly List<PlayableGraph> _graphs = new List<PlayableGraph>
        {
            new PlayableGraph() // an invalid graph, for compatible with Unity 2019
        };

        private RefreshRate _refreshRate;

        private long _nextUpdateViewTimeMS;


        public void SetNodeExtraLabelTable(IReadOnlyDictionary<PlayableHandle, string> nodeExtraLabelTable)
        {
            _graphView.SetNodeExtraLabelTable(nodeExtraLabelTable);
        }


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
            var currentTimeMS = GetCurrentEditorTimeMs();
            if (currentTimeMS >= _nextUpdateViewTimeMS)
            {
                _nextUpdateViewTimeMS = currentTimeMS + (long)_refreshRate;
                _graphView.Update(_graphPopupField.value);
            }

            DrawGraphNodeInspector();
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


        private static long GetCurrentEditorTimeMs()
        {
            return (long)(EditorApplication.timeSinceStartup * 1000);
        }
    }
}