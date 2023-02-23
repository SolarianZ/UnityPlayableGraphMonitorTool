using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Collections.Generic;
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


        private readonly List<PlayableGraph> _graphs = new List<PlayableGraph>
        {
            new PlayableGraph() // an invalid graph, for compatible with Unity 2019
        };


        private void OnEnable()
        {
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
            PlayableUtility.graphCreated -= OnGraphCreated;
            PlayableUtility.destroyingGraph -= OnDestroyingGraph;

            GraphTool.ClearGlobalCache();
        }

        private void Update()
        {
            // _graphView.Update(_graphPopupField.value);
            _graphView.Update_New(_graphPopupField.value);


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
    }
}