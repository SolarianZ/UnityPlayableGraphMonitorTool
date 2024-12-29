using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Utility;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow
    {
        private const float _SIDEBAR_WIDTH = 250f;

        private VisualElement _sidebarPanel;

        private IMGUIContainer _nodeDescriptionLabel;

        private MiniMap _graphMiniMap;

        private StringBuilder _graphDescBuilder = new StringBuilder();


        private void CreateSidebar(VisualElement container)
        {
            // Sidebar
            _sidebarPanel = new VisualElement
            {
                name = "side-bar",
                style =
                {
                    width = new Length(_SIDEBAR_WIDTH, LengthUnit.Pixel),
                    height = new Length(100, LengthUnit.Percent),
                    backgroundColor = GraphTool.GetNodeInspectorBackgroundColor(),
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4,
                    display = _displayInspector ? DisplayStyle.Flex : DisplayStyle.None,
                }
            };
            container.Add(_sidebarPanel);

            // Node descriptions
            var scrollView = new ScrollView
            {
                name = "node-desc-container",
                style =
                {
                    flexGrow = 1,
                }
            };
            _nodeDescriptionLabel = new IMGUIContainer(DrawNodeDescription)
            {
                name = "node-desc-label",
                style =
                {
                    color = GraphTool.GetNodeInspectorTextColor(),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            scrollView.Add(_nodeDescriptionLabel);
            _sidebarPanel.Add(scrollView);

            // MiniMap
            _graphMiniMap = new MiniMap
            {
                anchored = true,
                windowed = true,
                graphView = _graphView,
                style =
                {
                    width = new Length(100, LengthUnit.Percent),
                    minHeight = 80,
                    maxHeight = _SIDEBAR_WIDTH,
                }
            };
            _sidebarPanel.Add(_graphMiniMap);
        }

        private void DrawInspector()
        {
            if (!_inspectorToggle.value)
            {
                return;
            }

            // MiniMap
            _graphMiniMap.MarkDirtyRepaint();
        }

        void DrawNodeDescription() {
            // Node description
            if (_graphView.selection.Count == 1 &&
                _graphView.selection[0] is GraphViewNode node)
            {
                node.DrawNodeDescription();
            }
            // PlayableGraph description
            else
            {
                GetPlayableGraphDescription();
            }
        }

        private void GetPlayableGraphDescription()
        {
            _graphDescBuilder.Clear();

            var playableGraph = _graphPopupField.value;
            if (!playableGraph.IsValid())
            {
                _graphDescBuilder.AppendLine("Invalid PlayableGraph");
            }
            else
            {
                const string LINE = "----------";

                _graphDescBuilder.AppendLine(playableGraph.GetEditorName())
                    .AppendLine(LINE)
                    .Append("HashCode: ").AppendLine(playableGraph.GetHashCode().ToString())
                    .AppendLine(LINE)
                    .Append("IsValid: ").AppendLine(playableGraph.IsValid().ToString())
                    .Append("IsDone: ").AppendLine(playableGraph.IsDone().ToString())
                    .Append("IsPlaying: ").AppendLine(playableGraph.IsPlaying().ToString())
                    .Append("TimeUpdateMode: ").AppendLine(playableGraph.GetTimeUpdateMode().ToString())
                    .AppendLine(LINE)
                    .Append("OutputCount: ").AppendLine(playableGraph.GetOutputCount().ToString())
                    .Append("PlayableCount: ").AppendLine(playableGraph.GetPlayableCount().ToString())
                    .Append("RootPlayableCount: ").AppendLine(playableGraph.GetRootPlayableCount().ToString())
                    .AppendLine(LINE)
                    .Append("Resolver: ").AppendLine(playableGraph.GetResolver()?.ToString() ?? "Null");
            }

            GUILayout.Label(_graphDescBuilder.ToString());
        }
    }
}