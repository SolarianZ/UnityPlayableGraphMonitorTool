using GBG.PlayableGraphMonitor.Editor.Node;
using GBG.PlayableGraphMonitor.Editor.Utility;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;


namespace GBG.PlayableGraphMonitor.Editor
{
    public partial class PlayableGraphMonitorWindow
    {
        private const float _SIDEBAR_WIDTH = 250f;

        private VisualElement _sidebarPanel;

        private Label _nodeDescriptionLabel;

        private MiniMap _graphMiniMap;


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
            _nodeDescriptionLabel = new Label
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

            // Node description
            if (_graphView.selection.Count == 1 &&
                _graphView.selection[0] is GraphViewNode node)
            {
                _nodeDescriptionLabel.text = node.GetNodeDescription();
                return;
            }

            _nodeDescriptionLabel.text = "Select one node to show details.";

            // MiniMap
            _graphMiniMap.MarkDirtyRepaint();
        }
    }
}